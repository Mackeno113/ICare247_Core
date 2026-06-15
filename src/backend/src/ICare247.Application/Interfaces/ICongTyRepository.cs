// File    : ICongTyRepository.cs
// Module  : Organization / Companies
// Layer   : Application (Interfaces)
// Purpose : Hợp đồng truy cập dữ liệu công ty (TC_CongTy) trong Data DB tenant.
//           Tenant ngầm định qua IDataDbConnectionFactory (scoped) — KHÔNG truyền tenantId.

using ICare247.Application.Features.Organization.Companies.Models;

namespace ICare247.Application.Interfaces;

/// <summary>Đọc/ghi cây công ty + lookup phục vụ form (Data DB tenant). Parameterized 100%.</summary>
public interface ICongTyRepository
{
    /// <summary>Toàn bộ công ty (chưa xóa) dạng phẳng — UI dựng cây qua Id/ChaId.</summary>
    Task<IReadOnlyList<CompanyTreeNodeDto>> GetTreeAsync(CancellationToken ct = default);

    /// <summary>Chi tiết 1 công ty (kèm tên lookup đang chọn) cho form Sửa. Null nếu không có.</summary>
    Task<CompanyDetailDto?> GetByIdAsync(long id, CancellationToken ct = default);

    /// <summary>Option cấp công ty (TC_CapCongTy) — tập nhỏ, sắp theo ThuTu.</summary>
    Task<IReadOnlyList<LookupOptionDto>> GetCapCongTyOptionsAsync(CancellationToken ct = default);

    /// <summary>Option ngân hàng (DM_NganHang) — Extra = tên viết tắt.</summary>
    Task<IReadOnlyList<LookupOptionDto>> GetNganHangOptionsAsync(CancellationToken ct = default);

    /// <summary>Tìm phường-xã theo từ khóa (TOP 50) — Extra = tên tỉnh/thành (cascade suy Tỉnh).</summary>
    Task<IReadOnlyList<LookupOptionDto>> SearchPhuongXaAsync(string? term, CancellationToken ct = default);

    /// <summary>Mã công ty đã tồn tại chưa (loại trừ chính bản ghi khi Sửa).</summary>
    Task<bool> ExistsMaAsync(string ma, long? excludeId, CancellationToken ct = default);

    /// <summary>Đặt <paramref name="newParentId"/> làm cha của <paramref name="id"/> có tạo vòng lặp cây không.</summary>
    Task<bool> WouldCreateCycleAsync(long id, long? newParentId, CancellationToken ct = default);

    /// <summary>Thêm công ty mới → trả Id. CreatedBy = userId (0 nếu hệ thống).</summary>
    Task<long> InsertAsync(CompanyInput input, long? userId, CancellationToken ct = default);

    /// <summary>Cập nhật công ty theo Id (bump Ver, UpdatedBy/At).</summary>
    Task UpdateAsync(long id, CompanyInput input, long? userId, CancellationToken ct = default);

    /// <summary>Số công ty con trực tiếp + số phòng ban đang gắn (để chặn xóa khi còn phụ thuộc).</summary>
    Task<(int Children, int Departments)> CountDependentsAsync(long id, CancellationToken ct = default);

    /// <summary>Xóa mềm công ty (IsDeleted=1, bump Ver).</summary>
    Task DeleteAsync(long id, long? userId, CancellationToken ct = default);
}
