import { useEffect, useId, useRef } from 'react';
import { createPortal } from 'react-dom';
import { AlertCircle, ChevronLeft, ChevronRight, Inbox, X } from 'lucide-react';
import { formatNumber } from '@/utils/format';

/* ------------------------------------------------------------------ Button */
export function Button({ variant = 'default', size, block, loading, icon: Icon, children, className = '', ...props }) {
    const cls = [
        'btn',
        variant !== 'default' && `btn-${variant}`,
        size && `btn-${size}`,
        block && 'btn-block',
        className
    ].filter(Boolean).join(' ');

    return (
        <button type="button" className={cls} disabled={loading || props.disabled} {...props}>
            {loading ? <span className="spinner" aria-hidden /> : Icon && <Icon aria-hidden />}
            {children}
        </button>
    );
}

export function IconButton({ icon: Icon, label, variant = 'ghost', size = 'sm', className = '', ...props }) {
    return (
        <button
            type="button"
            className={`btn btn-icon btn-${variant} ${size ? `btn-${size}` : ''} ${className}`}
            aria-label={label}
            title={label}
            {...props}
        >
            <Icon aria-hidden />
        </button>
    );
}

/* ------------------------------------------------------------------- Field */
export function Field({ label, required, hint, error, children, className = '', htmlFor }) {
    return (
        <div className={`field ${className}`}>
            {label && (
                <label className="field-label" htmlFor={htmlFor}>
                    {label}
                    {required && <span className="req" aria-hidden>*</span>}
                </label>
            )}
            {children}
            {error ? (
                <span className="field-error" role="alert"><AlertCircle size={13} aria-hidden />{error}</span>
            ) : hint ? (
                <span className="field-hint">{hint}</span>
            ) : null}
        </div>
    );
}

export function Input({ label, required, hint, error, icon: Icon, suffix, className = '', id, ...props }) {
    const autoId = useId();
    const inputId = id ?? autoId;
    const input = (
        <input
            id={inputId}
            className="input"
            aria-invalid={error ? 'true' : undefined}
            required={required}
            {...props}
        />
    );
    return (
        <Field label={label} required={required} hint={hint} error={error} className={className} htmlFor={inputId}>
            {Icon || suffix ? (
                <div className="input-group">
                    {Icon && <Icon aria-hidden />}
                    {input}
                    {suffix && <div className="input-suffix">{suffix}</div>}
                </div>
            ) : input}
        </Field>
    );
}

export function Select({ label, required, hint, error, options = [], placeholder, className = '', id, ...props }) {
    const autoId = useId();
    const selectId = id ?? autoId;
    return (
        <Field label={label} required={required} hint={hint} error={error} className={className} htmlFor={selectId}>
            <select id={selectId} className="select" aria-invalid={error ? 'true' : undefined} {...props}>
                {placeholder !== undefined && <option value="">{placeholder}</option>}
                {options.map(o => <option key={o.value} value={o.value}>{o.label}</option>)}
            </select>
        </Field>
    );
}

export function Textarea({ label, required, hint, error, className = '', id, ...props }) {
    const autoId = useId();
    const areaId = id ?? autoId;
    return (
        <Field label={label} required={required} hint={hint} error={error} className={className} htmlFor={areaId}>
            <textarea id={areaId} className="textarea" aria-invalid={error ? 'true' : undefined} {...props} />
        </Field>
    );
}

/* ------------------------------------------------------------------- Card */
export function Card({ title, subtitle, actions, footer, flush, children, className = '', ...props }) {
    return (
        <section className={`card ${flush ? 'card-flush' : ''} ${className}`} {...props}>
            {(title || actions) && (
                <header className="card-header">
                    <div style={{ minWidth: 0 }}>
                        {title && <h3>{title}</h3>}
                        {subtitle && <div className="card-subtitle">{subtitle}</div>}
                    </div>
                    {actions && <div className="row" style={{ marginLeft: 'auto' }}>{actions}</div>}
                </header>
            )}
            <div className="card-body">{children}</div>
            {footer && <footer className="card-footer">{footer}</footer>}
        </section>
    );
}

/* ------------------------------------------------------------------- Badge */
export function Badge({ tone = 'neutral', icon: Icon, dot, children, className = '' }) {
    return (
        <span className={`badge badge-${tone} ${dot ? 'badge-dot' : ''} ${className}`}>
            {Icon && <Icon aria-hidden />}
            {children}
        </span>
    );
}

/* ------------------------------------------------------------------ Alert */
export function Alert({ tone = 'info', icon: Icon = AlertCircle, title, children, className = '' }) {
    return (
        <div className={`alert alert-${tone} ${className}`} role={tone === 'danger' ? 'alert' : 'status'}>
            <Icon aria-hidden />
            <div>
                {title && <strong>{title}</strong>}
                {children}
            </div>
        </div>
    );
}

/* --------------------------------------------------------------- Skeleton */
export function Skeleton({ width = '100%', height = 14, radius, style }) {
    return <span className="skeleton" style={{ width, height, borderRadius: radius, ...style }} aria-hidden />;
}

export function TableSkeleton({ rows = 6, cols = 5 }) {
    return (
        <tbody aria-busy="true">
            {Array.from({ length: rows }).map((_, r) => (
                <tr key={r}>
                    {Array.from({ length: cols }).map((__, c) => (
                        <td key={c}>
                            <Skeleton width={c === 0 ? '70%' : `${40 + ((r + c) % 3) * 15}%`} />
                            {c === 0 && <Skeleton width="40%" height={10} style={{ marginTop: 8 }} />}
                        </td>
                    ))}
                </tr>
            ))}
        </tbody>
    );
}

/* ------------------------------------------------------------ Empty state */
export function EmptyState({ icon: Icon = Inbox, title, description, action }) {
    return (
        <div className="empty">
            <div className="empty-icon"><Icon aria-hidden /></div>
            <h3>{title}</h3>
            {description && <p>{description}</p>}
            {action && <div style={{ marginTop: 10 }}>{action}</div>}
        </div>
    );
}

/* ----------------------------------------------------------------- Modal */
export function Modal({ open, onClose, title, description, icon: Icon, tone, size, footer, children, closeOnBackdrop = true }) {
    const dialogRef = useRef(null);
    const titleId = useId();
    // Referencia estable: el efecto de foco/teclado solo debe ejecutarse al abrir o cerrar.
    const onCloseRef = useRef(onClose);
    useEffect(() => { onCloseRef.current = onClose; }, [onClose]);

    useEffect(() => {
        if (!open) return undefined;
        const previouslyFocused = document.activeElement;
        const onKey = e => {
            if (e.key === 'Escape') onCloseRef.current?.();
            if (e.key === 'Tab' && dialogRef.current) {
                const focusables = dialogRef.current.querySelectorAll(
                    'button:not([disabled]), [href], input:not([disabled]), select:not([disabled]), textarea:not([disabled]), [tabindex]:not([tabindex="-1"])'
                );
                if (!focusables.length) return;
                const first = focusables[0];
                const last = focusables[focusables.length - 1];
                if (e.shiftKey && document.activeElement === first) { e.preventDefault(); last.focus(); }
                else if (!e.shiftKey && document.activeElement === last) { e.preventDefault(); first.focus(); }
            }
        };
        document.addEventListener('keydown', onKey);
        document.body.style.overflow = 'hidden';
        const t = setTimeout(() => {
            const target = dialogRef.current?.querySelector('input, select, textarea, button.btn-primary, button.btn-danger');
            target?.focus();
        }, 30);
        return () => {
            clearTimeout(t);
            document.removeEventListener('keydown', onKey);
            document.body.style.overflow = '';
            previouslyFocused?.focus?.();
        };
    }, [open]);

    if (!open) return null;

    return createPortal(
        <div className="modal-backdrop" onMouseDown={e => closeOnBackdrop && e.target === e.currentTarget && onClose?.()}>
            <div
                ref={dialogRef}
                className={`modal ${size === 'lg' ? 'modal-lg' : ''}`}
                role="dialog"
                aria-modal="true"
                aria-labelledby={titleId}
            >
                <div className="modal-header">
                    {Icon && <div className={`modal-icon ${tone ?? ''}`}><Icon aria-hidden /></div>}
                    <div style={{ flex: 1, minWidth: 0 }}>
                        <h2 id={titleId}>{title}</h2>
                        {description && <p>{description}</p>}
                    </div>
                    <IconButton icon={X} label="Cerrar" onClick={onClose} />
                </div>
                <div className="modal-body">{children}</div>
                {footer && <div className="modal-footer">{footer}</div>}
            </div>
        </div>,
        document.body
    );
}

/* ------------------------------------------------------------ Pagination */
export function Pagination({ pageNumber, pageSize, totalCount, onPageChange, onPageSizeChange }) {
    const totalPages = Math.max(1, Math.ceil((totalCount || 0) / pageSize));
    const from = totalCount ? (pageNumber - 1) * pageSize + 1 : 0;
    const to = Math.min(pageNumber * pageSize, totalCount || 0);

    const pages = [];
    const start = Math.max(1, Math.min(pageNumber - 2, totalPages - 4));
    for (let p = start; p <= Math.min(totalPages, start + 4); p++) pages.push(p);

    return (
        <div className="pagination">
            <span>
                Mostrando <strong>{formatNumber(from)}–{formatNumber(to)}</strong> de <strong>{formatNumber(totalCount)}</strong>
            </span>
            {onPageSizeChange && (
                <select
                    className="select"
                    style={{ width: 'auto', height: 32, fontSize: 13 }}
                    value={pageSize}
                    onChange={e => onPageSizeChange(Number(e.target.value))}
                    aria-label="Registros por página"
                >
                    {[10, 20, 50, 100].map(s => <option key={s} value={s}>{s} por página</option>)}
                </select>
            )}
            <div className="pages">
                <button className="page-btn" onClick={() => onPageChange(pageNumber - 1)} disabled={pageNumber <= 1} aria-label="Página anterior">
                    <ChevronLeft />
                </button>
                {pages.map(p => (
                    <button
                        key={p}
                        className={`page-btn ${p === pageNumber ? 'active' : ''}`}
                        onClick={() => onPageChange(p)}
                        aria-current={p === pageNumber ? 'page' : undefined}
                    >
                        {p}
                    </button>
                ))}
                <button className="page-btn" onClick={() => onPageChange(pageNumber + 1)} disabled={pageNumber >= totalPages} aria-label="Página siguiente">
                    <ChevronRight />
                </button>
            </div>
        </div>
    );
}

/* ---------------------------------------------------------- Page header */
export function PageHeader({ eyebrow, eyebrowIcon: EyebrowIcon, title, description, actions, breadcrumbs }) {
    return (
        <div>
            {breadcrumbs}
            <div className="page-header">
                <div style={{ minWidth: 0 }}>
                    {eyebrow && <div className="eyebrow">{EyebrowIcon && <EyebrowIcon aria-hidden />}{eyebrow}</div>}
                    <h1>{title}</h1>
                    {description && <p>{description}</p>}
                </div>
                {actions && <div className="page-actions">{actions}</div>}
            </div>
        </div>
    );
}
