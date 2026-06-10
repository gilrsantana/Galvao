using Galvao.Domain.Entities;
using Galvao.Shared;

namespace Galvao.Application.Common.Interfaces;

public interface IMemberRepository
{
    Task<Member?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Member?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task AddAsync(Member member, CancellationToken cancellationToken = default);
    void Update(Member member);
    Task<PagedResponse<Member>> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    void Activate(Member member);
    void UnActivate(Member member);
}
