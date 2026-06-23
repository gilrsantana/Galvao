using Galvao.Application.Common.Interfaces;
using Galvao.Domain.MemberUserAggregate.Entities;
using Microsoft.EntityFrameworkCore;

namespace Galvao.Infrastructure.Persistence.Repositories;

public class MemberRepository(GalvaoDbContext context) : BaseEntityRepository<Member>(context), IMemberRepository
{
    public Task<Member?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        DbSet.FirstOrDefaultAsync(m => m.Email == email, cancellationToken);

    public void Remove(Member member)
    {
        DbSet.Remove(member);
    }
}
