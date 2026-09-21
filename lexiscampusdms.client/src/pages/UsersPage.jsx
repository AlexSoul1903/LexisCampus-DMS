import { useCallback, useEffect, useRef, useState } from 'react';
import { toast } from 'sonner';
import {
    Eye, FilterX, KeyRound, Lock, MoreHorizontal, Search, ShieldUser, UserCheck, UserPlus,
    UserRoundPen, UserX, Users
} from 'lucide-react';
import {
    Badge, Button, EmptyState, IconButton, Input, Modal, PageHeader, Pagination, Select, TableSkeleton
} from '@/components/ui';
import RoleChip from '@/components/users/RoleChip';
import UserFormModal from '@/components/users/UserFormModal';
import { useAuth } from '@/context/AuthContext';
import { ROLE_LABELS, ROLE_OPTIONS } from '@/config/constants';
import { userService } from '@/services/userService';
import { formatDate, formatDateTime, initials } from '@/utils/format';
import { getErrorMessage } from '@/utils/errors';

const STATUS_OPTIONS = [
    { value: 'true', label: 'Activos' },
    { value: 'false', label: 'Inactivos' }
];

export default function UsersPage() {
    const { user: me } = useAuth();
    const [filters, setFilters] = useState({ searchTerm: '', role: '', isActive: '', pageNumber: 1, pageSize: 10 });
    const [searchInput, setSearchInput] = useState('');
    const [result, setResult] = useState({ data: [], totalCount: 0 });
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState(null);
    const [modal, setModal] = useState(null); // { type, user }

    useEffect(() => {
        const t = setTimeout(() => setFilters(f => (f.searchTerm === searchInput ? f : { ...f, searchTerm: searchInput, pageNumber: 1 })), 350);
        return () => clearTimeout(t);
    }, [searchInput]);

    const load = useCallback(async () => {
        setLoading(true);
        setError(null);
        try {
            const res = await userService.list(filters);
            setResult({ data: res.data ?? [], totalCount: res.totalCount ?? 0 });
        } catch (err) {
            setError(getErrorMessage(err, 'No se pudo cargar el padrón de usuarios.'));
        } finally {
            setLoading(false);
        }
    }, [filters]);

    useEffect(() => { load(); }, [load]);

    const setFilter = key => e => setFilters(f => ({ ...f, [key]: e.target.value, pageNumber: 1 }));
    const hasFilters = filters.searchTerm || filters.role || filters.isActive;

    const replaceUser = updated => {
        if (!updated) return load();
        setResult(r => ({ ...r, data: r.data.map(u => (u.id === updated.id ? updated : u)) }));
    };

    const changeStatus = async (u, { isActive, resetLockout }) => {
        try {
            const updated = await userService.changeStatus(u.id, { isActive, resetLockout });
            replaceUser(updated);
            toast.success(
                resetLockout ? 'Bloqueo restablecido' : isActive ? 'Usuario activado' : 'Usuario desactivado',
                { description: u.fullName }
            );
        } catch (err) {
            toast.error('No se pudo actualizar el estado', { description: getErrorMessage(err) });
        }
    };

    return (
        <div className="page">
            <PageHeader
                eyebrow="Administración · LEX-309"
                eyebrowIcon={ShieldUser}
                title="Usuarios y roles"
                description="Administración centralizada de identidades institucionales, permisos y control de bloqueos."
                actions={<Button variant="primary" icon={UserPlus} onClick={() => setModal({ type: 'create' })}>Nuevo usuario</Button>}
            />

            <section className="card">
                <div className="filters" style={{ gridTemplateColumns: 'minmax(220px, 2fr) minmax(160px, 1fr) minmax(160px, 1fr) auto' }}>
                    <Input label="Buscar" icon={Search} placeholder="Nombre, usuario, correo o departamento" value={searchInput} onChange={e => setSearchInput(e.target.value)} />
                    <Select label="Rol" placeholder="Todos los roles" options={ROLE_OPTIONS} value={filters.role} onChange={setFilter('role')} />
                    <Select label="Estado" placeholder="Todos" options={STATUS_OPTIONS} value={filters.isActive} onChange={setFilter('isActive')} />
                    <Button icon={FilterX} disabled={!hasFilters} onClick={() => { setSearchInput(''); setFilters(f => ({ ...f, searchTerm: '', role: '', isActive: '', pageNumber: 1 })); }}>Limpiar</Button>
                </div>

                <div className="table-wrap">
                    <table className="table">
                        <thead>
                            <tr>
                                <th>Usuario</th>
                                <th>Rol</th>
                                <th>Departamento</th>
                                <th>Estado</th>
                                <th>Intentos</th>
                                <th>Alta</th>
                                <th className="right">Acciones</th>
                            </tr>
                        </thead>
                        {loading ? <TableSkeleton cols={7} /> : (
                            <tbody>
                                {result.data.map(u => (
                                    <tr key={u.id}>
                                        <td>
                                            <div className="row" style={{ gap: 12 }}>
                                                <span className="avatar" style={{ opacity: u.isActive ? 1 : 0.45 }}>{initials(u.fullName)}</span>
                                                <div style={{ minWidth: 0 }}>
                                                    <div className="cell-title">{u.fullName}{u.id === me?.id && <span className="muted" style={{ fontWeight: 400 }}> (usted)</span>}</div>
                                                    <div className="cell-sub">@{u.username} · {u.email}</div>
                                                </div>
                                            </div>
                                        </td>
                                        <td><RoleChip role={u.role} /></td>
                                        <td className="soft text-sm" style={{ maxWidth: 180 }}>{u.department || '—'}</td>
                                        <td><UserStatus user={u} /></td>
                                        <td>
                                            <span style={{ fontVariantNumeric: 'tabular-nums', fontWeight: 600, color: u.failedLoginAttempts >= 3 ? 'var(--danger-700)' : 'var(--gray-600)' }}>
                                                {u.failedLoginAttempts}
                                            </span>
                                            <span className="muted"> / 5</span>
                                        </td>
                                        <td className="soft text-sm" style={{ whiteSpace: 'nowrap' }} title={formatDateTime(u.createdAtUtc)}>{formatDate(u.createdAtUtc)}</td>
                                        <td>
                                            <div className="actions">
                                                {(u.isLockedOut || u.failedLoginAttempts > 0) && (
                                                    <IconButton icon={KeyRound} label="Restablecer bloqueo" onClick={() => changeStatus(u, { isActive: u.isActive, resetLockout: true })} />
                                                )}
                                                <UserRowMenu
                                                    user={u}
                                                    isSelf={u.id === me?.id}
                                                    onView={() => setModal({ type: 'view', user: u })}
                                                    onEdit={() => setModal({ type: 'edit', user: u })}
                                                    onRole={() => setModal({ type: 'role', user: u })}
                                                    onToggle={() => setModal({ type: 'toggle', user: u })}
                                                />
                                            </div>
                                        </td>
                                    </tr>
                                ))}
                            </tbody>
                        )}
                    </table>
                </div>

                {!loading && error && <EmptyState icon={Users} title="Error al cargar" description={error} action={<Button variant="primary" onClick={load}>Reintentar</Button>} />}
                {!loading && !error && !result.data.length && (
                    <EmptyState icon={Users} title="Sin usuarios" description={hasFilters ? 'Ningún usuario coincide con los filtros.' : 'Aún no hay usuarios registrados.'} />
                )}
                {!error && result.totalCount > 0 && (
                    <Pagination
                        pageNumber={filters.pageNumber}
                        pageSize={filters.pageSize}
                        totalCount={result.totalCount}
                        onPageChange={p => setFilters(f => ({ ...f, pageNumber: p }))}
                        onPageSizeChange={s => setFilters(f => ({ ...f, pageSize: s, pageNumber: 1 }))}
                    />
                )}
            </section>

            {modal?.type === 'create' && (
                <UserFormModal open mode="create" onClose={() => setModal(null)} onSaved={() => load()} />
            )}
            {modal?.type === 'edit' && (
                <UserFormModal key={modal.user.id} open mode="edit" user={modal.user} onClose={() => setModal(null)} onSaved={replaceUser} />
            )}
            {modal?.type === 'role' && (
                <ChangeRoleModal key={modal.user.id} open user={modal.user} onClose={() => setModal(null)} onSaved={replaceUser} />
            )}
            <ToggleStatusModal
                open={modal?.type === 'toggle'}
                user={modal?.user}
                onClose={() => setModal(null)}
                onConfirm={async () => { await changeStatus(modal.user, { isActive: !modal.user.isActive, resetLockout: false }); setModal(null); }}
            />
            <UserDetailModal open={modal?.type === 'view'} user={modal?.user} onClose={() => setModal(null)} />
        </div>
    );
}

function UserStatus({ user }) {
    if (user.isLockedOut) return <Badge tone="danger" icon={Lock}>Bloqueado</Badge>;
    if (!user.isActive) return <Badge tone="neutral" dot>Inactivo</Badge>;
    return <Badge tone="success" dot>Activo</Badge>;
}

function UserRowMenu({ user, isSelf, onView, onEdit, onRole, onToggle }) {
    const [open, setOpen] = useState(false);
    const ref = useRef(null);

    useEffect(() => {
        if (!open) return undefined;
        const close = e => { if (!ref.current?.contains(e.target)) setOpen(false); };
        document.addEventListener('mousedown', close);
        return () => document.removeEventListener('mousedown', close);
    }, [open]);

    const item = (Icon, label, fn, cls = '') => (
        <button className={`dropdown-item ${cls}`} role="menuitem" onClick={() => { setOpen(false); fn(); }}>
            <Icon />{label}
        </button>
    );

    return (
        <div ref={ref} style={{ position: 'relative' }}>
            <IconButton icon={MoreHorizontal} label="Más acciones" onClick={() => setOpen(o => !o)} aria-expanded={open} />
            {open && (
                <div className="dropdown" style={{ width: 210 }} role="menu">
                    {item(Eye, 'Ver ficha', onView)}
                    {item(UserRoundPen, 'Editar datos', onEdit)}
                    {!isSelf && item(ShieldUser, 'Cambiar rol', onRole)}
                    {!isSelf && item(user.isActive ? UserX : UserCheck, user.isActive ? 'Desactivar' : 'Activar', onToggle, user.isActive ? 'danger' : '')}
                </div>
            )}
        </div>
    );
}

function ChangeRoleModal({ open, user, onClose, onSaved }) {
    const [role, setRole] = useState(user?.role ?? '');
    const [saving, setSaving] = useState(false);

    const save = async () => {
        setSaving(true);
        try {
            const updated = await userService.changeRole(user.id, role);
            toast.success('Rol actualizado', { description: `${user.fullName} ahora es ${ROLE_LABELS[role]}.` });
            onSaved(updated);
            onClose();
        } catch (err) {
            toast.error('No se pudo cambiar el rol', { description: getErrorMessage(err) });
        } finally {
            setSaving(false);
        }
    };

    return (
        <Modal
            open={open}
            onClose={onClose}
            icon={ShieldUser}
            title="Cambiar rol"
            description={user?.fullName}
            footer={<><Button onClick={onClose}>Cancelar</Button><Button variant="primary" loading={saving} disabled={role === user?.role} onClick={save}>Asignar rol</Button></>}
        >
            <div className="stack-sm" role="radiogroup">
                {ROLE_OPTIONS.map(r => (
                    <label key={r.value} className="file-card" style={{ cursor: 'pointer', borderColor: role === r.value ? 'var(--primary-500)' : undefined, background: role === r.value ? 'var(--primary-50)' : undefined }}>
                        <input type="radio" name="role" value={r.value} checked={role === r.value} onChange={() => setRole(r.value)} style={{ accentColor: 'var(--primary-700)' }} />
                        <div className="file-meta">
                            <strong>{r.label}</strong>
                            <span className="muted text-sm">{r.description}</span>
                        </div>
                        {user?.role === r.value && <Badge tone="info">Actual</Badge>}
                    </label>
                ))}
            </div>
        </Modal>
    );
}

function ToggleStatusModal({ open, user, onClose, onConfirm }) {
    const [saving, setSaving] = useState(false);
    const deactivate = user?.isActive;
    return (
        <Modal
            open={open}
            onClose={onClose}
            icon={deactivate ? UserX : UserCheck}
            tone={deactivate ? 'danger' : undefined}
            title={deactivate ? 'Desactivar usuario' : 'Activar usuario'}
            description={user?.fullName}
            footer={
                <>
                    <Button onClick={onClose}>Cancelar</Button>
                    <Button
                        variant={deactivate ? 'danger' : 'success'}
                        loading={saving}
                        onClick={async () => { setSaving(true); await onConfirm(); setSaving(false); }}
                    >
                        {deactivate ? 'Desactivar' : 'Activar'}
                    </Button>
                </>
            }
        >
            <p className="soft">
                {deactivate
                    ? 'El usuario no podrá iniciar sesión hasta que sea reactivado. Sus acciones previas permanecen en la bitácora.'
                    : 'El usuario podrá volver a iniciar sesión con sus credenciales actuales.'}
            </p>
        </Modal>
    );
}

function UserDetailModal({ open, user, onClose }) {
    if (!user) return null;
    return (
        <Modal open={open} onClose={onClose} title={user.fullName} description={`@${user.username}`} footer={<Button onClick={onClose}>Cerrar</Button>}>
            <dl className="dl">
                <dt>Correo</dt><dd>{user.email}</dd>
                <dt>Rol</dt><dd><RoleChip role={user.role} /></dd>
                <dt>Estado</dt><dd><UserStatus user={user} /></dd>
                <dt>Departamento</dt><dd>{user.department || '—'}</dd>
                <dt>Matrícula</dt><dd className="mono">{user.studentRegistration || '—'}</dd>
                <dt>Intentos fallidos</dt><dd>{user.failedLoginAttempts} de 5</dd>
                {user.lockoutEndUtc && <><dt>Bloqueado hasta</dt><dd>{formatDateTime(user.lockoutEndUtc)}</dd></>}
                <dt>Creado</dt><dd>{formatDateTime(user.createdAtUtc)} · {user.createdBy || 'sistema'}</dd>
                <dt>Última modificación</dt><dd>{user.lastModifiedAtUtc ? `${formatDateTime(user.lastModifiedAtUtc)} · ${user.lastModifiedBy}` : '—'}</dd>
            </dl>
        </Modal>
    );
}
