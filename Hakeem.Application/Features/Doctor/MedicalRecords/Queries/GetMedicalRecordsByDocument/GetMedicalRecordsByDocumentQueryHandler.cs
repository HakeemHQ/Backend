using Hakeem.Application.Common;
using Hakeem.Application.Features.MedicalRecords.DTOs;
using Hakeem.Application.Repositories.PatientReviewConfirmation;
using MediatR;

namespace Hakeem.Application.Features.Doctor.MedicalRecords.Queries.GetMedicalRecordsByDocument;

public sealed class GetMedicalRecordsByDocumentQueryHandler(
    ISourceReferenceRepository sourceReferenceRepository)
    : IRequestHandler<GetMedicalRecordsByDocumentQuery, PaginatedResult<MedicalRecordDto>>
{
    public async Task<PaginatedResult<MedicalRecordDto>> Handle(
        GetMedicalRecordsByDocumentQuery request,
        CancellationToken cancellationToken)
    {
        var records = await sourceReferenceRepository.GetMedicalRecordsByDocumentAsync(
            request.DocumentId,
            request.Search,
            request.RecordType,
            request.FromDate,
            request.ToDate,
            request.PageNumber,
            request.PageSize,
            cancellationToken);

        var items = records.Items.Select(record => new MedicalRecordDto
        {
            MedicalRecordId = record.Id,
            RecordType = record.RecordType,
            DisplayName = record.DisplayName,
            ClinicalDate = record.ClinicalDate,
            Status = record.Status
        });

        return new PaginatedResult<MedicalRecordDto>(
            items,
            records.TotalCount,
            records.PageNumber,
            records.PageSize);
    }
}
