// File    : HookStoreTemplate.cs
// Module  : Core/Services
// Layer   : Core
// Purpose : Sinh nội dung file .sql skeleton cho hook store của 1 màn (SVHOOK-5, ADR-029):
//           spc_Grid_<Table> (validate trước ghi) + sp_AfterSave_Grid_<Table> (hậu xử lý).
//           Skeleton RỖNG pass-through, bọc IF OBJECT_ID IS NULL → KHÔNG ghi đè logic đã viết tay.
//           Pure string builder — không chạm DB/IO (IO do ViewModel ghi file).

namespace ConfigStudio.WPF.UI.Core.Services;

/// <summary>
/// Khuôn SQL skeleton cho hook store theo convention ICare247 (xem spec 18).
/// </summary>
public static class HookStoreTemplate
{
    /// <summary>Tên store validate TRƯỚC khi ghi (vd <c>spc_Grid_DM_PhuongXa</c>).</summary>
    public static string ValidateProcName(string tableCode) => $"spc_Grid_{tableCode}";

    /// <summary>Tên store hậu xử lý SAU khi ghi (vd <c>sp_AfterSave_Grid_DM_PhuongXa</c>).</summary>
    public static string AfterSaveProcName(string tableCode) => $"sp_AfterSave_Grid_{tableCode}";

    /// <summary>Tên hook SAU IMPORT — chạy 1 lần cuối mẻ (vd <c>sp_AfterImport_DM_PhuongXa</c>). ADR-034 §12.2.</summary>
    public static string AfterImportProcName(string tableCode) => $"sp_AfterImport_{tableCode}";

    /// <summary>
    /// Skeleton <c>spc_Grid_&lt;Table&gt;</c> — validate, trả result set lỗi
    /// (error_key, args_json, field_name, severity). Rỗng = hợp lệ. Hiện pass-through.
    /// </summary>
    public static string BuildValidateProc(string schema, string tableCode)
    {
        var s = NormalizeSchema(schema);
        var proc = ValidateProcName(tableCode);
        return $$"""
            -- =============================================================================
            -- File    : {{proc}}.sql
            -- Database: Data DB (Target DB per-tenant)
            -- Purpose : SVHOOK — VALIDATE trước khi ghi cho màn {{tableCode}} (engine-driven).
            --           Trả result set lỗi: error_key, args_json, field_name, severity (RỖNG = hợp lệ).
            --           Token args (theo vị trí): {0}=giá trị · {1}=nhãn · {2}/{3}=giới hạn.
            --           field_name NULL = lỗi cấp form (banner). Handler resolve i18n server-side.
            -- Sinh bởi: ConfigStudio (skeleton RỖNG pass-through). IF OBJECT_ID IS NULL → KHÔNG ghi đè.
            -- Spec    : docs/spec/18_SAVE_VALIDATION_HOOK_SPEC.md · ADR-029.
            -- =============================================================================
            IF OBJECT_ID('{{s}}.{{proc}}','P') IS NULL
            EXEC('
            CREATE PROCEDURE {{s}}.{{proc}}
                @Id BIGINT, @TenantId INT, @NguoiDungID BIGINT,
                @LangCode NVARCHAR(10), @PayloadJson NVARCHAR(MAX)
            AS
            BEGIN
                SET NOCOUNT ON;
                -- Tách field từ payload, vd:
                --   DECLARE @Ma NVARCHAR(50) = JSON_VALUE(@PayloadJson, ''$.Ma'');
                -- Validate → nạp lỗi vào result set (args_json = mảng [giá trị, nhãn, ...]):
                --   SELECT N''sys.val.Unique'' AS error_key,
                --          N''["00433","Mã"]'' AS args_json,
                --          N''Ma'' AS field_name, N''error'' AS severity;
                -- Hiện tại: pass-through (không lỗi).
                SELECT TOP 0
                    CAST(NULL AS NVARCHAR(200)) AS error_key,
                    CAST(NULL AS NVARCHAR(MAX)) AS args_json,
                    CAST(NULL AS NVARCHAR(128)) AS field_name,
                    CAST(NULL AS NVARCHAR(20))  AS severity;
            END');
            GO

            """;
    }

    /// <summary>
    /// Skeleton <c>sp_AfterSave_Grid_&lt;Table&gt;</c> — hậu xử lý sau khi ghi.
    /// Trả result set lỗi (cùng contract spc_) nếu muốn rollback; rỗng = OK.
    /// </summary>
    public static string BuildAfterSaveProc(string schema, string tableCode)
    {
        var s = NormalizeSchema(schema);
        var proc = AfterSaveProcName(tableCode);
        return $$"""
            -- =============================================================================
            -- File    : {{proc}}.sql
            -- Database: Data DB (Target DB per-tenant)
            -- Purpose : SVHOOK — HẬU XỬ LÝ sau khi ghi cho màn {{tableCode}} (engine-driven).
            --           @Id = id thật vừa ghi (0=thêm mới, >0=cập nhật). @Source/@ImportSessionId = ngữ cảnh
            --           import (engine chỉ truyền khi IMPORT; save tay dùng DEFAULT). ADR-034 §12.1.
            --           Trả result set lỗi (cùng contract spc_) → rollback cả bản ghi; rỗng = OK.
            -- Sinh bởi: ConfigStudio (skeleton RỖNG). IF OBJECT_ID IS NULL → KHÔNG ghi đè.
            -- Spec    : docs/spec/18_SAVE_VALIDATION_HOOK_SPEC.md · ADR-029/034.
            -- =============================================================================
            IF OBJECT_ID('{{s}}.{{proc}}','P') IS NULL
            EXEC('
            CREATE PROCEDURE {{s}}.{{proc}}
                @Id BIGINT, @TenantId INT, @NguoiDungID BIGINT,
                @LangCode NVARCHAR(10), @PayloadJson NVARCHAR(MAX),
                @Source NVARCHAR(20) = N''MANUAL'', @ImportSessionId UNIQUEIDENTIFIER = NULL
            AS
            BEGIN
                SET NOCOUNT ON;
                -- Hậu xử lý ở đây (vd chỉ khi import: IF @Source = N''IMPORT'' ...). Hiện tại: pass-through.
                RETURN;
            END');
            GO

            """;
    }

    /// <summary>
    /// Skeleton <c>sp_AfterImport_&lt;Table&gt;</c> — hook chạy 1 LẦN cuối mẻ import (ADR-034 §12.2).
    /// Nhận thống kê mẻ + mảng Id đã ghi. Lỗi KHÔNG rollback dữ liệu đã ghi. Opt-in qua OBJECT_ID.
    /// </summary>
    public static string BuildAfterImportProc(string schema, string tableCode)
    {
        var s = NormalizeSchema(schema);
        var proc = AfterImportProcName(tableCode);
        return $$"""
            -- =============================================================================
            -- File    : {{proc}}.sql
            -- Database: Data DB (Target DB per-tenant)
            -- Purpose : HOOK SAU IMPORT cho màn {{tableCode}} — chạy 1 lần cuối mẻ (ADR-034 §12.2).
            --           Nhận thống kê mẻ + @RecordIdsJson (mảng Id đã ghi). Lỗi KHÔNG rollback dữ liệu.
            -- Sinh bởi: ConfigStudio (skeleton RỖNG). IF OBJECT_ID IS NULL → KHÔNG ghi đè.
            -- Spec    : docs/spec/25_FK_LOOKUP_SPEC.md §12.2 · ADR-034.
            -- =============================================================================
            IF OBJECT_ID('{{s}}.{{proc}}','P') IS NULL
            EXEC('
            CREATE PROCEDURE {{s}}.{{proc}}
                @ImportSessionId UNIQUEIDENTIFIER, @NguoiDungID BIGINT, @TenantId INT,
                @InsertedCount INT, @UpdatedCount INT, @ErrorCount INT,
                @RecordIdsJson NVARCHAR(MAX), @ImportedAt DATETIME
            AS
            BEGIN
                SET NOCOUNT ON;
                -- Tổng hợp cuối mẻ (vd tính lại cây cho các Id vừa import):
                --   SELECT value FROM OPENJSON(@RecordIdsJson)
                RETURN;
            END');
            GO

            """;
    }

    /// <summary>
    /// Tách 2 skeleton (validate + after-save) thành danh sách batch T-SQL thực thi được
    /// (bỏ dòng <c>GO</c>) để chạy trực tiếp lên Target DB qua SqlCommand. Mỗi batch vẫn bọc
    /// <c>IF OBJECT_ID IS NULL EXEC('CREATE PROCEDURE…')</c> → chỉ tạo khi chưa có, KHÔNG đè.
    /// </summary>
    public static IReadOnlyList<string> BuildProcBatches(string schema, string tableCode)
    {
        var batches = new List<string>();
        foreach (var script in new[]
        {
            BuildValidateProc(schema, tableCode),
            BuildAfterSaveProc(schema, tableCode),
            BuildAfterImportProc(schema, tableCode)
        })
            batches.AddRange(SplitOnGo(script));
        return batches;
    }

    /// <summary>Tách 1 script thành các batch theo dòng <c>GO</c> (đứng riêng), bỏ batch rỗng.</summary>
    private static IEnumerable<string> SplitOnGo(string script)
    {
        var current = new System.Text.StringBuilder();
        foreach (var line in script.Replace("\r\n", "\n").Split('\n'))
        {
            if (string.Equals(line.Trim(), "GO", StringComparison.OrdinalIgnoreCase))
            {
                var batch = current.ToString().Trim();
                if (batch.Length > 0) yield return batch;
                current.Clear();
            }
            else
            {
                current.AppendLine(line);
            }
        }
        var tail = current.ToString().Trim();
        if (tail.Length > 0) yield return tail;
    }

    private static string NormalizeSchema(string? schema)
        => string.IsNullOrWhiteSpace(schema) ? "dbo" : schema.Trim();
}
