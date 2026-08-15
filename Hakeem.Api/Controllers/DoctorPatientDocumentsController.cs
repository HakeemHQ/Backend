using FluentValidation;
using FluentValidation.AspNetCore;
using Hakeem.Api.Authorization;
using Hakeem.Application.Common.ResponseModel;
using Hakeem.Application.Constants;
using Hakeem.Application.Features.Doctor.Documents.Commands.UploadDocument;
using Hakeem.Application.Features.Doctor.Documents.Queries.GetPatientDocuments;
using Hakeem.Application.Resources;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace Hakeem.Api.Controllers;

[ApiController]
[Route("doctor/patients/{patientId:guid}/documents")]
[Authorize(Policy = DoctorPatientAccessPolicy.Name)]
public sealed class DoctorPatientDocumentsController : ApiControllerBase
{
    private readonly IMediator _mediator;
    private readonly IValidator<DoctorUploadDocumentCommand> _validator;

    public DoctorPatientDocumentsController(
        IMediator mediator,
        IValidator<DoctorUploadDocumentCommand> validator,
        IStringLocalizer<SharedResource> localizer)
        : base(localizer)
    {
        _mediator = mediator;
        _validator = validator;
    }

    [HttpGet]
    public async Task<IActionResult> GetDocuments(
        Guid patientId,
        [FromQuery] string? documentName,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new GetDoctorPatientDocumentsQuery(
                patientId,
                documentName,
                pageNumber,
                pageSize),
            cancellationToken);

        return SuccessResponse(result);
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(10_551_296)]
    [RequestFormLimits(MultipartBodyLengthLimit = 10_551_296)]
    [ProducesResponseType(
        typeof(GenericResponseModel<DoctorUploadDocumentResult>),
        StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(GenericResponseModel<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(GenericResponseModel<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(GenericResponseModel<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(GenericResponseModel<object>), StatusCodes.Status413PayloadTooLarge)]
    [ProducesResponseType(typeof(GenericResponseModel<object>), StatusCodes.Status415UnsupportedMediaType)]
    [ProducesResponseType(typeof(GenericResponseModel<object>), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UploadDocument(
        Guid patientId,
        [FromForm, CustomizeValidator(Skip = true)] DoctorUploadDocumentCommand request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(GenericResponseModel<object>.Failure(
                Localizer["Validation.InvalidRequest"].Value,
                "Validation.InvalidRequest"));
        }

        request.PatientProfileId = patientId;

        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .Select(error => ErrorResponseModel.Create(
                    error.PropertyName,
                    error.ErrorMessage,
                    error.ErrorCode))
                .ToList();

            var response = GenericResponseModel<object>.Failure(
                Localizer["Validation.Error"].Value,
                errors);

            return validationResult.Errors.Any(
                error => error.ErrorCode == ErrorCodes.ValidationRequired)
                ? BadRequest(response)
                : UnprocessableEntity(response);
        }

        var result = await _mediator.Send(request, cancellationToken);
        return CreatedResponse(result, ErrorCodes.DocumentUploaded);
    }
}
