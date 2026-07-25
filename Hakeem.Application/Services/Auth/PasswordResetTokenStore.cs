using System.Collections.Concurrent;
using Hakeem.Domain.Interfaces.ServiceLifetime;

namespace Hakeem.Application.Services.Auth;

public interface IPasswordResetTokenStore : IScoped
{
    Task<string> CreateTokenAsync(Guid userId, CancellationToken cancellationToken);
    Task<(bool IsValid, Guid UserId)> ValidateAndConsumeAsync(string resetToken, CancellationToken cancellationToken);
}

public sealed class PasswordResetTokenStore : IPasswordResetTokenStore
{
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromMinutes(15);
    private readonly ConcurrentDictionary<string, PasswordResetTokenEntry> _tokens = new();

    public Task<string> CreateTokenAsync(Guid userId, CancellationToken cancellationToken)
    {
        var token = Convert.ToBase64String(Guid.NewGuid().ToByteArray())
            .Replace("=", string.Empty)
            .Replace("+", "-")
            .Replace("/", "_");

        _tokens[token] = new PasswordResetTokenEntry(userId, DateTime.UtcNow.Add(TokenLifetime));
        return Task.FromResult(token);
    }

    public Task<(bool IsValid, Guid UserId)> ValidateAndConsumeAsync(string resetToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(resetToken))
        {
            return Task.FromResult((false, Guid.Empty));
        }

        if (!_tokens.TryRemove(resetToken, out var entry))
        {
            return Task.FromResult((false, Guid.Empty));
        }

        return entry.ExpiresAt >= DateTime.UtcNow
            ? Task.FromResult((true, entry.UserId))
            : Task.FromResult((false, Guid.Empty));
    }

    private sealed record PasswordResetTokenEntry(Guid UserId, DateTime ExpiresAt);
}
