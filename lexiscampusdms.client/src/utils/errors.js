/**
 * Extrae un mensaje legible de un error de axios. El backend responde con
 * Result { isSuccess, message, errorCode, errors[] } o con ProblemDetails
 * de ASP.NET (errors: { campo: [mensajes] }).
 */
export function getErrorMessage(error, fallback = 'Ocurrió un error inesperado.') {
    if (!error) return fallback;

    if (!error.response) {
        if (error.code === 'ECONNABORTED') return 'La solicitud tardó demasiado. Intente nuevamente.';
        if (error.message === 'Network Error' || error.code === 'ERR_NETWORK') {
            return 'No se pudo conectar con el servidor. Verifique su conexión o que la API esté en ejecución.';
        }
        return error.message || fallback;
    }

    const { status, data } = error.response;

    if (status === 429) return 'Demasiadas solicitudes. Espere un minuto e intente de nuevo.';
    if (status === 403) return 'No tiene permisos para realizar esta acción.';
    if (status >= 500 && !data?.message) return 'Error interno del servidor. Intente más tarde.';

    const details = getErrorList(error);
    if (data?.message) {
        return details.length && !details.includes(data.message) ? `${data.message} ${details[0]}` : data.message;
    }
    if (details.length) return details[0];
    if (data?.title) return data.title;
    return fallback;
}

export function getErrorList(error) {
    const data = error?.response?.data;
    if (!data?.errors) return [];
    if (Array.isArray(data.errors)) return data.errors;
    return Object.values(data.errors).flat();
}

export function getErrorCode(error) {
    return error?.response?.data?.errorCode ?? null;
}
