/* eslint-disable react-refresh/only-export-components */
import { createContext, useCallback, useContext, useEffect, useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { toast } from 'sonner';
import { authService } from '@/services/authService';
import { SESSION_EXPIRED_EVENT } from '@/services/apiClient';

const AuthContext = createContext(null);

export function AuthProvider({ children }) {
    const navigate = useNavigate();
    const [user, setUser] = useState(() => authService.getStoredSession()?.user ?? null);

    const login = useCallback(async (username, password, remember) => {
        const loggedUser = await authService.login(username, password, remember);
        setUser(loggedUser);
        return loggedUser;
    }, []);

    const logout = useCallback((options = {}) => {
        authService.logout();
        setUser(null);
        navigate('/login', { replace: true, state: options.from ? { from: options.from } : undefined });
    }, [navigate]);

    // Sesión expirada (refresh token inválido): limpiar y redirigir.
    useEffect(() => {
        const onExpired = () => {
            if (!authService.getStoredSession()) {
                setUser(null);
                toast.warning('Su sesión ha expirado', { description: 'Inicie sesión nuevamente para continuar.' });
                navigate('/login', { replace: true });
            }
        };
        window.addEventListener(SESSION_EXPIRED_EVENT, onExpired);
        return () => window.removeEventListener(SESSION_EXPIRED_EVENT, onExpired);
    }, [navigate]);

    // Sincroniza el cierre de sesión entre pestañas.
    useEffect(() => {
        const onStorage = () => setUser(authService.getStoredSession()?.user ?? null);
        window.addEventListener('storage', onStorage);
        return () => window.removeEventListener('storage', onStorage);
    }, []);

    const value = useMemo(() => {
        const role = user?.role ?? null;
        return {
            user,
            role,
            isAuthenticated: Boolean(user),
            hasRole: (...roles) => Boolean(role) && roles.flat().some(r => r.toLowerCase() === role.toLowerCase()),
            login,
            logout
        };
    }, [user, login, logout]);

    return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
    const ctx = useContext(AuthContext);
    if (!ctx) throw new Error('useAuth debe usarse dentro de <AuthProvider>.');
    return ctx;
}
