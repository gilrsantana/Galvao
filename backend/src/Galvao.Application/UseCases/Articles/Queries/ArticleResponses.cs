namespace Galvao.Application.UseCases.Articles.Queries;

public record ArticleResponse(
    Guid Id,
    string Title,
    string Content,
    string Author,
    string Slug,
    bool IsPublished,
    DateTime? PublishedAt,
    bool Active,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
