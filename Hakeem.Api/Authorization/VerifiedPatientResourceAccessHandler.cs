using Hakeem.Application.Common.Interfaces;
using Hakeem.Application.Repositories.PatientProfiles;
using Hakeem.Domain.Enums.Identity;
using Microsoft.AspNetCore.Authorization;

namespace Hakeem.Api.Authorization;

public sealed class VerifiedPatientResourceAccessHandler(
    IPatientResourceResolver patientResourceResolver,
    IPatientProfileRepository patientProfileRepository,
    ICurrentUserContext currentUserContext)
    : AuthorizationHandler<PatientResourceAccessRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PatientResourceAccessRequirement requirement)
    {
        if (!requirement.AllowVerifiedPatient ||
            !context.User.IsInRole(nameof(ApplicationRole.Patient)))
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

        var patient = await patientProfileRepository.GetByUserIdAsync(
            userId,
            ResolveCancellationToken(context.Resource));

        if (patient is
            {
                IdentityVerificationStatus: IdentityVerificationStatus.Verified
            } &&
            patient.Id == patientId.Value)
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
