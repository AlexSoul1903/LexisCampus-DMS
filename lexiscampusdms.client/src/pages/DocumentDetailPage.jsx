import { useCallback, useEffect, useMemo, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { toast } from 'sonner';
import {
    Ban, Copy, Download, ExternalLink, FilePen, FileSearch, FileText, History, QrCode, ScrollText, ShieldCheck
} from 'lucide-react';
import { Alert, Badge, Button, Card, EmptyState, PageHeader, Skeleton } from '@/components/ui';
import { ValidityBadge, VersionTag } from '@/components/documents/DocumentBadges';
import DocumentTimeline from '@/components/documents/DocumentTimeline';
import DocumentViewer from '@/components/documents/DocumentViewer';
import HashBox from '@/components/documents/HashBox';
import RectifyModal from '@/components/documents/RectifyModal';
import RevokeModal from '@/components/documents/RevokeModal';
import { useAuth } from '@/context/AuthContext';
import { DOCUMENT_STATUS, ROLES, documentStatusLabel, documentTypeLabel } from '@/config/constants';
import { documentService } from '@/services/documentService';
import { publicVerificationService } from '@/services/publicVerificationService';
import { formatActor, formatBytes, formatDateTime, shortId } from '@/utils/format';
import { getErrorMessage } from '@/utils/errors';
import { copyToClipboard, saveBlob } from '@/utils/files';

export default function DocumentDetailPage() {
    const { id } = useParams();
    const { hasRole, user } = useAuth();
    const canOperate = hasRole(ROLES.ADMIN, ROLES.REGISTRO);

    const [doc, setDoc] = useState(null);
    const [versions, setVersions] = useState([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState(null);
    const [selectedVersion, setSelectedVersion] = useState(null);
    const [modal, setModal] = useState(null);

    const load = useCallback(async () => {
        setLoading(true);
        setError(null);
        try {
            const [d, v] = await Promise.all([documentService.getById(id), documentService.getVersions(id)]);
            setDoc(d);
            setVersions(v.length ? v : d.versions ?? []);
            setSelectedVersion(d.currentVersion);
        } catch (err) {
            setError({ status: err.response?.status, message: getErrorMessage(err, 'No se pudo cargar el documento.') });
        } finally {
            setLoading(false);
        }
    }, [id]);

    useEffect(() => { load(); }, [load]);

    const revoked = doc?.status === DOCUMENT_STATUS.REVOKED;
    const currentVersionInfo = versions.find(v => v.versionNumber === doc?.currentVersion);
    const verifyUrl = doc ? `${window.location.origin}/verify/${doc.currentFileHash}` : '';

    const download = async versionNumber => {
        const t = toast.loading('Preparando descarga…');
        try {
            const { blob, fileName } = await documentService.download(id, { versionNumber });
            saveBlob(blob, fileName ?? `${doc.title}_v${versionNumber ?? doc.currentVersion}`);
            toast.success('Descarga iniciada', { id: t });
        } catch (err) {
            toast.error('No se pudo descargar', { id: t, description: getErrorMessage(err) });
        }
    };

    const copyLink = async () => {
        (await copyToClipboard(verifyUrl))
            ? toast.success('Enlace de verificación copiado')
            : toast.error('No se pudo copiar el enlace');
    };

    const auditEvents = useMemo(() => buildAuditTrail(doc, versions, user), [doc, versions, user]);

    if (loading) {
        return (
            <div className="page">
                <Skeleton width={180} height={12} />
                <Skeleton width="45%" height={30} style={{ margin: '16px 0 28px' }} />
                <div className="grid-3">
                    <div className="card span-2" style={{ height: 600 }}><Skeleton height="100%" radius={14} /></div>
                    <div className="stack">
                        <div className="card card-body stack-sm">{Array.from({ length: 6 }).map((_, i) => <Skeleton key={i} />)}</div>
                        <div className="card card-body"><Skeleton height={180} /></div>
                    </div>
                </div>
            </div>
        );
    }

    if (error) {
        return (
            <div className="page">
                <Card>
                    <EmptyState
                        icon={FileSearch}
                        title={error.status === 404 ? 'Documento no encontrado' : 'No se pudo cargar el documento'}
                        description={error.message}
                        action={
                            <div className="row">
                                <Link to="/documents" className="btn">Volver al listado</Link>
                                {error.status !== 404 && <Button variant="primary" onClick={load}>Reintentar</Button>}
                            </div>
                        }
                    />
                </Card>
            </div>
        );
    }

    return (
        <div className="page">
            <PageHeader
                breadcrumbs={
                    <div className="breadcrumbs">
                        <Link to="/documents">Documentos</Link><span>/</span>
                        <Link to={`/students/${encodeURIComponent(doc.studentRegistration)}`}>{doc.studentRegistration}</Link><span>/</span>
                        <span className="mono">DOC-{shortId(doc.id)}</span>
                    </div>
                }
                eyebrow={documentTypeLabel(doc.documentType)}
                eyebrowIcon={FileText}
                title={doc.title}
                description={
                    <span className="row-wrap" style={{ marginTop: 4 }}>
                        <ValidityBadge doc={doc} />
                        <VersionTag version={doc.currentVersion} rectified={doc.currentVersion > 1} />
                        <span className="muted text-sm">Emitido el {formatDateTime(doc.createdAtUtc)} por {formatActor(doc.createdBy, user)}</span>
                    </span>
                }
                actions={
                    <>
                        <Button icon={Download} onClick={() => download()}>Descargar</Button>
                        {canOperate && !revoked && (
                            <>
                                <Button icon={FilePen} onClick={() => setModal('rectify')}>Rectificar</Button>
                                <Button variant="danger" icon={Ban} onClick={() => setModal('revoke')}>Revocar</Button>
                            </>
                        )}
                    </>
                }
            />

            {revoked && (
                <Alert tone="danger" icon={Ban} title={`Documento revocado · Resolución ${doc.resolutionNumber}`} className="mb">
                    {doc.revocationReason}
                    {doc.revocationObservations && <div style={{ marginTop: 4, opacity: 0.85 }}>{doc.revocationObservations}</div>}
                    <div className="text-xs" style={{ marginTop: 6, opacity: 0.8 }}>
                        Revocado el {formatDateTime(doc.revokedAtUtc)} por {formatActor(doc.revokedBy, user)}
                    </div>
                </Alert>
            )}

            <div className="grid-3" style={{ alignItems: 'start', marginTop: revoked ? 20 : 0 }}>
                <div className="span-2 stack">
                    <Card
                        flush
                        title="Visor del documento"
                        subtitle={selectedVersion === doc.currentVersion
                            ? `Versión vigente v${selectedVersion}`
                            : `Versión histórica v${selectedVersion} — no vigente`}
                        actions={selectedVersion !== doc.currentVersion && (
                            <Button size="sm" variant="soft" onClick={() => setSelectedVersion(doc.currentVersion)}>Volver a la vigente</Button>
                        )}
                    >
                        <DocumentViewer documentId={doc.id} versionNumber={selectedVersion} revoked={revoked} concealed={Boolean(modal)} />
                    </Card>

                    <Card
                        flush
                        title={<span className="row"><ScrollText size={16} /> Registro de auditoría</span>}
                        subtitle="Trazabilidad inmutable de las operaciones sobre el archivo"
                    >
                        <div className="table-wrap">
                            <table className="table">
                                <thead>
                                    <tr><th>Fecha y hora</th><th>Acción</th><th>Responsable</th><th>Detalle</th></tr>
                                </thead>
                                <tbody>
                                    {auditEvents.map(ev => (
                                        <tr key={ev.key}>
                                            <td style={{ whiteSpace: 'nowrap' }} className="soft">{formatDateTime(ev.at)}</td>
                                            <td><Badge tone={ev.tone}>{ev.action}</Badge></td>
                                            <td style={{ fontWeight: 500 }}>{ev.by || '—'}</td>
                                            <td className="soft text-sm">{ev.detail}</td>
                                        </tr>
                                    ))}
                                </tbody>
                            </table>
                        </div>
                    </Card>
                </div>

                <div className="stack">
                    <Card title="Ficha del documento">
                        <dl className="dl" style={{ gridTemplateColumns: '120px minmax(0,1fr)' }}>
                            <dt>Código</dt><dd className="mono">DOC-{shortId(doc.id)}</dd>
                            <dt>Estudiante</dt>
                            <dd><Link to={`/students/${encodeURIComponent(doc.studentRegistration)}`} className="mono">{doc.studentRegistration}</Link></dd>
                            <dt>Tipo</dt><dd>{documentTypeLabel(doc.documentType)}</dd>
                            <dt>Estado</dt><dd>{documentStatusLabel(doc.status)}</dd>
                            <dt>Versión</dt><dd>v{doc.currentVersion} de {versions.length || doc.currentVersion}</dd>
                            <dt>Tamaño</dt><dd>{formatBytes(currentVersionInfo?.fileSizeBytes)}</dd>
                            <dt>Formato</dt><dd>{currentVersionInfo?.mimeType ?? '—'}</dd>
                            <dt>Emitido por</dt><dd>{formatActor(doc.createdBy, user)}</dd>
                        </dl>
                        <div style={{ marginTop: 16 }}>
                            <div className="field-label" style={{ marginBottom: 6 }}>Huella SHA-256 vigente</div>
                            <HashBox hash={doc.currentFileHash} />
                        </div>
                    </Card>

                    <Card
                        title={<span className="row"><QrCode size={16} /> Verificación pública</span>}
                        subtitle="Código para validación por terceros"
                    >
                        <div className="stack" style={{ alignItems: 'center' }}>
                            <img
                                src={publicVerificationService.qrUrl(doc.currentFileHash, 'svg')}
                                alt="Código QR de verificación"
                                width={168}
                                height={168}
                                style={{ borderRadius: 10, border: '1px solid var(--border)', padding: 8, background: '#fff' }}
                            />
                            <div className="row-wrap" style={{ justifyContent: 'center' }}>
                                <Button size="sm" icon={Copy} onClick={copyLink}>Copiar enlace</Button>
                                <a className="btn btn-sm btn-soft" href={`/verify/${doc.currentFileHash}`} target="_blank" rel="noreferrer noopener">
                                    <ExternalLink />Abrir portal
                                </a>
                            </div>
                        </div>
                    </Card>

                    <Card title={<span className="row"><History size={16} /> Línea de tiempo forense</span>} subtitle="Evolución del documento desde su emisión">
                        <DocumentTimeline
                            document={doc}
                            versions={versions}
                            selectedVersion={selectedVersion}
                            onSelectVersion={setSelectedVersion}
                            onDownloadVersion={download}
                        />
                    </Card>

                    <div className="alert alert-info text-sm">
                        <ShieldCheck aria-hidden />
                        <div>Las consultas y descargas de este documento quedan registradas con usuario, fecha e IP en la bitácora del servidor.</div>
                    </div>
                </div>
            </div>

            <RectifyModal open={modal === 'rectify'} document={doc} onClose={() => setModal(null)} onRectified={load} />
            <RevokeModal open={modal === 'revoke'} document={doc} onClose={() => setModal(null)} onRevoked={load} />
        </div>
    );
}

function buildAuditTrail(doc, versions, user) {
    if (!doc) return [];
    const events = [];
    [...versions].sort((a, b) => a.versionNumber - b.versionNumber).forEach(v => {
        events.push({
            key: `v${v.versionNumber}`,
            at: v.createdAtUtc,
            action: v.versionNumber === 1 ? 'Carga inicial' : 'Rectificación',
            tone: v.versionNumber === 1 ? 'success' : 'warning',
            by: formatActor(v.uploadedBy, user),
            detail: `Versión v${v.versionNumber} · ${formatBytes(v.fileSizeBytes)} · SHA-256 ${v.fileHash.slice(0, 12)}…`
        });
    });
    if (doc.revokedAtUtc) {
        events.push({
            key: 'revoke',
            at: doc.revokedAtUtc,
            action: 'Revocación',
            tone: 'danger',
            by: formatActor(doc.revokedBy, user),
            detail: `Resolución ${doc.resolutionNumber}`
        });
    }
    return events.reverse();
}
