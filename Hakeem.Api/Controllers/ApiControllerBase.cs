using Hakeem.Application.Common.ResponseModel;
using Hakeem.Application.Resources;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace Hakeem.Api.Controllers;


[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    protected readonly IStringLocalizer<SharedResource> Localizer;

    protected ApiControllerBase(IStringLocalizer<SharedResource> localizer)
    {
        Localizer = localizer;
    }


    protected IActionResult SuccessResponse<T>(T data, string? messageKey = null, params object[] args)
    {
        var message = messageKey is not null
            ? Localizer[messageKey, args].Value
            : Localizer["Operation.Success"].Value;

        return Ok(GenericResponseModel<T>.Success(data, message));
    }


    protected IActionResult SuccessResponse(string? messageKey = null, params object[] args)
    {
        var message = messageKey is not null
            ? Localizer[messageKey, args].Value
            : Localizer["Operation.Success"].Value;
        return Ok(GenericResponseModel<object>.Success(null!, message));
    }


    protected IActionResult CreatedResponse<T>(T data, string? messageKey = null, params object[] args)
    {
        var message = messageKey is not null
            ? Localizer[messageKey, args].Value
            : Localizer["Operation.Success"].Value;
        return StatusCode(StatusCodes.Status201Created, GenericResponseModel<T>.Success(data, message));
    }
}
