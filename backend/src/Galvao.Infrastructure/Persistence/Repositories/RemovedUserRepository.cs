using Galvao.Domain.Entities;
using Galvao.Application.Common.Interfaces;

namespace Galvao.Infrastructure.Persistence.Repositories;

public class RemovedUserRepository : BaseEntityRepository<RemovedUser>, IRemovedUserRepository
{
    public RemovedUserRepository(GalvaoDbContext context) : base(context)
    {
    }
}
