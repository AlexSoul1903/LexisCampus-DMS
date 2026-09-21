import { useCallback, useRef, useState } from 'react';
import { sha256File } from '@/utils/files';

/**
 * Gestiona el archivo seleccionado y calcula su hash SHA-256 en el cliente.
 * Si se selecciona otro archivo antes de terminar, el resultado anterior se descarta.
 */
export function useFileHash({ onHashed } = {}) {
    const [file, setFile] = useState(null);
    const [hash, setHash] = useState(null);
    const [hashing, setHashing] = useState(false);
    const [error, setError] = useState(null);
    const requestRef = useRef(0);

    const selectFile = useCallback(async next => {
        const request = ++requestRef.current;
        setFile(next);
        setHash(null);
        setError(null);
        if (!next) {
            setHashing(false);
            return;
        }
        setHashing(true);
        try {
            const digest = await sha256File(next);
            if (request !== requestRef.current) return;
            setHash(digest);
            onHashed?.(digest, next);
        } catch (err) {
            if (request === requestRef.current) setError(err.message);
        } finally {
            if (request === requestRef.current) setHashing(false);
        }
    }, [onHashed]);

    return { file, hash, hashing, error, setError, selectFile };
}
