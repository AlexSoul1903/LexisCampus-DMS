const dateFmt = new Intl.DateTimeFormat('es-DO', { day: '2-digit', month: 'short', year: 'numeric' });
const dateTimeFmt = new Intl.DateTimeFormat('es-DO', {
    day: '2-digit', month: 'short', year: 'numeric', hour: '2-digit', minute: '2-digit'
});
const numberFmt = new Intl.NumberFormat('es-DO');

/** El backend devuelve DateTime UTC sin sufijo "Z"; se normaliza antes de parsear. */
export function parseUtc(value) {
    if (!value) return null;
    if (value instanceof Date) return value;
    const hasZone = /[zZ]|[+-]\d{2}:?\d{2}$/.test(value);
    return new Date(hasZone ? value : `${value}Z`);
}

export function formatDate(value) {
    const d = parseUtc(value);
    return d ? dateFmt.format(d) : '—';
}

export function formatDateTime(value) {
    const d = parseUtc(value);
    return d ? dateTimeFmt.format(d) : '—';
}

export function formatRelative(value) {
    const d = parseUtc(value);
    if (!d) return '—';
    const diff = (Date.now() - d.getTime()) / 1000;
    const rtf = new Intl.RelativeTimeFormat('es', { numeric: 'auto' });
    if (diff < 60) return 'hace un momento';
    if (diff < 3600) return rtf.format(-Math.round(diff / 60), 'minute');
    if (diff < 86400) return rtf.format(-Math.round(diff / 3600), 'hour');
    if (diff < 86400 * 30) return rtf.format(-Math.round(diff / 86400), 'day');
    return formatDate(value);
}

export function formatNumber(n) {
    return numberFmt.format(n ?? 0);
}

export function formatBytes(bytes) {
    if (!bytes && bytes !== 0) return '—';
    if (bytes < 1024) return `${bytes} B`;
    const units = ['KB', 'MB', 'GB'];
    let value = bytes / 1024;
    let i = 0;
    while (value >= 1024 && i < units.length - 1) {
        value /= 1024;
        i++;
    }
    return `${value.toFixed(value < 10 ? 1 : 0)} ${units[i]}`;
}

export function shortHash(hash, size = 10) {
    if (!hash) return '—';
    return hash.length > size * 2 ? `${hash.slice(0, size)}…${hash.slice(-6)}` : hash;
}

export function shortId(id) {
    return id ? String(id).split('-')[0].toUpperCase() : '—';
}

export function initials(name = '') {
    const parts = name.trim().split(/\s+/).filter(Boolean);
    if (!parts.length) return '?';
    return (parts[0][0] + (parts.length > 1 ? parts[parts.length - 1][0] : '')).toUpperCase();
}

/** Fecha local YYYY-MM-DD para inputs type="date". */
export function toDateInput(date) {
    const d = new Date(date);
    const pad = n => String(n).padStart(2, '0');
    return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`;
}

const GUID_RE = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

/**
 * El backend registra algunos autores como ID de usuario (GUID). Se muestra el
 * nombre si corresponde al usuario en sesión, o un identificador corto legible.
 */
export function formatActor(value, currentUser) {
    if (!value) return '—';
    if (!GUID_RE.test(value)) return value;
    if (currentUser?.id && value.toLowerCase() === currentUser.id.toLowerCase()) return currentUser.fullName || currentUser.username;
    return `Usuario ${shortId(value)}`;
}
