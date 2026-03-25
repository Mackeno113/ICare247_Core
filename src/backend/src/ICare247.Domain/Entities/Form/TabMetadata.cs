// File    : TabMetadata.cs
// Module  : Form
// Layer   : Domain
// Purpose : Metadata của một tab trong form — hỗ trợ layout multi-tab.
//           Nếu form có 0 hoặc 1 tab → FormRunner render phẳng (backward compat).

namespace ICare247.Domain.Entities.Form;

/// <summary>
/// Metadata của một tab trong form.
/// Maps từ bảng <c>Ui_Tab</c>.
/// </summary>
public sealed class TabMetadata
{
    /// <summary>Khóa chính trong bảng Ui_Tab.</summary>
    public int TabId { get; init; }

    /// <summary>Form chứa tab này.</summary>
    public int FormId { get; init; }

    /// <summary>Mã kỹ thuật duy nhất trong form (Ui_Tab.Tab_Code).</summary>
    public string TabCode { get; init; } = string.Empty;

    /// <summary>
    /// Resource key để lấy tiêu đề hiển thị qua Sys_Resource.
    /// Null = không hiển thị tab label.
    /// </summary>
    public string? TitleKey { get; init; }

    /// <summary>Icon tùy chọn (ví dụ: "person", "work", "star"). Null = không có icon.</summary>
    public string? IconKey { get; init; }

    /// <summary>Thứ tự hiển thị — tăng dần từ trái sang phải.</summary>
    public int OrderNo { get; init; }

    /// <summary>Tab mở mặc định khi form load. Mỗi form chỉ có 1 tab default.</summary>
    public bool IsDefault { get; init; }

    /// <summary>
    /// Danh sách sections thuộc tab này, đã sắp xếp theo Order_No.
    /// </summary>
    public IReadOnlyList<SectionMetadata> Sections { get; init; } = [];
}
