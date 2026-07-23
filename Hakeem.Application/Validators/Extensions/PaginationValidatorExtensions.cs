using Hakeem.Application.Resources;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace Hakeem.Application.Validators.Extensions;

public static class PaginationValidatorExtensions
{

    public static IRuleBuilderOptions<T, int> ValidatePageNumber<T>(this IRuleBuilder<T, int> ruleBuilder, IStringLocalizer<SharedResource> localizer)
    {
        return ruleBuilder
            .GreaterThanOrEqualTo(1).WithMessage(localizer["Pagination.PageNumber.Invalid"].Value);
    }


    public static IRuleBuilderOptions<T, int> ValidatePageSize<T>(
        this IRuleBuilder<T, int> ruleBuilder,
        int minPageSize,
        int maxPageSize,
        IStringLocalizer<SharedResource> localizer)
    {
        return ruleBuilder
            .InclusiveBetween(minPageSize, maxPageSize)
            .WithMessage(string.Format(localizer["Pagination.PageSize.Invalid"].Value, minPageSize, maxPageSize));
    }
}
