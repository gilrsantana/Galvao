using Galvao.Domain.Entities;
using Galvao.Application.Common.Interfaces;

namespace Galvao.Infrastructure.Persistence.Repositories;

public class ConsentLogRepository : BaseEntityRepository<ConsentLog>, IConsentLogRepository
{
    public ConsentLogRepository(GalvaoDbContext context) : base(context)
    {
    }
}
