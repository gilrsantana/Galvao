using Galvao.Domain.MemberUserAggregate.Entities;

namespace Galvao.Application.Common.Interfaces;

public interface IRemovedUserRepository
{
    Task AddAsync(RemovedUser removedUser, CancellationToken cancellationToken = default);
}
