// File    : CloneFormCommand.cs
// Module  : Forms
// Layer   : Application
// Purpose : Command nhân bản form — copy Form + Sections + Fields sang Form_Code mới.

using MediatR;

namespace ICare247.Application.Features.Forms.Commands.CloneForm;

/// <summary>
/// Nhân bản form: copy toàn bộ Sections, Fields, Events sang form mới.
/// Form mới có Version = 1 và Is_Active = 1.
/// Trả về Form_Id mới.
/// </summary>
public sealed record CloneFormCommand(
    string SourceFormCode,
    string NewFormCode,
    int TenantId,
    string CreatedBy
) : IRequest<int>;
