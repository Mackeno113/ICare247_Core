// File    : IPermissionService.cs
// Module  : Authorization
// Layer   : Application
// Purpose : Kiểm tra quyền của 1 người dùng trên 1 chức năng (HT_ChucNang.Ma) + thao tác —
//           phục vụ enforce ở server (deny-by-default). Đọc HT_VaiTro_Quyen theo vai trò user.

namespace ICare247.Application.Interfaces;

/// <summary>Thao tác cần kiểm quyền (khớp cờ HT_VaiTro_Quyen; Duyệt → workflow, không ở đây).</summary>
public enum PermissionOp
{
    Xem,
    Them,
    Sua,
    Xoa,
    InAn
}

/// <summary>Dịch vụ kiểm quyền runtime — dùng bởi attribute [RequirePermission].</summary>
public interface IPermissionService
{
    /// <summary>
    /// true nếu user (qua các vai trò) có <paramref name="op"/> trên chức năng <paramref name="funcCode"/>.
    /// Không có dòng/cờ = false (deny-by-default).
    /// </summary>
    Task<bool> HasPermissionAsync(long userId, string funcCode, PermissionOp op, CancellationToken ct = default);

    /// <summary>
    /// Kiểm quyền theo "đối tượng" engine (Ui_Form/Ui_View) cho endpoint generic.
    /// <b>Enforce-if-mapped:</b> nếu KHÔNG có node chức năng nào gắn (<c>LoaiDoiTuong</c>,<c>DoiTuong</c>)
    /// = (<paramref name="targetType"/>,<paramref name="targetCode"/>) → trả true (chưa cấu hình thì không khóa).
    /// Có gắn → mới áp deny-by-default theo <paramref name="op"/>.
    /// </summary>
    Task<bool> HasPermissionForTargetAsync(
        long userId, string targetType, string targetCode, PermissionOp op, CancellationToken ct = default);
}
