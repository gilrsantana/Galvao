using Galvao.Application.Common.CQRS;
using Galvao.Application.Common.Interfaces;
using Galvao.Application.UseCases.Articles.Queries;
using Galvao.Domain.ArticleAggregate.Entities;
using Galvao.Shared;

namespace Galvao.Application.UseCases.Articles.QueryHandlers;

public class GetPagedArticlesQueryHandler : IQueryHandler<GetPagedArticlesQuery, PagedResponse<ArticleResponse>>
{
    private readonly IArticleRepository _articleRepository;

    public GetPagedArticlesQueryHandler(IArticleRepository articleRepository)
    {
        _articleRepository = articleRepository;
    }

    public async Task<Result<PagedResponse<ArticleResponse>>> HandleAsync(GetPagedArticlesQuery query, CancellationToken cancellationToken = default)
    {
        PagedResponse<Article> pagedArticles;

        if (query.OnlyPublished)
        {
            pagedArticles = await _articleRepository.GetPagedPublishedAsync(query.Page, query.PageSize, cancellationToken);
        }
        else
        {
            pagedArticles = await _articleRepository.GetPagedAsync(query.Page, query.PageSize, cancellationToken);
        }

        var mappedArticles = pagedArticles.Items.Select(article => new ArticleResponse(
            article.Id,
            article.Title,
            article.Content,
            article.Author,
            article.Slug,
            article.IsPublished,
            article.PublishedAt,
            article.Active,
            article.CreatedAt,
            article.UpdatedAt
        )).ToList();

        var response = new PagedResponse<ArticleResponse>(
            mappedArticles,
            pagedArticles.TotalCount,
            pagedArticles.PageNumber,
            pagedArticles.PageSize);

        return response;
    }
}
