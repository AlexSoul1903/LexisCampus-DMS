import { useState } from 'react';
import { Link, Navigate, useLocation, useNavigate } from 'react-router-dom';
import { toast } from 'sonner';
import {
    Eye, EyeOff, Fingerprint, History, Lock, LogIn, ShieldAlert, ShieldCheck, User, WifiOff
} from 'lucide-react';
import { Alert, Button, Input } from '@/components/ui';
import BrandMark from '@/components/BrandMark';
import { useAuth } from '@/context/AuthContext';
import { MAX_LOGIN_ATTEMPTS } from '@/config/constants';
import { getErrorCode, getErrorMessage } from '@/utils/errors';

export default function LoginPage() {
    const { login, isAuthenticated } = useAuth();
    const navigate = useNavigate();
    const location = useLocation();

    const [username, setUsername] = useState('');
    const [password, setPassword] = useState('');
    const [remember, setRemember] = useState(true);
    const [showPassword, setShowPassword] = useState(false);
    const [loading, setLoading] = useState(false);
    const [fieldErrors, setFieldErrors] = useState({});
    const [error, setError] = useState(null); // { kind, message }
    const [attemptsLeft, setAttemptsLeft] = useState(null);

    const redirectTo = location.state?.from?.pathname ?? '/dashboard';

    if (isAuthenticated) return <Navigate to={redirectTo} replace />;

    const submit = async e => {
        e.preventDefault();
        const next = {};
        if (!username.trim()) next.username = 'Ingrese su usuario o correo institucional.';
        if (!password) next.password = 'Ingrese su contraseña.';
        setFieldErrors(next);
        if (Object.keys(next).length) return;

        setLoading(true);
        setError(null);
        try {
            const user = await login(username.trim(), password, remember);
            toast.success(`Bienvenido(a), ${user.fullName?.split(' ')[0] ?? user.username}`);
            navigate(redirectTo, { replace: true });
        } catch (err) {
            const code = getErrorCode(err);
            const message = getErrorMessage(err, 'No fue posible iniciar sesión.');
            if (!err.response) {
                setError({ kind: 'network', message });
            } else if (code === 'ACCOUNT_LOCKED') {
                setError({ kind: 'locked', message });
                setAttemptsLeft(0);
            } else if (code === 'ACCOUNT_INACTIVE') {
                setError({ kind: 'inactive', message });
            } else {
                const match = /quedan\s+(\d+)/i.exec(message);
                setAttemptsLeft(match ? Number(match[1]) : null);
                setError({ kind: 'credentials', message: message.replace(/^Credenciales inválidas\.\s*/i, '') || message });
            }
            setPassword('');
        } finally {
            setLoading(false);
        }
    };

    const used = attemptsLeft === null ? 0 : MAX_LOGIN_ATTEMPTS - attemptsLeft;

    return (
        <div className="auth">
            <section className="auth-hero">
                <div className="row" style={{ gap: 12 }}>
                    <div className="brand-mark"><BrandMark /></div>
                    <div>
                        <div className="brand-name" style={{ color: '#fff' }}>LexisCampus</div>
                        <div className="brand-sub">Document Management System</div>
                    </div>
                </div>

                <div>
                    <h1>Custodia digital de la <em>historia académica</em> institucional.</h1>
                    <p className="lead">
                        Emisión, rectificación y verificación de documentos universitarios con integridad criptográfica
                        y trazabilidad completa.
                    </p>
                    <div className="auth-features">
                        <div className="auth-feature">
                            <div className="fi"><Fingerprint /></div>
                            <div><strong>Integridad SHA-256</strong><span>Cada archivo se sella con su huella digital.</span></div>
                        </div>
                        <div className="auth-feature">
                            <div className="fi"><History /></div>
                            <div><strong>Versionado inmutable</strong><span>Rectificaciones con historial forense completo.</span></div>
                        </div>
                        <div className="auth-feature">
                            <div className="fi"><ShieldCheck /></div>
                            <div><strong>Verificación pública</strong><span>Terceros validan autenticidad por QR o hash.</span></div>
                        </div>
                    </div>
                </div>

                <div className="auth-foot">© {new Date().getFullYear()} LexisCampus · Registro Académico</div>
            </section>

            <section className="auth-panel">
                <div className="auth-card">
                    <h2>Iniciar sesión</h2>
                    <p className="sub">Acceso exclusivo para personal administrativo autorizado.</p>

                    <form className="stack" onSubmit={submit} noValidate>
                        {error?.kind === 'network' && (
                            <Alert tone="danger" icon={WifiOff} title="Sin conexión con el servidor">{error.message}</Alert>
                        )}
                        {error?.kind === 'locked' && (
                            <Alert tone="danger" icon={Lock} title="Cuenta bloqueada temporalmente">{error.message}</Alert>
                        )}
                        {error?.kind === 'inactive' && (
                            <Alert tone="warning" icon={ShieldAlert} title="Cuenta inactiva">
                                {error.message} Contacte al administrador del sistema.
                            </Alert>
                        )}
                        {error?.kind === 'credentials' && (
                            <Alert tone={attemptsLeft !== null && attemptsLeft <= 2 ? 'danger' : 'warning'} icon={ShieldAlert} title="Credenciales inválidas">
                                {error.message}
                                {attemptsLeft !== null && (
                                    <div className="attempts" aria-label={`${used} de ${MAX_LOGIN_ATTEMPTS} intentos usados`}>
                                        {Array.from({ length: MAX_LOGIN_ATTEMPTS }).map((_, i) => (
                                            <span key={i} className={i < used ? (attemptsLeft <= 2 ? 'used' : 'warn') : ''} />
                                        ))}
                                    </div>
                                )}
                            </Alert>
                        )}

                        <Input
                            label="Usuario o correo institucional"
                            icon={User}
                            placeholder="usuario@lexiscampus.edu"
                            autoComplete="username"
                            autoFocus
                            value={username}
                            onChange={e => setUsername(e.target.value)}
                            error={fieldErrors.username}
                        />
                        <Input
                            label="Contraseña"
                            icon={Lock}
                            type={showPassword ? 'text' : 'password'}
                            placeholder="••••••••"
                            autoComplete="current-password"
                            value={password}
                            onChange={e => setPassword(e.target.value)}
                            error={fieldErrors.password}
                            suffix={
                                <button
                                    type="button"
                                    className="btn btn-ghost btn-icon btn-sm"
                                    onClick={() => setShowPassword(s => !s)}
                                    aria-label={showPassword ? 'Ocultar contraseña' : 'Mostrar contraseña'}
                                >
                                    {showPassword ? <EyeOff /> : <Eye />}
                                </button>
                            }
                        />

                        <label className="checkbox">
                            <input type="checkbox" checked={remember} onChange={e => setRemember(e.target.checked)} />
                            Mantener la sesión iniciada en este equipo
                        </label>

                        <Button type="submit" variant="primary" size="lg" block loading={loading} icon={LogIn}>
                            {loading ? 'Verificando…' : 'Ingresar al sistema'}
                        </Button>

                        <div className="alert alert-info" style={{ fontSize: 12.5 }}>
                            <ShieldCheck aria-hidden />
                            <div>
                                <strong>Política de seguridad</strong>
                                Tras {MAX_LOGIN_ATTEMPTS} intentos fallidos consecutivos la cuenta se bloquea temporalmente por 15 minutos.
                            </div>
                        </div>
                    </form>

                    <p className="text-sm muted" style={{ marginTop: 24, textAlign: 'center' }}>
                        ¿Desea validar un documento? <Link to="/verify">Portal de verificación pública</Link>
                    </p>
                </div>
            </section>
        </div>
    );
}
