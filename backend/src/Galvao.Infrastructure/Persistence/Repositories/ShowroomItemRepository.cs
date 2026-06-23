using Galvao.Application.Common.Interfaces;
using Galvao.Domain.ShowroomAggregate.Entities;
using Galvao.Shared;
using Microsoft.EntityFrameworkCore;

namespace Galvao.Infrastructure.Persistence.Repositories;

public class ShowroomItemRepository(GalvaoDbContext context) : BaseEntityRepository<ShowroomItem>(context), IShowroomItemRepository
{
    public override Task<ShowroomItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        DbSet
            .Include(x => x.Photos)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public override async Task<PagedResponse<ShowroomItem>> GetPagedAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var totalCount = await DbSet.CountAsync(cancellationToken);
        var items = await DbSet
            .Include(x => x.Photos)
            .OrderByDescending(item => item.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResponse<ShowroomItem>(items, totalCount, page, pageSize);
    }

    public Task AddPhotoAsync(ShowroomItemPhoto photo, CancellationToken cancellationToken = default) =>
        Context.Set<ShowroomItemPhoto>().AddAsync(photo, cancellationToken).AsTask();
}
