import React, { useState } from 'react';
import { authService } from '../../services/authService';
import './LoginModal.css';

export default function LoginModal({ isOpen, onClose, onLoginSuccess, isFullScreen = false }) {
  // Input states - Hỗ trợ cả Admin và Mã hội viên
  const [username, setUsername] = useState('Admin');
  const [password, setPassword] = useState('');
  const [showPassword, setShowPassword] = useState(false);
  const [rememberDevice, setRememberDevice] = useState(true);

  // Status states
  const [loading, setLoading] = useState(false);
  const [isBiometricScanning, setIsBiometricScanning] = useState(false);
  const [errorMsg, setErrorMsg] = useState('');
  const [successMsg, setSuccessMsg] = useState('');

  if (!isOpen) return null;

  const handleNormalLogin = async (e) => {
    if (e) e.preventDefault();
    setErrorMsg('');
    setSuccessMsg('');
    setLoading(true);

    try {
      const res = await authService.login(username, password, rememberDevice);
      setSuccessMsg(res.message || 'Đăng nhập thành công!');
      setTimeout(() => {
        onLoginSuccess(res.user);
      }, 400);
    } catch (err) {
      console.error('Lỗi đăng nhập:', err);
      setErrorMsg(err.response?.data?.message || err.message || 'Tên đăng nhập hoặc mật khẩu không chính xác!');
    } finally {
      setLoading(false);
    }
  };

  // Xác thực sinh trắc học Face ID / Vân tay một chạm
  const handleBiometricAuth = async () => {
    setErrorMsg('');
    setSuccessMsg('');
    setIsBiometricScanning(true);

    try {
      // Giả lập quét 1 giây để tạo hiệu ứng radar cyber thực tế
      await new Promise(r => setTimeout(r, 1000));
      const res = await authService.biometricLogin();
      setSuccessMsg(res.message || 'Xác thực sinh trắc học thành công!');
      setTimeout(() => {
        setIsBiometricScanning(false);
        onLoginSuccess(res.user);
      }, 500);
    } catch (err) {
      setIsBiometricScanning(false);
      console.error('Lỗi Face ID:', err);
      setErrorMsg(err.response?.data?.message || err.message || 'Không thể xác thực sinh trắc học!');
    }
  };

  const fillQuickAccount = (user, pass = '') => {
    setUsername(user);
    setPassword(pass);
    setErrorMsg('');
  };

  return (
    <div
      className={`cyber-login-overlay ${isFullScreen ? 'fullscreen-mode' : ''}`}
      onClick={isFullScreen ? undefined : onClose}
    >
      <div className="cyber-login-container" onClick={(e) => e.stopPropagation()}>
        {/* Close Button - Chỉ hiện khi ở dạng pop-up từ trang quản trị */}
        {!isFullScreen && onClose && (
          <button className="cyber-modal-close-btn" onClick={onClose} title="Đóng cửa sổ">
            <i className="fa-solid fa-xmark"></i>
          </button>
        )}

        {/* ================= 1. LEFT HERO PANEL ================= */}
        <div className="cyber-login-hero">
          <div className="hero-top-content">
            <div className="hero-header-badge">
              <span className="wave-icon-wrap">
                <svg width="22" height="18" viewBox="0 0 24 20" fill="none" xmlns="http://www.w3.org/2000/svg">
                  <path d="M2 13C4.5 13 6 10 9 10C12 10 13.5 13 16 13C18.5 13 20 10 22 10" stroke="#00e5ff" strokeWidth="2.5" strokeLinecap="round"/>
                  <path d="M2 7C4.5 7 6 4 9 4C12 4 13.5 7 16 7C18.5 7 20 4 22 4" stroke="#00e5ff" strokeWidth="2.5" strokeLinecap="round" opacity="0.6"/>
                </svg>
              </span>
              <span className="badge-flow-text">ENERGY OF FLOW & MOMENTUM</span>
            </div>

            <h1 className="hero-main-title">TÂN AN PHÁT GYM</h1>
            <p className="hero-sub-desc">
              Dòng Chảy Linh Hoạt & Bứt Phá Tốc Độ. Hệ sinh thái quản lý phòng tập chuyên nghiệp.
            </p>

            {/* 3 Glassmorphism Feature Cards */}
            <div className="hero-features-grid">
              {/* Card 1: 0.2s */}
              <div className="feature-glass-card">
                <div className="card-top-meta">
                  <i className="fa-solid fa-gauge-high card-top-icon"></i>
                  <span className="card-chip">FLOW 01</span>
                </div>
                <div className="card-stat-number">0.2s</div>
                <div className="card-stat-label">Tốc độ quét cửa xoay</div>
              </div>

              {/* Card 2: 100% */}
              <div className="feature-glass-card">
                <div className="card-top-meta">
                  <i className="fa-solid fa-door-open card-top-icon"></i>
                  <span className="card-chip">AUTO</span>
                </div>
                <div className="card-stat-number">100%</div>
                <div className="card-stat-label">Tự động Check-in</div>
              </div>

              {/* Card 3: AI-SYNC */}
              <div className="feature-glass-card">
                <div className="card-top-meta">
                  <i className="fa-regular fa-face-smile card-top-icon"></i>
                  <span className="card-chip">BIOMETRIC</span>
                </div>
                <div className="card-stat-number">AI-SYNC</div>
                <div className="card-stat-label">Sinh trắc Face ID</div>
              </div>
            </div>
          </div>
        </div>

        {/* ================= 2. RIGHT FORM PANEL ================= */}
        <div className="cyber-login-form-side">
          {/* Quick Account Suggestions */}
          <div style={{ display: 'flex', alignItems: 'center', gap: '8px', marginBottom: '14px', fontSize: '11px', color: '#64748b' }}>
            <span>Gợi ý tài khoản:</span>
            <button
              type="button"
              onClick={() => fillQuickAccount('Admin', '')}
              style={{ background: 'rgba(0, 229, 255, 0.1)', border: '1px solid rgba(0, 229, 255, 0.25)', color: '#00e5ff', padding: '2px 8px', borderRadius: '4px', cursor: 'pointer', fontSize: '11px' }}
            >
              Admin
            </button>
            <button
              type="button"
              onClick={() => fillQuickAccount('3281283', '')}
              style={{ background: 'rgba(255, 255, 255, 0.06)', border: '1px solid rgba(255, 255, 255, 0.15)', color: '#cbd5e1', padding: '2px 8px', borderRadius: '4px', cursor: 'pointer', fontSize: '11px' }}
            >
              Hội viên (3281283)
            </button>
          </div>

          {/* Feedback Messages */}
          {isBiometricScanning && (
            <div className="biometric-scan-pulse">
              <i className="fa-solid fa-fingerprint fa-beat" style={{ fontSize: '18px' }}></i>
              <span>Đang kết nối CSDL Firebird và nhận diện sinh trắc học...</span>
            </div>
          )}

          {errorMsg && (
            <div className="error-banner-cyber">
              <i className="fa-solid fa-triangle-exclamation"></i>
              <span>{errorMsg}</span>
            </div>
          )}

          {successMsg && (
            <div style={{ padding: '8px 12px', marginBottom: '12px', borderRadius: '6px', background: 'rgba(16, 185, 129, 0.15)', border: '1px solid rgba(16, 185, 129, 0.3)', color: '#6ee7b7', fontSize: '11.5px', display: 'flex', alignItems: 'center', gap: '8px' }}>
              <i className="fa-solid fa-circle-check"></i>
              <span>{successMsg}</span>
            </div>
          )}

          <form onSubmit={handleNormalLogin}>
            {/* Input 1: Email / Member ID / Username */}
            <div className="cyber-field-group">
              <div className="field-label-row">
                <span className="field-title">EMAIL HOẶC MÃ HỘI VIÊN</span>
                <span className="fast-pass-tag">
                  <i className="fa-solid fa-bolt"></i>
                  <span>active pr fast-pass</span>
                </span>
              </div>
              <div className="cyber-input-wrap">
                <i className="fa-regular fa-circle-user input-left-icon"></i>
                <input
                  type="text"
                  className="cyber-input"
                  value={username}
                  onChange={(e) => setUsername(e.target.value)}
                  placeholder="Nhập tên đăng nhập hoặc mã thẻ..."
                  required
                  autoFocus
                />
              </div>
            </div>

            {/* Input 2: Password */}
            <div className="cyber-field-group">
              <div className="field-label-row">
                <span className="field-title">MẬT KHẨU</span>
                <a
                  href="#forgot"
                  className="forgot-pass-link"
                  onClick={(e) => { e.preventDefault(); alert('Mặc định tài khoản quản trị Admin để trống mật khẩu. Hội viên vui lòng liên hệ quầy lễ tân nếu quên mật khẩu!'); }}
                >
                  Quên mật khẩu?
                </a>
              </div>
              <div className="cyber-input-wrap">
                <i className="fa-solid fa-lock input-left-icon"></i>
                <input
                  type={showPassword ? 'text' : 'password'}
                  className="cyber-input"
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  placeholder="Nhập mật khẩu (Admin để trống)..."
                />
                <button
                  type="button"
                  className="password-toggle-btn"
                  onClick={() => setShowPassword(!showPassword)}
                  title={showPassword ? 'Ẩn mật khẩu' : 'Hiện mật khẩu'}
                >
                  <i className={`fa-regular ${showPassword ? 'fa-eye-slash' : 'fa-eye'}`}></i>
                </button>
              </div>
            </div>

            {/* Remember Checkbox */}
            <div
              className="remember-row"
              onClick={() => setRememberDevice(!rememberDevice)}
            >
              <div className={`custom-checkbox ${rememberDevice ? '' : 'unchecked'}`}>
                {rememberDevice && <i className="fa-solid fa-check"></i>}
              </div>
              <span className="remember-label-text">
                Ghi nhớ đăng nhập trên thiết bị lễ tân / máy cá nhân
              </span>
            </div>

            {/* Submit Button */}
            <button
              type="submit"
              className="btn-submit-cyber"
              disabled={loading || isBiometricScanning}
            >
              {loading ? (
                <>
                  <i className="fa-solid fa-circle-notch fa-spin"></i> ĐANG XÁC THỰC CSDL...
                </>
              ) : (
                <>
                  <span>ĐĂNG NHẬP NGAY</span>
                  <i className="fa-solid fa-arrow-right"></i>
                </>
              )}
            </button>

            {/* Secondary Biometric Button */}
            <button
              type="button"
              className="btn-biometric-cyber"
              onClick={handleBiometricAuth}
              disabled={loading || isBiometricScanning}
            >
              <i className="fa-solid fa-fingerprint"></i>
              <span>Xác thực Face ID / Vân tay một chạm</span>
            </button>
          </form>
        </div>
      </div>
    </div>
  );
}
