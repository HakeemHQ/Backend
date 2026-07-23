using Hakeem.Domain.Interfaces.ServiceLifetime;

namespace Hakeem.Application.Interfaces.Files;

public interface IFileUrlResolver : IScoped
{
    string ResolveFileUrl(string value);
    string ToAbsoluteUrl(string relativePath);
    string ResolveJsonString(string json);
}




