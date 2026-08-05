
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
        return ToAbsoluteUrl(value);
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


    private static bool IsAbsoluteUrl(string value)
    {
        return Uri.TryCreate(value, UriKind.Absolute, out var uri)
               && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }

}
