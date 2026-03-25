// File    : FieldLookupConfig.cs
// Module  : Form
// Layer   : Domain
// Purpose : Cấu hình FK lookup cho field dynamic (Lookup_Source = 'dynamic').
//           Quan hệ 1-1 với FieldMetadata. Maps từ bảng Ui_Field_Lookup.

namespace ICare247.Domain.Entities.Form;

/// <summary>
/// Cấu hình truy vấn dữ liệu nguồn cho field lookup động.
/// Chỉ tồn tại khi <see cref="FieldMetadata.LookupSource"/> = <c>"dynamic"</c>.
/// Maps từ bảng <c>Ui_Field_Lookup</c>.
/// </summary>
public sealed class FieldLookupConfig
{
    /// <summary>Khóa chính trong bảng Ui_Field_Lookup.</summary>
    public int LookupCfgId { get; init; }

    /// <summary>Field sở hữu config này (FK 1-1 với Ui_Field).</summary>
    public int FieldId { get; init; }

    /// <summary>
    /// Chế độ truy vấn dữ liệu nguồn.
    /// "table"      = SELECT từ bảng hoặc view (phổ biến nhất).
    /// "tvf"        = Table-Valued Function — truyền tham số phức tạp.
    /// "custom_sql" = SQL tùy chỉnh do người cấu hình nhập.
    /// </summary>
    public string QueryMode { get; init; } = "table";

    /// <summary>
    /// Tên bảng, view, TVF hoặc câu SQL tùy chỉnh — tùy theo <see cref="QueryMode"/>.
    /// </summary>
    public string SourceName { get; init; } = string.Empty;

    /// <summary>Cột lưu vào DB khi user chọn (ví dụ: "PhongBanID").</summary>
    public string ValueColumn { get; init; } = string.Empty;

    /// <summary>Cột hiển thị trong dropdown (ví dụ: "TenPhongBan").</summary>
    public string DisplayColumn { get; init; } = string.Empty;

    /// <summary>
    /// Mệnh đề WHERE tùy chọn (parameterized — KHÔNG string interpolation).
    /// Hỗ trợ system params: @TenantId, @Today, @CurrentUser.
    /// Hỗ trợ field params dạng @FieldCode cho cascading lookup.
    /// Ví dụ: "Is_Active = 1 AND Tenant_Id = @TenantId".
    /// </summary>
    public string? FilterSql { get; init; }

    /// <summary>Mệnh đề ORDER BY (ví dụ: "TenPhongBan ASC"). Null = không sắp xếp.</summary>
    public string? OrderBy { get; init; }

    /// <summary>Cho phép tìm kiếm trong dropdown hay không.</summary>
    public bool SearchEnabled { get; init; } = true;

    /// <summary>
    /// Cấu hình các cột hiển thị trong popup grid dạng JSON array.
    /// Ví dụ: [{"column":"MaPhongBan","title":"Mã","width":80}, ...].
    /// Null = chỉ hiển thị <see cref="DisplayColumn"/>, không dùng popup grid.
    /// </summary>
    public string? PopupColumnsJson { get; init; }
}
