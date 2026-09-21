import { lazy, Suspense } from 'react';
import { Navigate, Route, Routes } from 'react-router-dom';
import AppLayout from '@/layouts/AppLayout';
import ProtectedRoute from '@/routes/ProtectedRoute';
import { ROLES } from '@/config/constants';
import LoginPage from '@/pages/LoginPage';
import { ForbiddenPage, NotFoundPage } from '@/pages/StatusPages';

// Carga diferida por vista: cada módulo se descarga solo al visitarlo.
const DashboardPage = lazy(() => import('@/pages/DashboardPage'));
const DocumentsPage = lazy(() => import('@/pages/DocumentsPage'));
const UploadDocumentPage = lazy(() => import('@/pages/UploadDocumentPage'));
const DocumentDetailPage = lazy(() => import('@/pages/DocumentDetailPage'));
const StudentsPage = lazy(() => import('@/pages/StudentsPage'));
const StudentDossierPage = lazy(() => import('@/pages/StudentDossierPage'));
const UsersPage = lazy(() => import('@/pages/UsersPage'));
const ProfilePage = lazy(() => import('@/pages/ProfilePage'));
const VerifyPage = lazy(() => import('@/pages/VerifyPage'));

function PageLoader() {
    return (
        <div className="center-screen" style={{ minHeight: '60vh' }} aria-busy="true">
            <span className="spinner" style={{ width: 28, height: 28, color: 'var(--primary-600)' }} />
        </div>
    );
}

/**
 * Mapa de rutas y permisos:
 *  /login, /verify/:hash       → Público
 *  /dashboard, /documents      → Todos los roles autenticados
 *  /documents/upload           → Admin, Registro
 *  /admin/users                → Admin
 */
export default function App() {
    return (
        <Suspense fallback={<PageLoader />}>
            <Routes>
                <Route path="/login" element={<LoginPage />} />
                <Route path="/verify" element={<VerifyPage />} />
                <Route path="/verify/:hash" element={<VerifyPage />} />

                <Route element={<ProtectedRoute><AppLayout /></ProtectedRoute>}>
                    <Route path="/dashboard" element={<DashboardPage />} />
                    <Route path="/documents" element={<DocumentsPage />} />
                    <Route
                        path="/documents/upload"
                        element={<ProtectedRoute roles={[ROLES.ADMIN, ROLES.REGISTRO]}><UploadDocumentPage /></ProtectedRoute>}
                    />
                    <Route path="/documents/:id" element={<DocumentDetailPage />} />
                    <Route path="/students" element={<StudentsPage />} />
                    <Route path="/students/:matricula" element={<StudentDossierPage />} />
                    <Route
                        path="/admin/users"
                        element={<ProtectedRoute roles={[ROLES.ADMIN]}><UsersPage /></ProtectedRoute>}
                    />
                    <Route path="/profile" element={<ProfilePage />} />
                    <Route path="/forbidden" element={<ForbiddenPage />} />
                </Route>

                <Route path="/" element={<Navigate to="/dashboard" replace />} />
                <Route path="*" element={<NotFoundPage />} />
            </Routes>
        </Suspense>
    );
}
