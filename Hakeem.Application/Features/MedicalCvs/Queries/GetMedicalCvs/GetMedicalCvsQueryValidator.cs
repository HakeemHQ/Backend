using FluentValidation;
using Hakeem.Application.Resources;
using Hakeem.Application.Validators.Extensions;
using Microsoft.Extensions.Localization;

namespace Hakeem.Application.Features.MedicalCvs.Queries.GetMedicalCvs;

public sealed class GetMedicalCvsQueryValidator : AbstractValidator<GetMedicalCvsQuery>
{
    public GetMedicalCvsQueryValidator(IStringLocalizer<SharedResource> localizer)
    {
        RuleFor(query => query.Page).ValidatePageNumber(localizer);
        RuleFor(query => query.PageSize).ValidatePageSize(1, 100, localizer);
    }
}
