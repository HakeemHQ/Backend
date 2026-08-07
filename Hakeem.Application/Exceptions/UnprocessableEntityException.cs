using Microsoft.AspNetCore.Http;

namespace Hakeem.Application.Exceptions;

public sealed class UnprocessableEntityException(
    string code,
    params object[] args)
    : LocalizedHttpException(
        code,
        StatusCodes.Status422UnprocessableEntity,
        args);
