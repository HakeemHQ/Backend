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
