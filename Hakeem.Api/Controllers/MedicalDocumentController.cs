using FluentValidation;
using FluentValidation.AspNetCore;
using Hakeem.Application.Common.ResponseModel;
using Hakeem.Application.Constants;
using Hakeem.Application.Common;
using Hakeem.Application.Features.MedicalDocuments.Commands.UploadDocument;
using Hakeem.Application.Features.MedicalDocuments.DTOs;
using Hakeem.Application.Features.MedicalDocuments.Queries.GetDocumentById;
using Hakeem.Application.Features.MedicalDocuments.Queries.GetDocuments;
using Hakeem.Application.Resources;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace Hakeem.Api.Controllers;

[ApiController]
[Route("documents")]
[Authorize(Roles = "Patient")]
public sealed class MedicalDocumentController : ApiControllerBase
{
    private readonly IMediator _mediator;
    private readonly IValidator<UploadDocumentCommand> _validator;

    public MedicalDocumentController(
        IMediator mediator,
        IValidator<UploadDocumentCommand> validator,
        IStringLocalizer<SharedResource> localizer)
        : base(localizer)
    {
        _mediator = mediator;
        _validator = validator;
    }

    [HttpGet]
    public async Task<IActionResult> GetDocuments(
        [FromQuery] GetDocumentsQuery query,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(query, cancellationToken);
        return SuccessResponse(result);
    }

    [HttpGet("{documentId:guid}")]
    public async Task<IActionResult> GetDocumentById(
        Guid documentId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetDocumentByIdQuery(documentId),
            cancellationToken);
        return SuccessResponse(result);
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(10_551_296)]
    [RequestFormLimits(MultipartBodyLengthLimit = 10_551_296)]
    [ProducesResponseType(
        typeof(GenericResponseModel<UploadDocumentResult>),
        StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(GenericResponseModel<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(GenericResponseModel<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(GenericResponseModel<object>), StatusCodes.Status413PayloadTooLarge)]
    [ProducesResponseType(typeof(GenericResponseModel<object>), StatusCodes.Status415UnsupportedMediaType)]
    [ProducesResponseType(typeof(GenericResponseModel<object>), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UploadDocument(
        [FromForm, CustomizeValidator(Skip = true)] UploadDocumentCommand request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(GenericResponseModel<object>.Failure(
                Localizer["Validation.InvalidRequest"].Value,
                "Validation.InvalidRequest"));
        }

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
