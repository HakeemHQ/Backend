using Microsoft.AspNetCore.Http;

namespace Hakeem.Application.Exceptions;

public sealed class PayloadTooLargeException(string code, params object[] args)
    : LocalizedHttpException(code, StatusCodes.Status413PayloadTooLarge, args);
