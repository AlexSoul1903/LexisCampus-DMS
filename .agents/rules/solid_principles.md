# SOLID Principles Enforcement

Every class, interface, and service in this codebase must strictly adhere to the **SOLID** principles.

---

## 1. Single Responsibility Principle (SRP)
> *"A class should have one, and only one, reason to change."*

### Application Rules:
- **Controllers** have one responsibility: accept HTTP requests, validate request model state, delegate to application services, and return appropriate HTTP status codes.
- **Application Services** handle business logic and orchestrate domain entities and repositories for a specific use case or entity.
- **Repositories** handle database querying, persistence, and data mapping for a single entity or aggregate.
- **Validators** (e.g., FluentValidation) validate incoming data contracts; do not mix validation logic inside controllers or database queries.
- **DTOs** are simple data carriers; do not put business calculation or persistence methods on DTOs.
- **Domain Entities** represent business models and encapsulate domain invariants and business rules specific to the entity.

### Example:
❌ **Bad**:
```csharp
public class DocumentController : ControllerBase
{
    // Violation: Controller handles validation, hashing, database queries, and email sending
    [HttpPost]
    public async Task<IActionResult> Create(DocumentDto dto)
    {
        if (string.IsNullOrEmpty(dto.Title)) return BadRequest();
        var entity = new Document { Title = dto.Title };
        _context.Documents.Add(entity);
        await _context.SaveChangesAsync();
        await _smtpClient.SendEmailAsync(...);
        return Ok(entity);
    }
}
```

✔️ **Good**:
```csharp
public class DocumentController : ControllerBase
{
    private readonly IDocumentService _documentService;
    public DocumentController(IDocumentService documentService) => _documentService = documentService;

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateDocumentDto dto, CancellationToken ct)
    {
        var result = await _documentService.CreateAsync(dto, ct);
        return result.IsSuccess ? CreatedAtAction(nameof(GetById), new { id = result.Data.Id }, result) : BadRequest(result);
    }
}
```

---

## 2. Open/Closed Principle (OCP)
> *"Software entities should be open for extension, but closed for modification."*

### Application Rules:
- Use **interfaces** and **abstract base classes** to allow new behaviors to be added via new implementations (e.g., adding a new file storage provider like `S3FileStorageService` or `AzureBlobStorageService` implementing `IFileStorageService` without modifying existing caller code).
- Use **Strategy pattern** or **Decorators** (e.g., caching decorators, validation behaviors) rather than large `switch`/`if-else` chains.

---

## 3. Liskov Substitution Principle (LSP)
> *"Objects of a superclass should be replaceable with objects of its subclasses without breaking application behavior."*

### Application Rules:
- Implementations of `IGenericRepository<TEntity, TId>` and `IGenericService<TDto, ...>` must fulfill the complete interface contract without throwing `NotImplementedException` or changing the expected semantic behavior.
- Subclasses must not tighten preconditions or loosen postconditions.

---

## 4. Interface Segregation Principle (ISP)
> *"Clients should not be forced to depend on methods they do not use."*

### Application Rules:
- Favor small, cohesive, focused interfaces over bloated "god" interfaces.
- Separate read operations and write operations if needed (`IReadRepository<T>`, `IWriteRepository<T>`), or create domain-specific specialized interfaces (e.g., `IDocumentVersionRepository : IGenericRepository<DocumentVersion, Guid>`).
- Do not add unnecessary utility methods to general interfaces.

---

## 5. Dependency Inversion Principle (DIP)
> *"High-level modules should not depend on low-level modules. Both should depend on abstractions. Abstractions should not depend on details. Details should depend on abstractions."*

### Application Rules:
- Controllers and Services must depend **strictly on interfaces** (`IDocumentRepository`, `IEmailService`, `IUnitOfWork`), never on concrete classes (`DocumentRepository`, `SendGridEmailService`, `ApplicationDbContext`).
- All dependencies must be injected via constructor injection with `readonly` fields.
- Concrete implementations are wired in the respective `ServiceRegistration.cs` extension methods.
