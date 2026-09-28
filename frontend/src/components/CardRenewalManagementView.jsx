import React, { useState, useEffect, useRef } from 'react';
import { khachHangService } from '../services/khachHangService';
import SubtabAeModal from './SubtabAeModal';
import './CustomerManagement.css';

export default function CardRenewalManagementView({ onSwitchToCustomer, showNotification, openAddNewTrigger, prefillCustomer, onResetOpenAddTrigger }) {
  // --- STATE BỘ LỌC CỘT TRÁI ---
  const [fromDate, setFromDate] = useState(() => {
    const d = new Date();
    return new Date(d.getFullYear(), d.getMonth(), 1).toISOString().split('T')[0];
  });
  const [toDate, setToDate] = useState(() => {
    const d = new Date();
    return new Date(d.getFullYear(), d.getMonth() + 1, 0).toISOString().split('T')[0];
  });
  const [loaiGiaoDich, setLoaiGiaoDich] = useState('all');
  const [selectedLoaiTheId, setSelectedLoaiTheId] = useState('all');
  const [searchFilter, setSearchFilter] = useState('');

  // --- METADATA (LOẠI THẺ, CA TẬP, NHÂN VIÊN, MÁY VÂN TAY...) ---
  const [metadata, setMetadata] = useState({
    loaiThe: [],
    caTap: [],
    nhanVien: [],
    lyDoThuChi: [],
    mayVanTay: [],
    khoHang: [],
    cuaHang: []
  });

  // --- DỮ LIỆU LƯỚI CHÍNH GIA HẠN THẺ ---
  const [renewalList, setRenewalList] = useState([]);
  const [customers, setCustomers] = useState([]);
  const [loading, setLoading] = useState(false);
  const [selectedRow, setSelectedRow] = useState(null);

  // --- DỮ LIỆU CÁC SUBTAB PHÍA DƯỚI ---
  const [activeBottomTab, setActiveBottomTab] = useState('thongTin');
  const [subtabsData, setSubtabsData] = useState(null);
  const [loadingSubtabs, setLoadingSubtabs] = useState(false);

  // --- CO DÃN SPLITTER ---
  const [leftWidth, setLeftWidth] = useState(210);
  const [bottomHeight, setBottomHeight] = useState(210);
  const [isDraggingH, setIsDraggingH] = useState(false);
  const [isDraggingV, setIsDraggingV] = useState(false);

  // --- MODAL SUBTAB AE FORM (THÊM / SỬA) ---
  const [modalState, setModalState] = useState({
    show: false,
    mode: 'create', // 'create' | 'edit'
    tabId: 'giaHanThe',
    tabLabel: 'Gia hạn thẻ',
    initialData: null
  });

  // --- CONTEXT MENU CHUỘT PHẢI LƯỚI CHÍNH ---
  const [mainContextMenu, setMainContextMenu] = useState({
    visible: false,
    x: 0,
    y: 0,
    row: null,
    colKey: '',
    cellValue: ''
  });

  // --- CONTEXT MENU CHUỘT PHẢI CÁC SUBTAB BÊN DƯỚI ---
  const [subtabContextMenu, setSubtabContextMenu] = useState({
    visible: false,
    x: 0,
    y: 0,
    tabId: '',
    tabLabel: '',
    item: null,
    colKey: '',
    cellValue: ''
  });

  const [showSortSubmenu, setShowSortSubmenu] = useState(false);
  const [showSubtabSortSubmenu, setShowSubtabSortSubmenu] = useState(false);

  // Danh sách các tab chi tiết bên dưới (khớp 100% screenshot người dùng)
  const bottomTabs = [
    { id: 'thongTin', label: 'Thông tin' },
    { id: 'giaHanThe', label: 'Gia hạn thẻ' },
    { id: 'baoLuuThe', label: 'Bảo lưu thẻ' },
    { id: 'doiLoaiThe', label: 'Đổi loại thẻ' },
    { id: 'phieuThu', label: 'Phiếu thu' },
    { id: 'phieuChi', label: 'Phiếu chi' },
    { id: 'thuCongNo', label: 'Phiếu thu công nợ' },
    { id: 'datCoc', label: 'Đặt cọc' },
    { id: 'vaoRa', label: 'Vào ra' }
  ];

  // 1. Tải danh mục metadata
  useEffect(() => {
    khachHangService.getMetadata()
      .then(res => {
        if (res?.data) {
          setMetadata(res.data);
        }
      })
      .catch(err => console.error('Lỗi tải metadata:', err));
  }, []);

  // Dropdown trên form giao dịch phải lấy toàn bộ DKHACHHANG, không lấy từ
  // renewalList vì danh sách đó chỉ chứa khách đã từng phát sinh giao dịch.
  const fetchCustomers = async () => {
    try {
      const res = await khachHangService.getAll('', 'all');
      if (res?.data) {
        const normalizedCustomers = res.data.map((item) => ({
          ...item,
          dloaiTheId: item.dLoaiTheId || item.dloaiTheId || '',
          dcatapId: item.dCaTapId || item.dcatapId || '',
          nhanVienId: item.dNhanVienId || item.nhanVienId || ''
        }));
        setCustomers(normalizedCustomers);
        return normalizedCustomers;
      }
      return [];
    } catch (err) {
      console.error('Lỗi tải danh mục khách hàng:', err);
      showNotification && showNotification('Không thể tải danh mục khách hàng!');
      return [];
    }
  };

  useEffect(() => {
    fetchCustomers();
  }, []);

  // 2. Tải danh sách giao dịch gia hạn thẻ
  const fetchRenewalList = async () => {
    setLoading(true);
    try {
      const res = await khachHangService.getGiaHanTheList({
        fromDate,
        toDate,
        loaiGiaoDich: loaiGiaoDich !== 'all' ? loaiGiaoDich : undefined,
        loaiTheId: selectedLoaiTheId !== 'all' ? selectedLoaiTheId : undefined,
        search: searchFilter.trim() || undefined
      });
      if (res?.data) {
        setRenewalList(res.data);
        if (res.data.length > 0) {
          // Tự động chọn dòng đầu tiên hoặc giữ dòng hiện tại nếu còn tồn tại
          const match = selectedRow ? res.data.find(r => r.id === selectedRow.id) : null;
          handleSelectRow(match || res.data[0]);
        } else {
          setSelectedRow(null);
          setSubtabsData(null);
        }
      }
    } catch (err) {
      console.error('Lỗi tải danh sách gia hạn thẻ:', err);
      showNotification && showNotification('Lỗi kết nối khi tải danh sách gia hạn thẻ!');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchRenewalList();
  }, [fromDate, toDate, loaiGiaoDich, selectedLoaiTheId]);

  // 3. Chọn một dòng trên lưới chính -> Tải subtabs của khách hàng tương ứng
  const handleSelectRow = async (row) => {
    setSelectedRow(row);
    if (!row || !row.khachHangId) {
      setSubtabsData(null);
      return;
    }
    setLoadingSubtabs(true);
    try {
      const res = await khachHangService.getCustomerSubtabs(row.khachHangId);
      if (res?.data) {
        setSubtabsData(res.data);
      }
    } catch (err) {
      console.error('Lỗi tải subtabs khách hàng:', err);
    } finally {
      setLoadingSubtabs(false);
    }
  };

  // 4. Đóng menu chuột phải khi click ra ngoài
  useEffect(() => {
    const handleOutside = (e) => {
      if (e.target.closest('.wf-context-menu') || e.target.closest('.wf-submenu')) return;
      if (mainContextMenu.visible) setMainContextMenu(p => ({ ...p, visible: false }));
      if (subtabContextMenu.visible) setSubtabContextMenu(p => ({ ...p, visible: false }));
      setShowSortSubmenu(false);
      setShowSubtabSortSubmenu(false);
    };
    document.addEventListener('pointerdown', handleOutside);
    return () => document.removeEventListener('pointerdown', handleOutside);
  }, [mainContextMenu.visible, subtabContextMenu.visible]);

  // --- XỬ LÝ CONTEXT MENU LƯỚI CHÍNH ---
  const handleMainContextMenu = (e, row, colKey, val) => {
    e.preventDefault();
    e.stopPropagation();
    setSelectedRow(row);
    const menuWidth = 195;
    const menuHeight = 300;
    const x = e.clientX + menuWidth > window.innerWidth ? window.innerWidth - menuWidth - 10 : e.clientX;
    const y = e.clientY + menuHeight > window.innerHeight ? window.innerHeight - menuHeight - 10 : e.clientY;
    setMainContextMenu({
      visible: true,
      x,
      y,
      row,
      colKey,
      cellValue: val != null ? String(val) : ''
    });
  };

  // --- XỬ LÝ CONTEXT MENU SUBTABS BÊN DƯỚI ---
  const handleSubtabContextMenu = (e, tabId, item, colKey, val) => {
    e.preventDefault();
    e.stopPropagation();
    const tabObj = bottomTabs.find(t => t.id === tabId);
    const tabLabel = tabObj ? tabObj.label : 'Bản ghi';
    const menuWidth = 195;
    const menuHeight = 320;
    const x = e.clientX + menuWidth > window.innerWidth ? window.innerWidth - menuWidth - 10 : e.clientX;
    const y = e.clientY + menuHeight > window.innerHeight ? window.innerHeight - menuHeight - 10 : e.clientY;
    setSubtabContextMenu({
      visible: true,
      x,
      y,
      tabId,
      tabLabel,
      item,
      colKey,
      cellValue: val != null ? String(val) : ''
    });
  };

  // Thêm mới giao dịch gia hạn thẻ
  const handleAddNewRenewal = async () => {
    // Luôn nạp lại để khách vừa thêm ở màn hình Danh mục xuất hiện ngay.
    const latestCustomers = await fetchCustomers();
    const preferredCustomer = prefillCustomer?.id
      ? latestCustomers.find((item) => String(item.id) === String(prefillCustomer.id)) || prefillCustomer
      : null;
    setModalState({
      show: true,
      mode: 'create',
      tabId: 'giaHanThe',
      tabLabel: 'Gia hạn thẻ',
      initialData: preferredCustomer ? {
        khachHangId: preferredCustomer.id,
        tenKhach: preferredCustomer.tenKhachHang || preferredCustomer.name,
        maKhach: preferredCustomer.maThe || preferredCustomer.maKhach,
        dienThoai: preferredCustomer.dienThoai || '',
        diaChi: preferredCustomer.diaChi || '',
        email: preferredCustomer.email || '',
        loaiThe: preferredCustomer.loaiThe || '',
        dloaiTheId: preferredCustomer.dloaiTheId || preferredCustomer.dLoaiTheId || '',
        dcatapId: preferredCustomer.dcatapId || preferredCustomer.dCaTapId || '',
        tuNgay: preferredCustomer.tuNgay,
        denNgay: preferredCustomer.denNgay,
        soLan: preferredCustomer.soLan || 0,
        caTap: preferredCustomer.caTap || 'Cả ngày'
      } : selectedRow ? {
        khachHangId: selectedRow.khachHangId,
        tenKhach: selectedRow.khachHang,
        maKhach: selectedRow.maKhach,
        loaiTheId: selectedRow.loaiTheId
      } : null
    });
  };

  // Tự động mở form Thêm mới Gia hạn thẻ CHỈ khi người dùng click vào 'Gia hạn thẻ' trên sidebar nav
  const lastTriggerHandledRef = useRef(0);
  useEffect(() => {
    if (openAddNewTrigger && openAddNewTrigger > 0 && openAddNewTrigger !== lastTriggerHandledRef.current) {
      lastTriggerHandledRef.current = openAddNewTrigger;
      handleAddNewRenewal();
      if (typeof onResetOpenAddTrigger === 'function') {
        onResetOpenAddTrigger();
      }
    }
  }, [openAddNewTrigger]);

  // Sửa giao dịch gia hạn thẻ
  const handleEditRenewal = (rowToEdit) => {
    const row = rowToEdit || selectedRow;
    if (!row) {
      showNotification && showNotification('Vui lòng chọn một phiếu gia hạn thẻ để chỉnh sửa!');
      return;
    }
    setModalState({
      show: true,
      mode: 'edit',
      tabId: row.loaiGiaoDich === '2' ? 'doiLoaiThe' : (row.loaiGiaoDich === '9' ? 'baoLuuThe' : 'giaHanThe'),
      tabLabel: row.loaiGiaoDich === '2' ? 'Đổi loại thẻ' : (row.loaiGiaoDich === '9' ? 'Bảo lưu thẻ' : 'Gia hạn thẻ'),
      initialData: row
    });
  };

  // Xóa giao dịch gia hạn thẻ
  const handleDeleteRenewal = async (rowToDelete) => {
    const row = rowToDelete || selectedRow;
    if (!row) {
      showNotification && showNotification('Vui lòng chọn phiếu cần xóa!');
      return;
    }
    if (window.confirm(`Bạn có chắc chắn muốn xóa phiếu gia hạn '${row.soPhiu}' của khách hàng '${row.khachHang}' không?`)) {
      try {
        const res = await khachHangService.deleteSubtabItem('giaHanThe', row.id);
        if (res?.success) {
          showNotification && showNotification(res.message || 'Đã xóa phiếu gia hạn thành công!');
          fetchRenewalList();
        } else {
          showNotification && showNotification(`Lỗi: ${res?.message || 'Không thể xóa phiếu'}`);
        }
      } catch (err) {
        console.error('Lỗi xóa phiếu:', err);
        showNotification && showNotification('Lỗi kết nối khi xóa phiếu gia hạn!');
      }
    }
  };

  // Xóa bản ghi trong subtab bên dưới
  const handleDeleteSubtabItem = async (tabId, item) => {
    if (!item || !item.id) {
      showNotification && showNotification('Vui lòng chọn dòng cần xóa!');
      return;
    }
    if (window.confirm(`Bạn có chắc chắn muốn xóa bản ghi '${item.soPhiu || item.id}' trong tab này không?`)) {
      try {
        const res = await khachHangService.deleteSubtabItem(tabId, item.id);
        if (res?.success) {
          showNotification && showNotification('Đã xóa bản ghi thành công!');
          if (selectedRow?.khachHangId) {
            handleSelectRow(selectedRow);
          }
        } else {
          showNotification && showNotification(`Lỗi: ${res?.message || 'Không thể xóa'}`);
        }
      } catch (err) {
        console.error('Lỗi xóa bản ghi subtab:', err);
        showNotification && showNotification('Lỗi kết nối khi xóa bản ghi!');
      }
    }
  };

  // Xuất file Excel
  const handleExportExcel = () => {
    if (!renewalList || renewalList.length === 0) {
      showNotification && showNotification('Không có dữ liệu để xuất Excel!');
      return;
    }
    const headers = ['Số phiếu', 'Ngày', 'Khách hàng', 'Mã thẻ', 'Loại thẻ', 'Từ ngày', 'Đến ngày', 'Ngày tặng thêm', 'Số tiền', 'Tỉ lệ giảm', 'Tổng cộng', 'Thanh toán', 'Khuyến mãi'];
    const rows = renewalList.map(r => [
      r.soPhiu, r.ngay, r.khachHang, r.maKhach, r.loaiThe, r.tuNgay, r.denNgay, r.ngayTangThem, r.soTien, r.tiLeGiamGia, r.tongCong, r.thanhToan, r.khuyenMai
    ]);
    const csvContent = 'data:text/csv;charset=utf-8,\uFEFF' + [headers.join(','), ...rows.map(e => e.join(','))].join('\n');
    const encodedUri = encodeURI(csvContent);
    const link = document.createElement('a');
    link.setAttribute('href', encodedUri);
    link.setAttribute('download', `GiaHanThe_${fromDate}_${toDate}.csv`);
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    showNotification && showNotification('Đã xuất danh sách gia hạn thẻ ra file CSV/Excel!');
  };

  // Kéo thanh co dãn ngang (giữa lưới trên và tab dưới)
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

  // Tổng cộng các dòng
  const totalTongCong = renewalList.reduce((acc, cur) => acc + (cur.tongCong || 0), 0);
  const totalThanhToan = renewalList.reduce((acc, cur) => acc + (cur.thanhToan || 0), 0);

  const renewalSelectedCustomer = selectedRow ? {
    id: selectedRow.khachHangId,
    tenKhachHang: selectedRow.khachHang,
    maThe: selectedRow.maKhach,
    loaiThe: selectedRow.loaiThe,
    dloaiTheId: selectedRow.loaiTheId,
    tuNgay: selectedRow.tuNgay,
    denNgay: selectedRow.denNgay,
    soLan: selectedRow.soLan,
    caTap: selectedRow.caTap || 'Cả ngày',
    tGiaHanTheId: selectedRow.id
  } : null;

  const modalPrefillCustomer = modalState.mode === 'create' && modalState.initialData?.khachHangId ? {
    id: modalState.initialData.khachHangId,
    tenKhachHang: modalState.initialData.tenKhach,
    maThe: modalState.initialData.maKhach,
    dienThoai: modalState.initialData.dienThoai,
    diaChi: modalState.initialData.diaChi,
    email: modalState.initialData.email,
    loaiThe: modalState.initialData.loaiThe,
    dloaiTheId: modalState.initialData.dloaiTheId,
    dcatapId: modalState.initialData.dcatapId,
    tuNgay: modalState.initialData.tuNgay,
    denNgay: modalState.initialData.denNgay,
    soLan: modalState.initialData.soLan,
    caTap: modalState.initialData.caTap
  } : null;

  const modalCustomer = modalPrefillCustomer || renewalSelectedCustomer;

  return (
    <div className="cust-mgmt-container card-renewal-view" style={{ height: 'calc(100vh - 64px)' }}>
      {/* MAIN SPLIT BODY */}
      <div className="cust-split-body" style={{ flex: 1, display: 'flex', overflow: 'hidden' }}>
        {/* =================================================================== */}
        {/* CỘT BÊN TRÁI: BỘ LỌC NGÀY, LOẠI GIAO DỊCH, CÂY LOẠI THẺ            */}
        {/* =================================================================== */}
        <div
          className="cust-left-pane"
          style={{
            width: leftWidth,
            minWidth: 160,
            maxWidth: 360,
            background: '#ebf2f8',
            borderRight: '1px solid #a3b8cc',
            display: 'flex',
            flexDirection: 'column',
            boxSizing: 'border-box'
          }}
        >
          {/* Header Cây: Loại thẻ + Icon bánh răng */}
          <div
            style={{
              height: 25,
              background: 'linear-gradient(180deg, #ffffff 0%, #d8e5f2 100%)',
              borderBottom: '1px solid #b2c9dd',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'space-between',
              padding: '0 8px',
              fontSize: 11.5,
              fontWeight: 700,
              color: '#1e3a8a'
            }}
          >
            <span>Loại thẻ</span>
            <span style={{ cursor: 'pointer', color: '#64748b' }} title="Cấu hình loại thẻ">⚙️</span>
          </div>

          {/* Bộ lọc Ngày: Từ ngày - Đến ngày */}
          <div style={{ padding: '6px 8px', borderBottom: '1px solid #c9d8e6', background: '#f5f8fb', fontSize: 11 }}>
            <div style={{ display: 'flex', alignItems: 'center', marginBottom: 4, gap: 4 }}>
              <span style={{ width: 32, color: '#334155' }}>Từ:</span>
              <input
                type="date"
                style={{ flex: 1, height: 21, fontSize: 11, padding: '1px 4px', border: '1px solid #8caec7', background: '#fff' }}
                value={fromDate}
                onChange={(e) => setFromDate(e.target.value)}
              />
            </div>
            <div style={{ display: 'flex', alignItems: 'center', gap: 4 }}>
              <span style={{ width: 32, color: '#334155' }}>Đến:</span>
              <input
                type="date"
                style={{ flex: 1, height: 21, fontSize: 11, padding: '1px 4px', border: '1px solid #8caec7', background: '#fff' }}
                value={toDate}
                onChange={(e) => setToDate(e.target.value)}
              />
            </div>
          </div>

          {/* Combobox Loại giao dịch */}
          <div style={{ padding: '6px 8px', borderBottom: '1px solid #c9d8e6', background: '#f5f8fb' }}>
            <div style={{ fontSize: 10.5, color: '#64748b', marginBottom: 2 }}>Loại giao dịch</div>
            <select
              style={{ width: '100%', height: 22, fontSize: 11, border: '1px solid #8caec7', padding: '0 4px', background: '#fff' }}
              value={loaiGiaoDich}
              onChange={(e) => setLoaiGiaoDich(e.target.value)}
            >
              <option value="all">Tất cả giao dịch</option>
              <option value="1">Gia hạn thẻ</option>
              <option value="2">Đổi loại thẻ</option>
              <option value="9">Bảo lưu thẻ</option>
              <option value="0">Đăng ký ban đầu</option>
            </select>
          </div>

          {/* Mini Toolbar: Thêm, Sửa, Nạp lại cây loại thẻ */}
          <div
            style={{
              height: 24,
              background: '#e2edf7',
              borderBottom: '1px solid #c9d8e6',
              display: 'flex',
              alignItems: 'center',
              padding: '0 6px',
              gap: 4
            }}
          >
            <button
              type="button"
              className="wf-tool-btn"
              title="Thêm loại thẻ mới"
              onClick={() => showNotification && showNotification('Thêm loại thẻ mới')}
              style={{ border: 'none', background: 'transparent', cursor: 'pointer', fontSize: 12, color: '#16a34a' }}
            >
              ➕
            </button>
            <button
              type="button"
              className="wf-tool-btn"
              title="Chỉnh sửa loại thẻ"
              onClick={() => showNotification && showNotification('Chỉnh sửa loại thẻ')}
              style={{ border: 'none', background: 'transparent', cursor: 'pointer', fontSize: 12, color: '#d97706' }}
            >
              ✏️
            </button>
            <button
              type="button"
              className="wf-tool-btn"
              title="Nạp lại danh mục"
              onClick={fetchRenewalList}
              style={{ border: 'none', background: 'transparent', cursor: 'pointer', fontSize: 12, color: '#0284c7' }}
            >
              🔄
            </button>
          </div>

          {/* CÂY LOẠI THẺ (TREEVIEW) */}
          <div style={{ flex: 1, overflowY: 'auto', background: '#ffffff', padding: '4px 0', fontSize: 11.5 }}>
            {/* Mục: Tất cả */}
            <div
              style={{
                display: 'flex',
                alignItems: 'center',
                gap: 6,
                padding: '3px 8px',
                cursor: 'pointer',
                background: selectedLoaiTheId === 'all' ? '#d9e9f6' : 'transparent',
                fontWeight: selectedLoaiTheId === 'all' ? 700 : 400
              }}
              onClick={() => setSelectedLoaiTheId('all')}
            >
              <span style={{ color: '#0284c7' }}>📁</span>
              <span>Tất cả</span>
            </div>

            {/* Mục: Chưa thiết lập */}
            <div
              style={{
                display: 'flex',
                alignItems: 'center',
                gap: 6,
                padding: '3px 8px 3px 20px',
                cursor: 'pointer',
                background: selectedLoaiTheId === 'chuaThietLap' ? '#d9e9f6' : 'transparent'
              }}
              onClick={() => setSelectedLoaiTheId('chuaThietLap')}
            >
              <span style={{ color: '#94a3b8' }}>📄</span>
              <span>Chưa thiết lập</span>
            </div>

            {/* Danh sách các loại thẻ từ CSDL */}
            {metadata.loaiThe?.map((lt) => (
              <div
                key={lt.id}
                style={{
                  display: 'flex',
                  alignItems: 'center',
                  gap: 6,
                  padding: '3px 8px 3px 20px',
                  cursor: 'pointer',
                  background: selectedLoaiTheId === lt.id ? '#d9e9f6' : 'transparent',
                  fontWeight: selectedLoaiTheId === lt.id ? 700 : 400
                }}
                onClick={() => setSelectedLoaiTheId(lt.id)}
              >
                <span style={{ color: '#16a34a', fontSize: 12 }}>💳</span>
                <span>{lt.name}</span>
              </div>
            ))}

            {/* Mục: Thùng rác */}
            <div
              style={{
                display: 'flex',
                alignItems: 'center',
                gap: 6,
                padding: '3px 8px',
                cursor: 'pointer',
                marginTop: 6,
                borderTop: '1px dashed #e2e8f0',
                background: selectedLoaiTheId === 'trash' ? '#fee2e2' : 'transparent',
                color: '#dc2626'
              }}
              onClick={() => setSelectedLoaiTheId('trash')}
            >
              <span>🗑️</span>
              <span>Thùng rác</span>
            </div>
          </div>
        </div>

        {/* =================================================================== */}
        {/* KHU VỰC BÊN PHẢI: LƯỚI GIA HẠN THẺ + SPLITTER + SUBTABS BÊN DƯỚI     */}
        {/* =================================================================== */}
        <div className="cust-right-pane card-renewal-right-pane" style={{ flex: 1, display: 'flex', flexDirection: 'column', overflow: 'hidden' }}>
          {/* 1. LƯỚI CHÍNH GIA HẠN THẺ (GRID TRÊN) */}
          <div style={{ flex: 1, display: 'flex', flexDirection: 'column', overflow: 'hidden', background: '#ffffff' }}>
            {/* Header dải tiêu đề + Toolbar chuẩn WinForms */}
            <div
              style={{
                height: 28,
                background: 'linear-gradient(180deg, #f2f7fc 0%, #d8e5f2 100%)',
                borderBottom: '1px solid #aec4d9',
                display: 'flex',
                alignItems: 'center',
                padding: '0 8px',
                gap: 6,
                fontSize: 11
              }}
            >
              <span style={{ fontWeight: 700, color: '#0f2942', marginRight: 4 }}>Quản lý thẻ</span>

              {/* Lọc F3 */}
              <div style={{ display: 'flex', alignItems: 'center', gap: 3 }}>
                <span style={{ color: '#334155' }}>Lọc (F3):</span>
                <input
                  type="text"
                  placeholder="Tìm kiếm phiếu, khách hàng..."
                  style={{
                    height: 20,
                    width: 140,
                    padding: '1px 5px',
                    fontSize: 11,
                    border: '1px solid #7192b8',
                    background: '#ffffd5'
                  }}
                  value={searchFilter}
                  onChange={(e) => setSearchFilter(e.target.value)}
                  onKeyDown={(e) => {
                    if (e.key === 'Enter') fetchRenewalList();
                  }}
                />
              </div>

              <div style={{ width: 1, height: 16, background: '#cbd5e1', margin: '0 2px' }} />

              {/* Nút: Thêm mới (Insert) */}
              <button
                type="button"
                className="wf-tool-btn"
                onClick={handleAddNewRenewal}
                style={{
                  background: 'transparent',
                  border: '1px solid transparent',
                  padding: '2px 6px',
                  cursor: 'pointer',
                  display: 'flex',
                  alignItems: 'center',
                  gap: 4,
                  fontSize: 11
                }}
              >
                <span style={{ color: '#16a34a', fontWeight: 700 }}>➕</span>
                <span>Thêm mới (Insert)</span>
              </button>

              {/* Nút: Chỉnh sửa (F4) */}
              <button
                type="button"
                className="wf-tool-btn"
                onClick={() => handleEditRenewal()}
                style={{
                  background: 'transparent',
                  border: '1px solid transparent',
                  padding: '2px 6px',
                  cursor: 'pointer',
                  display: 'flex',
                  alignItems: 'center',
                  gap: 4,
                  fontSize: 11
                }}
              >
                <span style={{ color: '#d97706' }}>✏️</span>
                <span>Chỉnh sửa (F4)</span>
              </button>

              {/* Nút: Xóa (Del) */}
              <button
                type="button"
                className="wf-tool-btn"
                onClick={() => handleDeleteRenewal()}
                style={{
                  background: 'transparent',
                  border: '1px solid transparent',
                  padding: '2px 6px',
                  cursor: 'pointer',
                  display: 'flex',
                  alignItems: 'center',
                  gap: 4,
                  fontSize: 11
                }}
              >
                <span style={{ color: '#dc2626', fontWeight: 700 }}>✕</span>
                <span>Xóa (Del)</span>
              </button>

              <div style={{ width: 1, height: 16, background: '#cbd5e1', margin: '0 2px' }} />

              {/* Nút: Xuất excel */}
              <button
                type="button"
                className="wf-tool-btn"
                onClick={handleExportExcel}
                style={{
                  background: 'transparent',
                  border: '1px solid transparent',
                  padding: '2px 6px',
                  cursor: 'pointer',
                  display: 'flex',
                  alignItems: 'center',
                  gap: 4,
                  fontSize: 11
                }}
              >
                <span>📊</span>
                <span>Xuất excel</span>
              </button>

              {/* Nút: In */}
              <button
                type="button"
                className="wf-tool-btn"
                onClick={() => window.print()}
                style={{
                  background: 'transparent',
                  border: '1px solid transparent',
                  padding: '2px 6px',
                  cursor: 'pointer',
                  display: 'flex',
                  alignItems: 'center',
                  gap: 4,
                  fontSize: 11
                }}
              >
                <span>🖨️</span>
                <span>In</span>
              </button>

              {/* Nút: Tổng */}
              <button
                type="button"
                className="wf-tool-btn"
                onClick={() => showNotification && showNotification(`Tổng cộng: ${totalTongCong.toLocaleString()} đ | Đã thanh toán: ${totalThanhToan.toLocaleString()} đ`)}
                style={{
                  background: 'transparent',
                  border: '1px solid transparent',
                  padding: '2px 6px',
                  cursor: 'pointer',
                  fontSize: 11
                }}
              >
                <span>Σ Tổng</span>
              </button>

              {/* Nút: P.tích */}
              <button
                type="button"
                className="wf-tool-btn"
                onClick={() => showNotification && showNotification('Phân tích giao dịch gia hạn thẻ')}
                style={{
                  background: 'transparent',
                  border: '1px solid transparent',
                  padding: '2px 6px',
                  cursor: 'pointer',
                  fontSize: 11
                }}
              >
                <span>📈 P.tích</span>
              </button>
            </div>

            {/* BẢNG LƯỚI CHÍNH WINFORMS GRID */}
            <div style={{ flex: 1, overflow: 'auto', background: '#ffffff' }}>
              <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: 11.5, fontFamily: 'Segoe UI, Tahoma, sans-serif' }}>
                <thead>
                  <tr style={{ background: '#dce8f5', borderBottom: '1px solid #9fb9d0', height: 26, textAlign: 'left', position: 'sticky', top: 0, zIndex: 2 }}>
                    <th style={{ width: 28, textAlign: 'center', borderRight: '1px solid #b8cde0' }}></th>
                    <th style={{ padding: '0 8px', borderRight: '1px solid #b8cde0', whiteSpace: 'nowrap' }}>Số phiếu</th>
                    <th style={{ padding: '0 8px', borderRight: '1px solid #b8cde0', whiteSpace: 'nowrap' }}>Ngày</th>
                    <th style={{ padding: '0 8px', borderRight: '1px solid #b8cde0', whiteSpace: 'nowrap' }}>Khách hàng</th>
                    <th style={{ padding: '0 8px', borderRight: '1px solid #b8cde0', whiteSpace: 'nowrap' }}>Loại thẻ</th>
                    <th style={{ padding: '0 8px', borderRight: '1px solid #b8cde0', whiteSpace: 'nowrap' }}>Từ ngày</th>
                    <th style={{ padding: '0 8px', borderRight: '1px solid #b8cde0', whiteSpace: 'nowrap' }}>Đến ngày</th>
                    <th style={{ padding: '0 8px', borderRight: '1px solid #b8cde0', textAlign: 'right', whiteSpace: 'nowrap' }}>Ngày tặng thêm</th>
                    <th style={{ padding: '0 8px', borderRight: '1px solid #b8cde0', textAlign: 'right', whiteSpace: 'nowrap' }}>Số tiền</th>
                    <th style={{ padding: '0 8px', borderRight: '1px solid #b8cde0', textAlign: 'right', whiteSpace: 'nowrap' }}>Tỉ lệ giảm giá</th>
                    <th style={{ padding: '0 8px', borderRight: '1px solid #b8cde0', textAlign: 'right', whiteSpace: 'nowrap' }}>Tổng cộng</th>
                    <th style={{ padding: '0 8px', borderRight: '1px solid #b8cde0', textAlign: 'right', whiteSpace: 'nowrap' }}>Thanh toán</th>
                    <th style={{ padding: '0 8px', whiteSpace: 'nowrap' }}>Khuyến mãi</th>
                  </tr>
                </thead>
                <tbody>
                  {loading ? (
                    <tr>
                      <td colSpan={13} style={{ textAlign: 'center', padding: 24, color: '#64748b' }}>
                        Đang tải danh sách giao dịch gia hạn thẻ...
                      </td>
                    </tr>
                  ) : renewalList.length === 0 ? (
                    <tr>
                      <td colSpan={13} style={{ textAlign: 'center', padding: 24, color: '#94a3b8' }}>
                        Không có giao dịch gia hạn thẻ nào phù hợp với bộ lọc.
                      </td>
                    </tr>
                  ) : (
                    renewalList.map((row, idx) => {
                      const isSelected = selectedRow?.id === row.id;
                      return (
                        <tr
                          key={row.id || idx}
                          onClick={() => handleSelectRow(row)}
                          onDoubleClick={() => handleEditRenewal(row)}
                          onContextMenu={(e) => handleMainContextMenu(e, row, 'soPhiu', row.soPhiu)}
                          style={{
                            height: 24,
                            background: isSelected ? '#d9e9f6' : (idx % 2 === 1 ? '#f8fafc' : '#ffffff'),
                            borderBottom: '1px solid #e2e8f0',
                            cursor: 'pointer'
                          }}
                        >
                          <td style={{ textAlign: 'center', borderRight: '1px solid #f1f5f9', color: '#0284c7' }}>
                            {isSelected ? '▶' : idx + 1}
                          </td>
                          <td style={{ padding: '0 8px', borderRight: '1px solid #f1f5f9', fontWeight: 600, color: '#1e3a8a' }}>
                            {row.soPhiu}
                          </td>
                          <td style={{ padding: '0 8px', borderRight: '1px solid #f1f5f9' }}>
                            {row.ngay}
                          </td>
                          <td style={{ padding: '0 8px', borderRight: '1px solid #f1f5f9', fontWeight: 600 }}>
                            {row.khachHang}
                          </td>
                          <td style={{ padding: '0 8px', borderRight: '1px solid #f1f5f9' }}>
                            <span style={{ display: 'inline-flex', alignItems: 'center', gap: 4 }}>
                              <span style={{ color: '#16a34a' }}>💳</span>
                              {row.loaiThe}
                            </span>
                          </td>
                          <td style={{ padding: '0 8px', borderRight: '1px solid #f1f5f9' }}>
                            {row.tuNgay}
                          </td>
                          <td style={{ padding: '0 8px', borderRight: '1px solid #f1f5f9', fontWeight: 600 }}>
                            {row.denNgay}
                          </td>
                          <td style={{ padding: '0 8px', borderRight: '1px solid #f1f5f9', textAlign: 'right' }}>
                            {row.ngayTangThem ?? 0}
                          </td>
                          <td style={{ padding: '0 8px', borderRight: '1px solid #f1f5f9', textAlign: 'right' }}>
                            {(row.soTien || 0).toLocaleString()}
                          </td>
                          <td style={{ padding: '0 8px', borderRight: '1px solid #f1f5f9', textAlign: 'right' }}>
                            {row.tiLeGiamGia ?? 0}
                          </td>
                          <td style={{ padding: '0 8px', borderRight: '1px solid #f1f5f9', textAlign: 'right', fontWeight: 700, color: '#0f2942' }}>
                            {(row.tongCong || 0).toLocaleString()}
                          </td>
                          <td style={{ padding: '0 8px', borderRight: '1px solid #f1f5f9', textAlign: 'right', fontWeight: 700, color: '#16a34a' }}>
                            {(row.thanhToan || 0).toLocaleString()}
                          </td>
                          <td style={{ padding: '0 8px' }}>
                            {row.khuyenMai || ''}
                          </td>
                        </tr>
                      );
                    })
                  )}
                </tbody>
              </table>
            </div>
          </div>

          {/* 2. SPLITTER NGANG KÉO CO DÃN */}
          <div
            onPointerDown={handleStartDragH}
            style={{
              height: 5,
              background: '#cbdbe8',
              borderTop: '1px solid #9fb9d0',
              borderBottom: '1px solid #9fb9d0',
              cursor: 'row-resize',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              userSelect: 'none'
            }}
          >
            <div style={{ width: 40, height: 2, background: '#64748b', borderRadius: 1 }} />
          </div>

          {/* 3. KHU VỰC CÁC SUBTAB PHÍA DƯỚI (Khớp 100% WinForms screenshot) */}
          <div
            style={{
              height: bottomHeight,
              background: '#ebf2f8',
              display: 'flex',
              flexDirection: 'column',
              boxSizing: 'border-box',
              overflow: 'hidden'
            }}
          >
            {/* DẢI TAB HEADER */}
            <div
              style={{
                height: 26,
                background: '#d8e5f2',
                borderBottom: '1px solid #a3b8cc',
                display: 'flex',
                alignItems: 'flex-end',
                padding: '0 4px',
                gap: 2,
                overflowX: 'auto'
              }}
            >
              {bottomTabs.map((tab) => {
                const isActive = activeBottomTab === tab.id;
                return (
                  <div
                    key={tab.id}
                    onClick={() => setActiveBottomTab(tab.id)}
                    style={{
                      padding: '3px 10px',
                      fontSize: 11,
                      fontWeight: isActive ? 700 : 500,
                      color: isActive ? '#0f2942' : '#334155',
                      background: isActive ? '#ffffff' : '#e2edf7',
                      border: '1px solid #9fb9d0',
                      borderBottom: isActive ? '1px solid #ffffff' : '1px solid #9fb9d0',
                      borderRadius: '3px 3px 0 0',
                      cursor: 'pointer',
                      whiteSpace: 'nowrap'
                    }}
                  >
                    {tab.label}
                  </div>
                );
              })}
            </div>

            {/* NỘI DUNG TỪNG SUBTAB */}
            <div style={{ flex: 1, background: '#ffffff', overflow: 'auto', position: 'relative' }}>
              {/* SUBTAB 1: THÔNG TIN (Khớp screenshot của người dùng) */}
              {activeBottomTab === 'thongTin' && (
                <div style={{ padding: '16px 20px', fontSize: 11.5, color: '#1e293b' }}>
                  <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '8px 24px', maxWidth: 650 }}>
                    <div>
                      <span style={{ color: '#475569', marginRight: 8 }}>Khởi tạo:</span>
                      <strong>{selectedRow?.timeCreated || '26/09/2026 13:46 PM'}</strong>
                    </div>
                    <div>
                      <span style={{ color: '#475569', marginRight: 8 }}>Khởi tạo bởi:</span>
                      <strong>{selectedRow?.userCreated || 'Administrator'}</strong>
                    </div>
                    <div>
                      <span style={{ color: '#475569', marginRight: 8 }}>Sửa đổi gần nhất:</span>
                      <strong>{selectedRow?.timeModified || ''}</strong>
                    </div>
                    <div>
                      <span style={{ color: '#475569', marginRight: 8 }}>Sửa đổi bởi:</span>
                      <strong>{selectedRow?.userModified || ''}</strong>
                    </div>
                  </div>
                </div>
              )}

              {/* CÁC SUBTAB KHÁC: HIỂN THỊ DỮ LIỆU BẢNG + MENU CHUỘT PHẢI ĐẦY ĐỦ */}
              {activeBottomTab !== 'thongTin' && (
                <div
                  style={{ height: '100%', minHeight: 120 }}
                  onContextMenu={(e) => handleSubtabContextMenu(e, activeBottomTab, null, '', '')}
                >
                  {loadingSubtabs ? (
                    <div style={{ padding: 20, textAlign: 'center', color: '#64748b', fontSize: 11 }}>
                      Đang tải chi tiết...
                    </div>
                  ) : (
                    <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: 11, fontFamily: 'Segoe UI, Tahoma, sans-serif' }}>
                      <thead>
                        <tr style={{ background: '#f1f5f9', borderBottom: '1px solid #cbd5e1', height: 23, textAlign: 'left', position: 'sticky', top: 0 }}>
                          <th style={{ width: 28, textAlign: 'center', borderRight: '1px solid #e2e8f0' }}>STT</th>
                          <th style={{ padding: '0 6px', borderRight: '1px solid #e2e8f0' }}>Số phiếu</th>
                          <th style={{ padding: '0 6px', borderRight: '1px solid #e2e8f0' }}>Ngày</th>
                          <th style={{ padding: '0 6px', borderRight: '1px solid #e2e8f0' }}>Diễn giải / Chi tiết</th>
                          <th style={{ padding: '0 6px', borderRight: '1px solid #e2e8f0', textAlign: 'right' }}>Số tiền / Điểm</th>
                          <th style={{ padding: '0 6px' }}>Ghi chú</th>
                        </tr>
                      </thead>
                      <tbody>
                        {(!subtabsData || !subtabsData[activeBottomTab] || subtabsData[activeBottomTab].length === 0) ? (
                          <tr onContextMenu={(e) => handleSubtabContextMenu(e, activeBottomTab, null, '', '')}>
                            <td colSpan={6} style={{ textAlign: 'center', padding: 24, color: '#64748b', background: '#fafbfc' }}>
                              Chưa có dữ liệu nào trong tab này. (Nhấp chuột phải để Thêm mới)
                            </td>
                          </tr>
                        ) : (
                          subtabsData[activeBottomTab].map((item, idx) => (
                            <tr
                              key={item.id || idx}
                              onContextMenu={(e) => handleSubtabContextMenu(e, activeBottomTab, item, 'soPhiu', item.soPhiu)}
                              onDoubleClick={() => {
                                setModalState({
                                  show: true,
                                  mode: 'edit',
                                  tabId: activeBottomTab,
                                  tabLabel: bottomTabs.find(t => t.id === activeBottomTab)?.label || 'Bản ghi',
                                  initialData: item
                                });
                              }}
                              style={{
                                height: 22,
                                borderBottom: '1px solid #f1f5f9',
                                background: idx % 2 === 1 ? '#f8fafc' : '#ffffff',
                                cursor: 'pointer'
                              }}
                            >
                              <td style={{ textAlign: 'center', borderRight: '1px solid #f1f5f9', color: '#64748b' }}>{idx + 1}</td>
                              <td style={{ padding: '0 6px', borderRight: '1px solid #f1f5f9', fontWeight: 600, color: '#1e3a8a' }}>{item.soPhiu || item.maThe || '---'}</td>
                              <td style={{ padding: '0 6px', borderRight: '1px solid #f1f5f9' }}>{item.ngay || ''}</td>
                              <td style={{ padding: '0 6px', borderRight: '1px solid #f1f5f9' }}>
                                {item.loaiThe || item.dienGiai || item.lyDo || item.may || 'Chi tiết giao dịch'}
                              </td>
                              <td style={{ padding: '0 6px', borderRight: '1px solid #f1f5f9', textAlign: 'right', fontWeight: 600 }}>
                                {(item.soTien || item.tongCong || item.thu || item.chi || item.diemTang || 0).toLocaleString()}
                              </td>
                              <td style={{ padding: '0 6px', color: '#475569' }}>{item.note || ''}</td>
                            </tr>
                          ))
                        )}
                      </tbody>
                    </table>
                  )}
                </div>
              )}
            </div>
          </div>
        </div>
      </div>

      {/* =================================================================== */}
      {/* 4. CONTEXT MENU CHUỘT PHẢI LƯỚI CHÍNH GIA HẠN THẺ (WINFORMS STYLE)  */}
      {/* =================================================================== */}
      {mainContextMenu.visible && (
        <div
          className="wf-context-menu"
          style={{
            position: 'fixed',
            left: mainContextMenu.x,
            top: mainContextMenu.y,
            zIndex: 2000,
            background: '#f0f0f0',
            border: '1px solid #999',
            boxShadow: '2px 2px 5px rgba(0,0,0,0.3)',
            padding: 2,
            minWidth: 180,
            fontSize: 11,
            fontFamily: 'Segoe UI, Tahoma, sans-serif'
          }}
        >
          {/* 1. Thêm Gia hạn thẻ */}
          <div className="wf-menu-item" onClick={() => { setMainContextMenu(p => ({ ...p, visible: false })); handleAddNewRenewal(); }}>
            <span style={{ color: '#16a34a', fontWeight: 'bold' }}>✚</span>
            <span style={{ fontWeight: 600 }}>Thêm Gia hạn thẻ</span>
          </div>

          {/* 2. Thêm nhanh (excel) */}
          <div className="wf-menu-item" onClick={() => { setMainContextMenu(p => ({ ...p, visible: false })); showNotification && showNotification('Chức năng nhập nhanh Excel gia hạn thẻ đang cập nhật'); }}>
            <span></span>
            <span>Thêm nhanh (excel)</span>
          </div>

          {/* 3. Cập nhật nhanh (excel) */}
          <div className="wf-menu-item" onClick={() => { setMainContextMenu(p => ({ ...p, visible: false })); showNotification && showNotification('Chức năng cập nhật nhanh Excel gia hạn thẻ đang cập nhật'); }}>
            <span></span>
            <span>Cập nhật nhanh (excel)</span>
          </div>

          {/* 4. Chỉnh sửa */}
          <div className="wf-menu-item" onClick={() => { setMainContextMenu(p => ({ ...p, visible: false })); handleEditRenewal(); }}>
            <span style={{ color: '#d97706' }}>✏️</span>
            <span>Chỉnh sửa (F4)</span>
          </div>

          <div className="wf-menu-divider" />

          {/* 5. Sắp xếp theo ▶ */}
          <div
            className="wf-menu-item"
            onMouseEnter={() => setShowSortSubmenu(true)}
            onMouseLeave={() => setShowSortSubmenu(false)}
            style={{ position: 'relative' }}
          >
            <span>↕️</span>
            <span>Sắp xếp theo ▶</span>
            {showSortSubmenu && (
              <div
                className="wf-submenu"
                style={{
                  position: 'absolute',
                  left: '100%',
                  top: 0,
                  background: '#f0f0f0',
                  border: '1px solid #999',
                  boxShadow: '2px 2px 5px rgba(0,0,0,0.3)',
                  padding: 2,
                  minWidth: 140
                }}
              >
                <div
                  className="wf-menu-item"
                  onClick={() => {
                    setRenewalList(prev => [...prev].sort((a, b) => String(a.soPhiu).localeCompare(String(b.soPhiu))));
                    setMainContextMenu(p => ({ ...p, visible: false }));
                  }}
                >
                  <span style={{ fontSize: 10 }}>▲</span> Sắp xếp tăng dần
                </div>
                <div
                  className="wf-menu-item"
                  onClick={() => {
                    setRenewalList(prev => [...prev].sort((a, b) => String(b.soPhiu).localeCompare(String(a.soPhiu))));
                    setMainContextMenu(p => ({ ...p, visible: false }));
                  }}
                >
                  <span style={{ fontSize: 10 }}>▼</span> Sắp xếp giảm dần
                </div>
              </div>
            )}
          </div>

          {/* 6. Refresh */}
          <div className="wf-menu-item" onClick={() => { setMainContextMenu(p => ({ ...p, visible: false })); fetchRenewalList(); showNotification && showNotification('Đã làm mới danh sách gia hạn'); }}>
            <span style={{ color: '#16a34a' }}>🔄</span>
            <span>Refresh</span>
          </div>

          {/* 7. In danh sách */}
          <div className="wf-menu-item" onClick={() => { setMainContextMenu(p => ({ ...p, visible: false })); window.print(); }}>
            <span>🖨️</span>
            <span>In danh sách</span>
          </div>

          <div className="wf-menu-divider" />

          {/* 8. Sao chép ô */}
          <div className="wf-menu-item" onClick={() => {
            setMainContextMenu(p => ({ ...p, visible: false }));
            if (mainContextMenu.cellValue) {
              navigator.clipboard.writeText(mainContextMenu.cellValue);
              showNotification && showNotification('Đã sao chép nội dung ô!');
            }
          }}>
            <span style={{ color: '#0284c7' }}>📄</span>
            <span>Sao chép ô</span>
          </div>

          {/* 9. Sao chép dòng */}
          <div className="wf-menu-item" onClick={() => {
            setMainContextMenu(p => ({ ...p, visible: false }));
            if (mainContextMenu.row) {
              const r = mainContextMenu.row;
              const text = `${r.soPhiu}\t${r.ngay}\t${r.khachHang}\t${r.loaiThe}\t${r.tongCong}`;
              navigator.clipboard.writeText(text);
              showNotification && showNotification('Đã sao chép dòng vào bộ nhớ tạm!');
            }
          }}>
            <span style={{ color: '#0284c7' }}>📑</span>
            <span>Sao chép dòng</span>
          </div>

          {/* 10. Xóa */}
          <div className="wf-menu-item" onClick={() => { setMainContextMenu(p => ({ ...p, visible: false })); handleDeleteRenewal(); }}>
            <span style={{ color: '#dc2626' }}>❌</span>
            <span>Xóa (Del)</span>
          </div>

          <div className="wf-menu-divider" />

          {/* 11. Tự động dãn cột */}
          <div className="wf-menu-item" onClick={() => { setMainContextMenu(p => ({ ...p, visible: false })); showNotification && showNotification('Đã tự động dãn cột'); }}>
            <span></span>
            <span>Tự động dãn cột</span>
          </div>

          {/* 12. Cột hiển thị */}
          <div className="wf-menu-item" onClick={() => { setMainContextMenu(p => ({ ...p, visible: false })); showNotification && showNotification('Cột hiển thị mặc định theo hệ thống WinForms'); }}>
            <span></span>
            <span>Cột hiển thị</span>
          </div>

          {/* 13. Thuộc tính */}
          <div className="wf-menu-item" onClick={() => { setMainContextMenu(p => ({ ...p, visible: false })); showNotification && showNotification('Thuộc tính bảng: Quản lý gia hạn thẻ'); }}>
            <span></span>
            <span>Thuộc tính</span>
          </div>
        </div>
      )}

      {/* =================================================================== */}
      {/* 5. CONTEXT MENU CHUỘT PHẢI CÁC SUBTAB BÊN DƯỚI (WINFORMS STYLE)     */}
      {/* =================================================================== */}
      {subtabContextMenu.visible && (
        <div
          className="wf-context-menu"
          style={{
            position: 'fixed',
            left: subtabContextMenu.x,
            top: subtabContextMenu.y,
            zIndex: 2000,
            background: '#f0f0f0',
            border: '1px solid #999',
            boxShadow: '2px 2px 5px rgba(0,0,0,0.3)',
            padding: 2,
            minWidth: 180,
            fontSize: 11,
            fontFamily: 'Segoe UI, Tahoma, sans-serif'
          }}
        >
          {/* 1. Thêm [Tên tab] (e.g. Thêm Gia hạn thẻ, Thêm Đổi loại thẻ, Thêm Phiếu thu...) */}
          <div
            className="wf-menu-item"
            onClick={() => {
              setSubtabContextMenu(p => ({ ...p, visible: false }));
              setModalState({
                show: true,
                mode: 'create',
                tabId: subtabContextMenu.tabId,
                tabLabel: subtabContextMenu.tabLabel,
                initialData: selectedRow ? {
                  khachHangId: selectedRow.khachHangId,
                  tenKhach: selectedRow.khachHang,
                  maKhach: selectedRow.maKhach
                } : null
              });
            }}
          >
            <span style={{ color: '#16a34a', fontWeight: 'bold' }}>✚</span>
            <span style={{ fontWeight: 600 }}>Thêm {subtabContextMenu.tabLabel}</span>
          </div>

          {/* 2. Thêm nhanh (excel) */}
          <div
            className="wf-menu-item"
            onClick={() => {
              setSubtabContextMenu(p => ({ ...p, visible: false }));
              showNotification && showNotification('Chức năng nhập nhanh Excel cho tab này đang cập nhật');
            }}
          >
            <span></span>
            <span>Thêm nhanh (excel)</span>
          </div>

          {/* 3. Cập nhật nhanh (excel) */}
          <div
            className="wf-menu-item"
            onClick={() => {
              setSubtabContextMenu(p => ({ ...p, visible: false }));
              showNotification && showNotification('Chức năng cập nhật nhanh Excel đang cập nhật');
            }}
          >
            <span></span>
            <span>Cập nhật nhanh (excel)</span>
          </div>

          {/* 4. Chỉnh sửa */}
          <div
            className="wf-menu-item"
            onClick={() => {
              setSubtabContextMenu(p => ({ ...p, visible: false }));
              if (!subtabContextMenu.item) {
                showNotification && showNotification('Vui lòng chọn bản ghi cần chỉnh sửa!');
                return;
              }
              setModalState({
                show: true,
                mode: 'edit',
                tabId: subtabContextMenu.tabId,
                tabLabel: subtabContextMenu.tabLabel,
                initialData: subtabContextMenu.item
              });
            }}
          >
            <span style={{ color: '#d97706' }}>✏️</span>
            <span>Chỉnh sửa (F4)</span>
          </div>

          <div className="wf-menu-divider" />

          {/* 5. Sắp xếp theo ▶ */}
          <div
            className="wf-menu-item"
            onMouseEnter={() => setShowSubtabSortSubmenu(true)}
            onMouseLeave={() => setShowSubtabSortSubmenu(false)}
            style={{ position: 'relative' }}
          >
            <span>↕️</span>
            <span>Sắp xếp theo ▶</span>
            {showSubtabSortSubmenu && (
              <div
                className="wf-submenu"
                style={{
                  position: 'absolute',
                  left: '100%',
                  top: 0,
                  background: '#f0f0f0',
                  border: '1px solid #999',
                  boxShadow: '2px 2px 5px rgba(0,0,0,0.3)',
                  padding: 2,
                  minWidth: 140
                }}
              >
                <div
                  className="wf-menu-item"
                  onClick={() => {
                    const tab = subtabContextMenu.tabId;
                    setSubtabsData(prev => {
                      if (!prev || !prev[tab]) return prev;
                      return {
                        ...prev,
                        [tab]: [...prev[tab]].sort((a, b) => String(a.soPhiu || a.ngay).localeCompare(String(b.soPhiu || b.ngay)))
                      };
                    });
                    setSubtabContextMenu(p => ({ ...p, visible: false }));
                  }}
                >
                  <span style={{ fontSize: 10 }}>▲</span> Sắp xếp tăng dần
                </div>
                <div
                  className="wf-menu-item"
                  onClick={() => {
                    const tab = subtabContextMenu.tabId;
                    setSubtabsData(prev => {
                      if (!prev || !prev[tab]) return prev;
                      return {
                        ...prev,
                        [tab]: [...prev[tab]].sort((a, b) => String(b.soPhiu || b.ngay).localeCompare(String(a.soPhiu || a.ngay)))
                      };
                    });
                    setSubtabContextMenu(p => ({ ...p, visible: false }));
                  }}
                >
                  <span style={{ fontSize: 10 }}>▼</span> Sắp xếp giảm dần
                </div>
              </div>
            )}
          </div>

          {/* 6. Refresh */}
          <div
            className="wf-menu-item"
            onClick={() => {
              setSubtabContextMenu(p => ({ ...p, visible: false }));
              if (selectedRow?.khachHangId) {
                handleSelectRow(selectedRow);
                showNotification && showNotification('Đã làm mới dữ liệu subtab');
              }
            }}
          >
            <span style={{ color: '#16a34a' }}>🔄</span>
            <span>Refresh</span>
          </div>

          {/* 7. In danh sách */}
          <div
            className="wf-menu-item"
            onClick={() => {
              setSubtabContextMenu(p => ({ ...p, visible: false }));
              window.print();
            }}
          >
            <span>🖨️</span>
            <span>In danh sách</span>
          </div>

          <div className="wf-menu-divider" />

          {/* 8. Sao chép ô */}
          <div
            className="wf-menu-item"
            onClick={() => {
              setSubtabContextMenu(p => ({ ...p, visible: false }));
              if (subtabContextMenu.cellValue) {
                navigator.clipboard.writeText(subtabContextMenu.cellValue);
                showNotification && showNotification('Đã sao chép nội dung ô!');
              }
            }}
          >
            <span style={{ color: '#0284c7' }}>📄</span>
            <span>Sao chép ô</span>
          </div>

          {/* 9. Sao chép dòng / vùng chọn */}
          <div
            className="wf-menu-item"
            onClick={() => {
              setSubtabContextMenu(p => ({ ...p, visible: false }));
              if (subtabContextMenu.item) {
                const item = subtabContextMenu.item;
                const text = `${item.soPhiu || ''}\t${item.ngay || ''}\t${item.soTien || item.tongCong || item.thu || item.chi || item.diemTang || ''}\t${item.note || ''}`;
                navigator.clipboard.writeText(text);
                showNotification && showNotification('Đã sao chép dữ liệu dòng!');
              }
            }}
          >
            <span style={{ color: '#0284c7' }}>📑</span>
            <span>Sao chép vùng chọn</span>
          </div>

          {/* 10. Xóa */}
          <div
            className="wf-menu-item"
            onClick={() => {
              setSubtabContextMenu(p => ({ ...p, visible: false }));
              handleDeleteSubtabItem(subtabContextMenu.tabId, subtabContextMenu.item);
            }}
          >
            <span style={{ color: '#dc2626' }}>❌</span>
            <span>Xóa (Del)</span>
          </div>

          <div className="wf-menu-divider" />

          {/* 11. Tự động dãn cột */}
          <div
            className="wf-menu-item"
            onClick={() => {
              setSubtabContextMenu(p => ({ ...p, visible: false }));
              showNotification && showNotification('Đã tự động dãn cột theo nội dung');
            }}
          >
            <span></span>
            <span>Tự động dãn cột</span>
          </div>

          {/* 12. Cột hiển thị */}
          <div
            className="wf-menu-item"
            onClick={() => {
              setSubtabContextMenu(p => ({ ...p, visible: false }));
              showNotification && showNotification('Cột hiển thị mặc định theo hệ thống WinForms');
            }}
          >
            <span></span>
            <span>Cột hiển thị</span>
          </div>

          {/* 13. Thuộc tính */}
          <div
            className="wf-menu-item"
            onClick={() => {
              setSubtabContextMenu(p => ({ ...p, visible: false }));
              showNotification && showNotification(`Thuộc tính bảng: ${subtabContextMenu.tabLabel}`);
            }}
          >
            <span></span>
            <span>Thuộc tính</span>
          </div>
        </div>
      )}

      {/* =================================================================== */}
      {/* 6. MODAL SUBTAB AE FORM (THÊM / SỬA GIA HẠN THẺ & CÁC SUBTAB)       */}
      {/* =================================================================== */}
      {modalState.show && (
        <SubtabAeModal
          show={modalState.show}
          tabId={modalState.tabId}
          tabLabel={modalState.tabLabel}
          customer={modalCustomer}
          customers={customers}
          metadata={metadata}
          mode={modalState.mode}
          initialData={modalState.initialData}
          onClose={() => setModalState(p => ({ ...p, show: false }))}
          onSave={async (tabId, data, mode) => {
            if (mode === 'edit' && data.id) {
              await khachHangService.updateSubtabItem(tabId, data.id, data);
            } else {
              await khachHangService.createSubtabItem(tabId, data);
            }
            showNotification && showNotification('Lưu dữ liệu thành công!');
            fetchRenewalList();
            if (selectedRow?.khachHangId) {
              handleSelectRow(selectedRow);
            }
          }}
        />
      )}
    </div>
  );
}
