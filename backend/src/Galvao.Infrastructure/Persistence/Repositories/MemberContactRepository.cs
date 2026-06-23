using Galvao.Application.Common.Interfaces;
using Galvao.Domain.MemberContactAggregate.Entities;

namespace Galvao.Infrastructure.Persistence.Repositories;

public class MemberContactRepository(GalvaoDbContext context) : BaseEntityRepository<MemberContact>(context), IMemberContactRepository
{
    public void Remove(MemberContact memberContact)
    {
        DbSet.Remove(memberContact);
    }
}
