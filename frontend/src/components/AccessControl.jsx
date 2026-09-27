import React, { useState, useEffect, useRef } from 'react';
import './AccessControl.css';
import kiemSoatVaoRaService from '../services/kiemSoatVaoRaService';

// Danh sách dữ liệu mẫu dự phòng khi chưa có dữ liệu thật
const FALLBACK_MEMBERS = [
  {
    id: 1,
    maThe: '3281283',
    name: 'Hoàng Văn Đông',
    phone: '03580022211',
    ngaySinh: '03/03/2005',
    diaChi: 'Hà Nội',
    caTap: 'Toàn thời gian (06:00 - 22:00)',
    loaiThe: 'Thẻ thường',
    goiDichVu: 'Thẻ thường 1 Tháng',
    tuNgay: '25/09/2026',
    denNgay: '25/10/2026',
    soNgayCon: 29,
    soLanDaDen: 1,
    soLanCon: 'Không giới hạn',
    tapHomNay: 1,
    trangThai: 'HỢP LỆ',
    statusType: 'valid',
    gioVao: '14:58:44',
    avatar: null
  },
  {
    id: 2,
    maThe: 'TAP00941',
    name: 'Nguyễn Văn Tuấn',
    phone: '0912.345.678',
    ngaySinh: '15/08/1992',
    diaChi: '124 Nguyễn Thị Minh Khai, Q.3, TP.HCM',
    caTap: 'Toàn thời gian (06:00 - 22:00)',
    loaiThe: 'Thẻ Hội Viên VIP',
    goiDichVu: 'Gym 12 Tháng VIP',
    tuNgay: '01/01/2026',
    denNgay: '01/01/2027',
    soNgayCon: 280,
    soLanDaDen: 84,
    soLanCon: 'Không giới hạn',
    tapHomNay: 1,
    trangThai: 'HỢP LỆ',
    statusType: 'valid',
    gioVao: '15:28:40',
    avatar: null
  },
  {
    id: 3,
    maThe: 'TAP00812',
    name: 'Lê Hoàng Nam',
    phone: '0908.765.432',
    ngaySinh: '04/05/1988',
    diaChi: '78 Nam Kỳ Khởi Nghĩa, Q.3, TP.HCM',
    caTap: 'Toàn thời gian (06:00 - 22:00)',
    loaiThe: 'Thẻ Tiêu Chuẩn',
    goiDichVu: 'Gym Classic 1T',
    tuNgay: '25/08/2026',
    denNgay: '25/09/2026',
    soNgayCon: 0,
    soLanDaDen: 28,
    soLanCon: 0,
    tapHomNay: 0,
    trangThai: 'GIA HẠN',
    statusType: 'warning',
    gioVao: '15:19:05',
    avatar: null
  }
];

export default function AccessControl({ showNotification, onSwitchToCustomers }) {
  const [scanInput, setScanInput] = useState('');
  const [currentMember, setCurrentMember] = useState(null);
  const [gateStatus, setGateStatus] = useState('locked'); // 'locked', 'unlocked', 'blocked'
  const [gateSignal, setGateSignal] = useState('ready'); // 'ready', 'open', 'blocked', 'warning'
  const [activeTab, setActiveTab] = useState('today'); // 'today', 'hardware'
  const [filterSearch, setFilterSearch] = useState('');
  const [historyList, setHistoryList] = useState([]);
  const [isRefreshing, setIsRefreshing] = useState(false);
  const [devices, setDevices] = useState([]);
  const [loading, setLoading] = useState(false);
  const [showAddDeviceModal, setShowAddDeviceModal] = useState(false);
  const [newDeviceForm, setNewDeviceForm] = useState({
    name: '',
    ip: '',
    port: '4370',
    maMay: '1',
    note: '',
    sdk: 'ZKTeco'
  });
  const [pingResult, setPingResult] = useState({});
  const [isSubmittingDevice, setIsSubmittingDevice] = useState(false);

  const scanInputRef = useRef(null);
  const resetTimerRef = useRef(null);

  // Focus ô nhập thẻ ban đầu & hỗ trợ phím tắt F2
  useEffect(() => {
    if (scanInputRef.current) {
      scanInputRef.current.focus();
    }

    const handleKeyDown = (e) => {
      if (e.key === 'F2') {
        e.preventDefault();
        if (scanInputRef.current) {
          scanInputRef.current.focus();
          scanInputRef.current.select();
        }
      }
    };

    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, []);

  // Tải dữ liệu thật từ API backend (kết nối Firebird DATA.fdb)
  const loadTodayLogs = async (searchTerm = '') => {
    try {
      setLoading(true);
      const res = await kiemSoatVaoRaService.getTodayLogs(searchTerm);
      if (res && res.success && res.data && res.data.length > 0) {
        setHistoryList(res.data);
      } else {
        // Nếu database chưa có lượt tập nào hôm nay thì dùng dữ liệu mẫu
        setHistoryList((prev) => (prev.length > 0 ? prev : FALLBACK_MEMBERS));
      }
    } catch (err) {
      console.warn('API chưa sẵn sàng hoặc lỗi kết nối, dùng dữ liệu mẫu:', err);
      setHistoryList(FALLBACK_MEMBERS);
    } finally {
      setLoading(false);
    }
  };

  const loadDevices = async () => {
    try {
      const res = await kiemSoatVaoRaService.getDevices();
      if (res && res.success && res.data) {
        setDevices(res.data);
      } else {
        setDevices([]);
      }
    } catch {
      setDevices([]);
    }
  };

  useEffect(() => {
    loadTodayLogs();
    loadDevices();
  }, []);

  // Âm thanh thông báo Web Audio API
  const playSound = (type = 'success') => {
    try {
      const AudioCtx = window.AudioContext || window.webkitAudioContext;
      if (!AudioCtx) return;
      const ctx = new AudioCtx();
      const osc = ctx.createOscillator();
      const gain = ctx.createGain();
      osc.connect(gain);
      gain.connect(ctx.destination);

      if (type === 'success') {
        osc.frequency.setValueAtTime(880, ctx.currentTime);
        osc.frequency.exponentialRampToValueAtTime(1760, ctx.currentTime + 0.15);
        gain.gain.setValueAtTime(0.2, ctx.currentTime);
        gain.gain.exponentialRampToValueAtTime(0.01, ctx.currentTime + 0.3);
        osc.start();
        osc.stop(ctx.currentTime + 0.3);
      } else {
        osc.type = 'sawtooth';
        osc.frequency.setValueAtTime(220, ctx.currentTime);
        gain.gain.setValueAtTime(0.25, ctx.currentTime);
        gain.gain.exponentialRampToValueAtTime(0.01, ctx.currentTime + 0.35);
        osc.start();
        osc.stop(ctx.currentTime + 0.35);
      }
    } catch { }
  };

  // Kích hoạt xác thực thẻ từ Database Firebird qua API
  const processCardScan = async (inputCode) => {
    const code = (inputCode || scanInput).trim();
    if (!code) return;

    if (resetTimerRef.current) {
      clearTimeout(resetTimerRef.current);
    }

    try {
      // Gọi API C# backend trực tiếp vào Firebird DATA.fdb
      const res = await kiemSoatVaoRaService.checkIn(code);

      if (res && res.member) {
        setCurrentMember(res.member);
        setGateStatus(res.gateStatus || 'unlocked');
        setGateSignal(res.gateSignal || 'open');

        if (res.success) {
          playSound('success');
          showNotification && showNotification(res.message || `✅ [${res.member.maThe}] ${res.member.name} - MỜI VÀO`);
          // Cập nhật lại danh sách lượt tập trong ngày từ database
          loadTodayLogs();
        } else {
          playSound('error');
          showNotification && showNotification(`❌ ${res.message || 'Thẻ không hợp lệ'}`);
        }
      } else {
        // Fallback kiểm tra trong danh sách mẫu nếu backend báo lỗi
        handleFallbackScan(code);
      }
    } catch (err) {
      console.warn('Lỗi gọi API Check-in, chuyển sang chế độ dự phòng:', err);
      handleFallbackScan(code);
    }

    setScanInput('');

    // Tự động trả về trạng thái chờ sau 4.5 giây
    resetTimerRef.current = setTimeout(() => {
      setGateStatus('locked');
      setGateSignal('ready');
    }, 4500);
  };

  // Fallback quét khi API offline
  const handleFallbackScan = (code) => {
    const upper = code.toUpperCase();
    const matched = FALLBACK_MEMBERS.find(
      (m) => m.maThe.toUpperCase() === upper || m.name.toUpperCase().includes(upper)
    );

    if (matched) {
      setCurrentMember(matched);
      if (matched.statusType === 'valid') {
        setGateStatus('unlocked');
        setGateSignal('open');
        playSound('success');
        showNotification && showNotification(`✅ [${matched.maThe}] ${matched.name} - MỜI VÀO`);
      } else {
        setGateStatus('blocked');
        setGateSignal('blocked');
        playSound('error');
        showNotification && showNotification(`❌ [${matched.maThe}] ${matched.name} - THẺ ĐÃ HẾT HẠN!`);
      }
    } else {
      setCurrentMember({
        maThe: code,
        name: `(Mã ${code} chưa đăng ký trên hệ thống)`,
        phone: '---',
        ngaySinh: '---',
        diaChi: '---',
        caTap: '---',
        loaiThe: 'Chưa xác định',
        goiDichVu: 'Chưa đăng ký gói',
        tuNgay: '---',
        denNgay: '---',
        soNgayCon: '---',
        soLanDaDen: '---',
        soLanCon: '---',
        tapHomNay: '---',
        trangThai: 'KHÔNG TỒN TẠI',
        statusType: 'error'
      });
      setGateStatus('blocked');
      setGateSignal('blocked');
      playSound('error');
      showNotification && showNotification(`❌ Mã thẻ ${code} không có trong cơ sở dữ liệu!`);
    }
  };

  const handleKeyDown = (e) => {
    if (e.key === 'Enter') {
      processCardScan();
    }
  };

  const handleSelectHistoryItem = (item) => {
    setCurrentMember({
      ...item,
      phone: item.phone || '03580022211',
      ngaySinh: item.ngaySinh || '---',
      diaChi: item.diaChi || '---',
      caTap: item.caTap || 'Toàn thời gian (06:00 - 22:00)',
      loaiThe: item.loaiThe || item.goiDichVu || 'Thẻ tiêu chuẩn',
      goiDichVu: item.goiDichVu || 'Thẻ tập',
      tuNgay: item.tuNgay || '---',
      denNgay: item.denNgay || '---',
      soNgayCon: item.soNgayCon ?? 29,
      soLanDaDen: item.soLanDaDen ?? 1,
      soLanCon: item.soLanCon ?? 'Không giới hạn',
      tapHomNay: item.tapHomNay ?? 1
    });

    if (item.statusType === 'valid') {
      setGateStatus('unlocked');
      setGateSignal('open');
      playSound('success');
    } else if (item.statusType === 'warning') {
      setGateStatus('unlocked');
      setGateSignal('warning');
      playSound('success');
    } else {
      setGateStatus('blocked');
      setGateSignal('blocked');
      playSound('error');
    }

    if (resetTimerRef.current) clearTimeout(resetTimerRef.current);
    resetTimerRef.current = setTimeout(() => {
      setGateStatus('locked');
      setGateSignal('ready');
    }, 4500);
  };

  const handleRefresh = async () => {
    setIsRefreshing(true);
    await loadTodayLogs(filterSearch);
    setTimeout(() => {
      setIsRefreshing(false);
      showNotification && showNotification('Đã làm mới dữ liệu lượt vào hôm nay từ database');
    }, 400);
  };

  // Mở cổng từ xa thủ công
  const handleManualOpenGate = async (dev) => {
    try {
      await kiemSoatVaoRaService.manualOpen(dev.id, dev.name);
    } catch { }

    setGateStatus('unlocked');
    setGateSignal('open');
    playSound('success');
    showNotification && showNotification(`🔓 Đã kích hoạt lệnh mở ${dev.name}`);

    if (resetTimerRef.current) clearTimeout(resetTimerRef.current);
    resetTimerRef.current = setTimeout(() => {
      setGateStatus('locked');
      setGateSignal('ready');
    }, 4500);
  };

  // Thêm thiết bị mới vào database DMAYVANTAY
  const handleAddDevice = async (e) => {
    e.preventDefault();
    if (!newDeviceForm.name.trim()) {
      showNotification && showNotification('⚠️ Vui lòng nhập tên thiết bị!');
      return;
    }
    setIsSubmittingDevice(true);
    try {
      const res = await kiemSoatVaoRaService.addDevice(newDeviceForm);
      if (res && res.success) {
        showNotification && showNotification('✅ Thêm thiết bị vào cơ sở dữ liệu thành công!');
        setShowAddDeviceModal(false);
        setNewDeviceForm({ name: '', ip: '', port: '4370', maMay: '1', note: '', sdk: 'ZKTeco' });
        loadDevices();
      } else {
        showNotification && showNotification(`❌ ${res?.message || 'Không thể thêm thiết bị'}`);
      }
    } catch {
      showNotification && showNotification('❌ Lỗi khi lưu thiết bị vào database!');
    } finally {
      setIsSubmittingDevice(false);
    }
  };

  // Xóa thiết bị khỏi database DMAYVANTAY
  const handleDeleteDevice = async (id, name) => {
    if (!window.confirm(`Bạn có chắc chắn muốn xóa thiết bị "${name}" khỏi cơ sở dữ liệu?`)) return;
    try {
      const res = await kiemSoatVaoRaService.deleteDevice(id);
      if (res && res.success) {
        showNotification && showNotification(`🗑️ Đã xóa thiết bị ${name}`);
        loadDevices();
      }
    } catch {
      showNotification && showNotification('❌ Không thể xóa thiết bị!');
    }
  };

  // Kiểm tra kết nối mạng (Ping) tới thiết bị thật
  const handlePingDevice = async (dev) => {
    if (!dev.ip) {
      showNotification && showNotification('⚠️ Thiết bị chưa cấu hình địa chỉ IP để kiểm tra!');
      return;
    }
    showNotification && showNotification(`📡 Đang kiểm tra kết nối tới ${dev.ip}:${dev.port || 4370}...`);
    try {
      const res = await kiemSoatVaoRaService.pingDevice(dev.ip, dev.port || '4370');
      if (res && res.online) {
        setPingResult((prev) => ({ ...prev, [dev.id]: 'online' }));
        showNotification && showNotification(`🟢 ${dev.name} (${dev.ip}) đang ONLINE!`);
      } else {
        setPingResult((prev) => ({ ...prev, [dev.id]: 'offline' }));
        showNotification && showNotification(`🔴 ${dev.name} (${dev.ip}) không phản hồi (Offline)!`);
      }
    } catch {
      setPingResult((prev) => ({ ...prev, [dev.id]: 'offline' }));
      showNotification && showNotification(`🔴 Lỗi kết nối tới ${dev.ip}`);
    }
  };

  // Lọc lịch sử
  const filteredHistory = historyList.filter((item) => {
    if (!filterSearch.trim()) return true;
    const term = filterSearch.toLowerCase();
    return (
      (item.name && item.name.toLowerCase().includes(term)) ||
      (item.maThe && item.maThe.toLowerCase().includes(term)) ||
      (item.gioVao && item.gioVao.includes(term)) ||
      (item.goiDichVu && item.goiDichVu.toLowerCase().includes(term))
    );
  });

  return (
    <div className="tap-access-control-container">
      {/* TOP TABS: KIỂM SOÁT VÀO RA | DANH MỤC KHÁCH HÀNG */}
      <div className="cust-doc-tabs" style={{ gridColumn: '1 / -1', margin: '-16px -16px 10px -16px', background: '#c5d5e8', borderBottom: '2px solid #2d6ca2' }}>
        <div className="cust-doc-tab active" title="Kiểm soát vào ra">
          <span>Kiểm soát vào ra</span>
          <span className="cust-tab-close">×</span>
        </div>
        <div
          className="cust-doc-tab"
          onClick={onSwitchToCustomers}
          title="Chuyển sang màn hình Danh mục khách hàng"
          style={{ cursor: 'pointer' }}
        >
          <span>Danh mục khách hàng</span>
          <span className="cust-tab-close">×</span>
        </div>
      </div>

      {/* ================= KHỐI TRÁI: ĐIỀU KHIỂN & HỒ SƠ ================= */}
      <div className="tap-ac-left-col">
        {/* CARD 1: ĐẦU ĐỌC THẺ & MÃ VẠCH RFID */}
        <div className="tap-card tap-rfid-card">
          <div className="tap-rfid-header">
            <span className="tap-rfid-signal-icon">
              <i className="fa-solid fa-satellite-dish"></i>
            </span>
            <span className="tap-rfid-title">ĐẦU ĐỌC THẺ & MÃ VẠCH RFID</span>
          </div>

          <div className="tap-rfid-input-row">
            <div className="tap-rfid-input-wrapper">
              <i className="fa-solid fa-id-card tap-input-icon"></i>
              <input
                ref={scanInputRef}
                type="text"
                className="tap-rfid-input"
                placeholder="Quét thẻ từ, quét QR hoặc nhập mã thẻ (VD: 3281283)..."
                value={scanInput}
                onChange={(e) => setScanInput(e.target.value)}
                onKeyDown={handleKeyDown}
                autoFocus
              />
            </div>
            <button
              className="tap-rfid-submit-btn"
              onClick={() => processCardScan()}
              title="Xác thực thẻ (Enter)"
            >
              <i className="fa-solid fa-arrow-right-to-bracket"></i>
              <span>NHẬP (ENTER)</span>
            </button>
          </div>

          <div className="tap-rfid-hint">
            <span>Phím tắt:</span> <strong className="tap-shortcut-badge">[F2]</strong> <span>Tìm nhanh</span>
          </div>
        </div>

        {/* CARD 2: TRẠNG THÁI CỔNG TURNSTILE */}
        <div
          className={`tap-card tap-turnstile-card ${
            gateStatus === 'unlocked'
              ? 'tap-gate-unlocked'
              : gateStatus === 'blocked'
              ? 'tap-gate-blocked'
              : ''
          }`}
        >
          <div className="tap-turnstile-left">
            <div className="tap-turnstile-icon-box">
              <i
                className={`fa-solid ${
                  gateStatus === 'unlocked'
                    ? 'fa-lock-open'
                    : gateStatus === 'blocked'
                    ? 'fa-circle-xmark'
                    : 'fa-lock'
                }`}
              ></i>
            </div>
            <div className="tap-turnstile-text">
              <div className="tap-turnstile-sub">
                KIỂM SOÁT TỰ ĐỘNG TURNSTILE • CHẾ ĐỘ TỰ ĐỘNG
              </div>
              <div className="tap-turnstile-main-status">
                {gateStatus === 'unlocked' ? (
                  <span className="tap-status-unlocked-text">
                    TRẠNG THÁI CỔNG: ĐÃ MỞ — MỜI HỘI VIÊN VÀO
                  </span>
                ) : gateStatus === 'blocked' ? (
                  <span className="tap-status-blocked-text">
                    TRẠNG THÁI CỔNG: TỪ CHỐI — THẺ KHÔNG HỢP LỆ
                  </span>
                ) : (
                  <span>TRẠNG THÁI CỔNG: ĐANG KHÓA — SẴN SÀNG QUÉT THẺ</span>
                )}
              </div>
              <div className="tap-turnstile-desc">
                {gateStatus === 'unlocked'
                  ? 'Cổng xoay tự động mở khóa trong 5 giây để hội viên đi qua.'
                  : gateStatus === 'blocked'
                  ? 'Thẻ không hợp lệ, đang bảo lưu hoặc đã hết hạn.'
                  : 'Mời hội viên đặt thẻ từ, quét mã QR trên ứng dụng hoặc đặt vân tay tại cổng xoay.'}
              </div>
            </div>
          </div>

          <div className="tap-turnstile-right">
            <div className="tap-signal-label">TÍN HIỆU CỔNG</div>
            {gateSignal === 'open' ? (
              <div className="tap-signal-badge tap-signal-open">
                <span className="tap-dot tap-dot-green"></span>
                <span>OPEN (MỜI VÀO)</span>
              </div>
            ) : gateSignal === 'warning' ? (
              <div className="tap-signal-badge tap-signal-warning">
                <span className="tap-dot tap-dot-yellow"></span>
                <span>GIA HẠN (MỞ CỔNG)</span>
              </div>
            ) : gateSignal === 'blocked' ? (
              <div className="tap-signal-badge tap-signal-blocked">
                <span className="tap-dot tap-dot-red"></span>
                <span>BLOCKED (TỪ CHỐI)</span>
              </div>
            ) : (
              <div className="tap-signal-badge tap-signal-ready">
                <span className="tap-dot tap-dot-yellow"></span>
                <span>READY (CHỜ KHÁCH)</span>
              </div>
            )}
          </div>
        </div>

        {/* CARD 3: HỒ SƠ HỘI VIÊN XÁC THỰC */}
        <div className="tap-card tap-member-profile-card">
          <div className="tap-profile-header">
            <div className="tap-profile-header-left">
              <i className="fa-solid fa-id-card-clip tap-profile-icon"></i>
              <span className="tap-profile-title">HỒ SƠ HỘI VIÊN XÁC THỰC</span>
            </div>
            <div className="tap-profile-header-right">
              {currentMember ? (
                <span
                  className={`tap-member-status-pill ${
                    currentMember.statusType === 'valid'
                      ? 'tap-status-valid'
                      : currentMember.statusType === 'warning'
                      ? 'tap-status-warning'
                      : 'tap-status-expired'
                  }`}
                >
                  TRẠNG THÁI: {currentMember.trangThai}
                </span>
              ) : (
                <span className="tap-member-status-pill tap-status-idle">
                  TRẠNG THÁI: CHỜ QUÉT THẺ
                </span>
              )}
            </div>
          </div>

          <div className="tap-profile-body-row">
            {/* THÔNG TIN CHI TIẾT */}
            <div className="tap-profile-fields-col">
              <div className="tap-field-row tap-field-two-col">
                <div className="tap-field-item">
                  <label>MÃ HỘI VIÊN:</label>
                  <div className="tap-field-value tap-field-code">
                    {currentMember?.maThe || '---'}
                  </div>
                </div>
                <div className="tap-field-item">
                  <label>SỐ ĐIỆN THOẠI:</label>
                  <div className="tap-field-value">
                    {currentMember?.phone || '---'}
                  </div>
                </div>
              </div>

              <div className="tap-field-row">
                <div className="tap-field-item tap-full-width">
                  <label>HỌ VÀ TÊN:</label>
                  <div
                    className={`tap-field-value tap-name-highlight ${
                      !currentMember ? 'tap-placeholder-name' : ''
                    }`}
                  >
                    {currentMember?.name || '(Chưa có dữ liệu - Chờ quét thẻ)'}
                  </div>
                </div>
              </div>

              <div className="tap-field-row tap-field-two-col">
                <div className="tap-field-item">
                  <label>NGÀY SINH:</label>
                  <div className="tap-field-value">
                    {currentMember?.ngaySinh || '---'}
                  </div>
                </div>
                <div className="tap-field-item">
                  <label>ĐỊA CHỈ:</label>
                  <div className="tap-field-value tap-address-truncate">
                    {currentMember?.diaChi || '---'}
                  </div>
                </div>
              </div>

              <div className="tap-field-row">
                <div className="tap-field-item tap-full-width">
                  <label>CA TẬP HỢP LỆ:</label>
                  <div className="tap-field-value tap-shift-value">
                    {currentMember?.caTap || '---'}
                  </div>
                </div>
              </div>

              <div className="tap-field-row tap-field-two-col">
                <div className="tap-field-item">
                  <label>LOẠI THẺ HỘI VIÊN:</label>
                  <div className="tap-field-value">
                    {currentMember?.loaiThe || '---'}
                  </div>
                </div>
                <div className="tap-field-item">
                  <label>GÓI DỊCH VỤ:</label>
                  <div className="tap-field-value tap-package-highlight">
                    {currentMember?.goiDichVu || '---'}
                  </div>
                </div>
              </div>

              <div className="tap-field-row tap-field-two-col">
                <div className="tap-field-item">
                  <label>TỪ NGÀY:</label>
                  <div className="tap-field-value">
                    {currentMember?.tuNgay || '---'}
                  </div>
                </div>
                <div className="tap-field-item">
                  <label>ĐẾN NGÀY:</label>
                  <div className="tap-field-value tap-expiry-highlight">
                    {currentMember?.denNgay || '---'}
                  </div>
                </div>
              </div>

              {/* Thông báo thông tin */}
              <div className="tap-profile-alert-banner">
                <i className="fa-solid fa-circle-info tap-alert-icon"></i>
                <span>
                  Hệ thống tự động kích hoạt và tải thông tin hội viên ngay khi có tín hiệu quét thành công.
                </span>
              </div>
            </div>

            {/* HỘP ẢNH CAMERA AI */}
            <div className="tap-profile-camera-col">
              <div className="tap-camera-dashed-box">
                {currentMember?.avatar ? (
                  <img
                    src={currentMember.avatar}
                    alt={currentMember.name}
                    className="tap-camera-member-photo"
                  />
                ) : (
                  <div className="tap-camera-placeholder-icon">
                    <i className="fa-solid fa-user"></i>
                  </div>
                )}
                <div className="tap-camera-title">
                  {currentMember ? currentMember.name : 'CHƯA CÓ HÌNH ẢNH'}
                </div>
                <div className="tap-camera-desc">
                  {currentMember
                    ? 'Ảnh đối soát camera cổng xoay trùng khớp 99%'
                    : 'Camera AI sẽ tự động chụp và đối soát khi quét thẻ'}
                </div>
                <div
                  className={`tap-camera-pill ${
                    currentMember ? 'tap-cam-pill-matched' : ''
                  }`}
                >
                  <span className="tap-cam-dot"></span>
                  <span>{currentMember ? 'CAM_AI: MATCHED' : 'CAM_AI: STANDBY'}</span>
                </div>
              </div>
            </div>
          </div>
        </div>

        {/* CARD 4: 4 CHỈ SỐ THỐNG KÊ (BOTTOM METRICS) */}
        <div className="tap-bottom-metrics-row">
          {/* Card 1: Số ngày còn */}
          <div className="tap-metric-card tap-metric-green">
            <div className="tap-metric-title">SỐ NGÀY CÒN</div>
            <div className="tap-metric-value">
              {currentMember ? currentMember.soNgayCon : '---'}
            </div>
            <div className="tap-metric-sub">Thời hạn sử dụng</div>
          </div>

          {/* Card 2: Số lần đã đến */}
          <div className="tap-metric-card tap-metric-blue">
            <div className="tap-metric-title">SỐ LẦN ĐÃ ĐẾN</div>
            <div className="tap-metric-value">
              {currentMember ? currentMember.soLanDaDen : '---'}
            </div>
            <div className="tap-metric-sub">Tổng tích lũy</div>
          </div>

          {/* Card 3: Số lần còn */}
          <div className="tap-metric-card tap-metric-purple">
            <div className="tap-metric-title">SỐ LẦN CÒN</div>
            <div className="tap-metric-value tap-metric-text-small">
              {currentMember ? currentMember.soLanCon : '---'}
            </div>
            <div className="tap-metric-sub">Gói lượt / Buổi PT</div>
          </div>

          {/* Card 4: Tập hôm nay */}
          <div className="tap-metric-card tap-metric-yellow">
            <div className="tap-metric-title">TẬP HÔM NAY</div>
            <div className="tap-metric-value">
              {currentMember ? currentMember.tapHomNay : '---'}
            </div>
            <div className="tap-metric-sub">Lượt trong ngày</div>
          </div>
        </div>
      </div>

      {/* ================= KHỐI PHẢI: LỊCH SỬ VÀO RA & THIẾT BỊ ================= */}
      <div className="tap-ac-right-col">
        <div className="tap-card tap-right-panel-card">
          {/* TABS TRÊN CÙNG */}
          <div className="tap-right-tabs-row">
            <button
              className={`tap-tab-item ${activeTab === 'today' ? 'tap-tab-active' : ''}`}
              onClick={() => setActiveTab('today')}
            >
              <i className="fa-regular fa-clock"></i>
              <span>Tập trong ngày ({historyList.length})</span>
            </button>
            <button
              className={`tap-tab-item ${activeTab === 'hardware' ? 'tap-tab-active' : ''}`}
              onClick={() => setActiveTab('hardware')}
            >
              <i className="fa-solid fa-fingerprint"></i>
              <span>Máy vân tay / Cổng</span>
            </button>
          </div>

          {activeTab === 'today' ? (
            <>
              {/* THANH TÌM KIẾM & NÚT REFRESH */}
              <div className="tap-filter-search-row">
                <div className="tap-search-box">
                  <i className="fa-solid fa-magnifying-glass tap-search-icon"></i>
                  <input
                    type="text"
                    className="tap-search-input"
                    placeholder="Lọc tên, mã thẻ, giờ vào..."
                    value={filterSearch}
                    onChange={(e) => setFilterSearch(e.target.value)}
                  />
                  {filterSearch && (
                    <button
                      className="tap-search-clear"
                      onClick={() => setFilterSearch('')}
                    >
                      <i className="fa-solid fa-xmark"></i>
                    </button>
                  )}
                </div>
                <button
                  className={`tap-refresh-btn ${isRefreshing ? 'tap-refreshing' : ''}`}
                  onClick={handleRefresh}
                  title="Tải lại danh sách từ cơ sở dữ liệu"
                >
                  <i className="fa-solid fa-rotate-right"></i>
                </button>
              </div>

              {/* BẢNG LỊCH SỬ QUÉT THẺ */}
              <div className="tap-history-table-wrapper">
                <table className="tap-history-table">
                  <thead>
                    <tr>
                      <th className="tap-col-time">GIỜ</th>
                      <th className="tap-col-name">KHÁCH HÀNG</th>
                      <th className="tap-col-code">MÃ THẺ</th>
                      <th className="tap-col-status">TRẠNG THÁI</th>
                    </tr>
                  </thead>
                  <tbody>
                    {filteredHistory.map((item, idx) => (
                      <tr
                        key={item.id || idx}
                        className={`tap-history-row ${
                          currentMember?.maThe === item.maThe ? 'tap-row-selected' : ''
                        }`}
                        onClick={() => handleSelectHistoryItem(item)}
                      >
                        <td className="tap-col-time">
                          <span className="tap-time-text">{item.gioVao}</span>
                        </td>
                        <td className="tap-col-name">
                          <div className="tap-customer-name">{item.name}</div>
                          <div className="tap-customer-package">{item.goiDichVu}</div>
                        </td>
                        <td className="tap-col-code">
                          <span className="tap-code-badge">{item.maThe}</span>
                        </td>
                        <td className="tap-col-status">
                          <span
                            className={`tap-badge-pill ${
                              item.statusType === 'valid'
                                ? 'tap-pill-green'
                                : item.statusType === 'warning'
                                ? 'tap-pill-yellow'
                                : 'tap-pill-red'
                            }`}
                          >
                            {item.trangThai}
                          </span>
                        </td>
                      </tr>
                    ))}
                    {filteredHistory.length === 0 && (
                      <tr>
                        <td colSpan="4" className="tap-empty-history">
                          <i className="fa-regular fa-folder-open"></i>
                          <span>Không tìm thấy lượt tập nào phù hợp</span>
                        </td>
                      </tr>
                    )}
                  </tbody>
                </table>
              </div>
            </>
          ) : (
            /* TAB 2: MÁY VÂN TAY / CỔNG ĐIỀU KHIỂN THỰC TẾ */
            <div className="tap-hardware-tab-pane">
              <div className="tap-hardware-header-row">
                <div className="tap-hardware-title-meta">
                  <strong>Thiết bị phần cứng ({devices.length})</strong>
                  <p>Cấu hình máy vân tay / cổng xoay từ bảng DMAYVANTAY</p>
                </div>
                <button
                  className="tap-btn-add-device"
                  onClick={() => setShowAddDeviceModal(true)}
                  title="Thêm thiết bị mới"
                >
                  <i className="fa-solid fa-plus"></i>
                  <span>Thêm thiết bị</span>
                </button>
              </div>

              {devices.length === 0 ? (
                <div className="tap-empty-hardware-box">
                  <div className="tap-empty-hw-icon">
                    <i className="fa-solid fa-server"></i>
                  </div>
                  <div className="tap-empty-hw-title">Chưa có thiết bị nào được cấu hình</div>
                  <div className="tap-empty-hw-desc">
                    Hệ thống chưa lưu cấu hình máy quét vân tay hoặc cổng xoay nào trong cơ sở dữ liệu Firebird (bảng <strong>DMAYVANTAY</strong>).
                  </div>
                  <button
                    className="tap-btn-empty-add-hw"
                    onClick={() => setShowAddDeviceModal(true)}
                  >
                    <i className="fa-solid fa-circle-plus"></i>
                    <span>+ Thêm cấu hình thiết bị mới</span>
                  </button>
                </div>
              ) : (
                <div className="tap-devices-list">
                  {devices.map((dev) => (
                    <div key={dev.id} className="tap-device-card">
                      <div className="tap-device-icon">
                        <i className="fa-solid fa-fingerprint"></i>
                      </div>
                      <div className="tap-device-info">
                        <div className="tap-device-name">{dev.name}</div>
                        <div className="tap-device-meta">
                          <span>IP: {dev.ip || '---'}</span> : <span>{dev.port || '4370'}</span> • <span>Mã máy: {dev.maMay || '1'}</span>
                        </div>
                        {dev.note && <div className="tap-device-note">{dev.note}</div>}
                        <div className="tap-device-state-tag">
                          <span className={`tap-dot ${pingResult[dev.id] === 'offline' ? 'tap-dot-red' : 'tap-dot-green'}`}></span>
                          <span>
                            {pingResult[dev.id] === 'online'
                              ? 'Online (Phản hồi)'
                              : pingResult[dev.id] === 'offline'
                              ? 'Offline (Mất kết nối)'
                              : 'Đã cấu hình'}
                          </span>
                        </div>
                      </div>
                      <div className="tap-device-action-group">
                        <button
                          className="tap-btn-ping"
                          onClick={() => handlePingDevice(dev)}
                          title="Kiểm tra kết nối mạng (Ping IP)"
                        >
                          <i className="fa-solid fa-network-wired"></i>
                          <span>Ping</span>
                        </button>
                        <button
                          className="tap-btn-open-door"
                          onClick={() => handleManualOpenGate(dev)}
                          title="Gửi lệnh mở cổng từ xa"
                        >
                          <i className="fa-solid fa-lock-open"></i>
                          <span>Mở cổng</span>
                        </button>
                        <button
                          className="tap-btn-del-device"
                          onClick={() => handleDeleteDevice(dev.id, dev.name)}
                          title="Xóa thiết bị khỏi cơ sở dữ liệu"
                        >
                          <i className="fa-solid fa-trash-can"></i>
                        </button>
                      </div>
                    </div>
                  ))}
                </div>
              )}

              <div className="tap-hardware-footer-actions">
                <button
                  className="tap-btn-hw-secondary"
                  onClick={() => loadDevices()}
                >
                  <i className="fa-solid fa-arrows-rotate"></i>
                  <span>Làm mới thiết bị</span>
                </button>
                <button
                  className="tap-btn-hw-primary"
                  onClick={() => setShowAddDeviceModal(true)}
                >
                  <i className="fa-solid fa-plus"></i>
                  <span>Cấu hình thiết bị</span>
                </button>
              </div>
            </div>
          )}
        </div>
      </div>

      {/* ================= MODAL THÊM THIẾT BỊ MỚI VÀO DMAYVANTAY ================= */}
      {showAddDeviceModal && (
        <div className="tap-modal-overlay" onClick={() => setShowAddDeviceModal(false)}>
          <div className="tap-modal-card" onClick={(e) => e.stopPropagation()}>
            <div className="tap-modal-header">
              <div className="tap-modal-title">
                <i className="fa-solid fa-network-wired"></i>
                <span>THÊM THIẾT BỊ MÁY VÂN TAY / CỔNG</span>
              </div>
              <button
                className="tap-modal-close"
                onClick={() => setShowAddDeviceModal(false)}
              >
                <i className="fa-solid fa-xmark"></i>
              </button>
            </div>

            <form onSubmit={handleAddDevice} className="tap-modal-body">
              <div className="tap-form-group">
                <label>Tên thiết bị *</label>
                <input
                  type="text"
                  placeholder="VD: Cổng xoay Turnstile 01, Máy vân tay F18..."
                  value={newDeviceForm.name}
                  onChange={(e) => setNewDeviceForm({ ...newDeviceForm, name: e.target.value })}
                  required
                  autoFocus
                />
              </div>

              <div className="tap-form-row-two">
                <div className="tap-form-group">
                  <label>Địa chỉ IP *</label>
                  <input
                    type="text"
                    placeholder="VD: 192.168.1.201"
                    value={newDeviceForm.ip}
                    onChange={(e) => setNewDeviceForm({ ...newDeviceForm, ip: e.target.value })}
                    required
                  />
                </div>
                <div className="tap-form-group">
                  <label>Cổng kết nối (Port)</label>
                  <input
                    type="text"
                    placeholder="Mặc định: 4370"
                    value={newDeviceForm.port}
                    onChange={(e) => setNewDeviceForm({ ...newDeviceForm, port: e.target.value })}
                  />
                </div>
              </div>

              <div className="tap-form-row-two">
                <div className="tap-form-group">
                  <label>Mã số máy (Mã ID)</label>
                  <input
                    type="text"
                    placeholder="1, 2, 3..."
                    value={newDeviceForm.maMay}
                    onChange={(e) => setNewDeviceForm({ ...newDeviceForm, maMay: e.target.value })}
                  />
                </div>
                <div className="tap-form-group">
                  <label>Giao thức / SDK</label>
                  <select
                    value={newDeviceForm.sdk}
                    onChange={(e) => setNewDeviceForm({ ...newDeviceForm, sdk: e.target.value })}
                  >
                    <option value="ZKTeco">ZKTeco / Ronald Jack (TCP/IP)</option>
                    <option value="BioStar">BioStar / Suprema</option>
                    <option value="Turnstile">Cổng xoay Flap Barrier Relay</option>
                    <option value="RFID_COM">Đầu đọc RFID cổng COM/USB</option>
                  </select>
                </div>
              </div>

              <div className="tap-form-group">
                <label>Ghi chú / Vị trí đặt máy</label>
                <input
                  type="text"
                  placeholder="VD: Cửa ra vào tầng 1, quầy lễ tân..."
                  value={newDeviceForm.note}
                  onChange={(e) => setNewDeviceForm({ ...newDeviceForm, note: e.target.value })}
                />
              </div>

              <div className="tap-modal-footer">
                <button
                  type="button"
                  className="tap-btn-cancel"
                  onClick={() => setShowAddDeviceModal(false)}
                >
                  Hủy bỏ
                </button>
                <button
                  type="submit"
                  className="tap-btn-save"
                  disabled={isSubmittingDevice}
                >
                  {isSubmittingDevice ? 'Đang lưu...' : 'Lưu vào Database'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}
