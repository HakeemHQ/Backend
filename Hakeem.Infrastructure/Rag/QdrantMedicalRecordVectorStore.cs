using Hakeem.Application.Configurations;
using Hakeem.Application.Interfaces.Rag;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Qdrant.Client;
using Qdrant.Client.Grpc;

namespace Hakeem.Infrastructure.Rag;

public sealed class QdrantMedicalRecordVectorStore(
    QdrantClient qdrantClient,
    IOptions<QdrantConfiguration> options,
    ILogger<QdrantMedicalRecordVectorStore> logger)
    : IMedicalRecordVectorStore
{
    private readonly QdrantConfiguration _configuration = options.Value;
    private readonly SemaphoreSlim _collectionLock = new(1, 1);
    private bool _collectionEnsured;

    public async Task UpsertAsync(
        MedicalRecordVectorDocument document,
        float[] embedding,
        CancellationToken cancellationToken)
    {
        await EnsureCollectionExistsAsync(cancellationToken);

        var payloadValues = MedicalRecordVectorPayloadBuilder.SelectConfiguredFields(
            document,
            _configuration);

        var point = new PointStruct
        {
            Id = document.MedicalRecordId,
            Vectors = embedding
        };

        foreach (var (key, value) in payloadValues)
        {
            point.Payload[key] = value;
        }

        await qdrantClient.UpsertAsync(
            _configuration.CollectionName,
            [point],
            cancellationToken: cancellationToken);

        logger.LogInformation(
            "Upserted medical record {MedicalRecordId} into Qdrant collection {CollectionName}.",
            document.MedicalRecordId,
            _configuration.CollectionName);
    }

    public async Task<IReadOnlyList<MedicalRecordSearchResult>> SearchAsync(
        float[] embedding,
        Guid patientProfileId,
        int limit,
        CancellationToken cancellationToken)
    {
        await EnsureCollectionExistsAsync(cancellationToken);

        var collections = await qdrantClient.ListCollectionsAsync(cancellationToken);
        if (!collections.Contains(_configuration.CollectionName))
        {
            return [];
        }

        var filter = new Filter
        {
            Must =
            {
                new Condition
                {
                    Field = new FieldCondition
                    {
                        Key = _configuration.PatientProfileIdPayloadField,
                        Match = new Match
                        {
                            Keyword = patientProfileId.ToString()
                        }
                    }
                }
            }
        };

        var results = await qdrantClient.QueryAsync(
            _configuration.CollectionName,
            query: embedding,
            filter: filter,
            limit: (ulong)limit,
            cancellationToken: cancellationToken);

        return results
            .Select(MapSearchResult)
            .ToList();
    }

    private MedicalRecordSearchResult MapSearchResult(ScoredPoint point)
    {
        var medicalRecordId = QdrantPayloadReader.GetGuid(
            point.Payload,
            "medical_record_id");

        if (medicalRecordId == Guid.Empty)
        {
            medicalRecordId = QdrantPayloadReader.ParsePointId(point.Id);
        }

        return new MedicalRecordSearchResult(
            medicalRecordId,
            QdrantPayloadReader.GetGuid(point.Payload, _configuration.PatientProfileIdPayloadField),
            point.Score,
            QdrantPayloadReader.GetString(point.Payload, "record_type"),
            QdrantPayloadReader.GetString(point.Payload, "display_name"),
            QdrantPayloadReader.GetString(point.Payload, "status"),
            QdrantPayloadReader.GetDateTime(point.Payload, "clinical_date"),
            QdrantPayloadReader.GetString(point.Payload, "content"),
            QdrantPayloadReader.GetString(point.Payload, "fields"));
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
