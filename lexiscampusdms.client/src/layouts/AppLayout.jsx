import { useEffect, useRef, useState } from 'react';
import { Link, NavLink, Outlet, useLocation, useNavigate } from 'react-router-dom';
import {
    ChevronDown, FileStack, FolderSearch, LayoutDashboard, LogOut, Menu, Search,
    ShieldCheck, ShieldHalf, UploadCloud, UserRound, Users
} from 'lucide-react';
import { useAuth } from '@/context/AuthContext';
import { ROLES, ROLE_LABELS } from '@/config/constants';
import { initials } from '@/utils/format';
import BrandMark from '@/components/BrandMark';

const NAV = [
    {
        title: 'Gestión documental',
        items: [
            { to: '/dashboard', label: 'Panel principal', icon: LayoutDashboard },
            { to: '/documents', label: 'Documentos', icon: FileStack, end: true },
            { to: '/documents/upload', label: 'Cargar documento', icon: UploadCloud, roles: [ROLES.ADMIN, ROLES.REGISTRO] },
            { to: '/students', label: 'Expedientes', icon: FolderSearch }
        ]
    },
    {
        title: 'Seguridad y cumplimiento',
        items: [
            { to: '/verify', label: 'Verificación pública', icon: ShieldCheck, external: true },
            { to: '/admin/users', label: 'Usuarios y roles', icon: Users, roles: [ROLES.ADMIN] }
        ]
    }
];

export default function AppLayout() {
    const { user, role, hasRole, logout } = useAuth();
    const [sidebarOpen, setSidebarOpen] = useState(false);
    const [menuOpen, setMenuOpen] = useState(false);
    const [quickSearch, setQuickSearch] = useState('');
    const menuRef = useRef(null);
    const location = useLocation();
    const navigate = useNavigate();

    // Cierra el menú lateral y el desplegable al navegar.
    const [lastPath, setLastPath] = useState(location.pathname);
    if (lastPath !== location.pathname) {
        setLastPath(location.pathname);
        setSidebarOpen(false);
        setMenuOpen(false);
    }

    useEffect(() => {
        const onClick = e => {
            if (menuRef.current && !menuRef.current.contains(e.target)) setMenuOpen(false);
        };
        document.addEventListener('mousedown', onClick);
        return () => document.removeEventListener('mousedown', onClick);
    }, []);

    const onQuickSearch = e => {
        e.preventDefault();
        const q = quickSearch.trim();
        if (!q) return;
        navigate(`/students/${encodeURIComponent(q)}`);
        setQuickSearch('');
    };

    return (
        <div className="shell">
            <aside className={`sidebar ${sidebarOpen ? 'open' : ''}`} aria-label="Navegación principal">
                <Link to="/dashboard" className="sidebar-brand">
                    <div className="brand-mark"><BrandMark /></div>
                    <div>
                        <div className="brand-name">LexisCampus</div>
                        <div className="brand-sub">Gestión Documental</div>
                    </div>
                </Link>

                <nav className="sidebar-nav">
                    {NAV.map(section => {
                        const items = section.items.filter(i => !i.roles || hasRole(i.roles));
                        if (!items.length) return null;
                        return (
                            <div key={section.title} className="nav-section">
                                <div className="nav-section-title">{section.title}</div>
                                {items.map(item =>
                                    item.external ? (
                                        <a key={item.to} href={item.to} target="_blank" rel="noreferrer noopener" className="nav-link">
                                            <item.icon aria-hidden />
                                            {item.label}
                                        </a>
                                    ) : (
                                        <NavLink
                                            key={item.to}
                                            to={item.to}
                                            end={item.end}
                                            className={({ isActive }) => `nav-link ${isActive ? 'active' : ''}`}
                                        >
                                            <item.icon aria-hidden />
                                            {item.label}
                                        </NavLink>
                                    )
                                )}
                            </div>
                        );
                    })}
                </nav>

                <div className="sidebar-footer">
                    <ShieldHalf aria-hidden />
                    Conexión cifrada · Sesión {ROLE_LABELS[role] ?? role}
                </div>
            </aside>
            <div
                className={`sidebar-overlay ${sidebarOpen ? 'open' : ''}`}
                onClick={() => setSidebarOpen(false)}
                aria-hidden
            />

            <div className="main">
                <header className="topbar">
                    <button className="btn btn-ghost btn-icon menu-toggle" onClick={() => setSidebarOpen(true)} aria-label="Abrir menú">
                        <Menu />
                    </button>

                    <form className="topbar-search" onSubmit={onQuickSearch} role="search">
                        <div className="input-group">
                            <Search aria-hidden />
                            <input
                                className="input"
                                placeholder="Buscar expediente por matrícula…"
                                value={quickSearch}
                                onChange={e => setQuickSearch(e.target.value)}
                                aria-label="Buscar expediente por matrícula"
                            />
                        </div>
                    </form>

                    <div className="user-menu" ref={menuRef}>
                        <button className="user-trigger" onClick={() => setMenuOpen(o => !o)} aria-expanded={menuOpen} aria-haspopup="menu">
                            <span className="avatar">{initials(user?.fullName || user?.username)}</span>
                            <span className="who">
                                <strong>{user?.fullName || user?.username}</strong>
                                <span>{ROLE_LABELS[role] ?? role}{user?.department ? ` · ${user.department}` : ''}</span>
                            </span>
                            <ChevronDown aria-hidden />
                        </button>

                        {menuOpen && (
                            <div className="dropdown" role="menu">
                                <div className="dropdown-header">
                                    <strong>{user?.fullName}</strong>
                                    <span>{user?.email}</span>
                                </div>
                                <Link to="/profile" className="dropdown-item" role="menuitem">
                                    <UserRound aria-hidden /> Mi perfil
                                </Link>
                                <a href="/verify" target="_blank" rel="noopener" className="dropdown-item" role="menuitem">
                                    <ShieldCheck aria-hidden /> Portal de verificación
                                </a>
                                <hr className="divider" />
                                <button className="dropdown-item danger" role="menuitem" onClick={() => logout()}>
                                    <LogOut aria-hidden /> Cerrar sesión
                                </button>
                            </div>
                        )}
                    </div>
                </header>

                <main id="main-content">
                    <Outlet />
                </main>
            </div>
        </div>
    );
}
