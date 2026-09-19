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

### [LEX-302] Caso de Uso: Actualizar / Rectificar Documento (Nueva Versión)
- **Capa Onion**: `Application (Casos de Uso) / Persistence`
- **Fase / Sprint**: `Fase 3: Casos de Uso`
- **Estado**: `Done` ✅
- **Prioridad**: `Alta`
- **Story Points (Fibonacci)**: `5`
- **Asignado a**: `Backend Architect (.NET 8) — Core / API`
- **Declaración de Historia de Usuario**:
  > *Como Supervisor de Registro, quiero actualizar un documento existente cargando un archivo rectificado, para generar una nueva versión (v2, v3) conservando íntegro el historial previo.*
- **Notas Técnicas & Especificaciones de Arquitectura**:
  > *Garantizar inmutabilidad: ninguna fila previa de DocumentVersion se modifica ni se borra jamás. Índice único (DocumentId, VersionNumber) en base de datos para concurrencia segura. Transacción atómica con rollback y compensación de borrado en MinIO ante fallos de persistencia.*
- **Criterios de Aceptación (Definition of Done)**:
  - [x] Validar existencia previa del Documento por ID o matrícula + tipo (`GetWithDetailsAsync` y `GetByRegistrationAndTypeWithDetailsAsync`)
  - [x] Incrementar VersionNumber de forma segura y concurrente (ej. v1 -> v2) respaldado por índice único en SQL Server
  - [x] Subir nuevo binario a MinIO y registrar DocumentVersion enlazada con compensación en caso de fallo
  - [x] Actualizar puntero CurrentVersion en la entidad Document
  - [x] Registrar evento en AuditLog "DOCUMENT_RECTIFIED" (`AuditAction.DocumentRectified`) con motivo de cambio obligatorio

### [LEX-303] Endpoints REST en DocumentsController
- **Capa Onion**: `Presentation (Server) / Application / Persistence`
- **Fase / Sprint**: `Fase 3: Casos de Uso`
- **Estado**: `Done` ✅
- **Prioridad**: `Crítica`
- **Story Points (Fibonacci)**: `5`
- **Asignado a**: `Backend Architect (.NET 8) — Core / API`
- **Declaración de Historia de Usuario**:
  > *Como Desarrollador Frontend, quiero una API REST limpia y documentada en Swagger, para poder subir, buscar, consultar versiones y descargar expedientes académicos.*
- **Notas Técnicas & Especificaciones de Arquitectura**:
  > *Controlador DocumentsController con [ApiController], [Route("api/[controller]")]. Búsqueda paginada con PagedResult<T>, descarga con Content-Disposition inline/attachment, historial completo de versiones con hashes, y Swagger UI documentado con esquemas y comentarios XML.*
- **Criterios de Aceptación (Definition of Done)**:
  - [x] `POST /api/documents/upload`: Carga multipart con metadatos y retorno 201 Created con cabecera Location
  - [x] `GET /api/documents/search?matricula={val}&tipo={val}`: Búsqueda paginada con filtros (soporte alias español/inglés y rangos de fecha)
  - [x] `GET /api/documents/{id}/download`: Descarga de stream binario con Content-Disposition inline o attachment
  - [x] `GET /api/documents/{id}/versions`: Listado completo del historial de versiones y hashes criptográficos
  - [x] Swagger UI configurado con esquemas de request/response, descripciones y comentarios XML funcionales

### [LEX-304] Middleware de Autenticación JWT y Auditoría Automática
- **Capa Onion**: `Presentation (Server) / Application / Persistence`
- **Fase / Sprint**: `Fase 3: Casos de Uso (Seguridad y Auditoría)`
- **Estado**: `Done` ✅
- **Prioridad**: `Crítica`
- **Story Points (Fibonacci)**: `3`
- **Asignado a**: `Backend Architect (.NET 8) — Core / API`
- **Declaración de Historia de Usuario**:
  > *Como Administrador de Seguridad, quiero que la API valide tokens JWT Bearer, extraiga la identidad e IP del usuario y audite automáticamente todas las consultas y descargas de documentos en la tabla AuditLogs, para garantizar la trazabilidad y la integridad de los accesos a los expedientes.*
- **Notas Técnicas & Especificaciones de Arquitectura**:
  > *Autenticación JWT Bearer en Program.cs con validación completa (Issuer, Audience, Lifetime, Signing Key). Configuración de Swagger con esquema Bearer. CurrentUserService en Server implementando ICurrentUserService para extraer sub, name, roles y cabecera X-Forwarded-For. AuditLogMiddleware registrando acciones Viewed y Downloaded con UnitOfWork en la base de datos SQL Server. Roles restringidos [Authorize(Roles = "Registro,Admin,Auditor")] y mutaciones para [Authorize(Roles = "Registro,Admin")].*
- **Criterios de Aceptación (Definition of Done)**:
  - [x] Configurar autenticación JWT Bearer en `Program.cs` con validación de Issuer, Audience, Lifetime y Signing Key.
  - [x] Implementar `CurrentUserService` extrayendo Claims (`Sub`, `Role`/`Roles`, `Name`) y capturando IP real (`X-Forwarded-For`).
  - [x] Middleware personalizado `AuditLogMiddleware` para registrar eventos críticos (consultas/visualizaciones `Viewed` y descargas `Downloaded`) en la tabla `AuditLogs`.
  - [x] Políticas de autorización `[Authorize(Roles = "Registro,Admin,Auditor")]` en `DocumentsController` con roles restringidos para mutación (`Registro,Admin`).

---

### [LEX-305] Controlador de Autenticación y Emisión de Tokens JWT
- **Capa Onion**: `Presentation (Server) / Application / Shared / Persistence / Domain`
- **Fase / Sprint**: `Fase 3: Casos de Uso (Autenticación y Seguridad)`
- **Estado**: `Done` ✅
- **Prioridad**: `Crítica`
- **Story Points (Fibonacci)**: `5`
- **Asignado a**: `Backend Architect (.NET 8) — Core / API`
- **Declaración de Historia de Usuario**:
  > *Como Usuario de Registro o Administrador, quiero autenticarme mediante POST /api/auth/login con mis credenciales institucionales, para obtener un token JWT firmado y operar en el DMS según mis roles asignados.*
- **Notas Técnicas & Especificaciones de Arquitectura**:
  > *Secret key firmada con HMAC-SHA256 almacenada en appsettings / UserSecrets. Entidades User y RefreshToken en Core.Domain con soporte para rotación e invalidación de tokens. Verificación criptográfica con BCrypt (work factor 11). Mitigación de fuerza bruta con bloqueo temporal automático tras 5 intentos fallidos consecutivos durante 15 minutos. Controlador AuthController documentado en Swagger con endpoints /login y /refresh-token.*
- **Criterios de Aceptación (Definition of Done)**:
  - [x] Crear endpoint `POST /api/auth/login` aceptando `LoginRequestDto` (Username/Email y Password)
  - [x] Verificación criptográfica de contraseña (`BCrypt` / `IPasswordHasherService`)
  - [x] Generar token JWT con claims (`UserId`, `Name`, `Role`, `Matrícula`/`Dept`) y tiempo de expiración (60 min)
  - [x] Implementar endpoint `POST /api/auth/refresh-token` para renovación segura de sesión
  - [x] Mitigación de ataques de fuerza bruta con bloqueo temporal tras 5 intentos fallidos consecutivos

---

## 📋 Backlog (Próximas Historias)

| ID | Título | Capa Onion | Prioridad | Estado |
|---|---|---|---|---|
| **LEX-306** | Middleware de Excepciones Globales (RFC 7807) & Manejo de Errores | `Server` | Crítica | To Do |
| **LEX-307** | Endpoint de Registro / Alta de Usuarios y Gestión de Roles | `Server / Application` | Alta | To Do |
