import { useEffect, useMemo, useState } from 'react';
import { FileWarning } from 'lucide-react';
import { documentService } from '@/services/documentService';
import { useAuth } from '@/context/AuthContext';
import { getErrorMessage } from '@/utils/errors';
import { isImageFile } from '@/utils/files';

/**
 * Visor del documento (PDF o imagen) con marca de agua dinámica que identifica
 * al usuario que consulta, la fecha y hora, como disuasivo ante capturas.
 *
 * `concealed` oculta el visor mientras hay un modal abierto: el plugin PDF del
 * navegador se pinta por encima de cualquier capa HTML e ignora el z-index.
 */
export default function DocumentViewer({ documentId, versionNumber, revoked, concealed }) {
    const { user } = useAuth();
    const [state, setState] = useState({ loading: true, url: null, type: null, error: null });

    useEffect(() => {
        let objectUrl = null;
        let cancelled = false;
        setState({ loading: true, url: null, type: null, error: null });

        documentService
            .download(documentId, { versionNumber, inline: true })
            .then(({ blob, fileName }) => {
                if (cancelled) return;
                objectUrl = URL.createObjectURL(blob);
                const type = isImageFile(blob.type || fileName || '') ? 'image' : 'pdf';
                setState({ loading: false, url: objectUrl, type, error: null });
            })
            .catch(err => {
                if (!cancelled) setState({ loading: false, url: null, type: null, error: getErrorMessage(err, 'No se pudo cargar el archivo.') });
            });

        return () => {
            cancelled = true;
            if (objectUrl) URL.revokeObjectURL(objectUrl);
        };
    }, [documentId, versionNumber]);

    const watermarkText = useMemo(() => {
        const stamp = new Date().toLocaleString('es-DO', { dateStyle: 'short', timeStyle: 'short' });
        const base = `${user?.username ?? 'usuario'} · ${stamp}`;
        return revoked ? `REVOCADO · ${base}` : `LexisCampus · ${base}`;
    }, [user, revoked]);

    return (
        <div className="viewer" style={concealed ? { visibility: 'hidden' } : undefined}>
            {state.loading && (
                <div className="viewer-center">
                    <div className="stack-sm" style={{ alignItems: 'center' }}>
                        <span className="spinner" style={{ width: 28, height: 28, color: 'var(--primary-600)' }} />
                        <span>Cargando documento…</span>
                    </div>
                </div>
            )}
            {state.error && (
                <div className="viewer-center">
                    <div className="stack-sm" style={{ alignItems: 'center', maxWidth: 360 }}>
                        <FileWarning size={32} />
                        <strong style={{ color: 'var(--gray-800)' }}>Vista previa no disponible</strong>
                        <span className="text-sm">{state.error}</span>
                    </div>
                </div>
            )}
            {state.url && state.type === 'pdf' && (
                <iframe src={`${state.url}#toolbar=0&navpanes=0`} title="Visor de documento" />
            )}
            {state.url && state.type === 'image' && <img src={state.url} alt="Vista previa del documento" />}
            {state.url && (
                <div className="watermark" aria-hidden>
                    {Array.from({ length: 24 }).map((_, i) => (
                        <span key={i} style={revoked ? { color: 'var(--danger-700)' } : undefined}>{watermarkText}</span>
                    ))}
                </div>
            )}
        </div>
    );
}
