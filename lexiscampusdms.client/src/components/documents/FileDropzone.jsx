import { useRef, useState } from 'react';
import { FileText, Image as ImageIcon, RefreshCw, UploadCloud, X } from 'lucide-react';
import { IconButton } from '@/components/ui';
import { UPLOAD_RULES } from '@/config/constants';
import { formatBytes } from '@/utils/format';
import { isImageFile, validateUploadFile } from '@/utils/files';

/**
 * Área de arrastre para documentos (PDF / PNG / JPG, máx. 50 MB).
 */
export default function FileDropzone({ file, onChange, error, onError, disabled, compact }) {
    const inputRef = useRef(null);
    const [dragging, setDragging] = useState(false);

    const accept = f => {
        const validation = validateUploadFile(f);
        if (validation) {
            onError?.(validation);
            return;
        }
        onError?.(null);
        onChange(f);
    };

    const onDrop = e => {
        e.preventDefault();
        setDragging(false);
        if (disabled) return;
        const f = e.dataTransfer.files?.[0];
        if (f) accept(f);
    };

    if (file) {
        const img = isImageFile(file.type || file.name);
        return (
            <div className="file-card">
                <div className={`file-icon ${img ? 'img' : ''}`}>{img ? <ImageIcon /> : <FileText />}</div>
                <div className="file-meta">
                    <strong className="truncate">{file.name}</strong>
                    <span className="muted text-sm">{formatBytes(file.size)} · {img ? 'Imagen' : 'Documento PDF'}</span>
                </div>
                {!disabled && (
                    <>
                        <IconButton icon={RefreshCw} label="Reemplazar archivo" onClick={() => inputRef.current?.click()} />
                        <IconButton icon={X} label="Quitar archivo" onClick={() => onChange(null)} />
                    </>
                )}
                <input
                    ref={inputRef}
                    type="file"
                    hidden
                    accept={UPLOAD_RULES.accept}
                    onChange={e => { if (e.target.files?.[0]) accept(e.target.files[0]); e.target.value = ''; }}
                />
            </div>
        );
    }

    return (
        <div
            className={`dropzone ${dragging ? 'drag' : ''} ${error ? 'error' : ''}`}
            style={compact ? { padding: '24px 16px' } : undefined}
            role="button"
            tabIndex={0}
            onClick={() => !disabled && inputRef.current?.click()}
            onKeyDown={e => (e.key === 'Enter' || e.key === ' ') && inputRef.current?.click()}
            onDragOver={e => { e.preventDefault(); setDragging(true); }}
            onDragLeave={() => setDragging(false)}
            onDrop={onDrop}
            aria-label="Seleccionar o arrastrar archivo"
        >
            <div className="dropzone-icon"><UploadCloud aria-hidden /></div>
            <strong>Arrastre el archivo aquí o <span style={{ color: 'var(--primary-600)' }}>explore su equipo</span></strong>
            <span className="muted text-sm">PDF, PNG o JPG · Tamaño máximo 50 MB</span>
            <input
                ref={inputRef}
                type="file"
                accept={UPLOAD_RULES.accept}
                onChange={e => { if (e.target.files?.[0]) accept(e.target.files[0]); e.target.value = ''; }}
            />
        </div>
    );
}
