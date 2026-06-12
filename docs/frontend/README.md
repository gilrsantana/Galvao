# Frontend Architecture Documentation

This document describes the design, directory structure, state management, and API integration patterns of the Galvão client application.

---

## 1. Framework & Core Technologies

The Galvão frontend is built on **Angular (v22+)**, utilizing modern features like:
* **Standalone Components**: Eliminates NgModule boilerplate, making components self-contained, lightweight, and easy to trace.
* **Angular Signals**: Used for reactive state management, providing fine-grained reactivity, reducing change detection overhead, and simplifying code.
* **Functional Routing & Guards**: Simplifies guard writing by replacing class-based guards with functional injectables.
* **RxJS Integration**: Handled via component templates, async pipes, or RxJS utility blocks (like `from` and `firstValueFrom` to bridge Signals and asynchronous HTTP promises).

---

## 2. Directory Structure (`frontend/src/app`)

The application codebase is divided into two main folders under `src/app`:

```text
src/app/
├── core/                       # Shared modules, services, configurations
│   ├── constants/             # Global API endpoints and route settings
│   ├── guards/                # Route activation guards (admin, auth)
│   ├── interceptors/          # Global HTTP request middlewares
│   ├── models/                # TypeScript interfaces & API models
│   └── services/              # Singleton API services
└── features/                   # Feature modules and views
    ├── admin/                 # Administrator dashboards
    ├── articles/              # News browsing components
    ├── auth/                  # Login & registration views
    ├── home/                  # Landing page
    ├── settings/              # Member profile config panel
    └── showroom/              # Catalog list and detail views
```

---

## 3. Routing Map & Protection Rules

The routing structure is configured in `app.routes.ts`:

| Route Path | Associated Component | CanActivate Guards | Description |
|---|---|---|---|
| `''` | `HomeComponent` | None | System homepage, displays featured items and articles. |
| `'showroom'` | `ShowroomListComponent` | None | Public showroom catalog list. Supports paging. |
| `'showroom/:id'` | `ShowroomDetailComponent` | None | Detailed showroom item page, displaying photos. |
| `'articles'` | `ArticleListComponent` | None | News list page. Non-admins only see published articles. |
| `'articles/:id'` | `ArticleDetailComponent` | None | Detailed article viewing page. |
| `'login'` | `LoginComponent` | None | Credentials authentication entry form. |
| `'register'` | `RegisterComponent` | None | Sign-up form including newsletter preference choices. |
| `'settings'` | `SettingsComponent` | `authGuard` | Member configurations (updates profile or preferences). |
| `'admin'` | `AdminDashboardComponent`| `adminGuard` | Administrator panels to manage showroom/articles/users. |
| `'**'` | (Redirect to `''`) | None | Catch-all fallback pattern. |

* **authGuard**: Restricts access to authenticated users only. Checks if `AuthService.isAuthenticated()` returns true.
* **adminGuard**: Restricts access to administrators only. Checks if `AuthService.isAdmin()` returns true.

---

## 4. Signal-Based State Management

Authentication state is managed reactively inside `AuthService` using Angular Signals:

* **`tokenResponse`**: A raw signal storing the active `TokenResponse` (Access and Refresh JWT tokens, plus expiry timestamp). Initialized from browser local storage (`galvao_tokens`).
* **`isAuthenticated`**: A computed signal (`computed(() => this.tokenResponse() !== null)`) providing a boolean flag of auth state.
* **`currentUser`**: A computed signal that decodes the payload of the active access JWT to retrieve token claims.
* **`currentUserId`**: Extracted GUID string from the XML NameIdentifier claim.
* **`userRoles`**: Extracted array of roles from the Microsoft Security claims.
* **`isAdmin`**: Computed signal checking if `userRoles` contains the `"Admin"` role.

---

## 5. Network Request Architecture: HTTP Interceptor & Token Rotation

The client intercepts all outbound HTTP requests using the functional interceptor `authInterceptor`.

### Interceptor Logic Flow
1. **Bearer Attachment**: Reads `AuthService.tokenResponse()`. If present, clones the request and appends the header: `Authorization: Bearer <accessToken>`.
2. **Error Handling**: Catches outbound HTTP errors. If a request returns an **HTTP 401 Unauthorized**:
   - The interceptor halts the request pipeline.
   - It issues an asynchronous promise to `AuthService.refreshTokens()` (wrapped in RxJS `from`).
   - If token refresh succeeds: clones the failed request with the *new* access token, and retries the HTTP action.
   - If token refresh fails: logs out the user (clears local storage and signals), and propagates the error.

```text
[ Outgoing HTTP ]
        │
        ▼
   Has Token? ── Yes ──> [ Attach Header: Bearer JWT ] 
        │                               │
        No                              ▼
        │                       [ Send Request ]
        ▼                               │
[ Send Request ]                Was 401 Error?
                                        │
                              ┌── Yes ──┴── No ──> [ Return response/error ]
                              ▼
                      [ Refresh JWT Tokens ]
                              │
                    Refresh Successful?
                              │
                    ┌── Yes ──┴── No ──> [ Logout user & propagate error ]
                    ▼
          [ Retry with new JWT ]
```

---

## 6. Architecture Diagrams References

Refer to the visual diagrams for structural context:
* [C4 Context & Container Diagrams](../diagrams/c4/c4_diagrams.md): How the SPA communicates with the Web API and Resend endpoints.
* [System Use Case Diagram](../diagrams/use-case/use_case_diagram.md): Shows use case mappings between Guests, Members, and Admins.
* [Registration Sequence Flow](../diagrams/sequence/sequence_diagram.md): Outlines authentication and API sync timing.
* [User Login & Token Refresh Sequence Diagram](../diagrams/sequence/auth_sequence_diagram.md): Flows of user login and silent token renewal (refresh) events.
* [Newsletter Consent Sync Activity Diagram](../diagrams/activity/activity_diagram.md): Step-by-step registration validation routing inside backend services.
