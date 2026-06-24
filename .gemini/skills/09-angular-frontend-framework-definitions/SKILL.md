You are an expert in TypeScript, Angular, and scalable web application development. You write functional, maintainable, performant, and accessible code following Angular and TypeScript best practices.

## TS/Angular 22 Core Rules

- **Strict Type Checking**: Must be enabled. Avoid using `any`; use `unknown` or explicit interfaces defined in `core/models/`.
- **Standalone Components**: Always use standalone components. Do NOT set `standalone: true` in component decorators (default in Angular v20+).
- **Control Flow**: Always use native control flow (`@if`, `@for`, `@switch`) instead of legacy `*ngIf`/`*ngFor`.
- **Dependency Injection**: Use the `inject()` function at the property level instead of constructor injection.
  ```typescript
  private readonly http = inject(HttpClient);
  private readonly authService = inject(AuthService);
  ```

---

## State Management with Signals
- **Component State**: Use Signals (`signal()`, `computed()`) for local component and service state management.
- **Inputs & Outputs**: Use the `input()` and `output()` functions instead of legacy decorators (`@Input`, `@Output`).
  ```typescript
  readonly itemId = input.required<string>();
  readonly itemChanged = output<void>();
  ```
- **Derived State**: Always use `computed()` to calculate derived state rather than manual recalculation.
- **State Mutation**: Do NOT use `mutate` on signals. Always use `update()` or `set()`.

---

## API Services and HTTP Communication
- **Service Registration**: Services must be singleton and use the `providedIn: 'root'` option.
- **Asynchronous Actions**: For transactional requests (e.g. login, sign up, submit forms), prefer converting HTTP Observables to Promises using RxJS `firstValueFrom` to allow clean async/await patterns.
  ```typescript
  async submitData(data: CreateItemRequest): Promise<string> {
    return await firstValueFrom(
      this.http.post<string>(`${this.apiBase}/items`, data)
    );
  }
  ```
- **Queries/Lists**: Standard read streams can return `Observable<T>` directly. Use the async pipe or `.subscribe` to populate signals.
- **Generated Clients vs Manual Services**: 
  - `ng-openapi-gen` is configured and outputs to `core/api/`.
  - For domain-specific services (e.g., [ShowroomService](file:///home/gilmar/Development/ai-driven-development/projects/galvao/frontend/src/app/core/services/showroom.service.ts)), wrap endpoint calls into hand-written services that map directly to the clean models in `core/models/`.

---

## Tailwind CSS v4 Styling Standards
- **Theme Variables**: Enforce the usage of theme colors and design tokens defined in the `@theme` block of `styles.css`:
  - Backgrounds: `bg-bg-primary`, `bg-bg-secondary`, `bg-bg-dark`
  - Text: `text-text-primary`, `text-text-secondary`, `text-accent-gold`
  - Radii: `rounded-xl` (md), `rounded-3xl` (lg)
- **Transitions**: Use smooth cubic-bezier transitions for hover effects: `transition-all duration-400 ease-[cubic-bezier(0.16,1,0.3,1)]` (e.g., `-translate-y-1 shadow-lg border-accent-gold`).
- **No Arbitrary Colors**: Do not hardcode arbitrary hexadecimal or RGB colors directly in templates (avoid `bg-[#c5a880]`). Use the theme tokens.
