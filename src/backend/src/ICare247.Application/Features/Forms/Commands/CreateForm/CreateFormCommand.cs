// File    : CreateFormCommand.cs
// Module  : Forms
// Layer   : Application
// Purpose : Command tạo form mới — validate Form_Code unique, set Version=1, ghi audit log.

using MediatR;

namespace ICare247.Application.Features.Forms.Commands.CreateForm;

/// <summary>
/// Command tạo form mới trong Ui_Form.
/// Trả về Form_Id vừa tạo.
/// </summary>
public sealed record CreateFormCommand(
    string FormCode,
    int TableId,
    string Platform,
    string LayoutEngine,
    string? Description,
    int TenantId,
    string CreatedBy
) : IRequest<int>;
