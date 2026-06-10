using Galvao.Application.Common.CQRS;
using Galvao.Application.Common.Interfaces;
using Galvao.Application.UseCases.Articles.Queries;
using Galvao.Shared;

namespace Galvao.Application.UseCases.Articles.QueryHandlers;

public class GetArticleByIdQueryHandler : IQueryHandler<GetArticleByIdQuery, ArticleResponse>
{
    private readonly IArticleRepository _articleRepository;

    public GetArticleByIdQueryHandler(IArticleRepository articleRepository)
    {
        _articleRepository = articleRepository;
    }

    public async Task<Result<ArticleResponse>> HandleAsync(GetArticleByIdQuery query, CancellationToken cancellationToken = default)
    {
        var article = await _articleRepository.GetByIdAsync(query.ArticleId, cancellationToken);
        if (article is null)
        {
            return Result.Failure<ArticleResponse>(new Error("Article.NotFound", $"Article with ID '{query.ArticleId}' was not found."));
        }

        var response = new ArticleResponse(
            article.Id,
            article.Title,
            article.Content,
            article.Author,
            article.IsPublished,
            article.PublishedAt,
            article.Active,
            article.CreatedAt,
            article.UpdatedAt);

        return response;
    }
}
