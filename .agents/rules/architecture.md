# Onion Architecture & Layering Rules

## Overview
The solution follows strict **Onion Architecture** (Clean Architecture). All dependencies must point **inward** toward the Core Domain. Outer layers depend on inner layers; inner layers never depend on outer layers.

```
       +--------------------------------------------------+
       |             LexisCampusDMS.Server                | (Presentation / API)
       +--------------------------------------------------+
          |                      |                      |
          v                      |                      v
+--------------------+           |           +--------------------+
| .Infraestructure.  |           |           | .Infraestructure.  | (Infrastructure)
|     Persistence    |           |           |        Shared      |
+--------------------+           |           +--------------------+
          \                      v                      /
           +--------> +--------------------+ <---------+
                      |    .Application    | (Application / Use Cases)
                      +--------------------+
                                 |
                                 v
                      +--------------------+
                      |    .Core.Domain    | (Core Domain - Center)
                      +--------------------+
```

---

## Layer Definitions & Strict Boundaries

### 1. Core Domain Layer (`LexisCampusDMS.Core.Domain`)
- **Center of the architecture**: Contains enterprise domain logic, entities, value objects, domain events, domain enums, domain exceptions, and core repository interfaces.
- **Rule**: **ZERO external project dependencies**. Does NOT reference Entity Framework, ASP.NET Core, or any other layer.
- **Components**:
  - `Entities/`: Domain models inheriting from `BaseEntity` (e.g., `Document`, `User`, `Folder`).
  - `Interfaces/`: Core interfaces like `IGenericRepository<TEntity, TId>`, `IUnitOfWork`.
  - `Enums/`: Domain-wide enumerations.
  - `Exceptions/`: Domain-specific exceptions (e.g., `EntityNotFoundException`, `DomainValidationException`).
  - `Common/`: Common abstractions (e.g., `BaseEntity<TId>`, `AuditableEntity<TId>`).

### 2. Application Layer (`LexisCampusDMS.Application`)
- **Use Cases & Business Orchestration**: Implements business workflows, DTOs, service abstractions and generic services, validators, and mappings.
- **Rule**: References **ONLY** `LexisCampusDMS.Core.Domain`. Does NOT reference Persistence, Shared, or Server.
- **Components**:
  - `DTOs/`: Data Transfer Objects for requests and responses (e.g., `CreateDocumentDto`, `DocumentResponseDto`).
  - `Interfaces/`: Application service interfaces (e.g., `IGenericService<TDto, ...>`, `IDocumentService`, `IEmailService`).
  - `Services/`: Service implementations coordinating domain entities, repositories, and business rules.
  - `Mappings/`: Entity-to-DTO and DTO-to-Entity mapping definitions.
  - `Validators/`: Input validation rules (e.g., FluentValidation classes).
  - `Wrappers/`: API response envelopes (e.g., `Result<T>`, `PagedResult<T>`).
  - `ServiceRegistration.cs`: `AddApplicationLayer()` extension method for DI registration.

### 3. Persistence Infrastructure Layer (`LexisCampusDMS.Infraestructure.Persistence`)
- **Data Access & Storage**: Implements repository interfaces, EF Core DbContext, database migrations, configurations, and queries.
- **Rule**: References `LexisCampusDMS.Core.Domain` and `LexisCampusDMS.Application`.
- **Components**:
  - `Contexts/`: `ApplicationDbContext` inheriting from `DbContext`.
  - `Configurations/`: EF Core `IEntityTypeConfiguration<T>` classes (fluent API configurations).
  - `Repositories/`: `GenericRepository<TEntity, TId>` and specific repository implementations.
  - `UnitOfWork/`: `UnitOfWork` implementation managing transactions.
  - `Migrations/`: EF Core database migration files.
  - `ServiceRegistration.cs`: `AddPersistenceInfrastructure(this IServiceCollection services, IConfiguration configuration)` extension method.

### 4. Shared Infrastructure Layer (`LexisCampusDMS.Infraestructure.Shared`)
- **External Services & Tools**: Implements external communication and utilities.
- **Rule**: References `LexisCampusDMS.Core.Domain` and `LexisCampusDMS.Application`.
- **Components**:
  - `Services/`: Implementation of external services such as `EmailService`, `FileStorageService`, `DateTimeService`, `PdfGeneratorService`.
  - `ServiceRegistration.cs`: `AddSharedInfrastructure(this IServiceCollection services, IConfiguration configuration)` extension method.

### 5. Presentation / Server Layer (`LexisCampusDMS.Server`)
- **API Entry Point & Composition Root**: Exposes HTTP endpoints, configures middleware, handles authentication tokens, and wires up all dependencies.
- **Rule**: References `Application`, `Infraestructure.Persistence`, `Infraestructure.Shared`, and `Core.Domain`.
- **Components**:
  - `Controllers/`: Clean, thin API controllers that delegate requests to Application services.
  - `Middlewares/`: Global exception handling, security headers, rate limiting, request logging.
  - `Program.cs`: Composition root registering all layers and configuring the HTTP pipeline.

---

## Anti-Patterns & Prohibited Practices

❌ **DO NOT**:
1. Inject `DbContext` or data access code directly into Controllers or Application Services.
2. Return Domain Entities directly from Controllers or Application Services; always map to DTOs.
3. Reference `Infraestructure.Persistence` or `Infraestructure.Shared` inside `Application` or `Core.Domain`.
4. Put business logic inside Controllers; Controllers must only handle HTTP concerns and call Application Services.
5. Create circular references between any layers.
