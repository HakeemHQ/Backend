namespace Hakeem.Application.Features.PatientProfile.DTOs;

public sealed record PatientProfileResponse(
    Guid PatientId,
    Guid UserId,
    string PatientCode,
    string Email,
    string FullName,
    string BirthDate,
    string NationalIdMasked,
    string IdentityVerificationStatus,
    string Status,
    string FirstName,
    string LastName,
    string PhoneNumber,
    string Gender);
