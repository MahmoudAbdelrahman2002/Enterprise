using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Application.Common.Models;
using FluentValidation;

namespace Enterprise.Application.Common.Validation;

public abstract class PaginationRules<T> : AbstractValidator<T> where T : PaginationParams
{
    protected PaginationRules(IAppLocalizer localizer)
    {
        RuleFor(x => x.PageSize).InclusiveBetween(1, ValidationPolicy.PageSizeMax).WithMessage(_ => localizer[MessageKeys.Validation.PageRange, ValidationPolicy.PageSizeMax]);
        RuleFor(x => x.PageNumber).Must((query, page) => page >= 1 && (long)(page - 1) * query.PageSize <= int.MaxValue)
            .WithMessage(_ => localizer[MessageKeys.Validation.PageRange, ValidationPolicy.PageSizeMax]);
    }
}
