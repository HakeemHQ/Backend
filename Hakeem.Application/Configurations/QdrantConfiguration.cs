namespace Hakeem.Application.Configurations;

public sealed class QdrantConfiguration
{
    public const string SectionName = "Qdrant";

    public string Host { get; set; } = "localhost";

    public int Port { get; set; } = 6334;

    public string ApiKey { get; set; } = string.Empty;

    public bool UseTls { get; set; }

    public string CollectionName { get; set; } = "medical_records";

    public int VectorSize { get; set; } = 768;

    public string[] PayloadFields { get; set; } =
    [
        "medical_record_id",
        "patient_profile_id",
        "record_type",
        "display_name",
        "status",
        "clinical_date",
        "content",
        "fields"
    ];

    public string PatientProfileIdPayloadField { get; set; } = "patient_profile_id";

    public int DefaultSearchLimit { get; set; } = 10;

    public QdrantConnectionSettings GetConnectionSettings()
    {
        var host = Host.Trim();

        if (Uri.TryCreate(host, UriKind.Absolute, out var uri) &&
            (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
        {
            return new QdrantConnectionSettings
            {
                Host = uri.Host,
                Port = uri.IsDefaultPort ? Port : uri.Port,
                UseTls = uri.Scheme == Uri.UriSchemeHttps || UseTls
            };
        }

        return new QdrantConnectionSettings
        {
            Host = host,
            Port = Port,
            UseTls = UseTls
        };
    }
}

public sealed class QdrantConnectionSettings
{
    public string Host { get; init; } = string.Empty;

    public int Port { get; init; }

    public bool UseTls { get; init; }
}
