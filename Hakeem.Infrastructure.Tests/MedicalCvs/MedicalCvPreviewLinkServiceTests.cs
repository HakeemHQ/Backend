using Hakeem.Application.Configurations;
using Hakeem.Infrastructure.Services.MedicalCvs;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;

namespace Hakeem.Infrastructure.Tests.MedicalCvs;

public sealed class MedicalCvPreviewLinkServiceTests
{
    [Fact]
    public void Create_ThenValidate_ReturnsBoundPatientAccess()
    {
        var service = CreateService(15);
        var patientId = Guid.NewGuid();
        var medicalCvId = Guid.NewGuid();
        var versionId = Guid.NewGuid();

        var link = service.Create(patientId, medicalCvId, versionId);
        var isValid = service.TryValidate(
            link.Token,
            versionId,
            out var access);

        Assert.True(isValid);
        Assert.Equal(patientId, access.PatientId);
        Assert.Equal(medicalCvId, access.MedicalCvId);
        Assert.Equal(versionId, access.MedicalCvVersionId);
        Assert.InRange(
            link.ExpiresAt,
            DateTimeOffset.UtcNow.AddMinutes(14),
            DateTimeOffset.UtcNow.AddMinutes(16));
    }

    [Fact]
    public void TryValidate_WhenTokenIsTampered_ReturnsFalse()
    {
        var service = CreateService(15);
        var link = service.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid());

        var isValid = service.TryValidate(
            link.Token + "tampered",
            Guid.NewGuid(),
            out _);

        Assert.False(isValid);
    }

    [Fact]
    public void TryValidate_WhenTokenIsExpired_ReturnsFalse()
    {
        var service = CreateService(-1);
        var medicalCvId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var link = service.Create(
            Guid.NewGuid(),
            medicalCvId,
            versionId);

        var isValid = service.TryValidate(
            link.Token,
            versionId,
            out _);

        Assert.False(isValid);
    }

    private static MedicalCvPreviewLinkService CreateService(
        int lifetimeMinutes)
    {
        return new MedicalCvPreviewLinkService(
            new EphemeralDataProtectionProvider(),
            Options.Create(new MedicalCvPreviewLinkConfiguration
            {
                LifetimeMinutes = lifetimeMinutes
            }));
    }
}
