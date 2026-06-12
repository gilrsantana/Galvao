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
