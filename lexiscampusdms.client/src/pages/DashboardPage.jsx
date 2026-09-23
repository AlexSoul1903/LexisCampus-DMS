import { useCallback, useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import {
    ArrowRight, Ban, CalendarDays, FilePen, FileStack, FileText, FolderSearch, LayoutDashboard,
    RefreshCw, ShieldCheck, UploadCloud, UserCheck, Users
} from 'lucide-react';
import { Button, Card, EmptyState, PageHeader, Skeleton } from '@/components/ui';
import { ValidityBadge } from '@/components/documents/DocumentBadges';
import { useAuth } from '@/context/AuthContext';
import { DOCUMENT_STATUS, DOCUMENT_TYPES, ROLES, ROLE_LABELS, documentTypeLabel } from '@/config/constants';
import { documentService } from '@/services/documentService';
import { userService } from '@/services/userService';
import { formatNumber, formatRelative, toDateInput } from '@/utils/format';
import { getErrorMessage } from '@/utils/errors';

const SAMPLE_SIZE = 100;

function greeting() {
    const h = new Date().getHours();
    if (h < 12) return 'Buenos días';
    if (h < 19) return 'Buenas tardes';
    return 'Buenas noches';
}

export default function DashboardPage() {
    const { user, role, hasRole } = useAuth();
    const isAdmin = hasRole(ROLES.ADMIN);
    const canUpload = hasRole(ROLES.ADMIN, ROLES.REGISTRO);

    const [data, setData] = useState(null);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState(null);

    const load = useCallback(async () => {
        setLoading(true);
        setError(null);
        try {
            const now = new Date();
            const monthStart = toDateInput(new Date(now.getFullYear(), now.getMonth(), 1));

            const [sample, month, ...byType] = await Promise.all([
                documentService.search({ pageSize: SAMPLE_SIZE }),
                documentService.search({ fromDate: monthStart, pageSize: 1 }),
                ...DOCUMENT_TYPES.map(t => documentService.search({ tipo: t.value, pageSize: 1 }))
            ]);

            let users = null;
            if (isAdmin) {
                const [all, active] = await Promise.all([
                    userService.list({ pageSize: 100 }),
                    userService.list({ isActive: true, pageSize: 1 })
                ]);
                users = {
                    total: all.totalCount,
                    active: active.totalCount,
                    locked: (all.data ?? []).filter(u => u.isLockedOut).length
                };
            }

            const docs = sample.data ?? [];
            setData({
                total: sample.totalCount,
                thisMonth: month.totalCount,
                revoked: docs.filter(d => d.status === DOCUMENT_STATUS.REVOKED).length,
                rectified: docs.filter(d => d.currentVersion > 1 && d.status !== DOCUMENT_STATUS.REVOKED).length,
                sampled: sample.totalCount > SAMPLE_SIZE,
                recent: docs.slice(0, 6),
                byType: DOCUMENT_TYPES.map((t, i) => ({ ...t, count: byType[i].totalCount }))
                    .sort((a, b) => b.count - a.count),
                users
            });
        } catch (err) {
            setError(getErrorMessage(err, 'No se pudieron cargar las métricas.'));
        } finally {
            setLoading(false);
        }
    }, [isAdmin]);

    useEffect(() => { load(); }, [load]);

    const maxType = Math.max(1, ...(data?.byType ?? []).map(t => t.count));
    const firstName = user?.fullName?.split(' ')[0] ?? user?.username;

    return (
        <div className="page">
            <PageHeader
                eyebrow={`Panel · ${ROLE_LABELS[role] ?? role}`}
                eyebrowIcon={LayoutDashboard}
                title={`${greeting()}, ${firstName}`}
                description="Resumen de la actividad documental de la institución."
                actions={
                    <>
                        <Button icon={RefreshCw} onClick={load} disabled={loading}>Actualizar</Button>
                        {canUpload && (
                            <Link to="/documents/upload" className="btn btn-primary"><UploadCloud />Cargar documento</Link>
                        )}
                    </>
                }
            />

            {error && (
                <Card>
                    <EmptyState
                        icon={RefreshCw}
                        title="No se pudo cargar el panel"
                        description={error}
                        action={<Button variant="primary" onClick={load}>Reintentar</Button>}
                    />
                </Card>
            )}

            {!error && (
                <div className="stack" style={{ gap: 20 }}>
                    <div className="grid-4">
                        <StatTile loading={loading} label="Documentos custodiados" value={data?.total} icon={FileStack} tone="primary" foot="Total registrado en el sistema" />
                        <StatTile loading={loading} label="Emitidos este mes" value={data?.thisMonth} icon={CalendarDays} tone="success" foot={new Date().toLocaleDateString('es-DO', { month: 'long', year: 'numeric' })} />
                        <StatTile loading={loading} label="Rectificados" value={data?.rectified} icon={FilePen} tone="warning" foot={data?.sampled ? `En los últimos ${SAMPLE_SIZE} registros` : 'Con más de una versión'} />
                        <StatTile loading={loading} label="Revocados" value={data?.revoked} icon={Ban} tone="danger" foot={data?.sampled ? `En los últimos ${SAMPLE_SIZE} registros` : 'Sin validez legal'} />
                    </div>

                    <div className="grid-3">
                        <Card
                            className="span-2"
                            flush
                            title="Actividad reciente"
                            subtitle="Últimos documentos registrados"
                            actions={<Link to="/documents" className="btn btn-ghost btn-sm">Ver todos <ArrowRight /></Link>}
                        >
                            {loading ? (
                                Array.from({ length: 5 }).map((_, i) => (
                                    <div key={i} className="list-row">
                                        <Skeleton width={38} height={38} radius={9} />
                                        <div style={{ flex: 1 }}><Skeleton width="55%" /><Skeleton width="30%" height={10} style={{ marginTop: 8 }} /></div>
                                    </div>
                                ))
                            ) : data?.recent.length ? (
                                data.recent.map(doc => (
                                    <Link key={doc.id} to={`/documents/${doc.id}`} className="list-row">
                                        <div className="doc-icon"><FileText /></div>
                                        <div style={{ flex: 1, minWidth: 0 }}>
                                            <div className="truncate" style={{ fontWeight: 600 }}>{doc.title}</div>
                                            <div className="text-xs muted">
                                                {doc.studentRegistration} · {documentTypeLabel(doc.documentType)} · {formatRelative(doc.createdAtUtc)}
                                            </div>
                                        </div>
                                        <ValidityBadge doc={doc} />
                                    </Link>
                                ))
                            ) : (
                                <EmptyState
                                    icon={FileStack}
                                    title="Aún no hay documentos"
                                    description="Los documentos registrados aparecerán aquí."
                                    action={canUpload && <Link to="/documents/upload" className="btn btn-primary btn-sm"><UploadCloud />Cargar el primero</Link>}
                                />
                            )}
                        </Card>

                        <Card title="Distribución por tipo" subtitle="Documentos por categoría">
                            {loading ? (
                                <div className="bar-list">
                                    {Array.from({ length: 6 }).map((_, i) => <Skeleton key={i} height={26} />)}
                                </div>
                            ) : (
                                <div className="bar-list">
                                    {data?.byType.map(t => (
                                        <div key={t.value}>
                                            <div className="bar-item-top"><span>{t.label}</span><span>{formatNumber(t.count)}</span></div>
                                            <div className="bar-track">
                                                <div className="bar-fill" style={{ width: `${(t.count / maxType) * 100}%` }} />
                                            </div>
                                        </div>
                                    ))}
                                </div>
                            )}
                        </Card>
                    </div>

                    <div className="grid-3">
                        {isAdmin && (
                            <Card
                                title="Padrón de usuarios"
                                subtitle="Cuentas institucionales"
                                actions={<Link to="/admin/users" className="btn btn-ghost btn-sm">Gestionar <ArrowRight /></Link>}
                            >
                                {loading ? <Skeleton height={48} /> : (
                                    <div className="kpi-inline">
                                        <div><strong>{formatNumber(data?.users?.total)}</strong><span><Users size={12} /> Registrados</span></div>
                                        <div><strong style={{ color: 'var(--success-700)' }}>{formatNumber(data?.users?.active)}</strong><span><UserCheck size={12} /> Activos</span></div>
                                        <div><strong style={{ color: 'var(--danger-700)' }}>{formatNumber(data?.users?.locked)}</strong><span>Bloqueados</span></div>
                                    </div>
                                )}
                            </Card>
                        )}
                        <QuickAction to="/students" icon={FolderSearch} title="Expedientes estudiantiles" text="Consulte la historia documental completa de un alumno y exporte su expediente." />
                        <QuickAction to="/verify" external icon={ShieldCheck} title="Verificación pública" text="Valide la autenticidad de un documento por código QR, hash SHA-256 o archivo." />
                    </div>
                </div>
            )}
        </div>
    );
}

function StatTile({ loading, label, value, icon: Icon, tone, foot }) {
    return (
        <div className="card stat">
            <div className="stat-top">
                <span className="stat-label">{label}</span>
                <span className={`stat-icon ${tone}`}><Icon aria-hidden /></span>
            </div>
            {loading ? <Skeleton width={80} height={30} /> : <span className="stat-value">{formatNumber(value)}</span>}
            <span className="stat-foot">{foot}</span>
        </div>
    );
}

function QuickAction({ to, external, icon: Icon, title, text }) {
    const content = (
        <>
            <div className="row" style={{ gap: 12 }}>
                <span className="stat-icon primary"><Icon aria-hidden /></span>
                <h3 style={{ flex: 1 }}>{title}</h3>
                <ArrowRight size={16} className="muted" />
            </div>
            <p className="muted text-sm" style={{ marginTop: 10 }}>{text}</p>
        </>
    );
    const props = { className: 'card card-body', style: { display: 'block', color: 'inherit', textDecoration: 'none' } };
    return external
        ? <a href={to} target="_blank" rel="noreferrer noopener" {...props}>{content}</a>
        : <Link to={to} {...props}>{content}</Link>;
}
