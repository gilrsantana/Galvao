using Galvao.Application.Common.Interfaces;
using Galvao.Domain.MemberContactAggregate.Entities;

namespace Galvao.Infrastructure.Persistence.Repositories;

public class EmailAuditLogRepository(GalvaoDbContext context) : IEmailAuditLogRepository
{
    public Task AddAsync(EmailAuditLog log, CancellationToken cancellationToken = default) =>
        context.EmailAuditLogs.AddAsync(log, cancellationToken).AsTask();

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        context.SaveChangesAsync(cancellationToken);
}
