// File    : IPickerRepository.cs
// Module  : Pickers
// Layer   : Application
// Purpose : Hợp đồng đọc dữ liệu Picker API (spec 31 §3) trên Data DB tenant — nguồn địa bàn
//           (DM_TinhThanhPho / DM_PhuongXa). Nguồn mới (nhân viên…) thêm method tại đây.

using ICare247.Application.Features.Pickers;

namespace ICare247.Application.Interfaces;

/// <summary>Đọc danh mục cho các picker dùng chung (read-only, đã lọc IsDeleted).</summary>
public interface IPickerRepository
{
    /// <summary>Toàn bộ Tỉnh/Thành phố active (ParentId = null).</summary>
    Task<IReadOnlyList<PickerItemDto>> GetTinhThanhAsync(CancellationToken ct = default);

    /// <summary>Xã/Phường thuộc 1 tỉnh, lọc keyword (Ma/Ten contains), giới hạn top dòng.</summary>
    Task<IReadOnlyList<PickerItemDto>> SearchPhuongXaAsync(
        long tinhThanhPhoId, string? keyword, int top, CancellationToken ct = default);

    /// <summary>1 Xã/Phường theo Id (resolve giá trị đã lưu — ParentId = tỉnh). Null = không có.</summary>
    Task<PickerItemDto?> GetPhuongXaByIdAsync(long id, CancellationToken ct = default);
}
