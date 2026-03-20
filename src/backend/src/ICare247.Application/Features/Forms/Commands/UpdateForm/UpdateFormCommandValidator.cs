// File    : UpdateFormCommandValidator.cs
// Module  : Forms
// Layer   : Application
// Purpose : Validate input cho UpdateFormCommand.

using FluentValidation;

namespace ICare247.Application.Features.Forms.Commands.UpdateForm;

public sealed class UpdateFormCommandValidator : AbstractValidator<UpdateFormCommand>
{
    public UpdateFormCommandValidator()
    {
        RuleFor(c => c.FormCode)
            .NotEmpty().WithMessage("FormCode không được trống.");

        RuleFor(c => c.TableId)
            .GreaterThan(0).WithMessage("TableId phải lớn hơn 0.");

        RuleFor(c => c.Platform)
            .NotEmpty().WithMessage("Platform không được trống.")
            .Must(p => p is "web" or "mobile" or "wpf")
            .WithMessage("Platform phải là 'web', 'mobile' hoặc 'wpf'.");

        RuleFor(c => c.TenantId)
            .GreaterThan(0).WithMessage("TenantId phải lớn hơn 0.");

        RuleFor(c => c.UpdatedBy)
            .NotEmpty().WithMessage("UpdatedBy không được trống.");
    }
}
