// File    : LookupTemplateRecord.cs
// Module  : Data
// Layer   : Core
// Purpose : DTO mẫu lookup dùng chung — maps từ bảng Ui_Lookup_Template (db/083, PICKER-P4).
//           Field chọn mẫu qua Ui_Field_Lookup.Template_Code; tham số canonical map ở Param_Map.

namespace ConfigStudio.WPF.UI.Core.Data;

/// <summary>1 mẫu lookup đóng gói sẵn (nguồn + cột + filter) chọn được từ màn Cấu hình Field.</summary>
public sealed class LookupTemplateRecord
{
    /// <summary>Khóa nghiệp vụ (TPL_CONG_TY, TPL_PHUONG_XA…). Rỗng = sentinel "không dùng mẫu".</summary>
    public string TemplateCode { get; init; } = "";

    /// <summary>Tên hiển thị trong combo.</summary>
    public string Ten { get; init; } = "";

    /// <summary>Diễn giải cho admin (nguồn, điều kiện, migration phụ thuộc).</summary>
    public string? MoTa { get; init; }

    /// <summary>
    /// JSON array tham số canonical admin phải map:
    /// [{"name":"TinhId","type":"bigint","required":true,"moTa":"Field Tỉnh/Thành trên form"}].
    /// Null/rỗng = mẫu không cần map gì (token hệ thống engine tự resolve).
    /// </summary>
    public string? CanonicalParams { get; init; }
}
