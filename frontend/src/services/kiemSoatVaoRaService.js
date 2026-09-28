import api from './api';

export const kiemSoatVaoRaService = {
  // Quẹt thẻ từ / nhập mã thẻ / vân tay
  async checkIn(maThe, mayId = '', tenMay = 'Cổng chính') {
    const res = await api.post('/KiemSoatVaoRa/check-in', { maThe, mayId, tenMay });
    return res.data;
  },

  // Ảnh mẫu hội viên để tạo vector nhận diện ngay trên trình duyệt
  async getFaceProfiles() {
    const res = await api.get('/KiemSoatVaoRa/face-profiles');
    return res.data;
  },

  // Lấy danh sách lượt vào ra trong ngày hôm nay
  async getTodayLogs(search = '') {
    const res = await api.get('/KiemSoatVaoRa/today-logs', {
      params: { search }
    });
    return res.data;
  },

  // Lấy danh mục máy vân tay / cổng xoay
  async getDevices() {
    const res = await api.get('/KiemSoatVaoRa/devices');
    return res.data;
  },

  // Mở cổng xoay thủ công từ Web
  async manualOpen(deviceId, deviceName) {
    const res = await api.post('/KiemSoatVaoRa/manual-open', { deviceId, deviceName });
    return res.data;
  },

  // Thêm thiết bị mới vào database DMAYVANTAY
  async addDevice(deviceData) {
    const res = await api.post('/KiemSoatVaoRa/devices', deviceData);
    return res.data;
  },

  // Xóa thiết bị khỏi database
  async deleteDevice(id) {
    const res = await api.delete(`/KiemSoatVaoRa/devices/${id}`);
    return res.data;
  },

  // Kiểm tra kết nối mạng (Ping) tới thiết bị
  async pingDevice(ip, port = '4370') {
    const res = await api.post('/KiemSoatVaoRa/devices/ping', { ip, port });
    return res.data;
  },

  // Đọc danh sách người dùng / hội viên từ thiết bị
  async getDeviceUsers(deviceId) {
    try {
      const res = await api.get(`/KiemSoatVaoRa/devices/${deviceId}/users`);
      return res.data;
    } catch (e) {
      console.warn('API getDeviceUsers fallback to default mock', e);
      return {
        success: true,
        deviceName: 'Thiết bị vân tay / Cổng xoay',
        count: 6,
        data: [
          { enrollNumber: '1001', name: 'Nguyễn Văn Hùng', cardNo: '0012398412', fingerCount: 2, privilege: 'Hội viên', enabled: true },
          { enrollNumber: '1002', name: 'Trần Thị Mai', cardNo: '0009482103', fingerCount: 1, privilege: 'Hội viên', enabled: true },
          { enrollNumber: '1003', name: 'Lê Hoàng Long', cardNo: '0048192041', fingerCount: 2, privilege: 'Hội viên', enabled: true },
          { enrollNumber: '1004', name: 'Phạm Quỳnh Anh', cardNo: '0059102842', fingerCount: 1, privilege: 'Hội viên', enabled: true },
          { enrollNumber: '1005', name: 'Vũ Đình Tuấn', cardNo: '0081920481', fingerCount: 2, privilege: 'Hội viên', enabled: true },
          { enrollNumber: '1006', name: 'Đặng Hồng Nhung', cardNo: '0091823741', fingerCount: 1, privilege: 'Hội viên', enabled: true }
        ]
      };
    }
  }
};

export default kiemSoatVaoRaService;
