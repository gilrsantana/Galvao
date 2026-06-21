using Galvao.Application.Common.Interfaces;
using Galvao.Domain.MemberContactAggregate.Entities;

namespace Galvao.Infrastructure.Persistence.Repositories;

public class ConsentLogRepository : BaseEntityRepository<ConsentLog>, IConsentLogRepository
{
    public ConsentLogRepository(GalvaoDbContext context) : base(context)
    {
    }
}
