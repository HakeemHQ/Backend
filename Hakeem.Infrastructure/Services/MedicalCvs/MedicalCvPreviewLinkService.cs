using System.Security.Cryptography;
using System.Text.Json;
using Hakeem.Application.Configurations;
using Hakeem.Application.Interfaces.MedicalCvs;
using Hakeem.Domain.Interfaces.ServiceLifetime;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;

namespace Hakeem.Infrastructure.Services.MedicalCvs;

public sealed class MedicalCvPreviewLinkService(
    IDataProtectionProvider dataProtectionProvider,
    IOptions<MedicalCvPreviewLinkConfiguration> options)
    : IMedicalCvPreviewLinkService, ISingleton
{
    private const string Purpose = "Hakeem.MedicalCv.Preview.v1";
    private readonly ITimeLimitedDataProtector _protector =
        dataProtectionProvider
            .CreateProtector(Purpose)
            .ToTimeLimitedDataProtector();
    private readonly TimeSpan _lifetime = TimeSpan.FromMinutes(
        options.Value.LifetimeMinutes);

    public MedicalCvPreviewLink Create(
        Guid patientId,
        Guid medicalCvId,
        Guid medicalCvVersionId)
    {
        if (patientId == Guid.Empty ||
            medicalCvId == Guid.Empty ||
            medicalCvVersionId == Guid.Empty)
        {
            throw new ArgumentException(
                "Patient, medical CV, and version IDs are required.");
        }

        var access = new MedicalCvPreviewAccess(
            patientId,
            medicalCvId,
            medicalCvVersionId);
        var expiresAt = DateTimeOffset.UtcNow.Add(_lifetime);
        var token = _protector.Protect(
            JsonSerializer.Serialize(access),
            expiresAt);

        return new MedicalCvPreviewLink(token, expiresAt);
    }

    public bool TryValidate(
        string token,
        Guid medicalCvVersionId,
        out MedicalCvPreviewAccess access)
    {
        access = null!;

        if (string.IsNullOrWhiteSpace(token) ||
            medicalCvVersionId == Guid.Empty)
        {
            return false;
        }

        try
        {
            var payload = _protector.Unprotect(token, out _);
            var parsedAccess = JsonSerializer.Deserialize<MedicalCvPreviewAccess>(
                payload);

            if (parsedAccess is null ||
                parsedAccess.PatientId == Guid.Empty ||
                parsedAccess.MedicalCvVersionId != medicalCvVersionId)
            {
                return false;
            }

            access = parsedAccess;
            return true;
        }
        catch (CryptographicException)
        {
            return false;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
