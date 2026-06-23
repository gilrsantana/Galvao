using Galvao.Application.Common.Interfaces;
using Galvao.Domain.ArticleAggregate.Entities;
using Galvao.Shared;
using Microsoft.EntityFrameworkCore;

namespace Galvao.Infrastructure.Persistence.Repositories;

public class ArticleRepository(GalvaoDbContext context) : BaseEntityRepository<Article>(context), IArticleRepository
{

    public async Task<PagedResponse<Article>> GetPagedPublishedAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.Where(x => x.IsPublished && x.Active);
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(x => x.PublishedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResponse<Article>(items, totalCount, page, pageSize);
    }

    public Task<Article?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default) =>
        DbSet.FirstOrDefaultAsync(x => x.Slug == slug, cancellationToken);
}
