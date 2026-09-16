# Security Guidelines for API & Application

Security is a first-class requirement. All code generated for this solution must adhere to the following security standards and OWASP Top 10 guidelines.

---

## 1. Authentication & Authorization

### JWT Bearer Authentication
- Use standard JWT Bearer tokens with short lifespans (15–60 minutes).
- Implement Refresh Token rotation with cryptographic hashing stored in the database.
- Validate:
  - `ValidateIssuer = true`
  - `ValidateAudience = true`
  - `ValidateLifetime = true`
  - `ValidateIssuerSigningKey = true`
  - `ClockSkew = TimeSpan.Zero`

### Authorization Enforcements
- Protect endpoints with `[Authorize]` by default. Use `[AllowAnonymous]` only when explicitly required (e.g., login, register).
- Use **Policy-based** or **Role-based** authorization:
  ```csharp
  [Authorize(Roles = "Admin,SuperUser")]
  [Authorize(Policy = "CanManageDocuments")]
  ```
- Always extract user identity from `HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)` or custom claims—**NEVER** trust client-supplied user IDs in request bodies for authorization checks.

---

## 2. Input Validation & Mass Assignment Prevention

### FluentValidation
- Every incoming request DTO must have a dedicated FluentValidation validator.
- Validate string lengths, allowed formats, required fields, and ranges before processing.

### Over-Posting / Mass Assignment Defense
- **NEVER bind Domain Entities directly to Controller endpoints**.
- Always use separate `Create...Dto` and `Update...Dto` with explicit properties to prevent parameter tampering.

```csharp
// ❌ Dangerous (Mass assignment vulnerability)
[HttpPost]
public async Task<IActionResult> Create(User user) { ... }

// ✔️ Secure
[HttpPost]
public async Task<IActionResult> Create([FromBody] CreateUserRequestDto request) { ... }
```

---

## 3. SQL Injection Prevention & ORM Safety

- Use EF Core LINQ queries which automatically generate parameterized SQL.
- If raw SQL (`FromSqlRaw`, `ExecuteSqlRaw`) is ever necessary, **ALWAYS use parameterized queries** (`FromSqlInterpolated` or `DbParameter`), never string concatenation.

```csharp
// ❌ Dangerous (SQL Injection)
var user = await _context.Users.FromSqlRaw($"SELECT * FROM Users WHERE Email = '{email}'").ToListAsync();

// ✔️ Secure (Parameterized)
var user = await _context.Users.FromSqlInterpolated($"SELECT * FROM Users WHERE Email = {email}").ToListAsync();
```

---

## 4. Password Hashing & Sensitive Data Protection

- **Password Hashing**: Use `BCrypt.Net-Next`, `Argon2`, or `Microsoft.AspNetCore.Identity.IPasswordHasher<T>`. Never use MD5, SHA1, or plain SHA256 without salt and key derivation.
- **Secrets Management**:
  - Never commit API keys, connection strings, or JWT secret keys in source control or `appsettings.json`.
  - Use `UserSecrets` in development and Environment Variables or Azure Key Vault in production.
- **Sensitive Output**: Strip passwords, hash salts, and internal tokens from all response DTOs.

---

## 5. Security Headers & Middleware Pipeline

In `Program.cs` / API Middleware pipeline, enforce:
- **HSTS (HTTP Strict Transport Security)** in production (`app.UseHsts()`).
- **HTTPS Redirection** (`app.UseHttpsRedirection()`).
- **Security Headers**:
  - `X-Content-Type-Options: nosniff`
  - `X-Frame-Options: DENY` or `SAMEORIGIN`
  - `Content-Security-Policy (CSP)`
  - `Referrer-Policy: strict-origin-when-cross-origin`
  - `Permissions-Policy`

---

## 6. Rate Limiting & Denial of Service Defense

- Enable ASP.NET Core 8 built-in rate limiting (`Microsoft.AspNetCore.RateLimiting`):
  ```csharp
  builder.Services.AddRateLimiter(options =>
  {
      options.AddFixedWindowLimiter("api-policy", opt =>
      {
          opt.Window = TimeSpan.FromMinutes(1);
          opt.PermitLimit = 100;
          opt.QueueLimit = 10;
          opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
      });
  });
  ```
- Apply rate limiting to sensitive endpoints (e.g. `/api/auth/login`, `/api/auth/forgot-password`).

---

## 7. CORS Configuration

- Define strict, explicit CORS policies.
- Do NOT use `.AllowAnyOrigin()` together with `.AllowCredentials()`.
- Use specific allowed origins per environment.

---

## 8. Secure Error Handling & Information Leakage Prevention

- Never expose raw stack traces, database schema details, or exception messages in production.
- Use a Global Exception Handling Middleware that transforms uncaught exceptions into standard **RFC 7807 `ProblemDetails`** responses:
  ```json
  {
    "type": "https://tools.ietf.org/html/rfc7231#section-6.6.1",
    "title": "An error occurred while processing your request.",
    "status": 500,
    "instance": "/api/documents/123",
    "traceId": "00-123456789abcdef..."
  }
  ```
- In development, detailed errors may be logged, but sanitized before returning to the client.
