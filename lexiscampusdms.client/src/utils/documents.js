import { Ban, BadgeCheck, FilePen } from 'lucide-react';
import { DOCUMENT_STATUS } from '@/config/constants';

/**
 * Validez del documento con color semántico:
 * - Revocado → carmesí
 * - Rectificado (versión > 1) → ámbar
 * - Vigente → esmeralda
 */
export function getValidity(doc) {
    if (!doc) return null;
    if (doc.status === DOCUMENT_STATUS.REVOKED) return { key: 'revoked', label: 'Revocado', tone: 'danger', icon: Ban };
    if (doc.currentVersion > 1) return { key: 'rectified', label: 'Rectificado', tone: 'warning', icon: FilePen };
    return { key: 'valid', label: 'Vigente', tone: 'success', icon: BadgeCheck };
}

/** Extrae el hash/token de un QR: admite URL (…/verify/{hash}) o el valor directo. */
export function extractVerificationToken(text) {
    const value = text.trim();
    try {
        const url = new URL(value);
        const parts = url.pathname.split('/').filter(Boolean);
        return decodeURIComponent(parts[parts.length - 1] ?? '');
    } catch {
        return value;
    }
}
