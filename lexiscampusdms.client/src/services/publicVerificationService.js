import axios from 'axios';

/**
 * Consultas públicas (sin autenticación) para validar la autenticidad de un
 * documento por hash SHA-256 o token (ID del documento).
 */
const publicClient = axios.create({ baseURL: '/api/public', timeout: 30000 });

export const publicVerificationService = {
    /**
     * GET /api/public/verify/{hashOrToken}
     * @returns {Promise<{status:'valid'|'revoked'|'not-found', data?:object, message?:string}>}
     */
    async verify(hashOrToken) {
        try {
            const { data } = await publicClient.get(`/verify/${encodeURIComponent(hashOrToken.trim())}`);
            const result = data.data;
            return {
                status: result.valido ? 'valid' : 'revoked',
                data: result,
                message: data.message
            };
        } catch (error) {
            if (error.response?.status === 404) {
                return { status: 'not-found', message: error.response.data?.message };
            }
            throw error;
        }
    },

    /** URL de la imagen QR de verificación (PNG o SVG). */
    qrUrl(hashOrToken, format = 'svg') {
        return `/api/public/verify/${encodeURIComponent(hashOrToken)}/qr?format=${format}`;
    }
};
