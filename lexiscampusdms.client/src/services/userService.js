import apiClient from './apiClient';

export const userService = {
    /**
     * GET /api/users — listado paginado con filtros.
     * @param {{searchTerm?:string, role?:string, isActive?:boolean, pageNumber?:number, pageSize?:number}} filter
     */
    async list({ searchTerm, role, isActive, pageNumber = 1, pageSize = 10 } = {}) {
        const params = { pageNumber, pageSize };
        if (searchTerm?.trim()) params.searchTerm = searchTerm.trim();
        if (role) params.role = role;
        if (isActive !== undefined && isActive !== '') params.isActive = isActive;
        const { data } = await apiClient.get('/users', { params });
        return data;
    },

    /** GET /api/users/{id} */
    async getById(id) {
        const { data } = await apiClient.get(`/users/${id}`);
        return data.data;
    },

    /** POST /api/users */
    async create(payload) {
        const { data } = await apiClient.post('/users', payload);
        return data.data;
    },

    /** PUT /api/users/{id} */
    async update(id, { fullName, department, studentRegistration }) {
        const { data } = await apiClient.put(`/users/${id}`, {
            fullName,
            department: department || null,
            studentRegistration: studentRegistration || null
        });
        return data.data;
    },

    /** PUT /api/users/{id}/role */
    async changeRole(id, role) {
        const { data } = await apiClient.put(`/users/${id}/role`, { role });
        return data.data;
    },

    /** PATCH /api/users/{id}/status — activar/desactivar y/o resetear bloqueo. */
    async changeStatus(id, { isActive, resetLockout = false }) {
        const { data } = await apiClient.patch(`/users/${id}/status`, { isActive, resetLockout });
        return data.data;
    }
};
