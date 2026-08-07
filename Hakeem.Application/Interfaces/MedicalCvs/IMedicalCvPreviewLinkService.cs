using Hakeem.Domain.Interfaces.ServiceLifetime;

namespace Hakeem.Application.Interfaces.MedicalCvs;

public interface IMedicalCvPreviewLinkService : ISingleton
{
    MedicalCvPreviewLink Create(
        Guid patientId,
        Guid medicalCvId,
        Guid medicalCvVersionId);

    bool TryValidate(
        string token,
        Guid medicalCvId,
        Guid medicalCvVersionId,
        out MedicalCvPreviewAccess access);
}

public sealed record MedicalCvPreviewLink(
    string Token,
    DateTimeOffset ExpiresAt);

public sealed record MedicalCvPreviewAccess(
    Guid PatientId,
    Guid MedicalCvId,
    Guid MedicalCvVersionId);
