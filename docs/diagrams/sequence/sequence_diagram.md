# Sequence Diagrams

This document contains sequence diagrams showing the runtime interaction of components during core operations.

---

## 1. Member Registration & CRM Sync Sequence Flow

This sequence diagram depicts the detailed step-by-step process of user registration (`RegisterAsync` in `IdentityService`), including credential setup, validation, profile saving, and Resend integration.

```mermaid
sequenceDiagram
    autonumber
    actor User as Client Browser (SPA)
    participant Ctrl as AuthController
    participant Svc as IdentityService
    participant UM as UserManager (Account)
    participant Dom as Member (Entity)
    participant Repo as MemberRepository
    participant Resend as ResendEmailContactService
    participant ContactRepo as MemberContactRepository
    participant UoW as UnitOfWork (EF Context)
    participant DB as MySQL DB

    User->>Ctrl: POST /api/auth/register (RegisterRequest)
    Note over Ctrl: RegisterRequest contains email, password,<br/>name, & newsletter choices
    Ctrl->>Svc: RegisterAsync(email, password, name, preferences...)
    
    Svc->>UM: FindByEmailAsync(email)
    UM->>DB: Query account by email
    DB-->>UM: Return null (Email is unique)
    UM-->>Svc: Return null
    
    Svc->>Dom: Member.Create(email, name, preferences...)
    Note over Dom: Runs business rules & validations
    Dom-->>Svc: Return Result<Member> (Success, has GUID)

    Svc->>UM: CreateAsync(Account.Create(member.Id, email), password)
    Note over UM: Hashes password & runs complexity rules
    UM->>DB: INSERT INTO Accounts (Identity schema)
    DB-->>UM: Success
    UM-->>Svc: Return IdentityResult (Succeeded)

    Svc->>UM: AddToRoleAsync(account, "User")
    UM->>DB: INSERT INTO AccountRoles
    DB-->>UM: Success
    UM-->>Svc: Return IdentityResult (Succeeded)

    Svc->>Repo: AddAsync(member)
    Note over Repo: Adds Member profile state to EF tracker
    
    alt User accepted newsletters (AcceptNews or AcceptPromo is true)
        Svc->>Resend: CreateContactAsync(email, name, preferences)
        Note over Resend: Fetches/Creates newsletter marketing segments
        Resend->>Resend: GetOrCreateSegmentAsync(segmentName)
        Resend->>Resend: POST /contacts
        Resend-->>Svc: Return Result<string> (Resend Contact ID)
        
        Svc->>ContactRepo: AddAsync(MemberContact)
        Note over ContactRepo: Link Member ID to Resend Contact ID
    end

    Svc->>UoW: SaveChangesAsync()
    UoW->>DB: COMMIT TRANSACTION (Inserts Members, MemberContacts, etc.)
    DB-->>UoW: Transaction committed
    UoW-->>Svc: Complete save
    
    Svc-->>Ctrl: Return Result<Guid> (Member ID)
    Ctrl-->>User: HTTP 200 OK (Guid)
```

### Sequence Flow Description
1. The client browser issues a `POST /api/auth/register` with JSON body payload containing member details.
2. `AuthController` receives the DTO request and calls `RegisterAsync` on `IdentityService`.
3. `IdentityService` checks for email duplicate in ASP.NET Core Identity.
4. If unique, it calls the `Member.Create` static domain factory to validate name fields and preferences.
5. It then attempts to register credentials using the ASP.NET Core Identity `UserManager`. The system hashes passwords and updates ASP.NET Core Identity database tables.
6. The created user is assigned the default `"User"` role.
7. The local profile (`Member`) is queued for database insertion via `IMemberRepository.AddAsync`.
8. **Conditional CRM Sync**: If newsletter/marketing preferences are active, `ResendEmailContactService` sends an HTTP POST request to the external Resend API, returning the CRM's external contact identifier.
9. A `MemberContact` tracking record (linking the local account ID to the Resend ID) is queued in EF Core.
10. `UnitOfWork.SaveChangesAsync` is called, executing a MySQL write transaction, persisting both the user profile and the external sync tracking details.
11. A successful HTTP response is returned to the frontend.

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
