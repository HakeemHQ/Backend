
using Hakeem.Application.Configurations;
using Hakeem.Application.Interfaces.Files;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Hakeem.Infrastructure.Services.Files;

#pragma warning disable CS9113 // Parameter 'httpContextAccessor' is unread — kept for DI constructor contract
public class FileUrlResolver(IHttpContextAccessor httpContextAccessor, IOptions<FileStorageConfiguration> storageConfig, IOptions<FileSettingsConfiguration> fileSettings) : IFileUrlResolver
#pragma warning restore CS9113
{
    private readonly FileStorageConfiguration _storageConfig = storageConfig.Value;
    private readonly FileSettingsConfiguration _fileSettings = fileSettings.Value;





    public string ResolveFileUrl(string value)
    {
        // Return empty if value is null or empty
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        // Check if it's already an absolute URL
        if (IsAbsoluteUrl(value))
        {
            return value;
        }



        // Check if the value looks like a file path
        if (IsFilePath(value))
        {
            return ToAbsoluteUrl(value);
        }

        // Return original value if it doesn't appear to be a file
        return value;
    }



    public string ToAbsoluteUrl(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return relativePath;
        }

        // Already absolute
        if (IsAbsoluteUrl(relativePath))
        {
            return relativePath;
        }

        // Normalize the path separators
        var normalizedPath = relativePath.Replace("\\", "/");

        // Remove leading slash if present
        normalizedPath = normalizedPath.TrimStart('/');

        // Ensure base URL ends with a slash
        var baseUrl = _fileSettings.BaseUrl.TrimEnd('/');

        return $"{baseUrl}/{normalizedPath}";
    }


    public string Resolve(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath)) return relativePath ?? string.Empty;
        if (relativePath.StartsWith("http")) return relativePath;
        if (!relativePath.StartsWith("uploads/")) return relativePath;
        // Normalize the path separators
        var normalizedPath = relativePath.Replace("\\", "/");

        // Remove leading slash if present
        normalizedPath = normalizedPath.TrimStart('/');

        // Ensure base URL ends with a slash
        var baseUrl = ToAbsoluteUrl("").TrimEnd('/');

        return $"{baseUrl}/{normalizedPath}";
    }

    public JsonNode? ResolveJson(JsonNode? node)
    {
        if (node == null) return null;

        return node switch
        {
            JsonValue value => ResolveJsonValue(value),
            JsonArray array => ResolveJsonArray(array),
            JsonObject obj => ResolveJsonObject(obj),
            _ => node.DeepClone()
        };
    }

    private JsonNode ResolveJsonValue(JsonValue value)
    {
        if (value.TryGetValue<string>(out var s))
            return JsonValue.Create(Resolve(s));

        return value.DeepClone();
    }

    private JsonNode ResolveJsonArray(JsonArray array)
    {
        var newArray = new JsonArray();
        foreach (var item in array)
        {
            newArray.Add(ResolveJson(item));
        }
        return newArray;
    }

    private JsonNode ResolveJsonObject(JsonObject obj)
    {
        var newObj = new JsonObject();
        foreach (var kvp in obj)
        {
            newObj[kvp.Key] = ResolveJson(kvp.Value);
        }
        return newObj;
    }

    public string ResolveJsonString(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return "{}";
        try
        {
            var node = JsonNode.Parse(json);
            var resolvedNode = ResolveJson(node);
            return resolvedNode?.ToJsonString() ?? "{}";
        }
        catch (JsonException)
        {
            return json; // Return original if parse fails
        }
    }


    private static bool IsAbsoluteUrl(string value)
    {
        return Uri.TryCreate(value, UriKind.Absolute, out var uri)
               && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }

    private bool IsFilePath(string value)
    {
        // Check if the value has a file extension from allowed extensions
        var extension = Path.GetExtension(value)?.ToLowerInvariant();

        if (string.IsNullOrEmpty(extension))
        {
            return false;
        }

        return _storageConfig.AllowedExtensions.Contains(extension);
    }
}
