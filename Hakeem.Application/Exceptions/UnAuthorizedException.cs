using Microsoft.AspNetCore.Http;

namespace Hakeem.Application.Exceptions;

public class UnAuthorizedException(string code, params object[] args) : LocalizedHttpException(code, StatusCodes.Status401Unauthorized, args)
{
    public string Code { get; init; } = code;
}
