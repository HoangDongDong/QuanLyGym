import React, { useState, useEffect } from 'react';
import { khachHangService } from '../services/khachHangService';
import Number3DInput from './Number3DInput';

export default function SubtabAeModal({
  show,
  mode = 'create',
  tabId = 'giaHanThe',
  tabLabel = 'Gia hạn thẻ',
  customer = null,
  customers = [],
  initialData = null,
  metadata = {},
  onSave,
  onClose
}) {
  const [formData, setFormData] = useState({});
  const [saving, setSaving] = useState(false);

  // Helper date add
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

  // Convert date format helper (YYYY-MM-DD <-> DD/MM/YYYY)
  const toDisplayDate = (isoStr) => {
    if (!isoStr) return '';
    if (isoStr.includes('/')) return isoStr;
    const parts = isoStr.split('-');
    if (parts.length === 3) return `${parts[2]}/${parts[1]}/${parts[0]}`;
    return isoStr;
  };

  const toIsoDate = (displayStr) => {
    if (!displayStr) return '';
    if (displayStr.includes('-')) return displayStr;
    const parts = displayStr.split('/');
    if (parts.length === 3) return `${parts[2]}-${parts[1]}-${parts[0]}`;
    return displayStr;
  };

  // Khởi tạo dữ liệu form tương ứng với từng loại subtab
  useEffect(() => {
    if (show === false) return;

    const todayIso = new Date().toISOString().split('T')[0];
    const nowTimeStr = new Date().toTimeString().slice(0, 5); // HH:mm

    if (mode === 'edit' && initialData) {
      setFormData({
        ...initialData,
        ngay: initialData.ngay ? toIsoDate(initialData.ngay) : todayIso,
        tuNgay: initialData.tuNgay ? toIsoDate(initialData.tuNgay) : todayIso,
        denNgay: initialData.denNgay ? toIsoDate(initialData.denNgay) : todayIso
      });
    } else {
      const activeCust = customer || (customers && customers[0]) || null;
      const baseData = {
        soPhiu: '',
        ngay: todayIso,
        khachHangId: activeCust?.id || '',
        maKhach: activeCust?.maThe || '',
        tenKhach: activeCust?.tenKhachHang || '',
        dienThoai: activeCust?.dienThoai || '',
        diaChi: activeCust?.diaChi || '',
        email: activeCust?.email || '',
        note: '',
        nhanVienId: activeCust?.nhanVienId || '',
        nhanVien: activeCust?.nhanVien || 'Administrator'
      };

      // Tự động sinh số phiếu chuẩn theo cấu hình hệ thống (NOTEMPLATE)
      khachHangService.generateSlipNumber(tabId).then(res => {
        if (res && res.success && res.soPhiu) {
          setFormData(prev => ({ ...prev, soPhiu: res.soPhiu }));
        }
      }).catch(err => {
        console.error('Lỗi lấy số phiếu tự động:', err);
      });

      // 1. TAB: GIA HẠN THẺ (SFORM_Export/Gia hạn thẻ)
      if (tabId === 'giaHanThe') {
        const defaultLt = metadata.loaiThe?.find(l => l.id === activeCust?.dloaiTheId) || metadata.loaiThe?.[0];
        const soThang = defaultLt?.soThang || 1;
        const soNgay = defaultLt?.soNgay || (soThang * 30);
        const giaBan = defaultLt?.giaBan || 0;
        const denNgayCalc = addDays(todayIso, soNgay);

        setFormData({
          ...baseData,
          dloaiTheId: defaultLt?.id || '',
          loaiThe: defaultLt?.name || '',
          dcatapId: activeCust?.dcatapId || (metadata.caTap?.[0]?.id || ''),
          soLan: defaultLt?.soLan || 0,
          tuNgay: todayIso,
          soThang: soThang,
          soNgay: soNgay,
          ngayTangThem: 0,
          lanTangThem: 0,
          denNgay: denNgayCalc,
          soTien: giaBan,
          tiLeGiamGia: 0,
          tienGiamGia: 0,
          datTruoc: 0,
          tongCong: giaBan,
          thanhToan: giaBan,
          khuyenMai: '',
          chuaKichHoat: false
        });
      }
      // 2. TAB: BẢO LƯU THẺ (SFORM_Export/Bảo lưu thẻ)
      else if (tabId === 'baoLuuThe') {
        const soNgay = 30;
        const denNgayCalc = addDays(todayIso, soNgay);
        setFormData({
          ...baseData,
          dloaiTheId: activeCust?.dloaiTheId || '',
          loaiThe: activeCust?.loaiThe || '',
          soPhieuRef: activeCust?.maThe || 'HD0001',
          ngayRef: activeCust?.tuNgay ? toIsoDate(activeCust.tuNgay) : todayIso,
          tuNgayRef: activeCust?.tuNgay ? toIsoDate(activeCust.tuNgay) : todayIso,
          denNgayRef: activeCust?.denNgay ? toIsoDate(activeCust.denNgay) : todayIso,
          dcatapId: activeCust?.dcatapId || '',
          tuNgay: todayIso,
          soNgay: soNgay,
          denNgay: denNgayCalc,
          note: 'Bảo lưu theo yêu cầu của hội viên'
        });
      }
      // 3. TAB: PHIẾU THU (SFORM_Export/Phiếu thu)
      else if (tabId === 'phieuThu') {
        setFormData({
          ...baseData,
          loaiDoiTuong: 2, // 2: Khách hàng
          tenDoiTuong: activeCust?.tenKhachHang || '',
          diaChi: activeCust?.diaChi || '',
          dlyDoThuChiId: metadata.lyDoThuChi?.find(l => l.laThu)?.id || '',
          dienGiai: 'Thu tiền dịch vụ',
          chungTuGoc: '',
          dnhanVienId: activeCust?.nhanVienId || (metadata.nhanVien?.[0]?.id || ''),
          dkhachHangId: activeCust?.id || '',
          dnhaCungCapId: '',
          thu: 0,
          chuyenKhoan: false,
          taiKhoanNganHangId: '',
          dcuaHangId: metadata.cuaHang?.[0]?.id || '',
          khongThayDoiCongNo: false
        });
      }
      // 4. TAB: PHIẾU CHI (SFORM_Export/Phiếu chi)
      else if (tabId === 'phieuChi') {
        setFormData({
          ...baseData,
          loaiDoiTuong: 2,
          tenDoiTuong: activeCust?.tenKhachHang || '',
          diaChi: activeCust?.diaChi || '',
          dlyDoThuChiId: metadata.lyDoThuChi?.find(l => l.laChi)?.id || '',
          dienGiai: 'Chi tiền hội viên / dịch vụ',
          chungTuGoc: '',
          dnhanVienId: activeCust?.nhanVienId || (metadata.nhanVien?.[0]?.id || ''),
          dkhachHangId: activeCust?.id || '',
          dnhaCungCapId: '',
          chi: 0,
          chuyenKhoan: false,
          taiKhoanNganHangId: '',
          dcuaHangId: metadata.cuaHang?.[0]?.id || '',
          khongThayDoiCongNo: false
        });
      }
      // 5. TAB: ĐẶT CỌC (SFORM_Export/Đặt cọc)
      else if (tabId === 'datCoc') {
        const defaultLt = metadata.loaiThe?.[0];
        const giaTriGoi = defaultLt?.giaBan || 1000000;
        setFormData({
          ...baseData,
          tenDoiTuong: activeCust?.tenKhachHang || '',
          diaChi: activeCust?.diaChi || '',
          dienThoai: activeCust?.dienThoai || '',
          dlyDoThuChiId: metadata.lyDoThuChi?.find(l => l.name?.toLowerCase().includes('cọc') || l.name?.toLowerCase().includes('đặt'))?.id || '',
          dloaiTheId: defaultLt?.id || '',
          giaTriGoi: giaTriGoi,
          giamGia: 0,
          tienGiam: 0,
          tongCong: giaTriGoi,
          thu: 500000,
          tongDat: 500000,
          chuyenKhoan: false,
          taiKhoanNganHangId: ''
        });
      }
      // 6. TAB: PHIẾU THU CÔNG NỢ (SFORM_Export/Phiếu thu công nợ)
      else if (tabId === 'thuCongNo') {
        setFormData({
          ...baseData,
          dnhanVienId: activeCust?.nhanVienId || (metadata.nhanVien?.[0]?.id || ''),
          dienGiai: 'Thu công nợ khách hàng',
          dlyDoThuChiId: metadata.lyDoThuChi?.find(l => l.name?.toLowerCase().includes('nợ'))?.id || '',
          chungTuGoc: '',
          thu: 0,
          chuyenKhoan: false
        });
      }
      // 7. TAB: ĐƠN HÀNG (SFORM_Export/Đơn hàng)
      else if (tabId === 'donHang') {
        setFormData({
          ...baseData,
          gioThanhToan: nowTimeStr,
          dkhoXuatId: metadata.khoHang?.[0]?.id || '',
          dnhanVienXuatId: metadata.nhanVien?.[0]?.id || '',
          userThanhToanId: 'Administrator',
          giaoHang: '',
          dienGiai: '',
          tienHang: 0,
          phiVanChuyen: 0,
          tiLeGiamGia: 0,
          tienGiamGia: 0,
          tiLeThue: 0,
          tienThue: 0,
          doiTra: 0,
          tongCong: 0,
          thanhToan: 0,
          conLai: 0
        });
      }
      // 8. TAB: ĐỔI LOẠI THẺ (SFORM_Export/Đổi loại thẻ)
      else if (tabId === 'doiLoaiThe') {
        const curLt = metadata.loaiThe?.find(l => l.name === activeCust?.loaiThe || l.id === activeCust?.dloaiTheId) || metadata.loaiThe?.[0];
        const nextLt = metadata.loaiThe?.find(l => l.id !== curLt?.id) || metadata.loaiThe?.[1] || curLt;
        const curGia = curLt?.giaBan || 0;
        const nextGia = nextLt?.giaBan || curGia;
        const diff = Math.max(0, nextGia - curGia);
        const soThang = nextLt?.soThang || 1;
        const soNgay = nextLt?.soNgay || (soThang * 30);
        const tuNgay = activeCust?.tuNgay ? toIsoDate(activeCust.tuNgay) : todayIso;
        const denNgayCalc = addDays(tuNgay, soNgay);

        setFormData({
          ...baseData,
          // Thông tin thẻ cũ (Panel1)
          ngayRef: activeCust?.tuNgay ? toIsoDate(activeCust.tuNgay) : todayIso,
          loaiTheRef: activeCust?.loaiThe || curLt?.name || '',
          tuNgayRef: activeCust?.tuNgay ? toIsoDate(activeCust.tuNgay) : todayIso,
          denNgayRef: activeCust?.denNgay ? toIsoDate(activeCust.denNgay) : todayIso,
          soTienRef: curGia,
          tiLeGiamRef: 0,
          tienGiamRef: 0,
          tongCongRef: curGia,
          soLanRef: activeCust?.soLan || curLt?.soLan || 0,
          caTapRef: activeCust?.caTap || (metadata.caTap?.[0]?.name || ''),
          refId: activeCust?.tGiaHanTheId || '',

          // Thông tin thẻ mới
          dloaiTheId: nextLt?.id || '',
          tuNgay: tuNgay,
          denNgay: denNgayCalc,
          soLan: nextLt?.soLan || 0,
          soThang: soThang,
          soNgay: soNgay,
          ngayTangThem: 0,
          lanTangThem: 0,
          soTien: nextGia,
          tiLeGiamGia: 0,
          tienGiamGia: 0,
          numTruTheCu: curGia,
          tongCong: diff,
          thanhToan: diff,
          dcatapId: activeCust?.dcatapId || (metadata.caTap?.[0]?.id || ''),
          note: 'Đổi sang thẻ ' + (nextLt?.name || '')
        });
      }
      // 9. TAB: BÁO GIÁ (STABLEDESC TBAOGIA)
      else if (tabId === 'baoGia') {
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
      }
      // 10. TAB: ĐẶT HÀNG (STABLEDESC TDATHANG)
      else if (tabId === 'datHang') {
        setFormData({
          ...baseData,
          loaiGia: 'Giá bán lẻ',
          tienHang: 0,
          tiLeGiamGia: 0,
          tienGiamGia: 0,
          tiLeThue: 0,
          tienThue: 0,
          phiVanChuyen: 0,
          tongCong: 0,
          soPhieuThu: '',
          soTienDat: 0,
          soHoaDon: ''
        });
      }
      // 11. TAB: TĂNG GIẢM ĐIỂM (STABLEDESC TTANGGIAMDIEM)
      else if (tabId === 'tangGiamDiem') {
        setFormData({
          ...baseData,
          diemTang: 10,
          diemGiam: 0,
          lyDo: 'Tích điểm thành viên',
          note: 'Thưởng tích lũy điểm hội viên'
        });
      }
      // 12. TAB: VÀO RA (STABLEDESC TVAORA)
      else if (tabId === 'vaoRa') {
        setFormData({
          ...baseData,
          dmayVanTayId: metadata.mayVanTay?.[0]?.id || '',
          gio: nowTimeStr,
          khan: '',
          tu: '',
          note: 'Quẹt thẻ vào phòng tập'
        });
      }
      else {
        setFormData(baseData);
      }
    }
  }, [show, mode, tabId, customer, initialData, metadata, customers]);

  if (!show) return null;

  // Lấy thông tin khách khi chọn combobox khách hàng
  const handleSelectCustomer = (custId) => {
    const cust = customers.find(c => c.id === custId) || customer;
    if (!cust) return;

    setFormData(prev => {
      const updated = {
        ...prev,
        khachHangId: cust.id,
        maKhach: cust.maThe || '',
        tenKhach: cust.tenKhachHang || cust.name || '',
        tenDoiTuong: cust.tenKhachHang || cust.name || '',
        diaChi: cust.diaChi || '',
        dienThoai: cust.dienThoai || '',
        dloaiTheId: cust.dloaiTheId || prev.dloaiTheId,
        dcatapId: cust.dcatapId || prev.dcatapId
      };

      if (tabId === 'giaHanThe') {
        const lt = metadata.loaiThe?.find(l => l.id === (cust.dloaiTheId || prev.dloaiTheId));
        if (lt) {
          updated.soThang = lt.soThang || 1;
          updated.soNgay = lt.soNgay || (lt.soThang * 30 || 30);
          updated.soLan = lt.soLan || 0;
          updated.soTien = lt.giaBan || 0;
          updated.tongCong = lt.giaBan || 0;
          updated.thanhToan = lt.giaBan || 0;
          updated.denNgay = addDays(updated.tuNgay || prev.tuNgay, updated.soNgay);
        }
      } else if (tabId === 'doiLoaiThe') {
        const curLt = metadata.loaiThe?.find(l => l.name === cust.loaiThe || l.id === cust.dloaiTheId) || metadata.loaiThe?.[0];
        const curGia = curLt?.giaBan || 0;
        updated.loaiTheRef = cust.loaiThe || curLt?.name || '';
        updated.tuNgayRef = cust.tuNgay ? toIsoDate(cust.tuNgay) : (prev.ngay || '');
        updated.denNgayRef = cust.denNgay ? toIsoDate(cust.denNgay) : (prev.ngay || '');
        updated.soTienRef = curGia;
        updated.tongCongRef = curGia;
        updated.soLanRef = cust.soLan || curLt?.soLan || 0;
        updated.caTapRef = cust.caTap || '';
        updated.numTruTheCu = curGia;
        const st = Number(prev.soTien) || 0;
        const tg = Number(prev.tienGiamGia) || 0;
        const tc = Math.max(0, st - tg - curGia);
        updated.tongCong = tc;
        updated.thanhToan = tc;
      }
      return updated;
    });
  };

  // Cập nhật giá trị và tự động tính toán nghiệp vụ
  const handleChange = (field, value) => {
    setFormData(prev => {
      const updated = { ...prev, [field]: value };

      // 1. Gia hạn thẻ
      if (tabId === 'giaHanThe') {
        if (field === 'dloaiTheId') {
          const lt = metadata.loaiThe?.find(l => l.id === value);
          if (lt) {
            updated.soTien = lt.giaBan || 0;
            updated.soThang = lt.soThang || 1;
            updated.soNgay = lt.soNgay || (lt.soThang * 30 || 30);
            updated.soLan = lt.soLan || 0;
            const tg = Math.round((lt.giaBan || 0) * (Number(prev.tiLeGiamGia) || 0) / 100);
            updated.tienGiamGia = tg;
            updated.tongCong = (lt.giaBan || 0) - tg;
            updated.thanhToan = (lt.giaBan || 0) - tg;
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
        } else if (field === 'tienGiamGia') {
          const st = Number(prev.soTien) || 0;
          const tg = Number(value) || 0;
          updated.tongCong = st - tg;
          updated.thanhToan = st - tg;
          updated.tiLeGiamGia = st > 0 ? Math.round((tg / st) * 100) : 0;
        }
      }

      // 2. Bảo lưu thẻ
      if (tabId === 'baoLuuThe') {
        if (field === 'tuNgay' || field === 'soNgay') {
          const tn = field === 'tuNgay' ? value : prev.tuNgay;
          const sn = field === 'soNgay' ? Number(value) || 0 : Number(prev.soNgay) || 0;
          updated.denNgay = addDays(tn, sn);
        }
      }

      // 3. Đặt cọc
      if (tabId === 'datCoc') {
        if (field === 'dloaiTheId') {
          const lt = metadata.loaiThe?.find(l => l.id === value);
          if (lt) {
            updated.giaTriGoi = lt.giaBan || 0;
            const gg = Number(prev.giamGia) || 0;
            const tg = Math.round(lt.giaBan * gg / 100);
            updated.tienGiam = tg;
            updated.tongCong = lt.giaBan - tg;
          }
        } else if (field === 'giamGia' || field === 'giaTriGoi') {
          const gtg = field === 'giaTriGoi' ? Number(value) || 0 : Number(prev.giaTriGoi) || 0;
          const gg = field === 'giamGia' ? Number(value) || 0 : Number(prev.giamGia) || 0;
          const tg = Math.round(gtg * gg / 100);
          updated.tienGiam = tg;
          updated.tongCong = gtg - tg;
        } else if (field === 'thu') {
          updated.tongDat = Number(value) || 0;
        }
      }

      // 4. Đơn hàng
      if (tabId === 'donHang') {
        const th = field === 'tienHang' ? Number(value) || 0 : Number(prev.tienHang) || 0;
        const tg = field === 'tienGiamGia' ? Number(value) || 0 : Number(prev.tienGiamGia) || 0;
        const tt = field === 'tienThue' ? Number(value) || 0 : Number(prev.tienThue) || 0;
        const pvc = field === 'phiVanChuyen' ? Number(value) || 0 : Number(prev.phiVanChuyen) || 0;
        const dt = field === 'doiTra' ? Number(value) || 0 : Number(prev.doiTra) || 0;
        const tong = th - tg + tt + pvc - dt;
        const ttPay = field === 'thanhToan' ? Number(value) || 0 : Number(prev.thanhToan) || 0;
        updated.tongCong = tong;
        updated.conLai = tong - ttPay;
      }

      // 5. Đổi loại thẻ (SFORM_Export/Đổi loại thẻ/Code.cs)
      if (tabId === 'doiLoaiThe') {
        if (field === 'dloaiTheId') {
          const lt = metadata.loaiThe?.find(l => l.id === value);
          if (lt) {
            const st = lt.giaBan || 0;
            const sn = lt.soNgay || (lt.soThang * 30 || 30);
            const tru = Number(prev.numTruTheCu) || 0;
            const tg = Math.round(st * (Number(prev.tiLeGiamGia) || 0) / 100);
            const tc = Math.max(0, st - tg - tru);
            updated.soTien = st;
            updated.soLan = lt.soLan || 0;
            updated.soThang = lt.soThang || 1;
            updated.soNgay = sn;
            updated.tienGiamGia = tg;
            updated.tongCong = tc;
            updated.thanhToan = tc;
            updated.denNgay = addDays(updated.tuNgay || prev.tuNgay, sn + (Number(prev.ngayTangThem) || 0));
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
          const tru = Number(prev.numTruTheCu) || 0;
          const tc = Math.max(0, st - tg - tru);
          updated.tienGiamGia = tg;
          updated.tongCong = tc;
          updated.thanhToan = tc;
        } else if (field === 'tienGiamGia') {
          const st = Number(prev.soTien) || 0;
          const tg = Number(value) || 0;
          const tru = Number(prev.numTruTheCu) || 0;
          const tc = Math.max(0, st - tg - tru);
          updated.tongCong = tc;
          updated.thanhToan = tc;
          updated.tiLeGiamGia = st > 0 ? Math.round((tg / st) * 100) : 0;
        }
      }

      // 6. Báo giá & Đặt hàng (STABLEDESC TBAOGIA / TDATHANG)
      if (tabId === 'baoGia' || tabId === 'datHang') {
        const th = field === 'tienHang' ? Number(value) || 0 : Number(prev.tienHang) || 0;
        let tlGiam = field === 'tiLeGiamGia' ? Number(value) || 0 : Number(prev.tiLeGiamGia) || 0;
        let tGiam = field === 'tienGiamGia' ? Number(value) || 0 : Number(prev.tienGiamGia) || 0;
        if (field === 'tiLeGiamGia') {
          tGiam = Math.round(th * tlGiam / 100);
          updated.tienGiamGia = tGiam;
        } else if (field === 'tienGiamGia') {
          tlGiam = th > 0 ? Math.round((tGiam / th) * 100) : 0;
          updated.tiLeGiamGia = tlGiam;
        }
        let tlThue = field === 'tiLeThue' ? Number(value) || 0 : Number(prev.tiLeThue) || 0;
        let tThue = field === 'tienThue' ? Number(value) || 0 : Number(prev.tienThue) || 0;
        if (field === 'tiLeThue') {
          tThue = Math.round((th - tGiam) * tlThue / 100);
          updated.tienThue = tThue;
        } else if (field === 'tienThue') {
          tlThue = (th - tGiam) > 0 ? Math.round((tThue / (th - tGiam)) * 100) : 0;
          updated.tiLeThue = tlThue;
        }
        const pvc = field === 'phiVanChuyen' ? Number(value) || 0 : Number(prev.phiVanChuyen) || 0;
        const tc = th - tGiam + tThue + pvc;
        updated.tongCong = tc;
      }

      return updated;
    });
  };

  const handleSubmit = async (e) => {
    if (e && e.preventDefault) e.preventDefault();
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

  // Xác định tiêu đề chuẩn theo screenshot WinForms
  const formTitles = {
    giaHanThe: 'GIA HẠN THẺ',
    baoLuuThe: 'BẢO LƯU THẺ',
    phieuThu: 'PHIẾU THU',
    phieuChi: 'PHIẾU CHI',
    datCoc: 'ĐẶT CỌC',
    thuCongNo: 'PHIẾU THU CÔNG NỢ',
    donHang: 'ĐƠN HÀNG',
    doiLoaiThe: 'ĐỔI LOẠI THẺ',
    baoGia: 'BÁO GIÁ',
    datHang: 'ĐẶT HÀNG',
    tangGiamDiem: 'TĂNG GIẢM ĐIỂM',
    vaoRa: 'VÀO RA'
  };

  const bannerTitle = formTitles[tabId] || tabLabel.toUpperCase();
  const windowTitle = `${bannerTitle} - ${mode === 'create' ? 'THÊM MỚI' : 'CHỈNH SỬA'}`;

  // Kích thước canvas chuẩn từ AELayout.xml và STABLEDESC
  const formDimensions = {
    giaHanThe: { width: 564, height: 433 },
    baoLuuThe: { width: 541, height: 432 },
    phieuThu: { width: 527, height: 382 },
    phieuChi: { width: 527, height: 382 },
    datCoc: { width: 514, height: 330 },
    thuCongNo: { width: 590, height: 454 },
    donHang: { width: 660, height: 510 },
    doiLoaiThe: { width: 570, height: 487 },
    baoGia: { width: 921, height: 451 },
    datHang: { width: 1020, height: 451 },
    tangGiamDiem: { width: 370, height: 233 },
    vaoRa: { width: 400, height: 205 }
  };

  const dim = formDimensions[tabId] || { width: 564, height: 433 };

  // Common inline style definitions matching exact WinForms DevExpress / Krypton
  const lblStyle = {
    position: 'absolute',
    fontFamily: "Tahoma, 'Segoe UI', Arial, sans-serif",
    fontSize: '11px',
    color: '#000000',
    lineHeight: '22px',
    userSelect: 'none',
    whiteSpace: 'nowrap'
  };

  const inputStyle = (extra = {}) => ({
    position: 'absolute',
    height: '21px',
    boxSizing: 'border-box',
    border: '1px solid #7192b8',
    borderRadius: '1px',
    padding: '1px 5px',
    fontFamily: "Tahoma, 'Segoe UI', Arial, sans-serif",
    fontSize: '11px',
    color: '#000000',
    background: '#ffffff',
    outline: 'none',
    ...extra
  });

  const yellowInputStyle = (extra = {}) => inputStyle({
    background: '#ffffd5', // Pale yellow for mandatory / key inputs
    ...extra
  });

  return (
    <div
      className="choice-dialog-backdrop"
      onClick={onClose}
      style={{
        position: 'fixed',
        top: 0,
        left: 0,
        right: 0,
        bottom: 0,
        background: 'rgba(0, 0, 0, 0.45)',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        zIndex: 1050
      }}
    >
      <div
        className="winforms-window"
        onClick={(e) => e.stopPropagation()}
        style={{
          width: dim.width + 2,
          maxWidth: '98vw',
          background: '#cbdbe8',
          border: '1px solid #5a82a6',
          boxShadow: '0 8px 30px rgba(0, 0, 0, 0.5)',
          borderRadius: '4px 4px 0 0',
          overflow: 'hidden',
          display: 'flex',
          flexDirection: 'column'
        }}
      >
        {/* ========================================================================= */}
        {/* 1. WINDOW TITLE BAR (WINFORMS AERO / CLASSIC)                             */}
        {/* ========================================================================= */}
        <div
          style={{
            height: 28,
            background: 'linear-gradient(180deg, #eef5fc 0%, #bdd3e8 100%)',
            borderBottom: '1px solid #8caec7',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'space-between',
            padding: '0 4px 0 8px',
            userSelect: 'none'
          }}
        >
          <div style={{ display: 'flex', alignItems: 'center', gap: 6 }}>
            {/* Small WinForms icon */}
            <div
              style={{
                width: 16,
                height: 14,
                background: 'linear-gradient(180deg, #38bdf8 0%, #0284c7 100%)',
                border: '1px solid #0369a1',
                borderRadius: 2,
                boxShadow: 'inset 0 1px 0 rgba(255,255,255,0.6)'
              }}
            />
            <span style={{ fontSize: 11.5, fontWeight: 700, color: '#0f2942', letterSpacing: '0.2px' }}>
              {windowTitle}
            </span>
          </div>

          <div style={{ display: 'flex', alignItems: 'center' }}>
            <button
              onClick={onClose}
              style={{
                width: 28,
                height: 20,
                background: 'transparent',
                border: 'none',
                color: '#334155',
                fontSize: 12,
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                cursor: 'pointer',
                borderRadius: 2
              }}
              onMouseEnter={(e) => {
                e.currentTarget.style.background = '#e81123';
                e.currentTarget.style.color = '#ffffff';
              }}
              onMouseLeave={(e) => {
                e.currentTarget.style.background = 'transparent';
                e.currentTarget.style.color = '#334155';
              }}
            >
              ✕
            </button>
          </div>
        </div>

        {/* ========================================================================= */}
        {/* 2. FORM BANNER (HEADER STRIP VỚI ICON & TIÊU ĐỀ IN ĐẬM)                  */}
        {/* ========================================================================= */}
        <div
          style={{
            height: 44,
            background: 'linear-gradient(180deg, #ffffff 0%, #e2edf7 100%)',
            borderBottom: '1px solid #b5ccdf',
            display: 'flex',
            alignItems: 'center',
            padding: '0 12px',
            gap: 12
          }}
        >
          {/* Cyan/Blue Card Graphic Badge */}
          <div
            style={{
              width: 32,
              height: 24,
              background: 'linear-gradient(135deg, #38bdf8 0%, #0ea5e9 100%)',
              borderRadius: 3,
              border: '1px solid #0284c7',
              boxShadow: '0 1px 3px rgba(0,0,0,0.15)',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              color: '#ffffff',
              fontSize: 14,
              fontWeight: 700
            }}
          >
            💳
          </div>
          <span style={{ fontSize: 14, fontWeight: 700, color: '#1e293b', fontFamily: "Tahoma, 'Segoe UI', sans-serif" }}>
            {bannerTitle}
          </span>
        </div>

        {/* ========================================================================= */}
        {/* 3. TOOLSTRIP (CÁC NÚT THAO TÁC NHANH: PHÍM TẮT, TRƯỚC, SAU, TẠO MỚI)     */}
        {/* ========================================================================= */}
        <div
          style={{
            height: 25,
            background: 'linear-gradient(180deg, #f2f7fc 0%, #d8e5f2 100%)',
            borderBottom: '1px solid #aec4d9',
            display: 'flex',
            alignItems: 'center',
            padding: '0 6px',
            gap: 4,
            fontSize: 11,
            color: '#1e293b'
          }}
        >
          <button type="button" className="wf-tool-btn" style={{ background: 'transparent', border: '1px solid transparent', padding: '1px 5px', fontSize: 11, cursor: 'pointer', display: 'flex', alignItems: 'center', gap: 3 }}>
            <span>⌨️</span> Phím tắt ▾
          </button>
          <div style={{ width: 1, height: 14, background: '#c1d4e6', margin: '0 2px' }} />
          <button type="button" className="wf-tool-btn" style={{ background: 'transparent', border: '1px solid transparent', padding: '1px 5px', fontSize: 11, cursor: 'pointer', display: 'flex', alignItems: 'center', gap: 3, opacity: 0.6 }}>
            <span>⬅</span> Trước (F10)
          </button>
          <button type="button" className="wf-tool-btn" style={{ background: 'transparent', border: '1px solid transparent', padding: '1px 5px', fontSize: 11, cursor: 'pointer', display: 'flex', alignItems: 'center', gap: 3, opacity: 0.6 }}>
            <span>➡</span> Sau (F11)
          </button>
          <div style={{ width: 1, height: 14, background: '#c1d4e6', margin: '0 2px' }} />
          <button type="button" className="wf-tool-btn" style={{ background: 'transparent', border: '1px solid transparent', padding: '1px 5px', fontSize: 11, cursor: 'pointer', display: 'flex', alignItems: 'center', gap: 3 }}>
            <span>📄</span> Tạo mới
          </button>
          <button type="button" className="wf-tool-btn" style={{ background: 'transparent', border: '1px solid transparent', padding: '1px 5px', fontSize: 11, cursor: 'pointer', display: 'flex', alignItems: 'center', gap: 3, opacity: 0.6 }}>
            <span>📑</span> Sao chép
          </button>
        </div>

        {/* ========================================================================= */}
        {/* 4. FORM CANVAS: EXACT LOCATIONS & SIZES FROM AELAYOUT.XML                 */}
        {/* ========================================================================= */}
        <div
          style={{
            position: 'relative',
            width: dim.width,
            height: dim.height,
            background: '#cbdbe8',
            overflow: 'hidden'
          }}
        >
          {/* ----------------------------------------------------------------------- */}
          {/* FORM 1: GIA HẠN THẺ (Khớp 100% hình ảnh thực tế của người dùng)         */}
          {/* ----------------------------------------------------------------------- */}
          {tabId === 'giaHanThe' && (
            <>
              {/* Ngày & Số phiếu */}
              <div style={{ ...lblStyle, left: 10, top: 11, width: 41 }}>Ngày</div>
              <input
                type="date"
                required
                style={yellowInputStyle({ left: 118, top: 9, width: 128 })}
                value={formData.ngay || ''}
                onChange={(e) => handleChange('ngay', e.target.value)}
              />
              <div style={{ ...lblStyle, left: 327, top: 13, width: 61 }}>Số phiếu</div>
              <input
                type="text"
                required
                style={inputStyle({ left: 413, top: 10, width: 140, fontWeight: 700, background: '#f0fbfb', color: '#1e3a8a' })}
                value={formData.soPhiu || ''}
                onChange={(e) => handleChange('soPhiu', e.target.value)}
              />

              {/* Khách hàng (Dropdown) */}
              <div style={{ ...lblStyle, left: 10, top: 39, width: 78 }}>Khách hàng</div>
              <select
                style={yellowInputStyle({ left: 118, top: 37, width: 435, height: 23, fontWeight: 600 })}
                value={formData.khachHangId || ''}
                onChange={(e) => handleSelectCustomer(e.target.value)}
              >
                <option value="">-- Chọn khách hàng --</option>
                {customers && customers.length > 0 ? (
                  customers.map(c => (
                    <option key={c.id} value={c.id}>
                      {c.maThe ? `[${c.maThe}] ` : ''}{c.tenKhachHang || c.name} - {c.dienThoai || ''}
                    </option>
                  ))
                ) : (
                  customer && <option value={customer.id}>[{customer.maThe}] {customer.tenKhachHang}</option>
                )}
              </select>

              {/* Tên khách */}
              <div style={{ ...lblStyle, left: 10, top: 68, width: 71 }}>Tên khách</div>
              <input
                type="text"
                readOnly
                style={inputStyle({ left: 118, top: 66, width: 435, background: '#f8fafc' })}
                value={formData.tenKhach || ''}
              />

              {/* Địa chỉ */}
              <div style={{ ...lblStyle, left: 10, top: 96, width: 48 }}>Địa chỉ</div>
              <input
                type="text"
                style={inputStyle({ left: 118, top: 94, width: 435 })}
                value={formData.diaChi || ''}
                onChange={(e) => handleChange('diaChi', e.target.value)}
              />

              {/* Điện thoại */}
              <div style={{ ...lblStyle, left: 10, top: 124, width: 67 }}>Điện thoại</div>
              <input
                type="text"
                style={inputStyle({ left: 118, top: 122, width: 435 })}
                value={formData.dienThoai || ''}
                onChange={(e) => handleChange('dienThoai', e.target.value)}
              />

              {/* Mã thẻ */}
              <div style={{ ...lblStyle, left: 10, top: 152, width: 48 }}>Mã thẻ</div>
              <input
                type="text"
                style={inputStyle({ left: 118, top: 150, width: 435 })}
                value={formData.maKhach || ''}
                onChange={(e) => handleChange('maKhach', e.target.value)}
              />

              {/* Loại thẻ */}
              <div style={{ ...lblStyle, left: 10, top: 180, width: 55 }}>Loại thẻ</div>
              <select
                style={yellowInputStyle({ left: 118, top: 178, width: 435, height: 23, fontWeight: 600 })}
                value={formData.dloaiTheId || ''}
                onChange={(e) => handleChange('dloaiTheId', e.target.value)}
              >
                <option value="">-- Chọn loại thẻ / Gói tập --</option>
                {metadata.loaiThe?.map(lt => (
                  <option key={lt.id} value={lt.id}>
                    {lt.name} ({lt.giaBan?.toLocaleString()} đ - {lt.soThang || 1} tháng)
                  </option>
                ))}
              </select>

              {/* Từ ngày, Đến ngày, Số lần tập */}
              <div style={{ ...lblStyle, left: 10, top: 209, width: 57 }}>Từ ngày</div>
              <input
                type="date"
                required
                style={yellowInputStyle({ left: 118, top: 207, width: 101 })}
                value={formData.tuNgay || ''}
                onChange={(e) => handleChange('tuNgay', e.target.value)}
              />
              <div style={{ ...lblStyle, left: 225, top: 209, width: 65 }}>Đến ngày</div>
              <input
                type="date"
                required
                style={yellowInputStyle({ left: 295, top: 207, width: 103, fontWeight: 700 })}
                value={formData.denNgay || ''}
                onChange={(e) => handleChange('denNgay', e.target.value)}
              />
              <div style={{ ...lblStyle, left: 403, top: 209, width: 68 }}>Số lần tập</div>
              <input
                type="number"
                min="0"
                style={inputStyle({ left: 477, top: 207, width: 76, textAlign: 'right' })}
                value={formData.soLan ?? 0}
                onChange={(e) => handleChange('soLan', parseInt(e.target.value) || 0)}
              />

              {/* Ngày tặng thêm, Lần tặng thêm */}
              <div style={{ ...lblStyle, left: 10, top: 237, width: 102 }}>Ngày tặng thêm</div>
              <input
                type="number"
                min="0"
                style={inputStyle({ left: 118, top: 235, width: 101, textAlign: 'right' })}
                value={formData.ngayTangThem ?? 0}
                onChange={(e) => handleChange('ngayTangThem', parseInt(e.target.value) || 0)}
              />
              <div style={{ ...lblStyle, left: 225, top: 237, width: 91 }}>Lần tặng thêm</div>
              <input
                type="number"
                min="0"
                style={inputStyle({ left: 337, top: 235, width: 55, textAlign: 'right' })}
                value={formData.lanTangThem ?? 0}
                onChange={(e) => handleChange('lanTangThem', parseInt(e.target.value) || 0)}
              />

              {/* Ca tập */}
              <div style={{ ...lblStyle, left: 10, top: 265, width: 47 }}>Ca tập</div>
              <select
                style={inputStyle({ left: 118, top: 263, width: 435, height: 23 })}
                value={formData.dcatapId || ''}
                onChange={(e) => handleChange('dcatapId', e.target.value)}
              >
                <option value="">-- Cả ngày (Mặc định) --</option>
                {metadata.caTap?.map(ct => (
                  <option key={ct.id} value={ct.id}>{ct.name}</option>
                ))}
              </select>

              {/* Số tiền & Checkbox Kích hoạt sau */}
              <div style={{ ...lblStyle, left: 10, top: 292, width: 49 }}>Số tiền</div>
              <Number3DInput
                style={{ left: 118, top: 291, width: 128, fontWeight: 600 }}
                value={formData.soTien ?? 0}
                onChange={(num) => handleChange('soTien', num)}
              />
              <label style={{ position: 'absolute', left: 425, top: 292, display: 'flex', alignItems: 'center', gap: 6, fontSize: 11, cursor: 'pointer', userSelect: 'none' }}>
                <input
                  type="checkbox"
                  checked={!!formData.chuaKichHoat}
                  onChange={(e) => handleChange('chuaKichHoat', e.target.checked)}
                />
                <span>Kích hoạt sau</span>
              </label>

              {/* Giảm giá (%) & Đặt trước */}
              <div style={{ ...lblStyle, left: 10, top: 320, width: 62 }}>Giảm giá</div>
              <Number3DInput
                style={{ left: 118, top: 318, width: 34 }}
                value={formData.tiLeGiamGia ?? 0}
                onChange={(num) => handleChange('tiLeGiamGia', num)}
              />
              <div style={{ ...lblStyle, left: 158, top: 320, width: 20 }}>%</div>
              <Number3DInput
                style={{ left: 184, top: 318, width: 62 }}
                value={formData.tienGiamGia ?? 0}
                onChange={(num) => handleChange('tienGiamGia', num)}
              />
              <div style={{ ...lblStyle, left: 317, top: 320, width: 60 }}>Đặt trước</div>
              <Number3DInput
                style={{ left: 425, top: 317, width: 128 }}
                value={formData.datTruoc ?? 0}
                onChange={(num) => handleChange('datTruoc', num)}
              />

              {/* Tổng cộng & Thanh toán */}
              <div style={{ ...lblStyle, left: 10, top: 346, width: 73 }}>Tổng cộng</div>
              <Number3DInput
                readOnly
                isYellow
                style={{ left: 118, top: 344, width: 128, fontWeight: 700 }}
                value={formData.tongCong ?? 0}
              />
              <div style={{ ...lblStyle, left: 317, top: 346, width: 75 }}>Thanh toán</div>
              <Number3DInput
                style={{ left: 425, top: 344, width: 128, fontWeight: 600 }}
                value={formData.thanhToan ?? 0}
                onChange={(num) => handleChange('thanhToan', num)}
              />

              {/* Khuyến mãi */}
              <div style={{ ...lblStyle, left: 10, top: 374, width: 77 }}>Khuyến mãi</div>
              <input
                type="text"
                style={inputStyle({ left: 118, top: 372, width: 435 })}
                value={formData.khuyenMai || ''}
                onChange={(e) => handleChange('khuyenMai', e.target.value)}
              />

              {/* Ghi chú */}
              <div style={{ ...lblStyle, left: 10, top: 402, width: 52 }}>Ghi chú</div>
              <input
                type="text"
                style={inputStyle({ left: 118, top: 400, width: 435 })}
                value={formData.note || ''}
                onChange={(e) => handleChange('note', e.target.value)}
              />
            </>
          )}

          {/* ----------------------------------------------------------------------- */}
          {/* FORM 2: BẢO LƯU THẺ (Khớp AELayout.xml Bảo lưu thẻ 541x432)             */}
          {/* ----------------------------------------------------------------------- */}
          {tabId === 'baoLuuThe' && (
            <>
              {/* Ngày & Số phiếu */}
              <div style={{ ...lblStyle, left: 10, top: 13, width: 41 }}>Ngày</div>
              <input
                type="date"
                required
                style={yellowInputStyle({ left: 125, top: 11, width: 95 })}
                value={formData.ngay || ''}
                onChange={(e) => handleChange('ngay', e.target.value)}
              />
              <div style={{ ...lblStyle, left: 320, top: 13, width: 61 }}>Số phiếu</div>
              <input
                type="text"
                required
                style={inputStyle({ left: 410, top: 11, width: 123, fontWeight: 700, background: '#f0fbfb', color: '#1e3a8a' })}
                value={formData.soPhiu || ''}
                onChange={(e) => handleChange('soPhiu', e.target.value)}
              />

              {/* Khách hàng */}
              <div style={{ ...lblStyle, left: 10, top: 41, width: 78 }}>Khách hàng</div>
              <select
                style={yellowInputStyle({ left: 125, top: 39, width: 408, height: 21, fontWeight: 600 })}
                value={formData.khachHangId || ''}
                onChange={(e) => handleSelectCustomer(e.target.value)}
              >
                <option value="">-- Chọn khách hàng --</option>
                {customers?.map(c => (
                  <option key={c.id} value={c.id}>[{c.maThe}] {c.tenKhachHang || c.name}</option>
                )) || <option value={customer?.id}>[{customer?.maThe}] {customer?.tenKhachHang}</option>}
              </select>

              {/* Groupbox: Thông tin bảo lưu (Panel1) */}
              <div style={{ ...lblStyle, left: 10, top: 70, width: 110, fontWeight: 600 }}>Thông tin bảo lưu</div>
              <div
                style={{
                  position: 'absolute',
                  left: 125,
                  top: 66,
                  width: 408,
                  height: 169,
                  border: '1px solid #7192b8',
                  background: '#d8e5f2',
                  padding: 8
                }}
              >
                {/* Ngày gốc */}
                <div style={{ position: 'absolute', left: 14, top: 8, fontSize: 11 }}>Ngày</div>
                <input type="date" readOnly style={inputStyle({ left: 72, top: 5, width: 95, background: '#f1f5f9' })} value={formData.ngayRef || formData.ngay || ''} />

                {/* Số phiếu gốc */}
                <div style={{ position: 'absolute', left: 14, top: 34, fontSize: 11 }}>Số phiếu</div>
                <input type="text" readOnly style={inputStyle({ left: 72, top: 31, width: 320, background: '#f1f5f9' })} value={formData.soPhieuRef || ''} />

                {/* Loại thẻ gốc */}
                <div style={{ position: 'absolute', left: 14, top: 61, fontSize: 11 }}>Loại thẻ</div>
                <input type="text" readOnly style={inputStyle({ left: 72, top: 58, width: 320, background: '#f1f5f9' })} value={formData.loaiThe || customer?.loaiThe || ''} />

                {/* Từ ngày gốc */}
                <div style={{ position: 'absolute', left: 14, top: 88, fontSize: 11 }}>Từ ngày</div>
                <input type="date" readOnly style={inputStyle({ left: 72, top: 85, width: 95, background: '#f1f5f9' })} value={formData.tuNgayRef || ''} />

                {/* Đến ngày gốc */}
                <div style={{ position: 'absolute', left: 14, top: 115, fontSize: 11 }}>Đến ngày</div>
                <input type="date" readOnly style={inputStyle({ left: 72, top: 112, width: 95, background: '#f1f5f9' })} value={formData.denNgayRef || ''} />

                {/* Ca tập gốc */}
                <div style={{ position: 'absolute', left: 14, top: 142, fontSize: 11 }}>Ca tập</div>
                <input type="text" readOnly style={inputStyle({ left: 72, top: 139, width: 320, background: '#f1f5f9' })} value={customer?.caTap || 'Cả ngày'} />
              </div>

              {/* Bảo lưu từ ngày & Đến ngày */}
              <div style={{ ...lblStyle, left: 11, top: 243, width: 99 }}>Bảo lưu từ ngày</div>
              <input
                type="date"
                required
                style={yellowInputStyle({ left: 125, top: 241, width: 95 })}
                value={formData.tuNgay || ''}
                onChange={(e) => handleChange('tuNgay', e.target.value)}
              />
              <div style={{ ...lblStyle, left: 320, top: 246, width: 112 }}>Bảo lưu đến ngày</div>
              <input
                type="date"
                required
                style={yellowInputStyle({ left: 438, top: 243, width: 95, fontWeight: 700 })}
                value={formData.denNgay || ''}
                onChange={(e) => handleChange('denNgay', e.target.value)}
              />

              {/* Số ngày */}
              <div style={{ ...lblStyle, left: 11, top: 273, width: 58 }}>Số ngày</div>
              <input
                type="number"
                min="1"
                style={inputStyle({ left: 125, top: 271, width: 95, textAlign: 'right', fontWeight: 600 })}
                value={formData.soNgay ?? 30}
                onChange={(e) => handleChange('soNgay', parseInt(e.target.value) || 0)}
              />

              {/* Ghi chú */}
              <div style={{ ...lblStyle, left: 11, top: 302, width: 52 }}>Ghi chú</div>
              <textarea
                style={{
                  ...inputStyle({ left: 125, top: 299, width: 408, height: '115px' }),
                  padding: 4,
                  resize: 'none'
                }}
                value={formData.note || ''}
                onChange={(e) => handleChange('note', e.target.value)}
              />
            </>
          )}

          {/* ----------------------------------------------------------------------- */}
          {/* FORM 3 & 4: PHIẾU THU & PHIẾU CHI (Khớp 100% AELayout.xml 527x379)      */}
          {/* ----------------------------------------------------------------------- */}
          {(tabId === 'phieuThu' || tabId === 'phieuChi') && (
            <>
              {/* Ngày & Số phiếu */}
              <div style={{ ...lblStyle, left: 10, top: 13, width: 32 }}>Ngày</div>
              <input
                type="date"
                required
                style={yellowInputStyle({ left: 90, top: 10, width: 95 })}
                value={formData.ngay || ''}
                onChange={(e) => handleChange('ngay', e.target.value)}
              />
              <div style={{ ...lblStyle, left: 191, top: 13, width: 49 }}>Số phiếu</div>
              <input
                type="text"
                required
                style={inputStyle({ left: 261, top: 10, width: 134, fontWeight: 700, background: '#f0fbfb', color: '#1e3a8a' })}
                value={formData.soPhiu || ''}
                onChange={(e) => handleChange('soPhiu', e.target.value)}
              />

              {/* Phân loại (Lý do thu / chi) */}
              <div style={{ ...lblStyle, left: 10, top: 40, width: 51 }}>Phân loại</div>
              <select
                style={inputStyle({ left: 90, top: 37, width: 305, height: 21 })}
                value={formData.dlyDoThuChiId || ''}
                onChange={(e) => handleChange('dlyDoThuChiId', e.target.value)}
              >
                <option value="">-- Chọn phân loại --</option>
                {metadata.lyDoThuChi?.filter(l => tabId === 'phieuThu' ? l.laThu : l.laChi).map(l => (
                  <option key={l.id} value={l.id}>{l.name}</option>
                ))}
              </select>

              {/* Lý do thu / chi (Diễn giải) */}
              <div style={{ ...lblStyle, left: 10, top: 67, width: 51 }}>{tabId === 'phieuThu' ? 'Lý do thu' : 'Lý do chi'}</div>
              <input
                type="text"
                style={inputStyle({ left: 90, top: 64, width: 305 })}
                value={formData.dienGiai || ''}
                onChange={(e) => handleChange('dienGiai', e.target.value)}
              />

              {/* Chứng từ gốc & Loại đối tượng */}
              <div style={{ ...lblStyle, left: 10, top: 94, width: 71 }}>Chứng từ gốc</div>
              <input
                type="text"
                style={inputStyle({ left: 90, top: 91, width: 305 })}
                value={formData.chungTuGoc || ''}
                onChange={(e) => handleChange('chungTuGoc', e.target.value)}
              />
              <div style={{ ...lblStyle, left: 401, top: 98, width: 75 }}>Loại đối tượng</div>
              <select
                style={inputStyle({ left: 401, top: 117, width: 117, height: 21 })}
                value={formData.loaiDoiTuong ?? 2}
                onChange={(e) => handleChange('loaiDoiTuong', Number(e.target.value))}
              >
                <option value={2}>Khách hàng</option>
                <option value={1}>Nhân viên</option>
                <option value={3}>Nhà cung cấp</option>
                <option value={0}>Đối tượng khác</option>
              </select>

              {/* Tên đối tượng */}
              <div style={{ ...lblStyle, left: 10, top: 120, width: 74 }}>Tên đối tượng</div>
              <input
                type="text"
                style={inputStyle({ left: 90, top: 117, width: 305 })}
                value={formData.tenDoiTuong || ''}
                onChange={(e) => handleChange('tenDoiTuong', e.target.value)}
              />

              {/* Địa chỉ */}
              <div style={{ ...lblStyle, left: 10, top: 147, width: 40 }}>Địa chỉ</div>
              <input
                type="text"
                style={inputStyle({ left: 90, top: 144, width: 305 })}
                value={formData.diaChi || ''}
                onChange={(e) => handleChange('diaChi', e.target.value)}
              />

              {/* Nhân viên */}
              <div style={{ ...lblStyle, left: 10, top: 174, width: 56 }}>Nhân viên</div>
              <select
                style={inputStyle({ left: 91, top: 170, width: 305, height: 21 })}
                value={formData.dnhanVienId || ''}
                onChange={(e) => handleChange('dnhanVienId', e.target.value)}
              >
                <option value="">-- Chọn nhân viên --</option>
                {metadata.nhanVien?.map(nv => (
                  <option key={nv.id} value={nv.id}>{nv.name}</option>
                ))}
              </select>

              {/* Khách hàng */}
              <div style={{ ...lblStyle, left: 10, top: 201, width: 65 }}>Khách hàng</div>
              <select
                style={inputStyle({ left: 90, top: 197, width: 305, height: 21 })}
                value={formData.khachHangId || ''}
                onChange={(e) => handleSelectCustomer(e.target.value)}
              >
                <option value="">-- Chọn khách hàng --</option>
                {customers?.map(c => (
                  <option key={c.id} value={c.id}>[{c.maThe}] {c.tenKhachHang || c.name}</option>
                ))}
              </select>

              {/* Nhà cung cấp */}
              <div style={{ ...lblStyle, left: 11, top: 226, width: 75 }}>Nhà cung cấp</div>
              <select
                style={inputStyle({ left: 90, top: 224, width: 306, height: 21 })}
                value={formData.dnhaCungCapId || ''}
                onChange={(e) => handleChange('dnhaCungCapId', e.target.value)}
              >
                <option value="">-- Chọn nhà cung cấp --</option>
              </select>

              {/* Số tiền thu/chi */}
              <div style={{ ...lblStyle, left: 12, top: 252, width: 43, fontWeight: 700 }}>Số tiền:</div>
              <Number3DInput
                isYellow
                style={{ left: 91, top: 250, width: 140, fontWeight: 700, color: tabId === 'phieuThu' ? '#16a34a' : '#dc2626' }}
                value={tabId === 'phieuThu' ? (formData.thu ?? 0) : (formData.chi ?? 0)}
                onChange={(num) => handleChange(tabId === 'phieuThu' ? 'thu' : 'chi', num)}
              />

              {/* Chuyển khoản */}
              <label style={{ position: 'absolute', left: 92, top: 276, display: 'flex', alignItems: 'center', gap: 6, fontSize: 11, cursor: 'pointer' }}>
                <input
                  type="checkbox"
                  checked={!!formData.chuyenKhoan}
                  onChange={(e) => handleChange('chuyenKhoan', e.target.checked)}
                />
                <span>{tabId === 'phieuThu' ? 'Chuyển vào tài khoản' : 'Chuyển từ tài khoản'}</span>
              </label>

              {/* Cửa hàng */}
              <div style={{ ...lblStyle, left: 12, top: 302, width: 53 }}>Cửa hàng</div>
              <select
                style={inputStyle({ left: 92, top: 299, width: 304, height: 21 })}
                value={formData.dcuaHangId || ''}
                onChange={(e) => handleChange('dcuaHangId', e.target.value)}
              >
                {metadata.cuaHang?.map(c => <option key={c.id} value={c.id}>{c.name}</option>) || <option value="">Cửa hàng chính</option>}
              </select>

              {/* Không thay đổi công nợ */}
              <label style={{ position: 'absolute', left: 91, top: 326, display: 'flex', alignItems: 'center', gap: 6, fontSize: 11, cursor: 'pointer' }}>
                <input
                  type="checkbox"
                  checked={!!formData.khongThayDoiCongNo}
                  onChange={(e) => handleChange('khongThayDoiCongNo', e.target.checked)}
                />
                <span>Không thay đổi công nợ</span>
              </label>

              {/* Ghi chú */}
              <div style={{ ...lblStyle, left: 11, top: 351, width: 44 }}>Ghi chú</div>
              <input
                type="text"
                style={inputStyle({ left: 91, top: 348, width: 305 })}
                value={formData.note || ''}
                onChange={(e) => handleChange('note', e.target.value)}
              />
            </>
          )}

          {/* ----------------------------------------------------------------------- */}
          {/* FORM 5: ĐẶT CỌC (Khớp AELayout.xml Đặt cọc 514x327)                     */}
          {/* ----------------------------------------------------------------------- */}
          {tabId === 'datCoc' && (
            <>
              {/* Ngày & Số phiếu */}
              <div style={{ ...lblStyle, left: 10, top: 13, width: 41 }}>Ngày</div>
              <input
                type="date"
                required
                style={yellowInputStyle({ left: 120, top: 11, width: 95 })}
                value={formData.ngay || ''}
                onChange={(e) => handleChange('ngay', e.target.value)}
              />
              <div style={{ ...lblStyle, left: 288, top: 14, width: 61 }}>Số phiếu</div>
              <input
                type="text"
                required
                style={inputStyle({ left: 355, top: 11, width: 150, fontWeight: 700, background: '#f0fbfb', color: '#1e3a8a' })}
                value={formData.soPhiu || ''}
                onChange={(e) => handleChange('soPhiu', e.target.value)}
              />

              {/* Khách hàng & Button Chọn */}
              <div style={{ ...lblStyle, left: 10, top: 41, width: 78 }}>Khách hàng</div>
              <input
                type="text"
                style={inputStyle({ left: 120, top: 39, width: 326 })}
                value={formData.tenDoiTuong || ''}
                onChange={(e) => handleChange('tenDoiTuong', e.target.value)}
              />
              <button
                type="button"
                style={{
                  ...inputStyle({ left: 452, top: 39, width: 53, height: 23, textAlign: 'center' }),
                  background: 'linear-gradient(180deg, #fff 0%, #e2ecf5 100%)',
                  cursor: 'pointer'
                }}
                onClick={() => {
                  const cust = customers?.[0];
                  if (cust) handleSelectCustomer(cust.id);
                }}
              >
                Chọn
              </button>

              {/* Địa chỉ & Button Xóa */}
              <div style={{ ...lblStyle, left: 10, top: 69, width: 48 }}>Địa chỉ</div>
              <input
                type="text"
                style={inputStyle({ left: 120, top: 67, width: 326 })}
                value={formData.diaChi || ''}
                onChange={(e) => handleChange('diaChi', e.target.value)}
              />
              <button
                type="button"
                style={{
                  ...inputStyle({ left: 452, top: 67, width: 53, height: 23, textAlign: 'center' }),
                  background: 'linear-gradient(180deg, #fff 0%, #e2ecf5 100%)',
                  cursor: 'pointer'
                }}
                onClick={() => handleChange('diaChi', '')}
              >
                Xóa
              </button>

              {/* Điện thoại */}
              <div style={{ ...lblStyle, left: 10, top: 98, width: 67 }}>Điện thoại</div>
              <input
                type="text"
                style={inputStyle({ left: 120, top: 95, width: 326 })}
                value={formData.dienThoai || ''}
                onChange={(e) => handleChange('dienThoai', e.target.value)}
              />

              {/* Lý do thu chi */}
              <div style={{ ...lblStyle, left: 10, top: 125, width: 81 }}>Lý do thu chi</div>
              <select
                style={inputStyle({ left: 120, top: 123, width: 385, height: 23 })}
                value={formData.dlyDoThuChiId || ''}
                onChange={(e) => handleChange('dlyDoThuChiId', e.target.value)}
              >
                <option value="">-- Chọn lý do --</option>
                {metadata.lyDoThuChi?.map(l => (
                  <option key={l.id} value={l.id}>{l.name}</option>
                ))}
              </select>

              {/* Loại thẻ */}
              <div style={{ ...lblStyle, left: 10, top: 154, width: 55 }}>Loại thẻ</div>
              <select
                style={yellowInputStyle({ left: 120, top: 152, width: 385, height: 23, fontWeight: 600 })}
                value={formData.dloaiTheId || ''}
                onChange={(e) => handleChange('dloaiTheId', e.target.value)}
              >
                <option value="">-- Chọn loại thẻ đặt cọc --</option>
                {metadata.loaiThe?.map(lt => (
                  <option key={lt.id} value={lt.id}>{lt.name} ({lt.giaBan?.toLocaleString()} đ)</option>
                ))}
              </select>

              {/* Giá trị gói & Giảm giá (%) */}
              <div style={{ ...lblStyle, left: 10, top: 183, width: 64 }}>Giá trị gói</div>
              <Number3DInput
                style={{ left: 120, top: 181, width: 95 }}
                value={formData.giaTriGoi ?? 0}
                onChange={(num) => handleChange('giaTriGoi', num)}
              />
              <div style={{ ...lblStyle, left: 234, top: 184, width: 85 }}>Giảm giá (%)</div>
              <Number3DInput
                style={{ left: 325, top: 181, width: 95 }}
                value={formData.giamGia ?? 0}
                onChange={(num) => handleChange('giamGia', num)}
              />

              {/* Tổng cộng & Tiền giảm */}
              <div style={{ ...lblStyle, left: 10, top: 211, width: 73 }}>Tổng cộng</div>
              <Number3DInput
                readOnly
                isYellow
                style={{ left: 120, top: 209, width: 95, fontWeight: 700 }}
                value={formData.tongCong ?? 0}
              />
              <div style={{ ...lblStyle, left: 234, top: 211, width: 68 }}>Tiền giảm</div>
              <Number3DInput
                readOnly
                style={{ left: 325, top: 209, width: 95 }}
                value={formData.tienGiam ?? 0}
              />

              {/* Số tiền đặt trước & Tổng đặt */}
              <div style={{ ...lblStyle, left: 10, top: 239, width: 103, fontWeight: 700 }}>Số tiền đặt trước</div>
              <Number3DInput
                isYellow
                style={{ left: 120, top: 237, width: 95, fontWeight: 700, color: '#16a34a' }}
                value={formData.thu ?? 0}
                onChange={(num) => handleChange('thu', num)}
              />
              <div style={{ ...lblStyle, left: 234, top: 239, width: 62 }}>Tổng đặt</div>
              <Number3DInput
                readOnly
                style={{ left: 325, top: 237, width: 95, fontWeight: 700 }}
                value={formData.tongDat ?? formData.thu ?? 0}
              />

              {/* Chuyển khoản */}
              <label style={{ position: 'absolute', left: 120, top: 267, display: 'flex', alignItems: 'center', gap: 6, fontSize: 11, cursor: 'pointer' }}>
                <input
                  type="checkbox"
                  checked={!!formData.chuyenKhoan}
                  onChange={(e) => handleChange('chuyenKhoan', e.target.checked)}
                />
                <span>Chuyển khoản</span>
              </label>

              {/* Ghi chú */}
              <div style={{ ...lblStyle, left: 10, top: 296, width: 52 }}>Ghi chú</div>
              <input
                type="text"
                style={inputStyle({ left: 120, top: 294, width: 385 })}
                value={formData.note || ''}
                onChange={(e) => handleChange('note', e.target.value)}
              />
            </>
          )}

          {/* ----------------------------------------------------------------------- */}
          {/* FORM 6: PHIẾU THU CÔNG NỢ (Khớp AELayout.xml 590x454)                   */}
          {/* ----------------------------------------------------------------------- */}
          {tabId === 'thuCongNo' && (
            <>
              {/* Ngày & Số phiếu */}
              <div style={{ ...lblStyle, left: 10, top: 13, width: 32 }}>Ngày</div>
              <input
                type="date"
                required
                style={yellowInputStyle({ left: 90, top: 10, width: 95 })}
                value={formData.ngay || ''}
                onChange={(e) => handleChange('ngay', e.target.value)}
              />
              <div style={{ ...lblStyle, left: 344, top: 13, width: 49 }}>Số phiếu</div>
              <input
                type="text"
                required
                style={inputStyle({ left: 424, top: 10, width: 158, fontWeight: 700, background: '#f0fbfb', color: '#1e3a8a' })}
                value={formData.soPhiu || ''}
                onChange={(e) => handleChange('soPhiu', e.target.value)}
              />

              {/* Nhân viên & Diễn giải */}
              <div style={{ ...lblStyle, left: 10, top: 39, width: 56 }}>Nhân viên</div>
              <select
                style={inputStyle({ left: 90, top: 36, width: 248, height: 21 })}
                value={formData.dnhanVienId || ''}
                onChange={(e) => handleChange('dnhanVienId', e.target.value)}
              >
                <option value="">-- Chọn nhân viên --</option>
                {metadata.nhanVien?.map(nv => (
                  <option key={nv.id} value={nv.id}>{nv.name}</option>
                ))}
              </select>
              <div style={{ ...lblStyle, left: 344, top: 39, width: 48 }}>Diễn giải</div>
              <input
                type="text"
                style={inputStyle({ left: 424, top: 36, width: 158 })}
                value={formData.dienGiai || ''}
                onChange={(e) => handleChange('dienGiai', e.target.value)}
              />

              {/* Lý do thu chi & Chứng từ gốc */}
              <div style={{ ...lblStyle, left: 10, top: 66, width: 68 }}>Lý do thu chi</div>
              <select
                style={inputStyle({ left: 90, top: 63, width: 248, height: 21 })}
                value={formData.dlyDoThuChiId || ''}
                onChange={(e) => handleChange('dlyDoThuChiId', e.target.value)}
              >
                <option value="">-- Thu công nợ --</option>
                {metadata.lyDoThuChi?.map(l => (
                  <option key={l.id} value={l.id}>{l.name}</option>
                ))}
              </select>
              <div style={{ ...lblStyle, left: 344, top: 66, width: 71 }}>Chứng từ gốc</div>
              <input
                type="text"
                style={inputStyle({ left: 424, top: 63, width: 158 })}
                value={formData.chungTuGoc || ''}
                onChange={(e) => handleChange('chungTuGoc', e.target.value)}
              />

              {/* Số tiền thu & Chuyển khoản */}
              <div style={{ ...lblStyle, left: 10, top: 93, width: 58, fontWeight: 700 }}>Số tiền thu</div>
              <Number3DInput
                isYellow
                style={{ left: 90, top: 90, width: 95, fontWeight: 700, color: '#16a34a' }}
                value={formData.thu ?? 0}
                onChange={(num) => handleChange('thu', num)}
              />
              <label style={{ position: 'absolute', left: 191, top: 92, display: 'flex', alignItems: 'center', gap: 6, fontSize: 11, cursor: 'pointer' }}>
                <input
                  type="checkbox"
                  checked={!!formData.chuyenKhoan}
                  onChange={(e) => handleChange('chuyenKhoan', e.target.checked)}
                />
                <span>Chuyển khoản</span>
              </label>

              {/* Ghi chú */}
              <div style={{ ...lblStyle, left: 7, top: 119, width: 44 }}>Ghi chú</div>
              <input
                type="text"
                style={inputStyle({ left: 90, top: 116, width: 492 })}
                value={formData.note || ''}
                onChange={(e) => handleChange('note', e.target.value)}
              />

              {/* Grid danh sách công nợ (grMain) */}
              <div
                style={{
                  position: 'absolute',
                  left: 7,
                  top: 141,
                  width: 575,
                  height: 303,
                  border: '1px solid #7192b8',
                  background: '#ffffff',
                  overflowY: 'auto'
                }}
              >
                <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: 11, fontFamily: "Tahoma, sans-serif" }}>
                  <thead>
                    <tr style={{ background: '#dce8f5', borderBottom: '1px solid #9fb9d0', height: 24, textAlign: 'left' }}>
                      <th style={{ padding: '0 6px', borderRight: '1px solid #b8cde0' }}>Ngày</th>
                      <th style={{ padding: '0 6px', borderRight: '1px solid #b8cde0' }}>Số phiếu nợ</th>
                      <th style={{ padding: '0 6px', borderRight: '1px solid #b8cde0' }}>Khoản nợ</th>
                      <th style={{ padding: '0 6px', borderRight: '1px solid #b8cde0', textAlign: 'right' }}>Tổng nợ</th>
                      <th style={{ padding: '0 6px', textAlign: 'right' }}>Thu lần này</th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr style={{ height: 24, borderBottom: '1px solid #e2e8f0' }}>
                      <td style={{ padding: '0 6px', borderRight: '1px solid #f1f5f9' }}>{toDisplayDate(formData.ngay)}</td>
                      <td style={{ padding: '0 6px', borderRight: '1px solid #f1f5f9' }}>{formData.soPhiu}</td>
                      <td style={{ padding: '0 6px', borderRight: '1px solid #f1f5f9' }}>Công nợ hội viên {customer?.tenKhachHang || ''}</td>
                      <td style={{ padding: '0 6px', borderRight: '1px solid #f1f5f9', textAlign: 'right', fontWeight: 600 }}>{(formData.thu || 0).toLocaleString()}</td>
                      <td style={{ padding: '0 6px', textAlign: 'right', color: '#16a34a', fontWeight: 700 }}>{(formData.thu || 0).toLocaleString()}</td>
                    </tr>
                  </tbody>
                </table>
              </div>
            </>
          )}

          {/* ----------------------------------------------------------------------- */}
          {/* FORM 7: ĐƠN HÀNG (Khớp AELayout.xml Đơn hàng 660x510)                   */}
          {/* ----------------------------------------------------------------------- */}
          {tabId === 'donHang' && (
            <>
              {/* Ngày, Số phiếu, Giờ thanh toán */}
              <div style={{ ...lblStyle, left: 10, top: 13, width: 32 }}>Ngày</div>
              <input
                type="date"
                required
                style={yellowInputStyle({ left: 98, top: 10, width: 95 })}
                value={formData.ngay || ''}
                onChange={(e) => handleChange('ngay', e.target.value)}
              />
              <div style={{ ...lblStyle, left: 199, top: 13, width: 49 }}>Số phiếu</div>
              <input
                type="text"
                required
                style={inputStyle({ left: 254, top: 10, width: 141, fontWeight: 700, background: '#f0fbfb', color: '#1e3a8a' })}
                value={formData.soPhiu || ''}
                onChange={(e) => handleChange('soPhiu', e.target.value)}
              />
              <div style={{ ...lblStyle, left: 403, top: 14, width: 77 }}>Giờ thanh toán</div>
              <input
                type="time"
                style={inputStyle({ left: 491, top: 11, width: 72 })}
                value={formData.gioThanhToan || ''}
                onChange={(e) => handleChange('gioThanhToan', e.target.value)}
              />

              {/* Khách hàng */}
              <div style={{ ...lblStyle, left: 10, top: 39, width: 65 }}>Khách hàng</div>
              <select
                style={yellowInputStyle({ left: 98, top: 36, width: 554, height: 21, fontWeight: 600 })}
                value={formData.khachHangId || ''}
                onChange={(e) => handleSelectCustomer(e.target.value)}
              >
                <option value="">-- Chọn khách hàng --</option>
                {customers?.map(c => (
                  <option key={c.id} value={c.id}>[{c.maThe}] {c.tenKhachHang || c.name} - {c.dienThoai || ''}</option>
                ))}
              </select>

              {/* Kho xuất & Nhân viên xuất */}
              <div style={{ ...lblStyle, left: 10, top: 64, width: 49 }}>Kho xuất</div>
              <select
                style={inputStyle({ left: 98, top: 63, width: 215, height: 21 })}
                value={formData.dkhoXuatId || ''}
                onChange={(e) => handleChange('dkhoXuatId', e.target.value)}
              >
                {metadata.khoHang?.map(k => <option key={k.id} value={k.id}>{k.name}</option>) || <option value="">KHO TỔNG</option>}
              </select>
              <div style={{ ...lblStyle, left: 377, top: 66, width: 79 }}>Nhân viên xuất</div>
              <select
                style={inputStyle({ left: 462, top: 63, width: 190, height: 21 })}
                value={formData.dnhanVienXuatId || ''}
                onChange={(e) => handleChange('dnhanVienXuatId', e.target.value)}
              >
                <option value="">-- Chọn nhân viên --</option>
                {metadata.nhanVien?.map(nv => <option key={nv.id} value={nv.id}>{nv.name}</option>)}
              </select>

              {/* Ghi chú & Diễn giải */}
              <div style={{ ...lblStyle, left: 10, top: 93, width: 44 }}>Ghi chú</div>
              <input
                type="text"
                style={inputStyle({ left: 98, top: 90, width: 215 })}
                value={formData.note || ''}
                onChange={(e) => handleChange('note', e.target.value)}
              />
              <div style={{ ...lblStyle, left: 377, top: 93, width: 48 }}>Diễn giải</div>
              <input
                type="text"
                style={inputStyle({ left: 462, top: 90, width: 190 })}
                value={formData.dienGiai || ''}
                onChange={(e) => handleChange('dienGiai', e.target.value)}
              />

              {/* Thanh toán bởi & Giao hàng */}
              <div style={{ ...lblStyle, left: 10, top: 119, width: 79 }}>Thanh toán bởi</div>
              <input
                type="text"
                readOnly
                style={inputStyle({ left: 98, top: 116, width: 215, background: '#f8fafc' })}
                value={formData.userThanhToanId || 'Administrator'}
              />
              <div style={{ ...lblStyle, left: 377, top: 119, width: 56 }}>Giao hàng</div>
              <input
                type="text"
                style={inputStyle({ left: 462, top: 116, width: 190 })}
                value={formData.giaoHang || ''}
                onChange={(e) => handleChange('giaoHang', e.target.value)}
              />

              {/* Tab Mua / Trả (Lưới chi tiết đơn hàng tabMuaTra) */}
              <div
                style={{
                  position: 'absolute',
                  left: 10,
                  top: 143,
                  width: 642,
                  height: 196,
                  border: '1px solid #7192b8',
                  background: '#ffffff',
                  display: 'flex',
                  flexDirection: 'column'
                }}
              >
                <div style={{ height: 24, background: '#dce8f5', borderBottom: '1px solid #aec4d9', display: 'flex', alignItems: 'center', padding: '0 8px', fontSize: 11, fontWeight: 700, color: '#1e3a8a' }}>
                  <span>Chi tiết hàng hóa / Dịch vụ đơn hàng</span>
                </div>
                <div style={{ flex: 1, padding: 10, color: '#64748b', fontSize: 11 }}>
                  Chưa có sản phẩm hàng hóa nào trong giỏ hàng. Nhập trực tiếp tổng tiền bên dưới.
                </div>
              </div>

              {/* Bảng tính tổng tiền bên phải */}
              <div style={{ ...lblStyle, left: 380, top: 348, width: 55 }}>Tiền hàng</div>
              <Number3DInput
                style={{ left: 468, top: 345, width: 184 }}
                value={formData.tienHang ?? 0}
                onChange={(num) => handleChange('tienHang', num)}
              />

              <div style={{ ...lblStyle, left: 380, top: 374, width: 83 }}>Phí vận chuyển</div>
              <Number3DInput
                style={{ left: 468, top: 371, width: 184 }}
                value={formData.phiVanChuyen ?? 0}
                onChange={(num) => handleChange('phiVanChuyen', num)}
              />

              <div style={{ ...lblStyle, left: 380, top: 400, width: 69 }}>Tỉ lệ giảm giá</div>
              <Number3DInput
                style={{ left: 468, top: 397, width: 51 }}
                value={formData.tiLeGiamGia ?? 0}
                onChange={(num) => handleChange('tiLeGiamGia', num)}
              />
              <div style={{ ...lblStyle, left: 525, top: 400, width: 15 }}>%</div>
              <Number3DInput
                style={{ left: 557, top: 398, width: 95 }}
                value={formData.tienGiamGia ?? 0}
                onChange={(num) => handleChange('tienGiamGia', num)}
              />

              <div style={{ ...lblStyle, left: 380, top: 428, width: 51 }}>Tỉ lệ thuế</div>
              <Number3DInput
                style={{ left: 468, top: 425, width: 51 }}
                value={formData.tiLeThue ?? 0}
                onChange={(num) => handleChange('tiLeThue', num)}
              />
              <div style={{ ...lblStyle, left: 525, top: 428, width: 15 }}>%</div>
              <Number3DInput
                style={{ left: 557, top: 426, width: 95 }}
                value={formData.tienThue ?? 0}
                onChange={(num) => handleChange('tienThue', num)}
              />

              <div style={{ ...lblStyle, left: 380, top: 454, width: 38 }}>Đổi trả</div>
              <Number3DInput
                style={{ left: 468, top: 451, width: 184 }}
                value={formData.doiTra ?? 0}
                onChange={(num) => handleChange('doiTra', num)}
              />

              <div style={{ ...lblStyle, left: 380, top: 482, width: 59, fontWeight: 700 }}>Tổng cộng</div>
              <Number3DInput
                readOnly
                isYellow
                style={{ left: 468, top: 479, width: 184, fontWeight: 700, color: '#1e3a8a' }}
                value={formData.tongCong ?? 0}
              />
            </>
          )}

          {/* ----------------------------------------------------------------------- */}
          {/* FORM 8: ĐỔI LOẠI THẺ (Khớp 100% WinForms AELayout.xml 570x487)          */}
          {/* ----------------------------------------------------------------------- */}
          {tabId === 'doiLoaiThe' && (
            <>
              {/* Ngày & Số phiếu */}
              <div style={{ ...lblStyle, left: 10, top: 13, width: 41 }}>Ngày</div>
              <input
                type="date"
                required
                style={yellowInputStyle({ left: 118, top: 10, width: 104 })}
                value={formData.ngay || ''}
                onChange={(e) => handleChange('ngay', e.target.value)}
              />
              <div style={{ ...lblStyle, left: 333, top: 13, width: 61 }}>Số phiếu</div>
              <input
                type="text"
                required
                style={inputStyle({ left: 407, top: 10, width: 154, fontWeight: 700, background: '#f0fbfb', color: '#1e3a8a' })}
                value={formData.soPhiu || ''}
                onChange={(e) => handleChange('soPhiu', e.target.value)}
              />

              {/* Khách hàng */}
              <div style={{ ...lblStyle, left: 10, top: 39, width: 78 }}>Khách hàng</div>
              <select
                style={yellowInputStyle({ left: 118, top: 38, width: 443, height: 23, fontWeight: 600 })}
                value={formData.khachHangId || ''}
                onChange={(e) => handleSelectCustomer(e.target.value)}
              >
                <option value="">-- Chọn khách hàng --</option>
                {customers?.map(c => (
                  <option key={c.id} value={c.id}>[{c.maThe}] {c.tenKhachHang || c.name} - {c.dienThoai || ''}</option>
                ))}
              </select>

              {/* Địa chỉ & Điện thoại */}
              <div style={{ ...lblStyle, left: 10, top: 69, width: 48 }}>Địa chỉ</div>
              <input
                type="text"
                readOnly
                style={inputStyle({ left: 118, top: 67, width: 166, background: '#f8fafc' })}
                value={formData.diaChi || ''}
              />
              <div style={{ ...lblStyle, left: 327, top: 70, width: 67 }}>Điện thoại</div>
              <input
                type="text"
                readOnly
                style={inputStyle({ left: 407, top: 67, width: 154, background: '#f8fafc' })}
                value={formData.dienThoai || ''}
              />

              {/* Nhóm thông tin thẻ cũ (Panel1) */}
              <div style={{ ...lblStyle, left: 10, top: 97, width: 95, fontWeight: 600 }}>Thông tin thẻ cũ</div>
              <div
                style={{
                  position: 'absolute',
                  left: 118,
                  top: 95,
                  width: 443,
                  height: 165,
                  border: '1px solid #7192b8',
                  background: '#dce8f5',
                  boxSizing: 'border-box'
                }}
              >
                {/* Ngày thẻ cũ */}
                <div style={{ ...lblStyle, left: 7, top: 5, fontSize: 11 }}>Ngày</div>
                <input
                  type="date"
                  readOnly
                  style={inputStyle({ left: 71, top: 3, width: 95, background: '#f1f5f9' })}
                  value={formData.ngayRef || ''}
                />

                {/* Loại thẻ cũ */}
                <div style={{ ...lblStyle, left: 7, top: 32, fontSize: 11 }}>Loại thẻ</div>
                <input
                  type="text"
                  readOnly
                  style={inputStyle({ left: 71, top: 30, width: 351, background: '#f1f5f9', fontWeight: 600 })}
                  value={formData.loaiTheRef || ''}
                />

                {/* Từ ngày & Đến ngày cũ */}
                <div style={{ ...lblStyle, left: 7, top: 59, fontSize: 11 }}>Từ ngày</div>
                <input
                  type="date"
                  readOnly
                  style={inputStyle({ left: 71, top: 57, width: 95, background: '#f1f5f9' })}
                  value={formData.tuNgayRef || ''}
                />
                <div style={{ ...lblStyle, left: 263, top: 59, fontSize: 11 }}>Đến ngày</div>
                <input
                  type="date"
                  readOnly
                  style={inputStyle({ left: 327, top: 57, width: 95, background: '#f1f5f9' })}
                  value={formData.denNgayRef || ''}
                />

                {/* Số tiền & Giảm giá cũ */}
                <div style={{ ...lblStyle, left: 7, top: 85, fontSize: 11 }}>Số tiền</div>
                <input
                  type="number"
                  readOnly
                  style={inputStyle({ left: 71, top: 83, width: 95, textAlign: 'right', background: '#f1f5f9' })}
                  value={formData.soTienRef ?? 0}
                />
                <div style={{ ...lblStyle, left: 193, top: 85, fontSize: 11 }}>Giảm giá</div>
                <input
                  type="number"
                  readOnly
                  style={inputStyle({ left: 259, top: 83, width: 41, textAlign: 'right', background: '#f1f5f9' })}
                  value={formData.tiLeGiamRef ?? 0}
                />
                <div style={{ ...lblStyle, left: 306, top: 85, fontSize: 11 }}>%</div>
                <input
                  type="number"
                  readOnly
                  style={inputStyle({ left: 327, top: 83, width: 95, textAlign: 'right', background: '#f1f5f9' })}
                  value={formData.tienGiamRef ?? 0}
                />

                {/* Cộng & Số lần cũ */}
                <div style={{ ...lblStyle, left: 7, top: 111, fontSize: 11, fontWeight: 600 }}>Cộng</div>
                <input
                  type="number"
                  readOnly
                  style={yellowInputStyle({ left: 71, top: 109, width: 95, textAlign: 'right', fontWeight: 700 })}
                  value={formData.tongCongRef ?? 0}
                />
                <div style={{ ...lblStyle, left: 193, top: 111, fontSize: 11 }}>Số lần</div>
                <input
                  type="number"
                  readOnly
                  style={inputStyle({ left: 259, top: 110, width: 41, textAlign: 'right', background: '#f1f5f9' })}
                  value={formData.soLanRef ?? 0}
                />

                {/* Ca tập cũ */}
                <div style={{ ...lblStyle, left: 7, top: 138, fontSize: 11 }}>Ca tập</div>
                <input
                  type="text"
                  readOnly
                  style={inputStyle({ left: 72, top: 136, width: 351, background: '#f1f5f9' })}
                  value={formData.caTapRef || 'Cả ngày'}
                />
              </div>

              {/* Thông tin thẻ mới bên dưới */}
              <div style={{ ...lblStyle, left: 10, top: 267, width: 55, fontWeight: 600 }}>Loại thẻ</div>
              <select
                style={yellowInputStyle({ left: 118, top: 266, width: 443, height: 23, fontWeight: 600 })}
                value={formData.dloaiTheId || ''}
                onChange={(e) => handleChange('dloaiTheId', e.target.value)}
              >
                <option value="">-- Chọn loại thẻ / Gói tập mới --</option>
                {metadata.loaiThe?.map(lt => (
                  <option key={lt.id} value={lt.id}>
                    {lt.name} ({lt.giaBan?.toLocaleString()} đ - {lt.soThang || 1} tháng)
                  </option>
                ))}
              </select>

              {/* Từ ngày, Đến ngày, Số lần mới */}
              <div style={{ ...lblStyle, left: 10, top: 296, width: 57 }}>Từ ngày</div>
              <input
                type="date"
                required
                style={yellowInputStyle({ left: 118, top: 295, width: 130 })}
                value={formData.tuNgay || ''}
                onChange={(e) => handleChange('tuNgay', e.target.value)}
              />
              <div style={{ ...lblStyle, left: 262, top: 297, width: 65 }}>Đến ngày</div>
              <input
                type="date"
                required
                style={yellowInputStyle({ left: 333, top: 294, width: 95, fontWeight: 700 })}
                value={formData.denNgay || ''}
                onChange={(e) => handleChange('denNgay', e.target.value)}
              />
              <div style={{ ...lblStyle, left: 436, top: 296, width: 46 }}>Số lần</div>
              <input
                type="number"
                min="0"
                style={inputStyle({ left: 501, top: 294, width: 60, textAlign: 'right' })}
                value={formData.soLan ?? 0}
                onChange={(e) => handleChange('soLan', parseInt(e.target.value) || 0)}
              />

              {/* Ngày tặng thêm & Lần tặng thêm */}
              <div style={{ ...lblStyle, left: 10, top: 325, width: 102 }}>Ngày tặng thêm</div>
              <input
                type="number"
                min="0"
                style={inputStyle({ left: 118, top: 323, width: 130, textAlign: 'right' })}
                value={formData.ngayTangThem ?? 0}
                onChange={(e) => handleChange('ngayTangThem', parseInt(e.target.value) || 0)}
              />
              <div style={{ ...lblStyle, left: 404, top: 326, width: 91 }}>Lần tặng thêm</div>
              <input
                type="number"
                min="0"
                style={inputStyle({ left: 501, top: 324, width: 60, textAlign: 'right' })}
                value={formData.lanTangThem ?? 0}
                onChange={(e) => handleChange('lanTangThem', parseInt(e.target.value) || 0)}
              />

              {/* Số tiền thẻ mới */}
              <div style={{ ...lblStyle, left: 10, top: 353, width: 49 }}>Số tiền</div>
              <Number3DInput
                style={{ left: 118, top: 351, width: 130, fontWeight: 600 }}
                value={formData.soTien ?? 0}
                onChange={(num) => handleChange('soTien', num)}
              />

              {/* Giảm giá & Trừ thẻ cũ */}
              <div style={{ ...lblStyle, left: 10, top: 380, width: 62 }}>Giảm giá</div>
              <Number3DInput
                style={{ left: 118, top: 377, width: 30 }}
                value={formData.tiLeGiamGia ?? 0}
                onChange={(num) => handleChange('tiLeGiamGia', num)}
              />
              <div style={{ ...lblStyle, left: 153, top: 380, width: 20 }}>%</div>
              <Number3DInput
                style={{ left: 177, top: 377, width: 71 }}
                value={formData.tienGiamGia ?? 0}
                onChange={(num) => handleChange('tienGiamGia', num)}
              />
              <div style={{ ...lblStyle, left: 328, top: 379, width: 66, fontWeight: 700, color: '#b45309' }}>Trừ thẻ cũ</div>
              <Number3DInput
                readOnly
                isYellow
                style={{ left: 431, top: 377, width: 130, fontWeight: 700, color: '#b45309' }}
                value={formData.numTruTheCu ?? formData.tongCongRef ?? 0}
              />

              {/* Tổng cộng & Thanh toán */}
              <div style={{ ...lblStyle, left: 10, top: 405, width: 73, fontWeight: 700 }}>Tổng cộng</div>
              <Number3DInput
                readOnly
                isYellow
                style={{ left: 118, top: 403, width: 130, fontWeight: 700, color: '#1e3a8a' }}
                value={formData.tongCong ?? 0}
              />
              <div style={{ ...lblStyle, left: 323, top: 405, width: 75 }}>Thanh toán</div>
              <Number3DInput
                style={{ left: 431, top: 403, width: 130, fontWeight: 600 }}
                value={formData.thanhToan ?? 0}
                onChange={(num) => handleChange('thanhToan', num)}
              />

              {/* Ca tập */}
              <div style={{ ...lblStyle, left: 10, top: 433, width: 47 }}>Ca tập</div>
              <select
                style={inputStyle({ left: 118, top: 430, width: 443, height: 23 })}
                value={formData.dcatapId || ''}
                onChange={(e) => handleChange('dcatapId', e.target.value)}
              >
                <option value="">-- Cả ngày (Mặc định) --</option>
                {metadata.caTap?.map(ct => (
                  <option key={ct.id} value={ct.id}>{ct.name}</option>
                ))}
              </select>

              {/* Ghi chú */}
              <div style={{ ...lblStyle, left: 10, top: 460, width: 52 }}>Ghi chú</div>
              <input
                type="text"
                style={inputStyle({ left: 118, top: 457, width: 443 })}
                value={formData.note || ''}
                onChange={(e) => handleChange('note', e.target.value)}
              />
            </>
          )}

          {/* ----------------------------------------------------------------------- */}
          {/* FORM 9: BÁO GIÁ & FORM 10: ĐẶT HÀNG (STABLEDESC TBAOGIA / TDATHANG)       */}
          {/* ----------------------------------------------------------------------- */}
          {(tabId === 'baoGia' || tabId === 'datHang') && (
            <>
              {/* Ngày & Số phiếu */}
              <div style={{ ...lblStyle, left: 10, top: 12, width: 40 }}>Ngày</div>
              <input
                type="date"
                required
                style={yellowInputStyle({ left: 90, top: 10, width: 115 })}
                value={formData.ngay || ''}
                onChange={(e) => handleChange('ngay', e.target.value)}
              />
              <div style={{ ...lblStyle, left: 220, top: 12, width: 55 }}>Số phiếu</div>
              <input
                type="text"
                required
                style={inputStyle({ left: 280, top: 10, width: 140, fontWeight: 700, background: '#f0fbfb', color: '#1e3a8a' })}
                value={formData.soPhiu || ''}
                onChange={(e) => handleChange('soPhiu', e.target.value)}
              />

              {/* Khách hàng */}
              <div style={{ ...lblStyle, left: 10, top: 38, width: 70 }}>Khách hàng</div>
              <select
                style={yellowInputStyle({ left: 90, top: 36, width: 330, height: 22, fontWeight: 600 })}
                value={formData.khachHangId || ''}
                onChange={(e) => handleSelectCustomer(e.target.value)}
              >
                <option value="">-- Chọn khách hàng --</option>
                {customers?.map(c => (
                  <option key={c.id} value={c.id}>[{c.maThe}] {c.tenKhachHang || c.name} - {c.dienThoai || ''}</option>
                ))}
              </select>

              {/* Địa chỉ */}
              <div style={{ ...lblStyle, left: 10, top: 65, width: 50 }}>Địa chỉ</div>
              <input
                type="text"
                style={inputStyle({ left: 90, top: 63, width: 330 })}
                value={formData.diaChi || ''}
                onChange={(e) => handleChange('diaChi', e.target.value)}
              />

              {/* Điện thoại & Email */}
              <div style={{ ...lblStyle, left: 10, top: 92, width: 60 }}>Điện thoại</div>
              <input
                type="text"
                style={inputStyle({ left: 90, top: 90, width: 140 })}
                value={formData.dienThoai || ''}
                onChange={(e) => handleChange('dienThoai', e.target.value)}
              />
              <div style={{ ...lblStyle, left: 245, top: 92, width: 35 }}>Email</div>
              <input
                type="email"
                style={inputStyle({ left: 285, top: 90, width: 135 })}
                value={formData.email || ''}
                onChange={(e) => handleChange('email', e.target.value)}
              />

              {/* Nhân viên kinh doanh */}
              <div style={{ ...lblStyle, left: 10, top: 119, width: 60 }}>Nhân viên</div>
              <select
                style={inputStyle({ left: 90, top: 117, width: 330, height: 22 })}
                value={formData.dnhanVienId || ''}
                onChange={(e) => handleChange('dnhanVienId', e.target.value)}
              >
                <option value="">-- Chọn nhân viên kinh doanh --</option>
                {metadata.nhanVien?.map(nv => (
                  <option key={nv.id} value={nv.id}>{nv.name}</option>
                ))}
              </select>

              {/* Diễn giải */}
              <div style={{ ...lblStyle, left: 10, top: 146, width: 60 }}>Diễn giải</div>
              <input
                type="text"
                style={inputStyle({ left: 90, top: 144, width: 330 })}
                value={formData.dienGiai || ''}
                onChange={(e) => handleChange('dienGiai', e.target.value)}
              />

              {/* Bảng chi tiết hàng hóa báo giá / đặt hàng */}
              <div
                style={{
                  position: 'absolute',
                  left: 10,
                  top: 173,
                  width: tabId === 'datHang' ? 620 : 540,
                  height: 265,
                  border: '1px solid #7192b8',
                  background: '#ffffff',
                  display: 'flex',
                  flexDirection: 'column'
                }}
              >
                <div style={{ height: 24, background: '#dce8f5', borderBottom: '1px solid #aec4d9', display: 'flex', alignItems: 'center', padding: '0 8px', fontSize: 11, fontWeight: 700, color: '#1e3a8a' }}>
                  <span>Chi tiết hàng hóa / dịch vụ {tabId === 'baoGia' ? 'báo giá' : 'đặt hàng'}</span>
                </div>
                <div style={{ flex: 1, padding: 10, color: '#64748b', fontSize: 11, overflowY: 'auto' }}>
                  <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: 11 }}>
                    <thead>
                      <tr style={{ background: '#f1f5f9', borderBottom: '1px solid #cbd5e1' }}>
                        <th style={{ textAlign: 'left', padding: '4px 6px' }}>Tên mặt hàng / Dịch vụ</th>
                        <th style={{ width: 60, textAlign: 'right', padding: '4px 6px' }}>SL</th>
                        <th style={{ width: 100, textAlign: 'right', padding: '4px 6px' }}>Đơn giá</th>
                        <th style={{ width: 110, textAlign: 'right', padding: '4px 6px' }}>Thành tiền</th>
                      </tr>
                    </thead>
                    <tbody>
                      <tr>
                        <td style={{ padding: '6px' }}>{tabId === 'baoGia' ? 'Gói dịch vụ Gym - Fitness trọn gói' : 'Dịch vụ Đặt hàng thiết bị / Thẻ Gym'}</td>
                        <td style={{ textAlign: 'right', padding: '6px' }}>1</td>
                        <td style={{ textAlign: 'right', padding: '6px' }}>{(formData.tienHang || 0).toLocaleString()}</td>
                        <td style={{ textAlign: 'right', padding: '6px', fontWeight: 600 }}>{(formData.tienHang || 0).toLocaleString()}</td>
                      </tr>
                    </tbody>
                  </table>
                </div>
              </div>

              {/* Bảng tính chiết khấu & tổng tiền bên phải */}
              <div style={{ position: 'absolute', left: tabId === 'datHang' ? 645 : 565, top: 10, width: 360, height: 430 }}>
                {/* Tiền hàng */}
                <div style={{ ...lblStyle, left: 10, top: 12, width: 80 }}>Tiền hàng</div>
                <input
                  type="number"
                  min="0"
                  style={inputStyle({ left: 110, top: 10, width: 230, textAlign: 'right', fontWeight: 600 })}
                  value={formData.tienHang ?? 0}
                  onChange={(e) => handleChange('tienHang', parseFloat(e.target.value) || 0)}
                />

                {/* Phí vận chuyển */}
                <div style={{ ...lblStyle, left: 10, top: 40, width: 90 }}>Phí vận chuyển</div>
                <input
                  type="number"
                  min="0"
                  style={inputStyle({ left: 110, top: 38, width: 230, textAlign: 'right' })}
                  value={formData.phiVanChuyen ?? 0}
                  onChange={(e) => handleChange('phiVanChuyen', parseFloat(e.target.value) || 0)}
                />

                {/* Tỉ lệ giảm giá */}
                <div style={{ ...lblStyle, left: 10, top: 68, width: 80 }}>Tỉ lệ giảm giá</div>
                <input
                  type="number"
                  min="0"
                  style={inputStyle({ left: 110, top: 66, width: 50, textAlign: 'right' })}
                  value={formData.tiLeGiamGia ?? 0}
                  onChange={(e) => handleChange('tiLeGiamGia', parseFloat(e.target.value) || 0)}
                />
                <div style={{ ...lblStyle, left: 165, top: 68, width: 15 }}>%</div>
                <input
                  type="number"
                  min="0"
                  style={inputStyle({ left: 185, top: 66, width: 155, textAlign: 'right' })}
                  value={formData.tienGiamGia ?? 0}
                  onChange={(e) => handleChange('tienGiamGia', parseFloat(e.target.value) || 0)}
                />

                {/* Tỉ lệ thuế */}
                <div style={{ ...lblStyle, left: 10, top: 96, width: 80 }}>Tỉ lệ thuế</div>
                <input
                  type="number"
                  min="0"
                  style={inputStyle({ left: 110, top: 94, width: 50, textAlign: 'right' })}
                  value={formData.tiLeThue ?? 0}
                  onChange={(e) => handleChange('tiLeThue', parseFloat(e.target.value) || 0)}
                />
                <div style={{ ...lblStyle, left: 165, top: 96, width: 15 }}>%</div>
                <input
                  type="number"
                  min="0"
                  style={inputStyle({ left: 185, top: 94, width: 155, textAlign: 'right' })}
                  value={formData.tienThue ?? 0}
                  onChange={(e) => handleChange('tienThue', parseFloat(e.target.value) || 0)}
                />

                {/* Tổng cộng */}
                <div style={{ ...lblStyle, left: 10, top: 126, width: 80, fontWeight: 700 }}>Tổng cộng</div>
                <input
                  type="number"
                  readOnly
                  style={yellowInputStyle({ left: 110, top: 124, width: 230, textAlign: 'right', fontWeight: 700, color: '#1e3a8a' })}
                  value={formData.tongCong ?? 0}
                />

                {/* Tab Đặt hàng có thêm Thanh toán / Đặt cọc */}
                {tabId === 'datHang' && (
                  <>
                    <div style={{ ...lblStyle, left: 10, top: 154, width: 80 }}>Đặt cọc/T.Toán</div>
                    <input
                      type="number"
                      min="0"
                      style={inputStyle({ left: 110, top: 152, width: 230, textAlign: 'right', fontWeight: 600 })}
                      value={formData.thanhToan ?? 0}
                      onChange={(e) => handleChange('thanhToan', parseFloat(e.target.value) || 0)}
                    />
                  </>
                )}

                {/* Ghi chú */}
                <div style={{ ...lblStyle, left: 10, top: tabId === 'datHang' ? 184 : 156, width: 80 }}>Ghi chú</div>
                <textarea
                  style={{
                    ...inputStyle({
                      left: 110,
                      top: tabId === 'datHang' ? 182 : 154,
                      width: 230,
                      height: tabId === 'datHang' ? '240px' : '268px'
                    }),
                    padding: 6,
                    resize: 'none'
                  }}
                  value={formData.note || ''}
                  onChange={(e) => handleChange('note', e.target.value)}
                />
              </div>
            </>
          )}

          {/* ----------------------------------------------------------------------- */}
          {/* FORM 11: TĂNG GIẢM ĐIỂM (STABLEDESC TTANGGIAMDIEM 370x233)              */}
          {/* ----------------------------------------------------------------------- */}
          {tabId === 'tangGiamDiem' && (
            <>
              {/* Ngày & Số phiếu */}
              <div style={{ ...lblStyle, left: 10, top: 12, width: 35 }}>Ngày</div>
              <input
                type="date"
                required
                style={yellowInputStyle({ left: 75, top: 10, width: 95 })}
                value={formData.ngay || ''}
                onChange={(e) => handleChange('ngay', e.target.value)}
              />
              <div style={{ ...lblStyle, left: 180, top: 12, width: 50 }}>Số phiếu</div>
              <input
                type="text"
                required
                style={inputStyle({ left: 235, top: 10, width: 120, fontWeight: 700, background: '#f0fbfb', color: '#1e3a8a' })}
                value={formData.soPhiu || ''}
                onChange={(e) => handleChange('soPhiu', e.target.value)}
              />

              {/* Khách hàng */}
              <div style={{ ...lblStyle, left: 10, top: 40, width: 65 }}>Khách hàng</div>
              <select
                style={yellowInputStyle({ left: 75, top: 38, width: 280, height: 22, fontWeight: 600 })}
                value={formData.khachHangId || ''}
                onChange={(e) => handleSelectCustomer(e.target.value)}
              >
                <option value="">-- Chọn khách hàng --</option>
                {customers?.map(c => (
                  <option key={c.id} value={c.id}>[{c.maThe}] {c.tenKhachHang || c.name}</option>
                ))}
              </select>

              {/* Điểm tăng (+) & Điểm giảm (-) */}
              <div style={{ ...lblStyle, left: 10, top: 69, width: 60, color: '#16a34a', fontWeight: 700 }}>Điểm tăng</div>
              <input
                type="number"
                min="0"
                style={inputStyle({ left: 75, top: 67, width: 95, textAlign: 'right', fontWeight: 700, color: '#16a34a' })}
                value={formData.diemTang ?? 0}
                onChange={(e) => handleChange('diemTang', parseInt(e.target.value) || 0)}
              />
              <div style={{ ...lblStyle, left: 180, top: 69, width: 60, color: '#dc2626', fontWeight: 700 }}>Điểm giảm</div>
              <input
                type="number"
                min="0"
                style={inputStyle({ left: 245, top: 67, width: 110, textAlign: 'right', fontWeight: 700, color: '#dc2626' })}
                value={formData.diemGiam ?? 0}
                onChange={(e) => handleChange('diemGiam', parseInt(e.target.value) || 0)}
              />

              {/* Lý do */}
              <div style={{ ...lblStyle, left: 10, top: 98, width: 50 }}>Lý do</div>
              <input
                type="text"
                style={inputStyle({ left: 75, top: 96, width: 280 })}
                value={formData.lyDo || ''}
                onChange={(e) => handleChange('lyDo', e.target.value)}
              />

              {/* Ghi chú */}
              <div style={{ ...lblStyle, left: 10, top: 127, width: 50 }}>Ghi chú</div>
              <textarea
                style={{
                  ...inputStyle({ left: 75, top: 125, width: 280, height: '95px' }),
                  padding: 4,
                  resize: 'none'
                }}
                value={formData.note || ''}
                onChange={(e) => handleChange('note', e.target.value)}
              />
            </>
          )}

          {/* ----------------------------------------------------------------------- */}
          {/* FORM 12: VÀO RA (STABLEDESC TVAORA 400x205)                              */}
          {/* ----------------------------------------------------------------------- */}
          {tabId === 'vaoRa' && (
            <>
              {/* Ngày & Giờ */}
              <div style={{ ...lblStyle, left: 10, top: 12, width: 35 }}>Ngày</div>
              <input
                type="date"
                required
                style={yellowInputStyle({ left: 75, top: 10, width: 115 })}
                value={formData.ngay || ''}
                onChange={(e) => handleChange('ngay', e.target.value)}
              />
              <div style={{ ...lblStyle, left: 205, top: 12, width: 30 }}>Giờ</div>
              <input
                type="time"
                step="1"
                required
                style={inputStyle({ left: 245, top: 10, width: 140 })}
                value={formData.gio || ''}
                onChange={(e) => handleChange('gio', e.target.value)}
              />

              {/* Khách hàng */}
              <div style={{ ...lblStyle, left: 10, top: 41, width: 65 }}>Khách hàng</div>
              <select
                style={yellowInputStyle({ left: 75, top: 39, width: 310, height: 22, fontWeight: 600 })}
                value={formData.khachHangId || ''}
                onChange={(e) => handleSelectCustomer(e.target.value)}
              >
                <option value="">-- Chọn khách hàng --</option>
                {customers?.map(c => (
                  <option key={c.id} value={c.id}>[{c.maThe}] {c.tenKhachHang || c.name}</option>
                ))}
              </select>

              {/* Mã thẻ */}
              <div style={{ ...lblStyle, left: 10, top: 71, width: 50 }}>Mã thẻ</div>
              <input
                type="text"
                readOnly
                style={inputStyle({ left: 75, top: 69, width: 310, background: '#f8fafc', fontWeight: 600 })}
                value={formData.maThe || formData.maKhach || customer?.maThe || ''}
              />

              {/* Máy vân tay / Thiết bị */}
              <div style={{ ...lblStyle, left: 10, top: 101, width: 50 }}>Thiết bị</div>
              <select
                style={inputStyle({ left: 75, top: 99, width: 310, height: 22 })}
                value={formData.dmayVanTayId || ''}
                onChange={(e) => handleChange('dmayVanTayId', e.target.value)}
              >
                <option value="">-- Máy quẹt vân tay / Cửa kiểm soát --</option>
                {metadata.mayVanTay?.map(m => (
                  <option key={m.id} value={m.id}>{m.name}</option>
                ))}
              </select>

              {/* Ghi chú */}
              <div style={{ ...lblStyle, left: 10, top: 131, width: 50 }}>Ghi chú</div>
              <input
                type="text"
                style={inputStyle({ left: 75, top: 129, width: 310 })}
                value={formData.note || ''}
                onChange={(e) => handleChange('note', e.target.value)}
              />
            </>
          )}
        </div>

        {/* ========================================================================= */}
        {/* 5. BOTTOM BAR (LƯU & IN, LƯU & XEM IN, LƯU, LƯU & MỚI, LƯU & THOÁT, THOÁT)*/}
        {/* ========================================================================= */}
        <div
          style={{
            height: 38,
            background: 'linear-gradient(180deg, #dbe8f5 0%, #c4d7ea 100%)',
            borderTop: '1px solid #b2c9dd',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'space-between',
            padding: '0 8px',
            fontSize: 11,
            userSelect: 'none'
          }}
        >
          {/* Nút bên trái */}
          <div style={{ display: 'flex', gap: 6 }}>
            <button
              type="button"
              className="wf-btn"
              onClick={handleSubmit}
              style={{
                background: 'linear-gradient(180deg, #ffffff 0%, #e2ecf5 100%)',
                border: '1px solid #7ea2c2',
                borderRadius: 2,
                padding: '3px 8px',
                fontSize: 11,
                cursor: 'pointer',
                display: 'flex',
                alignItems: 'center',
                gap: 4
              }}
            >
              <span>🖨</span> Lưu & In
            </button>
            <button
              type="button"
              className="wf-btn"
              onClick={handleSubmit}
              style={{
                background: 'linear-gradient(180deg, #ffffff 0%, #e2ecf5 100%)',
                border: '1px solid #7ea2c2',
                borderRadius: 2,
                padding: '3px 8px',
                fontSize: 11,
                cursor: 'pointer',
                display: 'flex',
                alignItems: 'center',
                gap: 4
              }}
            >
              <span>📄</span> Lưu & Xem in
            </button>
          </div>

          {/* Nút bên phải */}
          <div style={{ display: 'flex', gap: 6 }}>
            <button
              type="button"
              disabled={saving}
              className="wf-btn"
              onClick={handleSubmit}
              style={{
                background: 'linear-gradient(180deg, #ffffff 0%, #e2ecf5 100%)',
                border: '1px solid #7ea2c2',
                borderRadius: 2,
                padding: '3px 12px',
                fontSize: 11,
                fontWeight: 600,
                cursor: 'pointer',
                display: 'flex',
                alignItems: 'center',
                gap: 4
              }}
            >
              <span>💾</span> {saving ? 'Đang lưu...' : 'Lưu'}
            </button>
            <button
              type="button"
              disabled={saving}
              className="wf-btn"
              onClick={handleSubmit}
              style={{
                background: 'linear-gradient(180deg, #ffffff 0%, #e2ecf5 100%)',
                border: '1px solid #7ea2c2',
                borderRadius: 2,
                padding: '3px 10px',
                fontSize: 11,
                cursor: 'pointer'
              }}
            >
              Lưu & Mới
            </button>
            <button
              type="button"
              disabled={saving}
              className="wf-btn"
              onClick={handleSubmit}
              style={{
                background: 'linear-gradient(180deg, #ffffff 0%, #e2ecf5 100%)',
                border: '1px solid #7ea2c2',
                borderRadius: 2,
                padding: '3px 10px',
                fontSize: 11,
                cursor: 'pointer'
              }}
            >
              Lưu & thoát
            </button>
            <button
              type="button"
              disabled={saving}
              className="wf-btn"
              onClick={onClose}
              style={{
                background: 'linear-gradient(180deg, #ffffff 0%, #e2ecf5 100%)',
                border: '1px solid #7ea2c2',
                borderRadius: 2,
                padding: '3px 12px',
                fontSize: 11,
                cursor: 'pointer'
              }}
            >
              Thoát
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}
