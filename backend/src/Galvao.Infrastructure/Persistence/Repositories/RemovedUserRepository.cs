using Galvao.Application.Common.Interfaces;
using Galvao.Domain.MemberUserAggregate.Entities;

namespace Galvao.Infrastructure.Persistence.Repositories;

public class RemovedUserRepository(GalvaoDbContext context) : BaseEntityRepository<RemovedUser>(context), IRemovedUserRepository
{
}
