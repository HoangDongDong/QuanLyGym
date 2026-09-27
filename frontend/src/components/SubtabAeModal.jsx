import React, { useState, useEffect } from 'react';

export default function SubtabAeModal({
  show,
  mode = 'create',
  tabId = 'datHang',
  tabLabel = 'Đặt hàng',
  customer = null,
  initialData = null,
  metadata = {},
  onSave,
  onClose
}) {
  const [formData, setFormData] = useState({});
  const [saving, setSaving] = useState(false);

  // Khởi tạo dữ liệu form tương ứng với từng loại subtab
  useEffect(() => {
    if (!show) return;

    const todayStr = new Date().toISOString().split('T')[0];
    const prefixMap = {
      datHang: 'DH',
      baoGia: 'BG',
      donHang: 'HD',
      giaHanThe: 'GH',
      baoLuuThe: 'BL',
      doiLoaiThe: 'DL',
      tangGiamDiem: 'TG',
      theTrang: 'TT',
      phieuThu: 'PT',
      phieuChi: 'PC',
      thuCongNo: 'TCN',
      datCoc: 'DC',
      vaoRa: 'VR'
    };
    const prefix = prefixMap[tabId] || 'CT';
    const autoNo = `${prefix}${Date.now().toString().slice(-6)}`;

    if (mode === 'edit' && initialData) {
      setFormData({
        ...initialData,
        ngay: initialData.ngay ? (initialData.ngay.includes('/') ? initialData.ngay.split('/').reverse().join('-') : initialData.ngay) : todayStr
      });
    } else {
      // Dữ liệu tạo mới
      const baseData = {
        soPhiu: autoNo,
        ngay: todayStr,
        khachHangId: customer?.id || '',
        maKhach: customer?.maThe || '',
        tenKhach: customer?.tenKhachHang || '',
        dienThoai: customer?.dienThoai || '',
        diaChi: customer?.diaChi || '',
        email: customer?.email || '',
        note: '',
        nhanVienId: customer?.nhanVienId || '',
        nhanVien: customer?.nhanVien || 'Administrator'
      };

      if (tabId === 'datHang' || tabId === 'baoGia') {
        setFormData({
          ...baseData,
          tienHang: 0,
          tiLeGiamGia: 0,
          tienGiamGia: 0,
          tienThue: 0,
          phiVanChuyen: 0,
          tongCong: 0
        });
      } else if (tabId === 'donHang') {
        setFormData({
          ...baseData,
          tienHang: 0,
          tienGiamGia: 0,
          tongCong: 0,
          tienMat: 0,
          chuyenKhoan: 0,
          thanhToan: 0,
          conLai: 0,
          cuaHang: 'Cửa hàng chính'
        });
      } else if (tabId === 'giaHanThe') {
        setFormData({
          ...baseData,
          dloaiTheId: customer?.dloaiTheId || (metadata.loaiThe?.[0]?.id || ''),
          loaiThe: customer?.loaiThe || (metadata.loaiThe?.[0]?.name || ''),
          tuNgay: todayStr,
          denNgay: todayStr,
          soLan: 30,
          soTien: 500000,
          tongCong: 500000,
          thanhToan: 500000,
          chuaKichHoat: false
        });
      } else if (tabId === 'baoLuuThe') {
        setFormData({
          ...baseData,
          tuNgay: todayStr,
          denNgay: todayStr,
          soNgay: 30,
          soTien: 0,
          lyDo: 'Bảo lưu theo yêu cầu của hội viên'
        });
      } else if (tabId === 'doiLoaiThe') {
        setFormData({
          ...baseData,
          loaiTheCu: customer?.loaiThe || '',
          dloaiTheIdMoi: metadata.loaiThe?.[0]?.id || '',
          loaiTheMoi: metadata.loaiThe?.[0]?.name || '',
          soTien: 0
        });
      } else if (tabId === 'tangGiamDiem') {
        setFormData({
          ...baseData,
          loai: 'tang',
          diemTang: 10,
          diemGiam: 0,
          lyDo: 'Thưởng tích lũy hội viên'
        });
      } else if (tabId === 'theTrang') {
        setFormData({
          ...baseData,
          chieuCao: 170,
          canNang: 65,
          bmi: 22.5,
          vongNguc: 90,
          vongBung: 75,
          vongMong: 92
        });
      } else if (tabId === 'phieuThu' || tabId === 'thuCongNo' || tabId === 'datCoc') {
        setFormData({
          ...baseData,
          thu: 200000,
          chi: 0,
          dienGiai: tabId === 'thuCongNo' ? 'Thu công nợ hội viên' : (tabId === 'datCoc' ? 'Đặt cọc dịch vụ phòng tập' : 'Thu tiền hội viên'),
          chungTuGoc: '',
          chuyenKhoan: false,
          lyDoThuChi: 'Thu phí dịch vụ'
        });
      } else if (tabId === 'phieuChi') {
        setFormData({
          ...baseData,
          thu: 0,
          chi: 100000,
          dienGiai: 'Chi hoàn tiền / dịch vụ',
          chungTuGoc: '',
          chuyenKhoan: false,
          lyDoThuChi: 'Chi khác'
        });
      } else if (tabId === 'vaoRa') {
        setFormData({
          ...baseData,
          gio: new Date().toLocaleTimeString('vi-VN', { hour16: false, hour: '2-digit', minute: '2-digit', second: '2-digit' }),
          may: 'Cửa kiểm soát 1',
          trangThai: 'Hợp lệ'
        });
      } else {
        setFormData({ ...baseData });
      }
    }
  }, [show, mode, tabId, customer, initialData, metadata]);

  if (!show) return null;

  const handleChange = (field, val) => {
    setFormData(prev => {
      const updated = { ...prev, [field]: val };
      // Tự động tính toán phụ trợ
      if (tabId === 'theTrang' && (field === 'chieuCao' || field === 'canNang')) {
        const h = parseFloat(field === 'chieuCao' ? val : prev.chieuCao) / 100;
        const w = parseFloat(field === 'canNang' ? val : prev.canNang);
        if (h > 0 && w > 0) {
          updated.bmi = +(w / (h * h)).toFixed(1);
        }
      }
      if ((tabId === 'datHang' || tabId === 'baoGia') && (field === 'tienHang' || field === 'tienGiamGia' || field === 'tienThue' || field === 'phiVanChuyen')) {
        const th = parseFloat(field === 'tienHang' ? val : prev.tienHang) || 0;
        const gg = parseFloat(field === 'tienGiamGia' ? val : prev.tienGiamGia) || 0;
        const tt = parseFloat(field === 'tienThue' ? val : prev.tienThue) || 0;
        const pvc = parseFloat(field === 'phiVanChuyen' ? val : prev.phiVanChuyen) || 0;
        updated.tongCong = th - gg + tt + pvc;
      }
      return updated;
    });
  };

  const handleSubmit = async (keepOpen = false) => {
    if (!formData.soPhiu && tabId !== 'theTrang') {
      alert('Vui lòng nhập Số phiếu!');
      return;
    }

    try {
      setSaving(true);
      await onSave(tabId, formData, mode);
      setSaving(false);
      if (keepOpen) {
        // Tạo mã mới và giữ modal mở
        const prefixMap = { datHang: 'DH', baoGia: 'BG', donHang: 'HD', giaHanThe: 'GH', phieuThu: 'PT', thuCongNo: 'TCN', datCoc: 'DC' };
        const prefix = prefixMap[tabId] || 'CT';
        setFormData(prev => ({
          ...prev,
          soPhiu: `${prefix}${Date.now().toString().slice(-6)}`
        }));
      } else {
        onClose();
      }
    } catch (err) {
      setSaving(false);
      console.error(err);
      alert('Lỗi lưu dữ liệu: ' + (err.message || 'Không thể lưu'));
    }
  };

  return (
    <div className="sub-modal-backdrop" onClick={onClose} style={{ zIndex: 100001 }}>
      <div
        className="choice-dialog-window"
        onClick={(e) => e.stopPropagation()}
        style={{
          width: 580,
          background: '#f8fafc',
          boxShadow: '0 10px 25px -5px rgba(0, 0, 0, 0.4), 0 8px 10px -6px rgba(0, 0, 0, 0.3)',
          border: '1px solid #7da2ce',
          borderRadius: 4,
          overflow: 'hidden'
        }}
      >
        {/* WINFORMS TITLEBAR */}
        <div
          className="choice-dialog-titlebar"
          style={{
            background: 'linear-gradient(180deg, #3b82f6 0%, #1d4ed8 100%)',
            color: '#ffffff',
            padding: '7px 12px',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'space-between',
            fontSize: 13,
            fontWeight: 600
          }}
        >
          <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
            <span>{mode === 'create' ? '✚' : '✏️'}</span>
            <span>{mode === 'create' ? `Thêm mới ${tabLabel}` : `Chỉnh sửa ${tabLabel}`}</span>
            {customer && <span style={{ opacity: 0.85, fontWeight: 400 }}>- [{customer.tenKhachHang}]</span>}
          </div>
          <button
            className="choice-dialog-close"
            onClick={onClose}
            style={{
              background: 'transparent',
              border: 'none',
              color: '#ffffff',
              fontSize: 14,
              cursor: 'pointer',
              padding: '0 4px',
              lineHeight: 1
            }}
          >
            ✕
          </button>
        </div>

        {/* DIALOG BODY */}
        <div style={{ padding: '16px 20px', maxHeight: '75vh', overflowY: 'auto' }}>
          {/* CUSTOMER SUMMARY BOX */}
          <div
            style={{
              background: '#eff6ff',
              border: '1px solid #bfdbfe',
              borderRadius: 3,
              padding: '8px 12px',
              marginBottom: 14,
              display: 'grid',
              gridTemplateColumns: '1fr 1fr',
              gap: 8,
              fontSize: 12
            }}
          >
            <div><strong>Khách hàng:</strong> {customer?.tenKhachHang || 'Chưa chọn'}</div>
            <div><strong>Mã thẻ:</strong> {customer?.maThe || '---'}</div>
            <div><strong>Điện thoại:</strong> {customer?.dienThoai || '---'}</div>
            <div><strong>Địa chỉ:</strong> {customer?.diaChi || '---'}</div>
          </div>

          {/* DYNAMIC FORM FIELDS BASED ON TAB */}
          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '10px 14px', fontSize: 12 }}>
            {/* SỐ PHIẾU & NGÀY */}
            <div>
              <label style={{ display: 'block', marginBottom: 3, fontWeight: 600, color: '#334155' }}>Số phiếu *</label>
              <input
                type="text"
                className="tn-input"
                style={{ width: '100%', height: 26, fontSize: 12, padding: '2px 8px' }}
                value={formData.soPhiu || ''}
                onChange={(e) => handleChange('soPhiu', e.target.value)}
              />
            </div>
            <div>
              <label style={{ display: 'block', marginBottom: 3, fontWeight: 600, color: '#334155' }}>Ngày *</label>
              <input
                type="date"
                className="tn-input"
                style={{ width: '100%', height: 26, fontSize: 12, padding: '2px 8px' }}
                value={formData.ngay || ''}
                onChange={(e) => handleChange('ngay', e.target.value)}
              />
            </div>

            {/* TAB SPECIFIC: ĐẶT HÀNG / BÁO GIÁ */}
            {(tabId === 'datHang' || tabId === 'baoGia') && (
              <>
                <div>
                  <label style={{ display: 'block', marginBottom: 3, fontWeight: 500 }}>Tiền hàng</label>
                  <input
                    type="number"
                    className="tn-input"
                    style={{ width: '100%', height: 26, fontSize: 12, padding: '2px 8px' }}
                    value={formData.tienHang ?? 0}
                    onChange={(e) => handleChange('tienHang', parseFloat(e.target.value) || 0)}
                  />
                </div>
                <div>
                  <label style={{ display: 'block', marginBottom: 3, fontWeight: 500 }}>Tiền giảm giá</label>
                  <input
                    type="number"
                    className="tn-input"
                    style={{ width: '100%', height: 26, fontSize: 12, padding: '2px 8px' }}
                    value={formData.tienGiamGia ?? 0}
                    onChange={(e) => handleChange('tienGiamGia', parseFloat(e.target.value) || 0)}
                  />
                </div>
                <div>
                  <label style={{ display: 'block', marginBottom: 3, fontWeight: 500 }}>Tiền thuế</label>
                  <input
                    type="number"
                    className="tn-input"
                    style={{ width: '100%', height: 26, fontSize: 12, padding: '2px 8px' }}
                    value={formData.tienThue ?? 0}
                    onChange={(e) => handleChange('tienThue', parseFloat(e.target.value) || 0)}
                  />
                </div>
                <div>
                  <label style={{ display: 'block', marginBottom: 3, fontWeight: 600, color: '#16a34a' }}>Tổng cộng</label>
                  <input
                    type="number"
                    className="tn-input"
                    style={{ width: '100%', height: 26, fontSize: 12, padding: '2px 8px', fontWeight: 'bold', color: '#16a34a' }}
                    value={formData.tongCong ?? 0}
                    onChange={(e) => handleChange('tongCong', parseFloat(e.target.value) || 0)}
                  />
                </div>
              </>
            )}

            {/* TAB SPECIFIC: ĐƠN HÀNG */}
            {tabId === 'donHang' && (
              <>
                <div>
                  <label style={{ display: 'block', marginBottom: 3, fontWeight: 500 }}>Tiền hàng</label>
                  <input
                    type="number"
                    className="tn-input"
                    style={{ width: '100%', height: 26, fontSize: 12, padding: '2px 8px' }}
                    value={formData.tienHang ?? 0}
                    onChange={(e) => handleChange('tienHang', parseFloat(e.target.value) || 0)}
                  />
                </div>
                <div>
                  <label style={{ display: 'block', marginBottom: 3, fontWeight: 600, color: '#16a34a' }}>Tổng cộng</label>
                  <input
                    type="number"
                    className="tn-input"
                    style={{ width: '100%', height: 26, fontSize: 12, padding: '2px 8px', fontWeight: 'bold', color: '#16a34a' }}
                    value={formData.tongCong ?? 0}
                    onChange={(e) => handleChange('tongCong', parseFloat(e.target.value) || 0)}
                  />
                </div>
                <div>
                  <label style={{ display: 'block', marginBottom: 3, fontWeight: 500 }}>Thanh toán</label>
                  <input
                    type="number"
                    className="tn-input"
                    style={{ width: '100%', height: 26, fontSize: 12, padding: '2px 8px' }}
                    value={formData.thanhToan ?? 0}
                    onChange={(e) => handleChange('thanhToan', parseFloat(e.target.value) || 0)}
                  />
                </div>
                <div>
                  <label style={{ display: 'block', marginBottom: 3, fontWeight: 500 }}>Cửa hàng</label>
                  <input
                    type="text"
                    className="tn-input"
                    style={{ width: '100%', height: 26, fontSize: 12, padding: '2px 8px' }}
                    value={formData.cuaHang || ''}
                    onChange={(e) => handleChange('cuaHang', e.target.value)}
                  />
                </div>
              </>
            )}

            {/* TAB SPECIFIC: GIA HẠN THẺ */}
            {tabId === 'giaHanThe' && (
              <>
                <div>
                  <label style={{ display: 'block', marginBottom: 3, fontWeight: 500 }}>Loại thẻ / Gói tập</label>
                  <select
                    className="tn-input"
                    style={{ width: '100%', height: 26, fontSize: 12, padding: '2px 8px' }}
                    value={formData.dloaiTheId || ''}
                    onChange={(e) => {
                      const sel = metadata.loaiThe?.find(l => l.id === e.target.value);
                      handleChange('dloaiTheId', e.target.value);
                      if (sel) {
                        handleChange('loaiThe', sel.name);
                        if (sel.giaBan) handleChange('soTien', sel.giaBan);
                      }
                    }}
                  >
                    {metadata.loaiThe?.map(lt => (
                      <option key={lt.id} value={lt.id}>{lt.name} {lt.giaBan ? `(${lt.giaBan.toLocaleString()} đ)` : ''}</option>
                    ))}
                  </select>
                </div>
                <div>
                  <label style={{ display: 'block', marginBottom: 3, fontWeight: 600, color: '#16a34a' }}>Số tiền</label>
                  <input
                    type="number"
                    className="tn-input"
                    style={{ width: '100%', height: 26, fontSize: 12, padding: '2px 8px', fontWeight: 'bold' }}
                    value={formData.soTien ?? 0}
                    onChange={(e) => handleChange('soTien', parseFloat(e.target.value) || 0)}
                  />
                </div>
                <div>
                  <label style={{ display: 'block', marginBottom: 3, fontWeight: 500 }}>Từ ngày</label>
                  <input
                    type="date"
                    className="tn-input"
                    style={{ width: '100%', height: 26, fontSize: 12, padding: '2px 8px' }}
                    value={formData.tuNgay || ''}
                    onChange={(e) => handleChange('tuNgay', e.target.value)}
                  />
                </div>
                <div>
                  <label style={{ display: 'block', marginBottom: 3, fontWeight: 500 }}>Đến ngày</label>
                  <input
                    type="date"
                    className="tn-input"
                    style={{ width: '100%', height: 26, fontSize: 12, padding: '2px 8px' }}
                    value={formData.denNgay || ''}
                    onChange={(e) => handleChange('denNgay', e.target.value)}
                  />
                </div>
                <div>
                  <label style={{ display: 'block', marginBottom: 3, fontWeight: 500 }}>Số lần tập</label>
                  <input
                    type="number"
                    className="tn-input"
                    style={{ width: '100%', height: 26, fontSize: 12, padding: '2px 8px' }}
                    value={formData.soLan ?? 0}
                    onChange={(e) => handleChange('soLan', parseInt(e.target.value) || 0)}
                  />
                </div>
                <div style={{ display: 'flex', alignItems: 'center', marginTop: 18 }}>
                  <label style={{ cursor: 'pointer', display: 'flex', alignItems: 'center', gap: 6 }}>
                    <input
                      type="checkbox"
                      checked={!!formData.chuaKichHoat}
                      onChange={(e) => handleChange('chuaKichHoat', e.target.checked)}
                      style={{ accentColor: '#2563eb' }}
                    />
                    <span>Kích hoạt sau</span>
                  </label>
                </div>
              </>
            )}

            {/* TAB SPECIFIC: PHIẾU THU / CHI / CÔNG NỢ / ĐẶT CỌC */}
            {(tabId === 'phieuThu' || tabId === 'phieuChi' || tabId === 'thuCongNo' || tabId === 'datCoc') && (
              <>
                <div>
                  <label style={{ display: 'block', marginBottom: 3, fontWeight: 600, color: tabId === 'phieuChi' ? '#dc2626' : '#16a34a' }}>
                    {tabId === 'phieuChi' ? 'Số tiền chi *' : 'Số tiền thu *'}
                  </label>
                  <input
                    type="number"
                    className="tn-input"
                    style={{ width: '100%', height: 26, fontSize: 12, padding: '2px 8px', fontWeight: 'bold' }}
                    value={tabId === 'phieuChi' ? (formData.chi ?? 0) : (formData.thu ?? 0)}
                    onChange={(e) => {
                      const val = parseFloat(e.target.value) || 0;
                      if (tabId === 'phieuChi') handleChange('chi', val);
                      else handleChange('thu', val);
                    }}
                  />
                </div>
                <div>
                  <label style={{ display: 'block', marginBottom: 3, fontWeight: 500 }}>Lý do thu chi</label>
                  <input
                    type="text"
                    className="tn-input"
                    style={{ width: '100%', height: 26, fontSize: 12, padding: '2px 8px' }}
                    value={formData.lyDoThuChi || ''}
                    onChange={(e) => handleChange('lyDoThuChi', e.target.value)}
                  />
                </div>
                <div style={{ gridColumn: 'span 2' }}>
                  <label style={{ display: 'block', marginBottom: 3, fontWeight: 500 }}>Diễn giải</label>
                  <input
                    type="text"
                    className="tn-input"
                    style={{ width: '100%', height: 26, fontSize: 12, padding: '2px 8px' }}
                    value={formData.dienGiai || ''}
                    onChange={(e) => handleChange('dienGiai', e.target.value)}
                  />
                </div>
                <div>
                  <label style={{ display: 'block', marginBottom: 3, fontWeight: 500 }}>Chứng từ gốc</label>
                  <input
                    type="text"
                    className="tn-input"
                    style={{ width: '100%', height: 26, fontSize: 12, padding: '2px 8px' }}
                    value={formData.chungTuGoc || ''}
                    onChange={(e) => handleChange('chungTuGoc', e.target.value)}
                  />
                </div>
                <div style={{ display: 'flex', alignItems: 'center', marginTop: 18 }}>
                  <label style={{ cursor: 'pointer', display: 'flex', alignItems: 'center', gap: 6 }}>
                    <input
                      type="checkbox"
                      checked={!!formData.chuyenKhoan}
                      onChange={(e) => handleChange('chuyenKhoan', e.target.checked)}
                      style={{ accentColor: '#2563eb' }}
                    />
                    <span>Chuyển vào tài khoản</span>
                  </label>
                </div>
              </>
            )}

            {/* TAB SPECIFIC: THỂ TRẠNG */}
            {tabId === 'theTrang' && (
              <>
                <div>
                  <label style={{ display: 'block', marginBottom: 3, fontWeight: 500 }}>Chiều cao (cm)</label>
                  <input
                    type="number"
                    className="tn-input"
                    style={{ width: '100%', height: 26, fontSize: 12, padding: '2px 8px' }}
                    value={formData.chieuCao ?? 0}
                    onChange={(e) => handleChange('chieuCao', parseFloat(e.target.value) || 0)}
                  />
                </div>
                <div>
                  <label style={{ display: 'block', marginBottom: 3, fontWeight: 500 }}>Cân nặng (kg)</label>
                  <input
                    type="number"
                    className="tn-input"
                    style={{ width: '100%', height: 26, fontSize: 12, padding: '2px 8px' }}
                    value={formData.canNang ?? 0}
                    onChange={(e) => handleChange('canNang', parseFloat(e.target.value) || 0)}
                  />
                </div>
                <div>
                  <label style={{ display: 'block', marginBottom: 3, fontWeight: 600, color: '#0284c7' }}>Chỉ số BMI</label>
                  <input
                    type="number"
                    className="tn-input"
                    style={{ width: '100%', height: 26, fontSize: 12, padding: '2px 8px', fontWeight: 'bold' }}
                    value={formData.bmi ?? 0}
                    readOnly
                  />
                </div>
                <div>
                  <label style={{ display: 'block', marginBottom: 3, fontWeight: 500 }}>Vòng ngực (cm)</label>
                  <input
                    type="number"
                    className="tn-input"
                    style={{ width: '100%', height: 26, fontSize: 12, padding: '2px 8px' }}
                    value={formData.vongNguc ?? 0}
                    onChange={(e) => handleChange('vongNguc', parseFloat(e.target.value) || 0)}
                  />
                </div>
                <div>
                  <label style={{ display: 'block', marginBottom: 3, fontWeight: 500 }}>Vòng bụng (cm)</label>
                  <input
                    type="number"
                    className="tn-input"
                    style={{ width: '100%', height: 26, fontSize: 12, padding: '2px 8px' }}
                    value={formData.vongBung ?? 0}
                    onChange={(e) => handleChange('vongBung', parseFloat(e.target.value) || 0)}
                  />
                </div>
                <div>
                  <label style={{ display: 'block', marginBottom: 3, fontWeight: 500 }}>Vòng mông (cm)</label>
                  <input
                    type="number"
                    className="tn-input"
                    style={{ width: '100%', height: 26, fontSize: 12, padding: '2px 8px' }}
                    value={formData.vongMong ?? 0}
                    onChange={(e) => handleChange('vongMong', parseFloat(e.target.value) || 0)}
                  />
                </div>
              </>
            )}

            {/* TAB SPECIFIC: TĂNG GIẢM ĐIỂM */}
            {tabId === 'tangGiamDiem' && (
              <>
                <div>
                  <label style={{ display: 'block', marginBottom: 3, fontWeight: 500 }}>Loại giao dịch</label>
                  <select
                    className="tn-input"
                    style={{ width: '100%', height: 26, fontSize: 12, padding: '2px 8px' }}
                    value={formData.loai || 'tang'}
                    onChange={(e) => handleChange('loai', e.target.value)}
                  >
                    <option value="tang">Tăng điểm (+)</option>
                    <option value="giam">Giảm điểm (-)</option>
                  </select>
                </div>
                <div>
                  <label style={{ display: 'block', marginBottom: 3, fontWeight: 500 }}>Số điểm</label>
                  <input
                    type="number"
                    className="tn-input"
                    style={{ width: '100%', height: 26, fontSize: 12, padding: '2px 8px' }}
                    value={formData.loai === 'giam' ? (formData.diemGiam ?? 0) : (formData.diemTang ?? 0)}
                    onChange={(e) => {
                      const val = parseInt(e.target.value) || 0;
                      if (formData.loai === 'giam') {
                        handleChange('diemGiam', val);
                        handleChange('diemTang', 0);
                      } else {
                        handleChange('diemTang', val);
                        handleChange('diemGiam', 0);
                      }
                    }}
                  />
                </div>
                <div style={{ gridColumn: 'span 2' }}>
                  <label style={{ display: 'block', marginBottom: 3, fontWeight: 500 }}>Lý do</label>
                  <input
                    type="text"
                    className="tn-input"
                    style={{ width: '100%', height: 26, fontSize: 12, padding: '2px 8px' }}
                    value={formData.lyDo || ''}
                    onChange={(e) => handleChange('lyDo', e.target.value)}
                  />
                </div>
              </>
            )}

            {/* GHI CHÚ CHUNG CHO TẤT CẢ CÁC FORM */}
            <div style={{ gridColumn: 'span 2' }}>
              <label style={{ display: 'block', marginBottom: 3, fontWeight: 500 }}>Ghi chú</label>
              <textarea
                className="tn-input"
                style={{ width: '100%', height: 48, fontSize: 12, padding: '4px 8px', resize: 'vertical' }}
                value={formData.note || ''}
                onChange={(e) => handleChange('note', e.target.value)}
              />
            </div>
          </div>
        </div>

        {/* WINFORMS BUTTON BAR */}
        <div
          style={{
            background: '#e2e8f0',
            borderTop: '1px solid #cbd5e1',
            padding: '8px 16px',
            display: 'flex',
            justifyContent: 'flex-end',
            gap: 8
          }}
        >
          <button
            className="tn-btn-primary"
            style={{
              padding: '4px 16px',
              height: 28,
              fontSize: 12,
              fontWeight: 600,
              background: '#2563eb',
              color: '#ffffff',
              border: '1px solid #1d4ed8',
              borderRadius: 3,
              cursor: 'pointer'
            }}
            disabled={saving}
            onClick={() => handleSubmit(false)}
          >
            {saving ? 'Đang lưu...' : '💾 Lưu'}
          </button>
          {mode === 'create' && (
            <button
              className="tn-btn-secondary"
              style={{
                padding: '4px 14px',
                height: 28,
                fontSize: 12,
                background: '#ffffff',
                color: '#334155',
                border: '1px solid #94a3b8',
                borderRadius: 3,
                cursor: 'pointer'
              }}
              disabled={saving}
              onClick={() => handleSubmit(true)}
            >
              Lưu & Thêm
            </button>
          )}
          <button
            className="tn-btn-cancel"
            style={{
              padding: '4px 14px',
              height: 28,
              fontSize: 12,
              background: '#ffffff',
              color: '#475569',
              border: '1px solid #94a3b8',
              borderRadius: 3,
              cursor: 'pointer'
            }}
            disabled={saving}
            onClick={onClose}
          >
            Thoát
          </button>
        </div>
      </div>
    </div>
  );
}
