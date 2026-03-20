// File    : RestoreFormCommand.cs
// Module  : Forms
// Layer   : Application
// Purpose : Command khôi phục form — set Is_Active=1, ghi audit log.

using MediatR;

namespace ICare247.Application.Features.Forms.Commands.RestoreForm;

/// <summary>
/// Khôi phục form đã bị vô hiệu hóa: set Is_Active = 1.
/// </summary>
public sealed record RestoreFormCommand(
    string FormCode,
    int TenantId,
    string ChangedBy
) : IRequest;
