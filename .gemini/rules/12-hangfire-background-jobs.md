# Rule: Hangfire Background Processing and Orchestration

## Metadata
- **ID**: RULE-012-HANGFIRE-BACKGROUND-JOBS
- **Scope**: Application & Infrastructure Layers
- **Target Types**: Job Definitions, Job Schedulers
- **Status**: Active

## Overview
This rule outlines the patterns for background job scheduling, execution, and organization using **Hangfire** in the backend application. It ensures background processing is clean, transactional, and runs safely without blocking HTTP request pipelines.

---

## 1. Job Organization & Namespace
All background job logic must be placed inside the Application project:
- **Location**: `src/Galvao.Application/ApplicationJobs/`
- **Namespace**: `Galvao.Application.ApplicationJobs`
- **Class Naming**: Use the suffix `Job` (e.g., `EmailSyncJob.cs`, `CleanConsentLogsJob.cs`).

---

## 2. Job Class Patterns & Constructor Injection
- Jobs are standard classes. They must not inherit from base classes unless explicitly required by a framework extension.
- **Dependencies**: Inject repositories, services, and `IUnitOfWork` using the standard constructor injection pattern. Do not reference `GalvaoDbContext` directly.
- **Method Execution**: The executing method must be asynchronous, return `Task`, and accept a `CancellationToken`.

### Example Job Definition:
```csharp
using Galvao.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace Galvao.Application.ApplicationJobs;

public class EmailSyncJob
{
    private readonly IEmailAuditLogRepository _auditLogRepository;
    private readonly ILogger<EmailSyncJob> _logger;

    public EmailSyncJob(
        IEmailAuditLogRepository auditLogRepository,
        ILogger<EmailSyncJob> logger)
    {
        _auditLogRepository = auditLogRepository;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting email sync process...");
        
        // Execute operational checks...
        var count = await _auditLogRepository.GetPendingSyncCountAsync(cancellationToken);
        _logger.LogInformation("Processed {Count} pending emails", count);
    }
}
```

---

## 3. Job Registration & Scheduling
- **Composition Root**: Register the job class as `Transient` or `Scoped` inside [DependencyInjection.cs](file:///home/gilmar/Development/ai-driven-development/projects/galvao/backend/src/Galvao.Presentation/Configurations/DependencyInjection.cs).
- **Scheduling Invocation**:
  - For **One-Time/Enqueue** tasks: Use `IBackgroundJobClient` inside Use Case handlers.
  - For **Recurring** tasks: Use `IRecurringJobManager` during system startup in the Presentation composition root.
  - Standard recurring job configuration must occur inside `Configurations/DependencyInjection.cs` during application pipeline initialization:
    ```csharp
    public static void ConfigureRecurringJobs(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var manager = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();
        
        manager.AddOrUpdate<EmailSyncJob>(
            "email-synchronization",
            job => job.ExecuteAsync(CancellationToken.None),
            Cron.Minutely());
    }
    ```

---

## 4. Operational Invariant Rules
1. **Idempotency**: All jobs must be designed to be idempotent. In the event of network timeouts, Hangfire may retry the execution.
2. **Cancellation Handling**: Always propagate `CancellationToken` to downstream repository queries to ensure resources are released immediately if a job is cancelled/killed via the Hangfire Dashboard.
3. **Transaction Scope**: Explicitly call `SaveChangesAsync()` inside the job methods using `IUnitOfWork` to save state changes, rather than assuming automatic persistence.
