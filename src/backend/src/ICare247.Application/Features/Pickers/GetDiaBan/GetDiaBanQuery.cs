// File    : GetDiaBanQuery.cs
// Module  : Pickers
// Layer   : Application
// Purpose : Query nguồn địa bàn cho picker (spec 31 §3): Tỉnh/Thành (ParentId null) hoặc
//           Xã/Phường theo tỉnh (+keyword), hoặc resolve 1 Xã/Phường theo Id.

using ICare247.Application.Interfaces;
using MediatR;

namespace ICare247.Application.Features.Pickers.GetDiaBan;

/// <param name="Id">Có giá trị → trả đúng 1 Xã/Phường theo Id (resolve giá trị đã lưu).</param>
/// <param name="ParentId">Null = danh sách Tỉnh/Thành; có giá trị = Xã/Phường thuộc tỉnh đó.</param>
/// <param name="Keyword">Lọc contains Ma/Ten (chỉ áp khi liệt kê Xã/Phường).</param>
/// <param name="Top">Giới hạn dòng Xã/Phường (mặc định 50, tối đa 200).</param>
public sealed record GetDiaBanQuery(long? Id, long? ParentId, string? Keyword, int Top)
    : IRequest<IReadOnlyList<PickerItemDto>>;

public sealed class GetDiaBanQueryHandler : IRequestHandler<GetDiaBanQuery, IReadOnlyList<PickerItemDto>>
{
    private readonly IPickerRepository _repo;

    public GetDiaBanQueryHandler(IPickerRepository repo) => _repo = repo;

    /// <summary>Định tuyến theo tham số: Id → 1 xã; ParentId → xã theo tỉnh; còn lại → tỉnh.
    /// Sự kiện theo sau: FE đổ vào IcAddressBlock.</summary>
    public async Task<IReadOnlyList<PickerItemDto>> Handle(GetDiaBanQuery r, CancellationToken ct)
    {
        if (r.Id is { } id)
        {
            var item = await _repo.GetPhuongXaByIdAsync(id, ct);
            return item is null ? [] : [item];
        }

        if (r.ParentId is { } tinhId)
        {
            var top = Math.Clamp(r.Top, 1, 200);
            return await _repo.SearchPhuongXaAsync(tinhId, r.Keyword, top, ct);
        }

        return await _repo.GetTinhThanhAsync(ct);
    }
}
