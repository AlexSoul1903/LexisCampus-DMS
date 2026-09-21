import { Ban, Download, Eye, FilePen, FilePlus2 } from 'lucide-react';
import { Badge, Button } from '@/components/ui';
import { useAuth } from '@/context/AuthContext';
import { formatActor, formatBytes, formatDateTime, shortHash } from '@/utils/format';

/**
 * Línea de tiempo forense: evolución del documento desde su creación,
 * cada versión (rectificaciones) y la revocación, si aplica.
 */
export default function DocumentTimeline({ document: doc, versions, selectedVersion, onSelectVersion, onDownloadVersion }) {
    const { user } = useAuth();
    const sorted = [...(versions ?? [])].sort((a, b) => b.versionNumber - a.versionNumber);

    const events = [];

    if (doc?.revokedAtUtc) {
        events.push({
            key: 'revoked',
            tone: 'danger',
            icon: Ban,
            title: 'Documento revocado',
            meta: `${formatDateTime(doc.revokedAtUtc)} · por ${formatActor(doc.revokedBy, user)}`,
            body: (
                <div className="stack-sm">
                    <span><strong>Resolución:</strong> {doc.resolutionNumber}</span>
                    <span><strong>Motivo legal:</strong> {doc.revocationReason}</span>
                    {doc.revocationObservations && <span className="muted">{doc.revocationObservations}</span>}
                </div>
            )
        });
    }

    sorted.forEach(v => {
        const isFirst = v.versionNumber === 1;
        events.push({
            key: `v${v.versionNumber}`,
            version: v.versionNumber,
            tone: isFirst ? 'success' : 'warning',
            icon: isFirst ? FilePlus2 : FilePen,
            title: isFirst ? 'Emisión original' : 'Rectificación',
            meta: `${formatDateTime(v.createdAtUtc)} · por ${formatActor(v.uploadedBy, user)}`,
            body: (
                <div className="row-wrap text-xs muted">
                    <span className="mono" title={v.fileHash}>SHA-256 {shortHash(v.fileHash, 8)}</span>
                    <span>·</span>
                    <span>{formatBytes(v.fileSizeBytes)}</span>
                    <span>·</span>
                    <span>{v.mimeType}</span>
                </div>
            )
        });
    });

    if (!events.length) {
        return <p className="muted text-sm">Sin eventos registrados.</p>;
    }

    const latest = sorted[0]?.versionNumber;

    return (
        <div className="timeline">
            {events.map(ev => (
                <div key={ev.key} className={`tl-item ${ev.version && ev.version === selectedVersion ? 'selected' : ''}`}>
                    <div className={`tl-dot ${ev.tone}`}><ev.icon aria-hidden /></div>
                    <div className="tl-content">
                        <div className="tl-title">
                            {ev.title}
                            {ev.version && <span className="tag-version"><span className="v">v{ev.version}</span></span>}
                            {ev.version === latest && <Badge tone="info">Vigente</Badge>}
                        </div>
                        <div className="tl-meta">{ev.meta}</div>
                        <div className="tl-body">{ev.body}</div>
                        {ev.version && (
                            <div className="row" style={{ marginTop: 8 }}>
                                <Button
                                    size="sm"
                                    variant={ev.version === selectedVersion ? 'soft' : 'ghost'}
                                    icon={Eye}
                                    onClick={() => onSelectVersion?.(ev.version)}
                                >
                                    {ev.version === selectedVersion ? 'En visualización' : 'Ver versión'}
                                </Button>
                                <Button size="sm" variant="ghost" icon={Download} onClick={() => onDownloadVersion?.(ev.version)}>
                                    Descargar
                                </Button>
                            </div>
                        )}
                    </div>
                </div>
            ))}
        </div>
    );
}
