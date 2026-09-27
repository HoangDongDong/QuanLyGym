import api from './api';

export const khachHangService = {
  // Lấy danh sách khách hàng (tìm kiếm F3 & lọc trạng thái / nhóm theo cây)
  getAll: async (search = '', trangThaiId = 'all', nhomKhachHangId = '') => {
    try {
      const params = { search };
      if (trangThaiId) params.trangThaiId = trangThaiId;
      if (nhomKhachHangId) params.nhomKhachHangId = nhomKhachHangId;

      const response = await api.get('/khachhang', { params });
      return response.data;
    } catch (error) {
      console.error('Lỗi gọi API /khachhang:', error);
      return null;
    }
  },

  // Đếm số lượng khách hàng theo cây trạng thái bên trái
  getTreeCounts: async () => {
    try {
      const response = await api.get('/khachhang/tree-counts');
      return response.data;
    } catch (error) {
      console.error('Lỗi gọi API /khachhang/tree-counts:', error);
      return { success: false, counts: {} };
    }
  },

  // Lấy dữ liệu chi tiết cho tất cả các tab ở phần đáy
  getSubtabs: async (id) => {
    try {
      const response = await api.get(`/khachhang/${id}/subtabs`);
      return response.data;
    } catch (error) {
      console.error(`Lỗi gọi API /khachhang/${id}/subtabs:`, error);
      return { success: false, data: null };
    }
  },

  // Lấy danh mục tham chiếu phục vụ thêm/sửa khách hàng
  getMetadata: async () => {
    try {
      const response = await api.get('/khachhang/metadata');
      return response.data;
    } catch (error) {
      console.error('Lỗi gọi API /khachhang/metadata:', error);
      return { success: false, data: {} };
    }
  },

  // Thêm mới khách hàng
  create: async (data) => {
    const response = await api.post('/khachhang', data);
    return response.data;
  },

  // Cập nhật khách hàng
  update: async (id, data) => {
    const response = await api.put(`/khachhang/${id}`, data);
    return response.data;
  },

  // Xóa khách hàng (vào thùng rác STATUS = -1)
  delete: async (id) => {
    const response = await api.delete(`/khachhang/${id}`);
    return response.data;
  },

  // Nhập hàng loạt khách hàng từ Excel
  batchImport: async (customers) => {
    const response = await api.post('/khachhang/batch-import', customers);
    return response.data;
  },

  // Phục hồi khách hàng từ thùng rác
  restore: async (id) => {
    const response = await api.post(`/khachhang/${id}/restore`);
    return response.data;
  },

  // Xóa vĩnh viễn khách hàng khỏi CSDL
  permanentDelete: async (id) => {
    const response = await api.delete(`/khachhang/${id}/permanent`);
    return response.data;
  },

  // Lấy danh sách icon từ SIMAGE
  getIcons: async () => {
    try {
      const response = await api.get('/khachhang/icons');
      return response.data;
    } catch (error) {
      console.error('Lỗi gọi API /khachhang/icons:', error);
      return { success: false, data: [] };
    }
  },

  // Lấy danh sách cây (trạng thái hoặc nhóm khách hàng)
  getTreeItems: async (mode = 'trangThai') => {
    try {
      const response = await api.get(`/khachhang/tree-items?mode=${mode}`);
      return response.data;
    } catch (error) {
      console.error(`Lỗi gọi API /khachhang/tree-items?mode=${mode}:`, error);
      return { success: false, data: [] };
    }
  },

  // Tạo một mục cây mới (trạng thái, thư mục, hoặc phân cách)
  createTreeItem: async (data) => {
    const response = await api.post('/khachhang/tree-item', data);
    return response.data;
  },

  // Cập nhật mục cây
  updateTreeItem: async (id, data) => {
    const response = await api.put(`/khachhang/tree-item/${id}`, data);
    return response.data;
  },

  // Xóa mục cây
  deleteTreeItem: async (id, mode = 'trangThai') => {
    const response = await api.delete(`/khachhang/tree-item/${id}?mode=${mode}`);
    return response.data;
  },

  // Thêm nhanh hàng loạt mục cây
  batchCreateTreeItems: async (data) => {
    const response = await api.post('/khachhang/tree-items/batch', data);
    return response.data;
  }
};

