using Microsoft.AspNetCore.Http;

namespace Hakeem.Application.Exceptions;

public class ConflictException(string code, params object[] args) : LocalizedHttpException(code, StatusCodes.Status409Conflict, args)
{
    public string Code { get; } = code;
}
