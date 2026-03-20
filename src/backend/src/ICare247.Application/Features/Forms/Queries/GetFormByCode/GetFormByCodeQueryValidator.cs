// File    : GetFormByCodeQueryValidator.cs
// Module  : Forms
// Layer   : Application
// Purpose : Validate input cho GetFormByCodeQuery trước khi handler xử lý.

using FluentValidation;

namespace ICare247.Application.Features.Forms.Queries.GetFormByCode;

/// <summary>
/// Validate: FormCode không trống, TenantId > 0.
/// </summary>
public sealed class GetFormByCodeQueryValidator : AbstractValidator<GetFormByCodeQuery>
{
    public GetFormByCodeQueryValidator()
    {
        RuleFor(q => q.FormCode)
            .NotEmpty().WithMessage("FormCode không được trống.")
            .MaximumLength(100).WithMessage("FormCode tối đa 100 ký tự.");

        RuleFor(q => q.TenantId)
            .GreaterThan(0).WithMessage("TenantId phải lớn hơn 0.");

        RuleFor(q => q.LangCode)
            .NotEmpty().WithMessage("LangCode không được trống.")
            .MaximumLength(10).WithMessage("LangCode tối đa 10 ký tự.");

        RuleFor(q => q.Platform)
            .NotEmpty().WithMessage("Platform không được trống.");
    }
}
