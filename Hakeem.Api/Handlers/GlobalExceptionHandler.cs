using System.Net;
using Hakeem.Api.Utilities;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Hakeem.Application.Common.ResponseModel;
using Hakeem.Application.Common;
using Hakeem.Application.Constants;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Hakeem.Application;
using Hakeem.Application.Resources;
using Microsoft.Extensions.Localization;
using Hakeem.Application.Exceptions;
namespace Hakeem.Api.Handlers;

public class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger,
    IWebHostEnvironment webHostEnvironment,
    IStringLocalizerFactory stringLocalizerFactory
) : IExceptionHandler
{
    private readonly IStringLocalizer _localizer = stringLocalizerFactory.Create("Hakeem.Application.Resources.SharedResource", "Hakeem.Application");
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

        switch (exception)
        {
            case LocalizedHttpException ex:
                context.Response.StatusCode = ex.StatusCode;
                var localizedMessage = _localizer[ex.ErrorCode, ex.MessageArgs];
                await WriteResponse(context, localizedMessage.Value, ex.ErrorCode);
                break;

            case UnauthorizedAccessException:
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await WriteResponse(context, _localizer[ErrorCodes.AuthUnauthorized].Value, ErrorCodes.AuthUnauthorized);
                break;

            default:
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                var errorMessage = EnvironmentsChecker.IsInDevelopmentMode(_env)
                    ? $"Exception Message: {exception.Message}, " +
                      $"Inner Exception: {exception.InnerException?.Message}, " +
                      $"Stack Trace: {exception.StackTrace}"
                    : _localizer[ErrorCodes.ServerInternalError].Value;

                await WriteResponse(context, errorMessage, ErrorCodes.ServerInternalError);
                break;
        }

        return true; // short-circuit pipeline
    }

    /// <summary>
    /// Writes a failure response with the message at the top level only — no duplication in errorList.
    /// errorList is reserved for validation / field-level errors.
    /// </summary>
    private static Task WriteResponse(HttpContext context, string message, string errorCode)
    {
        var response = GenericResponseModel<object>.Failure(message, errorCode);
        return context.Response.WriteAsJsonAsync(response);
    }
}