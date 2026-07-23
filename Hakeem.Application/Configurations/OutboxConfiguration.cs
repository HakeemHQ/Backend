namespace Hakeem.Application.Configurations;

public class OutboxConfiguration
{
    public const string SectionName = "OutboxSettings";


    public int PollingIntervalSeconds { get; set; } = 10;


    public int BatchSize { get; set; } = 20;


    public int MaxRetries { get; set; } = 3;


    public int RetryDelaySeconds { get; set; } = 60;


    public int LockDurationSeconds { get; set; } = 120;
}
