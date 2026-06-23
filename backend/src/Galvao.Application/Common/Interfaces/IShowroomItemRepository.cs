using Galvao.Domain.ShowroomAggregate.Entities;
using Galvao.Shared;

namespace Galvao.Application.Common.Interfaces;

public interface IShowroomItemRepository : IBaseEntityRepository<ShowroomItem>
{
    Task AddPhotoAsync(ShowroomItemPhoto photo, CancellationToken cancellationToken = default);
}
