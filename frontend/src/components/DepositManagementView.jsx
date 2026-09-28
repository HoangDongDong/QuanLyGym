import React, { useState, useEffect, useRef } from 'react';
import { khachHangService } from '../services/khachHangService';
import { adminService } from '../services/adminService';
import SubtabAeModal from './SubtabAeModal';
import FastReportModal from './FastReportModal';
import './CustomerManagement.css';

const DEPOSIT_PRINT_COLUMNS = [
  { key: 'soPhieu', label: 'Số phiếu', width: '95px', defaultChecked: true },
  { key: 'ngay', label: 'Ngày', width: '85px', align: 'center', defaultChecked: true },
  { key: 'khachHang', label: 'Khách hàng', width: '140px', defaultChecked: true },
  { key: 'diaChi', label: 'Địa chỉ', width: '140px', defaultChecked: true },
  { key: 'dienThoai', label: 'Điện thoại', width: '100px', defaultChecked: true },
  { key: 'loaiThe', label: 'Loại thẻ', width: '105px', defaultChecked: true },
  { key: 'giaTaoGoi', label: 'Giá tạo gói', width: '95px', align: 'right', defaultChecked: true, format: (value) => Number(value || 0).toLocaleString('vi-VN') },
  { key: 'soTienCoc', label: 'Số tiền cọc', width: '95px', align: 'right', defaultChecked: true, format: (value) => Number(value || 0).toLocaleString('vi-VN') },
  { key: 'giamGia', label: 'Giảm giá (%)', width: '80px', align: 'right', defaultChecked: true },
  { key: 'tienGiam', label: 'Tiền giảm', width: '90px', align: 'right', defaultChecked: true, format: (value) => Number(value || 0).toLocaleString('vi-VN') },
  { key: 'trangThaiDangKy', label: 'Trạng thái', width: '105px', defaultChecked: true },
  { key: 'note', label: 'Ghi chú', width: '150px', defaultChecked: true }
];

export default function DepositManagementView({ showNotification }) {
  // --- STATE BỘ LỌC CỘT TRÁI ---
  const [fromDate, setFromDate] = useState(() => {
    const d = new Date();
    return new Date(d.getFullYear(), d.getMonth(), 1).toISOString().split('T')[0];
  });
  const [toDate, setToDate] = useState(() => {
    const d = new Date();
    return new Date(d.getFullYear(), d.getMonth() + 1, 0).toISOString().split('T')[0];
  });
  const [selectedLoaiTheId, setSelectedLoaiTheId] = useState('all');
  const [searchFilter, setSearchFilter] = useState('');

  // --- METADATA ---
  const [metadata, setMetadata] = useState({
    loaiThe: [],
    caTap: [],
    nhanVien: [],
    lyDoThuChi: [],
    mayVanTay: [],
    khoHang: [],
    cuaHang: []
  });
  const [customers, setCustomers] = useState([]);

  // --- DỮ LIỆU LƯỚI CHÍNH ---
  const [depositList, setDepositList] = useState([]);
  const [loading, setLoading] = useState(false);
  const [selectedRow, setSelectedRow] = useState(null);
  const [showFastReport, setShowFastReport] = useState(false);
  const [printCompanyInfo, setPrintCompanyInfo] = useState({
    name: '',
    address: '',
    phone: '',
    email: '',
    logoBase64: ''
  });

  // --- SPLIT PANEL ---
  const [leftWidth, setLeftWidth] = useState(200);
  const [isDraggingV, setIsDraggingV] = useState(false);
  const [bottomHeight, setBottomHeight] = useState(180);
  const [isDraggingH, setIsDraggingH] = useState(false);

  // --- BOTTOM TAB ---
  const [activeBottomTab, setActiveBottomTab] = useState('thongTin');
  const [modalState, setModalState] = useState({
    show: false,
    mode: 'create',
    tabId: 'datCoc',
    tabLabel: 'Đặt cọc',
    initialData: null,
    customer: null,
    sourceDepositId: null
  });

  // Load metadata (loại thẻ)
  useEffect(() => {
    const loadMeta = async () => {
      try {
        const res = await khachHangService.getMetadata();
        if (res?.success && res.data) setMetadata(res.data);
      } catch (e) {
        console.error('Lỗi load metadata:', e);
      }
    };
    loadMeta();
  }, []);

  useEffect(() => {
    khachHangService.getAll('', 'all')
      .then((res) => setCustomers(res?.data || []))
      .catch((err) => console.error('Lỗi tải khách hàng cho danh sách đặt cọc:', err));
  }, []);

  // Dùng cùng thông tin công ty/logo trong Cấu hình hệ thống như các màn in khác.
  useEffect(() => {
    const loadPrintCompanyInfo = async () => {
      try {
        const response = await adminService.getConfigs();
        const groups = response?.data || [];
        const normalizeKey = (value = '') => String(value)
          .normalize('NFD')
          .replace(/[\u0300-\u036f]/g, '')
          .replace(/[^a-zA-Z0-9]/g, '')
          .toLowerCase();
        const generalGroup = groups.find((group) => normalizeKey(group.groupName) === 'thongtinchung');
        const allItems = groups.flatMap((group) => group.items || []);
        const prioritizedItems = [...(generalGroup?.items || []), ...allItems];
        const findConfig = (aliases) => prioritizedItems.find((item) => {
          const name = normalizeKey(item.name);
          const caption = normalizeKey(item.caption);
          return aliases.includes(name) || aliases.includes(caption);
        });
        const readText = (aliases) => {
          const item = findConfig(aliases);
          return String(item?.textValue || item?.moreDetail || '').trim();
        };
        const logoItem = findConfig(['logo', 'logocongty', 'logodoanhnghiep']);

        setPrintCompanyInfo({
          name: readText(['companyname', 'tencongty', 'tendoanhnghiep', 'congty']),
          address: readText(['companyaddress', 'diachi', 'diachicongty', 'diachidoanhnghiep']),
          phone: readText(['companyphone', 'sodienthoai', 'dienthoai', 'dienthoaicongty', 'phone']),
          email: readText(['companyemail', 'email', 'emailcongty', 'emaildoanhnghiep']),
          logoBase64: logoItem?.blobValue || ''
        });
      } catch (err) {
        console.warn('Lỗi nạp thông tin công ty dùng cho bản in đặt cọc:', err);
      }
    };

    loadPrintCompanyInfo();
  }, []);

  // Load danh sách đặt cọc
  const fetchDepositList = async () => {
    setLoading(true);
    try {
      const params = { fromDate, toDate };
      if (selectedLoaiTheId && selectedLoaiTheId !== 'all') params.loaiTheId = selectedLoaiTheId;
      if (searchFilter.trim()) params.search = searchFilter.trim();

      const res = await khachHangService.getDatCocList(params);
      if (res?.success) {
        const rows = res.data || [];
        setDepositList(rows);
        setSelectedRow((current) => rows.find((row) => row.id === current?.id) || null);
      } else {
        setDepositList([]);
        setSelectedRow(null);
      }
    } catch (err) {
      console.error('Lỗi tải danh sách đặt cọc:', err);
      setDepositList([]);
      showNotification && showNotification('Lỗi kết nối khi tải danh sách đặt cọc!');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchDepositList();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [fromDate, toDate, selectedLoaiTheId]);

  // Kéo splitter dọc (trái / phải)
  const handleStartDragV = (e) => {
    e.preventDefault();
    setIsDraggingV(true);
    const startX = e.clientX;
    const startWidth = leftWidth;
    const onPointerMove = (ev) => {
      const newW = Math.max(160, Math.min(380, startWidth + (ev.clientX - startX)));
      setLeftWidth(newW);
    };
    const onPointerUp = () => {
      setIsDraggingV(false);
      window.removeEventListener('pointermove', onPointerMove);
      window.removeEventListener('pointerup', onPointerUp);
    };
    window.addEventListener('pointermove', onPointerMove);
    window.addEventListener('pointerup', onPointerUp);
  };

  // Kéo splitter ngang (trên / dưới)
  const handleStartDragH = (e) => {
    e.preventDefault();
    setIsDraggingH(true);
    const startY = e.clientY;
    const startHeight = bottomHeight;
    const onPointerMove = (ev) => {
      const deltaY = startY - ev.clientY;
      const newH = Math.max(120, Math.min(500, startHeight + deltaY));
      setBottomHeight(newH);
    };
    const onPointerUp = () => {
      setIsDraggingH(false);
      window.removeEventListener('pointermove', onPointerMove);
      window.removeEventListener('pointerup', onPointerUp);
    };
    window.addEventListener('pointermove', onPointerMove);
    window.addEventListener('pointerup', onPointerUp);
  };

  const handleSelectRow = (row) => setSelectedRow(row);

  const handleAddNew = () => {
    setModalState({
      show: true,
      mode: 'create',
      tabId: 'datCoc',
      tabLabel: 'Đặt cọc',
      initialData: null,
      customer: null,
      sourceDepositId: null
    });
  };
  const handleEdit = () => {
    if (!selectedRow) {
      window.alert('Mời bạn chọn một phiếu đặt cọc trước');
      return;
    }
    setModalState({
      show: true,
      mode: 'edit',
      tabId: 'datCoc',
      tabLabel: 'Đặt cọc',
      initialData: {
        ...selectedRow,
        khachHangId: selectedRow.khachHangId,
        tenKhach: selectedRow.khachHang,
        tenDoiTuong: selectedRow.khachHang,
        dloaiTheId: selectedRow.loaiTheId,
        giaTriGoi: selectedRow.giaTaoGoi,
        thu: selectedRow.soTienCoc,
        tongDat: selectedRow.soTienCoc,
        tienGiam: selectedRow.tienGiam
      },
      customer: null,
      sourceDepositId: null
    });
  };
  const handleDelete = async () => {
    if (!selectedRow) {
      window.alert('Mời bạn chọn một phiếu đặt cọc trước');
      return;
    }
    if (window.confirm(`Xóa phiếu đặt cọc "${selectedRow.soPhieu}"?`)) {
      try {
        const res = await khachHangService.deleteSubtabItem('datCoc', selectedRow.id);
        if (!res?.success) throw new Error(res?.message || 'Không thể xóa phiếu đặt cọc');
        showNotification && showNotification(`Đã xóa phiếu ${selectedRow.soPhieu}`);
        await fetchDepositList();
      } catch (err) {
        console.error('Lỗi xóa phiếu đặt cọc:', err);
        showNotification && showNotification(`Lỗi: ${err.message || 'Không thể xóa phiếu đặt cọc'}`);
      }
    }
  };
  const handleRegister = () => {
    if (!selectedRow) {
      window.alert('Mời bạn chọn một phiếu đặt cọc trước');
      return;
    }
    if (selectedRow.daDangKy) {
      window.alert('Phiếu đặt cọc này đã được đăng ký thẻ trước đó');
      return;
    }

    const customer = {
      id: selectedRow.khachHangId,
      tenKhachHang: selectedRow.khachHang,
      maThe: selectedRow.maKhach,
      diaChi: selectedRow.diaChi,
      dienThoai: selectedRow.dienThoai,
      loaiThe: selectedRow.loaiThe,
      dloaiTheId: selectedRow.loaiTheId,
      datTruoc: selectedRow.soTienCoc || 0,
      depositId: selectedRow.id
    };

    setModalState({
      show: true,
      mode: 'create',
      tabId: 'giaHanThe',
      tabLabel: 'Gia hạn thẻ',
      initialData: null,
      customer,
      sourceDepositId: selectedRow.id
    });
  };
  const handlePrint = () => setShowFastReport(true);

  const totalTienCoc = depositList.reduce((a, c) => a + (c.soTienCoc || 0), 0);
  const totalGiaTaoGoi = depositList.reduce((a, c) => a + (c.giaTaoGoi || 0), 0);

  return (
    <div className="cust-mgmt-container deposit-view" style={{ height: 'calc(100vh - 64px)' }}>
      {/* MAIN SPLIT BODY */}
      <div className="cust-split-body" style={{ flex: 1, display: 'flex', overflow: 'hidden' }}>

        {/* ===== CỘT TRÁI: LỌC LOẠI THẺ ===== */}
        <div
          className="cust-left-pane"
          style={{ width: leftWidth, minWidth: 160, maxWidth: 380, display: 'flex', flexDirection: 'column', boxSizing: 'border-box' }}
        >
          {/* Header */}
          <div style={{ height: 28, borderBottom: '1px solid rgba(226,232,240,0.45)', display: 'flex', alignItems: 'center', justifyContent: 'space-between', padding: '0 10px', fontSize: 12, fontWeight: 700, color: '#1e3a8a' }}>
            <span>Loại thẻ</span>
            <span style={{ cursor: 'pointer', color: '#64748b' }} title="Cấu hình loại thẻ">⚙️</span>
          </div>

          {/* Bộ lọc Ngày */}
          <div style={{ padding: '6px 8px', borderBottom: '1px solid rgba(226,232,240,0.45)', fontSize: 11 }}>
            <div style={{ marginBottom: 2, color: '#64748b', fontSize: 10.5 }}>Từ ngày</div>
            <input type="date" style={{ width: '100%', height: 22, fontSize: 11, padding: '1px 6px', boxSizing: 'border-box' }} value={fromDate} onChange={(e) => setFromDate(e.target.value)} />
            <div style={{ marginTop: 4, marginBottom: 2, color: '#64748b', fontSize: 10.5 }}>Đến ngày</div>
            <input type="date" style={{ width: '100%', height: 22, fontSize: 11, padding: '1px 6px', boxSizing: 'border-box' }} value={toDate} onChange={(e) => setToDate(e.target.value)} />
          </div>

          {/* Mini Toolbar */}
          <div className="cust-tree-toolbar">
            <button type="button" className="cust-tree-btn" title="Thêm loại thẻ mới" onClick={() => showNotification && showNotification('Thêm loại thẻ mới')}>
              <i className="fa-solid fa-plus" style={{ color: '#16a34a' }}></i>
            </button>
            <button type="button" className="cust-tree-btn" title="Chỉnh sửa loại thẻ" onClick={() => showNotification && showNotification('Chỉnh sửa loại thẻ')}>
              <i className="fa-solid fa-pen-to-square" style={{ color: '#d97706' }}></i>
            </button>
            <button type="button" className="cust-tree-btn" title="Nạp lại danh mục" onClick={fetchDepositList}>
              <i className="fa-solid fa-rotate" style={{ color: '#0284c7' }}></i>
            </button>
          </div>

          {/* CÂY LOẠI THẺ */}
          <div className="cust-tree-list" style={{ flex: 1, overflowY: 'auto', background: 'transparent', padding: '4px 0', fontSize: 11.5 }}>
            {/* Tất cả */}
            <div
              className={`cust-tree-item ${selectedLoaiTheId === 'all' ? 'active' : ''}`}
              onClick={() => setSelectedLoaiTheId('all')}
            >
              <div className="cust-tree-label">
                <span style={{ color: '#0284c7' }}>📁</span>
                <span style={{ fontWeight: selectedLoaiTheId === 'all' ? 700 : 400 }}>Tất cả</span>
              </div>
            </div>

            {/* Chưa thiết lập */}
            <div
              className={`cust-tree-item ${selectedLoaiTheId === 'chuaThietLap' ? 'active' : ''}`}
              onClick={() => setSelectedLoaiTheId('chuaThietLap')}
              style={{ paddingLeft: 28 }}
            >
              <div className="cust-tree-label">
                <span style={{ color: '#94a3b8' }}>📄</span>
                <span>Chưa thiết lập</span>
              </div>
            </div>

            {/* Danh sách loại thẻ */}
            {metadata.loaiThe?.map((lt) => (
              <div
                key={lt.id}
                className={`cust-tree-item ${selectedLoaiTheId === lt.id ? 'active' : ''}`}
                onClick={() => setSelectedLoaiTheId(lt.id)}
                style={{ paddingLeft: 28 }}
              >
                <div className="cust-tree-label">
                  <span style={{ color: '#16a34a', fontSize: 12 }}>💳</span>
                  <span style={{ fontWeight: selectedLoaiTheId === lt.id ? 700 : 400 }}>{lt.name}</span>
                </div>
              </div>
            ))}

            {/* Thùng rác */}
            <div style={{ borderTop: '1px dashed rgba(226,232,240,0.6)', marginTop: 6 }}>
              <div
                className={`cust-tree-item ${selectedLoaiTheId === 'trash' ? 'active' : ''}`}
                onClick={() => setSelectedLoaiTheId('trash')}
              >
                <div className="cust-tree-label" style={{ color: '#dc2626' }}>
                  <span>🗑️</span>
                  <span>Thùng rác</span>
                </div>
              </div>
            </div>
          </div>
        </div>

        {/* SPLITTER DỌC */}
        <div
          className={`cust-vertical-splitter ${isDraggingV ? 'dragging' : ''}`}
          onPointerDown={handleStartDragV}
          title="Kéo để co dãn"
        >
          <div className="splitter-grip-vertical">
            <span></span><span></span><span></span>
          </div>
        </div>

        {/* ===== KHU VỰC BÊN PHẢI ===== */}
        <div className="cust-right-pane deposit-right-pane" style={{ flex: 1, display: 'flex', flexDirection: 'column', overflow: 'hidden' }}>

          {/* HEADER */}
          <div className="cust-main-header">
            <div className="cust-header-title-group">
              <div className="cust-header-icon-box" style={{ background: '#10b981' }}>
                <i className="fa-solid fa-file-invoice-dollar"></i>
              </div>
              <div className="cust-header-title-text">
                <h2>Danh sách đặt cọc</h2>
                <p>Quản lý các phiếu đặt cọc và lịch sử thanh toán</p>
              </div>
            </div>
          </div>

          {/* GRID AREA */}
          <div style={{ flex: 1, display: 'flex', flexDirection: 'column', overflow: 'hidden', background: 'transparent' }}>

            {/* TOOLBAR RIBBON */}
            <div className="cust-ribbon-bar">
              <div className="cust-ribbon-filter">
                <span>Lọc (F3):</span>
                <input
                  type="text"
                  placeholder="Tìm kiếm phiếu, khách hàng, SĐT..."
                  value={searchFilter}
                  onChange={(e) => setSearchFilter(e.target.value)}
                  onKeyDown={(e) => { if (e.key === 'Enter') fetchDepositList(); }}
                />
              </div>

              <button type="button" className="cust-ribbon-btn primary" onClick={handleAddNew} title="Thêm mới phiếu đặt cọc (Insert)">
                <i className="fa-solid fa-plus" style={{ color: '#ffffff' }}></i>
                <span>Thêm mới (Insert)</span>
              </button>

              <button type="button" className="cust-ribbon-btn edit" onClick={handleEdit} title="Chỉnh sửa phiếu đặt cọc (F4)">
                <i className="fa-solid fa-pen" style={{ color: '#d97706' }}></i>
                <span>Chỉnh sửa (F4)</span>
              </button>

              <button type="button" className="cust-ribbon-btn danger" onClick={handleDelete} title="Xóa phiếu đặt cọc (Del)">
                <i className="fa-solid fa-xmark" style={{ color: '#e11d48' }}></i>
                <span>Xóa (Del)</span>
              </button>

              <div className="cust-ribbon-sep"></div>

              <button type="button" className="cust-ribbon-btn print" onClick={handlePrint} title="In danh sách đặt cọc">
                <i className="fa-solid fa-print" style={{ color: '#475569' }}></i>
                <span>In</span>
              </button>

              <button
                type="button"
                className="cust-ribbon-btn print"
                onClick={() => showNotification && showNotification(`Tổng tiền cọc: ${totalTienCoc.toLocaleString()} đ | Giá tạo gói: ${totalGiaTaoGoi.toLocaleString()} đ`)}
                title="Xem tổng tiền đặt cọc"
              >
                <span>Σ Tổng</span>
              </button>

              <div className="cust-ribbon-sep"></div>

              <button
                type="button"
                className="cust-ribbon-btn primary"
                onClick={handleRegister}
                title="Đăng ký khách hàng đặt cọc thành hội viên"
              >
                <i className="fa-solid fa-user-plus" style={{ color: '#ffffff' }}></i>
                <span>Đăng ký</span>
              </button>

              <button
                type="button"
                className="cust-ribbon-btn device"
                onClick={fetchDepositList}
                title="Hiển thị tất cả đặt cọc"
              >
                <i className="fa-solid fa-list" style={{ color: '#2563eb' }}></i>
                <span>Hiển thị tất cả</span>
              </button>
            </div>

            {/* BẢNG LƯỚI CHÍNH */}
            <div style={{ flex: 1, overflow: 'auto', background: 'transparent' }}>
              <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: 11.5, fontFamily: 'Segoe UI, Tahoma, sans-serif' }}>
                <thead>
                  <tr style={{ background: 'rgba(255,255,255,0.55)', backdropFilter: 'blur(8px)', borderBottom: '1px solid rgba(203,213,225,0.5)', height: 28, textAlign: 'left', position: 'sticky', top: 0, zIndex: 2 }}>
                    <th style={{ width: 28, textAlign: 'center', borderRight: '1px solid rgba(203,213,225,0.4)', padding: '0 4px' }}></th>
                    <th style={{ padding: '0 8px', borderRight: '1px solid rgba(203,213,225,0.4)', whiteSpace: 'nowrap' }}>Số phiếu</th>
                    <th style={{ padding: '0 8px', borderRight: '1px solid rgba(203,213,225,0.4)', whiteSpace: 'nowrap' }}>Ngày</th>
                    <th style={{ padding: '0 8px', borderRight: '1px solid rgba(203,213,225,0.4)', whiteSpace: 'nowrap', minWidth: 130 }}>Khách hàng</th>
                    <th style={{ padding: '0 8px', borderRight: '1px solid rgba(203,213,225,0.4)', whiteSpace: 'nowrap', minWidth: 110 }}>Địa chỉ</th>
                    <th style={{ padding: '0 8px', borderRight: '1px solid rgba(203,213,225,0.4)', textAlign: 'right', whiteSpace: 'nowrap' }}>Số tiền cọc</th>
                    <th style={{ padding: '0 8px', borderRight: '1px solid rgba(203,213,225,0.4)', textAlign: 'right', whiteSpace: 'nowrap' }}>Giá tạo gói</th>
                    <th style={{ padding: '0 8px', borderRight: '1px solid rgba(203,213,225,0.4)', whiteSpace: 'nowrap' }}>Loại thẻ</th>
                    <th style={{ padding: '0 8px', borderRight: '1px solid rgba(203,213,225,0.4)', whiteSpace: 'nowrap' }}>Điện thoại</th>
                    <th style={{ padding: '0 8px', borderRight: '1px solid rgba(203,213,225,0.4)', whiteSpace: 'nowrap', minWidth: 100 }}>Ghi chú</th>
                    <th style={{ padding: '0 8px', borderRight: '1px solid rgba(203,213,225,0.4)', textAlign: 'right', whiteSpace: 'nowrap' }}>Giảm giá</th>
                    <th style={{ padding: '0 8px', textAlign: 'right', whiteSpace: 'nowrap' }}>Tiền giảm</th>
                  </tr>
                </thead>
                <tbody>
                  {loading ? (
                    <tr>
                      <td colSpan={12} style={{ textAlign: 'center', padding: 24, color: '#64748b' }}>
                        <i className="fa-solid fa-spinner fa-spin"></i> Đang tải danh sách đặt cọc...
                      </td>
                    </tr>
                  ) : depositList.length === 0 ? (
                    <tr>
                      <td colSpan={12} style={{ textAlign: 'center', padding: 24, color: '#94a3b8' }}>
                        Không có phiếu đặt cọc nào phù hợp với bộ lọc.
                      </td>
                    </tr>
                  ) : (
                    depositList.map((row, idx) => {
                      const isSelected = selectedRow?.id === row.id;
                      return (
                        <tr
                          key={row.id || idx}
                          className={isSelected ? 'selected' : ''}
                          onClick={() => handleSelectRow(row)}
                          onDoubleClick={handleEdit}
                          style={{
                            height: 25,
                            background: isSelected
                              ? 'rgba(59,130,246,0.16)'
                              : (idx % 2 === 1 ? 'rgba(255,255,255,0.22)' : 'transparent'),
                            borderBottom: '1px solid rgba(226,232,240,0.4)',
                            cursor: 'pointer'
                          }}
                        >
                          <td style={{ textAlign: 'center', borderRight: '1px solid rgba(241,245,249,0.6)', color: '#0284c7', fontSize: 12 }}>
                            {isSelected ? '▶' : idx + 1}
                          </td>
                          <td style={{ padding: '0 8px', borderRight: '1px solid rgba(241,245,249,0.6)', fontWeight: 600, color: '#1e3a8a' }}>
                            {row.soPhieu}
                          </td>
                          <td style={{ padding: '0 8px', borderRight: '1px solid rgba(241,245,249,0.6)', color: isSelected ? '#1e3a8a' : '#0f2942', background: isSelected ? 'rgba(59,130,246,0.18)' : 'rgba(255,255,220,0.5)', fontWeight: 600 }}>
                            {row.ngay}
                          </td>
                          <td style={{ padding: '0 8px', borderRight: '1px solid rgba(241,245,249,0.6)', fontWeight: 600 }}>
                            {row.khachHang}
                          </td>
                          <td style={{ padding: '0 8px', borderRight: '1px solid rgba(241,245,249,0.6)', color: '#475569' }}>
                            {row.diaChi}
                          </td>
                          <td style={{ padding: '0 8px', borderRight: '1px solid rgba(241,245,249,0.6)', textAlign: 'right', fontWeight: 700, color: '#dc2626' }}>
                            {(row.soTienCoc || 0).toLocaleString()}
                          </td>
                          <td style={{ padding: '0 8px', borderRight: '1px solid rgba(241,245,249,0.6)', textAlign: 'right', fontWeight: 700, color: '#0f2942' }}>
                            {(row.giaTaoGoi || 0).toLocaleString()}
                          </td>
                          <td style={{ padding: '0 8px', borderRight: '1px solid rgba(241,245,249,0.6)' }}>
                            {row.loaiThe && (
                              <span style={{ display: 'inline-flex', alignItems: 'center', gap: 4 }}>
                                <span style={{ color: '#16a34a' }}>💳</span>
                                {row.loaiThe}
                              </span>
                            )}
                          </td>
                          <td style={{ padding: '0 8px', borderRight: '1px solid rgba(241,245,249,0.6)', color: '#475569' }}>
                            {row.dienThoai}
                          </td>
                          <td style={{ padding: '0 8px', borderRight: '1px solid rgba(241,245,249,0.6)', color: '#64748b', fontStyle: row.note ? 'normal' : 'italic' }}>
                            {row.note || row.dienGiai || ''}
                          </td>
                          <td style={{ padding: '0 8px', borderRight: '1px solid rgba(241,245,249,0.6)', textAlign: 'right' }}>
                            {row.giamGia ? `${row.giamGia}%` : 0}
                          </td>
                          <td style={{ padding: '0 8px', textAlign: 'right' }}>
                            {(row.tienGiam || 0).toLocaleString()}
                          </td>
                        </tr>
                      );
                    })
                  )}
                </tbody>
              </table>
            </div>
          </div>

          {/* SPLITTER NGANG */}
          <div
            onPointerDown={handleStartDragH}
            style={{
              height: 6,
              background: 'rgba(255,255,255,0.35)',
              borderTop: '1px solid rgba(226,232,240,0.5)',
              borderBottom: '1px solid rgba(226,232,240,0.5)',
              cursor: 'row-resize',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              userSelect: 'none'
            }}
          >
            <div style={{ width: 40, height: 2, background: '#94a3b8', borderRadius: 1 }} />
          </div>

          {/* SUBTABS PHÍA DƯỚI */}
          <div
            style={{
              height: bottomHeight,
              background: 'rgba(255,255,255,0.38)',
              backdropFilter: 'blur(14px)',
              WebkitBackdropFilter: 'blur(14px)',
              border: '1px solid rgba(255,255,255,0.6)',
              borderRadius: 14,
              boxShadow: '0 6px 20px rgba(0,0,0,0.02)',
              display: 'flex',
              flexDirection: 'column',
              boxSizing: 'border-box',
              overflow: 'hidden'
            }}
          >
            {/* Tab bar */}
            <div style={{ display: 'flex', borderBottom: '1px solid rgba(226,232,240,0.5)', background: 'rgba(255,255,255,0.3)', overflowX: 'auto', flexShrink: 0 }}>
              {[
                { id: 'thongTin', label: 'Thông tin' },
                { id: 'chiTietThanhToan', label: 'Chi tiết thanh toán' },
                { id: 'giaHanThe', label: 'Gia hạn thẻ' },
                { id: 'baoLuuThe', label: 'Bảo lưu thẻ' },
                { id: 'doiLoaiThe', label: 'Đổi loại thẻ' }
              ].map(tab => (
                <div
                  key={tab.id}
                  onClick={() => setActiveBottomTab(tab.id)}
                  style={{
                    padding: '6px 14px',
                    fontSize: 12,
                    fontWeight: activeBottomTab === tab.id ? 700 : 400,
                    color: activeBottomTab === tab.id ? '#2563eb' : '#475569',
                    borderBottom: activeBottomTab === tab.id ? '2px solid #2563eb' : '2px solid transparent',
                    cursor: 'pointer',
                    whiteSpace: 'nowrap',
                    userSelect: 'none',
                    transition: 'all 0.15s ease'
                  }}
                >
                  {tab.label}
                </div>
              ))}
            </div>

            {/* Tab content */}
            <div style={{ flex: 1, overflowY: 'auto', padding: '10px 16px', fontSize: 12 }}>
              {activeBottomTab === 'thongTin' && selectedRow ? (
                <div>
                  <div style={{ color: '#64748b', marginBottom: 6 }}>
                    <span style={{ marginRight: 16 }}>
                      Khởi tạo: <strong>{selectedRow.timeCreated}</strong>
                      {selectedRow.nguoiTao && <> bởi:<strong>{selectedRow.nguoiTao}</strong></>}
                    </span>
                    {selectedRow.timeModified && (
                      <span>
                        Sửa đổi gần nhất: <strong>{selectedRow.timeModified}</strong>
                        {selectedRow.nguoiSua && <> bởi:<strong>{selectedRow.nguoiSua}</strong></>}
                      </span>
                    )}
                  </div>
                  <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(180px, 1fr))', gap: 10, marginTop: 8 }}>
                    {[
                      { label: 'SỐ PHIẾU', value: selectedRow.soPhieu },
                      { label: 'NGÀY', value: selectedRow.ngay },
                      { label: 'KHÁCH HÀNG', value: selectedRow.khachHang },
                      { label: 'MÃ KHÁCH', value: selectedRow.maKhach },
                      { label: 'ĐIỆN THOẠI', value: selectedRow.dienThoai },
                      { label: 'ĐỊA CHỈ', value: selectedRow.diaChi },
                      { label: 'LOẠI THẺ', value: selectedRow.loaiThe },
                      { label: 'SỐ TIỀN CỌC', value: (selectedRow.soTienCoc || 0).toLocaleString() + ' đ' },
                      { label: 'GIÁ TẠO GÓI', value: (selectedRow.giaTaoGoi || 0).toLocaleString() + ' đ' },
                      { label: 'GHI CHÚ', value: selectedRow.note || selectedRow.dienGiai || '---' },
                    ].map(({ label, value }) => (
                      <div key={label} style={{ background: 'rgba(255,255,255,0.6)', backdropFilter: 'blur(8px)', border: '1px solid rgba(226,232,240,0.7)', borderRadius: 10, padding: '8px 12px' }}>
                        <div style={{ fontSize: 9.5, fontWeight: 700, textTransform: 'uppercase', color: '#64748b', letterSpacing: 0.3, marginBottom: 2 }}>{label}</div>
                        <div style={{ fontSize: 12, fontWeight: 650, color: '#1e293b', overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>{value || '---'}</div>
                      </div>
                    ))}
                  </div>
                </div>
              ) : activeBottomTab === 'thongTin' ? (
                <div style={{ color: '#94a3b8', fontStyle: 'italic', textAlign: 'center', paddingTop: 20 }}>
                  Chọn một phiếu đặt cọc để xem thông tin chi tiết
                </div>
              ) : (
                <div style={{ color: '#94a3b8', fontStyle: 'italic', textAlign: 'center', paddingTop: 20 }}>
                  {selectedRow
                    ? `Chưa có dữ liệu cho tab này (phiếu: ${selectedRow.soPhieu})`
                    : 'Chọn một phiếu đặt cọc để xem thông tin'}
                </div>
              )}
            </div>
          </div>
        </div>
      </div>

      {modalState.show && (
        <SubtabAeModal
          show={modalState.show}
          mode={modalState.mode}
          tabId={modalState.tabId}
          tabLabel={modalState.tabLabel}
          customer={modalState.customer}
          customers={customers}
          initialData={modalState.initialData}
          metadata={metadata}
          onClose={() => setModalState((current) => ({ ...current, show: false }))}
          onSave={async (tabId, data, mode) => {
            if (mode === 'edit' && data.id) {
              await khachHangService.updateSubtabItem(tabId, data.id, data);
            } else {
              const result = await khachHangService.createSubtabItem(tabId, data);
              if (tabId === 'giaHanThe' && modalState.sourceDepositId && result?.id) {
                await khachHangService.registerDeposit(modalState.sourceDepositId, result.id);
              }
            }
            showNotification && showNotification(
              tabId === 'giaHanThe' ? 'Đăng ký thẻ từ phiếu đặt cọc thành công!' : 'Lưu phiếu đặt cọc thành công!'
            );
            await fetchDepositList();
          }}
        />
      )}

      <FastReportModal
        show={showFastReport}
        onClose={() => setShowFastReport(false)}
        rows={depositList.map((row) => ({
          ...row,
          note: row.note || row.dienGiai || '',
          trangThaiDangKy: row.daDangKy ? 'Đã đăng ký' : 'Chưa đăng ký'
        }))}
        columns={DEPOSIT_PRINT_COLUMNS}
        initialTitle="Danh sách đặt cọc"
        sheetName="DanhSachDatCoc"
        companyInfo={printCompanyInfo}
        showNotification={showNotification}
      />
    </div>
  );
}
