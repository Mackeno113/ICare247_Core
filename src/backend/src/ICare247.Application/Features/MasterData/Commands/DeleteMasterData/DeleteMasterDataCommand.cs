// File    : DeleteMasterDataCommand.cs
// Module  : MasterData
// Layer   : Application
// Purpose : Command xóa cứng 1 bản ghi danh mục — chặn nếu đang bị tham chiếu (soft-check).

using ICare247.Application.Features.MasterData.Models;
using MediatR;

namespace ICare247.Application.Features.MasterData.Commands.DeleteMasterData;

public sealed record DeleteMasterDataCommand(string FormCode, int TenantId, object Id)
    : IRequest<MasterDataDeleteResult>;
