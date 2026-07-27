namespace Hakeem.Application.Features.PatientProfile.DTOs;

public sealed record PatientProfileResponse(
    Guid UserId,
    string Email,
    string FullName,
    string BirthDate,
    string Status,
    string FirstName,
    string LastName,
    string PhoneNumber,
    string Gender);
