using System.Security.Claims;
using Hakeem.Api.Authorization;
using Hakeem.Application.Common.Interfaces;
using Hakeem.Application.Interfaces.Access;
using Hakeem.Application.Repositories.DoctorProfiles;
using Hakeem.Application.Repositories.PatientProfiles;
using Hakeem.Domain.Entities;
using Hakeem.Domain.Enums.Identity;
using Microsoft.AspNetCore.Authorization;

namespace Hakeem.Infrastructure.Tests.Access;

public sealed class PatientResourceAccessAuthorizationHandlerTests
{
    [Fact]
    public async Task CombinedAccess_DoctorWithActiveAccess_Succeeds()
    {
        var patientId = Guid.NewGuid();
        var doctor = CreateDoctor();
        var requirement = new PatientResourceAccessRequirement(
            AllowVerifiedPatient: true);
        var context = CreateContext(requirement, ApplicationRole.Doctor);
        var accessService = new FakeAccessService(hasActiveAccess: true);
        var handler = new DoctorPatientResourceAccessHandler(
            new FakePatientResourceResolver(patientId),
            accessService,
            new FakeDoctorProfileRepository(doctor),
            new FakeCurrentUserContext(doctor.UserId));

        await handler.HandleAsync(context);

        Assert.True(context.HasSucceeded);
        Assert.Equal(patientId, accessService.CheckedPatientId);
    }

    [Fact]
    public async Task CombinedAccess_VerifiedResourceOwner_Succeeds()
    {
        var patient = CreatePatient(IdentityVerificationStatus.Verified);
        var requirement = new PatientResourceAccessRequirement(
            AllowVerifiedPatient: true);
        var context = CreateContext(requirement, ApplicationRole.Patient);
        var handler = new VerifiedPatientResourceAccessHandler(
            new FakePatientResourceResolver(patient.Id),
            new FakePatientProfileRepository(patient),
            new FakeCurrentUserContext(patient.UserId));

        await handler.HandleAsync(context);

        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task CombinedAccess_VerifiedPatientWhoDoesNotOwnResource_DoesNotSucceed()
    {
        var patient = CreatePatient(IdentityVerificationStatus.Verified);
        var requirement = new PatientResourceAccessRequirement(
            AllowVerifiedPatient: true);
        var context = CreateContext(requirement, ApplicationRole.Patient);
        var handler = new VerifiedPatientResourceAccessHandler(
            new FakePatientResourceResolver(Guid.NewGuid()),
            new FakePatientProfileRepository(patient),
            new FakeCurrentUserContext(patient.UserId));

        await handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task CombinedAccess_UnverifiedResourceOwner_DoesNotSucceed()
    {
        var patient = CreatePatient(IdentityVerificationStatus.Pending);
        var requirement = new PatientResourceAccessRequirement(
            AllowVerifiedPatient: true);
        var context = CreateContext(requirement, ApplicationRole.Patient);
        var handler = new VerifiedPatientResourceAccessHandler(
            new FakePatientResourceResolver(patient.Id),
            new FakePatientProfileRepository(patient),
            new FakeCurrentUserContext(patient.UserId));

        await handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task DoctorOnlyAccess_VerifiedResourceOwner_DoesNotSucceed()
    {
        var patient = CreatePatient(IdentityVerificationStatus.Verified);
        var requirement = new PatientResourceAccessRequirement(
            AllowVerifiedPatient: false);
        var context = CreateContext(requirement, ApplicationRole.Patient);
        var handler = new VerifiedPatientResourceAccessHandler(
            new FakePatientResourceResolver(patient.Id),
            new FakePatientProfileRepository(patient),
            new FakeCurrentUserContext(patient.UserId));

        await handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    private static AuthorizationHandlerContext CreateContext(
        PatientResourceAccessRequirement requirement,
        ApplicationRole role)
    {
        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.Role, role.ToString())],
            "Test");
        return new AuthorizationHandlerContext(
            [requirement],
            new ClaimsPrincipal(identity),
            resource: new object());
    }

    private static DoctorProfile CreateDoctor() => new()
    {
        Id = Guid.NewGuid(),
        UserId = Guid.NewGuid()
    };

    private static PatientProfile CreatePatient(
        IdentityVerificationStatus verificationStatus) => new()
    {
        Id = Guid.NewGuid(),
        UserId = Guid.NewGuid(),
        IdentityVerificationStatus = verificationStatus
    };

    private sealed record FakeCurrentUserContext(Guid UserId) : ICurrentUserContext;

    private sealed class FakePatientResourceResolver(Guid patientId)
        : IPatientResourceResolver
    {
        public Task<Guid?> ResolvePatientIdAsync(object? resource) =>
            Task.FromResult<Guid?>(patientId);
    }

    private sealed class FakeAccessService(bool hasActiveAccess)
        : IDoctorPatientAccessService
    {
        public Guid? CheckedPatientId { get; private set; }

        public Task<bool> HasActiveAccessAsync(
            Guid doctorId,
            Guid patientId,
            CancellationToken cancellationToken)
        {
            CheckedPatientId = patientId;
            return Task.FromResult(hasActiveAccess);
        }
    }

    private sealed class FakeDoctorProfileRepository(DoctorProfile doctor)
        : IDoctorProfileRepository
    {
        public Task<DoctorProfile?> GetByUserIdAsync(
            Guid userId,
            CancellationToken cancellationToken) =>
            Task.FromResult<DoctorProfile?>(
                userId == doctor.UserId ? doctor : null);

        public void Add(DoctorProfile doctorProfile) =>
            throw new NotSupportedException();

        public Task<bool> LicenseNumberExistsAsync(
            string licenseNumber,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<DoctorProfile>> GetAllAsync(
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<DoctorProfile?> GetByIdAsync(
            Guid doctorId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<DoctorProfile?> GetByIdForUpdateAsync(
            Guid doctorId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<DoctorProfile>> GetFilteredAsync(
            string? search,
            string? specialty,
            AccountStatus? status,
            int page,
            int pageSize,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class FakePatientProfileRepository(PatientProfile patient)
        : IPatientProfileRepository
    {
        public Task<PatientProfile?> GetByIdAsync(
            Guid patientProfileId,
            CancellationToken cancellationToken) =>
            Task.FromResult<PatientProfile?>(
                patientProfileId == patient.Id ? patient : null);

        public Task<PatientProfile?> GetByUserIdAsync(
            Guid userId,
            CancellationToken cancellationToken) =>
            Task.FromResult<PatientProfile?>(
                userId == patient.UserId ? patient : null);

        public Task<PatientProfile?> UpdateByUserIdAsync(
            Guid userId,
            string? fullName,
            DateTime? birthDate,
            string? firstName,
            string? lastName,
            string? phoneNumber,
            string? gender,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}
