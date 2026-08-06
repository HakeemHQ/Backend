using Hakeem.Application.Constants;
using Hakeem.Application.Exceptions;
using Hakeem.Application.Interfaces.Processors;
using Hakeem.Application.Interfaces.Rag;
using Hakeem.Application.Repositories.MedicalRecords;
using Microsoft.Extensions.Logging;

namespace Hakeem.Application.Services.Rag;

public sealed class MedicalRecordIndexProcessor(
    IMedicalRecordsRepository medicalRecordsRepository,
    IEmbeddingService embeddingService,
    IMedicalRecordVectorStore vectorStore,
    ILogger<MedicalRecordIndexProcessor> logger)
    : IMedicalRecordIndexProcessor
{
    public async Task ProcessAsync(
        Guid medicalRecordId,
        CancellationToken cancellationToken)
    {
        var record = await medicalRecordsRepository.GetByIdWithFieldsAsync(
            medicalRecordId,
            cancellationToken);

        if (record is null)
        {
            throw new NotFoundException(ErrorCodes.MedicalRecordNotFound);
        }

        var document = new MedicalRecordVectorDocument(
            record.Id,
            record.PatientProfileId,
            record.RecordType,
            record.DisplayName,
            record.Status,
            record.ClinicalDate,
            record.Fields
                .Select(field => new MedicalRecordFieldPayload(field.FieldName, field.Value))
                .ToList());

        var textToEmbed = BuildEmbeddingText(document);
        var embedding = await embeddingService.GenerateEmbeddingAsync(
            textToEmbed,
            cancellationToken);

        await vectorStore.UpsertAsync(
            document,
            embedding,
            cancellationToken);

        logger.LogInformation(
            "Indexed medical record {MedicalRecordId} into Qdrant.",
            medicalRecordId);
    }

    private static string BuildEmbeddingText(MedicalRecordVectorDocument document)
    {
        var fieldsText = string.Join(
            Environment.NewLine,
            document.Fields.Select(field => $"{field.FieldName}: {field.Value}"));

        return $"""
            Record Type: {document.RecordType}
            Display Name: {document.DisplayName}
            Status: {document.Status}
            Clinical Date: {document.ClinicalDate:yyyy-MM-dd}
            {fieldsText}
            """;
    }
}
