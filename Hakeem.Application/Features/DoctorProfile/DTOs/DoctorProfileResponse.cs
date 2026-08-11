namespace Hakeem.Application.Features.DoctorProfile.DTOs;

public sealed record DoctorProfileResponse(
    Guid DoctorId,
    string FullName,
    string Email,
    string Specialty,
    string LicenseNumber,
    string Status);
