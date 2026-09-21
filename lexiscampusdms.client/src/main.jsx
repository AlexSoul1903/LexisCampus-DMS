import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { BrowserRouter } from 'react-router-dom';
import { Toaster } from 'sonner';
import './index.css';
import App from './App.jsx';
import { AuthProvider } from '@/context/AuthContext';

createRoot(document.getElementById('root')).render(
    <StrictMode>
        <BrowserRouter>
            <AuthProvider>
                <App />
                <Toaster
                    position="top-right"
                    richColors
                    closeButton
                    toastOptions={{ style: { fontFamily: 'var(--font-sans)' } }}
                />
            </AuthProvider>
        </BrowserRouter>
    </StrictMode>
);
