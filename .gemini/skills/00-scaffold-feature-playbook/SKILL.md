---
name: scaffold-feature-playbook
description: Master playbook to guide the chronological creation of a new feature (domain, use cases, DB mapping, DI, controller, and tests) by linking individual skills.
---

# Playbook: Scaffolding a New Feature Slice

Use this playbook when you need to add a new entity or feature to the application. You must follow the steps below sequentially, utilizing the individual referenced skills.

---

## The Scaffolding Sequence Checklist

### 🏁 Phase 1: Core Domain Modeling
- [ ] Create the domain entity inheriting from `BaseEntity` with proper property encapsulation.
  - 👉 *Refer to skill:* [01-create-domain-entity](file:///home/gilmar/Development/FrontEndTemplate/backend/.gemini/skills/01-create-domain-entity/SKILL.md)

### ⚙️ Phase 2: Application Use Cases
- [ ] Implement the Commands, Queries, and Response records.
- [ ] Write the Handlers implementing `ICommandHandler` or `IQueryHandler`.
- [ ] Implement manual mappings inside the handlers.
  - 👉 *Refer to skill:* [02-create-use-case](file:///home/gilmar/Development/FrontEndTemplate/backend/.gemini/skills/02-create-use-case/SKILL.md)

### 🗄️ Phase 3: Infrastructure and Database Mapping
- [ ] Add the database Fluent API configuration under `Persistence/Configurations/`.
- [ ] Create the repository inheriting from `BaseEntityRepository<TEntity>`.
  - 👉 *Refer to skill:* [04-create-infrastructure-mapping](file:///home/gilmar/Development/FrontEndTemplate/backend/.gemini/skills/04-create-infrastructure-mapping/SKILL.md)

### 🔌 Phase 4: Dependency Injection Wiring
- [ ] Register the CQRS Use Case handlers in `Application/Extensions/DependencyInjection.cs`.
- [ ] Register the Repository interface mapping in `Infrastructure/Extensions/DependencyInjection.cs`.
  - 👉 *Refer to skill:* [05-configure-dependency-injection](file:///home/gilmar/Development/FrontEndTemplate/backend/.gemini/skills/05-configure-dependency-injection/SKILL.md)

### 🌐 Phase 5: Exposing the API
- [ ] Create the Controller inheriting from `ApiControllerBase`.
- [ ] Inject Use Case handlers and expose actions using `HandleResult`.
- [ ] Annotate actions with appropriate `[ProducesResponseType]` OpenAPI attributes.
  - 👉 *Refer to skill:* [06-create-api-controller](file:///home/gilmar/Development/FrontEndTemplate/backend/.gemini/skills/06-create-api-controller/SKILL.md)

### 🧪 Phase 6: Verification and Testing
- [ ] Write unit tests for domain logic and application handlers using `Moq` for mocks.
  - 👉 *Refer to skill:* [07-create-unit-test](file:///home/gilmar/Development/FrontEndTemplate/backend/.gemini/skills/07-create-unit-test/SKILL.md)
- [ ] Run `dotnet build` to ensure the entire solution compiles with zero errors.
