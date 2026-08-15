using Hakeem.Application.Common.Interfaces;
using Hakeem.Application.Interfaces.Access;
using Hakeem.Application.Repositories.DoctorProfiles;
using Hakeem.Domain.Enums.Identity;
using Microsoft.AspNetCore.Authorization;

namespace Hakeem.Api.Authorization;

public sealed class DoctorPatientResourceAccessHandler(
    IPatientResourceResolver patientResourceResolver,
    IDoctorPatientAccessService accessService,
    IDoctorProfileRepository doctorProfileRepository,
    ICurrentUserContext currentUserContext)
    : AuthorizationHandler<PatientResourceAccessRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PatientResourceAccessRequirement requirement)
    {
        if (!context.User.IsInRole(nameof(ApplicationRole.Doctor)))
        {
            return;
        }

        Guid userId;
        try
        {
            userId = currentUserContext.UserId;
        }
        catch (UnauthorizedAccessException)
        {
            return;
        }

        var patientId = await patientResourceResolver.ResolvePatientIdAsync(
            context.Resource);
        if (!patientId.HasValue)
        {
            return;
        }

        var cancellationToken = ResolveCancellationToken(context.Resource);
        var doctor = await doctorProfileRepository.GetByUserIdAsync(
            userId,
            cancellationToken);

        if (doctor is not null &&
            await accessService.HasActiveAccessAsync(
                doctor.Id,
                patientId.Value,
                cancellationToken))
        {
            context.Succeed(requirement);
        }
    }

    private static CancellationToken ResolveCancellationToken(object? resource) =>
        resource switch
        {
            HttpContext httpContext => httpContext.RequestAborted,
            Microsoft.AspNetCore.Mvc.Filters.AuthorizationFilterContext filterContext =>
                filterContext.HttpContext.RequestAborted,
            _ => CancellationToken.None
        };
}
