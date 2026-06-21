using Galvao.Domain.ArticleAggregate.Entities;
using Galvao.Shared;

namespace Galvao.Application.Common.Interfaces;

public interface IArticleRepository
{
    Task<Article?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Article?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task AddAsync(Article article, CancellationToken cancellationToken = default);
    void Update(Article article);
    Task<PagedResponse<Article>> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<PagedResponse<Article>> GetPagedPublishedAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    void Activate(Article article);
    void UnActivate(Article article);
}
