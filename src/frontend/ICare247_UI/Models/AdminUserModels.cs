// File    : AdminUserModels.cs
// Module  : ICare247_UI
// Purpose : ViewModel màn Người dùng (admin): dòng lưới, chi tiết + vai trò, node cây công ty truy cập.

namespace ICare247_UI.Models;

/// <summary>1 dòng trên lưới Người dùng (GET /api/v1/admin/users).</summary>
public sealed class UserListVm
{
    public long Id { get; set; }
    public string Ma { get; set; } = "";
    public string TenDangNhap { get; set; } = "";
    public string LoaiTaiKhoan { get; set; } = "";
    public string TrangThai { get; set; } = "";
    public bool LaQuanTri { get; set; }
    public bool KichHoatMobile { get; set; }
    public DateTime? HetHanTaiKhoan { get; set; }
    public DateTime? LanDangNhapCuoi { get; set; }
    public string? VaiTro { get; set; }
}

/// <summary>Chi tiết user (GET /api/v1/admin/users/{id}) — bind trực tiếp vào form tab Thông tin.</summary>
public sealed class UserDetailVm
{
    public long Id { get; set; }
    public string Ma { get; set; } = "";
    public string TenDangNhap { get; set; } = "";
    public string LoaiTaiKhoan { get; set; } = "Local";
    public string TrangThai { get; set; } = "HoatDong";
    public bool LaQuanTri { get; set; }
    public bool KichHoatMobile { get; set; }
    public DateTime? HetHanTaiKhoan { get; set; }
    public bool DoiMatKhauLanSau { get; set; }
    public List<UserRoleVm> VaiTro { get; set; } = [];
}

/// <summary>1 vai trò trong tab Vai trò (toàn bộ vai trò + cờ đã gán, tick tại chỗ).</summary>
public sealed class UserRoleVm
{
    public long Id { get; set; }
    public string Ma { get; set; } = "";
    public string Ten { get; set; } = "";
    public string? MoTa { get; set; }
    public bool DaGan { get; set; }
}

/// <summary>
/// 1 node cây công ty ở tab "Công ty truy cập" (GET /api/v1/admin/users/{id}/companies).
/// GanRieng = tick được (WYSIWYG); TheoVaiTro = kế thừa động, chỉ hiển thị badge.
/// </summary>
public sealed class UserCompanyNodeVm
{
    public long Id { get; set; }
    public string? Ma { get; set; }
    public string Ten { get; set; } = "";
    public long? ParentId { get; set; }
    public bool GanRieng { get; set; }
    public bool TheoVaiTro { get; set; }
    public bool LaMacDinh { get; set; }
}

/// <summary>1 node cây công ty ở phần "Phạm vi công ty" màn Phân quyền (theo vai trò).</summary>
public sealed class RoleCompanyNodeVm
{
    public long Id { get; set; }
    public string? Ma { get; set; }
    public string Ten { get; set; } = "";
    public long? ParentId { get; set; }
    public bool DaGan { get; set; }
}
