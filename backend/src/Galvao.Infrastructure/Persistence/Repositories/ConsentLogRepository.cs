using Galvao.Application.Common.Interfaces;
using Galvao.Domain.MemberContactAggregate.Entities;

namespace Galvao.Infrastructure.Persistence.Repositories;

public class ConsentLogRepository(GalvaoDbContext context) : BaseEntityRepository<ConsentLog>(context), IConsentLogRepository
{
}
