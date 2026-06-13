using Galvao.Domain.Entities;

namespace Galvao.Application.Common.Interfaces;

public interface IMemberContactRepository
{
    Task<MemberContact?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(MemberContact memberContact, CancellationToken cancellationToken = default);
    void Update(MemberContact memberContact);
    void Remove(MemberContact memberContact);
}
