using MediatR;

namespace Hakeem.Application.Features.Auth.Commands.Registration;

public sealed class RegisterCommand : IRequest<RegisterResult>
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Gender { get; set; } = string.Empty;
    public DateOnly BirthDate { get; set; }
}

public sealed record RegisterResult(
    Guid UserId,
    string Email,
    string UserType,
    string Status,
    RegisterProfileResult Profile);

public sealed record RegisterProfileResult(
    string FullName,
    DateOnly BirthDate);
