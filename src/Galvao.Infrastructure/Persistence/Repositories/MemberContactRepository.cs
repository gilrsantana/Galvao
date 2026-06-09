using Galvao.Domain.Entities;
using Galvao.Application.Common.Interfaces;

namespace Galvao.Infrastructure.Persistence.Repositories;

public class MemberContactRepository : BaseEntityRepository<MemberContact>, IMemberContactRepository
{
    public MemberContactRepository(GalvaoDbContext context) : base(context)
    {
    }
}
