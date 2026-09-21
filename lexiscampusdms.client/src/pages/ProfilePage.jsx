import { useMemo } from 'react';
import { KeyRound, LogOut, ShieldCheck, UserRound } from 'lucide-react';
import { Button, Card, PageHeader } from '@/components/ui';
import RoleChip from '@/components/users/RoleChip';
import { useAuth } from '@/context/AuthContext';
import { ROLES } from '@/config/constants';
import { tokenStorage } from '@/services/tokenStorage';
import { formatDateTime, initials } from '@/utils/format';

const PERMISSIONS = {
    [ROLES.ADMIN]: ['Control total del sistema', 'Gestión de usuarios, roles y bloqueos', 'Carga, rectificación y revocación de documentos', 'Consulta y exportación de expedientes'],
    [ROLES.REGISTRO]: ['Carga y certificación de documentos', 'Rectificación de versiones', 'Revocación con resolución legal', 'Consulta y exportación de expedientes'],
    [ROLES.AUDITOR]: ['Consulta y búsqueda de documentos', 'Revisión de líneas de tiempo forenses', 'Exportación de expedientes', 'Acceso de solo lectura']
};

export default function ProfilePage() {
    const { user, role, logout } = useAuth();
    const expiresAt = useMemo(() => tokenStorage.get()?.expiresAt, []);

    return (
        <div className="page">
            <PageHeader eyebrow="Cuenta" eyebrowIcon={UserRound} title="Mi perfil" description="Información de su identidad institucional y permisos asignados." />

            <div className="grid-3" style={{ alignItems: 'start' }}>
                <Card className="span-2">
                    <div className="row" style={{ gap: 16, marginBottom: 24 }}>
                        <span className="avatar avatar-lg">{initials(user?.fullName)}</span>
                        <div>
                            <h2>{user?.fullName}</h2>
                            <div className="row-wrap" style={{ marginTop: 6 }}>
                                <RoleChip role={role} />
                                <span className="muted text-sm">@{user?.username}</span>
                            </div>
                        </div>
                    </div>
                    <dl className="dl">
                        <dt>Correo institucional</dt><dd>{user?.email}</dd>
                        <dt>Departamento</dt><dd>{user?.department || '—'}</dd>
                        <dt>Matrícula asociada</dt><dd className="mono">{user?.studentRegistration || '—'}</dd>
                        <dt>Identificador</dt><dd className="mono text-sm">{user?.id}</dd>
                        <dt>Token vigente hasta</dt><dd>{expiresAt ? formatDateTime(new Date(expiresAt)) : '—'} <span className="muted text-sm">(se renueva automáticamente)</span></dd>
                    </dl>
                </Card>

                <div className="stack">
                    <Card title="Permisos del rol">
                        <ul className="stack-sm" style={{ margin: 0, padding: 0, listStyle: 'none' }}>
                            {(PERMISSIONS[role] ?? []).map(p => (
                                <li key={p} className="row text-sm"><ShieldCheck size={15} style={{ color: 'var(--success-600)' }} />{p}</li>
                            ))}
                        </ul>
                    </Card>
                    <Card title="Seguridad">
                        <div className="stack-sm">
                            <p className="muted text-sm row"><KeyRound size={15} />Para cambiar su contraseña contacte al administrador del sistema.</p>
                            <Button variant="danger" icon={LogOut} onClick={() => logout()}>Cerrar sesión</Button>
                        </div>
                    </Card>
                </div>
            </div>
        </div>
    );
}
