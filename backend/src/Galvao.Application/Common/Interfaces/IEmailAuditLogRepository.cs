using Galvao.Domain.Entities;

namespace Galvao.Application.Common.Interfaces;

public interface IEmailAuditLogRepository : IUnitOfWork
{
    Task AddAsync(EmailAuditLog log, CancellationToken cancellationToken = default);
}
