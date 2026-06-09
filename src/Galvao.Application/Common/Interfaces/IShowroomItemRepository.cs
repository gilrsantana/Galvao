using Galvao.Domain.Entities;
using Galvao.Shared;

namespace Galvao.Application.Common.Interfaces;

public interface IShowroomItemRepository
{
    Task<ShowroomItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(ShowroomItem item, CancellationToken cancellationToken = default);
    void Update(ShowroomItem item);
    Task<PagedResponse<ShowroomItem>> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    void Activate(ShowroomItem item);
    void UnActivate(ShowroomItem item);
    Task AddPhotoAsync(ShowroomItemPhoto photo, CancellationToken cancellationToken = default);
}
