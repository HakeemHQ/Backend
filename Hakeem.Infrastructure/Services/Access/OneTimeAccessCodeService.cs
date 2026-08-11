using System.Security.Cryptography;
using Hakeem.Application.Interfaces.Access;
using Hakeem.Application.Interfaces.Services.Auth;

namespace Hakeem.Infrastructure.Services.Access;

public sealed class OneTimeAccessCodeService(IPasswordHasher passwordHasher)
    : IOneTimeAccessCodeService
{
    public IssuedOneTimeAccessCode Generate()
    {
        var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        return new IssuedOneTimeAccessCode(code, passwordHasher.Hash(code));
    }

    public bool Verify(string code, string codeHash)
    {
        return passwordHasher.Verify(code, codeHash);
    }
}
