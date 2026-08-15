using System.Text.Json;
using Hakeem.Application.Constants;
using Hakeem.Application.Exceptions;
using Hakeem.Application.Features.MedicalDataExtraction.DTOs;
using Hakeem.Application.Repositories.MedicalDocuments;
using Hakeem.Application.Services.Access;
using MediatR;

namespace Hakeem.Application.Features.MedicalDataExtraction.Queries.GetExtractedFields;

public sealed class GetExtractedFieldsQueryHandler(
    IMedicalDocumentRepository medicalDocumentRepository,
    IDoctorPatientAccessGuard doctorPatientAccessGuard)
    : IRequestHandler<GetExtractedFieldsQuery, ExtractedFieldsResponse>
{
    public async Task<ExtractedFieldsResponse> Handle(
        GetExtractedFieldsQuery request,
        CancellationToken cancellationToken)
    {
        var document = await medicalDocumentRepository.GetByIdAsync(
            request.DocumentId,
            cancellationToken);

        if (document is null)
        {
            throw new NotFoundException(ErrorCodes.DocumentNotFound);
        }

        await doctorPatientAccessGuard.RequireDoctorWithActiveAccessAsync(
            document.PatientProfileId,
            cancellationToken);

        var extractedItems =
            await medicalDocumentRepository.GetExtractedItemsAsync(
                document.Id,
                cancellationToken);

        var items = extractedItems
            .OrderBy(item => item.SequenceNumber)
            .Select(item => new ExtractedItemResponse(
                item.Id,
                item.ItemType,
                item.SequenceNumber,
                item.PageNumber,
                item.ExtractedFields
                    .Select(field => new ExtractedFieldResponse(
                        field.Id,
                        field.FieldName,
                        field.ExtractedValue,
                        field.Confidence,
                        field.EvidenceText,
                        DeserializeIssues(field.Issues)))
                    .ToList()))
            .ToList();

        return new ExtractedFieldsResponse(
            document.Id,
            document.DocumentType,
            document.ExtractionStatus.ToString(),
            items);
    }

    private static IReadOnlyList<string> DeserializeIssues(string issues)
    {
        if (string.IsNullOrWhiteSpace(issues))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<string>>(issues) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }
}
