import { useState } from 'react';
import { Link, useNavigate, useSearchParams } from 'react-router-dom';
import { toast } from 'sonner';
import {
    ArrowLeft, Check, CheckCircle2, ClipboardList, Eye, FileUp, Fingerprint, ShieldCheck, TriangleAlert, UploadCloud
} from 'lucide-react';
import { Alert, Button, Card, Field, Input, PageHeader, Select, Textarea } from '@/components/ui';
import FileDropzone from '@/components/documents/FileDropzone';
import HashBox from '@/components/documents/HashBox';
import { DOCUMENT_TYPES, documentTypeLabel } from '@/config/constants';
import { documentService } from '@/services/documentService';
import { getErrorList, getErrorMessage } from '@/utils/errors';
import { useFileHash } from '@/hooks/useFileHash';
import { formatBytes } from '@/utils/format';

const TYPE_OPTIONS = DOCUMENT_TYPES.map(t => ({ value: t.value, label: t.label }));

export default function UploadDocumentPage() {
    const navigate = useNavigate();
    const [params] = useSearchParams();

    const { file, hash, hashing, error: fileError, setError: setFileError, selectFile } = useFileHash();
    const [form, setForm] = useState({
        title: '',
        studentRegistration: params.get('matricula') ?? '',
        documentType: '',
        initialComment: ''
    });
    const [errors, setErrors] = useState({});
    const [progress, setProgress] = useState(0);
    const [submitting, setSubmitting] = useState(false);
    const [serverErrors, setServerErrors] = useState([]);
    const [created, setCreated] = useState(null);

    // Sugiere un título a partir del nombre del archivo.
    const onFile = f => {
        selectFile(f);
        if (f && !form.title) {
            const base = f.name.replace(/\.[^.]+$/, '').replace(/[_-]+/g, ' ').trim();
            setForm(s => ({ ...s, title: base.charAt(0).toUpperCase() + base.slice(1) }));
        }
    };

    const set = key => e => {
        setForm(f => ({ ...f, [key]: e.target.value }));
        if (errors[key]) setErrors(er => ({ ...er, [key]: null }));
    };

    const metaComplete = form.title.trim() && form.studentRegistration.trim() && form.documentType;
    const step = !file ? 1 : !metaComplete ? 2 : 3;

    const validate = () => {
        const next = {};
        if (!file) next.file = 'Adjunte el documento a certificar.';
        if (!form.title.trim()) next.title = 'El título es obligatorio.';
        else if (form.title.length > 200) next.title = 'Máximo 200 caracteres.';
        if (!form.studentRegistration.trim()) next.studentRegistration = 'La matrícula es obligatoria.';
        else if (form.studentRegistration.length > 50) next.studentRegistration = 'Máximo 50 caracteres.';
        if (!form.documentType) next.documentType = 'Seleccione el tipo de documento.';
        if (form.initialComment.length > 500) next.initialComment = 'Máximo 500 caracteres.';
        setErrors(next);
        if (next.file) setFileError(next.file);
        return !Object.keys(next).length;
    };

    const submit = async e => {
        e.preventDefault();
        setServerErrors([]);
        if (!validate() || hashing) return;

        setSubmitting(true);
        setProgress(0);
        try {
            const doc = await documentService.upload({ file, ...form }, setProgress);
            const match = doc?.currentFileHash?.toLowerCase() === hash;
            setCreated({ doc, match });
            toast.success('Documento certificado correctamente', {
                description: match ? 'La huella SHA-256 coincide con el servidor.' : undefined
            });
        } catch (err) {
            setServerErrors(getErrorList(err));
            toast.error('No se pudo registrar el documento', { description: getErrorMessage(err) });
        } finally {
            setSubmitting(false);
        }
    };

    const reset = () => {
        selectFile(null); setErrors({}); setServerErrors([]); setCreated(null); setProgress(0);
        setForm({ title: '', studentRegistration: '', documentType: '', initialComment: '' });
    };

    const header = (
        <PageHeader
            breadcrumbs={
                <div className="breadcrumbs">
                    <Link to="/documents">Documentos</Link><span>/</span><span>Cargar documento</span>
                </div>
            }
            eyebrow="Certificación"
            eyebrowIcon={UploadCloud}
            title="Cargar y registrar documento"
            description="Flujo guiado de certificación: el archivo se sella con su huella SHA-256 antes del envío para garantizar su integridad."
        />
    );

    if (created) {
        const { doc, match } = created;
        return (
            <div className="page">
                {header}
                <Card>
                    <div className="stack" style={{ alignItems: 'center', textAlign: 'center', padding: '24px 0' }}>
                        <div className="stat-icon success" style={{ width: 64, height: 64, borderRadius: 18 }}>
                            <CheckCircle2 style={{ width: 32, height: 32 }} />
                        </div>
                        <div>
                            <h2>Documento certificado</h2>
                            <p className="muted" style={{ marginTop: 6 }}>
                                <strong>{doc.title}</strong> · Matrícula {doc.studentRegistration} · {documentTypeLabel(doc.documentType)}
                            </p>
                        </div>
                        <div style={{ width: '100%', maxWidth: 640, textAlign: 'left' }} className="stack-sm">
                            <HashBox hash={doc.currentFileHash} />
                            {match ? (
                                <Alert tone="success" icon={ShieldCheck} title="Integridad verificada">
                                    El hash calculado en su equipo coincide exactamente con el registrado por el servidor.
                                </Alert>
                            ) : (
                                <Alert tone="warning" icon={TriangleAlert} title="Diferencia de hash">
                                    El hash del servidor no coincide con el calculado localmente. Revise el documento.
                                </Alert>
                            )}
                        </div>
                        <div className="row-wrap" style={{ justifyContent: 'center' }}>
                            <Button icon={FileUp} onClick={reset}>Cargar otro documento</Button>
                            <Button variant="primary" icon={Eye} onClick={() => navigate(`/documents/${doc.id}`)}>Ver documento</Button>
                        </div>
                    </div>
                </Card>
            </div>
        );
    }

    return (
        <div className="page">
            {header}

            <div className="steps" aria-label="Progreso">
                <StepItem n={1} current={step} icon={FileUp} title="Archivo" text="PDF o imagen < 50 MB" />
                <StepItem n={2} current={step} icon={ClipboardList} title="Metadatos" text="Título, matrícula y tipo" />
                <StepItem n={3} current={step} icon={Fingerprint} title="Seguridad" text="Huella SHA-256 y envío" />
            </div>

            <form onSubmit={submit} noValidate>
                <div className="grid-3" style={{ alignItems: 'start' }}>
                    <div className="span-2 stack">
                        <Card title="1. Documento" subtitle="Arrastre el archivo original a certificar">
                            <Field error={fileError}>
                                <FileDropzone file={file} onChange={onFile} error={fileError} onError={setFileError} disabled={submitting} />
                            </Field>
                        </Card>

                        <Card title="2. Metadatos" subtitle="Información obligatoria para su indexación">
                            <div className="grid-2">
                                <Input
                                    className="span-2"
                                    label="Título oficial del documento"
                                    required
                                    placeholder="Ej. Récord de notas — Ingeniería de Software"
                                    value={form.title}
                                    onChange={set('title')}
                                    error={errors.title}
                                    maxLength={200}
                                />
                                <Input
                                    label="Matrícula del estudiante"
                                    required
                                    placeholder="Ej. 2021-0456"
                                    value={form.studentRegistration}
                                    onChange={set('studentRegistration')}
                                    error={errors.studentRegistration}
                                    maxLength={50}
                                />
                                <Select
                                    label="Tipo de documento"
                                    required
                                    placeholder="Seleccione un tipo"
                                    options={TYPE_OPTIONS}
                                    value={form.documentType}
                                    onChange={set('documentType')}
                                    error={errors.documentType}
                                />
                                <Textarea
                                    className="span-2"
                                    label="Comentario inicial"
                                    placeholder="Observaciones sobre la emisión (opcional)"
                                    rows={3}
                                    value={form.initialComment}
                                    onChange={set('initialComment')}
                                    error={errors.initialComment}
                                    hint={`${form.initialComment.length}/500`}
                                    maxLength={500}
                                />
                            </div>
                        </Card>
                    </div>

                    <div className="stack" style={{ position: 'sticky', top: 84 }}>
                        <Card title="3. Seguridad e integridad" subtitle="Huella criptográfica del archivo">
                            <div className="stack">
                                {file ? <HashBox hash={hash} loading={hashing} /> : (
                                    <p className="muted text-sm">El hash SHA-256 se calculará localmente en su navegador al seleccionar el archivo. El contenido no sale de su equipo hasta que confirme el registro.</p>
                                )}

                                <dl className="dl" style={{ gridTemplateColumns: '100px minmax(0,1fr)', fontSize: 13 }}>
                                    <dt>Archivo</dt><dd className="truncate">{file?.name ?? '—'}</dd>
                                    <dt>Tamaño</dt><dd>{file ? formatBytes(file.size) : '—'}</dd>
                                    <dt>Tipo</dt><dd>{form.documentType ? documentTypeLabel(Number(form.documentType)) : '—'}</dd>
                                    <dt>Matrícula</dt><dd className="mono">{form.studentRegistration || '—'}</dd>
                                </dl>

                                {serverErrors.length > 0 && (
                                    <Alert tone="danger" title="El servidor rechazó el registro">
                                        <ul style={{ margin: '4px 0 0', paddingLeft: 18 }}>
                                            {serverErrors.map(m => <li key={m}>{m}</li>)}
                                        </ul>
                                    </Alert>
                                )}

                                {submitting && (
                                    <div>
                                        <div className="bar-item-top text-sm"><span>Transfiriendo archivo…</span><span>{progress}%</span></div>
                                        <div className="bar-track"><div className="bar-fill" style={{ width: `${progress}%` }} /></div>
                                    </div>
                                )}

                                <Button type="submit" variant="primary" size="lg" block icon={ShieldCheck} loading={submitting} disabled={hashing}>
                                    {submitting ? 'Certificando…' : 'Certificar y registrar'}
                                </Button>
                                <Link to="/documents" className="btn btn-ghost btn-block"><ArrowLeft />Volver al listado</Link>
                            </div>
                        </Card>
                    </div>
                </div>
            </form>
        </div>
    );
}

function StepItem({ n, current, icon: Icon, title, text }) {
    const state = current > n ? 'done' : current === n ? 'active' : '';
    return (
        <div className={`step ${state}`}>
            <span className="step-num">{state === 'done' ? <Check /> : <Icon />}</span>
            <div>
                <strong>{title}</strong>
                <span>{text}</span>
            </div>
        </div>
    );
}
