using System.Security.Cryptography;
using Hakeem.Application.Interfaces.Identity;
using Hakeem.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace Hakeem.Infrastructure.Services.Identity;

public sealed class PatientCodeGenerator(ApplicationDbContext dbContext)
    : IPatientCodeGenerator
{
    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    private const int MaximumAttempts = 20;

    public async Task<string> GenerateUniqueAsync(CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < MaximumAttempts; attempt++)
        {
            var code = $"H{RandomCharacters(3)}-{RandomCharacters(3)}";
            var exists = await dbContext.PatientProfiles
                .AnyAsync(profile => profile.PatientCode == code, cancellationToken);

            if (!exists)
            {
                return code;
            }
        }

        throw new InvalidOperationException("Unable to generate a unique patient code.");
    }

    private static string RandomCharacters(int length)
    {
        return string.Create(length, Alphabet, static (span, alphabet) =>
        {
            for (var index = 0; index < span.Length; index++)
            {
                span[index] = alphabet[RandomNumberGenerator.GetInt32(alphabet.Length)];
            }
        });
    }
}
