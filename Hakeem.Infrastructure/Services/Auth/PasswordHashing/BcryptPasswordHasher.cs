using Hakeem.Application.Services.Auth.PasswordHashing;

namespace Hakeem.Infrastructure.Services.Auth.PasswordHashing;

public sealed class BcryptPasswordHasher : IPasswordHasher
{
    public string Hash(string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    public bool Verify(string password, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(passwordHash))
        {
            return false;
        }

        return BCrypt.Net.BCrypt.Verify(password, passwordHash);
    }
}
