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
