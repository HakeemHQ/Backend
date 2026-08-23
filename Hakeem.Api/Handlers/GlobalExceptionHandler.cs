using System.Globalization;
using System.Net;
using Hakeem.Api.Utilities;
using Hakeem.Application;
using Hakeem.Application.Common;
using Hakeem.Application.Common.ResponseModel;
using Hakeem.Application.Constants;
using Hakeem.Application.Exceptions;
using Hakeem.Application.Resources;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace Hakeem.Api.Handlers;

public class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger,
    IWebHostEnvironment webHostEnvironment,
    IStringLocalizer<SharedResource> localizer
) : IExceptionHandler
{
    private readonly IStringLocalizer<SharedResource> _localizer = localizer;
    private readonly ILogger<GlobalExceptionHandler> _logger = logger;
    private readonly IWebHostEnvironment _env = webHostEnvironment;

    public async ValueTask<bool> TryHandleAsync(
        HttpContext context,
        Exception exception,
        CancellationToken cancellationToken)
    {
        // Log the exception
        _logger.LogError(exception, "Exception occurred. Path: {Path}, Method: {Method}",
            context.Request.Path, context.Request.Method);

        context.Response.ContentType = "application/json";

        var culture = context.Features.Get<IRequestCultureFeature>()?.RequestCulture?.UICulture
                      ?? CultureInfo.CurrentUICulture;

        switch (exception)
        {
            case LocalizedHttpException ex:
                context.Response.StatusCode = ex.StatusCode;
                var localizedMessage = GetLocalizedMessage(ex.ErrorCode, culture, ex.MessageArgs);
                await WriteResponse(context, localizedMessage, ex.ErrorCode);
                break;

            case UnauthorizedAccessException:
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                var unauthMessage = GetLocalizedMessage(ErrorCodes.AuthUnauthorized, culture);
                await WriteResponse(context, unauthMessage, ErrorCodes.AuthUnauthorized);
                break;

            default:
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                var errorMessage = EnvironmentsChecker.IsInDevelopmentMode(_env)
                    ? $"Exception Message: {exception.Message}, " +
                      $"Inner Exception: {exception.InnerException?.Message}, " +
                      $"Stack Trace: {exception.StackTrace}"
                    : GetLocalizedMessage(ErrorCodes.ServerInternalError, culture);

                await WriteResponse(context, errorMessage, ErrorCodes.ServerInternalError);
                break;
        }

        return true; // short-circuit pipeline
    }

    private string GetLocalizedMessage(string errorCode, CultureInfo culture, params object[]? args)
    {
        try
        {
            var raw = SharedResource.ResourceManager.GetString(errorCode, culture);
            if (!string.IsNullOrEmpty(raw))
            {
                return (args != null && args.Length > 0)
                    ? string.Format(culture, raw, args)
                    : raw;
            }
        }
        catch
        {
            // Fallback to IStringLocalizer
        }

        var localized = _localizer[errorCode, args ?? Array.Empty<object>()];
        return localized.ResourceNotFound ? errorCode : localized.Value;
    }

    private static Task WriteResponse(HttpContext context, string message, string errorCode)
    {
        var response = GenericResponseModel<object>.Failure(message, errorCode);
        return context.Response.WriteAsJsonAsync(response);
    }
}