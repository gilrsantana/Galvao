using System.Linq.Expressions;
using Galvao.Application.Common.CQRS;
using Galvao.Application.Common.Interfaces;
using Galvao.Application.Common.Models;
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
        var advancedQuery = new AdvancedQuery<Article>
        {
            Skip = (query.Page - 1) * query.PageSize,
            Take = query.PageSize,
            NoTracking = true
        };

        if (query.OnlyPublished)
        {
            advancedQuery.Filters.Add(new FilterItem { PropertyName = "IsPublished", Operation = FilterOption.Equal, Value = true });
            advancedQuery.Filters.Add(new FilterItem { PropertyName = "Active", Operation = FilterOption.Equal, Value = true });
            advancedQuery.Ordering.Add(new OrderingItem { Field = "PublishedAt", Direction = SortingDirection.Descending });
        }
        else
        {
            advancedQuery.Ordering.Add(new OrderingItem { Field = "CreatedAt", Direction = SortingDirection.Descending });
        }

        var pagedArticles = await _articleRepository.AdvancedQueryAsync(advancedQuery, cancellationToken);

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
