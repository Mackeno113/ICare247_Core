// File    : DeactivateFormCommand.cs
// Module  : Forms
// Layer   : Application
// Purpose : Command vô hiệu hóa form — set Is_Active=0, invalidate cache, ghi audit log.

using MediatR;

namespace ICare247.Application.Features.Forms.Commands.DeactivateForm;

/// <summary>
/// Soft delete form: set Is_Active = 0.
/// Dữ liệu không bị xóa, có thể khôi phục bằng RestoreFormCommand.
/// </summary>
public sealed record DeactivateFormCommand(
    string FormCode,
    int TenantId,
    string ChangedBy
) : IRequest;
