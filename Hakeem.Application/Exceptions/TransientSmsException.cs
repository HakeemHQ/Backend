namespace Hakeem.Application.Exceptions;

/// <summary>
/// Thrown when the Ducont SMS API returns a transient (retryable) communication error
/// (codes 70, 80, 81, 110). The outbox processor will automatically retry according
/// to its configured backoff policy.
/// </summary>
public class TransientSmsException : SmsException
{
    public TransientSmsException(string errorCode, string description)
        : base(errorCode, description)
    {
    }
}
