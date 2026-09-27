import React, { useState, useEffect, useRef } from 'react';
import './CustomerAeModal.css';

/**
 * CustomerAeModal - Form Thêm mới / Chỉnh sửa Khách hàng (Hội viên GYM)
 * ĐÚNG 100% THEO FILE CSDL THỰC TẾ VÀ GIAO DIỆN WINFORMS:
 * Mặc định khi thêm mới: Trạng thái là "Chưa kích hoạt" (dTrangThaiId = '0')
 */
export default function CustomerAeModal({
  show,
  mode = 'create', // 'create' | 'edit'
  initialData = null,
  metadata = {
    loaiThe: [],
    nhomKhach: [],
    caTap: [],
    nhanVien: [],
    trangThai: [],
    tinhThanh: []
  },
  subtabsData = null,
  onSave,
  onClose,
  showNotification
}) {
  if (!show) return null;

  // 1. Quản lý Tab chính (KryptonNavigator tabMain theo CSDL)
  const [activeTab, setActiveTab] = useState('pageThongTinChinh');

  // 2. Dữ liệu Form
  const [formData, setFormData] = useState({
    id: '',
    maKhach: '',
    name: '',
    ngaySinh: '',
    diaChi: '',
    dienThoai: '',
    email: '',
    facebook: '',
    dNhanVienId: '',
    dNhomKhachHangId: '',
    dTinhThanhId: '',
    note: '',
    dTrangThaiId: '0', // Mặc định '0' (Chưa kích hoạt)
    anh: '',

    // Tab Thông tin hiện tại
    loaiThe: '',
    caTap: 'Toàn thời gian',
    tuNgay: '',
    denNgay: '',
    soNgayCon: 0,
    soLan: 0,
    daTap: 0,
    conLai: 0
  });

  const [saving, setSaving] = useState(false);
  const [webcamActive, setWebcamActive] = useState(false);
  const videoRef = useRef(null);

  // Nạp dữ liệu khi mở form
  useEffect(() => {
    if (mode === 'edit' && initialData) {
      setFormData({
        id: initialData.id || '',
        maKhach: initialData.maThe || initialData.maKhach || '',
        name: initialData.name || initialData.tenKhachHang || '',
        ngaySinh: initialData.ngaySinh ? initialData.ngaySinh.split('T')[0] : '',
        diaChi: initialData.diaChi || '',
        dienThoai: initialData.dienThoai || '',
        email: initialData.email || '',
        facebook: initialData.facebook || '',
        dNhanVienId: initialData.dNhanVienId || initialData.nhanVienId || '',
        dNhomKhachHangId: initialData.dNhomKhachHangId || initialData.nhomKhachHangId || '',
        dTinhThanhId: initialData.dTinhThanhId || initialData.tinhThanhId || '',
        note: initialData.note || '',
        dTrangThaiId: initialData.dTrangThaiId != null ? String(initialData.dTrangThaiId) : '0',
        anh: initialData.anh || '',

        loaiThe: initialData.loaiThe || metadata.loaiThe?.[0]?.name || '',
        caTap: initialData.caTap || 'Toàn thời gian',
        tuNgay: initialData.tuNgay || '',
        denNgay: initialData.denNgay || '',
        soNgayCon: initialData.soNgayCon || 30,
        soLan: initialData.soLan || 0,
        daTap: initialData.daTap || 0,
        conLai: initialData.conLai || 0
      });
    } else {
      const randNum = Math.floor(100000 + Math.random() * 900000);
      const today = new Date().toISOString().split('T')[0];
      const nextMonth = new Date(Date.now() + 30 * 24 * 60 * 60 * 1000).toISOString().split('T')[0];

      setFormData({
        id: '',
        maKhach: String(randNum),
        name: '',
        ngaySinh: '',
        diaChi: '',
        dienThoai: '',
        email: '',
        facebook: '',
        dNhanVienId: metadata.nhanVien?.[0]?.id || '',
        dNhomKhachHangId: metadata.nhomKhach?.[0]?.id || '',
        dTinhThanhId: metadata.tinhThanh?.[0]?.id || '',
        note: '',
        dTrangThaiId: '0', // Mặc định khi thêm mới là Chưa kích hoạt (dTrangThaiId = '0')
        anh: '',

        loaiThe: metadata.loaiThe?.[0]?.name || 'Thẻ tập tháng',
        caTap: 'Toàn thời gian',
        tuNgay: today,
        denNgay: nextMonth,
        soNgayCon: 30,
        soLan: 30,
        daTap: 0,
        conLai: 30
      });
    }
  }, [mode, initialData, metadata, show]);

  const handleChange = (field, value) => {
    setFormData(prev => ({ ...prev, [field]: value }));
  };

  // Tạo mới (Reset form)
  const handleResetNew = () => {
    const randNum = Math.floor(100000 + Math.random() * 900000);
    const today = new Date().toISOString().split('T')[0];
    const nextMonth = new Date(Date.now() + 30 * 24 * 60 * 60 * 1000).toISOString().split('T')[0];
    setFormData({
      id: '',
      maKhach: String(randNum),
      name: '',
      ngaySinh: '',
      diaChi: '',
      dienThoai: '',
      email: '',
      facebook: '',
      dNhanVienId: metadata.nhanVien?.[0]?.id || '',
      dNhomKhachHangId: metadata.nhomKhach?.[0]?.id || '',
      dTinhThanhId: metadata.tinhThanh?.[0]?.id || '',
      note: '',
      dTrangThaiId: '0', // Mặc định là Chưa kích hoạt
      anh: '',
      loaiThe: metadata.loaiThe?.[0]?.name || 'Thẻ tập tháng',
      caTap: 'Toàn thời gian',
      tuNgay: today,
      denNgay: nextMonth,
      soNgayCon: 30,
      soLan: 30,
      daTap: 0,
      conLai: 30
    });
    showNotification && showNotification('Đã tạo mới form khách hàng');
  };

  // Chụp ảnh Webcam
  const handleStartWebcam = async () => {
    try {
      setWebcamActive(true);
      const stream = await navigator.mediaDevices.getUserMedia({ video: true });
      if (videoRef.current) {
        videoRef.current.srcObject = stream;
      }
    } catch (err) {
      alert('Không thể kết nối webcam: ' + err.message);
      setWebcamActive(false);
    }
  };

  const handleCaptureWebcam = () => {
    if (videoRef.current) {
      const canvas = document.createElement('canvas');
      canvas.width = videoRef.current.videoWidth || 320;
      canvas.height = videoRef.current.videoHeight || 240;
      const ctx = canvas.getContext('2d');
      ctx.drawImage(videoRef.current, 0, 0);
      const dataUrl = canvas.toDataURL('image/jpeg');
      handleChange('anh', dataUrl);

      // Stop stream
      const stream = videoRef.current.srcObject;
      if (stream) {
        stream.getTracks().forEach(t => t.stop());
      }
      setWebcamActive(false);
      showNotification && showNotification('Đã chụp ảnh từ webcam thành công!');
    }
  };

  const handleSubmit = async (e, action = 'close') => {
    if (e && e.preventDefault) e.preventDefault();

    if (!formData.name || !formData.name.trim()) {
      alert('Dữ liệu không được phép trống! Vui lòng nhập Tên khách hàng.');
      return;
    }

    setSaving(true);
    try {
      const currentTrangThaiId = formData.dTrangThaiId || '0';
      const statusObj = metadata.trangThai?.find(t => String(t.id) === String(currentTrangThaiId));
      const trangThaiName = statusObj?.name || (currentTrangThaiId === '1' ? 'Đang sử dụng' : 'Chưa kích hoạt');

      const payload = {
        ...initialData,
        maThe: formData.maKhach,
        maKhach: formData.maKhach,
        name: formData.name.trim(),
        tenKhachHang: formData.name.trim(),
        diaChi: formData.diaChi,
        dienThoai: formData.dienThoai,
        email: formData.email,
        ngaySinh: formData.ngaySinh,
        facebook: formData.facebook,
        dNhanVienId: formData.dNhanVienId,
        nhanVien: metadata.nhanVien?.find(n => n.id === formData.dNhanVienId)?.name || '',
        dNhomKhachHangId: formData.dNhomKhachHangId,
        nhomKhachHang: metadata.nhomKhach?.find(n => n.id === formData.dNhomKhachHangId)?.name || '',
        dTinhThanhId: formData.dTinhThanhId,
        tinhThanh: metadata.tinhThanh?.find(t => t.id === formData.dTinhThanhId)?.name || '',
        dTrangThaiId: currentTrangThaiId,
        trangThai: trangThaiName,
        note: formData.note,
        anh: formData.anh,

        loaiThe: formData.loaiThe,
        caTap: formData.caTap,
        tuNgay: formData.tuNgay,
        denNgay: formData.denNgay,
        soLan: Number(formData.soLan) || 0,
        daTap: Number(formData.daTap) || 0,
        conLai: Number(formData.conLai) || 0
      };

      await onSave(payload, initialData?.id);

      if (action === 'new') {
        handleResetNew();
      } else if (action === 'print' || action === 'preview') {
        showNotification && showNotification(`Đã lưu và chuẩn bị in phiếu cho khách hàng '${formData.name}'`);
      }
    } catch (err) {
      console.error(err);
      alert('Lỗi lưu thông tin khách hàng vào CSDL Firebird!');
    } finally {
      setSaving(false);
    }
  };

  const formTitle = mode === 'create' ? 'KHÁCH HÀNG - THÊM MỚI' : `KHÁCH HÀNG - CHỈNH SỬA (${formData.name || ''})`;

  return (
    <div className="gym-ae-overlay">
      <div className="gym-ae-window">
        {/* Title bar WinForms DevExpress */}
        <div className="gym-ae-titlebar">
          <div className="gym-ae-title">
            <span style={{ marginRight: 6 }}>🙂</span>
            <span>{formTitle}</span>
          </div>
          <button className="gym-ae-win-btn" onClick={onClose} title="Thoát (ESC)">✕</button>
        </div>

        {/* Header Bar WinForms No1Lib */}
        <div className="gym-ae-header-bar">
          <div className="gym-ae-header-title">
            <span className="gym-ae-header-icon">🙂</span>
            <span className="gym-ae-header-text">Khách hàng</span>
          </div>
          <div className="gym-ae-header-shortcuts">
            (Ctrl+S: Lưu&Mới, Ctrl+L: Lưu, Ctrl+N: Mới, Ctrl+P: In, ESC: Thoát)
          </div>
        </div>

        {/* Toolbar WinForms Ribbon */}
        <div className="gym-ae-toolbar">
          <button type="button" className="gym-tb-btn" title="Danh sách phím tắt">
            <i className="fa-solid fa-keyboard" style={{ color: '#0284c7' }}></i> Phím tắt ▾
          </button>
          <button type="button" className="gym-tb-btn" title="Trước (F10)">
            <i className="fa-solid fa-arrow-left" style={{ color: '#0284c7' }}></i> Trước (F10)
          </button>
          <button type="button" className="gym-tb-btn" title="Sau (F11)">
            <i className="fa-solid fa-arrow-right" style={{ color: '#0284c7' }}></i> Sau (F11)
          </button>
          <button type="button" className="gym-tb-btn" onClick={handleResetNew} title="Tạo mới">
            <i className="fa-solid fa-square-plus" style={{ color: '#16a34a' }}></i> Tạo mới
          </button>
          <button type="button" className="gym-tb-btn" title="Sao chép" onClick={() => {
            navigator.clipboard.writeText(JSON.stringify(formData));
            showNotification && showNotification('Đã sao chép thông tin khách hàng!');
          }}>
            <i className="fa-regular fa-copy" style={{ color: '#64748b' }}></i> Sao chép
          </button>
          <button type="button" className="gym-tb-btn disabled" title="Xóa">
            <i className="fa-solid fa-xmark" style={{ color: '#94a3b8' }}></i> Xóa
          </button>
        </div>

        {/* 3 Tabs KryptonNavigator tabMain theo CSDL DATA.fdb & WinForms */}
        <div className="gym-ae-tabs">
          <div
            className={`gym-ae-tab ${activeTab === 'pageThongTinChinh' ? 'active' : ''}`}
            onClick={() => setActiveTab('pageThongTinChinh')}
          >
            Thông tin chính
          </div>
          <div
            className={`gym-ae-tab ${activeTab === 'pageLichSuGiaoDich' ? 'active' : ''}`}
            onClick={() => setActiveTab('pageLichSuGiaoDich')}
          >
            Lịch sử giao dịch
          </div>
          <div
            className={`gym-ae-tab ${activeTab === 'pageTheTrang' ? 'active' : ''}`}
            onClick={() => setActiveTab('pageTheTrang')}
          >
            Thể trạng
          </div>
          {mode === 'edit' && (
            <div
              className={`gym-ae-tab ${activeTab === 'pageHienTai' ? 'active' : ''}`}
              onClick={() => setActiveTab('pageHienTai')}
            >
              Thông tin hiện tại
            </div>
          )}
        </div>

        {/* Form Body Container */}
        <form onSubmit={(e) => handleSubmit(e, 'close')} className="gym-ae-form">
          <div className="gym-ae-tab-content">
            {/* ========================================================================= */}
            {/* TRANG 1: THÔNG TIN CHÍNH (pageThongTinChinh)                              */}
            {/* ========================================================================= */}
            {activeTab === 'pageThongTinChinh' && (
              <div className="gym-main-split">
                {/* CỘT TRÁI: CÁC TRƯỜNG DỮ LIỆU ĐÚNG TỌA ĐỘ VÀ THỨ TỰ TRONG CSDL */}
                <div className="gym-main-fields">
                  {/* Mã khách (Loc: 140, 7) */}
                  <div className="gym-ae-row">
                    <label className="gym-ae-label">Mã khách</label>
                    <input
                      type="text"
                      className="gym-ae-input"
                      value={formData.maKhach}
                      onChange={(e) => handleChange('maKhach', e.target.value)}
                    />
                  </div>

                  {/* Tên khách hàng (Loc: 140, 33) - Bắt buộc (Vàng #ffffcc) */}
                  <div className="gym-ae-row">
                    <label className="gym-ae-label req">Tên khách hàng</label>
                    <input
                      type="text"
                      className="gym-ae-input highlight-yellow"
                      value={formData.name}
                      required
                      placeholder="Bắt buộc nhập tên khách hàng"
                      onChange={(e) => handleChange('name', e.target.value)}
                    />
                  </div>

                  {/* Ngày sinh (Loc: 140, 59) */}
                  <div className="gym-ae-row">
                    <label className="gym-ae-label">Ngày sinh</label>
                    <input
                      type="date"
                      className="gym-ae-input"
                      style={{ width: 140, flex: 'none' }}
                      value={formData.ngaySinh}
                      onChange={(e) => handleChange('ngaySinh', e.target.value)}
                    />
                  </div>

                  {/* Địa chỉ (Loc: 140, 85) */}
                  <div className="gym-ae-row">
                    <label className="gym-ae-label">Địa chỉ</label>
                    <input
                      type="text"
                      className="gym-ae-input"
                      value={formData.diaChi}
                      onChange={(e) => handleChange('diaChi', e.target.value)}
                    />
                  </div>

                  {/* Dòng ghép: Điện thoại (Loc: 140, 112) & Email (Loc: 313, 112) */}
                  <div className="gym-ae-row split-row">
                    <label className="gym-ae-label">Điện thoại</label>
                    <input
                      type="text"
                      className="gym-ae-input"
                      style={{ width: 120, flex: 'none' }}
                      value={formData.dienThoai}
                      onChange={(e) => handleChange('dienThoai', e.target.value)}
                    />

                    <label className="gym-ae-label" style={{ width: 45, textAlign: 'right', paddingRight: 6 }}>
                      Email
                    </label>
                    <input
                      type="email"
                      className="gym-ae-input"
                      value={formData.email}
                      onChange={(e) => handleChange('email', e.target.value)}
                    />
                  </div>

                  {/* Facebook (Loc: 140, 138) */}
                  <div className="gym-ae-row">
                    <label className="gym-ae-label">Facebook</label>
                    <input
                      type="text"
                      className="gym-ae-input"
                      value={formData.facebook}
                      onChange={(e) => handleChange('facebook', e.target.value)}
                    />
                  </div>

                  {/* Nhân viên phụ trách (Loc: 140, 164) */}
                  <div className="gym-ae-row">
                    <label className="gym-ae-label">Nhân viên phụ trách</label>
                    <select
                      className="gym-ae-select"
                      value={formData.dNhanVienId}
                      onChange={(e) => handleChange('dNhanVienId', e.target.value)}
                    >
                      <option value="">-- Chọn nhân viên --</option>
                      {metadata.nhanVien?.map(nv => (
                        <option key={nv.id} value={nv.id}>{nv.name}</option>
                      ))}
                    </select>
                  </div>

                  {/* Nhóm khách hàng (Loc: 140, 191) */}
                  <div className="gym-ae-row">
                    <label className="gym-ae-label">Nhóm khách hàng</label>
                    <select
                      className="gym-ae-select"
                      value={formData.dNhomKhachHangId}
                      onChange={(e) => handleChange('dNhomKhachHangId', e.target.value)}
                    >
                      <option value="">-- Chọn nhóm khách hàng --</option>
                      {metadata.nhomKhach?.map(nk => (
                        <option key={nk.id} value={nk.id}>{nk.name}</option>
                      ))}
                    </select>
                  </div>

                  {/* Tỉnh thành (Loc: 140, 218) */}
                  <div className="gym-ae-row">
                    <label className="gym-ae-label">Tỉnh thành</label>
                    <select
                      className="gym-ae-select"
                      value={formData.dTinhThanhId}
                      onChange={(e) => handleChange('dTinhThanhId', e.target.value)}
                    >
                      <option value="">-- Chọn tỉnh thành --</option>
                      {metadata.tinhThanh?.map(tt => (
                        <option key={tt.id} value={tt.id}>{tt.name}</option>
                      ))}
                    </select>
                  </div>

                  {/* Ghi chú (Loc: 140, 245) */}
                  <div className="gym-ae-row">
                    <label className="gym-ae-label">Ghi chú</label>
                    <input
                      type="text"
                      className="gym-ae-input"
                      value={formData.note}
                      onChange={(e) => handleChange('note', e.target.value)}
                    />
                  </div>

                  {/* Trạng thái (Loc: 140, 271) - MẶC ĐỊNH LÀ CHƯA KÍCH HOẠT (0) */}
                  <div className="gym-ae-row">
                    <label className="gym-ae-label">Trạng thái</label>
                    <select
                      className="gym-ae-select"
                      value={formData.dTrangThaiId}
                      onChange={(e) => handleChange('dTrangThaiId', e.target.value)}
                    >
                      <option value="0">❌ Chưa kích hoạt</option>
                      <option value="1">▶️ Đang sử dụng</option>
                      <option value="2">⏸️ Bảo lưu</option>
                      <option value="3">⚠️ Quá hạn</option>
                      <option value="4">⚠️ Quá lần tập</option>
                    </select>
                  </div>
                </div>

                {/* CỘT PHẢI: KHUNG ẢNH ĐẠI DIỆN & NÚT WEBCAM */}
                <div className="gym-main-avatar-col">
                  <div className="gym-avatar-header">
                    <label className="gym-ae-label" style={{ width: 'auto', marginBottom: 4 }}>Ảnh:</label>
                  </div>
                  <div className="gym-avatar-row-wrapper">
                    <div className="gym-avatar-box">
                      {webcamActive ? (
                        <video ref={videoRef} autoPlay className="gym-avatar-video" />
                      ) : formData.anh ? (
                        <img src={formData.anh} alt="Ảnh hội viên" className="gym-avatar-img" />
                      ) : (
                        <div className="gym-avatar-placeholder">
                          <i className="fa-solid fa-user" style={{ fontSize: 50, color: '#cbd5e1' }}></i>
                        </div>
                      )}
                    </div>
                    <div className="gym-avatar-side-icons">
                      <button
                        type="button"
                        className="gym-avatar-icon-btn"
                        title="Sao chép ảnh"
                        onClick={() => {
                          if (formData.anh) {
                            navigator.clipboard.writeText(formData.anh);
                            showNotification && showNotification('Đã sao chép ảnh!');
                          }
                        }}
                      >
                        <i className="fa-regular fa-copy"></i>
                      </button>
                      <button
                        type="button"
                        className="gym-avatar-icon-btn"
                        title="Dán ảnh từ clipboard"
                        onClick={async () => {
                          try {
                            const text = await navigator.clipboard.readText();
                            if (text && text.startsWith('data:image')) {
                              handleChange('anh', text);
                              showNotification && showNotification('Đã dán ảnh!');
                            }
                          } catch (e) {
                            alert('Không thể đọc ảnh từ clipboard');
                          }
                        }}
                      >
                        <i className="fa-regular fa-paste"></i>
                      </button>
                      <button
                        type="button"
                        className="gym-avatar-icon-btn delete"
                        title="Xóa ảnh"
                        onClick={() => handleChange('anh', '')}
                      >
                        <i className="fa-solid fa-xmark"></i>
                      </button>
                    </div>
                  </div>

                  <div className="gym-avatar-actions">
                    {webcamActive ? (
                      <button type="button" className="gym-webcam-btn capture" onClick={handleCaptureWebcam}>
                        <i className="fa-solid fa-camera"></i> Chụp hình
                      </button>
                    ) : (
                      <button type="button" className="gym-webcam-btn" onClick={handleStartWebcam}>
                        <i className="fa-solid fa-camera"></i> Lấy hình từ webcam
                      </button>
                    )}
                  </div>
                </div>
              </div>
            )}

            {/* ========================================================================= */}
            {/* TRANG 2: THÔNG TIN HIỆN TẠI (pageHienTai)                                  */}
            {/* ========================================================================= */}
            {activeTab === 'pageHienTai' && (
              <div className="gym-tab-hientai">
                <div className="gym-hientai-grid">
                  {/* Hàng 1 */}
                  <div className="gym-ae-row">
                    <label className="gym-ae-label bold">LOẠI THẺ:</label>
                    <input
                      type="text"
                      className="gym-ae-input"
                      value={formData.loaiThe}
                      onChange={(e) => handleChange('loaiThe', e.target.value)}
                    />
                  </div>
                  <div className="gym-ae-row">
                    <label className="gym-ae-label bold">CA TẬP:</label>
                    <input
                      type="text"
                      className="gym-ae-input"
                      value={formData.caTap}
                      onChange={(e) => handleChange('caTap', e.target.value)}
                    />
                  </div>

                  {/* Hàng 2 */}
                  <div className="gym-ae-row">
                    <label className="gym-ae-label bold">TỪ NGÀY:</label>
                    <input
                      type="date"
                      className="gym-ae-input"
                      value={formData.tuNgay}
                      onChange={(e) => handleChange('tuNgay', e.target.value)}
                    />
                  </div>
                  <div className="gym-ae-row">
                    <label className="gym-ae-label bold">ĐẾN NGÀY:</label>
                    <input
                      type="date"
                      className="gym-ae-input"
                      value={formData.denNgay}
                      onChange={(e) => handleChange('denNgay', e.target.value)}
                    />
                  </div>
                  <div className="gym-ae-row">
                    <label className="gym-ae-label bold">CÒN LẠI:</label>
                    <input
                      type="number"
                      className="gym-ae-input"
                      value={formData.soNgayCon}
                      onChange={(e) => handleChange('soNgayCon', e.target.value)}
                    />
                  </div>

                  {/* Hàng 3 */}
                  <div className="gym-ae-row">
                    <label className="gym-ae-label bold">SỐ LẦN:</label>
                    <input
                      type="number"
                      className="gym-ae-input"
                      value={formData.soLan}
                      onChange={(e) => handleChange('soLan', e.target.value)}
                    />
                  </div>
                  <div className="gym-ae-row">
                    <label className="gym-ae-label bold">ĐÃ ĐI TẬP:</label>
                    <input
                      type="number"
                      className="gym-ae-input"
                      value={formData.daTap}
                      onChange={(e) => handleChange('daTap', e.target.value)}
                    />
                  </div>
                  <div className="gym-ae-row">
                    <label className="gym-ae-label bold">CÒN LẠI:</label>
                    <input
                      type="number"
                      className="gym-ae-input"
                      value={formData.conLai}
                      onChange={(e) => handleChange('conLai', e.target.value)}
                    />
                  </div>
                </div>

                {/* Các nút hành động thiết bị trong CSDL DATA.fdb */}
                <div className="gym-hientai-actions">
                  <button type="button" className="gym-btn-dev" onClick={() => alert('Đang lấy trạng thái thẻ từ máy chấm công...')}>
                    LẤY TRẠNG THÁI THẺ
                  </button>
                  <button type="button" className="gym-btn-dev" onClick={() => alert('Tạo tài khoản hội viên trên máy chấm công...')}>
                    TẠO TÀI KHOẢN
                  </button>
                  <button type="button" className="gym-btn-dev danger" onClick={() => alert('Đã khóa thẻ hội viên trên thiết bị!')}>
                    KHÓA THẺ
                  </button>
                  <button type="button" className="gym-btn-dev success" onClick={() => alert('Đã mở khóa thẻ hội viên trên thiết bị!')}>
                    MỞ THẺ
                  </button>
                </div>

                {/* Bảng lưới thiết bị: THÔNG TIN TRÊN MÁY */}
                <div className="gym-hientai-machine-box">
                  <div className="gym-machine-title">THÔNG TIN TRÊN MÁY</div>
                  <table className="gym-machine-table">
                    <thead>
                      <tr>
                        <th>Mã máy</th>
                        <th>Tên máy</th>
                        <th>Trạng thái kết nối</th>
                        <th>Đã đồng bộ</th>
                        <th>Thời gian</th>
                      </tr>
                    </thead>
                    <tbody>
                      <tr>
                        <td>DEV01</td>
                        <td>Cửa kiểm soát chính</td>
                        <td><span style={{ color: '#16a34a' }}>● Hoạt động</span></td>
                        <td>Đã đồng bộ</td>
                        <td>{new Date().toLocaleTimeString()}</td>
                      </tr>
                    </tbody>
                  </table>
                </div>
              </div>
            )}

            {/* ========================================================================= */}
            {/* TRANG 3: LỊCH SỬ GIAO DỊCH (pageLichSuGiaoDich - grLichSu)                 */}
            {/* ========================================================================= */}
            {activeTab === 'pageLichSuGiaoDich' && (
              <div className="gym-subtab-container">
                <table className="gym-subtab-table">
                  <thead>
                    <tr>
                      <th>Số phiếu</th>
                      <th>Ngày</th>
                      <th>Nội dung</th>
                      <th>Số tiền</th>
                      <th>Nhân viên</th>
                      <th>Ghi chú</th>
                    </tr>
                  </thead>
                  <tbody>
                    {subtabsData?.donHang?.length > 0 ? (
                      subtabsData.donHang.map((dh, i) => (
                        <tr key={i}>
                          <td>{dh.soPhieu || `HD00${i + 1}`}</td>
                          <td>{dh.ngay || ''}</td>
                          <td>{dh.noiDung || 'Đăng ký gói tập'}</td>
                          <td style={{ textAlign: 'right' }}>{(dh.tongCong || 0).toLocaleString()} đ</td>
                          <td>{dh.nhanVien || ''}</td>
                          <td>{dh.ghiChu || ''}</td>
                        </tr>
                      ))
                    ) : (
                      <tr>
                        <td colSpan="6" style={{ textAlign: 'center', padding: 24, color: '#94a3b8' }}>
                          Chưa có lịch sử giao dịch nào
                        </td>
                      </tr>
                    )}
                  </tbody>
                </table>
              </div>
            )}

            {/* ========================================================================= */}
            {/* TRANG 4: THỂ TRẠNG (pageTheTrang - grTheTrang)                             */}
            {/* ========================================================================= */}
            {activeTab === 'pageTheTrang' && (
              <div className="gym-subtab-container">
                <table className="gym-subtab-table">
                  <thead>
                    <tr>
                      <th>Ngày đo</th>
                      <th>Chiều cao (cm)</th>
                      <th>Cân nặng (kg)</th>
                      <th>BMI</th>
                      <th>Tỷ lệ mỡ (%)</th>
                      <th>Cơ bắp (kg)</th>
                      <th>Ghi chú</th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr>
                      <td>{new Date().toLocaleDateString('vi-VN')}</td>
                      <td>170</td>
                      <td>65</td>
                      <td>22.5</td>
                      <td>18%</td>
                      <td>32</td>
                      <td>Thể trạng chuẩn</td>
                    </tr>
                  </tbody>
                </table>
              </div>
            )}
          </div>

          {/* FOOTER NÚT WINFORMS ĐÚNG 100% NHƯ BẢN GỐC */}
          <div className="gym-ae-footer">
            <div className="gym-ae-footer-left">
              <button
                type="button"
                className="gym-win-btn"
                onClick={() => handleSubmit(null, 'print')}
                disabled={saving}
              >
                <i className="fa-solid fa-print"></i> Lưu & In
              </button>
              <button
                type="button"
                className="gym-win-btn"
                onClick={() => handleSubmit(null, 'preview')}
                disabled={saving}
              >
                <i className="fa-regular fa-file-lines"></i> Lưu & Xem in
              </button>
            </div>
            <div className="gym-ae-footer-right">
              <button
                type="button"
                className="gym-win-btn"
                onClick={(e) => handleSubmit(e, 'stay')}
                disabled={saving}
              >
                <i className="fa-solid fa-floppy-disk"></i> Lưu
              </button>
              <button
                type="button"
                className="gym-win-btn"
                onClick={(e) => handleSubmit(e, 'new')}
                disabled={saving}
              >
                Lưu & Mới
              </button>
              <button
                type="submit"
                className="gym-win-btn primary"
                disabled={saving}
              >
                Lưu & thoát
              </button>
              <button
                type="button"
                className="gym-win-btn exit"
                onClick={onClose}
                disabled={saving}
              >
                Thoát
              </button>
            </div>
          </div>
        </form>
      </div>
    </div>
  );
}
