# Frontend Architecture Documentation

This document describes the design, directory structure, state management, and API integration patterns of the Galvão client application.

---

## 1. Framework & Core Technologies

The Galvão frontend is built on **Angular (v22+)**, utilizing modern features like:
* **Standalone Components**: Eliminates NgModule boilerplate, making components self-contained, lightweight, and easy to trace.
* **Angular Signals**: Used for reactive state management, providing fine-grained reactivity, reducing change detection overhead, and simplifying code.
* **Server-Side Rendering (SSR) & SSG**: Uses Angular's unified rendering engine to compile pages server-side, with an Express Node.js host handling requests.
* **Dynamic SEO & Metadata**: Exposes dynamic titles, descriptions, and structural JSON-LD schemas inside details pages to satisfy search indexing requirements.
* **Layout Stability (CLS Reduction)**: Implements layout constraint heights combined with skeletal shimmer animations to eliminate Cumulative Layout Shift.
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

## 6. Server-Side Rendering (SSR) & Pre-rendering (SSG)

The frontend uses Angular SSR to render views on the server prior to sending them to the browser, which significantly improves initial load performance (FCP) and SEO indexability.

### Server Routing Policy (`src/app/app.routes.server.ts`)
The server routes specify different rendering configurations depending on the path:
* **Server-Side Rendering (`RenderMode.Server`)**: Applied to pages containing dynamic content or requiring authentication:
  * `showroom/:id` (Dynamic details)
  * `articles/:id` (Dynamic news details)
  * `settings` (Authenticated user preferences)
  * `admin` (Authenticated administrator panel)
* **Pre-rendering (`RenderMode.Prerender`)**: Applied statically at build time for public or relatively static views (under fallback `**` route matching):
  * `''` (Homepage)
  * `showroom` (Product catalog list)
  * `articles` (News catalog list)
  * `login` / `register` (Auth forms)

### Browser Safety and Node Compatibility
Because SSR executes the Angular application in a server-side Node.js environment, web browser-only global APIs (such as `localStorage` and `window`) are unavailable.
To prevent execution crashes:
* The dependency injection token `PLATFORM_ID` is injected into core services (e.g., `AuthService`).
* Functions utilize `isPlatformBrowser(this.platformId)` guards before reading or mutating client state storage:
  ```typescript
  if (isPlatformBrowser(this.platformId)) {
    const tokens = localStorage.getItem('galvao_tokens');
    // Safe to access browser localStorage
  }
  ```

---

## 7. Search Engine Optimization (SEO) & Structured Data (JSON-LD)

To optimize indexation for search engines (like Google or Bing), the system implements automatic metadata enrichment and structured JSON-LD schemas.

### Base Metadata & Route Titles
* Default configuration (such as base language tags `lang="pt-BR"`) is initialized in `index.html`.
* Custom page titles are embedded directly into route mappings (`app.routes.ts`) through the `title` property.

### Dynamic Details Enrichment
For dynamic articles and products, `ArticleDetailComponent` and `ShowroomDetailComponent` utilize Angular's `Title` and `Meta` services inside their subscription hooks to set contextual details:
* **Metadata**: The title is updated to match the item title, and the `<meta name="description">` tag is populated with the item's summary description.
* **Metadata Teardown**: Upon component destruction (`ngOnDestroy`), the description tag is safely restored to default values to avoid polluting other routes.

### Dynamic JSON-LD Schema
To provide explicit structural semantics, detail components dynamically compile and insert standard `@context: https://schema.org` scripts into the document head:
* **Article Schema (`@type: Article`)**: Maps headline, description, author name, datePublished, and dateModified.
* **Product Schema (`@type: Product`)**: Maps product name, description, category, price, BRL currency, and stock availability.
* **Lifecycle Cleanup**: The scripts are assigned unique identifiers (e.g., `id="article-jsonld"`) and are programmatically removed from the document tree during `ngOnDestroy` to prevent multiple conflicting schemas.

---

## 8. Cumulative Layout Shift (CLS) Reduction & Skeletal Loaders

To ensure a smooth user experience and high Core Web Vitals (specifically Cumulative Layout Shift - CLS) ratings, the system ensures layout stability during data loading.

### Skeletal Loaders (`.skeleton-shimmer`)
* When fetching asynchronous lists or grids, components check the reactive `isLoading()` Signal.
* While `isLoading()` is `true`, a matching number of skeletal cards are rendered instead of blank space.
* The `.skeleton-shimmer` class utilizes a CSS keyframe animation (`skeleton-loading`) with a linear gradient moving horizontally across the element to represent a loading shimmer:
  ```css
  .skeleton-shimmer {
    background: linear-gradient(90deg, var(--bg-secondary) 25%, rgba(197, 168, 128, 0.15) 37%, var(--bg-secondary) 63%);
    background-size: 400% 100%;
    animation: skeleton-loading 1.4s ease infinite;
  }
  ```

### Layout Constraints
* Grid elements are assigned explicit container heights (`min-height: 480px` on grid lists) and image aspects (`aspect-ratio: 16/9`).
* This reserves layout area on the viewport, preventing surrounding page content from shifting abruptly when API responses return and elements swap.

---

## 9. Architecture Diagrams References

Refer to the visual diagrams for structural context:
* [C4 Context & Container Diagrams](../diagrams/c4/c4_diagrams.md): How the SPA communicates with the Web API and Resend endpoints under the SSR Express host.
* [System Use Case Diagram](../diagrams/use-case/use_case_diagram.md): Shows use case mappings between Guests, Members, and Admins.
* [Registration Sequence Flow](../diagrams/sequence/sequence_diagram.md): Outlines authentication and API sync timing.
* [User Login & Token Refresh Sequence Diagram](../diagrams/sequence/auth_sequence_diagram.md): Flows of user login and silent token renewal (refresh) events.
* [Newsletter Consent Sync Activity Diagram](../diagrams/activity/activity_diagram.md): Step-by-step registration validation routing inside backend services.
