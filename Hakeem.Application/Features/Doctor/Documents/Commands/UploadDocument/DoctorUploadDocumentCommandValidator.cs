using FluentValidation;
using Hakeem.Application.Constants;
using Hakeem.Application.Resources;
using Microsoft.Extensions.Localization;

namespace Hakeem.Application.Features.Doctor.Documents.Commands.UploadDocument;

public sealed class DoctorUploadDocumentCommandValidator
    : AbstractValidator<DoctorUploadDocumentCommand>
{
    public DoctorUploadDocumentCommandValidator(IStringLocalizer<SharedResource> localizer)
    {
        RuleFor(x => x.PatientProfileId)
            .NotEmpty()
            .WithMessage(localizer[ErrorCodes.ValidationRequired].Value)
            .WithErrorCode(ErrorCodes.ValidationRequired);

        RuleFor(x => x.File)
            .NotNull()
            .WithMessage(localizer[ErrorCodes.DocumentFileRequired].Value)
            .WithErrorCode(ErrorCodes.ValidationRequired)
            .Must(file => file is null || file.Length > 0)
            .WithMessage(localizer[ErrorCodes.DocumentFileRequired].Value)
            .WithErrorCode(ErrorCodes.ValidationRequired);

        RuleFor(x => x.Title)
            .NotEmpty()
            .WithMessage(localizer[ErrorCodes.ValidationRequired].Value)
            .WithErrorCode(ErrorCodes.ValidationRequired)
            .MaximumLength(200)
            .WithMessage(localizer[ErrorCodes.DocumentTitleTooLong].Value)
            .WithErrorCode(ErrorCodes.DocumentTitleTooLong);

        RuleFor(x => x.DocumentDate)
            .NotEmpty()
            .WithMessage(localizer[ErrorCodes.ValidationRequired].Value)
            .WithErrorCode(ErrorCodes.ValidationRequired)
            .LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage(localizer[ErrorCodes.DocumentDateInFuture].Value)
            .WithErrorCode(ErrorCodes.DocumentDateInFuture);
    }
}
