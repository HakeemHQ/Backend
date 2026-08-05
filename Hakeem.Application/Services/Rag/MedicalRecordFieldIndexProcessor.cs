using Hakeem.Application.Constants;
using Hakeem.Application.Exceptions;
using Hakeem.Application.Interfaces.Processors;
using Hakeem.Application.Interfaces.Rag;
using Hakeem.Application.Repositories.MedicalRecords;
using Microsoft.Extensions.Logging;

namespace Hakeem.Application.Services.Rag;

public sealed class MedicalRecordFieldIndexProcessor(
    IMedicalRecordFieldRepository medicalRecordFieldRepository,
    IEmbeddingService embeddingService,
    IMedicalRecordFieldVectorStore vectorStore,
    ILogger<MedicalRecordFieldIndexProcessor> logger)
    : IMedicalRecordFieldIndexProcessor
{
    public async Task ProcessAsync(
        Guid medicalRecordFieldId,
        CancellationToken cancellationToken)
    {
        var field = await medicalRecordFieldRepository.GetByIdWithRecordAsync(
            medicalRecordFieldId,
            cancellationToken);

        if (field is null)
        {
            throw new NotFoundException(ErrorCodes.MedicalRecordFieldNotFound);
        }

        var document = new MedicalRecordFieldVectorDocument(
            field.Id,
            field.MedicalRecordId,
            field.MedicalRecord.PatientProfileId,
            field.MedicalRecord.RecordType,
            field.FieldName,
            field.Value,
            field.MedicalRecord.ClinicalDate);

        var textToEmbed = BuildEmbeddingText(document);
        var embedding = await embeddingService.GenerateEmbeddingAsync(
            textToEmbed,
            cancellationToken);

        await vectorStore.UpsertAsync(
            document,
            embedding,
            cancellationToken);

        logger.LogInformation(
            "Indexed medical record field {MedicalRecordFieldId} into Qdrant.",
            medicalRecordFieldId);
    }

    private static string BuildEmbeddingText(MedicalRecordFieldVectorDocument document)
    {
        return $"""
            Record Type: {document.RecordType}
            Field: {document.FieldName}
            Value: {document.Value}
            Clinical Date: {document.ClinicalDate:yyyy-MM-dd}
            """;
    }
}
