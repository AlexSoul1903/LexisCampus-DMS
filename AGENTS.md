# AI Assistant Guidelines & Architectural Blueprint for LexisCampus DMS

Welcome to the **LexisCampus DMS** repository. When generating, modifying, or refactoring code in this workspace, you **MUST** strictly follow the guidelines, architecture patterns, and security practices documented below.

---

## 🏛️ 1. Architecture: Strict Onion (Clean) Architecture

The solution is divided into 5 clear concentric layers. Dependencies **ALWAYS point inward**:

```
        +--------------------------------------------------+
        |             LexisCampusDMS.Server                |  (Presentation / Web API)
        +--------------------------------------------------+
           |                      |                      |
           v                      |                      v
 +--------------------+           |           +--------------------+
 | .Infraestructure.  |           |           | .Infraestructure.  |  (Infrastructure)
 |     Persistence    |           |           |        Shared      |
 +--------------------+           |           +--------------------+
           \                      v                      /
            +--------> +--------------------+ <---------+
                       |    .Application    |  (Use Cases & Business Logic)
                       +--------------------+
                                  |
                                  v
                       +--------------------+
                       |    .Core.Domain    |  (Entities & Domain Rules - Core)
                       +--------------------+
```

### Layer Responsibilities:
1. **`LexisCampusDMS.Core.Domain`** (Core / Center)
   - Entities (`BaseEntity<TId>`, `AuditableEntity<TId>`), Value Objects, Domain Enums, Domain Exceptions.
   - Core Interfaces: `IGenericRepository<TEntity, TId>`, `IUnitOfWork`.
   - **Zero external project dependencies**.
2. **`LexisCampusDMS.Application`** (Application Logic)
   - DTOs (`Create...Dto`, `Update...Dto`, `...ResponseDto`), Mappings, FluentValidators.
   - Service Interfaces & Implementations (`IGenericService<...>`, `GenericService<...>`).
   - Response envelopes (`Result<T>`, `PagedResult<T>`).
   - `ServiceRegistration.cs` (`AddApplicationLayer()`).
   - **Depends only on `LexisCampusDMS.Core.Domain`**.
3. **`LexisCampusDMS.Infraestructure.Persistence`** (Database & EF Core)
   - `ApplicationDbContext`, EF Core Entity Configurations, Migrations.
   - Repositories: `GenericRepository<TEntity, TId>` and entity-specific repositories.
   - Unit of Work implementation.
   - `ServiceRegistration.cs` (`AddPersistenceInfrastructure()`).
   - **Depends on `LexisCampusDMS.Core.Domain` and `LexisCampusDMS.Application`**.
4. **`LexisCampusDMS.Infraestructure.Shared`** (External Services)
   - Email, File Storage, PDF generation, DateTime providers, SMS.
   - `ServiceRegistration.cs` (`AddSharedInfrastructure()`).
   - **Depends on `LexisCampusDMS.Core.Domain` and `LexisCampusDMS.Application`**.
5. **`LexisCampusDMS.Server`** (Presentation / API)
   - Web API Controllers, Middlewares (Global Exception Handling, Security Headers, Rate Limiting), Swagger.
   - Composition root in `Program.cs`.
   - **References all underlying layers**.

---

## 💎 2. SOLID Principles Checklist

Every feature or modification must comply with SOLID:
- **S (Single Responsibility)**: Controllers only handle HTTP; Services only handle business logic; Repositories only handle data persistence; Validators only validate; DTOs only carry data.
- **O (Open/Closed)**: Rely on interfaces and strategy patterns so new features or storage providers can be added without modifying existing code.
- **L (Liskov Substitution)**: All repository and service implementations must fully honor their base interface contracts.
- **I (Interface Segregation)**: Interfaces must be small, cohesive, and role-focused.
- **D (Dependency Inversion)**: Always inject abstractions (`IGenericRepository`, `IUnitOfWork`, `IDocumentService`) via constructor injection with `readonly` fields. Never instantiate concrete services directly.

---

## 🧩 3. Required Design Patterns

### Generic Repository (`IGenericRepository<TEntity, TId>`)
- Sits in `Core.Domain.Interfaces`.
- Implemented by `GenericRepository<TEntity, TId>` in `Infraestructure.Persistence.Repositories`.
- Standard methods: `GetByIdAsync`, `GetAllAsync`, `FindAsync(predicate)`, `GetPagedAsync(...)`, `ExistsAsync(predicate)`, `AddAsync`, `AddRangeAsync`, `Update`, `Remove`, `RemoveRange`.

### Generic Service (`IGenericService<TDto, TCreateDto, TUpdateDto, TId>`)
- Sits in `Application.Interfaces`.
- Implemented in `Application.Services`.
- Standard methods: `GetByIdAsync`, `GetAllAsync`, `GetPagedAsync`, `CreateAsync`, `UpdateAsync`, `DeleteAsync`.

### Unit of Work (`IUnitOfWork`)
- Manages transactional boundaries across repositories (`SaveChangesAsync`, `BeginTransactionAsync`, `CommitTransactionAsync`, `RollbackTransactionAsync`).

### Result Envelope Pattern
- Services must return `Result<T>` or `PagedResult<T>` rather than throwing exceptions for predictable business validation failures.

---

## 🔒 4. Security Requirements (API & Application)

1. **Authentication & Authorization**:
   - Secure JWT Bearer authentication with refresh tokens.
   - Secure endpoints with `[Authorize]` by default.
   - Extract user identity strictly from `ClaimsPrincipal`, never from untrusted client request bodies.
2. **Input Validation & Mass Assignment Prevention**:
   - Validate every DTO with FluentValidation.
   - Never accept Domain Entities in controller action parameters; always use explicit request DTOs.
3. **SQL & Injection Defense**:
   - Use EF Core parameterized LINQ queries. Never concatenate raw SQL strings.
4. **Secrets & Sensitive Data**:
   - Never commit passwords, API keys, or connection strings to git.
   - Hash passwords with BCrypt, Argon2, or ASP.NET Identity PasswordHasher.
   - Strip passwords and internal hashes from all response DTOs.
5. **Rate Limiting & Security Headers**:
   - Enforce rate limiting on auth and sensitive endpoints.
   - Configure HSTS, X-Content-Type-Options, X-Frame-Options, CSP, and CORS strictly.
6. **Error Handling & Information Leakage**:
   - Implement Global Exception Handling Middleware producing standard RFC 7807 `ProblemDetails`.
   - Never expose raw stack traces or internal database errors to clients in production.

---

## 📝 5. Code Quality & Standards
- Enable Nullable Reference Types (`#nullable enable`) and eliminate warnings.
- Always propagate `CancellationToken cancellationToken = default` on asynchronous methods.
- Use `AsNoTracking()` in EF Core for read-only queries.
- Keep API controllers ultra-thin.
- Organize code with file-scoped namespaces (`namespace LexisCampusDMS...;`).

---

## 📚 Detailed Rules Reference
For exhaustive details, see:
- [Architecture Rules](.agents/rules/architecture.md)
- [SOLID Principles](.agents/rules/solid_principles.md)
- [Design Patterns](.agents/rules/design_patterns.md)
- [Security Guidelines](.agents/rules/security.md)
- [Coding Standards](.agents/rules/coding_standards.md)
