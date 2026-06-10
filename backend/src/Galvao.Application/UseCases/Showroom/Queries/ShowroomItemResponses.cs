namespace Galvao.Application.UseCases.Showroom.Queries;

public record ShowroomItemPhotoResponse(
    Guid Id,
    string Url,
    string Caption,
    bool IsPrimary);

public record ShowroomItemResponse(
    Guid Id,
    string Title,
    string Description,
    decimal Price,
    string Category,
    bool Active,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    List<ShowroomItemPhotoResponse> Photos);
