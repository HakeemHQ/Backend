using Hakeem.Domain.Interfaces.ServiceLifetime;

namespace Hakeem.Application.Interfaces.Rag;

public interface IQdrantTestService : IScoped
{
    Task<TestQdrantUpsertResult> UpsertTestDocumentAsync(
        string text,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<TestQdrantSearchResult>> SearchAsync(
        string query,
        int limit,
        CancellationToken cancellationToken);
}

public sealed record TestQdrantUpsertResult(
    Guid PointId,
    string Text,
    int Dimensions,
    string CollectionName);

public sealed record TestQdrantSearchResult(
    Guid PointId,
    float Score,
    string? Content,
    string? FieldName,
    string? Value);
