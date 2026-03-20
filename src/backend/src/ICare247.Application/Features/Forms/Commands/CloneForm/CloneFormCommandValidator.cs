// File    : CloneFormCommandValidator.cs
// Module  : Forms
// Layer   : Application
// Purpose : Validate input cho CloneFormCommand.

using FluentValidation;

namespace ICare247.Application.Features.Forms.Commands.CloneForm;

public sealed class CloneFormCommandValidator : AbstractValidator<CloneFormCommand>
{
    public CloneFormCommandValidator()
    {
        RuleFor(c => c.SourceFormCode)
            .NotEmpty().WithMessage("SourceFormCode không được trống.");

        RuleFor(c => c.NewFormCode)
            .NotEmpty().WithMessage("NewFormCode không được trống.")
            .MaximumLength(100).WithMessage("NewFormCode tối đa 100 ký tự.")
            .Matches(@"^[A-Z0-9_]+$").WithMessage("NewFormCode chỉ chấp nhận chữ HOA, số và dấu gạch dưới.");

        RuleFor(c => c.TenantId)
            .GreaterThan(0).WithMessage("TenantId phải lớn hơn 0.");

        RuleFor(c => c.CreatedBy)
            .NotEmpty().WithMessage("CreatedBy không được trống.");
    }
}
