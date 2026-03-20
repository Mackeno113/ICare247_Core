// File    : CreateFormCommandValidator.cs
// Module  : Forms
// Layer   : Application
// Purpose : Validate input cho CreateFormCommand.

using FluentValidation;

namespace ICare247.Application.Features.Forms.Commands.CreateForm;

public sealed class CreateFormCommandValidator : AbstractValidator<CreateFormCommand>
{
    public CreateFormCommandValidator()
    {
        RuleFor(c => c.FormCode)
            .NotEmpty().WithMessage("FormCode không được trống.")
            .MaximumLength(100).WithMessage("FormCode tối đa 100 ký tự.")
            .Matches(@"^[A-Z0-9_]+$").WithMessage("FormCode chỉ chấp nhận chữ HOA, số và dấu gạch dưới.");

        RuleFor(c => c.TableId)
            .GreaterThan(0).WithMessage("TableId phải lớn hơn 0.");

        RuleFor(c => c.Platform)
            .NotEmpty().WithMessage("Platform không được trống.")
            .Must(p => p is "web" or "mobile" or "wpf")
            .WithMessage("Platform phải là 'web', 'mobile' hoặc 'wpf'.");

        RuleFor(c => c.LayoutEngine)
            .NotEmpty().WithMessage("LayoutEngine không được trống.");

        RuleFor(c => c.TenantId)
            .GreaterThan(0).WithMessage("TenantId phải lớn hơn 0.");

        RuleFor(c => c.CreatedBy)
            .NotEmpty().WithMessage("CreatedBy không được trống.");
    }
}
