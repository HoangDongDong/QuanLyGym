import api from './api';

export const adminService = {
  // 1. Lịch sử tương tác hệ thống (STRACKING)
  async getAuditLogs(search = '', limit = 100) {
    const res = await api.get('/Admin/audit-logs', {
      params: { search, limit }
    });
    return res.data;
  },

  // 2. Người dùng và phân quyền (SUSER & SGROUPUSER)
  async getUsers() {
    const res = await api.get('/Admin/users');
    return res.data;
  },

  async createUser(userData) {
    const res = await api.post('/Admin/users', userData);
    return res.data;
  },

  async updateUser(id, userData) {
    const res = await api.put(`/Admin/users/${id}`, userData);
    return res.data;
  },

  async getUserDetail(id) {
    const res = await api.get(`/Admin/users/${id}/detail`);
    return res.data;
  },

  async getUserMetadata() {
    const res = await api.get('/Admin/users/metadata');
    return res.data;
  },

  async createEmployee(data) {
    const res = await api.post('/Admin/employees', data);
    return res.data;
  },

  async deleteUser(id) {
    const res = await api.delete(`/Admin/users/${id}`);
    return res.data;
  },

  // 3. Cấu hình toàn hệ thống (SCONFIG & SCONFIGGROUP)
  async getConfigs() {
    const res = await api.get('/Admin/configs');
    return res.data;
  },

  async updateConfigs(items) {
    const res = await api.post('/Admin/configs/update', { items });
    return res.data;
  },

  // 4. Phân quyền người dùng & báo cáo (SGROUPUSER, SGROUPROLE, SFUNCTION, SREPORT, SREPORTROLE)
  async getPermissionsSummary() {
    const res = await api.get('/Admin/permissions/summary');
    return res.data;
  },

  async createGroup(data) {
    const res = await api.post('/Admin/permissions/groups', data);
    return res.data;
  },

  async updateGroup(id, data) {
    const res = await api.put(`/Admin/permissions/groups/${id}`, data);
    return res.data;
  },

  async deleteGroup(id) {
    const res = await api.delete(`/Admin/permissions/groups/${id}`);
    return res.data;
  },

  async uploadImage(name, data) {
    const res = await api.post('/Admin/images/upload', { name, data });
    return res.data;
  },

  async saveGroupRoles(groupId, roles) {
    const res = await api.post('/Admin/permissions/save-roles', { groupId, roles });
    return res.data;
  },

  async saveReportRoles(groupId, reportRoles) {
    const res = await api.post('/Admin/permissions/save-report-roles', { groupId, reportRoles });
    return res.data;
  }
};
