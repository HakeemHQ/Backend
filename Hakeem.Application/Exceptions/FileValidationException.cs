using Microsoft.AspNetCore.Http;

namespace Hakeem.Application.Exceptions;

public class FileValidationException : LocalizedHttpException
{
    public FileValidationException(string code, params object[] args) : base(code, StatusCodes.Status400BadRequest, args)
    {
    }
}
