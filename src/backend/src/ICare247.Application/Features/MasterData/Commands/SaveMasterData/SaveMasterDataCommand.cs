// File    : SaveMasterDataCommand.cs
// Module  : MasterData
// Layer   : Application
// Purpose : Command Insert (Id null) hoặc Update (Id có giá trị) 1 bản ghi danh mục.

using ICare247.Application.Features.MasterData.Models;
using MediatR;

namespace ICare247.Application.Features.MasterData.Commands.SaveMasterData;

/// <param name="FormCode">Mã form danh mục.</param>
/// <param name="TenantId">Tenant hiện tại.</param>
/// <param name="Id">PK của bản ghi: null = Insert, có giá trị = Update.</param>
/// <param name="Values">Cặp Cột↔Giá trị (key = tên cột DB = Field_Code).</param>
/// <param name="UserId">Người thao tác (claim sub) — để engine bơm CreatedBy/UpdatedBy. Null = không bơm.</param>
/// <param name="Source">Ngữ cảnh ghi truyền cho hook after-save: "MANUAL" (nhập tay) | "IMPORT". Mặc định MANUAL.</param>
/// <param name="ImportSessionId">Phiên import (chỉ khi Source="IMPORT") → truyền vào hook <c>@ImportSessionId</c>. Null = nhập tay.</param>
public sealed record SaveMasterDataCommand(
    string FormCode,
    int    TenantId,
    object? Id,
    Dictionary<string, object?> Values,
    long? UserId = null,
    string Source = "MANUAL",
    Guid? ImportSessionId = null) : IRequest<MasterDataSaveResult>;
