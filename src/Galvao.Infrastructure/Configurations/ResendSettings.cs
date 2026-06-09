namespace Galvao.Infrastructure.Configurations;

public class ResendSettings
{
    public const string SectionName = "ResendSettings";

    public string ApiKey { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
}
