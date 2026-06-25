---
type: business_rule
title: Resend CRM Synchronization and Fallback Rules
description: Detailed behavior of the system's background retry and inline fallback rules for downstream Resend CRM integration.
timestamp: 2026-06-25T04:02:00-03:00
tags: [crm, synchronization, fallback, resend, hangfire]
---

# Resend CRM Synchronization and Fallback Rules

The Galvão system integrates with Resend CRM to manage email marketing contacts and segments. Since external API calls can fail due to network timeouts, rate-limiting, or service outages, the system implements two distinct strategies to handle errors based on whether the action is synchronous (inline HTTP request) or asynchronous (background queue).

---

## 1. Flow A: Registration Synchronization (Background Retry)

When a new member registers and chooses to opt-in to marketing communications, the CRM contact creation is offloaded to a background worker to ensure fast response times for the user.

```text
[ User Registration ] 
       │
       ▼ (Database Commit)
[ Enqueue ICrmSyncJob ] ──> (Hangfire Worker Process)
                                   │
                                   ▼ (Calls Resend API)
                         { Resend API Successful? }
                          /                      \
                    (Yes) /                      \ (No)
                         ▼                        ▼
               [ Save Contact Id ]       [ Throw Exception ]
               [ Commit local DB ]                │
                                                  ▼
                                       (Hangfire Retry Loop:
                                     exp-backoff default retries)
```

### Mechanism Details
*   **Job Invocator**: Enqueued in `RegisterMemberCommandHandler` using `IBackgroundJobService.Enqueue<ICrmSyncJob>`.
*   **Handler**: `CrmSyncJob.SyncContactAsync(...)`.
*   **Failure Rule**: If `CreateContactAsync` returns a failure result (e.g. status code 500 or timeout), the job throws an `InvalidOperationException`:
    ```csharp
    throw new InvalidOperationException($"Resend API call failed: {resendResult.Error.Message}");
    ```
*   **Fallback / Recovery**:
    *   By throwing an exception, the job is marked as **Failed** in Hangfire.
    *   Hangfire's automatic retry filter intercepts the exception and schedules the job for a retry using an **exponential backoff** algorithm.
    *   This prevents temporary outages or API throttling from losing user contact synchronizations.

---

## 2. Flow B: Preference Updates (Inline Fallback)

When an existing member updates their marketing options via the user profile page, the update is handled synchronously. To prevent a failure in the external CRM service from blocking the user from saving their local profile settings, an inline fallback mechanism is employed.

```text
[ PUT /api/members/{id}/preferences ]
                  │
                  ▼
   [ Save Local Preferences & Log Consent ]
                  │
                  ▼
         [ Call Resend CRM API ]
                  │
        { Resend API Success? }
         /                   \
   (Yes) /                   \ (No)
        ▼                     ▼
[ Clear PendingSync ]  [ Mark As PendingSync = true ]
                       [ Log Console Admin Alert ]
        │                     │
        └──────────┬──────────┘
                   │
                   ▼
       [ Commit DB Save Changes ]
```

### Mechanism Details
*   **Handler**: `UpdateMarketingPreferencesCommandHandler.HandleAsync(...)`.
*   **Failure Rule**: The CRM sync step is wrapped in a `try-catch` block inside `SyncDownstreamSafelyAsync`:
    ```csharp
    try
    {
        var syncResult = await SyncContactPreferencesAsync(member, command, cancellationToken);
        if (syncResult.IsFailure)
        {
            syncFailed = true;
            syncError = syncResult;
        }
    }
    catch (Exception ex)
    {
        syncFailed = true;
        syncError = Result.Failure(new Error("EmailContact.SyncException", ex.Message));
    }
    ```
*   **Fallback / Recovery**:
    1.  **Mark Out-Of-Sync**: If `syncFailed` is true, the `Member` aggregate state is flagged with `PendingSync = true` (`member.MarkAsPendingSync()`).
    2.  **Console Alert**: Writes an administrator alert to the stderr log:
        ```text
        [ALERT] Admin alert: Downstream marketing provider sync failed for Member ID '{id}'. Logged as 'Pending Sync'.
        ```
    3.  **Local Data Integrity**: If synchronization succeeds, the flag is cleared (`member.ClearPendingSync()`).
    4.  **Save Local State**: Regardless of the sync status, the handler calls `_unitOfWork.SaveChangesAsync()` to persist the local changes (the updated profile and the consent logs). The API request returns a `200 OK` response to the user.

---

## 3. Reconciliation and Audit

*   **Audit Ledger**: The presence of `PendingSync = true` in the `Members` database table allows administrators to run periodic queries to identify unsynced profiles:
    ```sql
    SELECT Id, Email, FirstName, LastName, AcceptNews, AcceptPromo 
    FROM Members 
    WHERE PendingSync = 1;
    ```
*   **Reconciliation Job (Future Work)**: A recurring cron job can be registered to query all members with `PendingSync = true` and retry their synchronization in the background. Once successfully synced, `ClearPendingSync()` is invoked and saved to the database.
