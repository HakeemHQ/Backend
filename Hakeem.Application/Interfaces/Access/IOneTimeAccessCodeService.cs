using Hakeem.Domain.Interfaces.ServiceLifetime;

namespace Hakeem.Application.Interfaces.Access;

public interface IOneTimeAccessCodeService : IScoped
{
    IssuedOneTimeAccessCode Generate();
    bool Verify(string code, string codeHash);
}

public sealed record IssuedOneTimeAccessCode(
    string Code,
    string CodeHash);
