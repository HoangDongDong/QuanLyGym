import React, { useState, useEffect } from 'react';
import { khachHangService } from '../services/khachHangService';

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

  // Helper tính ngày cộng thêm
  const addDays = (dateStr, days) => {
    if (!dateStr) return '';
    try {
      const d = new Date(dateStr);
      d.setDate(d.getDate() + Number(days || 0));
      return d.toISOString().split('T')[0];
    } catch {
      return dateStr;
    }
  };

  const addMonths = (dateStr, months) => {
    if (!dateStr) return '';
    try {
      const d = new Date(dateStr);
      d.setMonth(d.getMonth() + Number(months || 0));
      return d.toISOString().split('T')[0];
    } catch {
      return dateStr;
    }
  };

  // Khởi tạo dữ liệu form tương ứng với từng loại subtab
  useEffect(() => {
    if (!show) return;

    const todayStr = new Date().toISOString().split('T')[0];
    const nowTimeStr = new Date().toTimeString().slice(0, 5); // HH:mm

    if (mode === 'edit' && initialData) {
      setFormData({
        ...initialData,
        ngay: initialData.ngay ? (initialData.ngay.includes('/') ? initialData.ngay.split('/').reverse().join('-') : initialData.ngay) : todayStr,
        tuNgay: initialData.tuNgay ? (initialData.tuNgay.includes('/') ? initialData.tuNgay.split('/').reverse().join('-') : initialData.tuNgay) : todayStr,
        denNgay: initialData.denNgay ? (initialData.denNgay.includes('/') ? initialData.denNgay.split('/').reverse().join('-') : initialData.denNgay) : todayStr
      });
    } else {
      // Dữ liệu tạo mới cơ bản
      const baseData = {
        soPhiu: '',
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

      // Tự động lấy số phiếu chuẩn theo cấu hình hệ thống (NOTEMPLATE)
      khachHangService.generateSlipNumber(tabId).then(res => {
        if (res && res.success && res.soPhiu) {
          setFormData(prev => ({ ...prev, soPhiu: res.soPhiu }));
        }
      }).catch(err => {
        console.error('Lỗi lấy số phiếu tự động:', err);
      });

      // 1. TAB: ĐƠN HÀNG (SFORM_Export/Đơn hàng)
      if (tabId === 'donHang') {
        setFormData({
          ...baseData,
          gioThanhToan: nowTimeStr,
          dkhoXuatId: metadata.khoHang?.[0]?.id || '',
          dnhanVienXuatId: metadata.nhanVien?.[0]?.id || '',
          userThanhToan: 'Administrator',
          tienHang: 0,
          tiLeGiamGia: 0,
          tienGiamGia: 0,
          tiLeThue: 0,
          tienThue: 0,
          phiVanChuyen: 0,
          tongCong: 0,
          thanhToan: 0,
          conLai: 0,
          giaoHang: '',
          doiTra: 0,
          dienGiai: ''
        });
      }
      // 2. TAB: GIA HẠN THẺ (SFORM_Export/Gia hạn thẻ)
      else if (tabId === 'giaHanThe') {
        const defaultLt = metadata.loaiThe?.find(l => l.id === customer?.dloaiTheId) || metadata.loaiThe?.[0];
        const soThang = defaultLt?.soThang || 1;
        const soNgay = defaultLt?.soNgay || (soThang * 30);
        const giaBan = defaultLt?.giaBan || 500000;
        const denNgayCalc = addDays(todayStr, soNgay);

        setFormData({
          ...baseData,
          dloaiTheId: defaultLt?.id || '',
          loaiThe: defaultLt?.name || '',
          dcatapId: customer?.dcatapId || (metadata.caTap?.[0]?.id || ''),
          soLan: defaultLt?.soLan || 30,
          tuNgay: todayStr,
          soThang: soThang,
          soNgay: soNgay,
          ngayTangThem: 0,
          lanTangThem: 0,
          denNgay: denNgayCalc,
          soTien: giaBan,
          tiLeGiamGia: 0,
          tienGiamGia: 0,
          tongCong: giaBan,
          thanhToan: giaBan,
          khuyenMai: '',
          chuaKichHoat: false
        });
      }
      // 3. TAB: BẢO LƯU THẺ (SFORM_Export/Bảo lưu thẻ)
      else if (tabId === 'baoLuuThe') {
        const soNgay = 30;
        const denNgayCalc = addDays(todayStr, soNgay);
        setFormData({
          ...baseData,
          dloaiTheId: customer?.dloaiTheId || '',
          loaiThe: customer?.loaiThe || '',
          tuNgayRef: customer?.tuNgay || todayStr,
          denNgayRef: customer?.denNgay || todayStr,
          tuNgay: todayStr,
          soNgay: soNgay,
          denNgay: denNgayCalc,
          soTien: 0,
          note: 'Bảo lưu theo yêu cầu của hội viên'
        });
      }
      // 4. TAB: PHIẾU THU (SFORM_Export/Phiếu thu)
      else if (tabId === 'phieuThu') {
        setFormData({
          ...baseData,
          dcuaHangId: metadata.cuaHang?.[0]?.id || '',
          loaiDoiTuong: 2, // 2: Khách hàng (SFORM WinForms chuẩn)
          tenDoiTuong: customer?.tenKhachHang || '',
          diaChi: customer?.diaChi || '',
          dnhanVienId: customer?.nhanVienId || (metadata.nhanVien?.[0]?.id || ''),
          dlyDoThuChiId: metadata.lyDoThuChi?.find(l => l.laThu)?.id || '',
          dienGiai: 'Thu tiền hội viên',
          chungTuGoc: '',
          thu: 200000,
          chuyenKhoan: false,
          khongThayDoiCongNo: false
        });
      }
      // 5. TAB: PHIẾU CHI (SFORM_Export/Phiếu chi)
      else if (tabId === 'phieuChi') {
        setFormData({
          ...baseData,
          dcuaHangId: metadata.cuaHang?.[0]?.id || '',
          loaiDoiTuong: 2, // 2: Khách hàng
          tenDoiTuong: customer?.tenKhachHang || '',
          diaChi: customer?.diaChi || '',
          dnhanVienId: customer?.nhanVienId || (metadata.nhanVien?.[0]?.id || ''),
          dlyDoThuChiId: metadata.lyDoThuChi?.find(l => l.laChi)?.id || '',
          dienGiai: 'Chi tiền hội viên / dịch vụ',
          chungTuGoc: '',
          chi: 100000,
          chuyenKhoan: false,
          khongThayDoiCongNo: false
        });
      }
      // 6. TAB: ĐẶT CỌC (SFORM_Export/Đặt cọc)
      else if (tabId === 'datCoc') {
        const defaultLt = metadata.loaiThe?.[0];
        const giaTriGoi = defaultLt?.giaBan || 1000000;
        setFormData({
          ...baseData,
          tenDoiTuong: customer?.tenKhachHang || '',
          dienThoai: customer?.dienThoai || '',
          diaChi: customer?.diaChi || '',
          dloaiTheId: defaultLt?.id || '',
          giaTriGoi: giaTriGoi,
          giamGia: 0,
          tienGiam: 0,
          tongCong: giaTriGoi,
          thu: 500000, // Tiền cọc trước
          conLai: giaTriGoi - 500000,
          dlyDoThuChiId: metadata.lyDoThuChi?.find(l => l.name?.toLowerCase().includes('cọc') || l.name?.toLowerCase().includes('đặt'))?.id || '',
          chuyenKhoan: false
        });
      }
      // 7. TAB: PHIẾU THU CÔNG NỢ (SFORM_Export/Phiếu thu công nợ)
      else if (tabId === 'thuCongNo') {
        setFormData({
          ...baseData,
          tenDoiTuong: customer?.tenKhachHang || '',
          diaChi: customer?.diaChi || '',
          dnhanVienId: customer?.nhanVienId || (metadata.nhanVien?.[0]?.id || ''),
          dlyDoThuChiId: metadata.lyDoThuChi?.find(l => l.name?.toLowerCase().includes('nợ'))?.id || '',
          dienGiai: 'Thu công nợ hội viên',
          chungTuGoc: '',
          thu: 500000,
          chuyenKhoan: false
        });
      }
      // CÁC TAB KHÁC: ĐẶT HÀNG, BÁO GIÁ, ĐỔI LOẠI THẺ, TĂNG GIẢM ĐIỂM
      else if (tabId === 'datHang' || tabId === 'baoGia') {
        setFormData({
          ...baseData,
          tienHang: 0,
          tiLeGiamGia: 0,
          tienGiamGia: 0,
          tiLeThue: 0,
          tienThue: 0,
          phiVanChuyen: 0,
          tongCong: 0
        });
      } else if (tabId === 'doiLoaiThe') {
        setFormData({
          ...baseData,
          loaiTheCu: customer?.loaiThe || '',
          dloaiTheIdMoi: metadata.loaiThe?.[0]?.id || '',
          soTien: 0
        });
      } else if (tabId === 'tangGiamDiem') {
        setFormData({
          ...baseData,
          diemTang: 10,
          diemGiam: 0,
          note: 'Thưởng tích lũy điểm hội viên'
        });
      } else {
        setFormData(baseData);
      }
    }
  }, [show, mode, tabId, customer, initialData, metadata]);

  if (!show) return null;

  const handleChange = (field, value) => {
    setFormData(prev => {
      const updated = { ...prev, [field]: value };

      // Tự động tính toán theo nghiệp vụ từng form
      // A. Đơn hàng: Tính tổng cộng & còn lại
      if (tabId === 'donHang') {
        const th = field === 'tienHang' ? Number(value) || 0 : Number(prev.tienHang) || 0;
        const tg = field === 'tienGiamGia' ? Number(value) || 0 : Number(prev.tienGiamGia) || 0;
        const tt = field === 'tienThue' ? Number(value) || 0 : Number(prev.tienThue) || 0;
        const pvc = field === 'phiVanChuyen' ? Number(value) || 0 : Number(prev.phiVanChuyen) || 0;
        const tong = th - tg + tt + pvc;
        const ttPay = field === 'thanhToan' ? Number(value) || 0 : Number(prev.thanhToan) || 0;
        updated.tongCong = tong;
        updated.conLai = tong - ttPay;
      }

      // B. Gia hạn thẻ: Đổi loại thẻ -> Cập nhật số tiền, số ngày, hạn đến ngày
      if (tabId === 'giaHanThe') {
        if (field === 'dloaiTheId') {
          const lt = metadata.loaiThe?.find(l => l.id === value);
          if (lt) {
            updated.soTien = lt.giaBan || 0;
            updated.tongCong = lt.giaBan || 0;
            updated.thanhToan = lt.giaBan || 0;
            updated.soThang = lt.soThang || 1;
            updated.soNgay = lt.soNgay || (lt.soThang * 30 || 30);
            updated.soLan = lt.soLan || 0;
            updated.denNgay = addDays(updated.tuNgay || prev.tuNgay, updated.soNgay + (Number(prev.ngayTangThem) || 0));
          }
        } else if (field === 'tuNgay' || field === 'soNgay' || field === 'ngayTangThem') {
          const tn = field === 'tuNgay' ? value : prev.tuNgay;
          const sn = field === 'soNgay' ? Number(value) || 0 : Number(prev.soNgay) || 0;
          const ntt = field === 'ngayTangThem' ? Number(value) || 0 : Number(prev.ngayTangThem) || 0;
          updated.denNgay = addDays(tn, sn + ntt);
        } else if (field === 'tiLeGiamGia' || field === 'soTien') {
          const st = field === 'soTien' ? Number(value) || 0 : Number(prev.soTien) || 0;
          const tl = field === 'tiLeGiamGia' ? Number(value) || 0 : Number(prev.tiLeGiamGia) || 0;
          const tg = Math.round(st * tl / 100);
          updated.tienGiamGia = tg;
          updated.tongCong = st - tg;
          updated.thanhToan = st - tg;
        } else if (field === 'thanhToan') {
          // Keep user payment
        }
      }

      // C. Bảo lưu thẻ: Thay đổi số ngày -> Cập nhật hạn bảo lưu đến ngày
      if (tabId === 'baoLuuThe') {
        if (field === 'tuNgay' || field === 'soNgay') {
          const tn = field === 'tuNgay' ? value : prev.tuNgay;
          const sn = field === 'soNgay' ? Number(value) || 0 : Number(prev.soNgay) || 0;
          updated.denNgay = addDays(tn, sn);
        }
      }

      // D. Đặt cọc: Đổi gói tập hoặc giảm giá -> Cập nhật tổng cộng & còn lại
      if (tabId === 'datCoc') {
        if (field === 'dloaiTheId') {
          const lt = metadata.loaiThe?.find(l => l.id === value);
          if (lt) {
            updated.giaTriGoi = lt.giaBan || 0;
            const gg = Number(prev.giamGia) || 0;
            const tg = Math.round(lt.giaBan * gg / 100);
            updated.tienGiam = tg;
            updated.tongCong = lt.giaBan - tg;
            updated.conLai = (lt.giaBan - tg) - (Number(prev.thu) || 0);
          }
        } else if (field === 'giamGia' || field === 'giaTriGoi' || field === 'thu') {
          const gtg = field === 'giaTriGoi' ? Number(value) || 0 : Number(prev.giaTriGoi) || 0;
          const gg = field === 'giamGia' ? Number(value) || 0 : Number(prev.giamGia) || 0;
          const tg = Math.round(gtg * gg / 100);
          const tc = gtg - tg;
          const thu = field === 'thu' ? Number(value) || 0 : Number(prev.thu) || 0;
          updated.tienGiam = tg;
          updated.tongCong = tc;
          updated.conLai = tc - thu;
        }
      }

      return updated;
    });
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!formData.soPhiu || !formData.soPhiu.trim()) {
      alert('Vui lòng nhập Số phiếu!');
      return;
    }

    try {
      setSaving(true);
      await onSave?.(tabId, formData, mode);
      onClose();
    } catch (err) {
      console.error('Lỗi lưu form subtab:', err);
      alert('Lỗi lưu bản ghi: ' + (err.message || err));
    } finally {
      setSaving(false);
    }
  };

  return (
    <div className="choice-dialog-backdrop" onClick={onClose} style={{ zIndex: 1050 }}>
      <div
        className="choice-dialog-window"
        onClick={(e) => e.stopPropagation()}
        style={{ width: tabId === 'donHang' || tabId === 'giaHanThe' ? 720 : 640, maxWidth: '96vw', borderRadius: 4 }}
      >
        {/* TITLEBAR */}
        <div
          className="choice-dialog-titlebar"
          style={{
            background: 'linear-gradient(180deg, #1e3a8a 0%, #1e40af 100%)',
            color: '#ffffff',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'space-between',
            padding: '7px 12px',
            fontSize: 13,
            fontWeight: 600
          }}
        >
          <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
            <span>{mode === 'create' ? '✚' : '✏️'}</span>
            <span>{mode === 'create' ? `Thêm mới ${tabLabel}` : `Chỉnh sửa ${tabLabel}`}</span>
            {customer && <span style={{ opacity: 0.9, fontWeight: 400 }}>- [{customer.tenKhachHang}]</span>}
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
        <form onSubmit={handleSubmit}>
          <div style={{ padding: '16px 20px', maxHeight: '78vh', overflowY: 'auto', background: '#f8fafc' }}>
            
            {/* THÔNG TIN CHUNG: SỐ PHIẾU & NGÀY (CÓ Ở TẤT CẢ CÁC FORM) */}
            <div style={{ display: 'grid', gridTemplateColumns: '1.2fr 1fr', gap: '10px 14px', marginBottom: 14 }}>
              <div>
                <label style={{ display: 'block', marginBottom: 3, fontWeight: 600, color: '#1e3a8a', fontSize: 12 }}>
                  Số phiếu <span style={{ color: '#dc2626' }}>*</span>
                </label>
                <input
                  type="text"
                  required
                  className="tn-input"
                  style={{ width: '100%', height: 28, fontSize: 13, fontWeight: 700, color: '#1d4ed8', fontFamily: 'Consolas, monospace' }}
                  value={formData.soPhiu || ''}
                  onChange={(e) => handleChange('soPhiu', e.target.value)}
                  placeholder="VD: BG26/00001"
                />
              </div>
              <div>
                <label style={{ display: 'block', marginBottom: 3, fontWeight: 600, color: '#334155', fontSize: 12 }}>
                  Ngày <span style={{ color: '#dc2626' }}>*</span>
                </label>
                <input
                  type="date"
                  required
                  className="tn-input"
                  style={{ width: '100%', height: 28, fontSize: 12 }}
                  value={formData.ngay || ''}
                  onChange={(e) => handleChange('ngay', e.target.value)}
                />
              </div>
            </div>

            {/* ========================================================================= */}
            {/* 1. FORM: PHIẾU THU (KHỚP CHUẨN SFORM_Export/Phiếu thu)                    */}
            {/* ========================================================================= */}
            {tabId === 'phieuThu' && (
              <div style={{ display: 'flex', flexDirection: 'column', gap: 10, fontSize: 12 }}>
                <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '10px 14px' }}>
                  <div>
                    <label style={{ display: 'block', marginBottom: 3, fontWeight: 600, color: '#334155' }}>Loại đối tượng</label>
                    <select
                      className="tn-input"
                      style={{ width: '100%', height: 28 }}
                      value={formData.loaiDoiTuong ?? 2}
                      onChange={(e) => handleChange('loaiDoiTuong', Number(e.target.value))}
                    >
                      <option value={2}>Khách hàng</option>
                      <option value={1}>Nhân viên</option>
                      <option value={3}>Nhà cung cấp</option>
                      <option value={0}>Đối tượng ngoài / Khác</option>
                    </select>
                  </div>
                  <div>
                    <label style={{ display: 'block', marginBottom: 3, fontWeight: 600, color: '#334155' }}>Cửa hàng</label>
                    <select
                      className="tn-input"
                      style={{ width: '100%', height: 28 }}
                      value={formData.dcuaHangId || ''}
                      onChange={(e) => handleChange('dcuaHangId', e.target.value)}
                    >
                      {metadata.cuaHang?.map(c => <option key={c.id} value={c.id}>{c.name}</option>) || <option value="">Cửa hàng chính</option>}
                    </select>
                  </div>
                </div>

                <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '10px 14px' }}>
                  <div>
                    <label style={{ display: 'block', marginBottom: 3, fontWeight: 600, color: '#334155' }}>Tên đối tượng / Khách</label>
                    <input
                      type="text"
                      className="tn-input"
                      style={{ width: '100%', height: 28 }}
                      value={formData.tenDoiTuong || ''}
                      onChange={(e) => handleChange('tenDoiTuong', e.target.value)}
                    />
                  </div>
                  <div>
                    <label style={{ display: 'block', marginBottom: 3, fontWeight: 600, color: '#334155' }}>Địa chỉ</label>
                    <input
                      type="text"
                      className="tn-input"
                      style={{ width: '100%', height: 28 }}
                      value={formData.diaChi || ''}
                      onChange={(e) => handleChange('diaChi', e.target.value)}
                    />
                  </div>
                </div>

                <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '10px 14px' }}>
                  <div>
                    <label style={{ display: 'block', marginBottom: 3, fontWeight: 600, color: '#334155' }}>Phân loại (Lý do thu)</label>
                    <select
                      className="tn-input"
                      style={{ width: '100%', height: 28 }}
                      value={formData.dlyDoThuChiId || ''}
                      onChange={(e) => handleChange('dlyDoThuChiId', e.target.value)}
                    >
                      <option value="">-- Chọn lý do thu --</option>
                      {metadata.lyDoThuChi?.filter(l => l.laThu).map(l => (
                        <option key={l.id} value={l.id}>{l.name}</option>
                      ))}
                    </select>
                  </div>
                  <div>
                    <label style={{ display: 'block', marginBottom: 3, fontWeight: 600, color: '#334155' }}>Nhân viên thu</label>
                    <select
                      className="tn-input"
                      style={{ width: '100%', height: 28 }}
                      value={formData.dnhanVienId || ''}
                      onChange={(e) => handleChange('dnhanVienId', e.target.value)}
                    >
                      <option value="">-- Chọn nhân viên --</option>
                      {metadata.nhanVien?.map(nv => (
                        <option key={nv.id} value={nv.id}>{nv.name}</option>
                      ))}
                    </select>
                  </div>
                </div>

                <div>
                  <label style={{ display: 'block', marginBottom: 3, fontWeight: 600, color: '#334155' }}>Lý do thu / Diễn giải</label>
                  <input
                    type="text"
                    className="tn-input"
                    style={{ width: '100%', height: 28 }}
                    value={formData.dienGiai || ''}
                    onChange={(e) => handleChange('dienGiai', e.target.value)}
                  />
                </div>

                <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '10px 14px' }}>
                  <div>
                    <label style={{ display: 'block', marginBottom: 3, fontWeight: 600, color: '#16a34a', fontSize: 13 }}>
                      Số tiền thu (VND) <span style={{ color: '#dc2626' }}>*</span>
                    </label>
                    <input
                      type="number"
                      required
                      min="0"
                      className="tn-input"
                      style={{ width: '100%', height: 32, fontSize: 15, fontWeight: 700, color: '#16a34a' }}
                      value={formData.thu ?? 0}
                      onChange={(e) => handleChange('thu', parseFloat(e.target.value) || 0)}
                    />
                  </div>
                  <div>
                    <label style={{ display: 'block', marginBottom: 3, fontWeight: 600, color: '#334155' }}>Chứng từ gốc</label>
                    <input
                      type="text"
                      className="tn-input"
                      style={{ width: '100%', height: 28 }}
                      value={formData.chungTuGoc || ''}
                      onChange={(e) => handleChange('chungTuGoc', e.target.value)}
                    />
                  </div>
                </div>

                <div style={{ display: 'flex', gap: 24, marginTop: 4 }}>
                  <label style={{ display: 'flex', alignItems: 'center', gap: 6, cursor: 'pointer' }}>
                    <input
                      type="checkbox"
                      checked={!!formData.chuyenKhoan}
                      onChange={(e) => handleChange('chuyenKhoan', e.target.checked)}
                    />
                    <span>Chuyển vào tài khoản</span>
                  </label>
                  <label style={{ display: 'flex', alignItems: 'center', gap: 6, cursor: 'pointer' }}>
                    <input
                      type="checkbox"
                      checked={!!formData.khongThayDoiCongNo}
                      onChange={(e) => handleChange('khongThayDoiCongNo', e.target.checked)}
                    />
                    <span>Không thay đổi công nợ</span>
                  </label>
                </div>
              </div>
            )}

            {/* ========================================================================= */}
            {/* 2. FORM: PHIẾU CHI (KHỚP CHUẨN SFORM_Export/Phiếu chi)                    */}
            {/* ========================================================================= */}
            {tabId === 'phieuChi' && (
              <div style={{ display: 'flex', flexDirection: 'column', gap: 10, fontSize: 12 }}>
                <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '10px 14px' }}>
                  <div>
                    <label style={{ display: 'block', marginBottom: 3, fontWeight: 600, color: '#334155' }}>Loại đối tượng</label>
                    <select
                      className="tn-input"
                      style={{ width: '100%', height: 28 }}
                      value={formData.loaiDoiTuong ?? 2}
                      onChange={(e) => handleChange('loaiDoiTuong', Number(e.target.value))}
                    >
                      <option value={2}>Khách hàng</option>
                      <option value={1}>Nhân viên</option>
                      <option value={3}>Nhà cung cấp</option>
                      <option value={0}>Đối tượng ngoài / Khác</option>
                    </select>
                  </div>
                  <div>
                    <label style={{ display: 'block', marginBottom: 3, fontWeight: 600, color: '#334155' }}>Cửa hàng</label>
                    <select
                      className="tn-input"
                      style={{ width: '100%', height: 28 }}
                      value={formData.dcuaHangId || ''}
                      onChange={(e) => handleChange('dcuaHangId', e.target.value)}
                    >
                      {metadata.cuaHang?.map(c => <option key={c.id} value={c.id}>{c.name}</option>) || <option value="">Cửa hàng chính</option>}
                    </select>
                  </div>
                </div>

                <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '10px 14px' }}>
                  <div>
                    <label style={{ display: 'block', marginBottom: 3, fontWeight: 600, color: '#334155' }}>Tên đối tượng nhận</label>
                    <input
                      type="text"
                      className="tn-input"
                      style={{ width: '100%', height: 28 }}
                      value={formData.tenDoiTuong || ''}
                      onChange={(e) => handleChange('tenDoiTuong', e.target.value)}
                    />
                  </div>
                  <div>
                    <label style={{ display: 'block', marginBottom: 3, fontWeight: 600, color: '#334155' }}>Địa chỉ</label>
                    <input
                      type="text"
                      className="tn-input"
                      style={{ width: '100%', height: 28 }}
                      value={formData.diaChi || ''}
                      onChange={(e) => handleChange('diaChi', e.target.value)}
                    />
                  </div>
                </div>

                <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '10px 14px' }}>
                  <div>
                    <label style={{ display: 'block', marginBottom: 3, fontWeight: 600, color: '#334155' }}>Phân loại (Lý do chi)</label>
                    <select
                      className="tn-input"
                      style={{ width: '100%', height: 28 }}
                      value={formData.dlyDoThuChiId || ''}
                      onChange={(e) => handleChange('dlyDoThuChiId', e.target.value)}
                    >
                      <option value="">-- Chọn lý do chi --</option>
                      {metadata.lyDoThuChi?.filter(l => l.laChi).map(l => (
                        <option key={l.id} value={l.id}>{l.name}</option>
                      ))}
                    </select>
                  </div>
                  <div>
                    <label style={{ display: 'block', marginBottom: 3, fontWeight: 600, color: '#334155' }}>Nhân viên chi</label>
                    <select
                      className="tn-input"
                      style={{ width: '100%', height: 28 }}
                      value={formData.dnhanVienId || ''}
                      onChange={(e) => handleChange('dnhanVienId', e.target.value)}
                    >
                      <option value="">-- Chọn nhân viên --</option>
                      {metadata.nhanVien?.map(nv => (
                        <option key={nv.id} value={nv.id}>{nv.name}</option>
                      ))}
                    </select>
                  </div>
                </div>

                <div>
                  <label style={{ display: 'block', marginBottom: 3, fontWeight: 600, color: '#334155' }}>Lý do chi / Diễn giải</label>
                  <input
                    type="text"
                    className="tn-input"
                    style={{ width: '100%', height: 28 }}
                    value={formData.dienGiai || ''}
                    onChange={(e) => handleChange('dienGiai', e.target.value)}
                  />
                </div>

                <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '10px 14px' }}>
                  <div>
                    <label style={{ display: 'block', marginBottom: 3, fontWeight: 600, color: '#dc2626', fontSize: 13 }}>
                      Số tiền chi (VND) <span style={{ color: '#dc2626' }}>*</span>
                    </label>
                    <input
                      type="number"
                      required
                      min="0"
                      className="tn-input"
                      style={{ width: '100%', height: 32, fontSize: 15, fontWeight: 700, color: '#dc2626' }}
                      value={formData.chi ?? 0}
                      onChange={(e) => handleChange('chi', parseFloat(e.target.value) || 0)}
                    />
                  </div>
                  <div>
                    <label style={{ display: 'block', marginBottom: 3, fontWeight: 600, color: '#334155' }}>Chứng từ gốc</label>
                    <input
                      type="text"
                      className="tn-input"
                      style={{ width: '100%', height: 28 }}
                      value={formData.chungTuGoc || ''}
                      onChange={(e) => handleChange('chungTuGoc', e.target.value)}
                    />
                  </div>
                </div>

                <div style={{ display: 'flex', gap: 24, marginTop: 4 }}>
                  <label style={{ display: 'flex', alignItems: 'center', gap: 6, cursor: 'pointer' }}>
                    <input
                      type="checkbox"
                      checked={!!formData.chuyenKhoan}
                      onChange={(e) => handleChange('chuyenKhoan', e.target.checked)}
                    />
                    <span>Chuyển từ tài khoản</span>
                  </label>
                  <label style={{ display: 'flex', alignItems: 'center', gap: 6, cursor: 'pointer' }}>
                    <input
                      type="checkbox"
                      checked={!!formData.khongThayDoiCongNo}
                      onChange={(e) => handleChange('khongThayDoiCongNo', e.target.checked)}
                    />
                    <span>Không thay đổi công nợ</span>
                  </label>
                </div>
              </div>
            )}

            {/* ========================================================================= */}
            {/* 3. FORM: ĐẶT CỌC (KHỚP CHUẨN SFORM_Export/Đặt cọc)                        */}
            {/* ========================================================================= */}
            {tabId === 'datCoc' && (
              <div style={{ display: 'flex', flexDirection: 'column', gap: 10, fontSize: 12 }}>
                <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '10px 14px' }}>
                  <div>
                    <label style={{ display: 'block', marginBottom: 3, fontWeight: 600, color: '#334155' }}>Khách hàng</label>
                    <input
                      type="text"
                      className="tn-input"
                      style={{ width: '100%', height: 28, fontWeight: 600 }}
                      value={formData.tenDoiTuong || ''}
                      onChange={(e) => handleChange('tenDoiTuong', e.target.value)}
                    />
                  </div>
                  <div>
                    <label style={{ display: 'block', marginBottom: 3, fontWeight: 600, color: '#334155' }}>
                      Điện thoại <span style={{ color: '#dc2626' }}>*</span>
                    </label>
                    <input
                      type="text"
                      required
                      className="tn-input"
                      style={{ width: '100%', height: 28 }}
                      value={formData.dienThoai || ''}
                      onChange={(e) => handleChange('dienThoai', e.target.value)}
                    />
                  </div>
                </div>

                <div>
                  <label style={{ display: 'block', marginBottom: 3, fontWeight: 600, color: '#334155' }}>Địa chỉ</label>
                  <input
                    type="text"
                    className="tn-input"
                    style={{ width: '100%', height: 28 }}
                    value={formData.diaChi || ''}
                    onChange={(e) => handleChange('diaChi', e.target.value)}
                  />
                </div>

                <div>
                  <label style={{ display: 'block', marginBottom: 3, fontWeight: 600, color: '#1e40af' }}>
                    Loại thẻ đặt cọc <span style={{ color: '#dc2626' }}>*</span>
                  </label>
                  <select
                    required
                    className="tn-input"
                    style={{ width: '100%', height: 28, fontWeight: 600 }}
                    value={formData.dloaiTheId || ''}
                    onChange={(e) => handleChange('dloaiTheId', e.target.value)}
                  >
                    <option value="">-- Chọn loại thẻ đặt cọc --</option>
                    {metadata.loaiThe?.map(lt => (
                      <option key={lt.id} value={lt.id}>{lt.name} ({lt.giaBan?.toLocaleString()} đ)</option>
                    ))}
                  </select>
                </div>

                <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr 1fr', gap: '10px 14px' }}>
                  <div>
                    <label style={{ display: 'block', marginBottom: 3, fontWeight: 500 }}>Giá trị gói (đ)</label>
                    <input
                      type="number"
                      min="0"
                      className="tn-input"
                      style={{ width: '100%', height: 28 }}
                      value={formData.giaTriGoi ?? 0}
                      onChange={(e) => handleChange('giaTriGoi', parseFloat(e.target.value) || 0)}
                    />
                  </div>
                  <div>
                    <label style={{ display: 'block', marginBottom: 3, fontWeight: 500 }}>Giảm giá (%)</label>
                    <input
                      type="number"
                      min="0"
                      max="100"
                      className="tn-input"
                      style={{ width: '100%', height: 28 }}
                      value={formData.giamGia ?? 0}
                      onChange={(e) => handleChange('giamGia', parseFloat(e.target.value) || 0)}
                    />
                  </div>
                  <div>
                    <label style={{ display: 'block', marginBottom: 3, fontWeight: 600, color: '#1e3a8a' }}>Tổng cộng (đ)</label>
                    <input
                      type="text"
                      readOnly
                      className="tn-input"
                      style={{ width: '100%', height: 28, background: '#f1f5f9', fontWeight: 700 }}
                      value={(formData.tongCong ?? 0).toLocaleString()}
                    />
                  </div>
                </div>

                <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '10px 14px' }}>
                  <div>
                    <label style={{ display: 'block', marginBottom: 3, fontWeight: 700, color: '#16a34a', fontSize: 13 }}>
                      Số tiền đặt trước (Thu cọc) <span style={{ color: '#dc2626' }}>*</span>
                    </label>
                    <input
                      type="number"
                      required
                      min="0"
                      className="tn-input"
                      style={{ width: '100%', height: 32, fontSize: 15, fontWeight: 700, color: '#16a34a' }}
                      value={formData.thu ?? 0}
                      onChange={(e) => handleChange('thu', parseFloat(e.target.value) || 0)}
                    />
                  </div>
                  <div>
                    <label style={{ display: 'block', marginBottom: 3, fontWeight: 600, color: '#b45309' }}>Còn lại cần thanh toán</label>
                    <input
                      type="text"
                      readOnly
                      className="tn-input"
                      style={{ width: '100%', height: 32, fontSize: 14, fontWeight: 700, background: '#fffbeb', color: '#b45309' }}
                      value={(formData.conLai ?? 0).toLocaleString() + ' đ'}
                    />
                  </div>
                </div>

                <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginTop: 4 }}>
                  <label style={{ display: 'flex', alignItems: 'center', gap: 6, cursor: 'pointer' }}>
                    <input
                      type="checkbox"
                      checked={!!formData.chuyenKhoan}
                      onChange={(e) => handleChange('chuyenKhoan', e.target.checked)}
                    />
                    <span>Chuyển khoản</span>
                  </label>
                </div>
              </div>
            )}

            {/* ========================================================================= */}
            {/* 4. FORM: PHIẾU THU CÔNG NỢ (KHỚP CHUẨN SFORM_Export/Phiếu thu công nợ)    */}
            {/* ========================================================================= */}
            {tabId === 'thuCongNo' && (
              <div style={{ display: 'flex', flexDirection: 'column', gap: 10, fontSize: 12 }}>
                <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '10px 14px' }}>
                  <div>
                    <label style={{ display: 'block', marginBottom: 3, fontWeight: 600, color: '#334155' }}>Khách hàng / Đối tượng nợ</label>
                    <input
                      type="text"
                      className="tn-input"
                      style={{ width: '100%', height: 28, fontWeight: 600 }}
                      value={formData.tenDoiTuong || ''}
                      onChange={(e) => handleChange('tenDoiTuong', e.target.value)}
                    />
                  </div>
                  <div>
                    <label style={{ display: 'block', marginBottom: 3, fontWeight: 600, color: '#334155' }}>Địa chỉ</label>
                    <input
                      type="text"
                      className="tn-input"
                      style={{ width: '100%', height: 28 }}
                      value={formData.diaChi || ''}
                      onChange={(e) => handleChange('diaChi', e.target.value)}
                    />
                  </div>
                </div>

                <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '10px 14px' }}>
                  <div>
                    <label style={{ display: 'block', marginBottom: 3, fontWeight: 600, color: '#334155' }}>Lý do thu chi</label>
                    <select
                      className="tn-input"
                      style={{ width: '100%', height: 28 }}
                      value={formData.dlyDoThuChiId || ''}
                      onChange={(e) => handleChange('dlyDoThuChiId', e.target.value)}
                    >
                      <option value="">-- Thu công nợ --</option>
                      {metadata.lyDoThuChi?.map(l => (
                        <option key={l.id} value={l.id}>{l.name}</option>
                      ))}
                    </select>
                  </div>
                  <div>
                    <label style={{ display: 'block', marginBottom: 3, fontWeight: 600, color: '#334155' }}>Nhân viên thu</label>
                    <select
                      className="tn-input"
                      style={{ width: '100%', height: 28 }}
                      value={formData.dnhanVienId || ''}
                      onChange={(e) => handleChange('dnhanVienId', e.target.value)}
                    >
                      <option value="">-- Chọn nhân viên --</option>
                      {metadata.nhanVien?.map(nv => (
                        <option key={nv.id} value={nv.id}>{nv.name}</option>
                      ))}
                    </select>
                  </div>
                </div>

                <div>
                  <label style={{ display: 'block', marginBottom: 3, fontWeight: 600, color: '#334155' }}>Diễn giải</label>
                  <input
                    type="text"
                    className="tn-input"
                    style={{ width: '100%', height: 28 }}
                    value={formData.dienGiai || ''}
                    onChange={(e) => handleChange('dienGiai', e.target.value)}
                  />
                </div>

                <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '10px 14px' }}>
                  <div>
                    <label style={{ display: 'block', marginBottom: 3, fontWeight: 700, color: '#16a34a', fontSize: 13 }}>
                      Số tiền thu công nợ <span style={{ color: '#dc2626' }}>*</span>
                    </label>
                    <input
                      type="number"
                      required
                      min="0"
                      className="tn-input"
                      style={{ width: '100%', height: 32, fontSize: 15, fontWeight: 700, color: '#16a34a' }}
                      value={formData.thu ?? 0}
                      onChange={(e) => handleChange('thu', parseFloat(e.target.value) || 0)}
                    />
                  </div>
                  <div>
                    <label style={{ display: 'block', marginBottom: 3, fontWeight: 600, color: '#334155' }}>Chứng từ gốc</label>
                    <input
                      type="text"
                      className="tn-input"
                      style={{ width: '100%', height: 28 }}
                      value={formData.chungTuGoc || ''}
                      onChange={(e) => handleChange('chungTuGoc', e.target.value)}
                    />
                  </div>
                </div>

                <div style={{ marginTop: 4 }}>
                  <label style={{ display: 'flex', alignItems: 'center', gap: 6, cursor: 'pointer' }}>
                    <input
                      type="checkbox"
                      checked={!!formData.chuyenKhoan}
                      onChange={(e) => handleChange('chuyenKhoan', e.target.checked)}
                    />
                    <span>Chuyển khoản</span>
                  </label>
                </div>
              </div>
            )}

            {/* ========================================================================= */}
            {/* 5. FORM: ĐƠN HÀNG (KHỚP CHUẨN SFORM_Export/Đơn hàng)                      */}
            {/* ========================================================================= */}
            {tabId === 'donHang' && (
              <div style={{ display: 'flex', flexDirection: 'column', gap: 10, fontSize: 12 }}>
                <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr 1fr', gap: '10px 14px' }}>
                  <div>
                    <label style={{ display: 'block', marginBottom: 3, fontWeight: 600, color: '#334155' }}>Giờ thanh toán</label>
                    <input
                      type="time"
                      className="tn-input"
                      style={{ width: '100%', height: 28 }}
                      value={formData.gioThanhToan || ''}
                      onChange={(e) => handleChange('gioThanhToan', e.target.value)}
                    />
                  </div>
                  <div>
                    <label style={{ display: 'block', marginBottom: 3, fontWeight: 600, color: '#334155' }}>Kho xuất</label>
                    <select
                      className="tn-input"
                      style={{ width: '100%', height: 28 }}
                      value={formData.dkhoXuatId || ''}
                      onChange={(e) => handleChange('dkhoXuatId', e.target.value)}
                    >
                      {metadata.khoHang?.map(k => <option key={k.id} value={k.id}>{k.name}</option>) || <option value="">Kho chính</option>}
                    </select>
                  </div>
                  <div>
                    <label style={{ display: 'block', marginBottom: 3, fontWeight: 600, color: '#334155' }}>Nhân viên xuất</label>
                    <select
                      className="tn-input"
                      style={{ width: '100%', height: 28 }}
                      value={formData.dnhanVienXuatId || ''}
                      onChange={(e) => handleChange('dnhanVienXuatId', e.target.value)}
                    >
                      <option value="">-- Chọn nhân viên --</option>
                      {metadata.nhanVien?.map(nv => <option key={nv.id} value={nv.id}>{nv.name}</option>)}
                    </select>
                  </div>
                </div>

                <div style={{ display: 'grid', gridTemplateColumns: '1.2fr 1fr 1fr', gap: '10px 14px' }}>
                  <div>
                    <label style={{ display: 'block', marginBottom: 3, fontWeight: 500 }}>Tiền hàng (đ)</label>
                    <input
                      type="number"
                      min="0"
                      className="tn-input"
                      style={{ width: '100%', height: 28 }}
                      value={formData.tienHang ?? 0}
                      onChange={(e) => handleChange('tienHang', parseFloat(e.target.value) || 0)}
                    />
                  </div>
                  <div>
                    <label style={{ display: 'block', marginBottom: 3, fontWeight: 500 }}>Tỉ lệ giảm (%)</label>
                    <input
                      type="number"
                      min="0"
                      max="100"
                      className="tn-input"
                      style={{ width: '100%', height: 28 }}
                      value={formData.tiLeGiamGia ?? 0}
                      onChange={(e) => handleChange('tiLeGiamGia', parseFloat(e.target.value) || 0)}
                    />
                  </div>
                  <div>
                    <label style={{ display: 'block', marginBottom: 3, fontWeight: 500 }}>Tiền giảm giá</label>
                    <input
                      type="number"
                      min="0"
                      className="tn-input"
                      style={{ width: '100%', height: 28 }}
                      value={formData.tienGiamGia ?? 0}
                      onChange={(e) => handleChange('tienGiamGia', parseFloat(e.target.value) || 0)}
                    />
                  </div>
                </div>

                <div style={{ display: 'grid', gridTemplateColumns: '1.2fr 1fr 1fr', gap: '10px 14px' }}>
                  <div>
                    <label style={{ display: 'block', marginBottom: 3, fontWeight: 500 }}>Phí vận chuyển</label>
                    <input
                      type="number"
                      min="0"
                      className="tn-input"
                      style={{ width: '100%', height: 28 }}
                      value={formData.phiVanChuyen ?? 0}
                      onChange={(e) => handleChange('phiVanChuyen', parseFloat(e.target.value) || 0)}
                    />
                  </div>
                  <div>
                    <label style={{ display: 'block', marginBottom: 3, fontWeight: 700, color: '#1e3a8a' }}>Tổng cộng (đ)</label>
                    <input
                      type="text"
                      readOnly
                      className="tn-input"
                      style={{ width: '100%', height: 28, background: '#f1f5f9', fontWeight: 700, color: '#1e3a8a' }}
                      value={(formData.tongCong ?? 0).toLocaleString()}
                    />
                  </div>
                  <div>
                    <label style={{ display: 'block', marginBottom: 3, fontWeight: 700, color: '#16a34a' }}>Thanh toán (đ)</label>
                    <input
                      type="number"
                      min="0"
                      className="tn-input"
                      style={{ width: '100%', height: 28, fontWeight: 700, color: '#16a34a' }}
                      value={formData.thanhToan ?? 0}
                      onChange={(e) => handleChange('thanhToan', parseFloat(e.target.value) || 0)}
                    />
                  </div>
                </div>

                <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '10px 14px' }}>
                  <div>
                    <label style={{ display: 'block', marginBottom: 3, fontWeight: 500 }}>Giao hàng</label>
                    <input
                      type="text"
                      className="tn-input"
                      style={{ width: '100%', height: 28 }}
                      value={formData.giaoHang || ''}
                      onChange={(e) => handleChange('giaoHang', e.target.value)}
                    />
                  </div>
                  <div>
                    <label style={{ display: 'block', marginBottom: 3, fontWeight: 500 }}>Diễn giải</label>
                    <input
                      type="text"
                      className="tn-input"
                      style={{ width: '100%', height: 28 }}
                      value={formData.dienGiai || ''}
                      onChange={(e) => handleChange('dienGiai', e.target.value)}
                    />
                  </div>
                </div>
              </div>
            )}

            {/* ========================================================================= */}
            {/* 6. FORM: GIA HẠN THẺ (KHỚP CHUẨN SFORM_Export/Gia hạn thẻ)                */}
            {/* ========================================================================= */}
            {tabId === 'giaHanThe' && (
              <div style={{ display: 'flex', flexDirection: 'column', gap: 10, fontSize: 12 }}>
                {/* GROUPBOX THÔNG TIN KHÁCH HÀNG */}
                <div style={{ background: '#f1f5f9', border: '1px solid #cbd5e1', borderRadius: 4, padding: '8px 12px', display: 'grid', gridTemplateColumns: '1fr 1.5fr 1fr 1fr', gap: 8 }}>
                  <div><span style={{ color: '#64748b' }}>Mã thẻ:</span> <strong>{customer?.maThe || '---'}</strong></div>
                  <div><span style={{ color: '#64748b' }}>Tên khách:</span> <strong style={{ color: '#1d4ed8' }}>{customer?.tenKhachHang || '---'}</strong></div>
                  <div><span style={{ color: '#64748b' }}>Điện thoại:</span> {customer?.dienThoai || '---'}</div>
                  <div><span style={{ color: '#64748b' }}>Ca tập:</span> {customer?.caTap || '---'}</div>
                </div>

                <div style={{ display: 'grid', gridTemplateColumns: '1.4fr 1fr 1fr', gap: '10px 14px' }}>
                  <div>
                    <label style={{ display: 'block', marginBottom: 3, fontWeight: 700, color: '#1e3a8a' }}>
                      Loại thẻ / Gói tập <span style={{ color: '#dc2626' }}>*</span>
                    </label>
                    <select
                      required
                      className="tn-input"
                      style={{ width: '100%', height: 28, fontWeight: 600 }}
                      value={formData.dloaiTheId || ''}
                      onChange={(e) => handleChange('dloaiTheId', e.target.value)}
                    >
                      <option value="">-- Chọn loại thẻ --</option>
                      {metadata.loaiThe?.map(lt => (
                        <option key={lt.id} value={lt.id}>{lt.name} ({lt.giaBan?.toLocaleString()} đ)</option>
                      ))}
                    </select>
                  </div>
                  <div>
                    <label style={{ display: 'block', marginBottom: 3, fontWeight: 600, color: '#334155' }}>Ca tập</label>
                    <select
                      className="tn-input"
                      style={{ width: '100%', height: 28 }}
                      value={formData.dcatapId || ''}
                      onChange={(e) => handleChange('dcatapId', e.target.value)}
                    >
                      <option value="">-- Chọn ca tập --</option>
                      {metadata.caTap?.map(ct => <option key={ct.id} value={ct.id}>{ct.name}</option>)}
                    </select>
                  </div>
                  <div>
                    <label style={{ display: 'block', marginBottom: 3, fontWeight: 600, color: '#334155' }}>Số lần tập</label>
                    <input
                      type="number"
                      min="0"
                      className="tn-input"
                      style={{ width: '100%', height: 28 }}
                      value={formData.soLan ?? 30}
                      onChange={(e) => handleChange('soLan', parseInt(e.target.value) || 0)}
                    />
                  </div>
                </div>

                <div style={{ display: 'grid', gridTemplateColumns: '1fr 0.8fr 0.8fr 1fr', gap: '10px 14px' }}>
                  <div>
                    <label style={{ display: 'block', marginBottom: 3, fontWeight: 600, color: '#334155' }}>
                      Từ ngày <span style={{ color: '#dc2626' }}>*</span>
                    </label>
                    <input
                      type="date"
                      required
                      className="tn-input"
                      style={{ width: '100%', height: 28 }}
                      value={formData.tuNgay || ''}
                      onChange={(e) => handleChange('tuNgay', e.target.value)}
                    />
                  </div>
                  <div>
                    <label style={{ display: 'block', marginBottom: 3, fontWeight: 500 }}>Số tháng</label>
                    <input
                      type="number"
                      min="0"
                      className="tn-input"
                      style={{ width: '100%', height: 28 }}
                      value={formData.soThang ?? 1}
                      onChange={(e) => handleChange('soThang', parseInt(e.target.value) || 0)}
                    />
                  </div>
                  <div>
                    <label style={{ display: 'block', marginBottom: 3, fontWeight: 500 }}>Số ngày</label>
                    <input
                      type="number"
                      min="0"
                      className="tn-input"
                      style={{ width: '100%', height: 28 }}
                      value={formData.soNgay ?? 30}
                      onChange={(e) => handleChange('soNgay', parseInt(e.target.value) || 0)}
                    />
                  </div>
                  <div>
                    <label style={{ display: 'block', marginBottom: 3, fontWeight: 600, color: '#1e3a8a' }}>
                      Đến ngày <span style={{ color: '#dc2626' }}>*</span>
                    </label>
                    <input
                      type="date"
                      required
                      className="tn-input"
                      style={{ width: '100%', height: 28, fontWeight: 700, color: '#1e3a8a' }}
                      value={formData.denNgay || ''}
                      onChange={(e) => handleChange('denNgay', e.target.value)}
                    />
                  </div>
                </div>

                <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr 1fr', gap: '10px 14px' }}>
                  <div>
                    <label style={{ display: 'block', marginBottom: 3, fontWeight: 500 }}>Ngày tặng thêm</label>
                    <input
                      type="number"
                      min="0"
                      className="tn-input"
                      style={{ width: '100%', height: 28 }}
                      value={formData.ngayTangThem ?? 0}
                      onChange={(e) => handleChange('ngayTangThem', parseInt(e.target.value) || 0)}
                    />
                  </div>
                  <div>
                    <label style={{ display: 'block', marginBottom: 3, fontWeight: 500 }}>Lần tặng thêm</label>
                    <input
                      type="number"
                      min="0"
                      className="tn-input"
                      style={{ width: '100%', height: 28 }}
                      value={formData.lanTangThem ?? 0}
                      onChange={(e) => handleChange('lanTangThem', parseInt(e.target.value) || 0)}
                    />
                  </div>
                  <div>
                    <label style={{ display: 'block', marginBottom: 3, fontWeight: 500 }}>Khuyến mãi</label>
                    <input
                      type="text"
                      className="tn-input"
                      style={{ width: '100%', height: 28 }}
                      value={formData.khuyenMai || ''}
                      onChange={(e) => handleChange('khuyenMai', e.target.value)}
                    />
                  </div>
                </div>

                <div style={{ display: 'grid', gridTemplateColumns: '1.2fr 1fr 1.2fr 1.2fr', gap: '10px 14px' }}>
                  <div>
                    <label style={{ display: 'block', marginBottom: 3, fontWeight: 500 }}>Giá gói (đ)</label>
                    <input
                      type="number"
                      min="0"
                      className="tn-input"
                      style={{ width: '100%', height: 28 }}
                      value={formData.soTien ?? 0}
                      onChange={(e) => handleChange('soTien', parseFloat(e.target.value) || 0)}
                    />
                  </div>
                  <div>
                    <label style={{ display: 'block', marginBottom: 3, fontWeight: 500 }}>Giảm (%)</label>
                    <input
                      type="number"
                      min="0"
                      max="100"
                      className="tn-input"
                      style={{ width: '100%', height: 28 }}
                      value={formData.tiLeGiamGia ?? 0}
                      onChange={(e) => handleChange('tiLeGiamGia', parseFloat(e.target.value) || 0)}
                    />
                  </div>
                  <div>
                    <label style={{ display: 'block', marginBottom: 3, fontWeight: 700, color: '#1e3a8a' }}>Tổng cộng (đ)</label>
                    <input
                      type="text"
                      readOnly
                      className="tn-input"
                      style={{ width: '100%', height: 28, background: '#f1f5f9', fontWeight: 700, color: '#1e3a8a' }}
                      value={(formData.tongCong ?? 0).toLocaleString()}
                    />
                  </div>
                  <div>
                    <label style={{ display: 'block', marginBottom: 3, fontWeight: 700, color: '#16a34a' }}>Thanh toán (đ)</label>
                    <input
                      type="number"
                      min="0"
                      className="tn-input"
                      style={{ width: '100%', height: 28, fontWeight: 700, color: '#16a34a' }}
                      value={formData.thanhToan ?? 0}
                      onChange={(e) => handleChange('thanhToan', parseFloat(e.target.value) || 0)}
                    />
                  </div>
                </div>

                <div style={{ marginTop: 4 }}>
                  <label style={{ display: 'flex', alignItems: 'center', gap: 6, cursor: 'pointer' }}>
                    <input
                      type="checkbox"
                      checked={!!formData.chuaKichHoat}
                      onChange={(e) => handleChange('chuaKichHoat', e.target.checked)}
                    />
                    <span>Kích hoạt sau (Chưa tính ngày tập cho đến khi khách quẹt thẻ lần đầu)</span>
                  </label>
                </div>
              </div>
            )}

            {/* ========================================================================= */}
            {/* 7. FORM: BẢO LƯU THẺ (KHỚP CHUẨN SFORM_Export/Bảo lưu thẻ)                */}
            {/* ========================================================================= */}
            {tabId === 'baoLuuThe' && (
              <div style={{ display: 'flex', flexDirection: 'column', gap: 12, fontSize: 12 }}>
                {/* KHUNG THÔNG TIN THẺ CẦN BẢO LƯU (REF) */}
                <div style={{ background: '#f1f5f9', border: '1px solid #cbd5e1', borderRadius: 4, padding: '10px 14px' }}>
                  <div style={{ fontWeight: 700, color: '#1e3a8a', marginBottom: 8 }}>
                    Thông tin thẻ đang sử dụng (Thẻ bảo lưu)
                  </div>
                  <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr 1fr', gap: 10 }}>
                    <div><span style={{ color: '#64748b' }}>Hội viên:</span> <strong>{customer?.tenKhachHang || '---'}</strong></div>
                    <div><span style={{ color: '#64748b' }}>Mã thẻ:</span> <strong>{customer?.maThe || '---'}</strong></div>
                    <div><span style={{ color: '#64748b' }}>Gói thẻ:</span> <strong style={{ color: '#16a34a' }}>{customer?.loaiThe || '---'}</strong></div>
                    <div><span style={{ color: '#64748b' }}>Từ ngày:</span> {customer?.tuNgay || '---'}</div>
                    <div><span style={{ color: '#64748b' }}>Hạn đến ngày:</span> <strong style={{ color: '#dc2626' }}>{customer?.denNgay || '---'}</strong></div>
                    <div><span style={{ color: '#64748b' }}>Số buổi còn:</span> <strong>{customer?.conLai ?? '---'}</strong></div>
                  </div>
                </div>

                {/* KHUNG THÔNG TIN BẢO LƯU */}
                <div style={{ background: '#ffffff', border: '1px solid #93c5fd', borderRadius: 4, padding: '12px 14px' }}>
                  <div style={{ fontWeight: 700, color: '#0369a1', marginBottom: 10 }}>
                    Thông tin thiết lập bảo lưu
                  </div>
                  <div style={{ display: 'grid', gridTemplateColumns: '1fr 0.8fr 1fr', gap: '10px 14px' }}>
                    <div>
                      <label style={{ display: 'block', marginBottom: 3, fontWeight: 600, color: '#334155' }}>
                        Bảo lưu từ ngày <span style={{ color: '#dc2626' }}>*</span>
                      </label>
                      <input
                        type="date"
                        required
                        className="tn-input"
                        style={{ width: '100%', height: 28 }}
                        value={formData.tuNgay || ''}
                        onChange={(e) => handleChange('tuNgay', e.target.value)}
                      />
                    </div>
                    <div>
                      <label style={{ display: 'block', marginBottom: 3, fontWeight: 600, color: '#334155' }}>
                        Số ngày <span style={{ color: '#dc2626' }}>*</span>
                      </label>
                      <input
                        type="number"
                        required
                        min="1"
                        max="365"
                        className="tn-input"
                        style={{ width: '100%', height: 28, fontWeight: 700 }}
                        value={formData.soNgay ?? 30}
                        onChange={(e) => handleChange('soNgay', parseInt(e.target.value) || 0)}
                      />
                    </div>
                    <div>
                      <label style={{ display: 'block', marginBottom: 3, fontWeight: 600, color: '#1e3a8a' }}>
                        Bảo lưu đến ngày <span style={{ color: '#dc2626' }}>*</span>
                      </label>
                      <input
                        type="date"
                        required
                        className="tn-input"
                        style={{ width: '100%', height: 28, fontWeight: 700, color: '#1e3a8a' }}
                        value={formData.denNgay || ''}
                        onChange={(e) => handleChange('denNgay', e.target.value)}
                      />
                    </div>
                  </div>

                  <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '10px 14px', marginTop: 10 }}>
                    <div>
                      <label style={{ display: 'block', marginBottom: 3, fontWeight: 500 }}>Phí bảo lưu (nếu có)</label>
                      <input
                        type="number"
                        min="0"
                        className="tn-input"
                        style={{ width: '100%', height: 28 }}
                        value={formData.soTien ?? 0}
                        onChange={(e) => handleChange('soTien', parseFloat(e.target.value) || 0)}
                      />
                    </div>
                    <div style={{ display: 'flex', alignItems: 'center', paddingTop: 18, color: '#0369a1', fontSize: 12 }}>
                      ℹ️ Sau thời gian bảo lưu, hạn thẻ của hội viên sẽ được tự động gia hạn thêm {formData.soNgay || 30} ngày.
                    </div>
                  </div>
                </div>
              </div>
            )}

            {/* ========================================================================= */}
            {/* CÁC TAB KHÁC (BÁO GIÁ, ĐẶT HÀNG, ĐỔI LOẠI THẺ, TĂNG GIẢM ĐIỂM)           */}
            {/* ========================================================================= */}
            {(tabId === 'datHang' || tabId === 'baoGia') && (
              <div style={{ display: 'flex', flexDirection: 'column', gap: 10, fontSize: 12 }}>
                <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr 1fr', gap: '10px 14px' }}>
                  <div>
                    <label style={{ display: 'block', marginBottom: 3, fontWeight: 500 }}>Tiền hàng</label>
                    <input
                      type="number"
                      min="0"
                      className="tn-input"
                      style={{ width: '100%', height: 28 }}
                      value={formData.tienHang ?? 0}
                      onChange={(e) => handleChange('tienHang', parseFloat(e.target.value) || 0)}
                    />
                  </div>
                  <div>
                    <label style={{ display: 'block', marginBottom: 3, fontWeight: 500 }}>Tiền giảm giá</label>
                    <input
                      type="number"
                      min="0"
                      className="tn-input"
                      style={{ width: '100%', height: 28 }}
                      value={formData.tienGiamGia ?? 0}
                      onChange={(e) => handleChange('tienGiamGia', parseFloat(e.target.value) || 0)}
                    />
                  </div>
                  <div>
                    <label style={{ display: 'block', marginBottom: 3, fontWeight: 600, color: '#1e3a8a' }}>Tổng cộng</label>
                    <input
                      type="number"
                      min="0"
                      className="tn-input"
                      style={{ width: '100%', height: 28, fontWeight: 700 }}
                      value={formData.tongCong ?? 0}
                      onChange={(e) => handleChange('tongCong', parseFloat(e.target.value) || 0)}
                    />
                  </div>
                </div>
              </div>
            )}

            {tabId === 'doiLoaiThe' && (
              <div style={{ display: 'flex', flexDirection: 'column', gap: 10, fontSize: 12 }}>
                <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '10px 14px' }}>
                  <div>
                    <label style={{ display: 'block', marginBottom: 3, fontWeight: 500 }}>Loại thẻ cũ</label>
                    <input
                      type="text"
                      readOnly
                      className="tn-input"
                      style={{ width: '100%', height: 28, background: '#f1f5f9' }}
                      value={customer?.loaiThe || '---'}
                    />
                  </div>
                  <div>
                    <label style={{ display: 'block', marginBottom: 3, fontWeight: 600, color: '#1e3a8a' }}>
                      Loại thẻ mới <span style={{ color: '#dc2626' }}>*</span>
                    </label>
                    <select
                      required
                      className="tn-input"
                      style={{ width: '100%', height: 28 }}
                      value={formData.dloaiTheIdMoi || ''}
                      onChange={(e) => handleChange('dloaiTheIdMoi', e.target.value)}
                    >
                      <option value="">-- Chọn loại thẻ mới --</option>
                      {metadata.loaiThe?.map(lt => <option key={lt.id} value={lt.id}>{lt.name}</option>)}
                    </select>
                  </div>
                </div>
                <div>
                  <label style={{ display: 'block', marginBottom: 3, fontWeight: 500 }}>Số tiền chênh lệch / Phí đổi thẻ</label>
                  <input
                    type="number"
                    min="0"
                    className="tn-input"
                    style={{ width: '100%', height: 28 }}
                    value={formData.soTien ?? 0}
                    onChange={(e) => handleChange('soTien', parseFloat(e.target.value) || 0)}
                  />
                </div>
              </div>
            )}

            {tabId === 'tangGiamDiem' && (
              <div style={{ display: 'flex', flexDirection: 'column', gap: 10, fontSize: 12 }}>
                <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '10px 14px' }}>
                  <div>
                    <label style={{ display: 'block', marginBottom: 3, fontWeight: 600, color: '#16a34a' }}>Điểm tăng (+)</label>
                    <input
                      type="number"
                      min="0"
                      className="tn-input"
                      style={{ width: '100%', height: 28 }}
                      value={formData.diemTang ?? 0}
                      onChange={(e) => handleChange('diemTang', parseInt(e.target.value) || 0)}
                    />
                  </div>
                  <div>
                    <label style={{ display: 'block', marginBottom: 3, fontWeight: 600, color: '#dc2626' }}>Điểm giảm (-)</label>
                    <input
                      type="number"
                      min="0"
                      className="tn-input"
                      style={{ width: '100%', height: 28 }}
                      value={formData.diemGiam ?? 0}
                      onChange={(e) => handleChange('diemGiam', parseInt(e.target.value) || 0)}
                    />
                  </div>
                </div>
              </div>
            )}

            {/* Ô GHI CHÚ CHUNG */}
            <div style={{ marginTop: 12 }}>
              <label style={{ display: 'block', marginBottom: 3, fontWeight: 600, color: '#334155', fontSize: 12 }}>
                Ghi chú
              </label>
              <textarea
                className="tn-input"
                style={{ width: '100%', height: 50, padding: '4px 8px', fontSize: 12, resize: 'vertical' }}
                value={formData.note || ''}
                onChange={(e) => handleChange('note', e.target.value)}
                placeholder="Nhập ghi chú thêm cho chứng từ..."
              />
            </div>
          </div>

          {/* FOOTER ACTIONS */}
          <div
            className="choice-dialog-footer"
            style={{
              display: 'flex',
              justifyContent: 'flex-end',
              gap: 8,
              padding: '10px 18px',
              background: '#f1f5f9',
              borderTop: '1px solid #e2e8f0'
            }}
          >
            <button
              type="submit"
              disabled={saving}
              className="tn-btn-primary"
              style={{
                height: 28,
                padding: '0 18px',
                fontSize: 12.5,
                background: '#1e3a8a',
                color: '#fff',
                border: 'none',
                borderRadius: 3,
                fontWeight: 600,
                cursor: 'pointer'
              }}
            >
              {saving ? 'Đang lưu...' : 'Lưu dữ liệu'}
            </button>
            <button
              type="button"
              disabled={saving}
              className="tn-btn-secondary"
              onClick={onClose}
              style={{
                height: 28,
                padding: '0 16px',
                fontSize: 12.5,
                background: '#fff',
                color: '#334155',
                border: '1px solid #cbd5e1',
                borderRadius: 3,
                cursor: 'pointer'
              }}
            >
              Bỏ qua
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
