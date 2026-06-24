---
type: architecture_document
title: Galvão System Architecture Overview
description: Main system overview and technology stack details for the Galvão project.
timestamp: 2026-06-24T07:23:16-03:00
tags: [overview, architecture, system]
---

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
* **Rendering Architecture**: Angular Server-Side Rendering (SSR) and static pre-rendering (SSG) powered by an Express Node.js host.
* **State Management**: Angular Signals (fine-grained reactive signals).
* **Styling & UX**: Standard CSS with responsive design components, featuring skeletal loading shimmers and layout constraints to minimize Cumulative Layout Shift (CLS).
* **SEO & Metadata**: Dynamic page titles, meta descriptions, and JSON-LD structured schemas (Article/Product) dynamically injected into the head.
* **API Communication**: HttpClient with a functional auth interceptor (handles token injections and silent refreshes, built with platform checking compatibility).

### Backend Server
* **Framework**: ASP.NET Core Web API (.NET Core 10).
* **Architecture**: Clean Architecture with CQRS (Command Query Responsibility Segregation).
* **Data Access**: Entity Framework Core (EF Core) 10.
* **Database**: MySQL Server database.
* **Security & Identity**: ASP.NET Core Identity Core using Guid keys, combined with JWT Bearer Authentication and Refresh token rotation.
* **Consent Auditing**: Auditing ledger utilizing a `ConsentLog` repository to track Opt-In/Opt-Out actions for compliance.
* **External Integrations**: Resend REST API Client for lead capture and segment subscriptions, decoupled via a robust local database sync-queue (`PendingSync`).

---

## 3. Directory Navigation

```text
docs/
├── index.md                   # Global bundle directory index (OKF Entry Point)
├── README.md                  # Main system overview (This file)
├── log.md                     # Chronological catalog update log
├── frontend/
│   ├── index.md               # Frontend catalog index
│   └── architecture.md        # Frontend architecture details
├── backend/
│   ├── index.md               # Backend catalog index
│   └── architecture.md        # Backend architecture details
└── diagrams/
    ├── index.md               # Diagrams catalog index
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
* [Frontend Architecture Overview](./frontend/architecture.md)
* [Backend Clean Architecture & API Specs](./backend/architecture.md)

### Architectural & UML Diagrams
* **C4 Model**: [System Context, Container & Components Diagrams](./diagrams/c4/c4_diagrams.md)
* **Domain Structure**: [Class Diagram](./diagrams/class/class_diagram.md)
* **Operational Flows**:
  * [Use Case Diagram](./diagrams/use-case/use_case_diagram.md)
  * [Member Registration Sequence Diagram](./diagrams/sequence/sequence_diagram.md)
  * [User Login & Token Refresh Sequence Diagram](./diagrams/sequence/auth_sequence_diagram.md)
  * [CRM Sync Activity Diagram](./diagrams/activity/activity_diagram.md)
  * [Article & Photo State Diagram](./diagrams/state/state_diagram.md)
