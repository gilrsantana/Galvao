# Galvão System Architecture Documentation

Welcome to the architectural documentation for **Galvão**, an application featuring a client showroom catalog, article feeds, and administration panels, built with .NET 10, Angular 22+, MySQL, and Resend.

---

## 1. System Overview

Galvão is structured as a decoupled Single Page Application (SPA) frontend and a RESTful Web API backend. The system enables users to browse products/showroom items, read news articles, register for accounts, and choose marketing subscription lists. Administrators can manage these items, upload photos, curate news feeds, assign roles, and handle customer communication records.

```text
               ┌────────────────────────┐
               │    Browser Client      │
               │   (Angular 22+ SPA)    │
               └───────────┬────────────┘
                           │
                     HTTPS Requests
                           │
                           ▼
               ┌────────────────────────┐
               │   Backend Web API      │
               │    (.NET Core 10)      │
               └───────────┬────────────┘
                           │
             ┌─────────────┴─────────────┐
             ▼                           ▼
  ┌─────────────────────┐     ┌─────────────────────┐
  │   MySQL Database    │     │   Resend Email API  │
  │ (EF Core Persistence│     │   (CRM Lead Sync)   │
  └─────────────────────┘     └─────────────────────┘
```

---

## 2. Technology Stack

### Frontend Client
* **Framework**: Angular (v22+) using Standalone Components.
* **State Management**: Angular Signals (fine-grained reactive signals).
* **Styling**: Standard CSS with responsive design components.
* **API Communication**: HttpClient with a functional auth interceptor (handles token injections and silent refreshes).

### Backend Server
* **Framework**: ASP.NET Core Web API (.NET Core 10).
* **Architecture**: Clean Architecture with CQRS (Command Query Responsibility Segregation).
* **Data Access**: Entity Framework Core (EF Core) 10.
* **Database**: MySQL Server database.
* **Security & Identity**: ASP.NET Core Identity Core using Guid keys, combined with JWT Bearer Authentication and Refresh token rotation.
* **External Integrations**: Resend REST API Client for lead capture and segment subscriptions.

---

## 3. Directory Navigation

```text
docs/
├── README.md                  # Main entry point (This file)
├── frontend/
│   └── README.md              # Frontend architecture (routing, signals, interceptors)
├── backend/
│   └── README.md              # Backend layers, controllers, configurations
└── diagrams/
    ├── c4/
    │   └── c4_diagrams.md     # Level 1 Context, Level 2 Container, Level 3 Components
    ├── class/
    │   └── class_diagram.md   # Structural class diagram for domain models
    ├── sequence/
    │   ├── sequence_diagram.md# Step-by-step registration transaction sequence
    │   └── auth_sequence_diagram.md# User login and token refresh flows
    ├── state/
    │   └── state_diagram.md   # Article & showroom photo state lifecycles
    ├── activity/
    │   └── activity_diagram.md# Registration and CRM sync decision workflow
    └── use-case/
        └── use_case_diagram.md# Actors and use case specifications
```

---

## 4. Quick Documentation Links

### Architecture Descriptions
* [Frontend Architecture Overview](./frontend/README.md)
* [Backend Clean Architecture & API Specs](./backend/README.md)

### Architectural & UML Diagrams
* **C4 Model**: [System Context, Container & Components Diagrams](./diagrams/c4/c4_diagrams.md)
* **Domain Structure**: [Class Diagram](./diagrams/class/class_diagram.md)
* **Operational Flows**:
  * [Use Case Diagram](./diagrams/use-case/use_case_diagram.md)
  * [Member Registration Sequence Diagram](./diagrams/sequence/sequence_diagram.md)
  * [User Login & Token Refresh Sequence Diagram](./diagrams/sequence/auth_sequence_diagram.md)
  * [CRM Sync Activity Diagram](./diagrams/activity/activity_diagram.md)
  * [Article & Photo State Diagram](./diagrams/state/state_diagram.md)
