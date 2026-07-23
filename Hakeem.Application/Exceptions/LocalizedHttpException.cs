namespace Hakeem.Application.Exceptions;

public class LocalizedHttpException(string errorCode, int statusCode, params object[] args) : Exception
{
    public string ErrorCode { get; init; } = errorCode;
    public int StatusCode { get; init; } = statusCode;
    public object[] MessageArgs { get; init; } = args;
}
