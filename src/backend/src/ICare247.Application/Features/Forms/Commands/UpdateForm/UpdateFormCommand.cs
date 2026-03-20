// File    : UpdateFormCommand.cs
// Module  : Forms
// Layer   : Application
// Purpose : Command cập nhật form — Version++, recalc Checksum, ghi audit log.

using MediatR;

namespace ICare247.Application.Features.Forms.Commands.UpdateForm;

/// <summary>
/// Command cập nhật form trong Ui_Form.
/// Tự động Version++ và recalc Checksum.
/// </summary>
public sealed record UpdateFormCommand(
    string FormCode,
    int TableId,
    string Platform,
    string LayoutEngine,
    string? Description,
    int TenantId,
    string UpdatedBy
) : IRequest;
