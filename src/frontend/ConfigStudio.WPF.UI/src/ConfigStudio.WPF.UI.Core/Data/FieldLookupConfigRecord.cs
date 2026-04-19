// File    : FieldLookupConfigRecord.cs
// Module  : Data
// Layer   : Core
// Purpose : DTO cho cấu hình FK lookup động — maps từ bảng Ui_Field_Lookup.

namespace ConfigStudio.WPF.UI.Core.Data;

/// <summary>
/// Cấu hình truy vấn dữ liệu nguồn cho field LookupBox (Lookup_Source = 'dynamic').
/// Maps 1-1 từ bảng <c>Ui_Field_Lookup</c>.
/// </summary>
public sealed class FieldLookupConfigRecord
{
    public int FieldId { get; init; }

    /// <summary>"table" | "tvf" | "custom_sql"</summary>
    public string QueryMode { get; init; } = "table";

    /// <summary>Tên bảng, view, TVF hoặc câu SQL tùy theo QueryMode.</summary>
    public string SourceName { get; init; } = "";

    /// <summary>Cột lưu vào DB (FK). VD: "PhongBanID".</summary>
    public string ValueColumn { get; init; } = "";

    /// <summary>Cột hiển thị trong ô input. VD: "TenPhongBan".</summary>
    public string DisplayColumn { get; init; } = "";

    /// <summary>WHERE clause parameterized. Null = không lọc.</summary>
    public string? FilterSql { get; init; }

    /// <summary>ORDER BY clause. Null = không sắp xếp.</summary>
    public string? OrderBy { get; init; }

    /// <summary>Cho phép tìm kiếm trong dropdown.</summary>
    public bool SearchEnabled { get; init; } = true;

    /// <summary>
    /// JSON array cột popup: [{"fieldName":"MaPhongBan","caption":"Mã","width":80},...].
    /// Null = không dùng popup grid, chỉ hiện DisplayColumn.
    /// </summary>
    public string? PopupColumnsJson { get; init; }

    // ── LookupBox (DxDropDownBox) — thêm từ Migration 014 ─────────────

    /// <summary>
    /// Chế độ hiển thị EditBox khi đã chọn.
    /// "TextOnly" | "CodeAndName" | "Custom". Mặc định: "TextOnly".
    /// </summary>
    public string EditBoxMode { get; init; } = "TextOnly";

    /// <summary>Cột mã code trong data source — dùng khi EditBoxMode = "CodeAndName".</summary>
    public string? CodeField { get; init; }

    /// <summary>Chiều rộng popup grid (px). Mặc định: 600.</summary>
    public int DropDownWidth { get; init; } = 600;

    /// <summary>Chiều cao popup grid (px). Mặc định: 400.</summary>
    public int DropDownHeight { get; init; } = 400;

    /// <summary>
    /// FieldCode của field trigger cascading reload (đơn lẻ — backward compat).
    /// Dùng <see cref="ReloadTriggerFields"/> để hỗ trợ nhiều trigger.
    /// </summary>
    public string? ReloadTriggerField { get; init; }

    // ── Multi-trigger cascading + Tree Control (Migration 016) ────────────

    /// <summary>
    /// Danh sách FieldCode trigger phân cách bằng dấu phẩy — multi-trigger cascading.
    /// VD: "ProvinceId,DistrictId". Null = không có (xem ReloadTriggerField).
    /// </summary>
    public string? ReloadTriggerFields { get; init; }

    // ── Tree Control config ────────────────────────────────────────────────

    /// <summary>
    /// Tên cột ParentId trong bảng nguồn để build cây phân cấp.
    /// VD: "Parent_Id". Null = không phải TreePicker.
    /// </summary>
    public string? TreeParentColumn { get; init; }

    /// <summary>WHERE filter node gốc — dùng cho lazy load. VD: "Parent_Id IS NULL".</summary>
    public string? TreeRootFilter { get; init; }

    /// <summary>"all" | "leaf" | "branch" — node nào được phép chọn. Mặc định "all".</summary>
    public string TreeSelectableLevel { get; init; } = "all";

    /// <summary>"all_at_once" | "lazy" — cách load dữ liệu cây. Mặc định "all_at_once".</summary>
    public string TreeLoadMode { get; init; } = "all_at_once";
}
