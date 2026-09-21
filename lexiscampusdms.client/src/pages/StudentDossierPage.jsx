import { useCallback, useEffect, useMemo, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { toast } from 'sonner';
import {
    Ban, BadgeCheck, FileArchive, FilePen, FileStack, FileText, FolderOpen, GraduationCap, UploadCloud
} from 'lucide-react';
import { Button, Card, EmptyState, PageHeader, Skeleton } from '@/components/ui';
import { ValidityBadge, VersionTag } from '@/components/documents/DocumentBadges';
import { getValidity } from '@/utils/documents';
import { useAuth } from '@/context/AuthContext';
import { DOCUMENT_TYPES, ROLES } from '@/config/constants';
import { documentService } from '@/services/documentService';
import { formatDate, formatDateTime, shortHash } from '@/utils/format';
import { getErrorMessage } from '@/utils/errors';
import { saveBlob } from '@/utils/files';

export default function StudentDossierPage() {
    const { matricula } = useParams();
    const { hasRole } = useAuth();
    const canUpload = hasRole(ROLES.ADMIN, ROLES.REGISTRO);

    const [docs, setDocs] = useState(null);
    const [error, setError] = useState(null);
    const [exporting, setExporting] = useState(false);

    const load = useCallback(async () => {
        setDocs(null);
        setError(null);
        try {
            setDocs(await documentService.getByStudent(matricula));
        } catch (err) {
            setError(getErrorMessage(err, 'No se pudo cargar el expediente.'));
        }
    }, [matricula]);

    useEffect(() => { load(); }, [load]);

    const summary = useMemo(() => {
        if (!docs) return null;
        const counts = { valid: 0, rectified: 0, revoked: 0 };
        docs.forEach(d => { counts[getValidity(d).key] += 1; });
        const byType = DOCUMENT_TYPES
            .map(t => ({ ...t, docs: docs.filter(d => d.documentType === t.value) }))
            .filter(t => t.docs.length);
        const dates = docs.map(d => d.createdAtUtc).sort();
        return { ...counts, total: docs.length, byType, first: dates[0], last: dates[dates.length - 1] };
    }, [docs]);

    const exportZip = async () => {
        setExporting(true);
        const t = toast.loading('Generando paquete del expediente…', { description: 'Incluye documentos originales y manifiesto JSON firmado.' });
        try {
            const { blob, fileName } = await documentService.downloadDossierZip(matricula);
            saveBlob(blob, fileName);
            toast.success('Expediente exportado', { id: t, description: fileName });
        } catch (err) {
            toast.error('No se pudo exportar el expediente', { id: t, description: getErrorMessage(err) });
        } finally {
            setExporting(false);
        }
    };

    return (
        <div className="page">
            <PageHeader
                breadcrumbs={
                    <div className="breadcrumbs">
                        <Link to="/students">Expedientes</Link><span>/</span><span className="mono">{matricula}</span>
                    </div>
                }
                eyebrow="Expediente estudiantil"
                eyebrowIcon={GraduationCap}
                title={<span className="row" style={{ gap: 14 }}><span className="avatar avatar-lg"><GraduationCap size={28} /></span><span>Matrícula {matricula}</span></span>}
                description={summary?.total ? `Historia documental desde ${formatDate(summary.first)} · última actualización ${formatDate(summary.last)}` : undefined}
                actions={
                    <>
                        {canUpload && (
                            <Link to={`/documents/upload?matricula=${encodeURIComponent(matricula)}`} className="btn"><UploadCloud />Agregar documento</Link>
                        )}
                        <Button variant="primary" icon={FileArchive} loading={exporting} disabled={!docs?.length} onClick={exportZip}>
                            Exportar expediente (ZIP)
                        </Button>
                    </>
                }
            />

            {error && (
                <Card><EmptyState icon={FolderOpen} title="Error al cargar" description={error} action={<Button variant="primary" onClick={load}>Reintentar</Button>} /></Card>
            )}

            {!error && docs?.length === 0 && (
                <Card>
                    <EmptyState
                        icon={FolderOpen}
                        title="Expediente sin documentos"
                        description={`No existen documentos registrados para la matrícula ${matricula}.`}
                        action={canUpload && <Link to={`/documents/upload?matricula=${encodeURIComponent(matricula)}`} className="btn btn-primary btn-sm"><UploadCloud />Cargar documento</Link>}
                    />
                </Card>
            )}

            {!error && (docs === null || docs.length > 0) && (
                <div className="stack" style={{ gap: 20 }}>
                    <div className="grid-4">
                        <Tile loading={!summary} label="Documentos emitidos" value={summary?.total} icon={FileStack} tone="primary" />
                        <Tile loading={!summary} label="Vigentes" value={summary?.valid} icon={BadgeCheck} tone="success" />
                        <Tile loading={!summary} label="Rectificados" value={summary?.rectified} icon={FilePen} tone="warning" />
                        <Tile loading={!summary} label="Revocados" value={summary?.revoked} icon={Ban} tone="danger" />
                    </div>

                    {docs === null ? (
                        <div className="card card-body"><Skeleton height={200} /></div>
                    ) : (
                        summary.byType.map(group => (
                            <Card key={group.value} flush title={group.label} subtitle={`${group.docs.length} documento${group.docs.length === 1 ? '' : 's'}`}>
                                {group.docs.map(doc => (
                                    <Link key={doc.id} to={`/documents/${doc.id}`} className="list-row">
                                        <div className="doc-icon"><FileText /></div>
                                        <div style={{ flex: 1, minWidth: 0 }}>
                                            <div className="truncate" style={{ fontWeight: 600 }}>{doc.title}</div>
                                            <div className="text-xs muted">
                                                Emitido {formatDateTime(doc.createdAtUtc)} · <span className="mono">{shortHash(doc.currentFileHash, 8)}</span>
                                            </div>
                                        </div>
                                        <VersionTag version={doc.currentVersion} />
                                        <ValidityBadge doc={doc} />
                                    </Link>
                                ))}
                            </Card>
                        ))
                    )}

                    {docs !== null && (
                        <div className="alert alert-info text-sm">
                            <FileArchive aria-hidden />
                            <div>
                                <strong>Exportación certificada</strong>
                                El paquete ZIP incluye los archivos originales de todos los documentos del expediente junto con un manifiesto JSON con sus hashes SHA-256 para validación independiente.
                            </div>
                        </div>
                    )}
                </div>
            )}
        </div>
    );
}

function Tile({ loading, label, value, icon: Icon, tone }) {
    return (
        <div className="card stat">
            <div className="stat-top">
                <span className="stat-label">{label}</span>
                <span className={`stat-icon ${tone}`}><Icon aria-hidden /></span>
            </div>
            {loading ? <Skeleton width={60} height={30} /> : <span className="stat-value">{value}</span>}
        </div>
    );
}
