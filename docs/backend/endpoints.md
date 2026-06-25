---
type: api_endpoint
title: Critical API Endpoints
description: Detailed documentation of key and complex API endpoints in the Galvão system.
timestamp: 2026-06-25T04:00:00-03:00
tags: [api, endpoints, authentication, members, gdpr]
---

# Critical API Endpoints

This document isolates and describes the critical, complex API endpoints within the Galvão backend. It outlines authentication requirements, JSON payloads, internal application handlers, database updates, and downstream integrations.

---

## 1. Member Registration
Registers a new system user, creates their local member profile, and enqueues a CRM contact synchronization job.

*   **Endpoint**: `POST /api/auth/register`
*   **Authentication**: Anonymous (`[AllowAnonymous]`)
*   **Request Headers**: `Content-Type: application/json`

### Request Payload (`RegisterRequest`)
```json
{
  "email": "user@example.com",
  "password": "SecurePassword123!",
  "displayName": "UserDisp",
  "firstName": "John",
  "lastName": "Doe",
  "acceptNews": true,
  "acceptPromo": false
}
```

### Execution Flow
1.  **Unique Checks**: Calls the Identity service to verify if the `email` already exists in the `Accounts` table. If it exists, returns `400 Bad Request` with error code `Identity.DuplicateEmail`.
2.  **Domain Creation**: Builds a new `Member` domain model. If inputs are invalid (e.g. malformed email), returns `400 Bad Request`.
3.  **Authentication Credentials**: Creates a password-secured `Account` (ASP.NET Core Identity user) using the same UUID generated for the `Member`.
4.  **Local Database Insertion**: Saves the `Member` to the `Members` table.
5.  **Downstream CRM Sync Job (Asynchronous)**:
    If `acceptNews` or `acceptPromo` is `true`, enqueues a background job using Hangfire (`ICrmSyncJob`). The job runs in the background to avoid blocking the HTTP request thread.
6.  **Commit**: Save changes to the database and returns the generated Member UUID.

### Response Codes
*   `200 OK`: Returns the generated Guid of the newly registered Member.
*   `400 Bad Request`: Validation failure or duplicate email.

---

## 2. Update Marketing Preferences
Modifies a member's opt-in/opt-out status, records a security/compliance log, and performs inline downstream synchronization with the CRM.

*   **Endpoint**: `PUT /api/members/{id}/preferences`
*   **Authentication**: Authorized User (JWT Bearer Token required)
*   **Route Parameter**: `id` (Guid) - The UUID of the member.

### Request Payload (`UpdateMarketingPreferencesRequest`)
```json
{
  "acceptNews": true,
  "acceptPromo": true,
  "consentToken": "cryptographic_token_string",
  "consentedAt": "2026-06-25T04:00:00Z"
}
```

### Context Capturing
The controller captures client information directly from the HTTP Context:
*   `IpAddress`: Extracted from `HttpContext.Connection.RemoteIpAddress` (mapped to `ConsentLog.IpAddress`).
*   `Source`: Extracted from the `User-Agent` HTTP header (mapped to `ConsentLog.Source`).

### Execution Flow
1.  **Validation**: Ensures the `Member` exists in the local database. If not, returns `404 Not Found`.
2.  **State Update**: Updates the `AcceptNews` and `AcceptPromo` fields on the `Member` aggregate.
3.  **Auditing (GDPR/LGPD)**: Creates and saves a new `ConsentLog` record logging the transition ("Opt-In" if either flag is true, else "Opt-Out"), IP address, User-Agent, and `ConsentToken`.
4.  **Inline Synchronisation**:
    *   Attempts to update preferences in Resend CRM synchronously via `IEmailContactService`.
    *   If no CRM contact is present, it creates one (or restores a `"DELETED"` contact) and records the association in `MemberContacts` and `EmailSegments`.
    *   If a CRM contact exists, it updates details and adjusts `EmailSegments`.
    *   **Resend API Failure Fallback**: If synchronization fails (due to connection failure or API limits), the exception is caught, the member profile is updated with `PendingSync = true`, and the local database changes (profile update + consent log) are still saved.
5.  **Commit**: Saves all local alterations to the database.

### Response Codes
*   `200 OK`: Preferences successfully updated (locally and possibly remotely).
*   `400 Bad Request`: Input validation failed.
*   `401 Unauthorized`: Missing or invalid JWT.
*   `404 NotFound`: Member does not exist.

---

## 3. Purge User Account (GDPR Compliance)
Permanently erases all personally identifiable information (PII) from the local database and third-party systems.

*   **Endpoint**: `DELETE /api/members/{id}`
*   **Authentication**: Authorized User (JWT Bearer Token required)
*   **Route Parameter**: `id` (Guid) - The UUID of the member to delete.

### Request Payload (`PurgeUserRequest`)
```json
{
  "password": "UserSecurePassword!"
}
```

### Security Check
The controller validates that the user is attempting to delete their own account. It compares the UUID from the JWT's `NameIdentifier` claim against the route parameter `id`. If they mismatch, returns `403 Forbidden`.

### Execution Flow
1.  **Verification**: Ensures the member exists locally. Verify the provided `password` against the hash stored in `Accounts` using `UserManager.CheckPasswordAsync`. Mismatches return `400 Bad Request` (`Identity.InvalidPassword`).
2.  **Downstream Deletion**:
    *   Loads the associated `MemberContact` tracker.
    *   Calls the Resend API (`DELETE /contacts/{externalId}`) to delete the contact record from the marketing platform.
    *   Removes the `MemberContact` and its `EmailSegments` locally from the database.
3.  **Local Profile Deletion**: Removes the profile record from the `Members` table.
4.  **Credential Deletion**: Deletes the ASP.NET Core Identity authentication record from the `Accounts` table.
5.  **Purge Log Generation**:
    *   Creates a `RemovedUser` audit log entry, mapping the user's name and email along with booleans verifying that personal details, credentials, and CRM data were successfully cleared.
    *   Note: For security/compliance, consent logs related to the member are cascades-deleted, and only the `RemovedUser` compliance ledger persists the deletion action.
6.  **Commit**: Saves everything to the database in a single atomic transaction.

### Response Codes
*   `200 OK`: Account and associated data completely purged.
*   `400 Bad Request`: Invalid password.
*   `401 Unauthorized`: Missing or invalid JWT.
*   `403 Forbidden`: Mismatch between JWT subject claim and request ID.
*   `404 NotFound`: Member does not exist.
