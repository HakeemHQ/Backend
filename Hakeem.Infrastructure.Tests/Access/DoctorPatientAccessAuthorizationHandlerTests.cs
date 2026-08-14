using System.Security.Claims;
using Hakeem.Api.Authorization;
using Hakeem.Application.Common.Interfaces;
using Hakeem.Application.Interfaces.Access;
using Hakeem.Application.Repositories.DoctorProfiles;
using Hakeem.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace Hakeem.Infrastructure.Tests.Access;

public sealed class DoctorPatientAccessAuthorizationHandlerTests
{
    [Fact]
    public async Task Handle_ExplicitPatientResourceAndActiveAccess_Succeeds()
    {
        var doctor = CreateDoctor();
        var patientId = Guid.NewGuid();
        var service = new FakeAccessService(hasActiveAccess: true);
        var requirement = new DoctorPatientAccessRequirement();
        var context = CreateAuthorizationContext(
            requirement,
            new DoctorPatientAccessResource(patientId));
        var handler = CreateHandler(doctor, service);

        await handler.HandleAsync(context);

        Assert.True(context.HasSucceeded);
        Assert.Equal(doctor.Id, service.CheckedDoctorId);
        Assert.Equal(patientId, service.CheckedPatientId);
    }

    [Fact]
    public async Task Handle_NoActiveAccess_DoesNotSucceed()
    {
        var doctor = CreateDoctor();
        var service = new FakeAccessService(hasActiveAccess: false);
        var requirement = new DoctorPatientAccessRequirement();
        var context = CreateAuthorizationContext(
            requirement,
            new DoctorPatientAccessResource(Guid.NewGuid()));
        var handler = CreateHandler(doctor, service);

        await handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
        Assert.True(service.WasCalled);
    }

    [Fact]
    public async Task Handle_PatientIdRouteValue_ResolvesPatientAndSucceeds()
    {
        var doctor = CreateDoctor();
        var patientId = Guid.NewGuid();
        var service = new FakeAccessService(hasActiveAccess: true);
        var requirement = new DoctorPatientAccessRequirement();
        var httpContext = new DefaultHttpContext();
        httpContext.Request.RouteValues[DoctorPatientAccessPolicy.PatientIdRouteValue]
            = patientId.ToString();
        var context = CreateAuthorizationContext(requirement, httpContext);
        var handler = CreateHandler(doctor, service);

        await handler.HandleAsync(context);

        Assert.True(context.HasSucceeded);
        Assert.Equal(patientId, service.CheckedPatientId);
    }

    [Fact]
    public async Task Handle_MissingPatientResource_DoesNotQueryAccess()
    {
        var doctor = CreateDoctor();
        var service = new FakeAccessService(hasActiveAccess: true);
        var requirement = new DoctorPatientAccessRequirement();
        var context = CreateAuthorizationContext(
            requirement,
            new DefaultHttpContext());
        var handler = CreateHandler(doctor, service);

        await handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
        Assert.False(service.WasCalled);
    }

    private static DoctorPatientAccessHandler CreateHandler(
        DoctorProfile doctor,
        FakeAccessService service) => new(
            service,
            new FakeDoctorProfileRepository(doctor),
            new FakeCurrentUserContext(doctor.UserId));

    private static AuthorizationHandlerContext CreateAuthorizationContext(
        DoctorPatientAccessRequirement requirement,
        object resource)
    {
        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.Role, "Doctor")],
            "Test");
        return new AuthorizationHandlerContext(
            [requirement],
            new ClaimsPrincipal(identity),
            resource);
    }

    private static DoctorProfile CreateDoctor() => new()
    {
        Id = Guid.NewGuid(),
        UserId = Guid.NewGuid()
    };

    private sealed record FakeCurrentUserContext(Guid UserId) : ICurrentUserContext;

    private sealed class FakeDoctorProfileRepository(DoctorProfile doctor)
        : IDoctorProfileRepository
    {
        public Task<DoctorProfile?> GetByUserIdAsync(
            Guid userId,
            CancellationToken cancellationToken) =>
            Task.FromResult<DoctorProfile?>(doctor.UserId == userId ? doctor : null);

        public void Add(DoctorProfile doctorProfile) => throw new NotSupportedException();
        public Task<bool> LicenseNumberExistsAsync(string licenseNumber, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
        public Task<IReadOnlyList<DoctorProfile>> GetAllAsync(CancellationToken cancellationToken) =>
            throw new NotSupportedException();
        public Task<DoctorProfile?> GetByIdAsync(Guid doctorId, CancellationToken cancellationToken) =>
            Task.FromResult<DoctorProfile?>(doctor.Id == doctorId ? doctor : null);
        public Task<DoctorProfile?> GetByIdForUpdateAsync(Guid doctorId, CancellationToken cancellationToken) =>
            Task.FromResult<DoctorProfile?>(doctor.Id == doctorId ? doctor : null);
    }

    private sealed class FakeAccessService(bool hasActiveAccess)
        : IDoctorPatientAccessService
    {
        public bool WasCalled { get; private set; }
        public Guid? CheckedDoctorId { get; private set; }
        public Guid? CheckedPatientId { get; private set; }

        public Task<bool> HasActiveAccessAsync(
            Guid doctorId,
            Guid patientId,
            CancellationToken cancellationToken)
        {
            WasCalled = true;
            CheckedDoctorId = doctorId;
            CheckedPatientId = patientId;
            return Task.FromResult(hasActiveAccess);
        }
    }
}
