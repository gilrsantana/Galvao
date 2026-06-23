using Galvao.Domain.ArticleAggregate.Entities;
using Galvao.Shared;

namespace Galvao.Application.Common.Interfaces;

public interface IArticleRepository : IBaseEntityRepository<Article>
{
    Task<Article?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<PagedResponse<Article>> GetPagedPublishedAsync(int page, int pageSize, CancellationToken cancellationToken = default);
}
