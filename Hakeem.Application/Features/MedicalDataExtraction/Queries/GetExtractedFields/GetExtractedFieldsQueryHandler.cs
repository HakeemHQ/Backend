using System.Text.Json;
using Hakeem.Application.Common.Interfaces;
using Hakeem.Application.Constants;
using Hakeem.Application.Exceptions;
using Hakeem.Application.Features.MedicalDataExtraction.DTOs;
using Hakeem.Application.Repositories.MedicalDocuments;
using Hakeem.Application.Repositories.PatientProfiles;
using MediatR;

namespace Hakeem.Application.Features.MedicalDataExtraction.Queries.GetExtractedFields;

public sealed class GetExtractedFieldsQueryHandler(
    IMedicalDocumentRepository medicalDocumentRepository,
    IPatientProfileRepository patientProfileRepository,
    ICurrentUserContext currentUserContext)
    : IRequestHandler<GetExtractedFieldsQuery, ExtractedFieldsResponse>
{
    public async Task<ExtractedFieldsResponse> Handle(
        GetExtractedFieldsQuery request,
        CancellationToken cancellationToken)
    {
        var patientProfile = await patientProfileRepository.GetByUserIdAsync(
            currentUserContext.UserId,
            cancellationToken);

        var document = await medicalDocumentRepository.GetByIdAsync(
            request.DocumentId,
            cancellationToken);

        if (patientProfile is null ||
            document is null ||
            document.PatientProfileId != patientProfile.Id)
        {
            throw new NotFoundException(ErrorCodes.DocumentNotFound);
        }

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
