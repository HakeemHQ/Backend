using Microsoft.AspNetCore.Http;

namespace Hakeem.Application.Exceptions;

public sealed class ServiceUnavailableException(
    string code,
    params object[] args)
    : LocalizedHttpException(
        code,
        StatusCodes.Status503ServiceUnavailable,
        args);
