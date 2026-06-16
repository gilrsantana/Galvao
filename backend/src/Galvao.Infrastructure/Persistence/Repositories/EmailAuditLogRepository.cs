using Galvao.Domain.Entities;
using Galvao.Application.Common.Interfaces;

namespace Galvao.Infrastructure.Persistence.Repositories;

public class EmailAuditLogRepository : IEmailAuditLogRepository
{
    private readonly GalvaoDbContext _context;

    public EmailAuditLogRepository(GalvaoDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(EmailAuditLog log, CancellationToken cancellationToken = default)
    {
        await _context.EmailAuditLogs.AddAsync(log, cancellationToken);
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }
}
