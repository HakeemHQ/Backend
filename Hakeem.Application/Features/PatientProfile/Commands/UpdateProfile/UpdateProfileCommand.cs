using MediatR;

namespace Hakeem.Application.Features.PatientProfile.Commands.UpdateProfile;

public sealed class UpdateProfileCommand : IRequest<UpdateProfileResult>
{
    public string? FullName { get; set; }
    public DateTime? BirthDate { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Gender { get; set; }
}

public sealed record UpdateProfileResult(
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
