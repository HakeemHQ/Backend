using Hakeem.Application.Common.ResponseModel;
using Hakeem.Application.Resources;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace Hakeem.Api.Controllers;

/// <summary>
/// Base controller providing localized response helpers.
/// All API controllers should inherit from this class.
/// </summary>
[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    protected readonly IStringLocalizer<SharedResource> Localizer;

    protected ApiControllerBase(IStringLocalizer<SharedResource> localizer)
    {
        Localizer = localizer;
    }

    /// <summary>
    /// Returns 200 OK with data and an optional localized success message.
    /// </summary>
    protected IActionResult SuccessResponse<T>(T data, string? messageKey = null, params object[] args)
    {
        var message = messageKey is not null
            ? Localizer[messageKey, args].Value
            : Localizer["Operation.Success"].Value;

        return Ok(GenericResponseModel<T>.Success(data, message));
    }

    /// <summary>
    /// Returns 200 OK with a localized success message and no meaningful data.
    /// </summary>
    protected IActionResult SuccessResponse(string? messageKey = null, params object[] args)
    {
        var message = messageKey is not null
            ? Localizer[messageKey, args].Value
            : Localizer["Operation.Success"].Value;
        return Ok(GenericResponseModel<object>.Success(null!, message));
    }

    /// <summary>
    /// Returns 201 Created with data and an optional localized message.
    /// </summary>
    protected IActionResult CreatedResponse<T>(T data, string? messageKey = null, params object[] args)
    {
        var message = messageKey is not null
            ? Localizer[messageKey, args].Value
            : Localizer["Operation.Success"].Value;
        return StatusCode(StatusCodes.Status201Created, GenericResponseModel<T>.Success(data, message));
    }
}
