import { useCallback, useEffect, useRef, useState } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import { toast } from 'sonner';
import {
    Ban, Download, Eye, FilePen, FileStack, FilterX, MoreHorizontal, Search, UploadCloud
} from 'lucide-react';
import { Button, EmptyState, IconButton, Input, PageHeader, Pagination, Select, TableSkeleton } from '@/components/ui';
import { ValidityBadge, VersionTag } from '@/components/documents/DocumentBadges';
import RectifyModal from '@/components/documents/RectifyModal';
import RevokeModal from '@/components/documents/RevokeModal';
import { useAuth } from '@/context/AuthContext';
import { DOCUMENT_STATUS, DOCUMENT_TYPES, ROLES, documentTypeLabel } from '@/config/constants';
import { documentService } from '@/services/documentService';
import { formatDate, shortId } from '@/utils/format';
import { getErrorMessage } from '@/utils/errors';
import { saveBlob } from '@/utils/files';

const TYPE_OPTIONS = DOCUMENT_TYPES.map(t => ({ value: t.value, label: t.label }));

export default function DocumentsPage() {
    const { hasRole } = useAuth();
    const canOperate = hasRole(ROLES.ADMIN, ROLES.REGISTRO);
    const [params, setParams] = useSearchParams();

    const filters = {
        matricula: params.get('matricula') ?? '',
        tipo: params.get('tipo') ?? '',
        fromDate: params.get('desde') ?? '',
        toDate: params.get('hasta') ?? '',
        pageNumber: Number(params.get('pagina') ?? 1),
        pageSize: Number(params.get('tamano') ?? 10)
    };

    const [matriculaInput, setMatriculaInput] = useState(filters.matricula);
    const [result, setResult] = useState({ data: [], totalCount: 0 });
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState(null);
    const [rectifyDoc, setRectifyDoc] = useState(null);
    const [revokeDoc, setRevokeDoc] = useState(null);

    const updateParams = useCallback((patch, resetPage = true) => {
        setParams(prev => {
            const next = new URLSearchParams(prev);
            const map = { matricula: 'matricula', tipo: 'tipo', fromDate: 'desde', toDate: 'hasta', pageNumber: 'pagina', pageSize: 'tamano' };
            Object.entries(patch).forEach(([k, v]) => {
                if (v === '' || v === null || v === undefined) next.delete(map[k]);
                else next.set(map[k], String(v));
            });
            if (resetPage && !('pageNumber' in patch)) next.delete('pagina');
            return next;
        }, { replace: true });
    }, [setParams]);

    // Búsqueda reactiva por matrícula con debounce.
    useEffect(() => {
        if (matriculaInput === filters.matricula) return undefined;
        const t = setTimeout(() => updateParams({ matricula: matriculaInput.trim() }), 400);
        return () => clearTimeout(t);
    }, [matriculaInput, filters.matricula, updateParams]);

    const dateError = filters.fromDate && filters.toDate && filters.toDate < filters.fromDate
        ? 'La fecha final debe ser posterior a la inicial.'
        : null;

    const queryKey = params.toString();
    const load = useCallback(async () => {
        if (dateError) return;
        setLoading(true);
        setError(null);
        try {
            const res = await documentService.search(filters);
            setResult({ data: res.data ?? [], totalCount: res.totalCount ?? 0 });
        } catch (err) {
            setError(getErrorMessage(err, 'No se pudieron consultar los documentos.'));
        } finally {
            setLoading(false);
        }
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [queryKey, dateError]);

    useEffect(() => { load(); }, [load]);

    const hasFilters = filters.matricula || filters.tipo || filters.fromDate || filters.toDate;

    const clearFilters = () => {
        setMatriculaInput('');
        setParams({}, { replace: true });
    };

    const download = async doc => {
        const id = toast.loading('Preparando descarga…');
        try {
            const { blob, fileName } = await documentService.download(doc.id);
            saveBlob(blob, fileName ?? `${doc.title}.pdf`);
            toast.success('Descarga iniciada', { id });
        } catch (err) {
            toast.error('No se pudo descargar el archivo', { id, description: getErrorMessage(err) });
        }
    };

    const replaceDoc = updated => {
        if (!updated) return load();
        setResult(r => ({ ...r, data: r.data.map(d => (d.id === updated.id ? { ...d, ...updated } : d)) }));
    };

    return (
        <div className="page">
            <PageHeader
                eyebrow="Núcleo documental"
                eyebrowIcon={FileStack}
                title="Documentos"
                description="Explore, filtre y opere sobre el acervo documental académico certificado."
                actions={canOperate && (
                    <Link to="/documents/upload" className="btn btn-primary"><UploadCloud />Cargar documento</Link>
                )}
            />

            <section className="card">
                <div className="filters">
                    <Input
                        label="Matrícula"
                        icon={Search}
                        placeholder="Ej. 2021-0456"
                        value={matriculaInput}
                        onChange={e => setMatriculaInput(e.target.value)}
                        maxLength={50}
                    />
                    <Select
                        label="Tipo de documento"
                        placeholder="Todos los tipos"
                        options={TYPE_OPTIONS}
                        value={filters.tipo}
                        onChange={e => updateParams({ tipo: e.target.value })}
                    />
                    <Input
                        label="Desde"
                        type="date"
                        value={filters.fromDate}
                        max={filters.toDate || undefined}
                        onChange={e => updateParams({ fromDate: e.target.value })}
                    />
                    <Input
                        label="Hasta"
                        type="date"
                        value={filters.toDate}
                        min={filters.fromDate || undefined}
                        onChange={e => updateParams({ toDate: e.target.value })}
                        error={dateError}
                    />
                    <Button icon={FilterX} onClick={clearFilters} disabled={!hasFilters}>Limpiar</Button>
                </div>

                <div className="table-wrap">
                    <table className="table">
                        <thead>
                            <tr>
                                <th>Documento</th>
                                <th>Estudiante</th>
                                <th>Tipo</th>
                                <th>Versión</th>
                                <th>Estado</th>
                                <th>Emisión</th>
                                <th className="right">Acciones</th>
                            </tr>
                        </thead>
                        {loading ? (
                            <TableSkeleton rows={filters.pageSize > 10 ? 10 : filters.pageSize} cols={7} />
                        ) : (
                            <tbody>
                                {result.data.map(doc => (
                                    <tr key={doc.id}>
                                        <td style={{ maxWidth: 320 }}>
                                            <Link to={`/documents/${doc.id}`} className="cell-title truncate" style={{ display: 'block', color: 'inherit' }}>
                                                {doc.title}
                                            </Link>
                                            <div className="cell-sub mono">DOC-{shortId(doc.id)}</div>
                                        </td>
                                        <td>
                                            <Link to={`/students/${encodeURIComponent(doc.studentRegistration)}`} className="mono" style={{ fontWeight: 500 }}>
                                                {doc.studentRegistration}
                                            </Link>
                                        </td>
                                        <td className="soft">{documentTypeLabel(doc.documentType)}</td>
                                        <td><VersionTag version={doc.currentVersion} rectified={doc.currentVersion > 1} /></td>
                                        <td><ValidityBadge doc={doc} /></td>
                                        <td className="soft" style={{ whiteSpace: 'nowrap' }}>{formatDate(doc.createdAtUtc)}</td>
                                        <td>
                                            <div className="actions">
                                                <Link to={`/documents/${doc.id}`} className="btn btn-ghost btn-icon btn-sm" title="Ver detalle" aria-label="Ver detalle">
                                                    <Eye />
                                                </Link>
                                                <IconButton icon={Download} label="Descargar" onClick={() => download(doc)} />
                                                {canOperate && (
                                                    <RowMenu
                                                        disabled={doc.status === DOCUMENT_STATUS.REVOKED}
                                                        onRectify={() => setRectifyDoc(doc)}
                                                        onRevoke={() => setRevokeDoc(doc)}
                                                    />
                                                )}
                                            </div>
                                        </td>
                                    </tr>
                                ))}
                            </tbody>
                        )}
                    </table>
                </div>

                {!loading && error && (
                    <EmptyState icon={FileStack} title="Error al consultar" description={error} action={<Button variant="primary" onClick={load}>Reintentar</Button>} />
                )}
                {!loading && !error && !result.data.length && (
                    <EmptyState
                        icon={hasFilters ? Search : FileStack}
                        title={hasFilters ? 'Sin resultados' : 'No hay documentos registrados'}
                        description={hasFilters ? 'Ningún documento coincide con los filtros aplicados.' : 'Cargue el primer documento para comenzar a construir el acervo.'}
                        action={hasFilters
                            ? <Button icon={FilterX} onClick={clearFilters}>Limpiar filtros</Button>
                            : canOperate && <Link to="/documents/upload" className="btn btn-primary btn-sm"><UploadCloud />Cargar documento</Link>}
                    />
                )}

                {!error && result.totalCount > 0 && (
                    <Pagination
                        pageNumber={filters.pageNumber}
                        pageSize={filters.pageSize}
                        totalCount={result.totalCount}
                        onPageChange={p => updateParams({ pageNumber: p }, false)}
                        onPageSizeChange={s => updateParams({ pageSize: s })}
                    />
                )}
            </section>

            <RectifyModal open={Boolean(rectifyDoc)} document={rectifyDoc} onClose={() => setRectifyDoc(null)} onRectified={replaceDoc} />
            <RevokeModal open={Boolean(revokeDoc)} document={revokeDoc} onClose={() => setRevokeDoc(null)} onRevoked={replaceDoc} />
        </div>
    );
}

function RowMenu({ disabled, onRectify, onRevoke }) {
    const [open, setOpen] = useState(false);
    const ref = useRef(null);

    useEffect(() => {
        if (!open) return undefined;
        const close = e => { if (!ref.current?.contains(e.target)) setOpen(false); };
        document.addEventListener('mousedown', close);
        return () => document.removeEventListener('mousedown', close);
    }, [open]);

    return (
        <div ref={ref} style={{ position: 'relative' }}>
            <IconButton icon={MoreHorizontal} label="Operar" onClick={() => setOpen(o => !o)} aria-expanded={open} disabled={disabled} />
            {open && (
                <div className="dropdown" style={{ width: 200 }} role="menu">
                    <button className="dropdown-item" role="menuitem" onClick={() => { setOpen(false); onRectify(); }}>
                        <FilePen /> Rectificar
                    </button>
                    <button className="dropdown-item danger" role="menuitem" onClick={() => { setOpen(false); onRevoke(); }}>
                        <Ban /> Revocar
                    </button>
                </div>
            )}
        </div>
    );
}
