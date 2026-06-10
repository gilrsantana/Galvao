namespace Galvao.Presentation.Requests.Showroom;

public record UpdateShowroomItemRequest(
    string Title,
    string Description,
    decimal Price,
    string Category);
