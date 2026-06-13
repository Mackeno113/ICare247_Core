// File    : AdminPermissionModels.cs
// Module  : ICare247_UI (host)
// Layer   : Frontend (UI)
// Purpose : Model màn Phân quyền — vai trò + node ma trận quyền (mutable để DxCheckBox @bind).

namespace ICare247_UI.Models;

/// <summary>Vai trò để chọn ở màn Phân quyền (khớp RoleDto backend).</summary>
public sealed record RoleVm(long Id, string Ma, string Ten, string? MoTa, bool LaHeThong);

/// <summary>
/// 1 node trong ma trận quyền. Dùng class (KHÔNG record) vì DxCheckBox @bind-Checked cần
/// property có setter để cập nhật trạng thái tick. Khớp RolePermNodeDto backend.
/// </summary>
public sealed class PermNode
{
    public long Id { get; set; }
    public long? ChaId { get; set; }
    public string Ma { get; set; } = "";
    public string Ten { get; set; } = "";
    public string Loai { get; set; } = "";
    public int ThuTu { get; set; }
    public bool Xem { get; set; }
    public bool Them { get; set; }
    public bool Sua { get; set; }
    public bool Xoa { get; set; }
    public bool InAn { get; set; }
}
