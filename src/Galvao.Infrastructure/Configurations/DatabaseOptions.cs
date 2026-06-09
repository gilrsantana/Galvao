namespace Galvao.Infrastructure.Configurations;

public class DatabaseOptions
{
    public const string SectionName = "DatabaseOptions";

    public bool EnableDetailedErrors { get; set; } = false;
    public bool EnableSensitiveDataLogging { get; set; } = false;
    public int CommandTimeout { get; set; } = 30;
    public bool EnableRetryOnFailure { get; set; } = true;
    public int MaxRetryCount { get; set; } = 3;
    public int MaxRetryDelaySeconds { get; set; } = 5;
    public int? MaxBatchSize { get; set; }
}
