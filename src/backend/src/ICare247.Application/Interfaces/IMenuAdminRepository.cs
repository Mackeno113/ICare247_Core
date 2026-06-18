// File    : IMenuAdminRepository.cs
// Module  : Admin/Menu
// Layer   : Application
// Purpose : Repository ghi/đọc cây menu HT_ChucNang ở Data DB tenant (Menu Builder).
//           Tách biệt Config DB — chỉ chạm Data DB qua IDataDbConnectionFactory.

using ICare247.Application.Features.Admin.Menu;

namespace ICare247.Application.Interfaces;

/// <summary>Đọc/ghi node menu (HT_ChucNang) cho màn Menu Builder. Ghi đơn-DB (Data tenant), không cross-DB.</summary>
public interface IMenuAdminRepository
{
    /// <summary>Toàn bộ node menu chưa xóa (mọi trạng thái KichHoat), sắp theo cấp + ThuTu.</summary>
    Task<IReadOnlyList<MenuNodeDto>> GetTreeAsync(CancellationToken ct = default);

    /// <summary>Danh sách phân hệ (HT_PhanHe) đang bật, sắp theo ThuTu — cho dropdown Module.</summary>
    Task<IReadOnlyList<ModuleOptionDto>> GetModulesAsync(CancellationToken ct = default);

    /// <summary>Thêm node mới — trả Id vừa sinh. <paramref name="ma"/> đã được đảm bảo duy nhất ở handler.</summary>
    Task<long> InsertAsync(MenuNodeWrite node, long userId, CancellationToken ct = default);

    /// <summary>Cập nhật node theo Id. Không đổi Ma/LaHeThong. Trả về true nếu có dòng được sửa.</summary>
    Task<bool> UpdateAsync(long id, MenuNodeWrite node, long userId, CancellationToken ct = default);

    /// <summary>Soft-delete node (IsDeleted=1). Trả về true nếu xóa được.</summary>
    Task<bool> SoftDeleteAsync(long id, long userId, CancellationToken ct = default);

    /// <summary>Số node con trực tiếp còn sống — chặn xóa node đang có con.</summary>
    Task<int> CountActiveChildrenAsync(long id, CancellationToken ct = default);

    /// <summary>Node có LaHeThong=1 không (base — không cho xóa). Null nếu không tồn tại.</summary>
    Task<bool?> IsSystemNodeAsync(long id, CancellationToken ct = default);

    /// <summary>Ma đã tồn tại (node chưa xóa) chưa — để handler sinh Ma duy nhất.</summary>
    Task<bool> MaExistsAsync(string ma, CancellationToken ct = default);

    /// <summary>Kiểm tra <paramref name="parentId"/> có nằm trong nhánh con của <paramref name="nodeId"/> không (chống vòng lặp khi đổi cha).</summary>
    Task<bool> IsDescendantAsync(long nodeId, long parentId, CancellationToken ct = default);
}

/// <summary>Trường ghi của 1 node menu (đã resolve từ NodeKind ở handler).</summary>
/// <param name="Ma">Khóa node (chỉ dùng khi Insert).</param>
public sealed record MenuNodeWrite(
    string Ma, string Ten, long? ChaId, string Loai, string? Module,
    string? DuongDan, string? Icon, int ThuTu, bool KichHoat,
    string? DoiTuong, string? LoaiDoiTuong);
