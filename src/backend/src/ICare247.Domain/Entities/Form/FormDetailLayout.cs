// File    : FormDetailLayout.cs
// Module  : Form
// Layer   : Domain
// Purpose : Cách form master hiển thị các pane chi tiết + danh sách pane (rail workspace).

namespace ICare247.Domain.Entities.Form;

/// <summary>
/// Kết quả đọc cấu hình master-detail của một form: kiểu bố cục + danh sách pane.
/// Tách khỏi <see cref="FormMetadata"/> (đọc phòng thủ, không phá hot-path form load
/// với tenant chưa chạy db/106). <see cref="Layout"/>='Inline' + <see cref="Panes"/> rỗng =
/// form không có chi tiết (mặc định an toàn).
/// </summary>
public sealed class FormDetailLayout
{
    /// <summary>
    /// <c>'Inline'</c> = lưới chi tiết chèn trong thân form (Spec 30 chứng từ) ·
    /// <c>'Rail'</c> = workspace có rail điều hướng con (hồ sơ NS_).
    /// </summary>
    public string Layout { get; init; } = "Inline";

    /// <summary>Danh sách pane theo thứ tự <see cref="FormDetailPane.OrderNo"/>. Rỗng = không có chi tiết.</summary>
    public IReadOnlyList<FormDetailPane> Panes { get; init; } = [];

    /// <summary>Không có pane chi tiết nào (form thường).</summary>
    public bool IsEmpty => Panes.Count == 0;

    /// <summary>Giá trị mặc định an toàn: Inline, không pane (dùng khi tenant chưa migrate).</summary>
    public static FormDetailLayout None { get; } = new();
}
