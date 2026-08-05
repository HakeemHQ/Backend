using Hakeem.Application.Configurations;
using Hakeem.Application.Interfaces.Rag;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Qdrant.Client;
using Qdrant.Client.Grpc;

namespace Hakeem.Infrastructure.Rag;

public sealed class QdrantMedicalRecordFieldVectorStore(
    QdrantClient qdrantClient,
    IOptions<QdrantConfiguration> options,
    ILogger<QdrantMedicalRecordFieldVectorStore> logger)
    : IMedicalRecordFieldVectorStore
{
    private readonly QdrantConfiguration _configuration = options.Value;
    private readonly SemaphoreSlim _collectionLock = new(1, 1);
    private bool _collectionEnsured;

    public async Task UpsertAsync(
        MedicalRecordFieldVectorDocument document,
        float[] embedding,
        CancellationToken cancellationToken)
    {
        await EnsureCollectionExistsAsync(cancellationToken);

        var point = new PointStruct
        {
            Id = document.MedicalRecordFieldId,
            Vectors = embedding,
            Payload =
            {
                ["medical_record_field_id"] = document.MedicalRecordFieldId.ToString(),
                ["medical_record_id"] = document.MedicalRecordId.ToString(),
                ["patient_profile_id"] = document.PatientProfileId.ToString(),
                ["record_type"] = document.RecordType,
                ["field_name"] = document.FieldName,
                ["value"] = document.Value,
                ["clinical_date"] = document.ClinicalDate.ToString("O"),
                ["content"] = $"{document.FieldName}: {document.Value}"
            }
        };

        await qdrantClient.UpsertAsync(
            _configuration.CollectionName,
            [point],
            cancellationToken: cancellationToken);

        logger.LogInformation(
            "Upserted medical record field {MedicalRecordFieldId} into Qdrant collection {CollectionName}.",
            document.MedicalRecordFieldId,
            _configuration.CollectionName);
    }

    private async Task EnsureCollectionExistsAsync(CancellationToken cancellationToken)
    {
        if (_collectionEnsured)
        {
            return;
        }

        await _collectionLock.WaitAsync(cancellationToken);
        try
        {
            if (_collectionEnsured)
            {
                return;
            }

            var collections = await qdrantClient.ListCollectionsAsync(cancellationToken);
            if (!collections.Contains(_configuration.CollectionName))
            {
                await qdrantClient.CreateCollectionAsync(
                    _configuration.CollectionName,
                    new VectorParams
                    {
                        Size = (ulong)_configuration.VectorSize,
                        Distance = Distance.Cosine
                    },
                    cancellationToken: cancellationToken);

                logger.LogInformation(
                    "Created Qdrant collection {CollectionName}.",
                    _configuration.CollectionName);
            }

            _collectionEnsured = true;
        }
        finally
        {
            _collectionLock.Release();
        }
    }
}
