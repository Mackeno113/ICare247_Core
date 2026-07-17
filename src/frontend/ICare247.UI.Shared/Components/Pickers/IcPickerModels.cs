// File    : IcPickerModels.cs
// Module  : Shared/Pickers
// Layer   : Frontend (Shared)
// Purpose : Model dùng chung cho bộ picker (spec 31): node cây, chế độ hiển thị,
//           hợp đồng nguồn dữ liệu công ty (host cài đặt — component tự nạp khi không truyền Items).

namespace ICare247.UI.Shared.Components.Pickers;

/// <summary>Chế độ hiển thị của picker dạng cây (spec 31 §4.1).</summary>
public enum IcPickerMode
{
    /// <summary>Dropdown chọn 1 giá trị (field trên form, switcher).</summary>
    Single,

    /// <summary>Cây checkbox chọn nhiều — WYSIWYG: tick cha lan toàn nhánh, bỏ tick con không rớt cha.</summary>
    MultiCheck
}

/// <summary>
/// 1 node dữ liệu của picker cây. <paramref name="CanAccess"/> = false → node tổ tiên trả kèm
/// chỉ để giữ cấu trúc cây: hiển thị mờ, không chọn/tick được.
/// </summary>
public sealed record IcPickerItem(long Id, string? Ma, string Ten, long? ParentId, bool CanAccess = true);

/// <summary>
/// Nguồn dữ liệu công ty theo quyền cho <c>IcCompanyPicker</c> khi màn không truyền Items.
/// Host cài đặt (gọi /api/v1/me/companies — gán riêng ∪ theo vai trò) và đăng ký DI.
/// </summary>
public interface ICompanyPickerSource
{
    /// <summary>Cây công ty user được truy cập (kèm tổ tiên CanAccess=false giữ cấu trúc).</summary>
    Task<IReadOnlyList<IcPickerItem>> GetCompaniesAsync(CancellationToken ct = default);
}
