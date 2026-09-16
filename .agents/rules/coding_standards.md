---
trigger: always_on
---

# Coding Standards & C# .NET 8 Best Practices

## 1. Language & Modern C# Features
- Use **file-scoped namespaces** (`namespace LexisCampusDMS.Application;`).
- Use **primary constructors** where appropriate for simple dependency injection.
- Keep **Nullable Reference Types** enabled (`<Nullable>enable</Nullable>`) and resolve all nullability warnings properly.
- Use pattern matching (`is not null`, `switch` expressions) over nested `if` statements.

---

## 2. Asynchronous Programming Rules
- Always use `async` / `await` for I/O bound operations (database calls, file system, HTTP requests).
- **Always accept and forward `CancellationToken cancellationToken = default`** through all layers (Controller -> Service -> Repository -> EF Core).
- **NEVER** use `.Result`, `.Wait()`, or `.GetAwaiter().GetResult()` to avoid thread pool starvation and deadlocks.
- Use `AsNoTracking()` in EF Core for read-only queries to optimize memory and performance.

---

## 3. Controller Guidelines
- Controllers must inherit from `ControllerBase` and be decorated with `[ApiController]`, `[Route("api/[controller]")]`.
- Controllers must remain **thin**; they only receive requests, delegate to services, and return `IActionResult` or `ActionResult<Result<T>>`.
- Annotate actions with `[ProducesResponseType(typeof(...), StatusCodes.Status200OK)]`, `[ProducesResponseType(StatusCodes.Status400BadRequest)]`, etc.
- Return appropriate HTTP status codes:
  - `200 OK` / `201 Created` for successful operations.
  - `204 NoContent` for deletions or updates with no content.
  - `400 BadRequest` for validation/business rule failures.
  - `401 Unauthorized` / `403 Forbidden` for auth failures.
  - `404 NotFound` when an entity does not exist.

---

## 4. Naming Conventions
- **Interfaces**: Prefix with `I` (e.g., `IGenericRepository`, `IDocumentService`).
- **Classes / Types**: PascalCase (e.g., `DocumentService`, `CreateDocumentDto`).
- **Methods**: PascalCase and suffix asynchronous methods with `Async` (e.g., `GetByIdAsync`).
- **Parameters & Local Variables**: camelCase (e.g., `cancellationToken`, `documentId`).
- **Private Fields**: Prefix with `_` and camelCase (e.g., `_unitOfWork`, `_logger`).
- **DTOs**: Suffix with `Dto` or `Request`/`Response` (e.g., `DocumentResponseDto`, `CreateDocumentRequestDto`).

---

## 5. Dependency Injection Guidelines
- Register services with proper lifetimes:
  - **Scoped**: Repositories, DbContext, Unit of Work, Application Services (`AddScoped`).
  - **Transient**: Stateless lightweight helpers or validators (`AddTransient`).
  - **Singleton**: Caches, thread-safe configuration objects (`AddSingleton`).
- Encapsulate registrations in each layer's `ServiceRegistration.cs` file using extension methods on `IServiceCollection`.

---

## 6. Language & Localization Standards
- **Code & Syntax**: Written strictly in **English** (class names, interfaces, methods, DTO properties, parameters, comments).
- **User-Facing & Exception Messages**: Written strictly in **Spanish**. Because the LexisCampus DMS frontend is in Spanish, all domain exception messages, FluentValidation messages, and user-facing API error feedback must be localized in clear, professional Spanish.
- **Error Codes**: Machine-readable error codes must remain in UPPERCASE_SNAKE_CASE English (e.g. `ErrorCode = "DOCUMENT_NOT_FOUND"`, `ErrorCode = "INVALID_DOCUMENT_STATE"`), paired with the Spanish message.

---

## 7. Kanban Workflow & Engineering Judgment
- **Kanban as a Functional Guide**: The Kanban cards define user intent and business needs ("what" to build). Never blindly copy-paste names or signatures from cards if they conflict with Clean Architecture, SOLID principles, or language standards.
- **Proactive Conflict Checking**: Before implementing any card, search the existing layers to prevent redundant interfaces, duplicate classes, or architectural boundary violations.
- **Continuous Improvement**: Improve, adapt, and refine signatures and designs along the way to maintain a clean, maintainable, and high-performance .NET 8 codebase.

