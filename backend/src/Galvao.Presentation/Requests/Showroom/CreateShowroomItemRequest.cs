namespace Galvao.Presentation.Requests.Showroom;

public record CreateShowroomItemRequest(
    string Title,
    string Description,
    decimal Price,
    string Category);
