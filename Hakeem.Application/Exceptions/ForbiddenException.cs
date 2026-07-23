using Microsoft.AspNetCore.Http;

namespace Hakeem.Application.Exceptions;

public class ForbiddenException(string code, params object[] args) : LocalizedHttpException(code, StatusCodes.Status403Forbidden, args)
{
    public string Code { get; init; } = code;
}
