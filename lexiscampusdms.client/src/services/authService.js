import apiClient from './apiClient';
import { tokenStorage } from './tokenStorage';

function toSession(payload) {
    return {
        accessToken: payload.accessToken,
        refreshToken: payload.refreshToken,
        expiresAt: Date.now() + payload.expiresInSeconds * 1000,
        user: payload.user
    };
}

export const authService = {
    /**
     * POST /api/auth/login
     * @returns {Promise<object>} Usuario autenticado (UserInfoDto).
     */
    async login(username, password, remember) {
        const { data } = await apiClient.post('/auth/login', { username, password });
        const session = toSession(data.data);
        tokenStorage.save(session, remember);
        return session.user;
    },

    /** POST /api/auth/refresh-token */
    async refresh() {
        const refreshToken = tokenStorage.getRefreshToken();
        const { data } = await apiClient.post('/auth/refresh-token', { refreshToken });
        const session = toSession(data.data);
        tokenStorage.update(session);
        return session.user;
    },

    /** Destruye la sesión local (el backend no expone endpoint de logout). */
    logout() {
        tokenStorage.clear();
    },

    getStoredSession() {
        return tokenStorage.get();
    }
};
