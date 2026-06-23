// File    : AuditColumnTemplate.cs
// Module  : Core/Services
// Layer   : Core
// Purpose : Sinh các câu lệnh T-SQL ALTER (idempotent) bổ sung KHỐI CỘT AUTO chuẩn
//           (§0.1 spec 11) còn thiếu cho MỘT bảng — phục vụ nút "Kiểm tra cột chuẩn"
//           trong SysTableManager (thực thi TRỰC TIẾP lên Target DB).
//           Mỗi câu lệnh tự bọc IF COL_LENGTH(...) IS NULL → chỉ thêm cột chưa có,
//           chạy lại an toàn. KHÔNG có 'GO' (chạy được qua SqlCommand/Dapper).
//           Pure string builder — không chạm DB/IO (việc thực thi do service Infrastructure).
//           Bản đối ứng cấp-1-bảng của db/061_ensure_audit_columns.sql (script toàn DB).

namespace ConfigStudio.WPF.UI.Core.Services;

/// <summary>
/// Khuôn SQL bổ sung cột auto chuẩn (CreatedBy/CreatedAt/UpdatedBy/UpdatedAt/IsDeleted/Ver)
/// cho 1 bảng theo convention ICare247 (xem spec 11 §0.1; KHÔNG gồm Ma/Ten — archetype §0.2).
/// </summary>
public static class AuditColumnTemplate
{
    /// <summary>Khối cột auto chuẩn — đối chiếu theo đúng thứ tự này.</summary>
    public static readonly IReadOnlyList<string> RequiredColumns =
        ["CreatedBy", "CreatedAt", "UpdatedBy", "UpdatedAt", "IsDeleted", "Ver"];

    /// <summary>
    /// Trả về các cột auto còn THIẾU so với khối chuẩn, so khớp KHÔNG phân biệt hoa/thường.
    /// </summary>
    /// <param name="existingColumns">Danh sách tên cột hiện có trong bảng (Target DB).</param>
    public static IReadOnlyList<string> FindMissing(IEnumerable<string> existingColumns)
    {
        var have = new HashSet<string>(existingColumns, StringComparer.OrdinalIgnoreCase);
        return RequiredColumns.Where(c => !have.Contains(c)).ToList();
    }

    /// <summary>
    /// Sinh danh sách câu lệnh T-SQL thực thi được (không 'GO') cho các cột thiếu — mỗi câu là
    /// 1 batch tự bọc <c>IF COL_LENGTH IS NULL</c> (idempotent). Rỗng nếu không thiếu cột nào.
    /// CreatedBy dùng default tạm 0 cho bản ghi cũ rồi DROP (insert phải set tường minh).
    /// </summary>
    /// <param name="schema">Schema SQL (mặc định dbo).</param>
    /// <param name="tableCode">Tên bảng vật lý trong Target DB.</param>
    /// <param name="missing">Cột thiếu (từ <see cref="FindMissing"/>).</param>
    public static IReadOnlyList<string> BuildAlterStatements(
        string schema, string tableCode, IReadOnlyList<string> missing)
    {
        if (missing.Count == 0)
            return [];

        var s = string.IsNullOrWhiteSpace(schema) ? "dbo" : schema.Trim();
        var full = $"[{s}].[{tableCode}]";

        return missing
            .Select(col => BuildColumnStatement(full, tableCode, col))
            .Where(stmt => stmt.Length > 0)
            .ToList();
    }

    /// <summary>Sinh 1 batch ALTER (bọc IF COL_LENGTH, không 'GO') cho 1 cột auto theo kiểu/ràng buộc chuẩn.</summary>
    private static string BuildColumnStatement(string full, string tableCode, string col)
    {
        return col switch
        {
            // CreatedBy NOT NULL không default: default tạm 0 cho bản ghi cũ rồi DROP ngay.
            "CreatedBy" =>
                $"""
                IF COL_LENGTH('{full}', 'CreatedBy') IS NULL
                BEGIN
                    ALTER TABLE {full} ADD CreatedBy bigint NOT NULL CONSTRAINT [DF_{tableCode}_CreatedBy_tmp] DEFAULT(0);
                    ALTER TABLE {full} DROP CONSTRAINT [DF_{tableCode}_CreatedBy_tmp];
                END
                """,
            "CreatedAt" =>
                $"""
                IF COL_LENGTH('{full}', 'CreatedAt') IS NULL
                    ALTER TABLE {full} ADD CreatedAt datetime2 NOT NULL CONSTRAINT [DF_{tableCode}_CreatedAt] DEFAULT(sysutcdatetime());
                """,
            "UpdatedBy" =>
                $"""
                IF COL_LENGTH('{full}', 'UpdatedBy') IS NULL
                    ALTER TABLE {full} ADD UpdatedBy bigint NULL;
                """,
            "UpdatedAt" =>
                $"""
                IF COL_LENGTH('{full}', 'UpdatedAt') IS NULL
                    ALTER TABLE {full} ADD UpdatedAt datetime2 NULL;
                """,
            "IsDeleted" =>
                $"""
                IF COL_LENGTH('{full}', 'IsDeleted') IS NULL
                    ALTER TABLE {full} ADD IsDeleted bit NOT NULL CONSTRAINT [DF_{tableCode}_IsDeleted] DEFAULT(0);
                """,
            "Ver" =>
                $"""
                IF COL_LENGTH('{full}', 'Ver') IS NULL
                    ALTER TABLE {full} ADD Ver int NOT NULL CONSTRAINT [DF_{tableCode}_Ver] DEFAULT(0);
                """,
            _ => ""
        };
    }
}
