using FluentValidation;
using Hakeem.Application.Resources;
using Hakeem.Application.Validators.Extensions;
using Microsoft.Extensions.Localization;

namespace Hakeem.Application.Features.DoctorPatientAccesses.Queries.GetDoctorPatientAccesses;

public sealed class GetDoctorPatientAccessesQueryValidator
    : AbstractValidator<GetDoctorPatientAccessesQuery>
{
    public GetDoctorPatientAccessesQueryValidator(
        IStringLocalizer<SharedResource> localizer)
    {
        RuleFor(query => query.PageNumber).ValidatePageNumber(localizer);
        RuleFor(query => query.PageSize).ValidatePageSize(1, 100, localizer);
    }
}
