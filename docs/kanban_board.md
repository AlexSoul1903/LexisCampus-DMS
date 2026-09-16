# LexisCampus DMS - Kanban Board & Sprint Tracking

## 📌 Sprint 1: Fase 1 - Núcleo del Sistema (Domain & Clean Architecture)

> [!NOTE]
> **Kanban as a Guide**: User stories and acceptance criteria define the functional requirements ("what" to build). Code must always be adapted, improved, and checked for conflicts across layers, adhering strictly to 100% English code (with only user-facing error messages in Spanish).

---

### [LEX-101] Modelar Entidades del Dominio Núcleo
- **Capa Onion**: `Domain (Core - Centro)`
- **Fase / Sprint**: `Fase 1: Núcleo del Sistema`
- **Estado**: `Done` ✅
- **Prioridad**: `Crítica`
- **Story Points (Fibonacci)**: `5`
- **Asignado a**: `Backend Architect (.NET 8) — Core / API`
- **Declaración de Historia de Usuario**:
  > *Como Arquitecto de Software, quiero definir las entidades base (Document, DocumentVersion, AuditLog) en el proyecto LexisCampus.Domain sin dependencias de Entity Framework ni librerías externas, para garantizar la pureza de la Arquitectura Onion.*
- **Notas Técnicas & Especificaciones de Arquitectura**:
  > *Proyecto: LexisCampus.Domain. Evitar cualquier paquete NuGet de terceros. Utilizar tipos nativos de C# 12 / .NET 8 (records, value objects cuando aplique).*
- **Criterios de Aceptación (Definition of Done)**:
  - [x] Crear entidad `Document` (`Id`, `Title`, `StudentRegistration` / `Matrícula`, `DocumentType`, `Status`, `CurrentVersion`, `CreatedAt`, `CreatedBy`, `Versions`, `AuditLogs`).
  - [x] Crear entidad `DocumentVersion` (`Id`, `DocumentId`, `VersionNumber`, `StoragePath`, `FileHashSha256`, `FileSize`, `MimeType`, `CreatedAt`, `CreatedByUserId`).
  - [x] Crear entidad `AuditLog` (`Id`, `UserId`, `Action`, `DocumentId`, `TimestampUtc`, `IpAddress`, `Details`).
  - [x] Definir enums: `DocumentType` (Transcript, BirthCertificate, Accreditation, StudyCertificate, Degree, Other), `DocumentStatus`, `UserRole`, `AuditAction`.
  - [x] Implementar excepciones de dominio específicas (`InvalidDocumentStateException`, `FileHashMismatchException`, `DomainValidationException`, `EntityNotFoundException`).
  - [x] Convención de Idiomas: Código en Inglés, Excepciones y Mensajes de Usuario en Español con `ErrorCode`.

---

### [LEX-102] Definir Contratos e Interfaces en Application
- **Capa Onion**: `Application (Casos de Uso)`
- **Fase / Sprint**: `Fase 1: Núcleo del Sistema`
- **Estado**: `Done` ✅
- **Prioridad**: `Alta`
- **Story Points (Fibonacci)**: `3`
- **Asignado a**: `Backend Architect (.NET 8) — Core / API`
- **Declaración de Historia de Usuario**:
  > *Como Desarrollador Backend, quiero definir las interfaces para repositorios, almacenamiento de streams y cálculo de hash en LexisCampus.Application, para establecer los contratos que implementará la infraestructura desacoplada.*
- **Notas Técnicas & Especificaciones de Arquitectura**:
  > *Proyecto: LexisCampus.Application. Interfaces puras. Solo referencia a LexisCampus.Domain.*
- **Criterios de Aceptación (Definition of Done)**:
  - [x] `IStorageService`: `UploadFileAsync(Stream fileStream, string storagePath, string contentType, CancellationToken cancellationToken = default)`
  - [x] `IStorageService`: `GetFileStreamAsync(string storagePath, CancellationToken cancellationToken = default)` y `DeleteFileAsync(string storagePath, CancellationToken cancellationToken = default)`
  - [x] `IDocumentRepository` (en `Core.Domain.Interfaces`): `RegisterDocumentAsync`, `GetByStudentRegistrationAsync`, `GetVersionsAsync`, `GetWithDetailsAsync`, `GetByStatusAsync` (heredando de `IGenericRepository<Document, Guid>`)
  - [x] `IHashService`: `ComputeSha256(Stream fileStream)` devolviendo el hash hexadecimal
  - [x] `ICurrentUserService`: `UserId`, `Role`, `IpAddress`, `IsAuthenticated` de la solicitud actual

---

### [LEX-103] Disponer de DTOs Tipados y Validaciones en Application
- **Capa Onion**: `Application (Casos de Uso)`
- **Fase / Sprint**: `Fase 1: Núcleo del Sistema`
- **Estado**: `Done` ✅
- **Prioridad**: `Media`
- **Story Points (Fibonacci)**: `2`
- **Asignado a**: `Backend Architect (.NET 8) — Core / API`
- **Declaración de Historia de Usuario**:
  > *Como Desarrollador API, quiero disponer de DTOs tipados en la capa Application, para aislar las entidades de dominio de las peticiones HTTP y respuestas REST.*
- **Notas Técnicas & Especificaciones de Arquitectura**:
  > *Definir DTOs inmutables en LexisCampus.Application.DTOs con validaciones FluentValidation opcionales.*
- **Criterios de Aceptación (Definition of Done)**:
  - [x] `UploadDocumentRequestDto`: (`Title`, `StudentRegistration`, `DocumentType`, `InitialComment`, `FileStream`, `FileName`, `ContentType`, `FileSizeBytes`)
  - [x] `DocumentResponseDto`: (`Id`, `Title`, `StudentRegistration`, `DocumentType`, `Status`, `CurrentVersion`, `CreatedAtUtc`, `CurrentFileHash`)
  - [x] `DocumentVersionDto`: (`VersionNumber`, `FileHash`, `FileSizeBytes`, `CreatedAtUtc`, `UploadedBy`, `MimeType`, `StoragePath`)
  - [x] `SearchFilterDto`: (`StudentRegistration`, `DocumentType`, `FromDateUtc`, `ToDateUtc`, `PageNumber`, `PageSize`)
  - [x] Validaciones FluentValidation con mensajes en español y registradas en DI (`UploadDocumentRequestDtoValidator`, `SearchFilterDtoValidator`)

---

## 📋 Backlog (Próximas Historias)

| ID | Título | Capa Onion | Prioridad | Estado |
|---|---|---|---|---|
| **LEX-104** | Configuración EF Core, ApplicationDbContext & Repositorios Genéricos | `Persistence` | Crítica | To Do |
| **LEX-105** | Implementación de Servicio Genérico & Servicio de Documentos | `Application` | Crítica | To Do |
| **LEX-106** | Proveedor de Almacenamiento Seguro de Archivos (Local/Cloud) | `Shared` | Media | To Do |
| **LEX-107** | Controladores API REST & Middleware de Excepciones Globales | `Server` | Crítica | To Do |
| **LEX-108** | Autenticación JWT, Refresh Tokens & Políticas de Autorización | `Server / Persistence` | Crítica | To Do |
