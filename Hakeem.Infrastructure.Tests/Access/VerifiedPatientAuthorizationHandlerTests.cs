using System.Security.Claims;
using Hakeem.Api.Authorization;
using Hakeem.Application.Common.Interfaces;
using Hakeem.Application.Repositories.PatientProfiles;
using Hakeem.Domain.Entities;
using Hakeem.Domain.Enums.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace Hakeem.Infrastructure.Tests.Access;

public sealed class VerifiedPatientAuthorizationHandlerTests
{
    [Fact]
    public async Task Handle_VerifiedPatient_Succeeds()
    {
        var patient = CreatePatient(IdentityVerificationStatus.Verified);
        var requirement = new VerifiedPatientRequirement();
        var context = CreateAuthorizationContext(requirement);
        var handler = CreateHandler(patient);

        await handler.HandleAsync(context);

        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task Handle_PendingPatient_DoesNotSucceed()
    {
        var patient = CreatePatient(IdentityVerificationStatus.Pending);
        var requirement = new VerifiedPatientRequirement();
        var context = CreateAuthorizationContext(requirement);
        var handler = CreateHandler(patient);

        await handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task Handle_MissingPatientProfile_DoesNotSucceed()
    {
        var userId = Guid.NewGuid();
        var requirement = new VerifiedPatientRequirement();
        var context = CreateAuthorizationContext(requirement);
        var handler = new VerifiedPatientHandler(
            new FakePatientProfileRepository(null),
            new FakeCurrentUserContext(userId));

        await handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    private static VerifiedPatientHandler CreateHandler(PatientProfile patient) =>
        new(
            new FakePatientProfileRepository(patient),
            new FakeCurrentUserContext(patient.UserId));

    private static AuthorizationHandlerContext CreateAuthorizationContext(
        VerifiedPatientRequirement requirement)
    {
        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.Role, nameof(ApplicationRole.Patient))],
            "Test");
        return new AuthorizationHandlerContext(
            [requirement],
            new ClaimsPrincipal(identity),
            new DefaultHttpContext());
    }

    private static PatientProfile CreatePatient(
        IdentityVerificationStatus verificationStatus) => new()
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            IdentityVerificationStatus = verificationStatus
        };

    private sealed record FakeCurrentUserContext(Guid UserId)
        : ICurrentUserContext;

    private sealed class FakePatientProfileRepository(PatientProfile? patient)
        : IPatientProfileRepository
    {
        public Task<PatientProfile?> GetByIdAsync(
            Guid patientProfileId,
            CancellationToken cancellationToken) =>
            Task.FromResult<PatientProfile?>(
                patient?.Id == patientProfileId ? patient : null);

        public Task<PatientProfile?> GetByUserIdAsync(
            Guid userId,
            CancellationToken cancellationToken) =>
            Task.FromResult<PatientProfile?>(
                patient?.UserId == userId ? patient : null);

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

