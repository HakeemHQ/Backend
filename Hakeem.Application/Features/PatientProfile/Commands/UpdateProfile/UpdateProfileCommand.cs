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
    Guid UserId,
    string Email,
    string FullName,
    string BirthDate,
    string Status,
    string FirstName,
    string LastName,
    string PhoneNumber,
    string Gender);
