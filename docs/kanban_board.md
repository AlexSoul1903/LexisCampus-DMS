# LexisCampus DMS - Kanban Board & Sprint Tracking

## 📌 Sprint 1: Fase 1 - Núcleo del Sistema (Domain & Clean Architecture)

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
  - [x] Definir enums: `DocumentType` (RecordNotas, ActaNacimiento, Convalidacion), `DocumentStatus`, `UserRole`, `AuditAction`.
  - [x] Implementar excepciones de dominio específicas (`InvalidDocumentStateException`, `FileHashMismatchException`, `DomainValidationException`, `EntityNotFoundException`).
  - [x] Convención de Idiomas: Código en Inglés, Excepciones y Mensajes de Usuario en Español con `ErrorCode`.

---

## 📋 Backlog (Próximas Historias)

| ID | Título | Capa Onion | Prioridad | Estado |
|---|---|---|---|---|
| **LEX-102** | Contratos de Repositorio & Unit of Work en Dominio | `Domain` | Alta | To Do |
| **LEX-103** | Configuración EF Core, ApplicationDbContext & Repositorios Genéricos | `Persistence` | Crítica | To Do |
| **LEX-104** | DTOs de Documentos, Mappings & FluentValidation | `Application` | Alta | To Do |
| **LEX-105** | Implementación de Servicio Genérico & Servicio de Documentos | `Application` | Crítica | To Do |
| **LEX-106** | Proveedor de Almacenamiento Seguro de Archivos (Local/Cloud) | `Shared` | Media | To Do |
| **LEX-107** | Controladores API REST & Middleware de Excepciones Globales | `Server` | Crítica | To Do |
| **LEX-108** | Autenticación JWT, Refresh Tokens & Políticas de Autorización | `Server / Persistence` | Crítica | To Do |
