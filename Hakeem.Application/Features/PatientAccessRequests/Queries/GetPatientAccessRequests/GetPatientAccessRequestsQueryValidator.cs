using FluentValidation;
using Hakeem.Application.Resources;
using Hakeem.Application.Validators.Extensions;
using Microsoft.Extensions.Localization;

namespace Hakeem.Application.Features.PatientAccessRequests.Queries.GetPatientAccessRequests;

public sealed class GetPatientAccessRequestsQueryValidator
    : AbstractValidator<GetPatientAccessRequestsQuery>
{
    public GetPatientAccessRequestsQueryValidator(
        IStringLocalizer<SharedResource> localizer)
    {
        RuleFor(query => query.PageNumber).ValidatePageNumber(localizer);
        RuleFor(query => query.PageSize).ValidatePageSize(1, 100, localizer);
    }
}
