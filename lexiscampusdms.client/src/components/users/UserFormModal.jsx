import { useMemo, useState } from 'react';
import { Check, Circle, Eye, EyeOff, UserPlus, UserRoundPen } from 'lucide-react';
import { toast } from 'sonner';
import { Button, Input, Modal, Select } from '@/components/ui';
import { ROLE_OPTIONS } from '@/config/constants';
import { userService } from '@/services/userService';
import { getErrorList, getErrorMessage } from '@/utils/errors';

const PASSWORD_RULES = [
    { id: 'len', label: 'Mínimo 8 caracteres', test: p => p.length >= 8 && p.length <= 128 },
    { id: 'upper', label: 'Una letra mayúscula', test: p => /[A-Z]/.test(p) },
    { id: 'lower', label: 'Una letra minúscula', test: p => /[a-z]/.test(p) },
    { id: 'digit', label: 'Un número', test: p => /[0-9]/.test(p) },
    { id: 'special', label: 'Un carácter especial', test: p => /[\W_]/.test(p) }
];

const STRENGTH_COLORS = ['var(--danger-600)', 'var(--danger-600)', 'var(--warning-600)', 'var(--warning-600)', 'var(--success-600)', 'var(--success-600)'];

const EMPTY = { username: '', email: '', fullName: '', password: '', role: '', department: '', studentRegistration: '' };

/**
 * Formulario de creación (mode="create") o edición (mode="edit") de usuarios.
 * La edición solo permite los campos que admite PUT /api/users/{id}.
 */
export default function UserFormModal({ open, mode = 'create', user, onClose, onSaved }) {
    const isEdit = mode === 'edit';
    // El padre monta el modal con una `key` por usuario, así el estado se inicializa desde las props.
    const [form, setForm] = useState(() => (isEdit && user
        ? { ...EMPTY, ...user, department: user.department ?? '', studentRegistration: user.studentRegistration ?? '' }
        : EMPTY));
    const [errors, setErrors] = useState({});
    const [showPassword, setShowPassword] = useState(false);
    const [saving, setSaving] = useState(false);

    const set = key => e => {
        setForm(f => ({ ...f, [key]: e.target.value }));
        if (errors[key]) setErrors(er => ({ ...er, [key]: null }));
    };

    const passed = useMemo(() => PASSWORD_RULES.filter(r => r.test(form.password)).length, [form.password]);

    const validate = () => {
        const e = {};
        if (!isEdit) {
            if (!form.username.trim()) e.username = 'El usuario es obligatorio.';
            else if (!/^[a-zA-Z0-9._-]{3,50}$/.test(form.username.trim())) e.username = '3 a 50 caracteres: letras, números, punto, guion o guion bajo.';
            if (!form.email.trim()) e.email = 'El correo es obligatorio.';
            else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(form.email.trim())) e.email = 'Formato de correo inválido.';
            if (passed < PASSWORD_RULES.length) e.password = 'La contraseña no cumple la política de complejidad.';
            if (!form.role) e.role = 'Seleccione un rol.';
        }
        if (form.fullName.trim().length < 2) e.fullName = 'El nombre completo es obligatorio.';
        else if (form.fullName.length > 150) e.fullName = 'Máximo 150 caracteres.';
        if (form.department.length > 100) e.department = 'Máximo 100 caracteres.';
        setErrors(e);
        return !Object.keys(e).length;
    };

    const submit = async e => {
        e.preventDefault();
        if (!validate()) return;
        setSaving(true);
        try {
            const saved = isEdit
                ? await userService.update(user.id, form)
                : await userService.create({
                    username: form.username.trim(),
                    email: form.email.trim(),
                    password: form.password,
                    fullName: form.fullName.trim(),
                    role: form.role,
                    department: form.department.trim() || null,
                    studentRegistration: form.studentRegistration.trim() || null
                });
            toast.success(isEdit ? 'Usuario actualizado' : 'Usuario creado', { description: saved?.fullName });
            onSaved?.(saved);
            onClose();
        } catch (err) {
            const list = getErrorList(err);
            toast.error(isEdit ? 'No se pudo actualizar' : 'No se pudo crear el usuario', {
                description: list.length > 1 ? list.join(' ') : getErrorMessage(err)
            });
        } finally {
            setSaving(false);
        }
    };

    return (
        <Modal
            open={open}
            onClose={() => !saving && onClose()}
            size="lg"
            icon={isEdit ? UserRoundPen : UserPlus}
            title={isEdit ? 'Editar usuario' : 'Nuevo usuario institucional'}
            description={isEdit ? `@${user?.username} · ${user?.email}` : 'Las credenciales se aplican de inmediato. Comparta la contraseña por un canal seguro.'}
            footer={
                <>
                    <Button onClick={onClose} disabled={saving}>Cancelar</Button>
                    <Button type="submit" form="user-form" variant="primary" loading={saving} icon={isEdit ? Check : UserPlus}>
                        {isEdit ? 'Guardar cambios' : 'Crear usuario'}
                    </Button>
                </>
            }
        >
            <form id="user-form" onSubmit={submit} noValidate className="grid-2">
                <Input className="span-2" label="Nombre completo" required value={form.fullName} onChange={set('fullName')} error={errors.fullName} maxLength={150} placeholder="Ej. María Fernanda Rosario" />
                {!isEdit && (
                    <>
                        <Input label="Usuario" required value={form.username} onChange={set('username')} error={errors.username} maxLength={50} placeholder="mrosario" autoComplete="off" />
                        <Input label="Correo institucional" type="email" required value={form.email} onChange={set('email')} error={errors.email} maxLength={150} placeholder="mrosario@lexiscampus.edu" autoComplete="off" />
                    </>
                )}
                <Input label="Departamento" value={form.department} onChange={set('department')} error={errors.department} maxLength={100} placeholder="Ej. Registro Académico" />
                <Input label="Matrícula asociada" value={form.studentRegistration} onChange={set('studentRegistration')} maxLength={50} placeholder="Opcional" />
                {!isEdit && (
                    <>
                        <Select
                            className="span-2"
                            label="Rol"
                            required
                            placeholder="Seleccione un rol"
                            options={ROLE_OPTIONS.map(r => ({ value: r.value, label: `${r.label} — ${r.description}` }))}
                            value={form.role}
                            onChange={set('role')}
                            error={errors.role}
                        />
                        <div className="span-2">
                            <Input
                                label="Contraseña temporal"
                                required
                                type={showPassword ? 'text' : 'password'}
                                value={form.password}
                                onChange={set('password')}
                                error={errors.password}
                                autoComplete="new-password"
                                maxLength={128}
                                suffix={
                                    <button type="button" className="btn btn-ghost btn-icon btn-sm" onClick={() => setShowPassword(s => !s)} aria-label={showPassword ? 'Ocultar' : 'Mostrar'}>
                                        {showPassword ? <EyeOff /> : <Eye />}
                                    </button>
                                }
                            />
                            <div className="strength" aria-hidden>
                                {PASSWORD_RULES.map((r, i) => (
                                    <span key={r.id} style={{ background: i < passed ? STRENGTH_COLORS[passed] : undefined }} />
                                ))}
                            </div>
                            <ul className="password-rules">
                                {PASSWORD_RULES.map(r => {
                                    const ok = r.test(form.password);
                                    return <li key={r.id} className={ok ? 'ok' : ''}>{ok ? <Check /> : <Circle />}{r.label}</li>;
                                })}
                            </ul>
                        </div>
                    </>
                )}
            </form>
        </Modal>
    );
}
