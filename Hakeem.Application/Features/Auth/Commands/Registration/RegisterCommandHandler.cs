using Hakeem.Application.Constants;
using Hakeem.Application.Exceptions;
using Hakeem.Application.Repositories.Users;
using Hakeem.Application.Interfaces.Services.Auth;
using Hakeem.Domain.Entities;
using Hakeem.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Hakeem.Application.Features.Auth.Commands.Registration;

public sealed class RegisterCommandHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IUnitOfWork unitOfWork)
    : IRequestHandler<RegisterCommand, RegisterResult>
{
    private const string PatientUserType = "Patient";
    private const string ActiveStatus = "Active";

    public async Task<RegisterResult> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var firstName = request.FirstName.Trim();
        var lastName = request.LastName.Trim();
        var gender = request.Gender.Trim();
        var normalizedGender = char.ToUpperInvariant(gender[0])
            + gender[1..].ToLowerInvariant();

        if (await userRepository.GetByEmailAsync(normalizedEmail, cancellationToken) is not null)
        {
            throw new ConflictException(ErrorCodes.UserEmailAlreadyExists);
        }

        var userId = Guid.NewGuid();
        var profile = new PatientProfile
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            FullName = $"{firstName} {lastName}",
            BirthDate = request.BirthDate.ToDateTime(TimeOnly.MinValue)
        };

        var user = new User
        {
            Id = userId,
            Email = normalizedEmail,
            UserType = PatientUserType,
            Status = ActiveStatus,
            FirstName = firstName,
            LastName = lastName,
            PhoneNumber = request.PhoneNumber.Trim(),
            Gender = normalizedGender,
            PasswordHash = passwordHasher.Hash(request.Password),
            PatientProfile = profile
        };

        userRepository.Add(user);
        try
        {
            await unitOfWork.SaveChanges(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsEmailUniquenessViolation(exception))
        {
            throw new ConflictException(ErrorCodes.UserEmailAlreadyExists);
        }

        return new RegisterResult(
            user.Id,
            user.Email,
            user.UserType,
            user.Status,
            new RegisterProfileResult(
                profile.FullName,
                DateOnly.FromDateTime(profile.BirthDate)));
    }

    private static bool IsEmailUniquenessViolation(DbUpdateException exception)
    {
        return exception.ToString().Contains("IX_Users_Email", StringComparison.OrdinalIgnoreCase);
    }
}
