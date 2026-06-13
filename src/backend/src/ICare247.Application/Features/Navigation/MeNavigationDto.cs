// File    : MeNavigationDto.cs
// Module  : Navigation
// Layer   : Application
// Purpose : DTO menu của người dùng hiện tại — cây node đã lọc theo quyền (Xem=1) kèm
//           cờ thao tác (Thêm/Sửa/Xóa/In) để client ẩn/hiện nút. Phục vụ GET /me/navigation.

namespace ICare247.Application.Features.Navigation;

/// <summary>
/// Một node menu user được thấy. Trả phẳng kèm <see cref="ChaMa"/> để client dựng cây
/// (sidebar lấy nhánh ViTriHienThi=Sidebar/Ca2; mỗi màn lấy nhánh con TrongMan/Ca2).
/// </summary>
/// <param name="Ma">Khóa chức năng ổn định (= HT_ChucNang.Ma), vd "hr.reward".</param>
/// <param name="Ten">Tên hiển thị (base/fallback; client dịch qua i18n theo Ma).</param>
/// <param name="ChaMa">Mã node cha (null = gốc).</param>
/// <param name="Loai">Menu / ManHinh / ChucNangCon.</param>
/// <param name="Module">Mã phân hệ (TC/NS/TL/TM/CN/BC/HT) — null với node nhóm.</param>
/// <param name="DuongDan">Route màn mở — null với node nhóm.</param>
/// <param name="Icon">Tên icon Lucide.</param>
/// <param name="ViTriHienThi">Sidebar / TrongMan / Ca2.</param>
/// <param name="ThuTu">Thứ tự trong cùng cấp.</param>
/// <param name="Xem">Có quyền xem/mở (node nhóm-tổ tiên có thể =false, chỉ là khung).</param>
/// <param name="Them">Quyền thêm mới.</param>
/// <param name="Sua">Quyền sửa.</param>
/// <param name="Xoa">Quyền xóa.</param>
/// <param name="InAn">Quyền in.</param>
public sealed record MeNavNodeDto(
    string Ma,
    string Ten,
    string? ChaMa,
    string Loai,
    string? Module,
    string? DuongDan,
    string? Icon,
    string ViTriHienThi,
    int ThuTu,
    bool Xem,
    bool Them,
    bool Sua,
    bool Xoa,
    bool InAn);

/// <summary>Toàn bộ menu user được thấy (đã gồm cờ quyền — gộp navigation + permissions).</summary>
/// <param name="Nodes">Danh sách node phẳng, đã sort theo ThuTu.</param>
public sealed record MeNavigationDto(IReadOnlyList<MeNavNodeDto> Nodes);
