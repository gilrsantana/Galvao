namespace Galvao.Presentation.Requests.Auth;

public record RefreshRequest(string AccessToken, string RefreshToken);
