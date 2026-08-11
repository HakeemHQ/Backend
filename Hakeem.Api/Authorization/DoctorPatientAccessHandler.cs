using Hakeem.Application.Common.Interfaces;
using Hakeem.Application.Interfaces.Access;
using Hakeem.Application.Repositories.DoctorProfiles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Hakeem.Api.Authorization;

public sealed class DoctorPatientAccessHandler(
    IDoctorPatientAccessService accessService,
    IDoctorProfileRepository doctorProfileRepository,
    ICurrentUserContext currentUserContext)
    : AuthorizationHandler<DoctorPatientAccessRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        DoctorPatientAccessRequirement requirement)
    {
        if (!TryResolvePatientId(context.Resource, out var patientId))
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

        var cancellationToken = ResolveCancellationToken(context.Resource);
        var doctor = await doctorProfileRepository.GetByUserIdAsync(
            userId,
            cancellationToken);

        if (doctor is null)
        {
            return;
        }

        if (await accessService.HasActiveAccessAsync(
                doctor.Id,
                patientId,
                cancellationToken))
        {
            context.Succeed(requirement);
        }
    }

    private static bool TryResolvePatientId(object? resource, out Guid patientId)
    {
        switch (resource)
        {
            case DoctorPatientAccessResource accessResource:
                patientId = accessResource.PatientId;
                return patientId != Guid.Empty;
            case Guid directPatientId:
                patientId = directPatientId;
                return patientId != Guid.Empty;
            case HttpContext httpContext:
                return TryResolveRoutePatientId(httpContext, out patientId);
            case AuthorizationFilterContext filterContext:
                return TryResolveRoutePatientId(filterContext.HttpContext, out patientId);
            default:
                patientId = Guid.Empty;
                return false;
        }
    }

    private static bool TryResolveRoutePatientId(
        HttpContext httpContext,
        out Guid patientId)
    {
        return TryParseRouteValue(
                httpContext,
                DoctorPatientAccessPolicy.PatientIdRouteValue,
                out patientId) ||
            TryParseRouteValue(
                httpContext,
                DoctorPatientAccessPolicy.PatientProfileIdRouteValue,
                out patientId);
    }

    private static bool TryParseRouteValue(
        HttpContext httpContext,
        string key,
        out Guid patientId)
    {
        if (httpContext.Request.RouteValues.TryGetValue(key, out var value) &&
            Guid.TryParse(value?.ToString(), out patientId) &&
            patientId != Guid.Empty)
        {
            return true;
        }

        patientId = Guid.Empty;
        return false;
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
