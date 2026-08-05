using FluentValidation;
using Hakeem.Application.Resources;
using Hakeem.Application.Validators.Extensions;
using Microsoft.Extensions.Localization;

namespace Hakeem.Application.Features.MedicalDocuments.Queries.GetDocuments;

public sealed class GetDocumentsQueryValidator : AbstractValidator<GetDocumentsQuery>
{
    public GetDocumentsQueryValidator(IStringLocalizer<SharedResource> localizer)
    {
        RuleFor(x => x.PageNumber).ValidatePageNumber(localizer);

        RuleFor(x => x.PageSize).ValidatePageSize(1, 100, localizer);
    }
}
