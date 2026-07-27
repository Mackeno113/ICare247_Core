// File    : GetErrorLogsQuery.cs
// Module  : Admin/ErrorLogs
// Layer   : Application
// Purpose : Query danh sách lỗi cho lưới màn Nhật ký lỗi (admin). Mặc định 7 ngày gần nhất
//           nếu FE không truyền khoảng thời gian — tránh full scan bảng log tăng vô hạn.

using ICare247.Application.Interfaces;
using MediatR;

namespace ICare247.Application.Features.Admin.ErrorLogs.GetErrorLogs;

/// <summary>
/// Lọc theo khoảng thời gian (UTC) + Mã lỗi (prefix CorrelationId, đúng 8 ký tự hiện trên toast)
/// + Nguồn (loại exception, contains). Không phân trang server — trả toàn bộ trong khoảng thời
/// gian để DxGrid tự sort/filter phía client (nhất quán quy ước lưới hiện có của dự án).
/// </summary>
public sealed record GetErrorLogsQuery(
    DateTime? Tu, DateTime? Den, string? MaLoi, string? Nguon) : IRequest<IReadOnlyList<ErrorLogListItemDto>>;

public sealed class GetErrorLogsQueryHandler : IRequestHandler<GetErrorLogsQuery, IReadOnlyList<ErrorLogListItemDto>>
{
    private const int DefaultDays = 7;

    private readonly IErrorLogRepository _repo;

    public GetErrorLogsQueryHandler(IErrorLogRepository repo) => _repo = repo;

    /// <summary>Đọc danh sách lỗi. Sự kiện theo sau: FE đổ vào lưới màn Nhật ký lỗi.</summary>
    public Task<IReadOnlyList<ErrorLogListItemDto>> Handle(GetErrorLogsQuery r, CancellationToken ct)
    {
        var denUtc = r.Den ?? DateTime.UtcNow;
        var tuUtc = r.Tu ?? denUtc.AddDays(-DefaultDays);
        return _repo.GetErrorLogsAsync(tuUtc, denUtc, r.MaLoi, r.Nguon, ct);
    }
}
