namespace Galvao.Presentation.Requests.Members;

public record UpdateMarketingPreferencesRequest(bool AcceptNews, bool AcceptPromo, string? ConsentToken, DateTime? ConsentedAt);

