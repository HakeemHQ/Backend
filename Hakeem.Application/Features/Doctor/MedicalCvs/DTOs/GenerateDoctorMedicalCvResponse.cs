namespace Hakeem.Application.Features.Doctor.MedicalCvs.DTOs
{
    public sealed record GenerateDoctorMedicalCvResponse(
        Guid MedicalCvId,
        string Title,
        GenerateDoctorMedicalCvVersionResponse LatestVersion);

    public sealed record GenerateDoctorMedicalCvVersionResponse(
        Guid MedicalCvVersionId,
        int VersionNumber, 
        string GenerationStatus,
        string VerificationStatus,
        string CreatedByRole,
        string PdfUrl,
        DateTimeOffset PreviewExpiresAt);
}
