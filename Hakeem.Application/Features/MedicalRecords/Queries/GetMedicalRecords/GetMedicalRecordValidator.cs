using FluentValidation;
using Hakeem.Application.Resources;
using Hakeem.Application.Validators.Extensions;
using Microsoft.Extensions.Localization;

namespace Hakeem.Application.Features.MedicalRecords.Queries.GetMedicalRecords
{
    public sealed class GetMedicalRecordsQueryValidator:AbstractValidator<GetMedicalRecordsQuery>
    {
        public GetMedicalRecordsQueryValidator(IStringLocalizer<SharedResource> localizer)
        {
            RuleFor(x => x.PageNumber).ValidatePageNumber(localizer);

            RuleFor(x => x.PageSize).ValidatePageSize(1, 100, localizer);
        }
    }
}
