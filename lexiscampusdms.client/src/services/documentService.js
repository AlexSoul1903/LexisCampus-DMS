import apiClient from './apiClient';

function toIsoStartOfDay(date) {
    return date ? new Date(`${date}T00:00:00`).toISOString() : undefined;
}

function toIsoEndOfDay(date) {
    return date ? new Date(`${date}T23:59:59.999`).toISOString() : undefined;
}

function clean(params) {
    return Object.fromEntries(Object.entries(params).filter(([, v]) => v !== undefined && v !== null && v !== ''));
}

export const documentService = {
    /**
     * GET /api/documents/search — búsqueda paginada.
     * @param {{matricula?:string, tipo?:number|string, fromDate?:string, toDate?:string, pageNumber?:number, pageSize?:number}} filters
     */
    async search({ matricula, tipo, fromDate, toDate, pageNumber = 1, pageSize = 10 } = {}) {
        const { data } = await apiClient.get('/documents/search', {
            params: clean({
                matricula: matricula?.trim(),
                tipo,
                fromDateUtc: toIsoStartOfDay(fromDate),
                toDateUtc: toIsoEndOfDay(toDate),
                pageNumber,
                pageSize
            })
        });
        return data;
    },

    /** GET /api/documents/{id} */
    async getById(id) {
        const { data } = await apiClient.get(`/documents/${id}`);
        return data.data;
    },

    /** GET /api/documents/{id}/versions */
    async getVersions(id) {
        const { data } = await apiClient.get(`/documents/${id}/versions`);
        return data.data ?? [];
    },

    /** GET /api/documents/student/{matricula} */
    async getByStudent(matricula) {
        const { data } = await apiClient.get(`/documents/student/${encodeURIComponent(matricula)}`);
        return data.data ?? [];
    },

    /**
     * POST /api/documents/upload (multipart/form-data)
     */
    async upload({ file, title, studentRegistration, documentType, initialComment }, onProgress) {
        const form = new FormData();
        form.append('File', file);
        form.append('Title', title.trim());
        form.append('StudentRegistration', studentRegistration.trim());
        form.append('DocumentType', String(documentType));
        if (initialComment?.trim()) form.append('InitialComment', initialComment.trim());

        const { data } = await apiClient.post('/documents/upload', form, {
            headers: { 'Content-Type': 'multipart/form-data' },
            onUploadProgress: e => onProgress?.(e.total ? Math.round((e.loaded / e.total) * 100) : 0)
        });
        return data.data;
    },

    /** POST /api/documents/{id}/rectify (multipart/form-data) */
    async rectify(id, { file, changeReason }, onProgress) {
        const form = new FormData();
        form.append('File', file);
        form.append('ChangeReason', changeReason.trim());

        const { data } = await apiClient.post(`/documents/${id}/rectify`, form, {
            headers: { 'Content-Type': 'multipart/form-data' },
            onUploadProgress: e => onProgress?.(e.total ? Math.round((e.loaded / e.total) * 100) : 0)
        });
        return data.data;
    },

    /** POST /api/documents/{id}/revoke */
    async revoke(id, { reason, resolutionNumber, observations }) {
        const { data } = await apiClient.post(`/documents/${id}/revoke`, {
            reason: reason.trim(),
            resolutionNumber: resolutionNumber.trim(),
            observations: observations?.trim() || null
        });
        return data.data;
    },

    /**
     * GET /api/documents/{id}/download — devuelve el archivo como Blob.
     * @param {{versionNumber?:number, inline?:boolean}} options
     */
    async download(id, { versionNumber, inline = false } = {}) {
        const response = await apiClient.get(`/documents/${id}/download`, {
            params: clean({ versionNumber, inline }),
            responseType: 'blob'
        });
        return {
            blob: response.data,
            fileName: parseFileName(response.headers['content-disposition'])
        };
    },

    /** GET /api/documents/{id}/verify — verificación interna por ID. */
    async verify(id) {
        const { data } = await apiClient.get(`/documents/${id}/verify`);
        return data.data;
    },

    /** GET /api/documents/student/{matricula}/dossier-zip */
    async downloadDossierZip(matricula) {
        const response = await apiClient.get(`/documents/student/${encodeURIComponent(matricula)}/dossier-zip`, {
            responseType: 'blob'
        });
        return {
            blob: response.data,
            fileName: parseFileName(response.headers['content-disposition']) ?? `Expediente_${matricula}.zip`
        };
    }
};

function parseFileName(disposition) {
    if (!disposition) return null;
    const utf8 = /filename\*=UTF-8''([^;]+)/i.exec(disposition);
    if (utf8) return decodeURIComponent(utf8[1]);
    const plain = /filename="?([^";]+)"?/i.exec(disposition);
    return plain ? plain[1] : null;
}
