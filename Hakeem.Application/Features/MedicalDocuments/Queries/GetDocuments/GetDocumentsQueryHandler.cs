using Hakeem.Application.Common;
using Hakeem.Application.Common.Interfaces;
using Hakeem.Application.Constants;
using Hakeem.Application.Exceptions;
using Hakeem.Application.Features.MedicalDocuments.DTOs;
using Hakeem.Application.Repositories.MedicalDocuments;
using Hakeem.Application.Repositories.PatientProfiles;
using MediatR;

namespace Hakeem.Application.Features.MedicalDocuments.Queries.GetDocuments;

public sealed class GetDocumentsQueryHandler(
    ICurrentUserContext currentUserContext,
    IPatientProfileRepository patientProfileRepository,
    IMedicalDocumentRepository medicalDocumentRepository)
    : IRequestHandler<GetDocumentsQuery, PaginatedResult<MedicalDocumentDto>>
{
    public async Task<PaginatedResult<MedicalDocumentDto>> Handle(
        GetDocumentsQuery request,
        CancellationToken cancellationToken)
    {
        var patientProfile = await patientProfileRepository.GetByUserIdAsync(
            currentUserContext.UserId,
            cancellationToken);

        if (patientProfile is null)
        {
            throw new UnAuthorizedException(ErrorCodes.DocumentPatientProfileNotFound);
        }

        var pagedDocuments = await medicalDocumentRepository.GetDocumentsAsync(
            patientProfile.Id,
            request.DocumentName,
            request.PageNumber,
            request.PageSize,
            cancellationToken);

        var items = pagedDocuments.Items.Select(document => new MedicalDocumentDto
        {
            DocumentId = document.Id,
            DocumentType = document.DocumentType,
            Title = document.Title,
            DocumentDate = DateOnly.FromDateTime(document.DocumentDate),
            ExtractionStatus = document.ExtractionStatus.ToString()
        });

        return new PaginatedResult<MedicalDocumentDto>(
            items,
            pagedDocuments.TotalCount,
            pagedDocuments.PageNumber,
            pagedDocuments.PageSize);
    }
}
