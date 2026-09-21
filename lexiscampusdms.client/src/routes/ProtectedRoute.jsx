import { Navigate, Outlet, useLocation } from 'react-router-dom';
import { useAuth } from '@/context/AuthContext';

/**
 * Protege rutas por autenticación y, opcionalmente, por rol.
 * - Sin sesión → /login (conservando la ruta de origen).
 * - Rol no permitido → /forbidden.
 */
export default function ProtectedRoute({ roles, children }) {
    const { isAuthenticated, hasRole } = useAuth();
    const location = useLocation();

    if (!isAuthenticated) {
        return <Navigate to="/login" replace state={{ from: location }} />;
    }

    if (roles?.length && !hasRole(roles)) {
        return <Navigate to="/forbidden" replace />;
    }

    return children ?? <Outlet />;
}
