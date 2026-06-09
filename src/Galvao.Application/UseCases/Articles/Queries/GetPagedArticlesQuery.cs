using Galvao.Application.Common.CQRS;
using Galvao.Shared;

namespace Galvao.Application.UseCases.Articles.Queries;

public record GetPagedArticlesQuery(int Page, int PageSize, bool OnlyPublished = true) : IQuery<PagedResponse<ArticleResponse>>;
