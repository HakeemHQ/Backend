namespace Hakeem.Application.Exceptions;


public class SmsException : Exception
{
    public string ErrorCode { get; }

    public SmsException(string errorCode, string description)
        : base($"SMS API error {errorCode}: {description}")
    {
        ErrorCode = errorCode;
    }
}
