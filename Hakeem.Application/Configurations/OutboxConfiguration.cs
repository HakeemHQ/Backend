namespace Hakeem.Application.Configurations;

public class OutboxConfiguration
{
    public const string SectionName = "OutboxSettings";

    /// <summary>
    /// How often the background worker polls for pending events (in seconds).
    /// </summary>
    public int PollingIntervalSeconds { get; set; } = 10;

    /// <summary>
    /// Maximum number of events to claim per polling cycle.
    /// </summary>
    public int BatchSize { get; set; } = 20;

    /// <summary>
    /// Maximum number of retry attempts before marking an event as Failed.
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Base delay between retries (in seconds). Actual delay uses exponential backoff: delay * 2^retryCount.
    /// </summary>
    public int RetryDelaySeconds { get; set; } = 60;

    /// <summary>
    /// How long a claimed event is locked (in seconds) to prevent other instances from processing it.
    /// </summary>
    public int LockDurationSeconds { get; set; } = 120;
}
