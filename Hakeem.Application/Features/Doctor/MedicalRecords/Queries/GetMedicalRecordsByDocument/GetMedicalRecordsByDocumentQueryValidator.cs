using FluentValidation;
using Hakeem.Application.Resources;
using Hakeem.Application.Validators.Extensions;
using Microsoft.Extensions.Localization;

namespace Hakeem.Application.Features.Doctor.MedicalRecords.Queries.GetMedicalRecordsByDocument;

public sealed class GetMedicalRecordsByDocumentQueryValidator
    : AbstractValidator<GetMedicalRecordsByDocumentQuery>
{
    public GetMedicalRecordsByDocumentQueryValidator(
        IStringLocalizer<SharedResource> localizer)
    {
        RuleFor(query => query.DocumentId).NotEmpty();
        RuleFor(query => query.PageNumber).ValidatePageNumber(localizer);
        RuleFor(query => query.PageSize).ValidatePageSize(1, 100, localizer);
    }
}
