---
type: database_table
title: Database Schema and Mappings
description: Detailed schema mapping and relationship configurations of the Galvão MySQL database.
timestamp: 2026-06-25T03:59:00-03:00
tags: [database, schema, mysql, ef-core]
---

# Database Schema and Mappings

The Galvão database is backed by MySQL and managed via Entity Framework Core (EF Core). The schema leverages Clean Architecture concepts and features a strict audit ledger for compliance, combined with optimization patterns such as Shared Primary Keys.

---

## 1. Identity & Profile Tables (Shared Primary Key Pattern)

The core user accounts, profile details, and CRM tracking use a **Shared Primary Key One-to-One Pattern**. The primary key (`Id`) of the parent entity is shared downstream, ensuring perfect database-level synchronization and cascade deletes without requiring separate foreign key columns.

```text
[ Accounts ] (Parent: Authentication Credentials)
     │
     └── (1:1 Key: Id -> Id) ──> [ Members ] (Profile details & Sync Status)
                                      │
                                      └── (1:1 Key: Id -> MemberId) ──> [ MemberContacts ] (CRM tracking details)
```

### Accounts Table (`Accounts`)
*   **Purpose**: Manages ASP.NET Core Identity credentials, password hashes, and security logs.
*   **Key Columns**:
    *   `Id` (UUID Version 7, Primary Key)
    *   `UserName` (varchar(256), Unique)
    *   `Email` (varchar(256), Unique)
    *   `PasswordHash` (longtext)
    *   `SecurityStamp` (longtext)
    *   `ConcurrencyStamp` (longtext)

### Members Table (`Members`)
*   **Purpose**: Stores user profile information, communication consent options, and CRM sync queue indicators.
*   **Key Columns**:
    *   `Id` (UUID, Primary Key, Foreign Key referencing `Accounts.Id` with Cascade Delete)
    *   `FirstName` (varchar(100), Required)
    *   `LastName` (varchar(100), Required)
    *   `DisplayName` (varchar(100), Required)
    *   `Email` (varchar(256), Required, Unique Index)
    *   `AcceptNews` (bool, Required) - Opt-in/out for Newsletter segment.
    *   `AcceptPromo` (bool, Required) - Opt-in/out for Promotions segment.
    *   `PendingSync` (bool, Default: `false`) - Indicates if local preferences failed to sync with the downstream CRM.

### Member Contacts Table (`MemberContacts`)
*   **Purpose**: Tracks external email marketing contacts (Resend CRM) associated with local members.
*   **Key Columns**:
    *   `Id` (UUID, Primary Key)
    *   `MemberId` (UUID, Unique Foreign Key referencing `Members.Id` with Cascade Delete)
    *   `ExternalContactId` (varchar(150), Required) - Contains the Resend contact UUID, or `"DELETED"` if unsubscribed/GDPR-purged.
    *   `Email` (varchar(256), Required)
    *   `EmailProvider` (varchar(50), String Conversion, e.g., `"Resend"`)
    *   `PhoneNumber` (varchar(50), Nullable)

---

## 2. Marketing Auditing & Segmentation Tables

### Consent Logs Table (`ConsentLogs`)
*   **Purpose**: Stores a tamper-evident audit ledger capturing opt-in/opt-out transitions for GDPR/LGPD compliance.
*   **Key Columns**:
    *   `Id` (UUID, Primary Key)
    *   `MemberId` (UUID, Required Foreign Key referencing `Members.Id` with Restrict/No Action)
    *   `Action` (varchar(50), Required) - Contains `"Opt-In"` or `"Opt-Out"`.
    *   `IpAddress` (varchar(100), Nullable) - Client request IP address.
    *   `Source` (varchar(500), Nullable) - Path/Route where the action occurred.
    *   `ConsentToken` (varchar(250), Nullable) - Cryptographic verification token.

### Email Segments Table (`EmailSegments`)
*   **Purpose**: Tracks which subscription mailing list lists are active for a specific CRM contact.
*   **Key Columns**:
    *   `Id` (UUID, Primary Key)
    *   `MemberContactId` (UUID, Foreign Key referencing `MemberContacts.Id` with Cascade Delete)
    *   `ESegmentType` (varchar(50), String Conversion) - Contains `"News"` or `"Promo"`.
    *   `SubscriptionDate` (datetime, Required)
    *   `UnSubscriptionDate` (datetime, Nullable)

### Email Audit Logs Table (`EmailAuditLogs`)
*   **Purpose**: Audits outbound communication attempts (e.g. system confirmations, marketing mails).
*   **Key Columns**:
    *   `Id` (UUID, Primary Key)
    *   `MemberId` (UUID, Nullable Foreign Key referencing `Members.Id` with SetNull)
    *   `RecipientEmail` (varchar(256), Required)
    *   `Subject` (varchar(256), Required)
    *   `EMailProvider` (varchar(50), String Conversion, e.g., `"Resend"`)
    *   `ETypeOfMessage` (varchar(50), String Conversion, e.g., `"Confirmation"`, `"Newsletter"`)
    *   `StatusCode` (int, Required) - HTTP Status response from provider (e.g. 200, 429, 500).
    *   `ExternalMessageId` (varchar(150), Nullable) - Sent message ID returned by the provider.
    *   `ErrorMessage` (text, Nullable) - Captured error response in case of API failure.
    *   `SentAtUtc` (datetime, Required)

---

## 3. Core System Data Tables

### Articles Table (`Articles`)
*   **Purpose**: Manages news articles, publishing states, and slugs.
*   **Key Columns**:
    *   `Id` (UUID, Primary Key)
    *   `Title` (varchar(200), Required)
    *   `Slug` (varchar(200), Required, Unique Index)
    *   `Content` (text, Required)
    *   `IsPublished` (bool, Required)
    *   `PublishedAt` (datetime, Nullable)
    *   `CreatedAt` (datetime, Required)
    *   `UpdatedAt` (datetime, Nullable)
    *   `Active` (bool, Required) - Logical deletion flag.

### Showroom Items Table (`ShowroomItems`)
*   **Purpose**: Stores physical or digital products displayed in the client showroom.
*   **Key Columns**:
    *   `Id` (UUID, Primary Key)
    *   `Title` (varchar(150), Required)
    *   `Price` (decimal(18,2), Required)
    *   `Category` (varchar(100), Required)
    *   `Active` (bool, Required) - Logical deletion flag.

### Showroom Item Photos Table (`ShowroomItemPhotos`)
*   **Purpose**: Photos linked to showroom items.
*   **Key Columns**:
    *   `Id` (UUID, Primary Key)
    *   `ShowroomItemId` (UUID, Foreign Key referencing `ShowroomItems.Id` with Cascade Delete)
    *   `PhotoUrl` (varchar(500), Required)
    *   `IsPrimary` (bool, Required) - Identifies if the photo is the primary image.

---

## 4. Compliance Ledger (GDPR/LGPD Purges)

### Removed Users Table (`RemovedUsers`)
*   **Purpose**: Audit trail proving deletion of personal data upon customer requests.
*   **Key Columns**:
    *   `Id` (UUID, Primary Key)
    *   `MemberId` (UUID, Required) - ID of the purged profile.
    *   `PersonalDetailsRemoved` (bool)
    *   `AuthCredentialsRemoved` (bool)
    *   `MarketingProviderRemoved` (bool)
    *   `ConsentLogsPurged` (bool)
    *   `RemovedAtUtc` (datetime)
