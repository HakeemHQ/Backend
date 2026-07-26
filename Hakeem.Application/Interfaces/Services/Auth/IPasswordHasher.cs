using Hakeem.Domain.Interfaces.ServiceLifetime;

namespace Hakeem.Application.Interfaces.Services.Auth;

public interface IPasswordHasher : IScoped
{
    string Hash(string password);
    bool Verify(string password, string passwordHash);
}
