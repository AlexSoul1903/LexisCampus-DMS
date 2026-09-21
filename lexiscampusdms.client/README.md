# LexisCampus DMS — Frontend (React + Vite)

Interfaz web del Sistema de Gestión Documental Académica. Implementa la
*Especificación Técnica Frontend — DMS*: autenticación JWT con refresh token,
gestión documental con integridad SHA-256, línea de tiempo forense, expedientes
estudiantiles, portal público de verificación y administración de usuarios.

## Stack

| Área | Tecnología |
|---|---|
| UI | React 19 + Vite |
| Enrutamiento | `react-router-dom` v7 (rutas protegidas por rol) |
| HTTP | `axios` con interceptores (Bearer + refresh token con cola) |
| Iconografía | `lucide-react` |
| Notificaciones | `sonner` |
| Escaneo QR | `jsqr` |
| Estilos | Sistema de diseño propio en `src/index.css` (Inter + Outfit, paleta institucional `#1E3A8A`) |

## Requisitos

- Node.js 20+ y npm 10+
- Backend `LexisCampusDMS.Server` en ejecución (ver README raíz)

## Puesta en marcha

```bash
cd lexiscampusdms.client
npm install
npm run dev
```

La app queda en `https://localhost:5173` y todas las llamadas a `/api` se
redirigen por proxy al backend.

### Variables de entorno (opcionales)

Cree un archivo `.env.development.local` (no se versiona) para ajustar el entorno:

| Variable | Por defecto | Uso |
|---|---|---|
| `VITE_API_TARGET` | `http://localhost:5078` | URL del backend para el proxy de `/api`. |
| `VITE_DEV_HTTPS` | `true` | `false` sirve Vite por HTTP (evita el certificado de desarrollo). |

Si se ejecuta desde Visual Studio con el perfil HTTPS, el proxy usa
automáticamente `ASPNETCORE_HTTPS_PORT`.

### Scripts

| Script | Descripción |
|---|---|
| `npm run dev` | Servidor de desarrollo con HMR |
| `npm run build` | Compilación de producción en `dist/` |
| `npm run preview` | Sirve la compilación de producción |
| `npm run lint` | Análisis estático con ESLint |

## Usuarios de prueba (seed del backend)

| Usuario | Contraseña | Rol |
|---|---|---|
| `admin` | `Admin123@` | Admin |
| `registro` | `Registro123@` | Registro |
| `auditor` | `Auditor123@` | Auditor |

## Estructura

```
src/
├── App.jsx                 # Mapa de rutas y permisos (carga diferida por vista)
├── main.jsx                # Router, AuthProvider y Toaster
├── index.css               # Sistema de diseño (tokens, componentes, layout)
├── config/constants.js     # Roles, tipos y estados de documento (enums del backend)
├── context/AuthContext.jsx # user, role, isAuthenticated, hasRole, login, logout
├── routes/ProtectedRoute.jsx
├── layouts/AppLayout.jsx   # Shell: sidebar filtrada por rol + header
├── services/               # Capa de API
│   ├── apiClient.js        # axios, Bearer e interceptor de refresh con cola
│   ├── authService.js
│   ├── documentService.js
│   ├── publicVerificationService.js
│   ├── userService.js
│   └── tokenStorage.js     # localStorage ("recordar sesión") o sessionStorage
├── hooks/useFileHash.js    # Selección de archivo + SHA-256 en el cliente
├── components/
│   ├── ui/                 # Button, Input, Select, Modal, Badge, Skeleton, Pagination…
│   ├── documents/          # Dropzone, visor con marca de agua, timeline, modales
│   ├── users/              # Formulario de usuario, chip de rol
│   └── verification/       # Escáner QR (cámara o imagen)
├── pages/                  # Vistas
└── utils/                  # Formato, archivos/hash, errores de API
```

## Rutas

| Ruta | Acceso | Vista |
|---|---|---|
| `/login` | Público | Inicio de sesión con aviso de bloqueo tras 5 intentos |
| `/verify`, `/verify/:hash` | Público | Verificación por hash, QR o archivo (verde / rojo / gris) |
| `/dashboard` | Todos | Métricas y actividad reciente |
| `/documents` | Todos | Explorador con filtros por matrícula, tipo y fechas |
| `/documents/upload` | Admin, Registro | Carga guiada: dropzone → metadatos → hash SHA-256 |
| `/documents/:id` | Todos | Visor con marca de agua, línea de tiempo forense, auditoría, QR |
| `/students`, `/students/:matricula` | Todos | Hub de expedientes y exportación ZIP |
| `/admin/users` | Admin | Usuarios, roles, activación y reseteo de bloqueo |
| `/profile` | Todos | Perfil y permisos del rol |
