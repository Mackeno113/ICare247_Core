// File    : IUserGridLayoutRepository.cs
// Module  : UserPreference
// Layer   : Application
// Purpose : Repo layout lưới per-user trên Data DB (HT_NguoiDung_LuoiLayout). Data DB là per-tenant
//           (KHÔNG có Tenant_Id) → tenant đã nằm trong connection, không truyền tham số tenant.

namespace ICare247.Application.Interfaces;

/// <summary>
/// Đọc/ghi layout lưới (GridPersistentLayout JSON) theo người dùng + View. Lưu ở Data DB.
/// </summary>
public interface IUserGridLayoutRepository
{
    /// <summary>Lấy JSON layout đã lưu; <c>null</c> nếu user chưa tùy chỉnh View này.</summary>
    Task<string?> GetAsync(long userId, string viewCode, string platform, CancellationToken ct = default);

    /// <summary>Tạo mới hoặc cập nhật layout (UPSERT theo NguoiDung_Id + View_Code + Platform).</summary>
    Task UpsertAsync(long userId, string viewCode, string platform, string layoutJson, CancellationToken ct = default);

    /// <summary>Xóa layout của user cho View (về mặc định Ui_View).</summary>
    Task DeleteAsync(long userId, string viewCode, string platform, CancellationToken ct = default);
}
