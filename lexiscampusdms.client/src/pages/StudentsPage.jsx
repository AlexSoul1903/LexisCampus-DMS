import { useEffect, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { ArrowRight, FolderSearch, GraduationCap, Search } from 'lucide-react';
import { Button, Card, Input, PageHeader, Skeleton } from '@/components/ui';
import { documentService } from '@/services/documentService';
import { formatRelative } from '@/utils/format';

/**
 * Buscador de expedientes. Muestra además los estudiantes con actividad
 * documental reciente (derivado de los últimos documentos registrados).
 */
export default function StudentsPage() {
    const navigate = useNavigate();
    const [matricula, setMatricula] = useState('');
    const [error, setError] = useState(null);
    const [recent, setRecent] = useState(null);

    useEffect(() => {
        documentService.search({ pageSize: 100 })
            .then(res => {
                const map = new Map();
                (res.data ?? []).forEach(d => {
                    const cur = map.get(d.studentRegistration) ?? { matricula: d.studentRegistration, count: 0, last: d.createdAtUtc };
                    cur.count += 1;
                    if (d.createdAtUtc > cur.last) cur.last = d.createdAtUtc;
                    map.set(d.studentRegistration, cur);
                });
                setRecent([...map.values()].slice(0, 12));
            })
            .catch(() => setRecent([]));
    }, []);

    const submit = e => {
        e.preventDefault();
        const m = matricula.trim();
        if (!m) { setError('Ingrese una matrícula.'); return; }
        navigate(`/students/${encodeURIComponent(m)}`);
    };

    return (
        <div className="page">
            <PageHeader
                eyebrow="Hub de expedientes"
                eyebrowIcon={FolderSearch}
                title="Expedientes estudiantiles"
                description="Acceda a la historia documental consolidada de cada alumno y exporte su expediente certificado."
            />

            <Card>
                <form onSubmit={submit} className="row" style={{ alignItems: 'flex-end', gap: 12, flexWrap: 'wrap' }}>
                    <Input
                        className="spacer"
                        label="Matrícula del estudiante"
                        icon={Search}
                        placeholder="Ej. 2021-0456"
                        value={matricula}
                        onChange={e => { setMatricula(e.target.value); setError(null); }}
                        error={error}
                        autoFocus
                        maxLength={50}
                    />
                    <Button type="submit" variant="primary" icon={FolderSearch}>Abrir expediente</Button>
                </form>
            </Card>

            <h3 style={{ margin: '28px 0 12px' }}>Actividad reciente</h3>
            <div className="grid-4">
                {recent === null && Array.from({ length: 4 }).map((_, i) => (
                    <div key={i} className="card card-body stack-sm"><Skeleton width="60%" /><Skeleton width="40%" height={10} /></div>
                ))}
                {recent?.map(s => (
                    <Link key={s.matricula} to={`/students/${encodeURIComponent(s.matricula)}`} className="card card-body" style={{ color: 'inherit', textDecoration: 'none' }}>
                        <div className="row" style={{ gap: 12 }}>
                            <span className="stat-icon primary"><GraduationCap /></span>
                            <div style={{ flex: 1, minWidth: 0 }}>
                                <div className="mono truncate" style={{ fontWeight: 600, fontSize: 14 }}>{s.matricula}</div>
                                <div className="text-xs muted">{s.count} documento{s.count === 1 ? '' : 's'} · {formatRelative(s.last)}</div>
                            </div>
                            <ArrowRight size={16} className="muted" />
                        </div>
                    </Link>
                ))}
            </div>
            {recent?.length === 0 && <p className="muted text-sm">No hay actividad reciente registrada.</p>}
        </div>
    );
}
