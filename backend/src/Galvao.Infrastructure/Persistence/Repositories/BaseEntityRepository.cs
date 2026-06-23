using Galvao.Application.Common.Interfaces;
using Galvao.Application.Common.Models;
using Galvao.Domain.Base;
using Galvao.Shared;
using Galvao.Infrastructure.Persistence.Helpers;
using Microsoft.EntityFrameworkCore;

namespace Galvao.Infrastructure.Persistence.Repositories;

public abstract class BaseEntityRepository<TEntity>(GalvaoDbContext context) : IBaseEntityRepository<TEntity>
    where TEntity : BaseEntity
{
    protected readonly GalvaoDbContext Context = context;
    protected readonly DbSet<TEntity> DbSet = context.Set<TEntity>();

    public virtual Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        DbSet.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task AddAsync(TEntity entity, CancellationToken cancellationToken = default) =>
        DbSet.AddAsync(entity, cancellationToken).AsTask();

    public void Update(TEntity entity)
    {
        entity.Update();
        DbSet.Update(entity);
    }

    public virtual async Task<PagedResponse<TEntity>> GetPagedAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var totalCount = await DbSet.CountAsync(cancellationToken);
        var items = await DbSet
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResponse<TEntity>(items, totalCount, page, pageSize);
    }

    public void Activate(TEntity entity)
    {
        entity.Activate();
        Update(entity);
    }

    public void UnActivate(TEntity entity)
    {
        entity.UnActivate();
        Update(entity);
    }

    public async Task<PagedResponse<TEntity>> AdvancedQueryAsync(
        AdvancedQuery<TEntity> queryRequest,
        CancellationToken cancellationToken = default)
    {
        IQueryable<TEntity> query = DbSet;

        if (queryRequest.NoTracking)
        {
            query = query.AsNoTracking();
        }

        if (queryRequest.Includes != null)
        {
            foreach (var include in queryRequest.Includes)
            {
                query = query.Include(include);
            }
        }

        var predicate = ExpressionBuilder.BuildPredicate<TEntity>(queryRequest.Filters);
        if (predicate != null)
        {
            query = query.Where(predicate);
        }

        var orderedQuery = ExpressionBuilder.ApplyOrdering(query, queryRequest.Ordering);

        var totalCount = await orderedQuery.CountAsync(cancellationToken);

        var items = await orderedQuery
            .Skip(queryRequest.Skip)
            .Take(queryRequest.Take)
            .ToListAsync(cancellationToken);

        int pageNumber = queryRequest.Take > 0 ? (queryRequest.Skip / queryRequest.Take) + 1 : 1;
        return new PagedResponse<TEntity>(items, totalCount, pageNumber, queryRequest.Take);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        Context.SaveChangesAsync(cancellationToken);
}
