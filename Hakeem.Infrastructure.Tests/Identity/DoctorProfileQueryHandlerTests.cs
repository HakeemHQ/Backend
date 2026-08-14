using Hakeem.Application.Common.Interfaces;
using Hakeem.Application.Features.DoctorProfile.Queries.GetDoctorProfile;
using Hakeem.Application.Repositories.DoctorProfiles;
using Hakeem.Domain.Entities;
using Hakeem.Domain.Enums.Identity;

namespace Hakeem.Infrastructure.Tests.Identity;

public sealed class DoctorProfileQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsSignedInDoctorProfile()
    {
        var userId = Guid.NewGuid();
        var doctorId = Guid.NewGuid();
        var profile = new DoctorProfile
        {
            Id = doctorId,
            UserId = userId,
            Specialty = "Cardiology",
            LicenseNumber = "EG-12345",
            User = new User
            {
                Id = userId,
                Email = "ahmed@hakeem.test",
                FirstName = "Dr. Ahmed",
                LastName = "Hassan",
                Role = ApplicationRole.Doctor,
                Status = AccountStatus.Active
            }
        };
        var handler = new GetDoctorProfileQueryHandler(
            new FakeDoctorProfileRepository(profile),
            new FakeCurrentUserContext(userId));

        var response = await handler.Handle(
            new GetDoctorProfileQuery(),
            CancellationToken.None);

        Assert.Equal(doctorId, response.DoctorId);
        Assert.Equal("Dr. Ahmed Hassan", response.FullName);
        Assert.Equal("ahmed@hakeem.test", response.Email);
        Assert.Equal("Cardiology", response.Specialty);
        Assert.Equal("EG-12345", response.LicenseNumber);
        Assert.Equal("Active", response.Status);
    }

    private sealed record FakeCurrentUserContext(Guid UserId) : ICurrentUserContext;

    private sealed class FakeDoctorProfileRepository(DoctorProfile profile)
        : IDoctorProfileRepository
    {
        public Task<DoctorProfile?> GetByUserIdAsync(
            Guid userId,
            CancellationToken cancellationToken) =>
            Task.FromResult<DoctorProfile?>(profile.UserId == userId ? profile : null);

        public void Add(DoctorProfile doctorProfile) => throw new NotSupportedException();
        public Task<bool> LicenseNumberExistsAsync(string licenseNumber, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
        public Task<IReadOnlyList<DoctorProfile>> GetAllAsync(CancellationToken cancellationToken) =>
            throw new NotSupportedException();
        public Task<DoctorProfile?> GetByIdAsync(Guid doctorId, CancellationToken cancellationToken) =>
            Task.FromResult<DoctorProfile?>(profile.Id == doctorId ? profile : null);
        public Task<DoctorProfile?> GetByIdForUpdateAsync(Guid doctorId, CancellationToken cancellationToken) =>
            Task.FromResult<DoctorProfile?>(profile.Id == doctorId ? profile : null);
    }
}
