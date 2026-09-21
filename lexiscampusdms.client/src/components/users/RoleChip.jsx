import { ShieldCheck } from 'lucide-react';
import { ROLE_LABELS } from '@/config/constants';

export default function RoleChip({ role }) {
    return <span className={`role-chip role-${role}`}><ShieldCheck aria-hidden />{ROLE_LABELS[role] ?? role}</span>;
}
