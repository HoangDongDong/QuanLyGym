import React, { useState, useEffect } from 'react';
import { kiemSoatVaoRaService } from '../services/kiemSoatVaoRaService';
import { khachHangService } from '../services/khachHangService';

export const DeviceSyncModal = ({
  show,
  onClose,
  onSuccess,
  showNotification
}) => {
  if (!show) return null;

  const [devices, setDevices] = useState([]);
  const [selectedDeviceId, setSelectedDeviceId] = useState('');
  const [deviceUsers, setDeviceUsers] = useState([]);
  const [selectedUserIds, setSelectedUserIds] = useState(new Set());
  const [loadingDevices, setLoadingDevices] = useState(false);
  const [readingUsers, setReadingUsers] = useState(false);
  const [importing, setImporting] = useState(false);

  useEffect(() => {
    loadDevices();
  }, []);

  const loadDevices = async () => {
    setLoadingDevices(true);
    try {
      const res = await kiemSoatVaoRaService.getDevices();
      if (res && res.data && res.data.length > 0) {
        setDevices(res.data);
        setSelectedDeviceId(res.data[0].id);
      } else {
        // Fallback default devices if DB table empty
        const defaultDevices = [
          { id: 'dev-1', name: 'Cổng xoay Tripod chính (Lối vào)', ip: '192.168.1.201', port: '4370' },
          { id: 'dev-2', name: 'Máy quẹt vân tay / thẻ tại quầy lễ tân', ip: '192.168.1.202', port: '4370' },
          { id: 'dev-3', name: 'Cổng xoay kiểm soát lối ra', ip: '192.168.1.203', port: '4370' }
        ];
        setDevices(defaultDevices);
        setSelectedDeviceId('dev-1');
      }
    } catch (err) {
      console.error('Lỗi tải danh sách thiết bị:', err);
    } finally {
      setLoadingDevices(false);
    }
  };

  const handleReadFromDevice = async () => {
    if (!selectedDeviceId) return;
    setReadingUsers(true);
    try {
      const res = await kiemSoatVaoRaService.getDeviceUsers(selectedDeviceId);
      if (res && res.data) {
        setDeviceUsers(res.data);
        // Select all by default
        const allIds = new Set(res.data.map(u => u.enrollNumber));
        setSelectedUserIds(allIds);
        showNotification && showNotification(`Đã đọc ${res.data.length} người dùng từ thiết bị!`);
      }
    } catch (err) {
      console.error('Lỗi đọc từ thiết bị:', err);
      showNotification && showNotification('Lỗi kết nối thiết bị! Đã tải dữ liệu mẫu.');
    } finally {
      setReadingUsers(false);
    }
  };

  const toggleSelectUser = (enrollNo) => {
    const next = new Set(selectedUserIds);
    if (next.has(enrollNo)) {
      next.delete(enrollNo);
    } else {
      next.add(enrollNo);
    }
    setSelectedUserIds(next);
  };

  const toggleSelectAll = () => {
    if (selectedUserIds.size === deviceUsers.length) {
      setSelectedUserIds(new Set());
    } else {
      setSelectedUserIds(new Set(deviceUsers.map(u => u.enrollNumber)));
    }
  };

  const handleImportToDatabase = async () => {
    const selected = deviceUsers.filter(u => selectedUserIds.has(u.enrollNumber));
    if (selected.length === 0) {
      showNotification && showNotification('Vui lòng tích chọn ít nhất 1 hội viên để đồng bộ!');
      return;
    }

    setImporting(true);
    try {
      const payload = selected.map(u => ({
        maThe: u.cardNo || u.enrollNumber,
        name: u.name,
        dienThoai: '',
        diaChi: 'Đồng bộ từ máy chấm công',
        loaiThe: 'Thẻ kiểm soát cửa',
        soLan: 90,
        conLai: 90,
        daTap: 0,
        maVanTay: `FP_ENROLL_${u.enrollNumber}`,
        note: `Đồng bộ từ thiết bị ${devices.find(d => d.id === selectedDeviceId)?.name || 'Cổng xoay'}`
      }));

      const res = await khachHangService.batchImport(payload);
      if (res.success) {
        showNotification && showNotification(res.message || `Đã đồng bộ ${selected.length} hội viên vào hệ thống!`);
        onSuccess && onSuccess();
        onClose();
      } else {
        showNotification && showNotification(res.message || 'Lỗi khi đồng bộ!');
      }
    } catch (err) {
      console.error('Lỗi đồng bộ:', err);
      showNotification && showNotification('Lỗi khi đồng bộ vào CSDL!');
    } finally {
      setImporting(false);
    }
  };

  return (
    <div className="fr-overlay">
      <div className="cust-import-modal">
        <div className="cust-import-header">
          <div className="cust-import-title">
            <i className="fa-solid fa-globe" style={{ color: '#0284c7', marginRight: 8 }}></i>
            Đồng bộ / Thêm hội viên từ máy chấm công & kiểm soát vào ra
          </div>
          <button className="fr-close-btn" onClick={onClose} title="Đóng">
            <i className="fa-solid fa-xmark"></i>
          </button>
        </div>

        <div className="cust-import-body">
          <div className="cust-device-selector-row">
            <div className="cust-field-group" style={{ flex: 1 }}>
              <label>Chọn thiết bị máy chấm công / cổng xoay:</label>
              <select
                className="cust-select"
                value={selectedDeviceId}
                onChange={(e) => setSelectedDeviceId(e.target.value)}
              >
                {devices.map(d => (
                  <option key={d.id} value={d.id}>
                    {d.name} ({d.ip}:{d.port || '4370'})
                  </option>
                ))}
              </select>
            </div>
            <button
              className="fr-btn primary"
              style={{ alignSelf: 'flex-end', height: 36 }}
              onClick={handleReadFromDevice}
              disabled={readingUsers}
            >
              {readingUsers ? <i className="fa-solid fa-spinner fa-spin"></i> : <i className="fa-solid fa-satellite-dish"></i>}
              <span> Đọc dữ liệu từ thiết bị</span>
            </button>
          </div>

          {deviceUsers.length === 0 ? (
            <div className="cust-empty-device-state">
              <i className="fa-solid fa-network-wired cust-device-big-icon"></i>
              <div className="cust-device-state-title">Chưa tải dữ liệu từ thiết bị</div>
              <div className="cust-device-state-sub">
                Nhấn <strong>"Đọc dữ liệu từ thiết bị"</strong> để kết nối TCP/IP tới máy và tải danh sách mã thẻ, mã vân tay đã đăng ký trên máy.
              </div>
            </div>
          ) : (
            <div className="cust-preview-section">
              <div className="cust-preview-stats">
                <span>Số người dùng trên máy: <strong>{deviceUsers.length}</strong></span>
                <span style={{ color: '#0284c7' }}>Đã chọn: <strong>{selectedUserIds.size}</strong></span>
              </div>
              <div className="cust-preview-table-box">
                <table className="cust-grid-table">
                  <thead>
                    <tr>
                      <th style={{ width: 35, textAlign: 'center' }}>
                        <input
                          type="checkbox"
                          checked={selectedUserIds.size === deviceUsers.length && deviceUsers.length > 0}
                          onChange={toggleSelectAll}
                        />
                      </th>
                      <th style={{ width: 85 }}>Mã Enroll</th>
                      <th style={{ width: 170 }}>Họ và tên</th>
                      <th style={{ width: 120 }}>Mã thẻ RFID</th>
                      <th style={{ width: 100, textAlign: 'center' }}>Số vân tay</th>
                      <th style={{ width: 110 }}>Quyền hạn</th>
                      <th style={{ width: 90, textAlign: 'center' }}>Trạng thái</th>
                    </tr>
                  </thead>
                  <tbody>
                    {deviceUsers.map(u => (
                      <tr
                        key={u.enrollNumber}
                        onClick={() => toggleSelectUser(u.enrollNumber)}
                        style={{ cursor: 'pointer', background: selectedUserIds.has(u.enrollNumber) ? '#f0fdf4' : 'transparent' }}
                      >
                        <td style={{ textAlign: 'center' }} onClick={(e) => e.stopPropagation()}>
                          <input
                            type="checkbox"
                            checked={selectedUserIds.has(u.enrollNumber)}
                            onChange={() => toggleSelectUser(u.enrollNumber)}
                          />
                        </td>
                        <td style={{ fontWeight: 600 }}>{u.enrollNumber}</td>
                        <td style={{ fontWeight: 600 }}>{u.name}</td>
                        <td>{u.cardNo || <em style={{ color: '#94a3b8' }}>Chưa có thẻ</em>}</td>
                        <td style={{ textAlign: 'center' }}>
                          <span className="cust-badge valid">{u.fingerCount || 1} mẫu</span>
                        </td>
                        <td>{u.privilege || 'Hội viên'}</td>
                        <td style={{ textAlign: 'center' }}>
                          <span className="cust-badge valid">Kích hoạt</span>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>
          )}
        </div>

        <div className="fr-dialog-footer">
          <button className="fr-btn" onClick={onClose} disabled={importing}>
            Đóng
          </button>
          <button
            className="fr-btn primary"
            onClick={handleImportToDatabase}
            disabled={importing || selectedUserIds.size === 0}
          >
            {importing ? <i className="fa-solid fa-spinner fa-spin"></i> : <i className="fa-solid fa-download"></i>}
            <span> Đồng bộ {selectedUserIds.size} hội viên vào CSDL Gym</span>
          </button>
        </div>
      </div>
    </div>
  );
};

export default DeviceSyncModal;
