import axios from 'axios';
import { tokenStorage } from './tokenStorage';

/**
 * Cliente HTTP global. Todas las rutas son relativas a /api; en desarrollo
 * Vite hace proxy hacia el backend ASP.NET Core (ver vite.config.js).
 */
const apiClient = axios.create({
    baseURL: '/api',
    headers: { 'Content-Type': 'application/json' },
    timeout: 60000
});

// Cliente sin interceptores para el refresh (evita bucles de reintento).
const refreshClient = axios.create({ baseURL: '/api' });

const SESSION_EXPIRED_EVENT = 'lexiscampus:session-expired';

apiClient.interceptors.request.use(config => {
    const token = tokenStorage.getAccessToken();
    if (token) {
        config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
});

let isRefreshing = false;
let pendingQueue = [];

function flushQueue(error, token = null) {
    pendingQueue.forEach(({ resolve, reject }) => (error ? reject(error) : resolve(token)));
    pendingQueue = [];
}

function isAuthEndpoint(url = '') {
    return url.includes('/auth/login') || url.includes('/auth/refresh-token');
}

/**
 * Interceptor de refresh: ante un 401 pausa las peticiones en cola, solicita
 * un nuevo access token con el refresh token y reintenta la petición original.
 */
apiClient.interceptors.response.use(
    response => response,
    async error => {
        const original = error.config;
        const status = error.response?.status;

        if (status !== 401 || !original || original._retry || isAuthEndpoint(original.url)) {
            return Promise.reject(error);
        }

        const refreshToken = tokenStorage.getRefreshToken();
        if (!refreshToken) {
            window.dispatchEvent(new Event(SESSION_EXPIRED_EVENT));
            return Promise.reject(error);
        }

        if (isRefreshing) {
            return new Promise((resolve, reject) => {
                pendingQueue.push({ resolve, reject });
            }).then(token => {
                original.headers.Authorization = `Bearer ${token}`;
                return apiClient(original);
            });
        }

        original._retry = true;
        isRefreshing = true;

        try {
            const { data } = await refreshClient.post('/auth/refresh-token', { refreshToken });
            const payload = data?.data;
            if (!data?.isSuccess || !payload?.accessToken) {
                throw new Error(data?.message || 'No se pudo renovar la sesión.');
            }

            tokenStorage.update({
                accessToken: payload.accessToken,
                refreshToken: payload.refreshToken,
                expiresAt: Date.now() + payload.expiresInSeconds * 1000,
                user: payload.user
            });

            flushQueue(null, payload.accessToken);
            original.headers.Authorization = `Bearer ${payload.accessToken}`;
            return apiClient(original);
        } catch (refreshError) {
            flushQueue(refreshError);
            tokenStorage.clear();
            window.dispatchEvent(new Event(SESSION_EXPIRED_EVENT));
            return Promise.reject(refreshError);
        } finally {
            isRefreshing = false;
        }
    }
);

export { SESSION_EXPIRED_EVENT };
export default apiClient;
