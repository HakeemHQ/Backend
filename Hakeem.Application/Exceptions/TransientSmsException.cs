namespace Hakeem.Application.Exceptions;


public class TransientSmsException : SmsException
{
    public TransientSmsException(string errorCode, string description)
        : base(errorCode, description)
    {
    }
}
