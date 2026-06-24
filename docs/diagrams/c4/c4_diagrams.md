---
type: architecture_document
title: C4 Model Diagrams
description: System Context, Container, and Component diagrams for the Galvão application.
timestamp: 2026-06-24T07:23:16-03:00
tags: [architecture, diagrams, c4, mermaid]
---

# C4 Model Diagrams

This document contains C4 Model diagrams representing the context, containers, and components of the Galvão system.

---

## 1. System Context Diagram (Level 1)

The System Context diagram provides a high-level view of the Galvão system, its users, and its external dependencies.

```mermaid
graph TB
    subgraph Users ["Actors"]
        Guest["Guest User (Unregistered)"]
        Member["Registered Member"]
        Admin["System Administrator"]
    end

    subgraph GalvaoSystem ["Galvão System"]
        Galvao["Galvão Web App & Web API"]
    end

    subgraph External ["External Services"]
        Resend["Resend API (Email CRM Sync)"]
    end

    Guest -->|Views showroom items & articles| Galvao
    Member -->|Manages profile, preferences & authentication| Galvao
    Admin -->|Manages showroom items, articles, users & roles| Galvao
    
    Galvao -->|Synchronizes contacts & newsletters| Resend

    style Users fill:none,stroke:#333,stroke-width:1px
    style External fill:none,stroke:#333,stroke-width:1px
    style Galvao fill:#1168bd,stroke:#0b4c8a,color:#fff
    style Resend fill:#3b82f6,stroke:#1d4ed8,color:#fff
```

### System Context Description
* **Actors**:
  * **Guest User (Unregistered)**: Can browse the showroom items and read published news articles.
  * **Registered Member**: A authenticated user who can configure their profile (display name, email, name) and customize their newsletter preferences (marketing and promotion options).
  * **System Administrator**: Can log in to manage all resources (creating and editing showroom items/photos, writing and publishing articles, managing system roles and users).
* **Galvão System**: The core system providing the showroom catalog and news feeds, while handling profile and admin dashboards.
* **Resend API**: An external API used to manage contacts, marketing segments (news and promotion lists), and newsletter preferences.

---

## 2. Container Diagram (Level 2)

The Container diagram shows the high-level architecture of the Galvão system, illustrating the responsibilities, technologies, and interactions between the frontend, backend, and data stores.

```mermaid
graph TB
    subgraph Client ["Client Browser"]
        SPA["Frontend SPA (Angular 22+)"]
    end

    subgraph Server ["Server Environment"]
        subgraph NodeHost ["Node.js Host"]
            Express["SSR Server (Express Host)"]
        end
        subgraph WebAPI ["Backend Web API (.NET 10)"]
            API["API Application (.NET Core 10)"]
        end
        DB[(MySQL Database)]
    end

    subgraph External ["External Systems"]
        Resend["Resend Email Service (REST API)"]
    end

    User["User / Client"] -->|Requests pages / Interacts with| Express
    Express -->|Serves SSR/Prerendered HTML| SPA
    Express -->|Queries data during SSR| API
    SPA -->|Sends HTTPS Requests| API
    API -->|Reads / Writes via EF Core| DB
    API -->|Syncs marketing contacts via HTTPS| Resend

    style Client fill:none,stroke:#333,stroke-width:1px
    style Server fill:none,stroke:#333,stroke-width:1px
    style External fill:none,stroke:#333,stroke-width:1px
    
    style Express fill:#83cd29,stroke:#5c921c,color:#fff
    style SPA fill:#dd0031,stroke:#a6120d,color:#fff
    style API fill:#1168bd,stroke:#0b4c8a,color:#fff
    style DB fill:#4f5b66,stroke:#343d46,color:#fff
    style Resend fill:#3b82f6,stroke:#1d4ed8,color:#fff
```

### Containers Description
1. **SSR Server (Express Host)**: A Node.js environment hosting the Express server. It intercepts page requests from the user, executes Server-Side Rendering (SSR) by invoking Angular's platform-server engine (which queries backend data during compile-time), and serves pre-rendered (SSG) static assets or dynamically built HTML to the client browser.
2. **Frontend SPA**: An Angular (v22+) Single Page Application running in the user's browser after being hydrated from the SSR host. It uses Angular Signals for state management and communicates with the backend API via HTTP client interceptors.
3. **Backend Web API**: A cross-platform API built on .NET 10 using C# and Clean Architecture principles. It handles business logic, database mutations, role-based authorization, and external service communications.
4. **MySQL Database**: A MySQL database storing application credentials (users, claims, roles), showroom item metadata (titles, captions, prices), image URLs, article content, and compliant consent log ledgers. It is accessed via Entity Framework Core.
5. **Resend API**: External transactional and marketing email service. The backend syncs member contact details, opt-in statuses, and segments to this container over HTTPS.

---

## 3. Backend Component Diagram (Level 3)

The Component diagram dives into the Backend Web API, detailing how Clean Architecture layers interact to process requests.

```mermaid
graph TB
    subgraph Presentation ["Presentation Layer"]
        Ctrl["Controllers: Articles, Auth, Members, Showroom"]
        MW["Custom Exception Handling Middleware"]
    end

    subgraph Application ["Application Layer"]
        subgraph CQRS ["CQRS Engine"]
            CmdHandlers["Command Handlers"]
            QryHandlers["Query Handlers"]
        end
        Interfaces["Repository / Service Interfaces"]
    end

    subgraph Infrastructure ["Infrastructure Layer"]
        DBContext["GalvaoDbContext: EF Core"]
        Identity["IdentityService / RoleService"]
        ResendSvc["ResendEmailContactService"]
    end

    subgraph Domain ["Domain Layer"]
        Entities["Entities: Article, Member, ConsentLog, ShowroomItem, BaseEntity"]
    end

    Ctrl -->|Invokes Commands / Queries| CQRS
    CmdHandlers & QryHandlers -->|Interact with| Entities
    CmdHandlers & QryHandlers -->|Use| Interfaces
    
    DBContext -.->|Implements| Interfaces
    Identity -.->|Implements| Interfaces
    ResendSvc -.->|Implements| Interfaces
    
    DBContext -->|Reads / Writes| MySQL[MySQL Database]
    Identity -->|Manages Users/Roles via EF| DBContext
    ResendSvc -->|Calls REST API| ResendAPI["Resend API"]

    style Presentation fill:#e0f2fe,stroke:#0284c7
    style Application fill:#f0fdf4,stroke:#16a34a
    style Infrastructure fill:#fef2f2,stroke:#dc2626
    style Domain fill:#fff7ed,stroke:#ea580c
    style MySQL fill:#4f5b66,stroke:#343d46,color:#fff
    style ResendAPI fill:#3b82f6,stroke:#1d4ed8,color:#fff
```

### Components Description
* **Presentation Layer**:
  * **Controllers**: Expose RESTful endpoints. Inject Command/Query handlers and return formatted results using the `HandleResult` pattern.
  * **Custom Exception Handling Middleware**: Intercepts unhandled exceptions globally and formats them as standard RFC 7807 Problem Details.
* **Application Layer**:
  * **Command Handlers**: Implement mutations, calling domain factory methods and repositories.
  * **Query Handlers**: Execute read queries (often projecting directly to DTOs/Responses).
  * **Interfaces**: Define the boundaries of data access, Identity service actions, and external integrations (including `IConsentLogRepository`).
* **Domain Layer**:
  * Contains enterprise business logic, model constraints (like `Member.UpdateMarketingPreferences`), and pure domain entities (`Article`, `Member`, `ConsentLog`, `ShowroomItem`, `ShowroomItemPhoto`, `BaseEntity`). It has no dependencies on other layers or database providers.
* **Infrastructure Layer**:
  * **GalvaoDbContext**: The EF Core database session, mapping entities (including `ConsentLog`) to the MySQL schema and managing entity tracker states.
  * **IdentityService**: Implements user creation, password verification, and JWT generation/validation.
  * **ResendEmailContactService**: Invokes Resend HTTP endpoints to manage marketing segments and synchronize contact list statuses.
