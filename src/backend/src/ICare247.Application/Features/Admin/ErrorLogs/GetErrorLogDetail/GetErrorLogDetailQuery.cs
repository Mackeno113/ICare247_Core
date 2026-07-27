// File    : GetErrorLogDetailQuery.cs
// Module  : Admin/ErrorLogs
// Layer   : Application
// Purpose : Query chi tiết 1 lỗi (message + stack trace đầy đủ) cho popup chi tiết màn Nhật ký lỗi.

using ICare247.Application.Interfaces;
using MediatR;

namespace ICare247.Application.Features.Admin.ErrorLogs.GetErrorLogDetail;

public sealed record GetErrorLogDetailQuery(long Id) : IRequest<ErrorLogDetailDto?>;

public sealed class GetErrorLogDetailQueryHandler : IRequestHandler<GetErrorLogDetailQuery, ErrorLogDetailDto?>
{
    private readonly IErrorLogRepository _repo;

    public GetErrorLogDetailQueryHandler(IErrorLogRepository repo) => _repo = repo;

    /// <summary>Đọc chi tiết 1 lỗi. Sự kiện theo sau: FE mở modal hiện stack trace copy-able.</summary>
    public Task<ErrorLogDetailDto?> Handle(GetErrorLogDetailQuery r, CancellationToken ct)
        => _repo.GetErrorLogDetailAsync(r.Id, ct);
}
