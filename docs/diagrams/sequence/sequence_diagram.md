# Sequence Diagrams

This document contains sequence diagrams showing the runtime interaction of components during core operations.

---

## 1. Member Registration & CRM Sync Flows

To improve readability and usability, the user registration process is divided into three focused sequence flows:
* **1.1. Registration Transaction & Job Enqueueing:** The synchronous HTTP registration phase that sets up the local user profile and queues background tasks.
* **1.2. Email Confirmation Background Job:** Asynchronous execution of sending the verification email.
* **1.3. CRM (Resend) Sync Background Job:** Asynchronous integration that synchronizes the subscriber information with Resend and updates tracking.

---

### 1.1. Registration Transaction & Job Enqueueing

This diagram depicts the synchronous registration transaction, credential setup, local profile staging, token generation, and Hangfire job enqueueing.

```mermaid
sequenceDiagram
    autonumber
    actor User as Client Browser (SPA)
    participant Ctrl as AuthController
    participant Svc as IdentityService
    participant UM as UserManager (Account)
    participant Dom as Member (Entity)
    participant Repo as MemberRepository
    participant HF as Hangfire (IBackgroundJobClient)
    participant UoW as UnitOfWork (EF Context)
    participant DB as MySQL DB

    User->>Ctrl: POST /api/auth/register (RegisterRequest)
    Note over Ctrl: Request has Email, Password,<br/>Name, & Preferences
    Ctrl->>Svc: RegisterAsync(...)
    
    Svc->>UM: FindByEmailAsync(email)
    UM->>DB: Query account by email
    DB-->>UM: Return null (Email is unique)
    UM-->>Svc: Return null
    
    Svc->>Dom: Member.Create(email, name, preferences...)
    Dom-->>Svc: Return Result<Member>

    Svc->>UM: CreateAsync(Account, password)
    UM->>DB: INSERT INTO Accounts
    DB-->>UM: Success
    UM-->>Svc: Return Succeeded

    Svc->>UM: AddToRoleAsync(account, "User")
    UM->>DB: INSERT INTO AccountRoles
    DB-->>UM: Success
    UM-->>Svc: Return Succeeded

    Svc->>Repo: AddAsync(member)
    
    Svc->>UM: GenerateEmailConfirmationTokenAsync(account)
    UM-->>Svc: Return token
    
    Svc->>HF: Enqueue(SendEmailConfirmationJob)
    HF->>DB: INSERT INTO Hangfire.Job (SendEmailConfirmationJob)
    DB-->>HF: Success
    
    alt acceptNews or acceptPromo is true
        Svc->>HF: Enqueue(CrmSyncJob)
        HF->>DB: INSERT INTO Hangfire.Job (CrmSyncJob)
        DB-->>HF: Success
    end

    Svc->>UoW: SaveChangesAsync()
    UoW->>DB: COMMIT TRANSACTION (Inserts Members, etc.)
    DB-->>UoW: Transaction committed
    
    Svc-->>Ctrl: Return Result<Guid> (Member ID)
    Ctrl-->>User: HTTP 200 OK (Guid)
```

**Description:**
1. The SPA client calls `POST /api/auth/register` with signup details.
2. `IdentityService.RegisterAsync` validates email uniqueness and runs domain entity validations (`Member.Create`).
3. An Identity `Account` is registered with the default `"User"` role.
4. The service generates a verification token and schedules a `SendEmailConfirmationJob` with Hangfire.
5. If the user opted into marketing/newsletters, it also schedules a `CrmSyncJob` with Hangfire.
6. The transaction is committed locally, returning the Member ID to the client instantly.

---

### 1.2. Email Confirmation Background Job

This diagram shows how the Hangfire server processes the enqueued email job in the background.

```mermaid
sequenceDiagram
    autonumber
    participant HFS as Hangfire Server (Processor)
    participant DB as MySQL DB
    participant ConfJob as SendEmailConfirmationJob
    participant UM as UserManager (Account)
    participant Resend as ResendEmailSender

    loop Polling
        HFS->>DB: Fetch enqueued jobs
        DB-->>HFS: Return SendEmailConfirmationJob
    end
    
    HFS->>ConfJob: SendConfirmationEmailAsync(userId, link)
    ConfJob->>UM: FindByIdAsync(userId)
    UM->>DB: Query user account
    DB-->>UM: Return account details
    
    ConfJob->>Resend: SendEmailAsync(email, subject, htmlContent)
    Resend-->>ConfJob: Success
    
    ConfJob-->>HFS: Job Complete
    HFS->>DB: Update job state to Succeeded
```

**Description:**
1. A background worker from the Hangfire pool pulls `SendEmailConfirmationJob` from the database queue.
2. The job checks user presence via `UserManager`.
3. It sends a stylized HTML validation email via `ResendEmailSender` and marks the job as successfully completed.

---

### 1.3. CRM (Resend) Sync Background Job

This diagram depicts how the Hangfire server processes the enqueued CRM contact sync to Resend.

```mermaid
sequenceDiagram
    autonumber
    participant HFS as Hangfire Server (Processor)
    participant DB as MySQL DB
    participant SyncJob as CrmSyncJob
    participant Resend as ResendEmailContactService
    participant ContactRepo as MemberContactRepository
    participant UoW as UnitOfWork (EF Context)

    loop Polling
        HFS->>DB: Fetch enqueued jobs
        DB-->>HFS: Return CrmSyncJob
    end
    
    HFS->>SyncJob: SyncContactAsync(userId, email, name, preferences)
    SyncJob->>Resend: CreateContactAsync(email, name, preferences)
    Note over Resend: POST /contacts (creates contact in CRM)
    Resend-->>SyncJob: Return Result<string> (Resend Contact ID)
    
    SyncJob->>ContactRepo: AddAsync(MemberContact)
    Note over ContactRepo: Link Member ID to Resend Contact ID
    
    SyncJob->>UoW: SaveChangesAsync()
    UoW->>DB: COMMIT (INSERT INTO MemberContacts)
    DB-->>UoW: Success
    
    SyncJob-->>HFS: Job Complete
    HFS->>DB: Update job state to Succeeded
```

**Description:**
1. The background worker pulls `CrmSyncJob` from the database queue.
2. The job triggers `ResendEmailContactService.CreateContactAsync` which calls Resend API's `/contacts` endpoint.
3. Upon receiving the external Contact ID from Resend, the job instantiates a `MemberContact` entity.
4. It persists this mapping to the database, completing the task. If any API errors occur, Hangfire automatically retries the task later.

---

## 2. Audited Marketing Preferences Update Sequence Flow

This sequence diagram depicts the detailed step-by-step runtime interaction when a user updates their marketing preferences (`UpdateMarketingPreferencesCommandHandler` in `Galvao.Application`), showing the consent logging and decoupled, error-tolerant downstream sync to Resend.

```mermaid
sequenceDiagram
    autonumber
    actor User as Client Browser (SPA)
    participant Ctrl as MembersController
    participant Hdlr as UpdateMarketingPreferencesCommandHandler
    participant Repo as MemberRepository
    participant Dom as Member (Entity)
    participant LogRepo as ConsentLogRepository
    participant Resend as ResendEmailContactService
    participant ContactRepo as MemberContactRepository
    participant UoW as UnitOfWork (EF Context)
    participant DB as MySQL DB

    User->>Ctrl: PUT /api/members/preferences (UpdateMarketingPreferencesCommand)
    Note over Ctrl: Request contains MemberId, preferences,<br/>IP, source, & consent token
    Ctrl->>Hdlr: HandleAsync(command, cancellationToken)
    
    Hdlr->>Repo: GetByIdAsync(memberId, cancellationToken)
    Repo->>DB: Query member profile by ID
    DB-->>Repo: Return Member record
    Repo-->>Hdlr: Return Member
    
    Hdlr->>Dom: UpdateMarketingPreferences(acceptNews, acceptPromo)
    Note over Dom: Sets AcceptNews/AcceptPromo and updates modified date
    Dom-->>Hdlr: Return Result.Success
    Hdlr->>Repo: Update(member)
    Note over Repo: Marks Member as modified in EF tracker

    Hdlr->>LogRepo: AddAsync(ConsentLog, cancellationToken)
    Note over LogRepo: Creates ConsentLog (Opt-In/Opt-Out) & adds to EF tracker

    rect rgb(240, 240, 240)
        Note over Hdlr: Try Sync Downstream Safely
        alt Any preference accepted (Opt-In)
            alt MemberContact is null or ExternalId is 'DELETED'
                Hdlr->>Resend: CreateContactAsync(email, name, preferences, cancellationToken)
                Resend->>Resend: POST /contacts
                Resend-->>Hdlr: Return external contact ID
                Hdlr->>ContactRepo: AddAsync(MemberContact)
            else MemberContact exists
                Hdlr->>ContactRepo: UpdateStatus(unsubscribed: false)
                Hdlr->>Resend: UpdateContactAsync(externalContactId, name, unsubscribed: false, cancellationToken)
                Resend-->>Hdlr: Return success
            end
        else Both preferences rejected (Opt-Out)
            Hdlr->>Resend: DeleteContactAsync(externalContactId, cancellationToken)
            Resend-->>Hdlr: Return success
            Hdlr->>ContactRepo: UpdateContactDetails('DELETED', email)
            Hdlr->>ContactRepo: UpdateStatus(unsubscribed: true)
        end
    end

    alt Sync succeeds
        Hdlr->>Dom: ClearPendingSync()
        Note over Dom: Sets PendingSync = false
    else Sync fails (Exception or API Error)
        Note over Hdlr: Sync failed! Catch exception.
        Hdlr->>Dom: MarkAsPendingSync()
        Note over Dom: Sets PendingSync = true
        Note over Hdlr: Prints admin console alert
    end

    Hdlr->>UoW: SaveChangesAsync(cancellationToken)
    UoW->>DB: COMMIT TRANSACTION (Saves Member, ConsentLog, and MemberContact updates)
    DB-->>UoW: Transaction committed
    UoW-->>Hdlr: Complete save
    
    Hdlr-->>Ctrl: Return Result.Success
    Ctrl-->>User: HTTP 200 OK
```

### Sequence Flow Description
1. The client browser issues a `PUT /api/members/preferences` with preference flags and auditing information (IP, Source, Consent Token).
2. `MembersController` delegates the command execution to `UpdateMarketingPreferencesCommandHandler.HandleAsync`.
3. The handler queries the `Member` record from the `MemberRepository`.
4. It calls `Member.UpdateMarketingPreferences` to update the flags in the local entity, and updates the repository state.
5. It instantiates a new `ConsentLog` record representing the explicit consent action ("Opt-In" / "Opt-Out") along with IP, Source, and Consent Token, adding it to the `ConsentLogRepository`.
6. **Downstream CRM Synchronization**:
   - The handler runs the sync in a safe wrapper.
   - If opting in, it creates a new contact in Resend (if missing/deleted) or updates the existing one to subscribed.
   - If opting out, it deletes the contact in Resend and marks the tracking record `MemberContact` as unsubscribed/DELETED.
7. **Sync Status Reconciliation**:
   - If the Resend API operations complete without error, the member is marked `PendingSync = false`.
   - If the Resend API throws an error (e.g. timeout, DNS issue, API key error), the exception is caught, the member is marked `PendingSync = true`, and a warning is logged to the server logs.
8. `UnitOfWork.SaveChangesAsync` persists all local changes (Member preferences, ConsentLog ledger, MemberContact tracking, and `PendingSync` state) to the MySQL database in a single transaction.
9. A successful response is returned to the client browser.
