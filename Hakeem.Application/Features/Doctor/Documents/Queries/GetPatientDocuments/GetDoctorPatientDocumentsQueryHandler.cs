using Hakeem.Application.Common;
using Hakeem.Application.Features.MedicalDocuments.DTOs;
using Hakeem.Application.Repositories.MedicalDocuments;
using Hakeem.Application.Services.Access;
using MediatR;

namespace Hakeem.Application.Features.Doctor.Documents.Queries.GetPatientDocuments;

public sealed class GetDoctorPatientDocumentsQueryHandler(
    IDoctorPatientAccessGuard doctorPatientAccessGuard,
    IMedicalDocumentRepository medicalDocumentRepository)
    : IRequestHandler<GetDoctorPatientDocumentsQuery, PaginatedResult<MedicalDocumentDto>>
{
    public async Task<PaginatedResult<MedicalDocumentDto>> Handle(
        GetDoctorPatientDocumentsQuery request,
        CancellationToken cancellationToken)
    {
        await doctorPatientAccessGuard.RequireDoctorWithActiveAccessAsync(
            request.PatientProfileId,
            cancellationToken);

        var pagedDocuments = await medicalDocumentRepository.GetDocumentsAsync(
            request.PatientProfileId,
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
