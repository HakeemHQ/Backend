namespace Hakeem.Application.Exceptions;

/// <summary>
/// Thrown when the Ducont SMS API returns a permanent (non-retryable) error.
/// Examples: invalid credentials, blank message/number, invalid account, no credit.
/// The outbox processor will still exhaust its retry count for this type — consider
/// setting MaxRetries=0 on the OutboxEvent when these conditions are known upfront.
/// </summary>
public class SmsException : Exception
{
    public string ErrorCode { get; }

    public SmsException(string errorCode, string description)
        : base($"SMS API error {errorCode}: {description}")
    {
        ErrorCode = errorCode;
    }
}
