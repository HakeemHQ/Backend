namespace Hakeem.Application.Configurations;

public sealed class HuggingFaceEmbeddingConfiguration
{
    public const string SectionName = "HuggingFaceEmbedding";

    public string EmbeddingsEndpoint { get; set; } =
        "https://router.huggingface.co/scaleway/v1/embeddings";

    public string ModelId { get; set; } = "qwen3-embedding-8b";

    public string ApiKey { get; set; } = string.Empty;

    public int Dimensions { get; set; } = 4096;
}
