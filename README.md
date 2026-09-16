# 🎓 LexisCampus DMS (Document Management System)

Sistema de Gestión Documental Universitario con Arquitectura Onion Estricta (Clean Architecture), .NET 8, EF Core 8, MinIO S3 y React.

---

## 🏛️ Arquitectura del Sistema

El proyecto implementa una arquitectura concéntrica desacoplada:

1. **`LexisCampusDMS.Core.Domain`**: Entidades del dominio (`Document`, `DocumentVersion`, `AuditLog`), enums, excepciones y contratos de repositorio. Cero dependencias externas.
2. **`LexisCampusDMS.Application`**: DTOs, validaciones con FluentValidation, contratos de servicios e interfaces de casos de uso. Depende únicamente de `Domain`.
3. **`LexisCampusDMS.Infraestructure.Persistence`**: `ApplicationDbContext`, configuraciones Fluent API, índices non-clustered, migraciones de Entity Framework Core 8 e implementaciones de repositorio.
4. **`LexisCampusDMS.Infraestructure.Shared`**: Proveedores externos de almacenamiento (MinIO S3 / Local), cálculo criptográfico de hashes (SHA-256) y servicios transversales.
5. **`LexisCampusDMS.Server`**: API Web RESTful en ASP.NET Core (.NET 8), middleware global de manejo de excepciones y documentación Swagger / OpenAPI.
6. **`lexiscampusdms.client`**: Frontend SPA en React / TypeScript.

---

## 🐳 Entorno de Infraestructura Local (Docker Compose)

El repositorio incluye un archivo [docker-compose.yml](file:///docker-compose.yml) para desplegar los servicios necesarios en cualquier entorno de desarrollo:

### Servicios Incluidos

| Servicio | Imagen | Puerto Host : Contenedor | Propósito |
|---|---|---|---|
| **`sqlserver`** | `mcr.microsoft.com/mssql/server:2022-latest` | `1433:1433` | Base de datos relacional SQL Server 2022 |
| **`minio`** (API) | `quay.io/minio/minio:latest` | `9000:9000` | API S3 para subida y descarga de archivos |
| **`minio`** (Console) | `quay.io/minio/minio:latest` | `9001:9001` | Consola Web administrativa de MinIO |

---

### Comandos de Ejecución

#### 1. Configurar variables de entorno
Copia la plantilla de variables de entorno:
```bash
cp .env.example .env
```

#### 2. Iniciar contenedores en segundo plano
```bash
docker compose up -d
```

#### 3. Verificar el estado y healthchecks
```bash
docker compose ps
```

#### 4. Ver logs en tiempo real
```bash
docker compose logs -f
```

#### 5. Detener los contenedores
```bash
docker compose down
```

> [!NOTE]
> Si deseas reiniciar completamente la base de datos y el almacenamiento de MinIO eliminando los volúmenes persistentes, ejecuta:
> ```bash
> docker compose down -v
> ```

---

## 🗄️ Credenciales por Defecto (Entorno Local)

### SQL Server 2022
- **Host**: `localhost,1433`
- **Usuario**: `sa`
- **Contraseña**: `LexisCampus@Pass19` (o la definida en `.env`)
- **Base de Datos**: `LexisCampusDMS`

### MinIO S3 Object Storage
- **Consola Web**: [http://localhost:9001](http://localhost:9001)
- **API Endpoint**: `http://localhost:9000`
- **Usuario Root**: `admin`
- **Contraseña Root**: `Admin@LexisCampus`
- **Bucket por defecto**: `lexiscampus-documents`

---

## 🚀 Ejecutar la Aplicación (.NET 8 API)

### 1. Aplicar Migraciones de Base de Datos
```bash
dotnet ef database update --project LexisCampusDMS.Infraestructure.Persistence --startup-project LexisCampusDMS.Server
```

### 2. Iniciar el Servidor API
```bash
dotnet run --project LexisCampusDMS.Server
```
* **Swagger UI**: [https://localhost:7147/swagger](https://localhost:7147/swagger) o [http://localhost:5068/swagger](http://localhost:5068/swagger)
