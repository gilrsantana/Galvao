using Galvao.Application.Common.CQRS;
using Galvao.Application.Common.Interfaces;
using Galvao.Application.UseCases.Articles.Queries;
using Galvao.Shared;

namespace Galvao.Application.UseCases.Articles.QueryHandlers;

public class GetArticleBySlugQueryHandler : IQueryHandler<GetArticleBySlugQuery, ArticleResponse>
{
    private readonly IArticleRepository _articleRepository;

    public GetArticleBySlugQueryHandler(IArticleRepository articleRepository)
    {
        _articleRepository = articleRepository;
    }

    public async Task<Result<ArticleResponse>> HandleAsync(GetArticleBySlugQuery query, CancellationToken cancellationToken = default)
    {
        var article = await _articleRepository.GetBySlugAsync(query.Slug, cancellationToken);
        if (article is null)
        {
            return Result.Failure<ArticleResponse>(new Error("Article.NotFound", $"Article with Slug '{query.Slug}' was not found."));
        }

        var response = new ArticleResponse(
            article.Id,
            article.Title,
            article.Content,
            article.Author,
            article.Slug,
            article.IsPublished,
            article.PublishedAt,
            article.Active,
            article.CreatedAt,
            article.UpdatedAt);

        return response;
    }
}
