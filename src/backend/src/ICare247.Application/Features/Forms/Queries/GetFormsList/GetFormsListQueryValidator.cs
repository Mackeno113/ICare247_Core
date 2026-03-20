// File    : GetFormsListQueryValidator.cs
// Module  : Forms
// Layer   : Application
// Purpose : Validate input cho GetFormsListQuery.

using FluentValidation;

namespace ICare247.Application.Features.Forms.Queries.GetFormsList;

public sealed class GetFormsListQueryValidator : AbstractValidator<GetFormsListQuery>
{
    public GetFormsListQueryValidator()
    {
        RuleFor(q => q.TenantId)
            .GreaterThan(0).WithMessage("TenantId phải lớn hơn 0.");

        RuleFor(q => q.Page)
            .GreaterThan(0).WithMessage("Page phải lớn hơn 0.");

        RuleFor(q => q.PageSize)
            .InclusiveBetween(1, 100).WithMessage("PageSize phải từ 1 đến 100.");
    }
}
