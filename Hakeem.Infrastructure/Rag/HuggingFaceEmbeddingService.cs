using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Hakeem.Application.Configurations;
using Hakeem.Application.Interfaces.Rag;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Hakeem.Infrastructure.Rag;

public sealed class HuggingFaceEmbeddingService(
    HttpClient httpClient,
    IOptions<HuggingFaceEmbeddingConfiguration> options,
    ILogger<HuggingFaceEmbeddingService> logger)
    : IEmbeddingService
{
    public async Task<float[]> GenerateEmbeddingAsync(
        string text,
        CancellationToken cancellationToken)
    {
        var configuration = options.Value;
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            configuration.EmbeddingsEndpoint);

        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", configuration.ApiKey);
        request.Content = JsonContent.Create(
            new HuggingFaceEmbeddingsRequest(text, configuration.ModelId));

        using var response = await httpClient.SendAsync(
            request,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogError(
                "Hugging Face embedding request failed with status {StatusCode}: {ErrorBody}",
                (int)response.StatusCode,
                errorBody);

            response.EnsureSuccessStatusCode();
        }

        var result = await response.Content.ReadFromJsonAsync<HuggingFaceEmbeddingsResponse>(
            cancellationToken: cancellationToken);

        var embedding = result?.Data.FirstOrDefault()?.Embedding;
        if (embedding is null || embedding.Length == 0)
        {
            throw new InvalidOperationException(
                "Hugging Face returned an empty embedding response.");
        }

        return embedding;
    }

    private sealed record HuggingFaceEmbeddingsRequest(
        [property: JsonPropertyName("input")] string Input,
        [property: JsonPropertyName("model")] string Model);

    private sealed record HuggingFaceEmbeddingsResponse(
        [property: JsonPropertyName("data")] List<HuggingFaceEmbeddingData> Data);

    private sealed record HuggingFaceEmbeddingData(
        [property: JsonPropertyName("embedding")] float[] Embedding);
}
