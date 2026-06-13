using Galvao.Domain.Entities;

namespace Galvao.Application.Common.Interfaces;

public interface IConsentLogRepository
{
    Task AddAsync(ConsentLog consentLog, CancellationToken cancellationToken = default);
}
