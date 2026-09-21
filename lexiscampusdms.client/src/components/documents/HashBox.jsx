import { Copy, Fingerprint } from 'lucide-react';
import { toast } from 'sonner';
import { IconButton, Skeleton } from '@/components/ui';
import { copyToClipboard } from '@/utils/files';

export default function HashBox({ hash, loading, label = 'SHA-256' }) {
    if (loading) {
        return (
            <div className="hash-box" aria-busy="true">
                <Fingerprint aria-hidden />
                <span className="hash-text">Calculando huella digital {label}…</span>
                <span className="spinner" style={{ color: '#6ee7b7' }} />
            </div>
        );
    }
    if (!hash) return <Skeleton height={40} />;

    return (
        <div className="hash-box">
            <Fingerprint aria-hidden />
            <span className="hash-text">{hash}</span>
            <IconButton
                icon={Copy}
                label="Copiar hash"
                onClick={async () => {
                    const ok = await copyToClipboard(hash);
                    ok ? toast.success('Hash copiado al portapapeles') : toast.error('No se pudo copiar');
                }}
            />
        </div>
    );
}
