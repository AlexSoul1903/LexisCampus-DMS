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

### [LEX-201] Configurar SQL Server con Entity Framework Core 8
- **Capa Onion**: `Infrastructure (Persistence)`
- **Fase / Sprint**: `Fase 2: Infraestructura y Persistencia`
- **Estado**: `Done` ✅
- **Prioridad**: `Crítica`
- **Story Points (Fibonacci)**: `5`
- **Asignado a**: `DevOps & Storage Lead — MinIO / SQL Server`
- **Declaración de Historia de Usuario**:
  > *Como DevOps / DBA, quiero implementar ApplicationDbContext y configurar Entity Framework Core con SQL Server 2022, para asegurar persistencia relacional optimizada con índices de búsqueda rápida.*
- **Notas Técnicas & Especificaciones de Arquitectura**:
  > *Proyecto: LexisCampus.Infrastructure (LexisCampusDMS.Infraestructure.Persistence). Conexión SQL Server 2022 en appsettings.json. Mapeo explícito con IEntityTypeConfiguration<T>.*
- **Criterios de Aceptación (Definition of Done)**:
  - [x] Configurar `ApplicationDbContext` con `DbSets` para `Document`, `DocumentVersion` y `AuditLog`
  - [x] Configurar Fluent API para relaciones (1 `Document` -> N `DocumentVersion` en cascada, 1 `Document` -> N `AuditLog` en SetNull)
  - [x] Crear índices non-clustered para búsquedas de alto rendimiento: `StudentRegistration`, `DocumentType`, `CreatedAtUtc`, `(DocumentId, VersionNumber)` y `FileHashSha256`
  - [x] Generar migración inicial vía CLI: `dotnet ef migrations add InitialCreate`
  - [x] Conexión SQL Server 2022 en `appsettings.json` y `appsettings.Development.json`

---

### [LEX-893] Orquestación de Entorno de Desarrollo con Docker Compose (SQL Server 2022 & MinIO S3)
- **Capa Onion**: `Infrastructure (Shared / Persistence)`
- **Fase / Sprint**: `Fase 2: Infraestructura y Persistencia`
- **Estado**: `Done` ✅
- **Prioridad**: `Media`
- **Story Points (Fibonacci)**: `3`
- **Asignado a**: `DevOps & Storage Lead — MinIO / SQL Server`
- **Declaración de Historia de Usuario**:
  > *Como Desarrollador / DevOps, quiero un archivo docker-compose.yml que orqueste contenedores para SQL Server 2022 y MinIO S3 con volúmenes persistentes y redes aisladas, para levantar todo el entorno de infraestructura local de LexisCampus DMS con un único comando reproducible en cualquier equipo.*
- **Notas Técnicas & Especificaciones de Arquitectura**:
  > *Archivos creados en la raíz: docker-compose.yml, .env.example, README.md y exclusión en .gitignore. SQL Server 2022 en puerto 1433 con healthcheck sqlcmd, MinIO en puertos 9000 (API) y 9001 (Consola Web).*
- **Criterios de Aceptación (Definition of Done)**:
  - [x] Crear docker-compose.yml con los servicios de SQL Server 2022 y MinIO S3
  - [x] Configurar puertos estándar: 1433 (SQL Server), 9000 (MinIO API) y 9001 (MinIO Console)
  - [x] Configurar volúmenes locales persistentes para datos de base de datos y almacenamiento de objetos (`mssql_data`, `minio_data`)
  - [x] Configurar healthchecks para arranque controlado de dependencias
  - [x] Crear archivo .env.example con las variables de entorno base requeridas
  - [x] Documentar comandos de inicio y detención (docker compose up -d / down) en el README

---

### [LEX-202] Implementar Almacenamiento de Objetos con MinIO / S3
- **Capa Onion**: `Infrastructure (Shared / Adaptadores)`
- **Fase / Sprint**: `Fase 2: Infraestructura y Persistencia`
- **Estado**: `Done` ✅
- **Prioridad**: `Alta`
- **Story Points (Fibonacci)**: `5`
- **Asignado a**: `DevOps & Storage Lead — MinIO / SQL Server`
- **Declaración de Historia de Usuario**:
  > *Como Ingeniero de Almacenamiento, quiero implementar MinioStorageService conectando al bucket "lexiscampus-docs", para almacenar los archivos binarios de forma escalable con rutas jerárquicas seguras.*
- **Notas Técnicas & Especificaciones de Arquitectura**:
  > *SDK Minio 6.0.4 y Polly 8.4.2 en LexisCampusDMS.Infraestructure.Shared. Configuración en appsettings.json/Development bajo sección 'Minio'. Bucket 'lexiscampus-docs' provisionado. Tests unitarios e integración en LexisCampusDMS.UnitTests.*
- **Criterios de Aceptación (Definition of Done)**:
  - [x] Configurar contenedor Docker de MinIO y provisionar bucket institucional "lexiscampus-docs"
  - [x] Instalar SDK oficial MinIO en LexisCampus.Infrastructure (`LexisCampusDMS.Infraestructure.Shared`)
  - [x] Implementar MinioStorageService respetando el contrato IStorageService (`UploadFileAsync`, `GetFileStreamAsync`, `DeleteFileAsync`, `ExistsAsync`)
  - [x] Estructurar la jerarquía de rutas: `/{matricula}/{anio}/{hash}_{archivo}.pdf` con sanitización contra path traversal (`StoragePathBuilder`)
  - [x] Manejo resiliente de excepciones de conexión y reintentos exponenciales con `Polly` (200ms, 400ms, 800ms)

---

### [LEX-203] Implementar Servicio Criptográfico de Hashing SHA-256
- **Capa Onion**: `Infrastructure (Shared / Adaptadores)`
- **Fase / Sprint**: `Fase 2: Infraestructura y Persistencia`
- **Estado**: `Done` ✅
- **Prioridad**: `Alta`
- **Story Points (Fibonacci)**: `3`
- **Asignado a**: `Security & Cert Specialist — Crypto / QR`
- **Declaración de Historia de Usuario**:
  > *Como Oficial de Seguridad, quiero un servicio HashService que calcule el digest SHA-256 de cada stream binario, para certificar la inmutabilidad y detectar alteraciones en los expedientes.*
- **Notas Técnicas & Especificaciones de Arquitectura**:
  > *Garantizar que el stream de entrada mantenga su Position = 0 si requiere ser reutilizado para el upload a MinIO. Implementación en Sha256HashService respetando contrato IHashService.*
- **Criterios de Aceptación (Definition of Done)**:
  - [x] Implementar HashService utilizando `System.Security.Cryptography.SHA256` (`Sha256HashService`)
  - [x] Cálculo no bloqueante sobre Stream sin cargar el archivo completo en memoria RAM (`ComputeSha256Async` con stream buffering y preservación de `Position = 0`)
  - [x] Retorno de hash en formato Hexadecimal en minúsculas estándar (64 caracteres) (`Convert.ToHexString().ToLowerInvariant()`)
  - [x] Método de verificación que compare stream descargado vs hash persistido en base de datos (`VerifySha256` y `VerifySha256Async`)

---

## 📌 Sprint 2: Fase 3 - Casos de Uso (Application & Persistence Orchestration)

### [LEX-301] Caso de Uso: Carga de Documento con Versionado Inicial
- **Capa Onion**: `Application (Casos de Uso) / Persistence`
- **Fase / Sprint**: `Fase 3: Casos de Uso`
- **Estado**: `Done` ✅
- **Prioridad**: `Crítica`
- **Story Points (Fibonacci)**: `5`
- **Asignado a**: `Backend Architect (.NET 8) — Core / API`
- **Declaración de Historia de Usuario**:
  > *Como Analista de Registro, quiero subir un nuevo documento adjuntando la matrícula y tipo, para que el sistema calcule su hash, lo guarde en MinIO y registre la versión v1 en la base de datos.*
- **Notas Técnicas & Especificaciones de Arquitectura**:
  > *Servicio de Aplicación DocumentService con transacción atómica (Unit of Work) y compensación ante errores. Repositorios GenericRepository, DocumentRepository y UnitOfWork en Persistence. Controlador DocumentsController con endpoint multipart/form-data. Envoltorio Result<T> en Application.Common.*
- **Criterios de Aceptación (Definition of Done)**:
  - [x] Validar archivo multipart (`IFormFile`) y metadatos requeridos (matrícula válida, tipo admitido) (`UploadDocumentRequestDtoValidator` con FluentValidation)
  - [x] Calcular hash SHA-256 sobre el stream entrante (`IHashService.ComputeSha256Async` sin bloquear RAM y preservando `Position = 0`)
  - [x] Subir el stream binario a MinIO y obtener la ruta persistida (`IStorageService.UploadFileAsync` con jerarquía estructurada `/{matricula}/{anio}/{hash}_{archivo}.pdf`)
  - [x] Crear registro transaccional en SQL Server (`Document` + `DocumentVersion` número 1) vía `IUnitOfWork`
  - [x] Registrar entrada en `AuditLog` con acción "DOCUMENT_UPLOADED" (`AuditAction.DocumentUploaded`), UserId e IP de la petición

---

## 📋 Backlog (Próximas Historias)

| ID | Título | Capa Onion | Prioridad | Estado |
|---|---|---|---|---|
| **LEX-302** | Caso de Uso: Carga de Nuevas Versiones de Documento (v2+) | `Application` | Alta | To Do |
| **LEX-303** | Caso de Uso: Búsqueda y Filtrado Paginado de Expedientes | `Application` | Alta | To Do |
| **LEX-304** | Controladores API REST & Middleware de Excepciones Globales (RFC 7807) | `Server` | Crítica | To Do |
| **LEX-305** | Autenticación JWT, Refresh Tokens & Políticas de Autorización | `Server / Persistence` | Crítica | To Do |
