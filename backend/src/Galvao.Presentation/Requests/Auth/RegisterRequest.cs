namespace Galvao.Presentation.Requests.Auth;

public record RegisterRequest(
    string Email,
    string Password,
    string DisplayName,
    string FirstName,
    string LastName,
    bool AcceptNews,
    bool AcceptPromo
);
