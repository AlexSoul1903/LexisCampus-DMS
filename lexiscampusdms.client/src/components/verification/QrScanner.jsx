import { useEffect, useRef, useState } from 'react';
import jsQR from 'jsqr';
import { Camera, CameraOff, ImageUp } from 'lucide-react';
import { Alert, Button } from '@/components/ui';
import { extractVerificationToken as extractToken } from '@/utils/documents';

function decodeImageData(imageData) {
    const code = jsQR(imageData.data, imageData.width, imageData.height, { inversionAttempts: 'attemptBoth' });
    return code?.data ?? null;
}

export default function QrScanner({ onDetected }) {
    const videoRef = useRef(null);
    const canvasRef = useRef(null);
    const fileRef = useRef(null);
    const [active, setActive] = useState(false);
    const [error, setError] = useState(null);

    useEffect(() => {
        if (!active) return undefined;
        let stream;
        let raf;
        let stopped = false;

        const tick = () => {
            if (stopped) return;
            const video = videoRef.current;
            const canvas = canvasRef.current;
            if (video && canvas && video.readyState === video.HAVE_ENOUGH_DATA) {
                canvas.width = video.videoWidth;
                canvas.height = video.videoHeight;
                const ctx = canvas.getContext('2d', { willReadFrequently: true });
                ctx.drawImage(video, 0, 0, canvas.width, canvas.height);
                const text = decodeImageData(ctx.getImageData(0, 0, canvas.width, canvas.height));
                if (text) {
                    setActive(false);
                    onDetected(extractToken(text));
                    return;
                }
            }
            raf = requestAnimationFrame(tick);
        };

        navigator.mediaDevices
            ?.getUserMedia({ video: { facingMode: 'environment' } })
            .then(s => {
                if (stopped) { s.getTracks().forEach(t => t.stop()); return; }
                stream = s;
                videoRef.current.srcObject = s;
                videoRef.current.play();
                raf = requestAnimationFrame(tick);
            })
            .catch(() => {
                setError('No se pudo acceder a la cámara. Conceda el permiso o cargue una imagen del código QR.');
                setActive(false);
            });

        if (!navigator.mediaDevices) {
            setError('Este navegador no permite el acceso a la cámara. Cargue una imagen del código QR.');
            setActive(false);
        }

        return () => {
            stopped = true;
            cancelAnimationFrame(raf);
            stream?.getTracks().forEach(t => t.stop());
        };
    }, [active, onDetected]);

    const onImage = e => {
        const file = e.target.files?.[0];
        e.target.value = '';
        if (!file) return;
        const img = new Image();
        img.onload = () => {
            const canvas = canvasRef.current;
            canvas.width = img.naturalWidth;
            canvas.height = img.naturalHeight;
            const ctx = canvas.getContext('2d', { willReadFrequently: true });
            ctx.drawImage(img, 0, 0);
            const text = decodeImageData(ctx.getImageData(0, 0, canvas.width, canvas.height));
            URL.revokeObjectURL(img.src);
            if (text) {
                setError(null);
                onDetected(extractToken(text));
            } else {
                setError('No se detectó un código QR legible en la imagen.');
            }
        };
        img.src = URL.createObjectURL(file);
    };

    return (
        <div className="stack">
            {active ? (
                <div className="qr-frame">
                    <video ref={videoRef} className="qr-video" muted playsInline />
                </div>
            ) : (
                <div className="dropzone" style={{ cursor: 'default' }}>
                    <div className="dropzone-icon"><Camera /></div>
                    <strong>Escanee el código QR impreso en el documento</strong>
                    <span className="muted text-sm">Use la cámara de su dispositivo o cargue una fotografía del código.</span>
                </div>
            )}
            <canvas ref={canvasRef} hidden />
            {error && <Alert tone="warning">{error}</Alert>}
            <div className="row-wrap" style={{ justifyContent: 'center' }}>
                {active ? (
                    <Button icon={CameraOff} onClick={() => setActive(false)}>Detener cámara</Button>
                ) : (
                    <Button variant="primary" icon={Camera} onClick={() => { setError(null); setActive(true); }}>Activar cámara</Button>
                )}
                <Button icon={ImageUp} onClick={() => fileRef.current?.click()}>Cargar imagen del QR</Button>
                <input ref={fileRef} type="file" accept="image/*" hidden onChange={onImage} />
            </div>
        </div>
    );
}
