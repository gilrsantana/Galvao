---
type: architecture_document
title: CRM Sync Activity workflow Diagram
description: Activity diagram showcasing decisions for member registration and Resend CRM sync queue.
timestamp: 2026-06-24T07:23:16-03:00
tags: [sync, diagrams, activity-diagram, uml, crm, mermaid]
---

# Activity Diagrams

This document details the operational workflows of the Galvão system.

---

## 1. Member Registration & CRM Sync Workflow

This diagram outlines the step-by-step logic executed when a new user registers an account, which involves credentials validation, local profile persistence, and conditional sync to the Resend email marketing platform.

```mermaid
stateDiagram-v2
    [*] --> SubmitForm : User submits Registration Form
    SubmitForm --> ValidateInput : Validate email, password, and profiles
    
    state ValidateInput {
        [*] --> CheckFields
        CheckFields --> FieldsInvalid : Missing / malformed info
        CheckFields --> FieldsValid : Valid info
    }

    FieldsInvalid --> AbortRegistration : Show Validation Errors
    FieldsValid --> CheckEmailUniqueness : Query MySQL for existing email

    CheckEmailUniqueness --> EmailExists : Email already taken
    CheckEmailUniqueness --> EmailUnique : Email is unique

    EmailExists --> AbortRegistration : Return Auth.EmailNotUnique error
    EmailUnique --> CreateIdentityAccount : Invoke UserManager.CreateAsync(account, password)

    CreateIdentityAccount --> IdentityFailed : Password weak or identity error
    CreateIdentityAccount --> IdentitySuccess : Account created in DB context

    IdentityFailed --> AbortRegistration : Return failure with details
    IdentitySuccess --> AddUserRole : Assign "User" role to account

    AddUserRole --> AddMemberProfile : Instantiate Member and add to Repository
    AddMemberProfile --> CheckMarketingConsent : Check AcceptNews OR AcceptPromo

    CheckMarketingConsent --> NoConsent : Both false
    CheckMarketingConsent --> ConsentGiven : At least one is true

    NoConsent --> SaveChanges : Proceed directly to DbContext save
    
    ConsentGiven --> CallResendAPI : Create Contact via ResendEmailContactService
    CallResendAPI --> ResendFailure : API Error / Timeout
    CallResendAPI --> ResendSuccess : Contact created, returns Contact ID

    ResendFailure --> AbortRegistration : Rollback database context transactions
    ResendSuccess --> RecordMemberContact : Create MemberContact record linking Member.Id to Resend.Id

    RecordMemberContact --> SaveChanges : Add tracking record to DB Context
    SaveChanges --> CompleteRegistration : Commit MySQL transaction (SaveChangesAsync)
    
    CompleteRegistration --> [*] : Return HTTP 200 OK (User ID)
    AbortRegistration --> [*] : Return HTTP 400 Bad Request
```

### Workflow Specification
* **Trigger**: A guest submits the Registration Form on the Angular SPA.
* **Key Decisions**:
  * **Input Check**: Validates field lengths and formats (e.g. valid email containing `@`).
  * **Email Uniqueness**: Queries the database to prevent duplicate registration.
  * **ASP.NET Identity Creation**: Validates password rules (length, digits, uppercase) and saves the auth login.
  * **Marketing Preferences Guard**: If the user did not opt-in to newsletters or promotions, CRM synchronization is skipped.
  * **Resend Integration API Response**: Calls Resend REST endpoints. If it fails (due to API key issues, network glitches, etc.), the database transaction is rolled back (`SaveChanges` is not called) and the registration fails to maintain consistency.
* **Final State**:
  * **Success**: The user has an authentication record, a local profile (`Member`), a tracked CRM identity (`MemberContact`), and receives an active account.
  * **Failure**: The transaction is aborted, no data is written, and error details (Problem Details JSON) are returned to the client.

---

## 2. Marketing Preferences Update & Audited Sync Workflow

This diagram outlines the step-by-step logic executed when a registered member updates their marketing preference settings. It involves consent auditing creation, cancellation token validation, and error-tolerant downstream sync mapping.

```mermaid
stateDiagram-v2
    [*] --> CheckCancellation1 : Member invokes UpdatePreferences
    CheckCancellation1 --> FetchMember : Token active
    CheckCancellation1 --> AbortUpdate : Token cancelled
    
    FetchMember --> MemberNotFound : Member is null
    FetchMember --> UpdatePreferencesLocal : Member exists
    
    MemberNotFound --> AbortUpdate : Return Member.NotFound
    
    UpdatePreferencesLocal --> CheckCancellation2 : Update local model properties
    CheckCancellation2 --> LogConsentEntry : Token active
    CheckCancellation2 --> AbortUpdate : Token cancelled
    
    LogConsentEntry --> CreateConsentLog : Instantiate ConsentLog (Opt-In or Opt-Out)
    CreateConsentLog --> ConsentLogInvalid : Validation fails
    CreateConsentLog --> SaveConsentLog : Validation succeeds (Result.Success)
    
    ConsentLogInvalid --> AbortUpdate : Return validation error
    SaveConsentLog --> TryCRMStoreSync : Add ConsentLog to Repository (Db tracker)
    
    state TryCRMStoreSync {
        [*] --> CheckConsentType
        CheckConsentType --> OptInType : AcceptNews OR AcceptPromo is true
        CheckConsentType --> OptOutType : Both are false
        
        OptInType --> CheckContactExists
        CheckContactExists --> CreateOrRestoreContact : Contact is null or ExternalId is 'DELETED'
        CheckContactExists --> UpdateExistingContact : Contact exists active
        
        OptOutType --> DeleteContactExternal : Delete external CRM record
        
        CreateOrRestoreContact --> CallResendCreate : Invoke CreateContactAsync
        UpdateExistingContact --> CallResendUpdate : Invoke UpdateContactAsync
        DeleteContactExternal --> CallResendDelete : Invoke DeleteContactAsync
        
        CallResendCreate --> SyncCallResult
        CallResendUpdate --> SyncCallResult
        CallResendDelete --> SyncCallResult
        
        state SyncCallResult {
            [*] --> HandleSyncSuccess : API Success
            [*] --> HandleSyncFailure : Exception or API Failure
        }
    }
    
    HandleSyncSuccess --> MarkClearSync : Clear PendingSync (PendingSync = false)
    HandleSyncFailure --> MarkPendingSync : Mark profile PendingSync (PendingSync = true)
    
    MarkClearSync --> SaveDatabaseChanges : Attempt SaveChangesAsync(cancellationToken)
    MarkPendingSync --> SaveDatabaseChanges : Attempt SaveChangesAsync(cancellationToken)
    
    SaveDatabaseChanges --> DatabaseSaveSuccess : Commit MySQL Transaction
    SaveDatabaseChanges --> DatabaseSaveFailure : DB Error (Catch Exception)
    
    DatabaseSaveSuccess --> ReturnSuccess : Return HTTP 200 OK
    DatabaseSaveFailure --> ReturnFailure : Return Database.SaveFailed (HTTP 400)
    
    AbortUpdate --> [*]
    ReturnSuccess --> [*]
    ReturnFailure --> [*]
```

### Workflow Specification
* **Trigger**: An authenticated user modifies their newsletter options on the Angular Settings panel, issuing a PUT request.
* **Key Decisions**:
  * **CancellationToken Validation**: At major checkpoints, the handler verifies cancellation requests from the client.
  * **Consent Action Classification**: Computes whether the operation constitutes an "Opt-In" (any marketing flag enabled) or "Opt-Out" (all flags disabled) to record in the compliance log.
  * **Downstream Integration Sandbox**: Wraps the Resend API communication in a try-catch pattern. If the HTTP call to Resend succeeds, `PendingSync` is cleared. If the call times out or throws an exception, `PendingSync` is set to `true`, converting a blocking API call into a non-blocking queue.
* **Final State**:
  * **Success**: The user's preferences are updated in the database, a `ConsentLog` auditor is written, and the CRM matches (or will match after background reconciliation).
  * **Failure**: The database transaction fails, and the user's preferences remain unchanged.
