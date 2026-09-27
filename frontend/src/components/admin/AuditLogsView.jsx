import React, { useState, useEffect } from 'react';
import { adminService } from '../../services/adminService';

export default function AuditLogsView({ showNotification }) {
  const [logs, setLogs] = useState([]);
  const [loading, setLoading] = useState(false);
  const [searchTerm, setSearchTerm] = useState('');
  const [filterType, setFilterType] = useState('ALL');

  const fetchLogs = async (search = '') => {
    setLoading(true);
    try {
      const res = await adminService.getAuditLogs(search, 150);
      if (res && res.data) {
        setLogs(res.data);
      }
    } catch {
      showNotification && showNotification('❌ Không thể tải lịch sử tương tác hệ thống!');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchLogs(searchTerm);
  }, []);

  const handleSearch = (e) => {
    if (e.key === 'Enter') {
      fetchLogs(searchTerm);
    }
  };

  const getActionBadgeClass = (action = '') => {
    const act = action.toLowerCase();
    if (act.includes('đăng nhập') || act.includes('login')) return 'tap-badge-green';
    if (act.includes('đăng xuất') || act.includes('logout')) return 'tap-badge-gray';
    if (act.includes('tạo mới') || act.includes('thêm')) return 'tap-badge-blue';
    if (act.includes('cập nhật') || act.includes('sửa')) return 'tap-badge-orange';
    if (act.includes('xóa') || act.includes('hủy')) return 'tap-badge-red';
    return 'tap-badge-purple';
  };

  const filteredLogs = logs.filter((item) => {
    if (filterType === 'ALL') return true;
    if (filterType === 'AUTH') {
      return item.hanhDong.toLowerCase().includes('đăng nhập') || item.hanhDong.toLowerCase().includes('đăng xuất');
    }
    if (filterType === 'MEMBER') {
      return item.doiTuong.toLowerCase().includes('khách hàng') || item.doiTuong.toLowerCase().includes('hội viên');
    }
    if (filterType === 'DATA') {
      return item.hanhDong.toLowerCase().includes('tạo') || item.hanhDong.toLowerCase().includes('cập nhật') || item.hanhDong.toLowerCase().includes('xóa');
    }
    return true;
  });

  return (
    <div className="tap-admin-page-container">
      {/* HEADER SECTION */}
      <div className="tap-admin-header-row">
        <div className="tap-admin-header-left">
          <div className="tap-admin-page-icon icon-audit">
            <i className="fa-solid fa-clock-rotate-left"></i>
          </div>
          <div>
            <h2 className="tap-admin-title">LỊCH SỬ TƯƠNG TÁC HỆ THỐNG</h2>
            <p className="tap-admin-subtitle">
              Ghi nhận toàn bộ thao tác, đăng nhập và thay đổi dữ liệu trong cơ sở dữ liệu Firebird (Bảng STRACKING)
            </p>
          </div>
        </div>

        <div className="tap-admin-header-right">
          <button className="tap-admin-btn-refresh" onClick={() => fetchLogs(searchTerm)} disabled={loading}>
            <i className={`fa-solid fa-arrows-rotate ${loading ? 'fa-spin' : ''}`}></i>
            <span>Làm mới ({filteredLogs.length})</span>
          </button>
        </div>
      </div>

      {/* FILTER & STATS BAR */}
      <div className="tap-admin-toolbar">
        <div className="tap-admin-search-wrap">
          <i className="fa-solid fa-magnifying-glass"></i>
          <input
            type="text"
            className="tap-admin-search-input"
            placeholder="Tìm theo tài khoản, đối tượng, hành động (Nhấn Enter)..."
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
            onKeyDown={handleSearch}
          />
          {searchTerm && (
            <button className="tap-clear-btn" onClick={() => { setSearchTerm(''); fetchLogs(''); }}>
              <i className="fa-solid fa-xmark"></i>
            </button>
          )}
        </div>

        <div className="tap-filter-tabs">
          <button
            className={`tap-filter-btn ${filterType === 'ALL' ? 'active' : ''}`}
            onClick={() => setFilterType('ALL')}
          >
            Tất cả ({logs.length})
          </button>
          <button
            className={`tap-filter-btn ${filterType === 'AUTH' ? 'active' : ''}`}
            onClick={() => setFilterType('AUTH')}
          >
            Đăng nhập / Ra
          </button>
          <button
            className={`tap-filter-btn ${filterType === 'MEMBER' ? 'active' : ''}`}
            onClick={() => setFilterType('MEMBER')}
          >
            Hội viên & Thẻ
          </button>
          <button
            className={`tap-filter-btn ${filterType === 'DATA' ? 'active' : ''}`}
            onClick={() => setFilterType('DATA')}
          >
            Thao tác dữ liệu
          </button>
        </div>
      </div>

      {/* TABLE */}
      <div className="tap-admin-card tap-table-card">
        <div className="tap-table-responsive">
          <table className="tap-admin-table">
            <thead>
              <tr>
                <th style={{ width: '150px' }}>THỜI GIAN</th>
                <th style={{ width: '160px' }}>TÀI KHOẢN</th>
                <th style={{ width: '170px' }}>ĐỐI TƯỢNG</th>
                <th>HÀNH ĐỘNG THỰC HIỆN</th>
                <th style={{ width: '120px' }}>ĐỊA CHỈ IP</th>
                <th style={{ width: '180px' }}>CHI TIẾT / GHI CHÚ</th>
              </tr>
            </thead>
            <tbody>
              {loading ? (
                <tr>
                  <td colSpan="6" className="tap-table-empty">
                    <i className="fa-solid fa-spinner fa-spin"></i>
                    <span>Đang nạp lịch sử tương tác từ hệ thống...</span>
                  </td>
                </tr>
              ) : filteredLogs.length === 0 ? (
                <tr>
                  <td colSpan="6" className="tap-table-empty">
                    <i className="fa-regular fa-folder-open"></i>
                    <span>Không có bản ghi nhật ký nào phù hợp</span>
                  </td>
                </tr>
              ) : (
                filteredLogs.map((log, idx) => (
                  <tr key={log.id || idx}>
                    <td className="tap-time-cell">
                      <span className="tap-date-text">{log.ngay || log.thoiGian.split(' ')[0]}</span>
                      <span className="tap-hour-text">{log.gio || log.thoiGian.split(' ')[1]}</span>
                    </td>
                    <td>
                      <div className="tap-user-tag">
                        <i className="fa-solid fa-user-shield"></i>
                        <span>{log.taiKhoan}</span>
                      </div>
                    </td>
                    <td>
                      <span className="tap-object-badge">
                        {log.doiTuong || 'Hệ thống'}
                      </span>
                    </td>
                    <td>
                      <span className={`tap-action-badge ${getActionBadgeClass(log.hanhDong)}`}>
                        {log.hanhDong}
                      </span>
                    </td>
                    <td className="tap-ip-cell">
                      <code>{log.ip}</code>
                    </td>
                    <td className="tap-note-cell">
                      {log.ghiChu || '---'}
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
}
