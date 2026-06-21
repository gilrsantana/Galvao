using Galvao.Application.Common.Interfaces;
using Galvao.Domain.Base;
using Galvao.Shared;
using Microsoft.EntityFrameworkCore;

namespace Galvao.Infrastructure.Persistence.Repositories;

public abstract class BaseEntityRepository<TEntity> : IUnitOfWork
    where TEntity : BaseEntity
{
    protected readonly GalvaoDbContext Context;
    protected readonly DbSet<TEntity> DbSet;

    protected BaseEntityRepository(GalvaoDbContext context)
    {
        Context = context;
        DbSet = context.Set<TEntity>();
    }

    public virtual async Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await DbSet.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default) =>
        await DbSet.AddAsync(entity, cancellationToken);

    public void Update(TEntity entity)
    {
        entity.Update(); // Updates timestamp
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

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        await Context.SaveChangesAsync(cancellationToken);
}
