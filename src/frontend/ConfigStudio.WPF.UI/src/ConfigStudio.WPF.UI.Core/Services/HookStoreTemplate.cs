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
                @Id BIGINT, @TenantId INT, @NguoiThucHien BIGINT,
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
            --           @Id = id thật vừa ghi. Ghi log / đẩy event / tính toán liên quan…
            --           Trả result set lỗi (cùng contract spc_) → rollback cả bản ghi; rỗng = OK.
            -- Sinh bởi: ConfigStudio (skeleton RỖNG). IF OBJECT_ID IS NULL → KHÔNG ghi đè.
            -- Spec    : docs/spec/18_SAVE_VALIDATION_HOOK_SPEC.md · ADR-029.
            -- =============================================================================
            IF OBJECT_ID('{{s}}.{{proc}}','P') IS NULL
            EXEC('
            CREATE PROCEDURE {{s}}.{{proc}}
                @Id BIGINT, @TenantId INT, @NguoiThucHien BIGINT,
                @LangCode NVARCHAR(10), @PayloadJson NVARCHAR(MAX)
            AS
            BEGIN
                SET NOCOUNT ON;
                -- Hậu xử lý ở đây. Hiện tại: không làm gì (pass-through).
                RETURN;
            END');
            GO

            """;
    }

    private static string NormalizeSchema(string? schema)
        => string.IsNullOrWhiteSpace(schema) ? "dbo" : schema.Trim();
}
