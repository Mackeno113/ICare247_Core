// File    : MenuAdminDtos.cs
// Module  : Admin/Menu
// Layer   : Application
// Purpose : DTO + hằng cho màn Menu Builder (cấu hình cây menu HT_ChucNang ở Data DB tenant).
//           Thiết kế mở rộng: NodeKind discriminator {Group, View, Form} — backend xử lý cả Form
//           (LC3) dù UI v1 chỉ phơi Group/View (LC1). Nâng cấp = bật dropdown Form ở client.

namespace ICare247.Application.Features.Admin.Menu;

/// <summary>Loại node menu — quyết định Loai/DuongDan/DoiTuong/LoaiDoiTuong khi lưu.</summary>
public static class MenuNodeKind
{
    /// <summary>Node nhóm (container) — không mở màn. Loai='Menu', không link.</summary>
    public const string Group = "Group";

    /// <summary>Node mở 1 Ui_View — DuongDan=/view/{code}, LoaiDoiTuong='View'.</summary>
    public const string View = "View";

    /// <summary>Node mở 1 Ui_Form (master-data) — DuongDan=/master/{code}, LoaiDoiTuong='Form'. (LC3)</summary>
    public const string Form = "Form";

    public static bool IsValid(string? kind) =>
        kind is Group or View or Form;
}

/// <summary>
/// Một node trong cây menu (đọc cho màn Menu Builder). Client dựng cây theo <paramref name="ChaId"/>.
/// </summary>
/// <param name="Id">HT_ChucNang.Id.</param>
/// <param name="Ma">Khóa kỹ thuật của node.</param>
/// <param name="Ten">Tên hiển thị trên menu.</param>
/// <param name="ChaId">Id node cha (null = gốc).</param>
/// <param name="Loai">Menu (nhóm) | ManHinh (lá mở màn).</param>
/// <param name="Module">Mã phân hệ (NS, TC, HT…).</param>
/// <param name="DuongDan">Route khi bấm (vd /view/Grid_KhachHang). Null với node nhóm.</param>
/// <param name="Icon">Tên icon.</param>
/// <param name="ThuTu">Thứ tự trong cùng cấp.</param>
/// <param name="KichHoat">Tenant bật/tắt node.</param>
/// <param name="DoiTuong">Mã đối tượng engine để chặn quyền (ViewCode/FormCode).</param>
/// <param name="LoaiDoiTuong">'View' | 'Form' | null.</param>
/// <param name="LaHeThong">1 = node base (đồng bộ từ master, không cho xóa) · 0 = custom.</param>
public sealed record MenuNodeDto(
    long Id, string Ma, string Ten, long? ChaId, string Loai, string? Module,
    string? DuongDan, string? Icon, int ThuTu, bool KichHoat,
    string? DoiTuong, string? LoaiDoiTuong, bool LaHeThong);

/// <summary>Một phân hệ (module) cho dropdown chọn Module ở Menu Builder.</summary>
/// <param name="Ma">Mã phân hệ (TC, NS, TL…) — khớp HT_ChucNang.Module.</param>
/// <param name="Ten">Tên hiển thị phân hệ.</param>
public sealed record ModuleOptionDto(string Ma, string Ten);
