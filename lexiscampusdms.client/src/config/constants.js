/**
 * Catálogos alineados con los enums del backend (LexisCampusDMS.Core.Domain.Enums).
 * El backend serializa los enums como enteros.
 */

export const ROLES = Object.freeze({
    ADMIN: 'Admin',
    REGISTRO: 'Registro',
    AUDITOR: 'Auditor'
});

export const ROLE_OPTIONS = [
    { value: ROLES.ADMIN, label: 'Administrador', description: 'Control total del sistema, usuarios y roles.' },
    { value: ROLES.REGISTRO, label: 'Registro', description: 'Carga, rectificación y revocación de documentos.' },
    { value: ROLES.AUDITOR, label: 'Auditor', description: 'Consulta, búsqueda y verificación (solo lectura).' }
];

export const ROLE_LABELS = Object.fromEntries(ROLE_OPTIONS.map(r => [r.value, r.label]));

// DocumentType enum
export const DOCUMENT_TYPES = [
    { value: 1, key: 'Transcript', label: 'Récord de notas' },
    { value: 2, key: 'BirthCertificate', label: 'Acta de nacimiento' },
    { value: 3, key: 'Accreditation', label: 'Acreditación' },
    { value: 4, key: 'StudyCertificate', label: 'Certificado de estudios' },
    { value: 5, key: 'Degree', label: 'Título universitario' },
    { value: 99, key: 'Other', label: 'Otro documento' }
];

// DocumentStatus enum
export const DOCUMENT_STATUS = Object.freeze({
    DRAFT: 1,
    PENDING_REVIEW: 2,
    APPROVED: 3,
    REJECTED: 4,
    ARCHIVED: 5,
    REVOKED: 6
});

export const DOCUMENT_STATUS_LABELS = {
    1: 'Registrado',
    2: 'Pendiente de revisión',
    3: 'Aprobado',
    4: 'Rechazado',
    5: 'Archivado',
    6: 'Revocado'
};

const STATUS_KEYS = { DRAFT: 1, PENDINGREVIEW: 2, APPROVED: 3, REJECTED: 4, ARCHIVED: 5, REVOKED: 6, REVOCADO: 6 };

/** Acepta el valor numérico del enum o su nombre ("DRAFT", "Revoked"). */
export function documentStatusLabel(value) {
    if (value === null || value === undefined) return '—';
    const key = typeof value === 'number' ? value : STATUS_KEYS[String(value).replace(/[_\s]/g, '').toUpperCase()];
    return DOCUMENT_STATUS_LABELS[key] ?? String(value);
}

const TYPE_BY_VALUE = Object.fromEntries(DOCUMENT_TYPES.map(t => [t.value, t]));
const TYPE_BY_KEY = Object.fromEntries(DOCUMENT_TYPES.map(t => [t.key.toLowerCase(), t]));

/** Acepta el valor numérico del enum o su nombre ("Transcript"). */
export function documentTypeLabel(value) {
    if (value === null || value === undefined) return '—';
    const t = TYPE_BY_VALUE[value] ?? TYPE_BY_KEY[String(value).toLowerCase()];
    return t ? t.label : String(value);
}

export const UPLOAD_RULES = Object.freeze({
    maxBytes: 50 * 1024 * 1024,
    extensions: ['.pdf', '.png', '.jpg', '.jpeg'],
    accept: 'application/pdf,image/png,image/jpeg,.pdf,.png,.jpg,.jpeg'
});

export const MAX_LOGIN_ATTEMPTS = 5;
