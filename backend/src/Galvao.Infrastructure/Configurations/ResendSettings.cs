namespace Galvao.Infrastructure.Configurations;

public class ResendSettings
{
    public const string SectionName = "ResendSettings";

    public string ManagerApiKey { get; set; } = string.Empty;
    public string SenderApiKey { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public string FromEmail { get; set; } = "onboarding@resend.dev";
}