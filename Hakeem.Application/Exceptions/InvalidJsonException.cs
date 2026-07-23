using Microsoft.AspNetCore.Http;
namespace Hakeem.Application.Exceptions;

public class InvalidJsonException(string code, params object[] args) : LocalizedHttpException(code, StatusCodes.Status400BadRequest, args)
{
    public string Code { get; init; } = code;
}
