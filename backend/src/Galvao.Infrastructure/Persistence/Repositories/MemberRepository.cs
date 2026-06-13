using Galvao.Domain.Entities;
using Galvao.Application.Common.Interfaces;
using Galvao.Shared;
using Microsoft.EntityFrameworkCore;

namespace Galvao.Infrastructure.Persistence.Repositories;

public class MemberRepository : BaseEntityRepository<Member>, IMemberRepository
{
    public MemberRepository(GalvaoDbContext context) : base(context)
    {
    }

    public async Task<Member?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        await DbSet.FirstOrDefaultAsync(m => m.Email == email, cancellationToken);

    public void Remove(Member member)
    {
        DbSet.Remove(member);
    }
}
