namespace Galvao.Presentation.Requests.Auth;

public record ConfirmEmailRequest(Guid UserId, string Token);
