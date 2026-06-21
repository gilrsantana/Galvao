using Galvao.Application.Common.Interfaces;
using Galvao.Domain.MemberContactAggregate.Entities;

namespace Galvao.Infrastructure.Persistence.Repositories;

public class MemberContactRepository : BaseEntityRepository<MemberContact>, IMemberContactRepository
{
    public MemberContactRepository(GalvaoDbContext context) : base(context)
    {
    }

    public void Remove(MemberContact memberContact)
    {
        DbSet.Remove(memberContact);
    }
}
