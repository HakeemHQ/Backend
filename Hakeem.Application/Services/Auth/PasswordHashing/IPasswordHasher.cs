using Hakeem.Domain.Interfaces.ServiceLifetime;

namespace Hakeem.Application.Services.Auth.PasswordHashing;

public interface IPasswordHasher : IScoped
{
    string Hash(string password);
    bool Verify(string password, string passwordHash);
}
