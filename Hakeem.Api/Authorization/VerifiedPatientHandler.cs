using Hakeem.Application.Common.Interfaces;
using Hakeem.Application.Repositories.PatientProfiles;
using Hakeem.Domain.Enums.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Hakeem.Api.Authorization;

public sealed class VerifiedPatientHandler(
    IPatientProfileRepository patientProfileRepository,
    ICurrentUserContext currentUserContext)
    : AuthorizationHandler<VerifiedPatientRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        VerifiedPatientRequirement requirement)
    {
        Guid userId;
        try
        {
            userId = currentUserContext.UserId;
        }
        catch (UnauthorizedAccessException)
        {
            return;
        }

        var patient = await patientProfileRepository.GetByUserIdAsync(
            userId,
            ResolveCancellationToken(context.Resource));

        if (patient?.IdentityVerificationStatus ==
            IdentityVerificationStatus.Verified)
        {
            context.Succeed(requirement);
        }
    }

    private static CancellationToken ResolveCancellationToken(object? resource)
    {
        return resource switch
        {
            HttpContext httpContext => httpContext.RequestAborted,
            AuthorizationFilterContext filterContext =>
                filterContext.HttpContext.RequestAborted,
            _ => CancellationToken.None
        };
    }
}
