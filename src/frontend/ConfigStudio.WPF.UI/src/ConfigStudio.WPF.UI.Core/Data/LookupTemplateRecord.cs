// File    : LookupTemplateRecord.cs
// Module  : Data
// Layer   : Core
// Purpose : DTO mẫu lookup dùng chung — maps từ bảng Ui_Lookup_Template (db/083, PICKER-P4).
//           Field chọn mẫu qua Ui_Field_Lookup.Template_Code; màn quản lý dùng toàn bộ cột.

namespace ConfigStudio.WPF.UI.Core.Data;

/// <summary>
/// Mẫu lookup đóng gói sẵn (nguồn + cột + filter), dùng cho cả combo Cấu hình Field
/// và màn CRUD mẫu lookup.
/// </summary>
public sealed class LookupTemplateRecord
{
    public int TemplateId { get; init; }

    /// <summary>Khóa nghiệp vụ (TPL_CONG_TY, TPL_PHUONG_XA…). Rỗng = sentinel "không dùng mẫu".</summary>
    public string TemplateCode { get; init; } = "";

    /// <summary>Tên hiển thị trong combo và lưới quản lý.</summary>
    public string Ten { get; init; } = "";

    /// <summary>Diễn giải cho admin (nguồn, điều kiện, migration phụ thuộc).</summary>
    public string? MoTa { get; init; }
    public string QueryMode { get; init; } = "table";
    public string SourceName { get; init; } = "";
    public string ValueColumn { get; init; } = "";
    public string DisplayColumn { get; init; } = "";
    public string? CodeField { get; init; }
    public string? FilterSql { get; init; }
    public string? OrderBy { get; init; }
    public string? PopupColumnsJson { get; init; }
    public string? ParentColumn { get; init; }

    /// <summary>
    /// JSON array tham số canonical admin phải map. Null/rỗng = mẫu không cần map gì
    /// vì token hệ thống được engine tự resolve.
    /// </summary>
    public string? CanonicalParams { get; init; }
    public bool IsActive { get; init; } = true;
    public bool IsSystem { get; init; }
    public bool IsCustomized { get; init; }
    public DateTime? SyncedAt { get; init; }
    public int? SourceVer { get; init; }
}
