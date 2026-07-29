using Microsoft.AspNetCore.Http;

namespace Hakeem.Application.Exceptions;

public sealed class UnsupportedMediaTypeException(string code, params object[] args)
    : LocalizedHttpException(code, StatusCodes.Status415UnsupportedMediaType, args);
