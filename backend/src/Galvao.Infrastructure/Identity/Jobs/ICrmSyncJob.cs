using System;
using System.Threading;
using System.Threading.Tasks;

namespace Galvao.Infrastructure.Identity.Jobs;

public interface ICrmSyncJob
{
    Task SyncContactAsync(
        Guid userId,
        string email,
        string firstName,
        string lastName,
        bool acceptNews,
        bool acceptPromo,
        CancellationToken cancellationToken = default);
}
