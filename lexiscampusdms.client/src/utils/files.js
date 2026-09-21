import { UPLOAD_RULES } from '@/config/constants';
import { formatBytes } from './format';

/** Calcula el hash SHA-256 (hex en minúsculas) de un archivo usando Web Crypto. */
export async function sha256File(file) {
    if (!window.crypto?.subtle) {
        throw new Error('El navegador no soporta Web Crypto (requiere HTTPS o localhost).');
    }
    const buffer = await file.arrayBuffer();
    const digest = await window.crypto.subtle.digest('SHA-256', buffer);
    return Array.from(new Uint8Array(digest))
        .map(b => b.toString(16).padStart(2, '0'))
        .join('');
}

/** Valida extensión y tamaño según las reglas del backend. Devuelve mensaje de error o null. */
export function validateUploadFile(file) {
    if (!file) return 'Seleccione un archivo.';
    const name = file.name.toLowerCase();
    const ext = name.slice(name.lastIndexOf('.'));
    if (!UPLOAD_RULES.extensions.includes(ext)) {
        return `Formato no permitido. Solo se aceptan ${UPLOAD_RULES.extensions.join(', ')}.`;
    }
    if (file.size === 0) return 'El archivo está vacío.';
    if (file.size > UPLOAD_RULES.maxBytes) {
        return `El archivo pesa ${formatBytes(file.size)} y excede el máximo permitido de 50 MB.`;
    }
    return null;
}

export function isImageFile(nameOrType = '') {
    return /image\/|\.(png|jpe?g)$/i.test(nameOrType);
}

/** Dispara la descarga de un Blob en el navegador. */
export function saveBlob(blob, fileName) {
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = fileName || 'documento';
    document.body.appendChild(a);
    a.click();
    a.remove();
    setTimeout(() => URL.revokeObjectURL(url), 1000);
}

export async function copyToClipboard(text) {
    try {
        await navigator.clipboard.writeText(text);
        return true;
    } catch {
        return false;
    }
}
