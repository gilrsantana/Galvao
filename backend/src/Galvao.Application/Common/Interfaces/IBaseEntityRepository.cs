using Galvao.Domain.Base;
using Galvao.Shared;
using Galvao.Application.Common.Models;

namespace Galvao.Application.Common.Interfaces;

public interface IBaseEntityRepository<TEntity> : IUnitOfWork where TEntity : BaseEntity
{
    Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);
    void Update(TEntity entity);
    Task<PagedResponse<TEntity>> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    void Activate(TEntity entity);
    void UnActivate(TEntity entity);
    Task<PagedResponse<TEntity>> AdvancedQueryAsync(AdvancedQuery<TEntity> queryRequest, CancellationToken cancellationToken = default);
}
