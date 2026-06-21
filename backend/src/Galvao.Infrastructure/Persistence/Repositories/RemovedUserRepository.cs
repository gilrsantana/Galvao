using Galvao.Application.Common.Interfaces;
using Galvao.Domain.MemberUserAggregate.Entities;

namespace Galvao.Infrastructure.Persistence.Repositories;

public class RemovedUserRepository : BaseEntityRepository<RemovedUser>, IRemovedUserRepository
{
    public RemovedUserRepository(GalvaoDbContext context) : base(context)
    {
    }
}
