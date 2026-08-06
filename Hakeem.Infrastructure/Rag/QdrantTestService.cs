using Google.Protobuf.Collections;
using Hakeem.Application.Configurations;
using Hakeem.Application.Interfaces.Rag;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Qdrant.Client;
using Qdrant.Client.Grpc;

namespace Hakeem.Infrastructure.Rag;

public sealed class QdrantTestService(
    QdrantClient qdrantClient,
    IEmbeddingService embeddingService,
    IMedicalRecordVectorStore vectorStore,
    IOptions<QdrantConfiguration> options,
    ILogger<QdrantTestService> logger)
    : IQdrantTestService
{
    private readonly QdrantConfiguration _configuration = options.Value;

    public async Task<TestQdrantUpsertResult> UpsertTestDocumentAsync(
        string text,
        CancellationToken cancellationToken)
    {
        var embedding = await embeddingService.GenerateEmbeddingAsync(
            text,
            cancellationToken);

        var pointId = Guid.NewGuid();
        var document = new MedicalRecordVectorDocument(
            pointId,
            Guid.NewGuid(),
            "TestRecord",
            text,
            "Confirmed",
            DateTime.UtcNow,
            [new MedicalRecordFieldPayload("TestField", text)]);

        await vectorStore.UpsertAsync(
            document,
            embedding,
            cancellationToken);

        logger.LogInformation(
            "Inserted test point {PointId} into Qdrant collection {CollectionName}.",
            pointId,
            _configuration.CollectionName);

        return new TestQdrantUpsertResult(
            pointId,
            text,
            embedding.Length,
            _configuration.CollectionName);
    }

    public async Task<IReadOnlyList<TestQdrantSearchResult>> SearchAsync(
        string query,
        int limit,
        CancellationToken cancellationToken)
    {
        var embedding = await embeddingService.GenerateEmbeddingAsync(
            query,
            cancellationToken);

        var collections = await qdrantClient.ListCollectionsAsync(cancellationToken);
        if (!collections.Contains(_configuration.CollectionName))
        {
            return [];
        }

        var results = await qdrantClient.SearchAsync(
            _configuration.CollectionName,
            embedding,
            limit: (ulong)limit,
            cancellationToken: cancellationToken);

        return results
            .Select(point => new TestQdrantSearchResult(
                ParsePointId(point.Id),
                point.Score,
                GetPayloadValue(point.Payload, "content"),
                GetPayloadValue(point.Payload, "display_name"),
                GetPayloadValue(point.Payload, "fields")))
            .ToList();
    }

    private static Guid ParsePointId(PointId pointId)
    {
        return pointId.HasUuid
            ? Guid.Parse(pointId.Uuid)
            : Guid.Empty;
    }

    private static string? GetPayloadValue(MapField<string, Value> payload, string key)
    {
        if (!payload.TryGetValue(key, out var value))
        {
            return null;
        }

        return value.KindCase switch
        {
            Value.KindOneofCase.StringValue => value.StringValue,
            Value.KindOneofCase.IntegerValue => value.IntegerValue.ToString(),
            Value.KindOneofCase.DoubleValue => value.DoubleValue.ToString(),
            Value.KindOneofCase.BoolValue => value.BoolValue.ToString(),
            _ => value.ToString()
        };
    }
}
