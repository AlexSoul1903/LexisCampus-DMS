import { Badge } from '@/components/ui';
import { getValidity } from '@/utils/documents';

export function ValidityBadge({ doc }) {
    const v = getValidity(doc);
    if (!v) return null;
    return <Badge tone={v.tone} icon={v.icon}>{v.label}</Badge>;
}

export function VersionTag({ version, rectified }) {
    return (
        <span className="tag-version">
            <span className="v">v{version}</span>
            {rectified && <span className="muted" style={{ fontFamily: 'var(--font-sans)' }}>(Rectificado)</span>}
        </span>
    );
}
