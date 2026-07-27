// File    : IErrorLogRepository.cs
// Module  : Admin/ErrorLogs
// Layer   : Application
// Purpose : Đọc NK_LoiHeThong (Audit DB tenant) cho màn Nhật ký lỗi.

using ICare247.Application.Features.Admin.ErrorLogs;

namespace ICare247.Application.Interfaces;

/// <summary>Đọc nhật ký lỗi 500 — lưới + chi tiết stack trace.</summary>
public interface IErrorLogRepository
{
    /// <summary>
    /// Danh sách lỗi trong khoảng thời gian, lọc thêm theo Mã lỗi (prefix CorrelationId) / Nguồn
    /// nếu có. KHÔNG phân trang server (DxGrid tự sort/filter/phân trang phía client trong
    /// khoảng thời gian đã lọc — nhất quán quy ước lưới hiện có của dự án).
    /// </summary>
    Task<IReadOnlyList<ErrorLogListItemDto>> GetErrorLogsAsync(
        DateTime tuUtc, DateTime denUtc, string? maLoi, string? nguon, CancellationToken ct = default);

    /// <summary>Chi tiết 1 lỗi (message + stack trace đầy đủ). Null nếu không tìm thấy.</summary>
    Task<ErrorLogDetailDto?> GetErrorLogDetailAsync(long id, CancellationToken ct = default);
}
