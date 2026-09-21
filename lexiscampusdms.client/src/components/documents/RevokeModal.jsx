import { useState } from 'react';
import { Ban, TriangleAlert } from 'lucide-react';
import { toast } from 'sonner';
import { Alert, Button, Input, Modal, Textarea } from '@/components/ui';
import { documentService } from '@/services/documentService';
import { getErrorMessage } from '@/utils/errors';

const EMPTY = { resolutionNumber: '', reason: '', observations: '', confirm: '' };

export default function RevokeModal({ open, onClose, document: doc, onRevoked }) {
    const [form, setForm] = useState(EMPTY);
    const [errors, setErrors] = useState({});
    const [saving, setSaving] = useState(false);

    const set = key => e => setForm(f => ({ ...f, [key]: e.target.value }));

    const close = () => {
        if (saving) return;
        setForm(EMPTY);
        setErrors({});
        onClose();
    };

    const submit = async e => {
        e.preventDefault();
        const next = {};
        if (!form.resolutionNumber.trim()) next.resolutionNumber = 'El número de resolución es obligatorio.';
        else if (form.resolutionNumber.length > 100) next.resolutionNumber = 'Máximo 100 caracteres.';
        if (!form.reason.trim()) next.reason = 'El motivo legal es obligatorio.';
        else if (form.reason.length > 1000) next.reason = 'Máximo 1000 caracteres.';
        if (form.observations.length > 2000) next.observations = 'Máximo 2000 caracteres.';
        if (form.confirm.trim().toUpperCase() !== 'REVOCAR') next.confirm = 'Escriba REVOCAR para confirmar.';
        setErrors(next);
        if (Object.keys(next).length) return;

        setSaving(true);
        try {
            const updated = await documentService.revoke(doc.id, form);
            toast.success('Documento revocado', { description: `Resolución ${form.resolutionNumber} registrada.` });
            setForm(EMPTY);
            onRevoked?.(updated);
            onClose();
        } catch (err) {
            toast.error('No se pudo revocar el documento', { description: getErrorMessage(err) });
        } finally {
            setSaving(false);
        }
    };

    return (
        <Modal
            open={open}
            onClose={close}
            icon={Ban}
            tone="danger"
            title="Revocar documento"
            description={doc?.title}
            footer={
                <>
                    <Button onClick={close} disabled={saving}>Cancelar</Button>
                    <Button variant="danger" icon={Ban} loading={saving} type="submit" form="revoke-form">Revocar definitivamente</Button>
                </>
            }
        >
            <form id="revoke-form" className="stack" onSubmit={submit} noValidate>
                <Alert tone="danger" icon={TriangleAlert} title="Acción irreversible">
                    El documento perderá toda validez legal y académica. La verificación pública mostrará el motivo y la resolución.
                </Alert>
                <Input
                    label="Número de resolución"
                    required
                    placeholder="Ej. RES-2026-045"
                    value={form.resolutionNumber}
                    onChange={set('resolutionNumber')}
                    error={errors.resolutionNumber}
                    maxLength={100}
                />
                <Textarea
                    label="Motivo legal de anulación"
                    required
                    placeholder="Describa el fundamento legal de la revocación"
                    value={form.reason}
                    onChange={set('reason')}
                    error={errors.reason}
                    maxLength={1000}
                />
                <Textarea
                    label="Observaciones"
                    placeholder="Información adicional (opcional)"
                    value={form.observations}
                    onChange={set('observations')}
                    error={errors.observations}
                    maxLength={2000}
                    rows={2}
                />
                <Input
                    label='Escriba "REVOCAR" para confirmar'
                    required
                    value={form.confirm}
                    onChange={set('confirm')}
                    error={errors.confirm}
                    autoComplete="off"
                />
            </form>
        </Modal>
    );
}
