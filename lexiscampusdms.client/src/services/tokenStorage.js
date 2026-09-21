/**
 * Persistencia de la sesión. Con "Recordar sesión" se usa localStorage;
 * de lo contrario sessionStorage (la sesión termina al cerrar el navegador).
 */
const KEY = 'lexiscampus.session';

function read(storage) {
    try {
        const raw = storage.getItem(KEY);
        return raw ? JSON.parse(raw) : null;
    } catch {
        return null;
    }
}

function activeStorage() {
    if (read(localStorage)) return localStorage;
    if (read(sessionStorage)) return sessionStorage;
    return null;
}

export const tokenStorage = {
    get() {
        return read(localStorage) ?? read(sessionStorage);
    },

    /** @param {{accessToken:string, refreshToken:string, expiresAt:number, user:object}} session */
    save(session, remember) {
        this.clear();
        const storage = remember ? localStorage : sessionStorage;
        storage.setItem(KEY, JSON.stringify(session));
    },

    /** Actualiza los tokens tras un refresh conservando el almacenamiento elegido. */
    update(partial) {
        const storage = activeStorage();
        const current = storage ? read(storage) : null;
        if (!storage || !current) return;
        storage.setItem(KEY, JSON.stringify({ ...current, ...partial }));
    },

    clear() {
        localStorage.removeItem(KEY);
        sessionStorage.removeItem(KEY);
    },

    getAccessToken() {
        return this.get()?.accessToken ?? null;
    },

    getRefreshToken() {
        return this.get()?.refreshToken ?? null;
    }
};
