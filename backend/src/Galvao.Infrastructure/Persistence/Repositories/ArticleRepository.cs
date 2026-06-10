using Galvao.Application.Common.Interfaces;
using Galvao.Domain.Entities;
using Galvao.Shared;
using Microsoft.EntityFrameworkCore;

namespace Galvao.Infrastructure.Persistence.Repositories;

public class ArticleRepository : BaseEntityRepository<Article>, IArticleRepository
{
    public ArticleRepository(GalvaoDbContext context) : base(context)
    {
    }

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
}
