using Galvao.Domain.MemberUserAggregate.Entities;
using Galvao.Shared;

namespace Galvao.Application.Common.Interfaces;

public interface IMemberRepository : IBaseEntityRepository<Member>
{
    Task<Member?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    void Remove(Member member);
}
