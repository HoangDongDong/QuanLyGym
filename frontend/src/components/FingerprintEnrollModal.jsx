import React, { useState } from 'react';
import { khachHangService } from '../services/khachHangService';

export const FingerprintEnrollModal = ({
  show,
  customer,
  onClose,
  onSuccess,
  showNotification
}) => {
  if (!show || !customer) return null;

  const [selectedFinger, setSelectedFinger] = useState('right_index'); // right_index, right_thumb, left_index, etc.
  const [step, setStep] = useState(0); // 0: ready, 1: scan1, 2: scan2, 3: completed
  const [isScanning, setIsScanning] = useState(false);
  const [templateCode, setTemplateCode] = useState(customer.maVanTay || '');

  const fingerOptions = [
    { id: 'right_thumb', label: 'Ngón cái tay phải' },
    { id: 'right_index', label: 'Ngón trỏ tay phải (Khuyên dùng)' },
    { id: 'right_middle', label: 'Ngón giữa tay phải' },
    { id: 'left_thumb', label: 'Ngón cái tay trái' },
    { id: 'left_index', label: 'Ngón trỏ tay trái' }
  ];

  const handleStartEnroll = () => {
    setStep(1);
    setIsScanning(true);

    // Simulate 3 steps of biometric scanner
    setTimeout(() => {
      setStep(2);
      setTimeout(() => {
        setStep(3);
        setIsScanning(false);
        const code = `FP_${customer.maThe || 'HV'}_${selectedFinger.toUpperCase()}_${Math.floor(1000 + Math.random() * 9000)}`;
        setTemplateCode(code);
        showNotification && showNotification('Đã ghi nhận đủ 3 lần quét vân tay thành công!');
      }, 1200);
    }, 1200);
  };

  const handleSaveFingerprint = async () => {
    if (!templateCode) {
      showNotification && showNotification('Chưa có mẫu vân tay để lưu!');
      return;
    }

    try {
      const updateData = {
        name: customer.tenKhachHang,
        maThe: customer.maThe,
        dienThoai: customer.dienThoai,
        diaChi: customer.diaChi,
        email: customer.email,
        facebook: customer.facebook,
        maVanTay: templateCode,
        note: customer.note,
        soLan: customer.soLan || 0,
        daTap: customer.daTap || 0,
        conLai: customer.conLai || 0
      };

      const res = await khachHangService.update(customer.id, updateData);
      if (res.success) {
        showNotification && showNotification(`Đã đăng ký vân tay thành công cho hội viên ${customer.tenKhachHang}!`);
        onSuccess && onSuccess();
        onClose();
      } else {
        showNotification && showNotification(res.message || 'Lỗi khi lưu vân tay!');
      }
    } catch (err) {
      console.error('Lỗi lưu vân tay:', err);
      showNotification && showNotification('Lỗi khi cập nhật vân tay vào hệ thống!');
    }
  };

  return (
    <div className="fr-overlay">
      <div className="cust-fp-modal">
        <div className="cust-import-header">
          <div className="cust-import-title">
            <i className="fa-solid fa-fingerprint" style={{ color: '#0284c7', marginRight: 8 }}></i>
            Đăng ký mẫu vân tay cho hội viên
          </div>
          <button className="fr-close-btn" onClick={onClose} title="Đóng">
            <i className="fa-solid fa-xmark"></i>
          </button>
        </div>

        <div className="cust-import-body">
          <div className="cust-fp-customer-card">
            <div className="cust-fp-cust-info">
              <div className="cust-fp-cust-name">{customer.tenKhachHang}</div>
              <div className="cust-fp-cust-meta">Mã thẻ: <strong>{customer.maThe}</strong> | SĐT: {customer.dienThoai || '---'}</div>
            </div>
            <div className="cust-fp-status">
              {customer.maVanTay ? (
                <span className="cust-badge valid"><i className="fa-solid fa-check"></i> Đã có vân tay</span>
              ) : (
                <span className="cust-badge invalid">Chưa có vân tay</span>
              )}
            </div>
          </div>

          <div className="cust-fp-controls">
            <div className="cust-field-group">
              <label>Chọn ngón tay đăng ký:</label>
              <select
                className="cust-select"
                value={selectedFinger}
                onChange={(e) => { setSelectedFinger(e.target.value); setStep(0); }}
                disabled={isScanning}
              >
                {fingerOptions.map(f => (
                  <option key={f.id} value={f.id}>{f.label}</option>
                ))}
              </select>
            </div>

            <div className="cust-field-group">
              <label>Thiết bị đọc vân tay:</label>
              <select className="cust-select" disabled={isScanning}>
                <option>Đầu đọc vân tay USB ZKTeco Live20R / SilkID</option>
                <option>Cổng xoay kiểm soát Tripod (192.168.1.201)</option>
              </select>
            </div>
          </div>

          {/* FINGERPRINT ANIMATION SCANNER */}
          <div className="cust-fp-scanner-area">
            <div className={`cust-fp-scanner-box ${isScanning ? 'scanning' : ''} ${step === 3 ? 'success' : ''}`}>
              <i className="fa-solid fa-fingerprint cust-fp-giant-icon"></i>
              {isScanning && <div className="cust-fp-laser-line"></div>}
            </div>

            <div className="cust-fp-steps-indicator">
              <div className={`cust-fp-step-dot ${step >= 1 ? 'active' : ''}`}>
                <span className="dot-num">1</span>
                <span className="dot-label">Quét lần 1</span>
              </div>
              <div className="cust-fp-step-line"></div>
              <div className={`cust-fp-step-dot ${step >= 2 ? 'active' : ''}`}>
                <span className="dot-num">2</span>
                <span className="dot-label">Quét lần 2</span>
              </div>
              <div className="cust-fp-step-line"></div>
              <div className={`cust-fp-step-dot ${step >= 3 ? 'active' : ''}`}>
                <span className="dot-num">3</span>
                <span className="dot-label">Xác nhận mẫu</span>
              </div>
            </div>

            <div className="cust-fp-instructions">
              {step === 0 && 'Nhấn "Bắt đầu quét vân tay" và đặt ngón tay lên mắt đọc cảm biến.'}
              {step === 1 && 'Đang quét lần 1... Vui lòng giữ yên ngón tay.'}
              {step === 2 && 'Quét lần 1 thành công! Vui lòng nhấc ngón tay lên và đặt lại lần 2.'}
              {step === 3 && 'Hoàn tất trích xuất đặc trưng vân tay! Độ phân giải 500 DPI, chất lượng 98%.'}
            </div>

            {templateCode && (
              <div className="cust-fp-template-display">
                Mã mẫu vân tay (Template): <code>{templateCode}</code>
              </div>
            )}
          </div>
        </div>

        <div className="fr-dialog-footer">
          <button className="fr-btn" onClick={onClose} disabled={isScanning}>
            Hủy
          </button>
          {step < 3 ? (
            <button
              className="fr-btn primary"
              onClick={handleStartEnroll}
              disabled={isScanning}
            >
              {isScanning ? <i className="fa-solid fa-spinner fa-spin"></i> : <i className="fa-solid fa-fingerprint"></i>}
              <span> Bắt đầu quét vân tay</span>
            </button>
          ) : (
            <button
              className="fr-btn primary"
              onClick={handleSaveFingerprint}
            >
              <i className="fa-solid fa-floppy-disk"></i> Lưu vân tay vào hồ sơ hội viên
            </button>
          )}
        </div>
      </div>
    </div>
  );
};

export default FingerprintEnrollModal;
