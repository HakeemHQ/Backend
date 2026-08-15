using Hakeem.Application.Common.Interfaces;
using Hakeem.Application.Constants;
using Hakeem.Application.Exceptions;
using Hakeem.Application.Features.PatientProfile.Commands.UpdateProfile;
using Hakeem.Application.Repositories.PatientProfiles;
using Hakeem.Domain.Entities;

namespace Hakeem.Infrastructure.Tests.Identity;

public sealed class UpdatePatientProfileCommandHandlerTests
{
    private const string NationalId = "30005120112345";

    [Fact]
    public async Task Handle_WhenNewBirthDateMatchesNationalId_UpdatesProfile()
    {
        var patient = CreatePatient();
        var repository = new FakePatientProfileRepository(patient);
        var handler = CreateHandler(patient.UserId, repository);

        var result = await handler.Handle(
            new UpdateProfileCommand
            {
                BirthDate = new DateTime(2000, 5, 12)
            },
            CancellationToken.None);

        Assert.True(repository.UpdateCalled);
        Assert.Equal("2000-05-12", result.BirthDate);
    }

    [Fact]
    public async Task Handle_WhenNewBirthDateDoesNotMatchNationalId_RejectsWithoutUpdating()
    {
        var patient = CreatePatient();
        var repository = new FakePatientProfileRepository(patient);
        var handler = CreateHandler(patient.UserId, repository);

        var exception = await Assert.ThrowsAsync<UnprocessableEntityException>(() =>
            handler.Handle(
                new UpdateProfileCommand
                {
                    BirthDate = new DateTime(2000, 5, 13)
                },
                CancellationToken.None));

        Assert.Equal(
            ErrorCodes.PatientIdentityNationalIdMismatch,
            exception.ErrorCode);
        Assert.False(repository.UpdateCalled);
        Assert.Equal(new DateTime(2000, 5, 12), patient.BirthDate);
    }

    private static UpdateProfileCommandHandler CreateHandler(
        Guid userId,
        IPatientProfileRepository repository) =>
        new(repository, new FakeCurrentUserContext(userId));

    private static PatientProfile CreatePatient()
    {
        var userId = Guid.NewGuid();
        return new PatientProfile
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            FullName = "Mazen Mohamed",
            BirthDate = new DateTime(2000, 5, 12),
            PatientCode = "H89K-27P",
            NationalId = NationalId,
            User = new User
            {
                Id = userId,
                Email = "mazen@hakeem.test",
                FirstName = "Mazen",
                LastName = "Mohamed",
                PhoneNumber = "+201000000000",
                Gender = "Male"
            }
        };
    }

    private sealed record FakeCurrentUserContext(Guid UserId) : ICurrentUserContext;

    private sealed class FakePatientProfileRepository(PatientProfile patient)
        : IPatientProfileRepository
    {
        public bool UpdateCalled { get; private set; }

        public Task<PatientProfile?> GetByIdAsync(
            Guid patientProfileId,
            CancellationToken cancellationToken) =>
            Task.FromResult<PatientProfile?>(
                patient.Id == patientProfileId ? patient : null);

        public Task<PatientProfile?> GetByUserIdAsync(
            Guid userId,
            CancellationToken cancellationToken) =>
            Task.FromResult<PatientProfile?>(
                patient.UserId == userId ? patient : null);

        public Task<PatientProfile?> UpdateByUserIdAsync(
            Guid userId,
            string? fullName,
            DateTime? birthDate,
            string? firstName,
            string? lastName,
            string? phoneNumber,
            string? gender,
            CancellationToken cancellationToken)
        {
            UpdateCalled = true;

            if (patient.UserId != userId)
            {
                return Task.FromResult<PatientProfile?>(null);
            }

            if (birthDate.HasValue)
            {
                patient.BirthDate = birthDate.Value;
            }

            return Task.FromResult<PatientProfile?>(patient);
        }
    }
}
