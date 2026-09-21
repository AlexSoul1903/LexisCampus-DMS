import { Link } from 'react-router-dom';
import { ArrowLeft, ShieldX } from 'lucide-react';
import { useAuth } from '@/context/AuthContext';

export function ForbiddenPage() {
    return (
        <div className="page">
            <div className="empty" style={{ paddingTop: 80 }}>
                <div className="empty-icon" style={{ background: 'var(--danger-50)', color: 'var(--danger-600)' }}><ShieldX /></div>
                <h2>Acceso denegado</h2>
                <p>Su rol no tiene permisos para acceder a esta sección. Si considera que es un error, contacte al administrador del sistema.</p>
                <Link to="/dashboard" className="btn btn-primary" style={{ marginTop: 12 }}><ArrowLeft />Volver al panel</Link>
            </div>
        </div>
    );
}

export function NotFoundPage() {
    const { isAuthenticated } = useAuth();
    return (
        <div className="center-screen">
            <div className="empty">
                <div className="error-code">404</div>
                <h2>Página no encontrada</h2>
                <p>La dirección solicitada no existe o fue movida.</p>
                <Link to={isAuthenticated ? '/dashboard' : '/login'} className="btn btn-primary" style={{ marginTop: 12 }}>
                    <ArrowLeft />{isAuthenticated ? 'Ir al panel' : 'Ir al inicio de sesión'}
                </Link>
            </div>
        </div>
    );
}
