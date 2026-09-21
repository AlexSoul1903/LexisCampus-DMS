import { useState } from 'react';
import { FilePen } from 'lucide-react';
import { toast } from 'sonner';
import { Alert, Button, Field, Modal, Textarea } from '@/components/ui';
import FileDropzone from './FileDropzone';
import HashBox from './HashBox';
import { documentService } from '@/services/documentService';
import { getErrorMessage } from '@/utils/errors';
import { useFileHash } from '@/hooks/useFileHash';

export default function RectifyModal({ open, onClose, document: doc, onRectified }) {
    const { file, hash, hashing, error: fileError, setError: setFileError, selectFile } = useFileHash();
    const [reason, setReason] = useState('');
    const [reasonError, setReasonError] = useState(null);
    const [progress, setProgress] = useState(0);
    const [saving, setSaving] = useState(false);

    const reset = () => {
        selectFile(null); setReason(''); setReasonError(null); setProgress(0);
    };

    const close = () => {
        if (saving) return;
        reset();
        onClose();
    };

    const sameAsCurrent = hash && doc?.currentFileHash && hash.toLowerCase() === doc.currentFileHash.toLowerCase();

    const submit = async e => {
        e.preventDefault();
        let ok = true;
        if (!file) { setFileError('Adjunte el archivo corregido.'); ok = false; }
        const r = reason.trim();
        if (r.length < 5) { setReasonError('El motivo debe tener al menos 5 caracteres.'); ok = false; }
        else if (r.length > 500) { setReasonError('Máximo 500 caracteres.'); ok = false; }
        else setReasonError(null);
        if (!ok || hashing) return;

        setSaving(true);
        try {
            const updated = await documentService.rectify(doc.id, { file, changeReason: r }, setProgress);
            if (hash && updated?.currentFileHash && updated.currentFileHash.toLowerCase() !== hash) {
                toast.warning('Rectificado, pero el hash del servidor no coincide con el calculado localmente.');
            } else {
                toast.success(`Documento rectificado a la versión v${updated?.currentVersion ?? ''}`, {
                    description: 'Integridad SHA-256 verificada entre cliente y servidor.'
                });
            }
            reset();
            onRectified?.(updated);
            onClose();
        } catch (err) {
            toast.error('No se pudo rectificar el documento', { description: getErrorMessage(err) });
        } finally {
            setSaving(false);
        }
    };

    return (
        <Modal
            open={open}
            onClose={close}
            size="lg"
            icon={FilePen}
            tone="warning"
            title="Rectificar documento"
            description={`Se generará la versión v${(doc?.currentVersion ?? 0) + 1}. Las versiones anteriores se conservan de forma inmutable.`}
            footer={
                <>
                    <Button onClick={close} disabled={saving}>Cancelar</Button>
                    <Button variant="primary" icon={FilePen} loading={saving} type="submit" form="rectify-form" disabled={hashing || sameAsCurrent}>
                        {saving ? `Subiendo ${progress}%` : 'Registrar rectificación'}
                    </Button>
                </>
            }
        >
            <form id="rectify-form" className="stack" onSubmit={submit} noValidate>
                <Field label="Archivo corregido" required error={fileError}>
                    <FileDropzone file={file} onChange={selectFile} error={fileError} onError={setFileError} disabled={saving} compact />
                </Field>
                {file && <HashBox hash={hash} loading={hashing} />}
                {sameAsCurrent && (
                    <Alert tone="warning" title="Archivo idéntico">
                        El archivo seleccionado es idéntico a la versión vigente (mismo hash SHA-256).
                    </Alert>
                )}
                <Textarea
                    label="Motivo de la rectificación"
                    required
                    placeholder="Ej. Corrección del nombre del estudiante según acta de nacimiento"
                    value={reason}
                    onChange={e => setReason(e.target.value)}
                    error={reasonError}
                    hint={`${reason.trim().length}/500 caracteres`}
                    maxLength={500}
                />
            </form>
        </Modal>
    );
}
