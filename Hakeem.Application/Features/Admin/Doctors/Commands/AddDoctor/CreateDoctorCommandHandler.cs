using Hakeem.Application.Constants;
using Hakeem.Application.Exceptions;
using Hakeem.Application.Features.Admin.Doctors;
using Hakeem.Application.Features.Admin.Doctors.DTOs;
using Hakeem.Application.Interfaces.Services.Auth;
using Hakeem.Application.Repositories.DoctorProfiles;
using Hakeem.Application.Repositories.Users;
using Hakeem.Domain.Entities;
using Hakeem.Domain.Enums.Identity;
using Hakeem.Domain.Interfaces;
using MediatR;
<<<<<<< HEAD
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
=======
using Microsoft.EntityFrameworkCore;
>>>>>>> AdminDoctorManagement

namespace Hakeem.Application.Features.Admin.Doctors.Commands.AddDoctor
{
    public sealed class CreateDoctorCommandHandler(
     IUserRepository userRepository,
     IDoctorProfileRepository doctorProfileRepository,
     IPasswordHasher passwordHasher,
     IUnitOfWork unitOfWork)
     : IRequestHandler<CreateDoctorCommand, CreateDoctorResponse>
    {
        public async Task<CreateDoctorResponse> Handle(
            CreateDoctorCommand request,
            CancellationToken cancellationToken)
        {
            var email = request.Email.Trim().ToLowerInvariant();
            var existingUser = await userRepository.GetByEmailAsync(
                email,
                cancellationToken);

            if (existingUser is not null)
            {
                throw new ConflictException(ErrorCodes.UserEmailAlreadyExists);
            }

            var (firstName, lastName) = SplitFullName(request.FullName);
            var normalizedFullName = $"{firstName} {lastName}".Trim();
            var passwordHash = passwordHasher.Hash(request.TemporaryPassword);
            var userId = Guid.NewGuid();
            var doctorId = Guid.NewGuid();

            var user = new User
            {
                Id = userId,
                FirstName = firstName,
                LastName = lastName,
                Email = email,
                PasswordHash = passwordHash,
                Role = ApplicationRole.Doctor,
                Status = AccountStatus.Active
            };

            var doctorProfile = new Domain.Entities.DoctorProfile
            {
                Id = doctorId,
                UserId = userId,
                User = user,
                Specialty = request.Specialty.Trim(),
                LicenseNumber = DoctorLicenseNumber.Generate(doctorId)
            };

            userRepository.Add(user);
            doctorProfileRepository.Add(doctorProfile);

            try
            {
                await unitOfWork.SaveChanges(cancellationToken);
            }
            catch (DbUpdateException exception)
                when (IsEmailConflict(exception))
            {
                throw new ConflictException(ErrorCodes.UserEmailAlreadyExists);
            }
            catch (DbUpdateException exception)
                when (IsLicenseConflict(exception))
            {
                throw new ConflictException(ErrorCodes.DoctorLicenseNumberConflict);
            }

            return new CreateDoctorResponse(
                userId,
                doctorId,
                user.Email,
                normalizedFullName,
                doctorProfile.Specialty,
                doctorProfile.LicenseNumber,
                user.Status.ToString());
        }

        private static (string FirstName, string LastName) SplitFullName(
            string fullName)
        {
            var parts = fullName.Split(
                ' ',
                StringSplitOptions.RemoveEmptyEntries |
                StringSplitOptions.TrimEntries);

            return parts.Length switch
            {
                0 => (string.Empty, string.Empty),
                1 => (parts[0], string.Empty),
                _ => (parts[0], string.Join(' ', parts.Skip(1)))
            };
        }

        private static bool IsEmailConflict(DbUpdateException exception) =>
            exception.ToString().Contains(
                "IX_Users_Email",
                StringComparison.OrdinalIgnoreCase);

        private static bool IsLicenseConflict(DbUpdateException exception) =>
            exception.ToString().Contains(
                "IX_DoctorProfiles_LicenseNumber",
                StringComparison.OrdinalIgnoreCase);
    }
}
