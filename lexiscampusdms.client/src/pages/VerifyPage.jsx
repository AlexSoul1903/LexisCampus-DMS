import { useCallback, useEffect, useState } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import {
    BadgeCheck, Ban, FileQuestion, FileSearch, Fingerprint, Hash, LogIn, QrCode, RotateCcw, Search, ShieldCheck
} from 'lucide-react';
import { Alert, Button, Card, Field, Input, Skeleton } from '@/components/ui';
import BrandMark from '@/components/BrandMark';
import FileDropzone from '@/components/documents/FileDropzone';
import HashBox from '@/components/documents/HashBox';
import QrScanner from '@/components/verification/QrScanner';
import { useAuth } from '@/context/AuthContext';
import { documentStatusLabel, documentTypeLabel } from '@/config/constants';
import { publicVerificationService } from '@/services/publicVerificationService';
import { formatDate, formatDateTime } from '@/utils/format';
import { getErrorMessage } from '@/utils/errors';
import { useFileHash } from '@/hooks/useFileHash';

const HASH_RE = /^[a-f0-9]{64}$/i;
const GUID_RE = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

export default function VerifyPage() {
    const { hash: routeHash } = useParams();
    const navigate = useNavigate();
    const { isAuthenticated } = useAuth();

    const [mode, setMode] = useState('hash');
    const [input, setInput] = useState(routeHash ?? '');
    const [inputError, setInputError] = useState(null);
    const [state, setState] = useState({ loading: false, result: null, error: null, query: null, source: null });

    const verify = useCallback(async (token, source = 'hash') => {
        setState({ loading: true, result: null, error: null, query: token, source });
        try {
            const result = await publicVerificationService.verify(token);
            setState({ loading: false, result, error: null, query: token, source });
        } catch (err) {
            setState({ loading: false, result: null, error: getErrorMessage(err, 'No se pudo completar la verificación.'), query: token, source });
        }
    }, []);

    // Enlace directo /verify/:hash (p. ej. desde el QR del documento).
    useEffect(() => {
        if (routeHash) verify(routeHash, 'link');
    }, [routeHash, verify]);

    const onHashed = useCallback(digest => verify(digest, 'file'), [verify]);
    const {
        file, hash: fileHash, hashing, error: fileError, setError: setFileError, selectFile
    } = useFileHash({ onHashed });

    const submitHash = e => {
        e.preventDefault();
        const token = input.trim();
        if (!token) { setInputError('Ingrese el hash SHA-256 o el código del documento.'); return; }
        if (!HASH_RE.test(token) && !GUID_RE.test(token)) {
            setInputError('Formato inválido. Un hash SHA-256 tiene 64 caracteres hexadecimales.');
            return;
        }
        setInputError(null);
        navigate(`/verify/${token.toLowerCase()}`, { replace: true });
        if (routeHash?.toLowerCase() === token.toLowerCase()) verify(token, 'hash');
    };

    const onQr = useCallback(token => {
        setInput(token);
        navigate(`/verify/${encodeURIComponent(token)}`, { replace: true });
    }, [navigate]);

    const reset = () => {
        setState({ loading: false, result: null, error: null, query: null, source: null });
        setInput('');
        selectFile(null);
        navigate('/verify', { replace: true });
    };

    const hasOutcome = state.loading || state.result || state.error;

    return (
        <div className="public">
            <header className="public-header">
                <Link to="/verify" className="row" style={{ gap: 12, textDecoration: 'none' }}>
                    <div className="brand-mark"><BrandMark /></div>
                    <div>
                        <div className="brand-name">LexisCampus</div>
                        <div className="brand-sub">Portal de verificación</div>
                    </div>
                </Link>
                <div className="spacer" />
                <Link to={isAuthenticated ? '/dashboard' : '/login'} className="btn btn-sm">
                    <LogIn />{isAuthenticated ? 'Ir al sistema' : 'Acceso institucional'}
                </Link>
            </header>

            <main className="public-main">
                <div className="public-hero">
                    <div className="seal"><ShieldCheck /></div>
                    <h1>Verificación de autenticidad</h1>
                    <p>Valide la integridad y vigencia de documentos académicos emitidos por la institución.<br />Servicio gratuito para empleadores, universidades y organismos públicos.</p>
                </div>

                {!hasOutcome && (
                    <Card>
                        <div className="stack">
                            <div className="tabs" role="tablist">
                                <TabButton id="hash" mode={mode} setMode={setMode} icon={Hash}>Hash SHA-256</TabButton>
                                <TabButton id="qr" mode={mode} setMode={setMode} icon={QrCode}>Código QR</TabButton>
                                <TabButton id="file" mode={mode} setMode={setMode} icon={Fingerprint}>Archivo</TabButton>
                            </div>

                            {mode === 'hash' && (
                                <form onSubmit={submitHash} className="stack">
                                    <Input
                                        label="Hash SHA-256 o código del documento"
                                        icon={Search}
                                        placeholder="e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855"
                                        value={input}
                                        onChange={e => { setInput(e.target.value); setInputError(null); }}
                                        error={inputError}
                                        hint="Se encuentra al pie del documento, junto al código QR."
                                        spellCheck={false}
                                        style={{ fontFamily: 'var(--font-mono)', fontSize: 13 }}
                                    />
                                    <Button type="submit" variant="primary" size="lg" icon={ShieldCheck} block>Verificar documento</Button>
                                </form>
                            )}

                            {mode === 'qr' && <QrScanner onDetected={onQr} />}

                            {mode === 'file' && (
                                <div className="stack">
                                    <Field error={fileError} hint="El archivo se procesa localmente: solo se envía su huella SHA-256, nunca el contenido.">
                                        <FileDropzone file={file} onChange={selectFile} error={fileError} onError={setFileError} />
                                    </Field>
                                    {file && <HashBox hash={fileHash} loading={hashing} />}
                                </div>
                            )}
                        </div>
                    </Card>
                )}

                {state.loading && (
                    <Card>
                        <div className="stack" style={{ alignItems: 'center', padding: '24px 0' }}>
                            <span className="spinner" style={{ width: 32, height: 32, color: 'var(--primary-600)' }} />
                            <strong>Consultando el registro institucional…</strong>
                            <Skeleton width="60%" />
                        </div>
                    </Card>
                )}

                {state.error && (
                    <Card>
                        <div className="stack">
                            <Alert tone="danger" title="No se pudo completar la verificación">{state.error}</Alert>
                            <div className="row"><Button icon={RotateCcw} onClick={reset}>Nueva consulta</Button>
                                <Button variant="primary" onClick={() => verify(state.query, state.source)}>Reintentar</Button></div>
                        </div>
                    </Card>
                )}

                {state.result && <VerificationResult result={state.result} query={state.query} source={state.source} onReset={reset} />}

                <p className="text-xs muted" style={{ textAlign: 'center', marginTop: 28 }}>
                    Por protección de datos, la matrícula del estudiante se muestra anonimizada. Consultas limitadas a 30 por minuto.
                </p>
            </main>
        </div>
    );
}

function TabButton({ id, mode, setMode, icon: Icon, children }) {
    return (
        <button type="button" role="tab" aria-selected={mode === id} className={`tab ${mode === id ? 'active' : ''}`} onClick={() => setMode(id)}>
            <Icon aria-hidden />{children}
        </button>
    );
}

function VerificationResult({ result, query, source, onReset }) {
    const { status, data, message } = result;

    if (status === 'not-found') {
        return (
            <Card className="verify-result" flush>
                <div className="verify-banner unknown">
                    <div className="vb-icon"><FileQuestion /></div>
                    <div>
                        <h2>Documento no encontrado o alterado</h2>
                        <p>{message ?? 'No existe un documento institucional con esta huella digital.'}</p>
                    </div>
                </div>
                <div className="verify-body stack">
                    <p className="soft">
                        {source === 'file'
                            ? 'La huella del archivo no coincide con ningún documento emitido. Si el archivo proviene de la institución, es probable que haya sido modificado después de su emisión.'
                            : 'Verifique que el hash se haya ingresado completo y sin espacios. Si el problema persiste, el documento podría no ser auténtico.'}
                    </p>
                    <div><div className="field-label" style={{ marginBottom: 6 }}>Huella consultada</div><HashBox hash={query} /></div>
                    <div><Button icon={Search} onClick={onReset}>Nueva verificación</Button></div>
                </div>
            </Card>
        );
    }

    const valid = status === 'valid';

    return (
        <Card className="verify-result" flush>
            <div className={`verify-banner ${valid ? 'valid' : 'revoked'}`}>
                <div className="vb-icon">{valid ? <BadgeCheck /> : <Ban />}</div>
                <div>
                    <h2>{valid ? 'Documento auténtico y vigente' : 'Documento revocado'}</h2>
                    <p>{valid ? 'La huella digital coincide con el registro oficial de la institución.' : 'Este documento fue anulado formalmente y carece de validez legal.'}</p>
                </div>
            </div>
            <div className="verify-body stack">
                {!valid && data.advertenciaRevocacion && (
                    <Alert tone="danger" icon={Ban} title="Motivo legal y resolución">{data.advertenciaRevocacion.replace(/^⚠️\s*/, '')}</Alert>
                )}
                <dl className="dl">
                    <dt>Documento</dt><dd>{data.titulo}</dd>
                    <dt>Tipo</dt><dd>{documentTypeLabel(data.tipoDocumento)}</dd>
                    <dt>Estudiante</dt><dd className="mono">{data.matriculaAnonimizada}</dd>
                    <dt>Fecha de emisión</dt><dd>{formatDate(data.fechaEmision)}</dd>
                    <dt>Versión</dt><dd>v{data.version}{data.version > 1 ? ' (rectificada)' : ''}</dd>
                    <dt>Estado</dt><dd>{documentStatusLabel(data.estado)}</dd>
                    <dt>Firmado por</dt><dd>{data.decanoFirmante}</dd>
                    <dt>Sello institucional</dt><dd className="mono break-all">{data.selloInstitucional}</dd>
                </dl>
                <div><div className="field-label" style={{ marginBottom: 6 }}>Huella SHA-256 registrada</div><HashBox hash={data.hashSha256} /></div>
                <div className="row-wrap">
                    <span className="text-xs muted" style={{ flex: 1 }}>Consulta realizada el {formatDateTime(new Date())}</span>
                    <Button icon={FileSearch} onClick={onReset}>Verificar otro documento</Button>
                </div>
            </div>
        </Card>
    );
}
