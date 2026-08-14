using Hakeem.Application.Exceptions;
using Hakeem.Application.Features.Admin.Doctors.DTOs;
using Hakeem.Application.Interfaces.Services.Auth;
using Hakeem.Application.Repositories.DoctorProfiles;
using Hakeem.Application.Repositories.Users;
using Hakeem.Domain.Entities;
using Hakeem.Domain.Enums.Identity;
using Hakeem.Domain.Interfaces;
using MediatR;

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
            // 1. Check email uniqueness
            var existingUser = await userRepository.GetByEmailAsync(
                request.Email,
                cancellationToken);

            if (existingUser is not null)
            {
                throw new ConflictException("User.EmailAlreadyExists");
            }

            // 2. Check license uniqueness
            var licenseExists =
                await doctorProfileRepository.LicenseNumberExistsAsync(
                    request.LicenseNumber,
                    cancellationToken);

            if (licenseExists)
            {
                throw new ConflictException(
                    "Doctor.LicenseNumberAlreadyExists");
            }

            // 3. Hash password
            var passwordHash = passwordHasher.Hash(request.Password);

            // 4. Create User
            var user = new User
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,
                PasswordHash = passwordHash,
                Role = ApplicationRole.Doctor,
                Status = AccountStatus.Active
            };

            // 5. Create DoctorProfile
            var doctorProfile = new Domain.Entities.DoctorProfile
            {
                User = user,
                Specialty = request.Specialty,
                LicenseNumber = request.LicenseNumber
            };

            // 6. Add both to DbContext
            userRepository.Add(user);
            doctorProfileRepository.Add(doctorProfile);

            // 7. Save both in one transaction
            await unitOfWork.SaveChanges(cancellationToken);

            // 8. Return response
            return new CreateDoctorResponse(
                doctorProfile.UserId,
                doctorProfile.Id,
                $"{user.FirstName} {user.LastName}".Trim(),
                user.Email,
                doctorProfile.Specialty,
                doctorProfile.LicenseNumber,
                user.Status);
        }
    }
}
