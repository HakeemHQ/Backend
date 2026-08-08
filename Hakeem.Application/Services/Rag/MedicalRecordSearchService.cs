using Hakeem.Application.Configurations;
using Hakeem.Application.Interfaces.Rag;
using Microsoft.Extensions.Options;

namespace Hakeem.Application.Services.Rag;

public sealed class MedicalRecordSearchService(
    IEmbeddingService embeddingService,
    IMedicalRecordVectorStore vectorStore,
    IOptions<QdrantConfiguration> options)
    : IMedicalRecordSearchService
{
    public async Task<IReadOnlyList<MedicalRecordSearchResult>> SearchAsync(
        string query,
        Guid patientProfileId,
        int limit,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return [];
        }

        var embedding = await embeddingService.GenerateEmbeddingAsync(
            query.Trim(),
            cancellationToken);

        var searchLimit = limit <= 0
            ? options.Value.DefaultSearchLimit
            : limit;

        return await vectorStore.SearchAsync(
            embedding,
            patientProfileId,
            searchLimit,
            cancellationToken);
    }
}
