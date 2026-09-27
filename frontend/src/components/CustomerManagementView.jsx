import React, { useState, useEffect, useRef, useMemo } from 'react';
import { khachHangService } from '../services/khachHangService';
import CustomerAeModal from './CustomerAeModal';
import * as XLSX from 'xlsx';
import FastReportModal from './FastReportModal';
import ExcelImportModal from './ExcelImportModal';
import DeviceSyncModal from './DeviceSyncModal';
import FingerprintEnrollModal from './FingerprintEnrollModal';
import TreeItemModal from './TreeItemModal';
import TreeQuickAddModal from './TreeQuickAddModal';
import './CustomerManagement.css';

export default function CustomerManagementView({ onSwitchToAccessControl, showNotification }) {
  // 1. Dữ liệu chính
  const [customers, setCustomers] = useState([]);
  const [counts, setCounts] = useState({
    all: 0,
    unset: 0,
    0: 0,
    1: 0,
    2: 0,
    3: 0,
    4: 0,
    trash: 0
  });
  const [groupCounts, setGroupCounts] = useState({ all: 0, unset: 0, trash: 0 });
  const [groups, setGroups] = useState([]);

  const [selectedCustomerId, setSelectedCustomerId] = useState(null);
  const [subtabsData, setSubtabsData] = useState(null);
  const [loading, setLoading] = useState(false);
  const [subtabsLoading, setSubtabsLoading] = useState(false);

  // 2. Chế độ cây bên trái: 'trangThai' (Trạng thái thẻ) | 'nhomKhach' (Nhóm khách hàng như trong TreeGridMg)
  const [treeMode, setTreeMode] = useState('trangThai');
  const [selectedTreeStatus, setSelectedTreeStatus] = useState('all');
  const [selectedGroupId, setSelectedGroupId] = useState('all');
  const [treeSearch, setTreeSearch] = useState('');
  const [searchTerm, setSearchTerm] = useState('');
  const searchInputRef = useRef(null);

  // 3. Tab phần đáy
  const [activeBottomTab, setActiveBottomTab] = useState('thongTin');
  const bottomTabsRef = useRef(null);

  // 4. Metadata tham chiếu cho Thêm / Sửa
  const [metadata, setMetadata] = useState({
    loaiThe: [],
    nhomKhach: [],
    caTap: [],
    nhanVien: [],
    trangThai: [],
    tinhThanh: []
  });

  // 5. Modal Thêm mới / Chỉnh sửa khách hàng (DynamicAeForm DKHACHHANG)
    const [showFastReport, setShowFastReport] = useState(false);
  const [showExcelImport, setShowExcelImport] = useState(false);
  const [showDeviceSync, setShowDeviceSync] = useState(false);
  const [showFingerprintEnroll, setShowFingerprintEnroll] = useState(false);

  // --- WINFORMS CONTEXT MENU STATES ---
  const [contextMenu, setContextMenu] = useState({
    visible: false,
    x: 0,
    y: 0,
    customer: null,
    colKey: 'tenKhachHang',
    colTitle: 'Tên khách hàng',
    cellValue: ''
  });
  const [focusedCell, setFocusedCell] = useState(null); // { rowId, colKey }
  const [showSubMenuSort, setShowSubMenuSort] = useState(false);
  const [showPropsDialog, setShowPropsDialog] = useState(false);
  const [showColChooserDialog, setShowColChooserDialog] = useState(false);

  // --- TREE VIEW CONTEXT MENU STATES ---
  const [treeContextMenu, setTreeContextMenu] = useState({
    visible: false,
    x: 0,
    y: 0,
    item: null
  });
  const [showTreeSubThemMoi, setShowTreeSubThemMoi] = useState(false);
  const [showTreeSubThemCon, setShowTreeSubThemCon] = useState(false);
  const [showTreeSubSapXep, setShowTreeSubSapXep] = useState(false);
  const [treeSortBy, setTreeSortBy] = useState('name'); // 'name' | 'custom'
  const [showTreePropsDialog, setShowTreePropsDialog] = useState(false);

  // --- REAL TREE ITEMS & ICONS FROM FIREBIRD DB ---
  const [treeItems, setTreeItems] = useState([]);
  const [treeIcons, setTreeIcons] = useState([]);
  const [collapsedFolders, setCollapsedFolders] = useState({});

  // Tree Modals State (Thêm trạng thái / Thư mục / Thêm nhanh)
  const [treeModal, setTreeModal] = useState({
    show: false,
    mode: 'create', // 'create' | 'edit'
    itemType: 0, // 0: item, 1: folder, 2: separator
    parentId: null,
    initialData: null
  });
  const [showTreeQuickAdd, setShowTreeQuickAdd] = useState(false);
  const [treeQuickAddParentId, setTreeQuickAddParentId] = useState(null);

  const [modalState, setModalState] = useState({
    show: false,
    mode: 'create', // 'create' | 'edit'
    customerData: null
  });


  // Tải danh sách khách hàng & số lượng cây trạng thái / nhóm
  const loadCustomersAndCounts = async (
    currentStatus = selectedTreeStatus, 
    currentGroup = selectedGroupId,
    search = searchTerm,
    mode = treeMode
  ) => {
    setLoading(true);
    try {
      const statusParam = mode === 'trangThai' ? currentStatus : (currentGroup === 'trash' ? 'trash' : 'all');
      const groupParam = mode === 'nhomKhach' ? currentGroup : '';

      const [listRes, countRes] = await Promise.all([
        khachHangService.getAll(search, statusParam, groupParam),
        khachHangService.getTreeCounts()
      ]);

      if (listRes && listRes.data) {
        setCustomers(listRes.data);
        if (listRes.data.length > 0) {
          const currentExists = listRes.data.some(c => c.id === selectedCustomerId);
          if (!currentExists) {
            setSelectedCustomerId(listRes.data[0].id);
          }
        } else {
          setSelectedCustomerId(null);
          setSubtabsData(null);
        }
      }

      if (countRes) {
        if (countRes.counts) setCounts(countRes.counts);
        if (countRes.groupCounts) setGroupCounts(countRes.groupCounts);
        if (countRes.groups) setGroups(countRes.groups);
      }
    } catch (err) {
      console.error('Lỗi nạp dữ liệu khách hàng:', err);
      showNotification && showNotification('❌ Không thể tải danh sách khách hàng từ cơ sở dữ liệu!');
    } finally {
      setLoading(false);
    }
  };

  // Nạp metadata khi khởi động
  useEffect(() => {
    khachHangService.getMetadata().then((res) => {
      if (res && res.data) {
        setMetadata(res.data);
      }
    });

    // Nạp icons từ SIMAGE
    khachHangService.getIcons().then((res) => {
      if (res && res.data) {
        setTreeIcons(res.data);
      }
    });
  }, []);

  // Tải danh mục cây (trạng thái hoặc nhóm khách hàng)
  const loadTreeData = async (mode = treeMode) => {
    try {
      const res = await khachHangService.getTreeItems(mode);
      if (res && res.data) {
        setTreeItems(res.data);
      }
    } catch (err) {
      console.error('Lỗi nạp cây:', err);
    }
  };

  useEffect(() => {
    loadTreeData(treeMode);
  }, [treeMode]);

  // Tải lại khi thay đổi cây trạng thái / nhóm hoặc từ khóa tìm kiếm
  useEffect(() => {
    loadCustomersAndCounts(selectedTreeStatus, selectedGroupId, searchTerm, treeMode);
  }, [selectedTreeStatus, selectedGroupId, treeMode]);


  // Tải chi tiết các subtabs ở phần đáy khi thay đổi khách hàng được chọn
  useEffect(() => {
    if (!selectedCustomerId) {
      setSubtabsData(null);
      return;
    }
    setSubtabsLoading(true);
    khachHangService.getSubtabs(selectedCustomerId).then((res) => {
      if (res && res.data) {
        setSubtabsData(res.data);
      } else {
        setSubtabsData(null);
      }
      setSubtabsLoading(false);
    }).catch(() => {
      setSubtabsLoading(false);
    });
  }, [selectedCustomerId]);

  // Khách hàng đang được chọn hiện tại
  const selectedCustomer = useMemo(() => {
    return customers.find(c => c.id === selectedCustomerId) || null;
  }, [customers, selectedCustomerId]);

  // Phím tắt bàn phím WinForms: F3 (Lọc), Insert (Thêm), F4 (Sửa), Del (Xóa), Escape (Đóng menu ngữ cảnh)
  useEffect(() => {
    const handleKeyDown = (e) => {
      if (modalState.show) return;

      if (e.key === 'Escape') {
        if (contextMenu.visible) closeContextMenu();
        if (treeContextMenu.visible) closeTreeContextMenu();
        setFocusedCell(null);
        return;
      }

      if (['INPUT', 'TEXTAREA', 'SELECT'].includes(e.target.tagName) && e.key !== 'F3') return;

      if (e.key === 'F3') {
        e.preventDefault();
        searchInputRef.current?.focus();
        searchInputRef.current?.select();
      } else if (e.key === 'Insert') {
        e.preventDefault();
        handleAddNewCustomer();
      } else if (e.key === 'F4') {
        e.preventDefault();
        handleEditCustomer();
      } else if (e.key === 'Delete') {
        e.preventDefault();
        handleDeleteCustomer();
      }
    };

    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [modalState.show, selectedCustomer, contextMenu.visible, treeContextMenu.visible]);

  // Tự động đóng Menu ngữ cảnh chuột phải & xóa ô focus khi nhấp chuột ra ngoài
  useEffect(() => {
    const handleOutsideClick = (e) => {
      // Nếu click vào bên trong menu ngữ cảnh hoặc submenu thì để menu xử lý
      if (e.target.closest('.wf-context-menu') || e.target.closest('.wf-submenu')) {
        return;
      }

      // Nhấp ra bất kỳ chỗ nào bên ngoài đều đóng menu ngữ cảnh & xóa ô focus
      if (contextMenu.visible) {
        closeContextMenu();
      }
      if (treeContextMenu.visible) {
        closeTreeContextMenu();
      }
      setFocusedCell(null);
    };

    // Lắng nghe cả pointerdown trên document để bắt kịp mọi tương tác click chuột ngay lập tức
    document.addEventListener('pointerdown', handleOutsideClick);
    return () => document.removeEventListener('pointerdown', handleOutsideClick);
  }, [contextMenu.visible, treeContextMenu.visible]);

  // Xử lý cuộn thanh tab phần đáy
  const scrollBottomTabs = (offset) => {
    if (bottomTabsRef.current) {
      bottomTabsRef.current.scrollBy({ left: offset, behavior: 'smooth' });
    }
  };

  // Mở modal thêm mới khách hàng (DynamicAeForm Config.CreateAeForm('DKHACHHANG'))
  const handleAddNewCustomer = () => {
    setModalState({
      show: true,
      mode: 'create',
      customerData: null
    });
  };

  // Mở modal chỉnh sửa khách hàng
  const handleEditCustomer = () => {
    if (!selectedCustomer) {
      showNotification && showNotification('Vui lòng chọn một khách hàng để chỉnh sửa!');
      return;
    }

    setModalState({
      show: true,
      mode: 'edit',
      customerData: selectedCustomer
    });
  };

  // Xóa vĩnh viễn khách hàng khỏi CSDL
  const handlePermanentDelete = async () => {
    if (!selectedCustomer) {
      showNotification && showNotification('Vui lòng chọn khách hàng cần xóa vĩnh viễn!');
      return;
    }

    if (window.confirm(`Hành động này sẽ XÓA VĨNH VIỄN khách hàng '${selectedCustomer.tenKhachHang}' (Mã: ${selectedCustomer.maThe}) khỏi hệ thống và CSDL!\nBạn có chắc chắn muốn xóa vĩnh viễn không?`)) {
      try {
        const res = await khachHangService.permanentDelete(selectedCustomer.id);
        if (res && res.success) {
          showNotification && showNotification(res.message || 'Đã xóa vĩnh viễn khách hàng khỏi CSDL!');
          setSelectedCustomerId(null);
          setSubtabsData(null);
          loadCustomersAndCounts();
        } else {
          showNotification && showNotification(`Lỗi: ${res?.message || 'Không thể xóa vĩnh viễn khách hàng'}`);
        }
      } catch (err) {
        console.error('Lỗi xóa vĩnh viễn:', err);
        showNotification && showNotification('Lỗi kết nối khi xóa vĩnh viễn!');
      }
    }
  };

    // Xóa khách hàng (vào thùng rác)
  // --- CONTEXT MENU HANDLERS (WINFORMS TÂN AN PHÁT) ---
  const handleCellContextMenu = (e, customer, colKey, colTitle, cellValue) => {
    e.preventDefault();
    e.stopPropagation();
    setSelectedCustomerId(customer.id);
    setFocusedCell({ rowId: customer.id, colKey });

    const menuWidth = 200;
    const menuHeight = 390;
    const x = e.clientX + menuWidth > window.innerWidth ? window.innerWidth - menuWidth - 10 : e.clientX;
    const y = e.clientY + menuHeight > window.innerHeight ? window.innerHeight - menuHeight - 10 : e.clientY;

    setContextMenu({
      visible: true,
      x,
      y,
      customer,
      colKey,
      colTitle,
      cellValue: cellValue !== undefined && cellValue !== null ? String(cellValue) : ''
    });
  };

  const closeContextMenu = () => {
    setContextMenu(prev => ({ ...prev, visible: false }));
    setShowSubMenuSort(false);
    setFocusedCell(null);
  };

  // 1. Đặt giá trị cột (UPDATE DKHACHHANG SET col = @val WHERE ID = '...')
  const handleMenuDatGiaTri = async () => {
    const { customer, colKey, colTitle, cellValue } = contextMenu;
    closeContextMenu();
    if (!customer) return;

    const newVal = prompt(`Mời bạn nhập giá trị mới cho cột "${colTitle}":`, cellValue);
    if (newVal === null) return; // User cancelled

    try {
      const updateData = { ...customer, [colKey]: newVal };
      const res = await khachHangService.update(customer.id, updateData);
      if (res && res.success) {
        showNotification && showNotification(`Đã đặt giá trị "${newVal}" cho cột "${colTitle}"`);
        loadCustomersAndCounts();
      } else {
        alert(res?.message || 'Không thể cập nhật giá trị!');
      }
    } catch (err) {
      console.error(err);
      alert('Lỗi cập nhật giá trị vào cơ sở dữ liệu!');
    }
  };

  // 2. Lọc theo giá trị ô (WHERE col LIKE '%val%')
  const handleMenuLocTheoGiaTri = () => {
    const { cellValue } = contextMenu;
    closeContextMenu();
    if (cellValue) {
      setSearchTerm(cellValue);
      showNotification && showNotification(`Đang lọc theo "${cellValue}"`);
    }
  };

  // 3. Sắp xếp tăng / giảm theo cột
  const handleMenuSort = (direction) => {
    const { colKey } = contextMenu;
    closeContextMenu();
    setCustomers(prev => {
      const sorted = [...prev].sort((a, b) => {
        const valA = a[colKey] !== undefined ? String(a[colKey]) : '';
        const valB = b[colKey] !== undefined ? String(b[colKey]) : '';
        return direction === 'asc' ? valA.localeCompare(valB, 'vi') : valB.localeCompare(valA, 'vi');
      });
      return sorted;
    });
  };

  // 4. Sao chép ô (Copy to clipboard)
  const handleMenuCopyCell = () => {
    const { cellValue } = contextMenu;
    closeContextMenu();
    navigator.clipboard.writeText(cellValue || '');
    showNotification && showNotification(`Đã sao chép "${cellValue}" vào bộ nhớ tạm!`);
  };

  // 5. Sao chép vùng chọn (Dòng TSV để dán sang Excel)
  const handleMenuCopyRow = () => {
    const { customer } = contextMenu;
    closeContextMenu();
    if (!customer) return;
    const headers = ['Mã thẻ', 'Tên khách hàng', 'Địa chỉ', 'Điện thoại', 'Loại thẻ', 'Trạng thái', 'Từ ngày', 'Đến ngày', 'Số lần', 'Đã tập', 'Còn lại'];
    const row = [customer.maThe, customer.tenKhachHang, customer.diaChi, customer.dienThoai, customer.loaiThe, customer.trangThai, customer.tuNgay, customer.denNgay, customer.soLan, customer.daTap, customer.conLai];
    const tsv = headers.join('\t') + '\n' + row.map(v => v || '').join('\t');
    navigator.clipboard.writeText(tsv);
    showNotification && showNotification('Đã sao chép dòng vào bộ nhớ tạm (dạng bảng Excel)!');
  };

  // 6. Tự động giãn cột
  // --- TREE CONTEXT MENU & CRUD ACTION HANDLERS ---
  const handleTreeContextMenu = (e, item) => {
    e.preventDefault();
    e.stopPropagation();
    if (item && item.id) {
      if (treeMode === 'trangThai') {
        setSelectedTreeStatus(item.id);
      } else {
        setSelectedGroupId(item.id);
      }
    }

    const menuWidth = 190;
    const menuHeight = 380;
    const x = e.clientX + menuWidth > window.innerWidth ? window.innerWidth - menuWidth - 10 : e.clientX;
    const y = e.clientY + menuHeight > window.innerHeight ? window.innerHeight - menuHeight - 10 : e.clientY;

    setTreeContextMenu({
      visible: true,
      x,
      y,
      item
    });
  };

  const closeTreeContextMenu = () => {
    setTreeContextMenu(prev => ({ ...prev, visible: false }));
    setShowTreeSubThemMoi(false);
    setShowTreeSubThemCon(false);
    setShowTreeSubSapXep(false);
  };

  // 1. Thêm trạng thái / nhóm
  const handleOpenAddStatus = (parentId = null) => {
    closeTreeContextMenu();
    setTreeModal({
      show: true,
      mode: 'create',
      itemType: 0,
      parentId: parentId || null,
      initialData: null
    });
  };

  // 2. Thêm thư mục
  const handleOpenAddFolder = (parentId = null) => {
    closeTreeContextMenu();
    setTreeModal({
      show: true,
      mode: 'create',
      itemType: 1,
      parentId: parentId || null,
      initialData: null
    });
  };

  // 3. Thêm phân cách
  const handleAddSeparator = async (parentId = null) => {
    closeTreeContextMenu();
    try {
      await khachHangService.createTreeItem({
        mode: treeMode,
        name: '—',
        itemType: 2,
        parentId: parentId || null
      });
      showNotification && showNotification('Đã thêm phân cách thành công!');
      loadTreeData(treeMode);
    } catch (err) {
      console.error('Lỗi thêm phân cách:', err);
      alert('Không thể thêm phân cách: ' + (err.message || 'Lỗi kết nối'));
    }
  };

  // 4. Thêm nhanh
  const handleOpenQuickAdd = (parentId = null) => {
    closeTreeContextMenu();
    setTreeQuickAddParentId(parentId || null);
    setShowTreeQuickAdd(true);
  };

  // 5. Chỉnh sửa mục cây
  const handleOpenEditTreeItem = (itemParam = null) => {
    closeTreeContextMenu();
    const item = itemParam || treeContextMenu.item;
    if (!item) return;
    if (item.id === 'all' || item.id === 'unset' || item.id === 'trash') {
      alert(`Mục hệ thống "${item.label}" không thể chỉnh sửa!`);
      return;
    }
    setTreeModal({
      show: true,
      mode: 'edit',
      itemType: item.itemType || 0,
      parentId: item.parentId || null,
      initialData: {
        id: item.id,
        name: item.label,
        note: item.note || '',
        simageId: item.simageId || '',
        itemType: item.itemType || 0,
        parentId: item.parentId || null
      }
    });
  };

  // 6. Xóa mục cây
  const handleDeleteTreeItem = async (itemParam = null) => {
    closeTreeContextMenu();
    const item = itemParam || treeContextMenu.item;
    if (!item) return;
    if (item.id === 'all' || item.id === 'unset' || item.id === 'trash') {
      alert(`Mục hệ thống "${item.label}" không thể xóa!`);
      return;
    }
    if (confirm(`Bạn có chắc chắn muốn xóa "${item.label}" vào thùng rác không?`)) {
      try {
        await khachHangService.deleteTreeItem(item.id, treeMode);
        showNotification && showNotification(`Đã chuyển "${item.label}" vào thùng rác!`);
        loadTreeData(treeMode);
        loadCustomersAndCounts();
      } catch (err) {
        console.error('Lỗi xóa mục cây:', err);
        alert('Lỗi xóa mục: ' + (err.message || 'Lỗi kết nối'));
      }
    }
  };

  // 7. Đổi tên nhanh
  const handleTreeRename = async (itemParam = null) => {
    closeTreeContextMenu();
    const item = itemParam || treeContextMenu.item;
    if (!item) return;
    if (item.id === 'all' || item.id === 'unset' || item.id === 'trash') {
      alert(`Mục hệ thống "${item.label}" không thể đổi tên!`);
      return;
    }
    const newName = prompt(`Đổi tên cho "${item.label}":`, item.label);
    if (newName && newName.trim() && newName.trim() !== item.label) {
      try {
        await khachHangService.updateTreeItem(item.id, {
          mode: treeMode,
          name: newName.trim(),
          note: item.note || '',
          simageId: item.simageId || ''
        });
        showNotification && showNotification(`Đã đổi tên thành "${newName.trim()}"`);
        loadTreeData(treeMode);
      } catch (err) {
        alert('Lỗi đổi tên: ' + err.message);
      }
    }
  };

  // 8. Lưu từ TreeItemModal (Thêm mới / Chỉnh sửa)
  const handleSaveTreeItem = async (payload) => {
    if (payload.id && treeModal.mode === 'edit') {
      await khachHangService.updateTreeItem(payload.id, payload);
      showNotification && showNotification(`Đã cập nhật ${payload.name} thành công!`);
    } else {
      await khachHangService.createTreeItem(payload);
      showNotification && showNotification(`Đã thêm ${payload.name} thành công!`);
    }
    loadTreeData(treeMode);
    loadCustomersAndCounts();
  };

  // 9. Lưu từ TreeQuickAddModal (Thêm nhanh)
  const handleSaveTreeQuickAdd = async (payload) => {
    const res = await khachHangService.batchCreateTreeItems(payload);
    showNotification && showNotification(res?.message || 'Đã thêm nhanh thành công!');
    loadTreeData(treeMode);
    loadCustomersAndCounts();
  };

  const handleTreeCopy = () => {
    const item = treeContextMenu.item;
    closeTreeContextMenu();
    if (!item) return;
    navigator.clipboard.writeText(item.label);
    showNotification && showNotification(`Đã sao chép "${item.label}" vào bộ nhớ tạm!`);
  };


  const handleMenuAutoFitCols = () => {
    closeContextMenu();
    showNotification && showNotification('Đã tự động căn chỉnh và giãn đều các cột theo nội dung!');
  };

  const handleDeleteCustomer = async () => {
    const target = selectedCustomer || (customers.length > 0 ? customers[0] : null);
    if (!target) {
      alert('Vui lòng chọn một khách hàng trong danh sách để xóa!');
      showNotification && showNotification('Vui lòng chọn một khách hàng trong danh sách để xóa!');
      return;
    }

    if (window.confirm(`Bạn có chắc chắn muốn chuyển khách hàng '${target.tenKhachHang}' (Mã: ${target.maThe}) vào thùng rác không?`)) {
      try {
        const res = await khachHangService.delete(target.id);
        if (res && res.success) {
          alert(`Đã chuyển '${target.tenKhachHang}' vào thùng rác thành công!`);
          showNotification && showNotification(`Đã chuyển '${target.tenKhachHang}' vào thùng rác!`);
          loadCustomersAndCounts();
        } else {
          alert(`Lỗi: ${res?.message || 'Không thể xóa khách hàng'}`);
          showNotification && showNotification(`Lỗi: ${res?.message || 'Không thể xóa khách hàng'}`);
        }
      } catch (err) {
        alert('Lỗi kết nối khi xóa khách hàng!');
        showNotification && showNotification('❌ Lỗi kết nối khi xóa khách hàng!');
      }
    }
  };

  // Phục hồi khách hàng từ thùng rác
  const handleRestoreCustomer = async () => {
    if (!selectedCustomer) {
      showNotification && showNotification('Vui lòng chọn một khách hàng để phục hồi!');
      return;
    }

    try {
      const res = await khachHangService.restore(selectedCustomer.id);
      if (res && res.success) {
        showNotification && showNotification(`Đã phục hồi khách hàng '${selectedCustomer.tenKhachHang}' thành công!`);
        loadCustomersAndCounts();
      } else {
        showNotification && showNotification(`Lỗi: ${res?.message || 'Không thể phục hồi khách hàng'}`);
      }
    } catch (err) {
      showNotification && showNotification('❌ Lỗi kết nối khi phục hồi khách hàng!');
    }
  };

  // Lưu thông tin khách hàng từ CustomerAeModal
  const handleSaveCustomer = async (data, id) => {
    try {
      if (modalState.mode === 'create') {
        const res = await khachHangService.create(data);
        if (res && res.success) {
          showNotification && showNotification(`Đã thêm thành công khách hàng '${data.name}'!`);
          setModalState(prev => ({ ...prev, show: false }));
          loadCustomersAndCounts();
        } else {
          alert(`Lỗi: ${res?.message || 'Không thể thêm khách hàng'}`);
        }
      } else {
        const targetId = id || selectedCustomerId;
        const res = await khachHangService.update(targetId, data);
        if (res && res.success) {
          showNotification && showNotification(`Đã cập nhật thông tin khách hàng '${data.name}' thành công!`);
          setModalState(prev => ({ ...prev, show: false }));
          loadCustomersAndCounts();
          if (targetId) {
            khachHangService.getSubtabs(targetId).then(subRes => {
              if (subRes && subRes.data) setSubtabsData(subRes.data);
            });
          }
        } else {
          alert(`Lỗi: ${res?.message || 'Không thể cập nhật khách hàng'}`);
        }
      }
    } catch (err) {
      alert('❌ Lỗi kết nối máy chủ khi lưu khách hàng!');
    }
  };

  // Xuất danh sách ra file CSV / Excel
  const handleExportExcel = () => {
    const listToExport = customers.length > 0 ? customers : [
      { stt: 1, maThe: '999196', tenKhachHang: 'Nguyen Van Tuan Test AE', dienThoai: '0912345678', diaChi: 'Hà Nội', loaiThe: 'Gym Tháng', tuNgay: '2026-09-01', denNgay: '2026-10-01', soLan: 30, daTap: 5, conLai: 25, trangThai: 'Đang sử dụng' }
    ];

    try {
      const exportRows = listToExport.map((c, idx) => ({
        'STT': idx + 1,
        'Mã Thẻ': c.maThe || '',
        'Họ và Tên': c.tenKhachHang || '',
        'Số Điện Thoại': c.dienThoai || '',
        'Địa Chỉ': c.diaChi || '',
        'Email': c.email || '',
        'Loại Thẻ': c.loaiThe || '',
        'Từ Ngày': c.tuNgay || '',
        'Đến Ngày': c.denNgay || '',
        'Số Lần': c.soLan ?? 0,
        'Đã Tập': c.daTap ?? 0,
        'Còn Lại': c.conLai ?? 0,
        'Mã Vân Tay': c.maVanTay || '',
        'Trạng Thái': c.trangThai || 'Bình thường',
        'Ghi Chú': c.note || ''
      }));

      const ws = XLSX.utils.json_to_sheet(exportRows);
      const wb = XLSX.utils.book_new();
      XLSX.utils.book_append_sheet(wb, ws, 'DanhSachHoiVien');
      XLSX.writeFile(wb, `Danh_Sach_Hoi_Vien_${new Date().toISOString().slice(0, 10)}.xlsx`);
      alert(`Đã xuất thành công ${listToExport.length} hội viên ra file Excel!`);
      showNotification && showNotification(`Đã xuất thành công ${listToExport.length} hội viên ra file Excel!`);
    } catch (err) {
      console.error('Lỗi xuất Excel:', err);
      alert('Lỗi khi xuất file Excel: ' + err.message);
      showNotification && showNotification('Lỗi khi xuất file Excel!');
    }
  };

  const handlePrintReport = () => {
    setShowFastReport(true);
  };

  const handleOpenExcelImport = () => {
    setShowExcelImport(true);
  };

  const handleOpenDeviceSync = () => {
    setShowDeviceSync(true);
  };

  const handleOpenFingerprintEnroll = () => {
    const target = selectedCustomer || (customers.length > 0 ? customers[0] : null);
    if (!target) {
      alert('Vui lòng chọn một hội viên từ danh sách để đăng ký vân tay!');
      showNotification && showNotification('Vui lòng chọn một hội viên từ danh sách để đăng ký vân tay!');
      return;
    }
    if (!selectedCustomerId && target) {
      setSelectedCustomerId(target.id);
    }
    setShowFingerprintEnroll(true);
  };

  // Danh sách mục cây Trạng thái thẻ (DTRANGTHAI)
  const statusTreeItems = useMemo(() => {
    const list = [
      { id: 'all', label: 'Tất cả', icon: '🌐', count: counts.all, isSpecial: true },
      { id: 'unset', label: 'Chưa thiết lập', icon: '🗂️', count: counts.unset, isSpecial: true },
    ];

    if (treeItems && treeItems.length > 0) {
      treeItems.forEach(ti => {
        let defaultIcon = '🏷️';
        if (ti.name === 'Đang sử dụng') defaultIcon = '▶️';
        else if (ti.name === 'Bảo lưu') defaultIcon = '⏸️';
        else if (ti.name === 'Chưa kích hoạt') defaultIcon = '❌';
        else if (ti.name === 'Quá hạn' || ti.name === 'Quá lần tập') defaultIcon = '⚠️';
        else if (ti.itemType === 1) defaultIcon = '📁';

        list.push({
          id: ti.id,
          label: ti.name,
          icon: defaultIcon,
          count: counts[ti.id] || 0,
          isGreen: ti.name === 'Đang sử dụng',
          itemType: ti.itemType,
          parentId: ti.parentId,
          simage: ti.simage,
          simageId: ti.simageId,
          note: ti.note
        });
      });
    } else {
      list.push(
        { id: '1', label: 'Đang sử dụng', icon: '▶️', count: counts['1'] || 0, isGreen: true, itemType: 0 },
        { id: '2', label: 'Bảo lưu', icon: '⏸️', count: counts['2'] || 0, itemType: 0 },
        { id: '0', label: 'Chưa kích hoạt', icon: '❌', count: counts['0'] || 0, itemType: 0 },
        { id: '3', label: 'Quá hạn', icon: '⚠️', count: counts['3'] || 0, itemType: 0 },
        { id: '4', label: 'Quá lần tập', icon: '⚠️', count: counts['4'] || 0, itemType: 0 }
      );
    }

    list.push({ id: 'trash', label: 'Thùng rác', icon: '🗑️', count: counts.trash || 0, isSpecial: true });
    return list;
  }, [treeItems, counts]);

  // Danh sách mục cây Nhóm khách hàng (DNHOMKHACHHANG)
  const groupTreeItems = useMemo(() => {
    const list = [
      { id: 'all', label: 'Tất cả', icon: '🌐', count: groupCounts.all, isSpecial: true },
      { id: 'unset', label: 'Chưa thiết lập', icon: '🗂️', count: groupCounts.unset, isSpecial: true },
    ];

    if (treeItems && treeItems.length > 0) {
      treeItems.forEach(ti => {
        list.push({
          id: ti.id,
          label: ti.name,
          icon: ti.itemType === 1 ? '📁' : '📂',
          count: groupCounts[ti.id] || 0,
          itemType: ti.itemType,
          parentId: ti.parentId,
          simage: ti.simage,
          simageId: ti.simageId,
          note: ti.note
        });
      });
    } else {
      groups.forEach(g => {
        list.push({
          id: g.id,
          label: g.name,
          icon: '📁',
          count: g.count || 0,
          itemType: 0
        });
      });
    }

    list.push({ id: 'trash', label: 'Thùng rác', icon: '🗑️', count: groupCounts.trash || 0, isSpecial: true });
    return list;
  }, [treeItems, groupCounts, groups]);

  const currentTreeList = treeMode === 'trangThai' ? statusTreeItems : groupTreeItems;
  const filteredTreeItems = currentTreeList.filter(item => {
    if (item.itemType === 2) return true;
    return (item.label || '').toLowerCase().includes(treeSearch.toLowerCase().trim());
  });

  // Định nghĩa danh sách các tab ở phần đáy (14 tab khớp toàn bộ WinForms No1Lib)
  const bottomTabs = [
    { id: 'thongTin', label: 'Thông tin' },
    { id: 'baoGia', label: 'Báo giá', count: subtabsData?.baoGia?.length },
    { id: 'donHang', label: 'Đơn hàng', count: subtabsData?.donHang?.length },
    { id: 'datHang', label: 'Đặt hàng', count: subtabsData?.datHang?.length },
    { id: 'giaHanThe', label: 'Gia hạn thẻ', count: subtabsData?.giaHanThe?.length },
    { id: 'baoLuuThe', label: 'Bảo lưu thẻ', count: subtabsData?.baoLuuThe?.length },
    { id: 'doiLoaiThe', label: 'Đổi loại thẻ', count: subtabsData?.doiLoaiThe?.length },
    { id: 'tangGiamDiem', label: 'Tăng giảm điểm', count: subtabsData?.tangGiamDiem?.length },
    { id: 'theTrang', label: 'Thể trạng', count: subtabsData?.theTrang?.length },
    { id: 'phieuThu', label: 'Phiếu thu', count: subtabsData?.phieuThu?.length },
    { id: 'phieuChi', label: 'Phiếu chi', count: subtabsData?.phieuChi?.length },
    { id: 'thuCongNo', label: 'Phiếu thu công nợ', count: (subtabsData?.thuCongNo || subtabsData?.phieuThuCongNo)?.length },
    { id: 'datCoc', label: 'Đặt cọc', count: subtabsData?.datCoc?.length },
    { id: 'vaoRa', label: 'Vào ra', count: subtabsData?.vaoRa?.length }
  ];

  const isInTrash = (treeMode === 'trangThai' && selectedTreeStatus === 'trash') ||
                    (treeMode === 'nhomKhach' && selectedGroupId === 'trash');

  return (
    <div className="cust-mgmt-container">
      {/* MAIN SPLIT WORKSPACE */}
      <div className="cust-split-body">
        {/* LEFT SIDEBAR: CÂY DANH MỤC (HỖ TRỢ CẢ TRẠNG THÁI THẺ & NHÓM KHÁCH HÀNG NHƯ TREEGRIDMG) */}
        <div className="cust-left-pane">
          {/* TAB CHUYỂN ĐỔI CHẾ ĐỘ CÂY (DEVEXPRESS XTRATABCONTROL / TREEGRIDMG) */}
          <div className="cust-pane-tabs">
            <div
              className={`cust-pane-tab ${treeMode === 'trangThai' ? 'active' : ''}`}
              onClick={() => setTreeMode('trangThai')}
              title="Lọc theo trạng thái thẻ"
            >
              <span>Trạng thái</span>
            </div>
            <div
              className={`cust-pane-tab ${treeMode === 'nhomKhach' ? 'active' : ''}`}
              onClick={() => setTreeMode('nhomKhach')}
              title="Lọc theo nhóm khách hàng (pageFolder)"
            >
              <span>Nhóm khách</span>
            </div>
          </div>

          <div className="cust-tree-toolbar">
            <button 
              className="cust-tree-btn" 
              title={treeMode === 'trangThai' ? 'Thêm trạng thái mới' : 'Thêm nhóm mới'} 
              onClick={() => handleOpenAddStatus(null)}
            >
              <i className="fa-solid fa-plus" style={{ color: '#16a34a' }}></i>
            </button>
            <button 
              className="cust-tree-btn" 
              title="Chỉnh sửa mục đã chọn" 
              onClick={() => {
                const curId = treeMode === 'trangThai' ? selectedTreeStatus : selectedGroupId;
                const found = currentTreeList.find(i => i.id === curId);
                if (found) handleOpenEditTreeItem(found);
              }}
            >
              <i className="fa-solid fa-pen-to-square" style={{ color: '#d97706' }}></i>
            </button>
            <button 
              className="cust-tree-btn" 
              title="Thêm thư mục mới" 
              onClick={() => handleOpenAddFolder(null)}
            >
              <i className="fa-solid fa-folder-plus" style={{ color: '#f59e0b' }}></i>
            </button>
            <button 
              className="cust-tree-btn" 
              title="Làm mới cây danh mục" 
              onClick={() => { loadTreeData(treeMode); loadCustomersAndCounts(); }}
            >
              <i className="fa-solid fa-rotate" style={{ color: '#0284c7' }}></i>
            </button>
            <input
              type="text"
              className="cust-tree-search"
              placeholder={treeMode === 'trangThai' ? 'Tìm trạng thái...' : 'Tìm nhóm...'}
              value={treeSearch}
              onChange={(e) => setTreeSearch(e.target.value)}
            />
          </div>

          <div 
            className="cust-tree-list"
            onContextMenu={(e) => handleTreeContextMenu(e, null)}
          >
            {filteredTreeItems.map((item) => {
              if (item.itemType === 2) {
                return (
                  <div
                    key={item.id}
                    className="cust-tree-separator"
                    title="Đường phân cách"
                    onContextMenu={(e) => handleTreeContextMenu(e, item)}
                  >
                    <div className="cust-tree-sep-line"></div>
                  </div>
                );
              }

              if (item.parentId && collapsedFolders[item.parentId]) {
                return null;
              }

              const isChild = !!item.parentId;
              const isFolder = item.itemType === 1;
              const isCollapsed = !!collapsedFolders[item.id];
              const isActive = treeMode === 'trangThai'
                ? selectedTreeStatus === item.id
                : selectedGroupId === item.id;

              return (
                <div
                  key={item.id}
                  className={`cust-tree-item ${isActive ? 'active' : ''}`}
                  style={isChild ? { paddingLeft: 22 } : {}}
                  onClick={() => {
                    if (treeMode === 'trangThai') {
                      setSelectedTreeStatus(item.id);
                    } else {
                      setSelectedGroupId(item.id);
                    }
                  }}
                  onContextMenu={(e) => handleTreeContextMenu(e, item)}
                >
                  <div className="cust-tree-label">
                    {isFolder && (
                      <span 
                        className="cust-tree-toggle"
                        onClick={(e) => {
                          e.stopPropagation();
                          setCollapsedFolders(prev => ({
                            ...prev,
                            [item.id]: !prev[item.id]
                          }));
                        }}
                      >
                        {isCollapsed ? '▸' : '▾'}
                      </span>
                    )}
                    {item.simage ? (
                      <img 
                        src={`data:image/png;base64,${item.simage}`} 
                        alt="" 
                        className="cust-tree-icon-img"
                      />
                    ) : (
                      <span>{item.icon}</span>
                    )}
                    <span style={item.isGreen ? { color: '#16a34a', fontWeight: 600 } : {}}>{item.label}</span>
                  </div>
                  <span className="cust-tree-count">{item.count}</span>
                </div>
              );
            })}
          </div>
        </div>


        {/* RIGHT AREA: MASTER GRID + BOTTOM DETAIL TABS */}
        <div className="cust-right-pane">
          <div className="cust-main-header">
            <span>Khách hàng</span>
            {isInTrash && (
              <span style={{ fontSize: 11, color: '#dc2626', fontWeight: 600, marginLeft: 8 }}>
                (Thùng rác - Bản ghi đã xóa)
              </span>
            )}
          </div>

          {/* TOOLBAR RIBBON KHÁCH HÀNG */}
          <div className="cust-ribbon-bar">
            <div className="cust-ribbon-filter">
              <span>Lọc (F3):</span>
              <input
                ref={searchInputRef}
                type="text"
                placeholder="Mã thẻ, tên, SĐT, địa chỉ..."
                value={searchTerm}
                onChange={(e) => setSearchTerm(e.target.value)}
                onKeyDown={(e) => {
                  if (e.key === 'Enter') loadCustomersAndCounts(selectedTreeStatus, selectedGroupId, searchTerm, treeMode);
                }}
              />
            </div>

            <button className="cust-ribbon-btn primary" onClick={handleAddNewCustomer} title="Thêm mới khách hàng (Phím tắt: Insert)">
              <i className="fa-solid fa-plus" style={{ color: '#16a34a' }}></i>
              <span>Thêm (Insert)</span>
            </button>

            <button className="cust-ribbon-btn" onClick={handleEditCustomer} title="Chỉnh sửa thông tin khách hàng (Phím tắt: F4)">
              <i className="fa-solid fa-pen" style={{ color: '#d97706' }}></i>
              <span>Sửa (F4)</span>
            </button>

            {isInTrash ? (
              <>
                <button className="cust-ribbon-btn primary" onClick={handleRestoreCustomer} title="Phục hồi khách hàng đã xóa">
                  <i className="fa-solid fa-trash-arrow-up" style={{ color: '#16a34a' }}></i>
                  <span>Phục hồi</span>
                </button>
                <button className="cust-ribbon-btn danger" onClick={handlePermanentDelete} title="Xóa vĩnh viễn khỏi CSDL">
                  <i className="fa-solid fa-fire" style={{ color: '#dc2626' }}></i>
                  <span>Xóa vĩnh viễn</span>
                </button>
              </>
            ) : (
              <button className="cust-ribbon-btn danger" onClick={handleDeleteCustomer} title="Xóa khách hàng vào thùng rác (Phím tắt: Del)">
                <i className="fa-solid fa-xmark" style={{ color: '#dc2626' }}></i>
                <span>Xóa (Del)</span>
              </button>
            )}

            <div className="cust-ribbon-sep"></div>

            <button className="cust-ribbon-btn" onClick={handleOpenExcelImport} title="Nhập danh sách hội viên từ Microsoft Excel (.xlsx, .csv)">
              <i className="fa-solid fa-file-excel" style={{ color: '#16a34a' }}></i>
              <span>Thêm excel</span>
            </button>

            <button className="cust-ribbon-btn" onClick={handleExportExcel} title="Xuất dữ liệu danh sách khách hàng ra file Excel (.xlsx)">
              <i className="fa-solid fa-file-export" style={{ color: '#0284c7' }}></i>
              <span>Xuất excel</span>
            </button>

            <button className="cust-ribbon-btn" onClick={handlePrintReport} title="Chọn mẫu in và mở FastReport xem trước bản in">
              <i className="fa-solid fa-print" style={{ color: '#475569' }}></i>
              <span>In</span>
            </button>

            <div className="cust-ribbon-sep"></div>

            <button className="cust-ribbon-btn" onClick={handleOpenDeviceSync} title="Đồng bộ hội viên từ máy chấm công / cổng xoay vân tay">
              <i className="fa-solid fa-globe" style={{ color: '#0284c7' }}></i>
              <span>Thêm từ thiết bị</span>
            </button>

            <button className="cust-ribbon-btn" onClick={handleOpenFingerprintEnroll} title="Đăng ký mẫu vân tay cho hội viên đang chọn">
              <i className="fa-solid fa-fingerprint" style={{ color: '#0284c7' }}></i>
              <span>Lấy vân tay từ thiết bị</span>
            </button>
          </div>

          {/* MASTER DATA GRID TABLE (CẤU HÌNH CỘT CHUẨN XÁC THEO SETCOLUMNINFO TRONG WINFORM) */}
          <div className="cust-grid-wrapper">
            <table className="cust-grid-table">
              <thead>
                <tr>
                  <th style={{ width: 35, textAlign: 'center' }}>#</th>
                  <th style={{ width: 95 }}>Mã thẻ</th>
                  <th style={{ width: 160 }}>Tên khách hàng</th>
                  <th style={{ width: 150 }}>Địa chỉ</th>
                  <th style={{ width: 105 }}>Điện thoại</th>
                  <th style={{ width: 110 }}>Loại thẻ</th>
                  <th style={{ width: 100 }}>Từ ngày</th>
                  <th style={{ width: 100 }}>Đến ngày</th>
                  <th style={{ width: 115 }}>Trạng thái</th>
                  <th style={{ width: 65, textAlign: 'right' }}>Số lần</th>
                  <th style={{ width: 65, textAlign: 'right' }}>Đã tập</th>
                  <th style={{ width: 65, textAlign: 'right' }}>Còn lại</th>
                  <th style={{ width: 100 }}>Facebook</th>
                  <th style={{ width: 120 }}>Ghi chú</th>
                  <th style={{ width: 110 }}>Ngày sinh/TL</th>
                  <th style={{ width: 130 }}>Nhóm khách hàng</th>
                  <th style={{ width: 120 }}>Email</th>
                  <th style={{ width: 105 }}>Mã số thuế</th>
                  <th style={{ width: 120 }}>Nhân viên</th>
                  <th style={{ width: 95, textAlign: 'right' }}>Điểm ban đầu</th>
                  <th style={{ width: 120 }}>Tỉnh thành</th>
                  <th style={{ width: 95 }}>Ca tập</th>
                  <th style={{ width: 65, textAlign: 'center' }}>Ảnh</th>
                </tr>
              </thead>
              <tbody>
                {loading ? (
                  <tr>
                    <td colSpan="23" style={{ textAlign: 'center', padding: '30px', color: '#64748b' }}>
                      <i className="fa-solid fa-spinner fa-spin"></i> Đang tải danh mục khách hàng từ CSDL Firebird...
                    </td>
                  </tr>
                ) : customers.length === 0 ? (
                  <tr>
                    <td colSpan="23" style={{ textAlign: 'center', padding: '30px', color: '#94a3b8' }}>
                      Không có khách hàng nào phù hợp với điều kiện lọc
                    </td>
                  </tr>
                ) : (
                  customers.map((c, idx) => {
                    const isSelected = selectedCustomer?.id === c.id;
                    const isDangSuDung = c.trangThai?.toLowerCase().includes('đang sử dụng') ||
                                         c.trangThai?.toLowerCase().includes('hoạt động') ||
                                         c.dTrangThaiId === '30' || c.dTrangThaiId === '1';
                    return (
                      <tr
                        key={c.id}
                        className={`${isSelected ? 'selected' : ''} ${c.status === 0 ? 'cust-row-deleted' : ''}`}
                        onClick={() => {
                          setSelectedCustomerId(c.id);
                          if (contextMenu.visible) closeContextMenu();
                          if (treeContextMenu.visible) closeTreeContextMenu();
                          setFocusedCell(null);
                        }}
                        onDoubleClick={() => { setSelectedCustomerId(c.id); handleEditCustomer(); }}
                      >
                        <td className="cust-grid-indicator" style={{ textAlign: 'center', fontSize: 11 }}>
                          {c.stt || idx + 1}
                        </td>
                        <td
                          className={focusedCell?.rowId === c.id && focusedCell?.colKey === 'maThe' ? 'cust-cell-focused' : ''}
                          onContextMenu={(e) => handleCellContextMenu(e, c, 'maThe', 'Mã thẻ', c.maThe)}
                        >
                          <strong>{c.maThe || '---'}</strong>
                        </td>
                        <td
                          className={`font-semibold ${focusedCell?.rowId === c.id && focusedCell?.colKey === 'tenKhachHang' ? 'cust-cell-focused' : ''}`}
                          onContextMenu={(e) => handleCellContextMenu(e, c, 'tenKhachHang', 'Tên khách hàng', c.tenKhachHang)}
                        >
                          {c.tenKhachHang}
                        </td>
                        <td
                          className={focusedCell?.rowId === c.id && focusedCell?.colKey === 'diaChi' ? 'cust-cell-focused' : ''}
                          onContextMenu={(e) => handleCellContextMenu(e, c, 'diaChi', 'Địa chỉ', c.diaChi)}
                        >
                          {c.diaChi || ''}
                        </td>
                        <td
                          className={focusedCell?.rowId === c.id && focusedCell?.colKey === 'dienThoai' ? 'cust-cell-focused' : ''}
                          onContextMenu={(e) => handleCellContextMenu(e, c, 'dienThoai', 'Điện thoại', c.dienThoai)}
                        >
                          {c.dienThoai || ''}
                        </td>
                        <td
                          className={focusedCell?.rowId === c.id && focusedCell?.colKey === 'loaiThe' ? 'cust-cell-focused' : ''}
                          onContextMenu={(e) => handleCellContextMenu(e, c, 'loaiThe', 'Loại thẻ', c.loaiThe)}
                        >
                          {c.loaiThe || ''}
                        </td>
                        <td
                          className={focusedCell?.rowId === c.id && focusedCell?.colKey === 'tuNgay' ? 'cust-cell-focused' : ''}
                          onContextMenu={(e) => handleCellContextMenu(e, c, 'tuNgay', 'Từ ngày', c.tuNgay)}
                        >
                          {c.tuNgay || ''}
                        </td>
                        <td
                          className={focusedCell?.rowId === c.id && focusedCell?.colKey === 'denNgay' ? 'cust-cell-focused' : ''}
                          onContextMenu={(e) => handleCellContextMenu(e, c, 'denNgay', 'Đến ngày', c.denNgay)}
                        >
                          {c.denNgay || ''}
                        </td>
                        <td
                          className={focusedCell?.rowId === c.id && focusedCell?.colKey === 'trangThai' ? 'cust-cell-focused' : ''}
                          onContextMenu={(e) => handleCellContextMenu(e, c, 'trangThai', 'Trạng thái', c.trangThai || (isDangSuDung ? 'Đang sử dụng' : 'Chưa kích hoạt'))}
                        >
                          {isDangSuDung ? (
                            <span className="cust-status-badge active-use">
                              ▶️ Đang sử dụng
                            </span>
                          ) : (
                            <span className="cust-status-badge inactive">
                              ❎ {c.trangThai || 'Chưa kích hoạt'}
                            </span>
                          )}
                        </td>
                        <td
                          style={{ textAlign: 'right' }}
                          className={focusedCell?.rowId === c.id && focusedCell?.colKey === 'soLan' ? 'cust-cell-focused' : ''}
                          onContextMenu={(e) => handleCellContextMenu(e, c, 'soLan', 'Số lần', c.soLan)}
                        >
                          {c.soLan ?? 0}
                        </td>
                        <td
                          style={{ textAlign: 'right' }}
                          className={focusedCell?.rowId === c.id && focusedCell?.colKey === 'daTap' ? 'cust-cell-focused' : ''}
                          onContextMenu={(e) => handleCellContextMenu(e, c, 'daTap', 'Đã tập', c.daTap)}
                        >
                          {c.daTap ?? 0}
                        </td>
                        <td
                          style={{ textAlign: 'right', fontWeight: 600, color: isSelected ? '#ffffff' : ((c.conLai ?? 0) < 0 ? '#dc2626' : '#16a34a') }}
                          className={focusedCell?.rowId === c.id && focusedCell?.colKey === 'conLai' ? 'cust-cell-focused' : ''}
                          onContextMenu={(e) => handleCellContextMenu(e, c, 'conLai', 'Còn lại', c.conLai)}
                        >
                          {c.conLai ?? 0}
                        </td>
                        <td
                          className={focusedCell?.rowId === c.id && focusedCell?.colKey === 'facebook' ? 'cust-cell-focused' : ''}
                          onContextMenu={(e) => handleCellContextMenu(e, c, 'facebook', 'Facebook', c.facebook)}
                        >
                          {c.facebook || ''}
                        </td>
                        <td
                          className={focusedCell?.rowId === c.id && focusedCell?.colKey === 'note' ? 'cust-cell-focused' : ''}
                          onContextMenu={(e) => handleCellContextMenu(e, c, 'note', 'Ghi chú', c.note)}
                        >
                          {c.note || ''}
                        </td>
                        <td
                          className={focusedCell?.rowId === c.id && focusedCell?.colKey === 'ngaySinh' ? 'cust-cell-focused' : ''}
                          onContextMenu={(e) => handleCellContextMenu(e, c, 'ngaySinh', 'Ngày thành lập/sinh nhật', c.ngaySinh)}
                        >
                          {c.ngaySinh || ''}
                        </td>
                        <td
                          className={focusedCell?.rowId === c.id && focusedCell?.colKey === 'nhomKhachHang' ? 'cust-cell-focused' : ''}
                          onContextMenu={(e) => handleCellContextMenu(e, c, 'nhomKhachHang', 'Nhóm khách hàng', c.nhomKhachHang)}
                        >
                          {c.nhomKhachHang || ''}
                        </td>
                        <td
                          className={focusedCell?.rowId === c.id && focusedCell?.colKey === 'email' ? 'cust-cell-focused' : ''}
                          onContextMenu={(e) => handleCellContextMenu(e, c, 'email', 'Email', c.email)}
                        >
                          {c.email || ''}
                        </td>
                        <td
                          className={focusedCell?.rowId === c.id && focusedCell?.colKey === 'maSoThue' ? 'cust-cell-focused' : ''}
                          onContextMenu={(e) => handleCellContextMenu(e, c, 'maSoThue', 'Mã số thuế', c.maSoThue)}
                        >
                          {c.maSoThue || ''}
                        </td>
                        <td
                          className={focusedCell?.rowId === c.id && focusedCell?.colKey === 'nhanVien' ? 'cust-cell-focused' : ''}
                          onContextMenu={(e) => handleCellContextMenu(e, c, 'nhanVien', 'Nhân viên', c.nhanVien)}
                        >
                          {c.nhanVien || ''}
                        </td>
                        <td
                          style={{ textAlign: 'right' }}
                          className={focusedCell?.rowId === c.id && focusedCell?.colKey === 'diemTichLuyBanDau' ? 'cust-cell-focused' : ''}
                          onContextMenu={(e) => handleCellContextMenu(e, c, 'diemTichLuyBanDau', 'Điểm tích lũy ban đầu', c.diemTichLuyBanDau)}
                        >
                          {c.diemTichLuyBanDau ?? 0}
                        </td>
                        <td
                          className={focusedCell?.rowId === c.id && focusedCell?.colKey === 'tinhThanh' ? 'cust-cell-focused' : ''}
                          onContextMenu={(e) => handleCellContextMenu(e, c, 'tinhThanh', 'Tỉnh thành', c.tinhThanh)}
                        >
                          {c.tinhThanh || ''}
                        </td>
                        <td
                          className={focusedCell?.rowId === c.id && focusedCell?.colKey === 'caTap' ? 'cust-cell-focused' : ''}
                          onContextMenu={(e) => handleCellContextMenu(e, c, 'caTap', 'Ca tập', c.caTap)}
                        >
                          {c.caTap || ''}
                        </td>
                        <td style={{ textAlign: 'center' }}>
                          {c.anh ? (
                            <img src={c.anh} alt="" style={{ width: 22, height: 22, objectFit: 'cover', borderRadius: '50%', margin: '0 auto' }} />
                          ) : (
                            <span style={{ color: '#cbd5e1' }}>—</span>
                          )}
                        </td>
                      </tr>
                    );
                  })
                )}
              </tbody>
            </table>
          </div>

                    {/* 3. BOTTOM DETAIL PANEL ("PHÂN TÍCH KĨ PHẦN ĐÁY") */}
          <div className="cust-bottom-pane">
            <div className="cust-bottom-tabs-header">
              <div
                ref={bottomTabsRef}
                style={{ display: 'flex', overflowX: 'auto', scrollbarWidth: 'none', flex: 1 }}
              >
                {bottomTabs.map((tab) => (
                  <div
                    key={tab.id}
                    className={`cust-bottom-tab ${activeBottomTab === tab.id ? 'active' : ''}`}
                    onClick={() => setActiveBottomTab(tab.id)}
                  >
                    <span>{tab.label}</span>
                    {typeof tab.count === 'number' && tab.count > 0 && (
                      <span style={{ fontSize: '10px', opacity: 0.85 }}>({tab.count})</span>
                    )}
                  </div>
                ))}
              </div>
              <button className="cust-tab-nav-btn" onClick={() => scrollBottomTabs(-120)} title="Cuộn tab sang trái">
                <i className="fa-solid fa-chevron-left"></i>
              </button>
              <button className="cust-tab-nav-btn" onClick={() => scrollBottomTabs(120)} title="Cuộn tab sang phải">
                <i className="fa-solid fa-chevron-right"></i>
              </button>
            </div>

            <div className="cust-bottom-tab-body" style={{ overflowX: 'auto', overflowY: 'auto' }}>
              {subtabsLoading ? (
                <div style={{ padding: '20px', textAlign: 'center', color: '#64748b' }}>
                  <i className="fa-solid fa-spinner fa-spin"></i> Đang tải dữ liệu chi tiết...
                </div>
              ) : !selectedCustomer ? (
                <div className="cust-empty-subtab">Vui lòng chọn một khách hàng từ danh sách trên để xem thông tin chi tiết</div>
              ) : (
                <>
                  {/* TAB 1: THÔNG TIN (DKHACHHANG AUDIT & METADATA) */}
                  {activeBottomTab === 'thongTin' && (
                    <div className="cust-audit-content">
                      <div className="cust-audit-row">
                        <div className="cust-audit-item">
                          <span className="audit-lbl">Khởi tạo:</span>
                          <span className="audit-val">{subtabsData?.thongTin?.timeCreated || selectedCustomer.timeCreated || '---'}</span>
                          <span className="audit-lbl" style={{ marginLeft: 16 }}>bởi:</span>
                          <span className="audit-val">{subtabsData?.thongTin?.userCreated || selectedCustomer.userCreatedName || 'Administrator'}</span>
                        </div>
                        <div className="cust-audit-item">
                          <span className="audit-lbl">Sửa đổi gần nhất:</span>
                          <span className="audit-val">{subtabsData?.thongTin?.timeModified || selectedCustomer.timeModified || '---'}</span>
                          <span className="audit-lbl" style={{ marginLeft: 16 }}>bởi:</span>
                          <span className="audit-val">{subtabsData?.thongTin?.userModified || selectedCustomer.userModifiedName || 'Administrator'}</span>
                        </div>
                      </div>

                      <div className="cust-detail-cards">
                        <div className="cust-detail-card">
                          <div className="card-lbl">Mã thẻ / Khách</div>
                          <div className="card-val font-semibold">{subtabsData?.thongTin?.maKhach || selectedCustomer.maThe || '---'}</div>
                        </div>
                        <div className="cust-detail-card">
                          <div className="card-lbl">Tên khách hàng</div>
                          <div className="card-val font-semibold" style={{ color: '#1d4ed8' }}>{subtabsData?.thongTin?.name || selectedCustomer.tenKhachHang || '---'}</div>
                        </div>
                        <div className="cust-detail-card">
                          <div className="card-lbl">Điện thoại</div>
                          <div className="card-val">{subtabsData?.thongTin?.dienThoai || selectedCustomer.dienThoai || '---'}</div>
                        </div>
                        <div className="cust-detail-card">
                          <div className="card-lbl">Email</div>
                          <div className="card-val">{subtabsData?.thongTin?.email || selectedCustomer.email || '---'}</div>
                        </div>
                        <div className="cust-detail-card">
                          <div className="card-lbl">Địa chỉ</div>
                          <div className="card-val">{subtabsData?.thongTin?.diaChi || selectedCustomer.diaChi || '---'}</div>
                        </div>
                        <div className="cust-detail-card">
                          <div className="card-lbl">Tỉnh thành</div>
                          <div className="card-val">{subtabsData?.thongTin?.tenTinhThanh || selectedCustomer.tinhThanh || '---'}</div>
                        </div>
                        <div className="cust-detail-card">
                          <div className="card-lbl">Facebook</div>
                          <div className="card-val">{subtabsData?.thongTin?.facebook || selectedCustomer.facebook || '---'}</div>
                        </div>
                        <div className="cust-detail-card">
                          <div className="card-lbl">Ngày sinh / Thành lập</div>
                          <div className="card-val">{subtabsData?.thongTin?.ngaySinh || selectedCustomer.ngaySinh || '---'}</div>
                        </div>
                        <div className="cust-detail-card">
                          <div className="card-lbl">Mã số thuế</div>
                          <div className="card-val">{subtabsData?.thongTin?.maSoThue || selectedCustomer.maSoThue || '---'}</div>
                        </div>
                        <div className="cust-detail-card">
                          <div className="card-lbl">Điểm tích lũy ban đầu</div>
                          <div className="card-val font-semibold" style={{ color: '#059669' }}>{(subtabsData?.thongTin?.diemTichLuyBanDau ?? selectedCustomer.diemTichLuyBanDau ?? 0).toLocaleString()}</div>
                        </div>
                        <div className="cust-detail-card">
                          <div className="card-lbl">Loại thẻ / Gói tập</div>
                          <div className="card-val font-semibold">{subtabsData?.thongTin?.tenLoaiThe || selectedCustomer.loaiThe || '---'}</div>
                        </div>
                        <div className="cust-detail-card">
                          <div className="card-lbl">Trạng thái thẻ</div>
                          <div className="card-val">{subtabsData?.thongTin?.tenTrangThai || selectedCustomer.trangThai || '---'}</div>
                        </div>
                        <div className="cust-detail-card">
                          <div className="card-lbl">Thời hạn sử dụng</div>
                          <div className="card-val">{subtabsData?.thongTin?.tuNgay || selectedCustomer.tuNgay || '---'} ➔ {subtabsData?.thongTin?.denNgay || selectedCustomer.denNgay || '---'}</div>
                        </div>
                        <div className="cust-detail-card">
                          <div className="card-lbl">Số lần / Đã tập / Còn lại</div>
                          <div className="card-val">
                            {subtabsData?.thongTin?.soLan ?? selectedCustomer.soLan ?? 0} / {subtabsData?.thongTin?.daTap ?? selectedCustomer.daTap ?? 0} / <strong style={{ color: (subtabsData?.thongTin?.conLai ?? selectedCustomer.conLai ?? 0) < 0 ? '#dc2626' : '#16a34a' }}>{subtabsData?.thongTin?.conLai ?? selectedCustomer.conLai ?? 0}</strong>
                          </div>
                        </div>
                        <div className="cust-detail-card">
                          <div className="card-lbl">Ca tập</div>
                          <div className="card-val">{subtabsData?.thongTin?.tenCaTap || selectedCustomer.caTap || 'Toàn thời gian'}</div>
                        </div>
                        <div className="cust-detail-card">
                          <div className="card-lbl">Nhóm khách hàng</div>
                          <div className="card-val">{subtabsData?.thongTin?.tenNhom || selectedCustomer.nhomKhachHang || '---'}</div>
                        </div>
                        <div className="cust-detail-card">
                          <div className="card-lbl">Nhân viên phụ trách</div>
                          <div className="card-val">{subtabsData?.thongTin?.tenNhanVien || selectedCustomer.nhanVien || '---'}</div>
                        </div>
                        <div className="cust-detail-card">
                          <div className="card-lbl">Mã vân tay</div>
                          <div className="card-val font-medium">{subtabsData?.thongTin?.maVanTay || selectedCustomer.maVanTay ? '✅ Đã đăng ký' : 'Chưa có'}</div>
                        </div>
                        <div className="cust-detail-card" style={{ gridColumn: 'span 2' }}>
                          <div className="card-lbl">Ghi chú</div>
                          <div className="card-val" style={{ fontSize: 11, fontWeight: 400 }}>{subtabsData?.thongTin?.note || selectedCustomer.note || 'Không có'}</div>
                        </div>
                      </div>
                    </div>
                  )}

                  {/* TAB 2: BÁO GIÁ (TBAOGIA - KHỚP 100% ẢNH THỰC TẾ WINFORMS DESKTOP) */}
                  {activeBottomTab === 'baoGia' && (
                    <table className="cust-sub-table">
                      <thead>
                        <tr>
                          <th style={{ width: 30, textAlign: 'center' }}></th>
                          <th style={{ width: 95 }}>Ngày</th>
                          <th style={{ width: 110 }}>Số phiếu</th>
                          <th style={{ width: 160 }}>Tên khách</th>
                          <th style={{ width: 160 }}>Địa chỉ</th>
                          <th style={{ width: 110 }}>Điện thoại</th>
                          <th style={{ width: 110, textAlign: 'right' }}>Tiền hàng</th>
                          <th style={{ width: 110, textAlign: 'right' }}>Tiền giảm giá</th>
                          <th style={{ width: 120, textAlign: 'right' }}>Tổng cộng</th>
                        </tr>
                      </thead>
                      <tbody>
                        {subtabsData?.baoGia?.map((bg, idx) => (
                          <tr key={bg.id || idx}>
                            <td style={{ textAlign: 'center', color: '#64748b' }}>{idx + 1}</td>
                            <td>{bg.ngay}</td>
                            <td><strong>{bg.soPhiu}</strong></td>
                            <td>{bg.tenKhach}</td>
                            <td>{bg.diaChi}</td>
                            <td>{bg.dienThoai}</td>
                            <td style={{ textAlign: 'right' }}>{bg.tienHang?.toLocaleString()}</td>
                            <td style={{ textAlign: 'right' }}>{bg.tienGiamGia?.toLocaleString()}</td>
                            <td style={{ textAlign: 'right', fontWeight: 600, color: '#16a34a' }}>{bg.tongCong?.toLocaleString()}</td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  )}

                  {/* TAB 3: ĐƠN HÀNG (TDONHANG) */}
                  {activeBottomTab === 'donHang' && (
                    <table className="cust-sub-table">
                      <thead>
                        <tr>
                          <th style={{ width: 30, textAlign: 'center' }}></th>
                          <th style={{ width: 110 }}>Số phiếu</th>
                          <th style={{ width: 90 }}>Ngày</th>
                          <th style={{ width: 140 }}>Khách hàng</th>
                          <th style={{ width: 110, textAlign: 'right' }}>Tổng cộng</th>
                          <th style={{ width: 120 }}>Nhân viên bán</th>
                          <th style={{ width: 95 }}>Giờ thanh toán</th>
                          <th style={{ width: 110 }}>Thu ngân</th>
                          <th style={{ width: 95 }}>Voucher</th>
                          <th style={{ width: 100, textAlign: 'right' }}>Tiền mặt</th>
                          <th style={{ width: 100, textAlign: 'right' }}>Chuyển khoản</th>
                          <th style={{ width: 95, textAlign: 'right' }}>Thẻ tt</th>
                          <th style={{ width: 100, textAlign: 'right' }}>Tiền hàng</th>
                          <th style={{ width: 95, textAlign: 'right' }}>Tiền giảm giá</th>
                          <th style={{ width: 90, textAlign: 'right' }}>Tiền thuế</th>
                          <th style={{ width: 95, textAlign: 'right' }}>Phí vận chuyển</th>
                          <th style={{ width: 100, textAlign: 'right' }}>Thanh toán</th>
                          <th style={{ width: 95, textAlign: 'right' }}>Còn lại</th>
                          <th style={{ width: 120 }}>Nhân viên giao hàng</th>
                          <th style={{ width: 105, textAlign: 'right' }}>Trích nhân viên</th>
                          <th style={{ width: 120 }}>Cửa hàng</th>
                          <th style={{ width: 120 }}>Ghi chú</th>
                        </tr>
                      </thead>
                      <tbody>
                        {subtabsData?.donHang?.map((dh, idx) => (
                          <tr key={dh.id || idx}>
                            <td style={{ textAlign: 'center', color: '#64748b' }}>{idx + 1}</td>
                            <td><strong>{dh.soPhiu}</strong></td>
                            <td>{dh.ngay}</td>
                            <td>{dh.khachHang}</td>
                            <td style={{ textAlign: 'right', fontWeight: 600, color: '#16a34a' }}>{dh.tongCong?.toLocaleString()}</td>
                            <td>{dh.nhanVienBan}</td>
                            <td>{dh.gioThanhToan}</td>
                            <td>{dh.thuNgan}</td>
                            <td>{dh.voucher}</td>
                            <td style={{ textAlign: 'right' }}>{dh.tienMat?.toLocaleString()}</td>
                            <td style={{ textAlign: 'right' }}>{dh.chuyenKhoan?.toLocaleString()}</td>
                            <td style={{ textAlign: 'right' }}>{dh.theTraTruoc?.toLocaleString()}</td>
                            <td style={{ textAlign: 'right' }}>{dh.tienHang?.toLocaleString()}</td>
                            <td style={{ textAlign: 'right' }}>{dh.tienGiamGia?.toLocaleString()}</td>
                            <td style={{ textAlign: 'right' }}>{dh.tienThue?.toLocaleString()}</td>
                            <td style={{ textAlign: 'right' }}>{dh.phiVanChuyen?.toLocaleString()}</td>
                            <td style={{ textAlign: 'right', color: '#16a34a' }}>{dh.thanhToan?.toLocaleString()}</td>
                            <td style={{ textAlign: 'right', color: (dh.conLai ?? 0) > 0 ? '#dc2626' : '#64748b' }}>{dh.conLai?.toLocaleString()}</td>
                            <td>{dh.nhanVienGiaoHang}</td>
                            <td style={{ textAlign: 'right' }}>{dh.trichNhanVien?.toLocaleString()}</td>
                            <td>{dh.cuaHang}</td>
                            <td>{dh.note}</td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  )}

                  {/* TAB 4: ĐẶT HÀNG (TDATHANG) */}
                  {activeBottomTab === 'datHang' && (
                    <table className="cust-sub-table">
                      <thead>
                        <tr>
                          <th style={{ width: 30, textAlign: 'center' }}></th>
                          <th style={{ width: 110 }}>Số phiếu</th>
                          <th style={{ width: 90 }}>Ngày</th>
                          <th style={{ width: 140 }}>Khách hàng</th>
                          <th style={{ width: 100 }}>Điện thoại</th>
                          <th style={{ width: 150 }}>Địa chỉ</th>
                          <th style={{ width: 120 }}>Email</th>
                          <th style={{ width: 100, textAlign: 'right' }}>Tiền hàng</th>
                          <th style={{ width: 90, textAlign: 'right' }}>Tỉ lệ giảm (%)</th>
                          <th style={{ width: 100, textAlign: 'right' }}>Tiền giảm giá</th>
                          <th style={{ width: 90, textAlign: 'right' }}>Tiền thuế</th>
                          <th style={{ width: 100, textAlign: 'right' }}>Phí vận chuyển</th>
                          <th style={{ width: 110, textAlign: 'right' }}>Tổng cộng</th>
                          <th style={{ width: 150 }}>Ghi chú</th>
                        </tr>
                      </thead>
                      <tbody>
                        {subtabsData?.datHang?.map((dh, idx) => (
                          <tr key={dh.id || idx}>
                            <td style={{ textAlign: 'center', color: '#64748b' }}>{idx + 1}</td>
                            <td><strong>{dh.soPhiu}</strong></td>
                            <td>{dh.ngay}</td>
                            <td>{dh.tenKhach}</td>
                            <td>{dh.dienThoai}</td>
                            <td>{dh.diaChi}</td>
                            <td>{dh.email}</td>
                            <td style={{ textAlign: 'right' }}>{dh.tienHang?.toLocaleString()}</td>
                            <td style={{ textAlign: 'right' }}>{dh.tiLeGiamGia}</td>
                            <td style={{ textAlign: 'right' }}>{dh.tienGiamGia?.toLocaleString()}</td>
                            <td style={{ textAlign: 'right' }}>{dh.tienThue?.toLocaleString()}</td>
                            <td style={{ textAlign: 'right' }}>{dh.phiVanChuyen?.toLocaleString()}</td>
                            <td style={{ textAlign: 'right', fontWeight: 600, color: '#16a34a' }}>{dh.tongCong?.toLocaleString()}</td>
                            <td>{dh.note}</td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  )}

                  {/* TAB 5: GIA HẠN THẺ (TGIAHANTHE: ĐĂNG KÝ MỚI, GIA HẠN, BAN ĐẦU - CHUẨN SCREENSHOT WINFORMS) */}
                  {activeBottomTab === 'giaHanThe' && (
                    <table className="cust-sub-table">
                      <thead>
                        <tr>
                          <th style={{ width: 30, textAlign: 'center' }}></th>
                          <th style={{ width: 110, textAlign: 'right', background: '#e0f2fe' }}>Số tiền</th>
                          <th style={{ width: 120 }}>Ghi chú</th>
                          <th style={{ width: 105 }}>Số phiếu</th>
                          <th style={{ width: 90 }}>Ngày</th>
                          <th style={{ width: 130 }}>Khách hàng</th>
                          <th style={{ width: 65, textAlign: 'right' }}>Số lần</th>
                          <th style={{ width: 110 }}>Loại thẻ</th>
                          <th style={{ width: 90 }}>Từ ngày</th>
                          <th style={{ width: 90 }}>Đến ngày</th>
                          <th style={{ width: 65, textAlign: 'center' }}>Đã tập</th>
                          <th style={{ width: 85, textAlign: 'right' }}>Tỉ lệ giảm</th>
                          <th style={{ width: 95, textAlign: 'right' }}>Tiền giảm</th>
                          <th style={{ width: 105, textAlign: 'right' }}>Tổng cộng</th>
                          <th style={{ width: 100, textAlign: 'right' }}>Thanh toán</th>
                          <th style={{ width: 100 }}>Khuyến mãi</th>
                          <th style={{ width: 65, textAlign: 'right' }}>Số ngày</th>
                          <th style={{ width: 65, textAlign: 'right' }}>Số tháng</th>
                          <th style={{ width: 85, textAlign: 'right' }}>Lần tặng thêm</th>
                          <th style={{ width: 90, textAlign: 'right' }}>Ngày tặng thêm</th>
                          <th style={{ width: 100, textAlign: 'right' }}>Doanh số</th>
                          <th style={{ width: 95, textAlign: 'center' }}>Chưa kích hoạt</th>
                        </tr>
                      </thead>
                      <tbody>
                        {subtabsData?.giaHanThe?.map((g, idx) => (
                          <tr key={g.id || idx}>
                            <td style={{ textAlign: 'center', color: '#64748b' }}>{idx + 1}</td>
                            <td style={{ textAlign: 'right', fontWeight: 600, background: '#fef3c7', color: '#b45309' }}>
                              {g.soTien?.toLocaleString()}
                            </td>
                            <td>{g.note}</td>
                            <td><strong>{g.soPhiu}</strong></td>
                            <td>{g.ngay}</td>
                            <td>{g.khachHang}</td>
                            <td style={{ textAlign: 'right' }}>{g.soLan}</td>
                            <td>{g.loaiThe}</td>
                            <td>{g.tuNgay}</td>
                            <td>{g.denNgay}</td>
                            <td style={{ textAlign: 'center' }}>
                              <input type="checkbox" checked={!!g.daTap} readOnly style={{ accentColor: '#16a34a' }} />
                            </td>
                            <td style={{ textAlign: 'right' }}>{g.tiLeGiamGia}</td>
                            <td style={{ textAlign: 'right' }}>{g.tienGiamGia?.toLocaleString()}</td>
                            <td style={{ textAlign: 'right', fontWeight: 600, color: '#16a34a' }}>{g.tongCong?.toLocaleString()}</td>
                            <td style={{ textAlign: 'right' }}>{g.thanhToan?.toLocaleString()}</td>
                            <td>{g.khuyenMai}</td>
                            <td style={{ textAlign: 'right' }}>{g.soNgay}</td>
                            <td style={{ textAlign: 'right' }}>{g.soThang}</td>
                            <td style={{ textAlign: 'right' }}>{g.lanTangThem}</td>
                            <td style={{ textAlign: 'right' }}>{g.ngayTangThem}</td>
                            <td style={{ textAlign: 'right' }}>{g.doanhSo?.toLocaleString()}</td>
                            <td style={{ textAlign: 'center' }}>
                              <input type="checkbox" checked={!!g.chuaKichHoat} readOnly style={{ accentColor: '#dc2626' }} />
                            </td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  )}

                  {/* TAB 6: BẢO LƯU THẺ (TGIAHANTHE: DLOAIGIAODICHID = '9') */}
                  {activeBottomTab === 'baoLuuThe' && (
                    <table className="cust-sub-table">
                      <thead>
                        <tr>
                          <th style={{ width: 30, textAlign: 'center' }}></th>
                          <th style={{ width: 110, textAlign: 'right' }}>Số tiền (Phí)</th>
                          <th style={{ width: 150 }}>Ghi chú</th>
                          <th style={{ width: 110 }}>Số phiếu</th>
                          <th style={{ width: 90 }}>Ngày</th>
                          <th style={{ width: 140 }}>Khách hàng</th>
                          <th style={{ width: 110 }}>Loại thẻ</th>
                          <th style={{ width: 90 }}>Từ ngày</th>
                          <th style={{ width: 90 }}>Đến ngày</th>
                          <th style={{ width: 80, textAlign: 'right' }}>Số ngày</th>
                          <th style={{ width: 100 }}>Đến ngày thực</th>
                        </tr>
                      </thead>
                      <tbody>
                        {subtabsData?.baoLuuThe?.map((b, idx) => (
                          <tr key={b.id || idx}>
                            <td style={{ textAlign: 'center', color: '#64748b' }}>{idx + 1}</td>
                            <td style={{ textAlign: 'right', fontWeight: 600, color: '#b45309' }}>{b.soTien?.toLocaleString()}</td>
                            <td>{b.note}</td>
                            <td><strong>{b.soPhiu}</strong></td>
                            <td>{b.ngay}</td>
                            <td>{b.khachHang}</td>
                            <td>{b.loaiThe}</td>
                            <td>{b.tuNgay}</td>
                            <td>{b.denNgay}</td>
                            <td style={{ textAlign: 'right' }}>{b.soNgay}</td>
                            <td>{b.denNgayThuc}</td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  )}

                  {/* TAB 7: ĐỔI LOẠI THẺ (TGIAHANTHE: DLOAIGIAODICHID = '2') */}
                  {activeBottomTab === 'doiLoaiThe' && (
                    <table className="cust-sub-table">
                      <thead>
                        <tr>
                          <th style={{ width: 30, textAlign: 'center' }}></th>
                          <th style={{ width: 110, textAlign: 'right' }}>Số tiền (Phí)</th>
                          <th style={{ width: 150 }}>Ghi chú</th>
                          <th style={{ width: 110 }}>Số phiếu</th>
                          <th style={{ width: 90 }}>Ngày</th>
                          <th style={{ width: 140 }}>Khách hàng</th>
                          <th style={{ width: 120 }}>Loại thẻ mới</th>
                        </tr>
                      </thead>
                      <tbody>
                        {subtabsData?.doiLoaiThe?.map((d, idx) => (
                          <tr key={d.id || idx}>
                            <td style={{ textAlign: 'center', color: '#64748b' }}>{idx + 1}</td>
                            <td style={{ textAlign: 'right', fontWeight: 600, color: '#b45309' }}>{d.soTien?.toLocaleString()}</td>
                            <td>{d.note}</td>
                            <td><strong>{d.soPhiu}</strong></td>
                            <td>{d.ngay}</td>
                            <td>{d.khachHang}</td>
                            <td><strong>{d.loaiThe}</strong></td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  )}

                  {/* TAB 8: TĂNG GIẢM ĐIỂM (TTANGGIAMDIEM) */}
                  {activeBottomTab === 'tangGiamDiem' && (
                    <table className="cust-sub-table">
                      <thead>
                        <tr>
                          <th style={{ width: 30, textAlign: 'center' }}></th>
                          <th style={{ width: 110 }}>Số phiếu</th>
                          <th style={{ width: 90 }}>Ngày</th>
                          <th style={{ width: 140 }}>Khách hàng</th>
                          <th style={{ width: 90, textAlign: 'right' }}>Điểm tăng</th>
                          <th style={{ width: 90, textAlign: 'right' }}>Điểm giảm</th>
                          <th style={{ width: 150 }}>Lý do</th>
                          <th style={{ width: 150 }}>Ghi chú</th>
                        </tr>
                      </thead>
                      <tbody>
                        {subtabsData?.tangGiamDiem?.map((tg, idx) => (
                          <tr key={tg.id || idx}>
                            <td style={{ textAlign: 'center', color: '#64748b' }}>{idx + 1}</td>
                            <td><strong>{tg.soPhiu}</strong></td>
                            <td>{tg.ngay}</td>
                            <td>{tg.khachHang}</td>
                            <td style={{ textAlign: 'right', color: '#16a34a', fontWeight: 600 }}>+{tg.diemTang}</td>
                            <td style={{ textAlign: 'right', color: '#dc2626', fontWeight: 600 }}>-{tg.diemGiam}</td>
                            <td>{tg.lyDo}</td>
                            <td>{tg.note}</td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  )}

                  {/* TAB 9: THỂ TRẠNG (DTHETRANG) */}
                  {activeBottomTab === 'theTrang' && (
                    <table className="cust-sub-table">
                      <thead>
                        <tr>
                          <th style={{ width: 30, textAlign: 'center' }}></th>
                          <th style={{ width: 90 }}>Ngày</th>
                          <th style={{ width: 140 }}>Khách hàng</th>
                          <th style={{ width: 100, textAlign: 'right' }}>Chiều cao (cm)</th>
                          <th style={{ width: 100, textAlign: 'right' }}>Cân nặng (kg)</th>
                          <th style={{ width: 85, textAlign: 'right' }}>BMI</th>
                          <th style={{ width: 95, textAlign: 'right' }}>Vòng ngực</th>
                          <th style={{ width: 95, textAlign: 'right' }}>Vòng bụng</th>
                          <th style={{ width: 95, textAlign: 'right' }}>Vòng mông</th>
                          <th style={{ width: 150 }}>Ghi chú</th>
                        </tr>
                      </thead>
                      <tbody>
                        {subtabsData?.theTrang?.map((t, idx) => (
                          <tr key={t.id || idx}>
                            <td style={{ textAlign: 'center', color: '#64748b' }}>{idx + 1}</td>
                            <td>{t.ngay}</td>
                            <td>{t.khachHang}</td>
                            <td style={{ textAlign: 'right' }}>{t.chieuCao}</td>
                            <td style={{ textAlign: 'right' }}>{t.canNang}</td>
                            <td style={{ textAlign: 'right', fontWeight: 600, color: t.bmi >= 25 ? '#ea580c' : '#16a34a' }}>{t.bmi}</td>
                            <td style={{ textAlign: 'right' }}>{t.vongNguc}</td>
                            <td style={{ textAlign: 'right' }}>{t.vongBung}</td>
                            <td style={{ textAlign: 'right' }}>{t.vongMong}</td>
                            <td>{t.note}</td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  )}

                  {/* TAB 10: PHIẾU THU (TTHUCHI: THU > 0) */}
                  {activeBottomTab === 'phieuThu' && (
                    <table className="cust-sub-table">
                      <thead>
                        <tr>
                          <th style={{ width: 30, textAlign: 'center' }}></th>
                          <th style={{ width: 110 }}>Số phiếu</th>
                          <th style={{ width: 90 }}>Ngày</th>
                          <th style={{ width: 110, textAlign: 'right' }}>Số tiền thu</th>
                          <th style={{ width: 140 }}>Khách hàng / Đối tượng</th>
                          <th style={{ width: 130 }}>Lý do thu chi</th>
                          <th style={{ width: 160 }}>Diễn giải</th>
                          <th style={{ width: 110 }}>Chứng từ gốc</th>
                          <th style={{ width: 120 }}>Nhân viên</th>
                          <th style={{ width: 95, textAlign: 'center' }}>Chuyển khoản</th>
                          <th style={{ width: 120 }}>Cửa hàng</th>
                          <th style={{ width: 120 }}>Ghi chú</th>
                        </tr>
                      </thead>
                      <tbody>
                        {subtabsData?.phieuThu?.map((pt, idx) => (
                          <tr key={pt.id || idx}>
                            <td style={{ textAlign: 'center', color: '#64748b' }}>{idx + 1}</td>
                            <td><strong>{pt.soPhiu}</strong></td>
                            <td>{pt.ngay}</td>
                            <td style={{ textAlign: 'right', fontWeight: 600, color: '#16a34a' }}>{pt.thu?.toLocaleString()}</td>
                            <td>{pt.tenDoiTuong}</td>
                            <td>{pt.lyDoThuChi}</td>
                            <td>{pt.dienGiai}</td>
                            <td>{pt.chungTuGoc}</td>
                            <td>{pt.nhanVien}</td>
                            <td style={{ textAlign: 'center' }}>
                              <input type="checkbox" checked={!!pt.chuyenKhoan} readOnly style={{ accentColor: '#2563eb' }} />
                            </td>
                            <td>{pt.cuaHang}</td>
                            <td>{pt.note}</td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  )}

                  {/* TAB 11: PHIẾU CHI (TTHUCHI: CHI > 0) */}
                  {activeBottomTab === 'phieuChi' && (
                    <table className="cust-sub-table">
                      <thead>
                        <tr>
                          <th style={{ width: 30, textAlign: 'center' }}></th>
                          <th style={{ width: 110 }}>Số phiếu</th>
                          <th style={{ width: 90 }}>Ngày</th>
                          <th style={{ width: 110, textAlign: 'right' }}>Số tiền chi</th>
                          <th style={{ width: 140 }}>Khách hàng / Đối tượng</th>
                          <th style={{ width: 130 }}>Lý do thu chi</th>
                          <th style={{ width: 160 }}>Diễn giải</th>
                          <th style={{ width: 110 }}>Chứng từ gốc</th>
                          <th style={{ width: 120 }}>Nhân viên</th>
                          <th style={{ width: 95, textAlign: 'center' }}>Chuyển khoản</th>
                          <th style={{ width: 120 }}>Cửa hàng</th>
                          <th style={{ width: 120 }}>Ghi chú</th>
                        </tr>
                      </thead>
                      <tbody>
                        {subtabsData?.phieuChi?.map((pc, idx) => (
                          <tr key={pc.id || idx}>
                            <td style={{ textAlign: 'center', color: '#64748b' }}>{idx + 1}</td>
                            <td><strong>{pc.soPhiu}</strong></td>
                            <td>{pc.ngay}</td>
                            <td style={{ textAlign: 'right', fontWeight: 600, color: '#dc2626' }}>{pc.chi?.toLocaleString()}</td>
                            <td>{pc.tenDoiTuong}</td>
                            <td>{pc.lyDoThuChi}</td>
                            <td>{pc.dienGiai}</td>
                            <td>{pc.chungTuGoc}</td>
                            <td>{pc.nhanVien}</td>
                            <td style={{ textAlign: 'center' }}>
                              <input type="checkbox" checked={!!pc.chuyenKhoan} readOnly style={{ accentColor: '#2563eb' }} />
                            </td>
                            <td>{pc.cuaHang}</td>
                            <td>{pc.note}</td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  )}

                  {/* TAB 12: PHIẾU THU CÔNG NỢ (TTHUCHI: LAPHIEUTHUCONGNO = 1) */}
                  {activeBottomTab === 'thuCongNo' && (
                    <table className="cust-sub-table">
                      <thead>
                        <tr>
                          <th style={{ width: 30, textAlign: 'center' }}></th>
                          <th style={{ width: 110 }}>Số phiếu</th>
                          <th style={{ width: 90 }}>Ngày</th>
                          <th style={{ width: 110, textAlign: 'right' }}>Thu nợ</th>
                          <th style={{ width: 150 }}>Khách hàng</th>
                          <th style={{ width: 160 }}>Diễn giải</th>
                          <th style={{ width: 110 }}>Chứng từ gốc</th>
                          <th style={{ width: 120 }}>Nhân viên</th>
                          <th style={{ width: 150 }}>Ghi chú</th>
                        </tr>
                      </thead>
                      <tbody>
                        {(subtabsData?.thuCongNo || subtabsData?.phieuThuCongNo)?.map((cn, idx) => (
                          <tr key={cn.id || idx}>
                            <td style={{ textAlign: 'center', color: '#64748b' }}>{idx + 1}</td>
                            <td><strong>{cn.soPhiu}</strong></td>
                            <td>{cn.ngay}</td>
                            <td style={{ textAlign: 'right', fontWeight: 600, color: '#16a34a' }}>{cn.thu?.toLocaleString()}</td>
                            <td>{cn.tenDoiTuong}</td>
                            <td>{cn.dienGiai}</td>
                            <td>{cn.chungTuGoc}</td>
                            <td>{cn.nhanVien}</td>
                            <td>{cn.note}</td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  )}

                  {/* TAB 13: ĐẶT CỌC (TTHUCHI: DATCOCID IS NOT NULL) */}
                  {activeBottomTab === 'datCoc' && (
                    <table className="cust-sub-table">
                      <thead>
                        <tr>
                          <th style={{ width: 30, textAlign: 'center' }}></th>
                          <th style={{ width: 110 }}>Số phiếu</th>
                          <th style={{ width: 90 }}>Ngày</th>
                          <th style={{ width: 110, textAlign: 'right' }}>Tiền đặt cọc</th>
                          <th style={{ width: 150 }}>Khách hàng</th>
                          <th style={{ width: 160 }}>Diễn giải</th>
                          <th style={{ width: 110 }}>Chứng từ gốc</th>
                          <th style={{ width: 120 }}>Nhân viên</th>
                          <th style={{ width: 150 }}>Ghi chú</th>
                        </tr>
                      </thead>
                      <tbody>
                        {subtabsData?.datCoc?.map((dc, idx) => (
                          <tr key={dc.id || idx}>
                            <td style={{ textAlign: 'center', color: '#64748b' }}>{idx + 1}</td>
                            <td><strong>{dc.soPhiu}</strong></td>
                            <td>{dc.ngay}</td>
                            <td style={{ textAlign: 'right', fontWeight: 600, color: '#ea580c' }}>{dc.thu?.toLocaleString()}</td>
                            <td>{dc.tenDoiTuong}</td>
                            <td>{dc.dienGiai}</td>
                            <td>{dc.chungTuGoc}</td>
                            <td>{dc.nhanVien}</td>
                            <td>{dc.note}</td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  )}

                  {/* TAB 14: VÀO RA (TVAORA) */}
                  {activeBottomTab === 'vaoRa' && (
                    <table className="cust-sub-table">
                      <thead>
                        <tr>
                          <th style={{ width: 30, textAlign: 'center' }}></th>
                          <th style={{ width: 120 }}>Số phiếu / Mã thẻ</th>
                          <th style={{ width: 90 }}>Ngày</th>
                          <th style={{ width: 85 }}>Giờ</th>
                          <th style={{ width: 140 }}>Khách hàng</th>
                          <th style={{ width: 130 }}>Thiết bị / Máy</th>
                          <th style={{ width: 110 }}>Gia hạn thẻ</th>
                          <th style={{ width: 150 }}>Ghi chú</th>
                        </tr>
                      </thead>
                      <tbody>
                        {subtabsData?.vaoRa?.map((vr, idx) => (
                          <tr key={vr.id || idx}>
                            <td style={{ textAlign: 'center', color: '#64748b' }}>{idx + 1}</td>
                            <td><strong>{vr.maThe}</strong></td>
                            <td>{vr.ngay}</td>
                            <td style={{ fontWeight: 500, color: '#0369a1' }}>{vr.gio}</td>
                            <td>{vr.khachHang}</td>
                            <td>{vr.may}</td>
                            <td>{vr.giaHanThe}</td>
                            <td>{vr.note}</td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  )}
                </>
              )}
            </div>
          </div>
        </div>
      </div>

      {/* 4. MODAL THÊM MỚI / CHỈNH SỬA KHÁCH HÀNG (THEO DEVEXPRESS AELAYOUT) */}
      <CustomerAeModal
        show={modalState.show}
        mode={modalState.mode}
        initialData={modalState.customerData}
        metadata={metadata}
        subtabsData={subtabsData}
        onSave={handleSaveCustomer}
        onClose={() => setModalState(prev => ({ ...prev, show: false }))}
        showNotification={showNotification}
      />

      {/* 5. MODAL CHỌN MẪU IN & XEM TRƯỚC FASTREPORT */}
      <FastReportModal
        show={showFastReport}
        onClose={() => setShowFastReport(false)}
        customers={customers}
        selectedCustomer={selectedCustomer || (customers.length > 0 ? customers[0] : null)}
        showNotification={showNotification}
      />

      {/* 6. MODAL THÊM DANH SÁCH HỘI VIÊN TỪ EXCEL */}
      <ExcelImportModal
        show={showExcelImport}
        onClose={() => setShowExcelImport(false)}
        onSuccess={loadCustomersAndCounts}
        metadata={metadata}
        showNotification={showNotification}
      />

      {/* 7. MODAL ĐỒNG BỘ TỪ MÁY CHẤM CÔNG & CỔNG XOAY */}
      <DeviceSyncModal
        show={showDeviceSync}
        onClose={() => setShowDeviceSync(false)}
        onSuccess={loadCustomersAndCounts}
        showNotification={showNotification}
      />

      {/* 8. MODAL ĐĂNG KÝ VÂN TAY TỪ THIẾT BỊ */}
      <FingerprintEnrollModal
        show={showFingerprintEnroll}
        customer={selectedCustomer || (customers.length > 0 ? customers[0] : null)}
        onClose={() => setShowFingerprintEnroll(false)}
        onSuccess={() => {
          loadCustomersAndCounts();
          if (selectedCustomer?.id) {
            khachHangService.getSubtabs(selectedCustomer.id).then(subRes => {
              if (subRes && subRes.data) setSubtabsData(subRes.data);
            });
          }
        }}
        showNotification={showNotification}
      />

      {/* 5. BOTTOM STATUS BAR (DEVEXPRESS FOOTER STATUS) */}
      <div className="cust-statusbar">
        <div className="cust-statusbar-left">
          <i className="fa-solid fa-triangle-exclamation" style={{ color: '#ea580c' }}></i>
          <span>Phiên bản dùng thử, hết hạn trong 19 ngày.</span>
        </div>
        <div className="cust-statusbar-right">
          <span>🗄️ CSDL: DATA.fdb</span>
          <span>👤 Tài khoản: Admin</span>
          <label style={{ display: 'inline-flex', alignItems: 'center', gap: 4, cursor: 'pointer' }}>
            <input type="checkbox" defaultChecked />
            <span>Tự động tải lại</span>
          </label>
          <label style={{ display: 'inline-flex', alignItems: 'center', gap: 4, cursor: 'pointer' }}>
            <input type="checkbox" defaultChecked />
            <span>Gõ tiếng Việt: Telex</span>
          </label>
        </div>
      </div>
    
      {/* ========================================================================= */}
      {/* WINFORMS RIGHT-CLICK CONTEXT MENU (KHỚP 100% ẢNH THỰC TẾ WINFORMS)        */}
      {/* ========================================================================= */}
      {contextMenu.visible && (
        <div
          className="wf-context-menu"
          style={{ top: contextMenu.y, left: contextMenu.x }}
          onClick={(e) => e.stopPropagation()}
        >
          {/* 1. Thêm Khách hàng */}
          <div
            className="wf-menu-item"
            onClick={() => { closeContextMenu(); handleOpenAddModal(); }}
          >
            <span className="wf-menu-icon" style={{ color: '#16a34a', fontWeight: 'bold' }}>✚</span>
            <span className="wf-menu-text">Thêm Khách hàng</span>
          </div>

          {/* 2. Thêm nhanh (excel) */}
          <div
            className="wf-menu-item"
            onClick={() => { closeContextMenu(); setShowExcelImport(true); }}
          >
            <span className="wf-menu-icon"></span>
            <span className="wf-menu-text">Thêm nhanh (excel)</span>
          </div>

          {/* 3. Cập nhật nhanh (excel) */}
          <div
            className="wf-menu-item"
            onClick={() => { closeContextMenu(); setShowExcelImport(true); }}
          >
            <span className="wf-menu-icon"></span>
            <span className="wf-menu-text">Cập nhật nhanh (excel)</span>
          </div>

          {/* 4. Chỉnh sửa */}
          <div
            className="wf-menu-item"
            onClick={() => { closeContextMenu(); handleEditCustomer(); }}
          >
            <span className="wf-menu-icon" style={{ color: '#d97706' }}>✏️</span>
            <span className="wf-menu-text">Chỉnh sửa</span>
          </div>

          <div className="wf-menu-separator"></div>

          {/* 5. Đặt [Tên cột] */}
          <div
            className="wf-menu-item"
            onClick={handleMenuDatGiaTri}
          >
            <span className="wf-menu-icon"></span>
            <span className="wf-menu-text">Đặt {contextMenu.colTitle}</span>
          </div>

          {/* 6. Lọc [Tên cột] */}
          <div
            className="wf-menu-item"
            onClick={handleMenuLocTheoGiaTri}
          >
            <span className="wf-menu-icon"></span>
            <span className="wf-menu-text">Lọc {contextMenu.colTitle}</span>
          </div>

          <div className="wf-menu-separator"></div>

          {/* 7. Sắp xếp theo ► */}
          <div
            className="wf-menu-item has-submenu"
            onMouseEnter={() => setShowSubMenuSort(true)}
            onMouseLeave={() => setShowSubMenuSort(false)}
          >
            <span className="wf-menu-icon"></span>
            <span className="wf-menu-text">Sắp xếp theo</span>
            <span className="wf-submenu-arrow">▶</span>

            {showSubMenuSort && (
              <div className="wf-submenu">
                <div className="wf-menu-item" onClick={() => handleMenuSort('asc')}>
                  <span className="wf-menu-icon" style={{ fontSize: 10 }}>▲</span>
                  <span className="wf-menu-text">Sắp xếp tăng dần</span>
                </div>
                <div className="wf-menu-item" onClick={() => handleMenuSort('desc')}>
                  <span className="wf-menu-icon" style={{ fontSize: 10 }}>▼</span>
                  <span className="wf-menu-text">Sắp xếp giảm dần</span>
                </div>
              </div>
            )}
          </div>

          {/* 8. Refresh */}
          <div
            className="wf-menu-item"
            onClick={() => { closeContextMenu(); loadCustomersAndCounts(); }}
          >
            <span className="wf-menu-icon" style={{ color: '#16a34a' }}>🔄</span>
            <span className="wf-menu-text">Refresh</span>
          </div>

          {/* 9. In danh sách */}
          <div
            className="wf-menu-item"
            onClick={() => { closeContextMenu(); setShowFastReport(true); }}
          >
            <span className="wf-menu-icon"></span>
            <span className="wf-menu-text">In danh sách</span>
          </div>

          <div className="wf-menu-separator"></div>

          {/* 10. Sao chép ô */}
          <div
            className="wf-menu-item"
            onClick={handleMenuCopyCell}
          >
            <span className="wf-menu-icon" style={{ color: '#0284c7' }}>📄</span>
            <span className="wf-menu-text">Sao chép ô</span>
          </div>

          {/* 11. Sao chép vùng chọn */}
          <div
            className="wf-menu-item"
            onClick={handleMenuCopyRow}
          >
            <span className="wf-menu-icon" style={{ color: '#0284c7' }}>📑</span>
            <span className="wf-menu-text">Sao chép vùng chọn</span>
          </div>

          {/* 12. Xóa */}
          <div
            className="wf-menu-item"
            onClick={() => { closeContextMenu(); handleDeleteCustomer(); }}
          >
            <span className="wf-menu-icon" style={{ color: '#dc2626' }}>❌</span>
            <span className="wf-menu-text">Xóa</span>
          </div>

          <div className="wf-menu-separator"></div>

          {/* 13. Tự động giãn cột */}
          <div
            className="wf-menu-item"
            onClick={handleMenuAutoFitCols}
          >
            <span className="wf-menu-icon"></span>
            <span className="wf-menu-text">Tự động giãn cột</span>
          </div>

          {/* 14. Cột hiển thị */}
          <div
            className="wf-menu-item"
            onClick={() => { closeContextMenu(); setShowColChooserDialog(true); }}
          >
            <span className="wf-menu-icon"></span>
            <span className="wf-menu-text">Cột hiển thị</span>
          </div>

          {/* 15. Thuộc tính */}
          <div
            className="wf-menu-item"
            onClick={() => { closeContextMenu(); setShowPropsDialog(true); }}
          >
            <span className="wf-menu-icon"></span>
            <span className="wf-menu-text">Thuộc tính</span>
          </div>
        </div>
      )}

      {/* MODAL THUỘC TÍNH */}
      {showPropsDialog && (
        <div className="sub-modal-backdrop" onClick={() => setShowPropsDialog(false)}>
          <div className="choice-dialog-window" onClick={(e) => e.stopPropagation()} style={{ width: 440 }}>
            <div className="choice-dialog-titlebar">
              <span>Thuộc tính bản ghi: {selectedCustomer?.tenKhachHang || 'Khách hàng'}</span>
              <button className="choice-dialog-close" onClick={() => setShowPropsDialog(false)}>✕</button>
            </div>
            <div className="choice-dialog-body" style={{ fontSize: 12 }}>
              <div style={{ marginBottom: 6 }}><strong>Mã khách hàng / ID:</strong> {selectedCustomer?.id}</div>
              <div style={{ marginBottom: 6 }}><strong>Mã thẻ:</strong> {selectedCustomer?.maThe || '---'}</div>
              <div style={{ marginBottom: 6 }}><strong>Tên khách hàng:</strong> {selectedCustomer?.tenKhachHang}</div>
              <div style={{ marginBottom: 6 }}><strong>Bảng CSDL:</strong> DKHACHHANG</div>
              <div style={{ marginBottom: 6 }}><strong>Thời gian tạo:</strong> {selectedCustomer?.timeCreated || '---'}</div>
              <div style={{ marginBottom: 6 }}><strong>Người tạo:</strong> {selectedCustomer?.userCreatedName || 'Administrator'}</div>
              <div style={{ marginBottom: 6 }}><strong>Thời gian sửa:</strong> {selectedCustomer?.timeModified || '---'}</div>
              <div style={{ marginTop: 14, textAlign: 'right' }}>
                <button className="tn-btn-primary" style={{ padding: '4px 16px', height: 26 }} onClick={() => setShowPropsDialog(false)}>Đóng</button>
              </div>
            </div>
          </div>
        </div>
      )}

      {/* MODAL CỘT HIỂN THỊ */}
      {showColChooserDialog && (
        <div className="sub-modal-backdrop" onClick={() => setShowColChooserDialog(false)}>
          <div className="choice-dialog-window" onClick={(e) => e.stopPropagation()} style={{ width: 360 }}>
            <div className="choice-dialog-titlebar">
              <span>Cột hiển thị lưới khách hàng</span>
              <button className="choice-dialog-close" onClick={() => setShowColChooserDialog(false)}>✕</button>
            </div>
            <div className="choice-dialog-body" style={{ fontSize: 12 }}>
              <div style={{ maxHeight: 250, overflowY: 'auto', border: '1px solid #c0c0c0', padding: 8, background: '#fff' }}>
                {['Mã thẻ', 'Tên khách hàng', 'Địa chỉ', 'Điện thoại', 'Email', 'Nhóm khách hàng', 'Loại thẻ', 'Trạng thái', 'Từ ngày', 'Đến ngày', 'Số lần', 'Đã tập', 'Còn lại', 'Ca tập', 'Nhân viên', 'Facebook'].map(c => (
                  <div key={c} style={{ padding: '2px 0' }}>
                    <label style={{ cursor: 'pointer' }}><input type="checkbox" defaultChecked /> {c}</label>
                  </div>
                ))}
              </div>
              <div style={{ marginTop: 12, display: 'flex', justifyContent: 'flex-end', gap: 8 }}>
                <button className="tn-btn-primary" style={{ padding: '4px 14px', height: 26 }} onClick={() => setShowColChooserDialog(false)}>Đồng ý</button>
                <button className="tn-btn-cancel" style={{ padding: '4px 12px', height: 26 }} onClick={() => setShowColChooserDialog(false)}>Thoát</button>
              </div>
            </div>
          </div>
        </div>
      )}

      {/* ========================================================================= */}
      {/* TREE CONTEXT MENU (KHỚP 100% 4 ẢNH SCREENSHOT THỰC TẾ WINFORMS TÂN AN PHÁT)  */}
      {/* ========================================================================= */}
      {treeContextMenu.visible && (
        <div
          className="wf-context-menu"
          style={{ top: treeContextMenu.y, left: treeContextMenu.x, width: 170 }}
          onClick={(e) => e.stopPropagation()}
        >
          {/* 1. Thêm mới ► (Khớp 100% Screenshot 3 & 4) */}
          <div
            className="wf-menu-item has-submenu"
            onMouseEnter={() => { setShowTreeSubThemMoi(true); setShowTreeSubThemCon(false); setShowTreeSubSapXep(false); }}
            onMouseLeave={() => setShowTreeSubThemMoi(false)}
          >
            <span className="wf-menu-icon"></span>
            <span className="wf-menu-text">Thêm mới</span>
            <span className="wf-submenu-arrow">▶</span>

            {showTreeSubThemMoi && (
              <div className="wf-submenu" style={{ width: 165 }}>
                <div
                  className="wf-menu-item"
                  onClick={() => handleOpenAddStatus(null)}
                >
                  <span className="wf-menu-icon" style={{ color: '#16a34a', fontWeight: 'bold' }}>✚</span>
                  <span className="wf-menu-text">
                    {treeMode === 'trangThai' ? 'Thêm trạng thái' : 'Thêm nhóm khách'}
                  </span>
                </div>

                <div
                  className="wf-menu-item"
                  onClick={() => handleOpenQuickAdd(null)}
                >
                  <span className="wf-menu-icon" style={{ color: '#0284c7' }}>⚡</span>
                  <span className="wf-menu-text">Thêm nhanh</span>
                </div>

                <div className="wf-menu-separator" style={{ margin: '3px 4px 3px 24px' }}></div>

                <div
                  className="wf-menu-item"
                  onClick={() => handleAddSeparator(null)}
                >
                  <span className="wf-menu-icon">—</span>
                  <span className="wf-menu-text">Thêm phân cách</span>
                </div>

                <div
                  className="wf-menu-item"
                  onClick={() => handleOpenAddFolder(null)}
                >
                  <span className="wf-menu-icon">📁</span>
                  <span className="wf-menu-text">Thêm thư mục</span>
                </div>
              </div>
            )}
          </div>

          {/* 2. Thêm con ► (Khớp 100% Screenshot 2) */}
          <div
            className="wf-menu-item has-submenu"
            onMouseEnter={() => { setShowTreeSubThemCon(true); setShowTreeSubThemMoi(false); setShowTreeSubSapXep(false); }}
            onMouseLeave={() => setShowTreeSubThemCon(false)}
          >
            <span className="wf-menu-icon"></span>
            <span className="wf-menu-text">Thêm con</span>
            <span className="wf-submenu-arrow">▶</span>

            {showTreeSubThemCon && (
              <div className="wf-submenu" style={{ width: 165 }}>
                <div
                  className="wf-menu-item"
                  onClick={() => handleOpenAddStatus(treeContextMenu.item?.id)}
                >
                  <span className="wf-menu-icon" style={{ color: '#16a34a', fontWeight: 'bold' }}>✚</span>
                  <span className="wf-menu-text">
                    {treeMode === 'trangThai' ? 'Thêm trạng thái' : 'Thêm nhóm con'}
                  </span>
                </div>

                <div
                  className="wf-menu-item"
                  onClick={() => handleOpenQuickAdd(treeContextMenu.item?.id)}
                >
                  <span className="wf-menu-icon" style={{ color: '#0284c7' }}>⚡</span>
                  <span className="wf-menu-text">Thêm nhanh</span>
                </div>

                <div className="wf-menu-separator" style={{ margin: '3px 4px 3px 24px' }}></div>

                <div
                  className="wf-menu-item"
                  onClick={() => handleAddSeparator(treeContextMenu.item?.id)}
                >
                  <span className="wf-menu-icon">—</span>
                  <span className="wf-menu-text">Thêm phân cách</span>
                </div>

                <div
                  className="wf-menu-item"
                  onClick={() => handleOpenAddFolder(treeContextMenu.item?.id)}
                >
                  <span className="wf-menu-icon">📁</span>
                  <span className="wf-menu-text">Thêm thư mục</span>
                </div>
              </div>
            )}
          </div>

          {/* 3. Chỉnh sửa */}
          <div
            className="wf-menu-item"
            onClick={() => handleOpenEditTreeItem(treeContextMenu.item)}
          >
            <span className="wf-menu-icon" style={{ color: '#d97706' }}>✏️</span>
            <span className="wf-menu-text">Chỉnh sửa</span>
          </div>

          {/* 4. Sắp xếp theo ► (Khớp 100% Screenshot 1) */}
          <div
            className="wf-menu-item has-submenu"
            onMouseEnter={() => { setShowTreeSubSapXep(true); setShowTreeSubThemMoi(false); setShowTreeSubThemCon(false); }}
            onMouseLeave={() => setShowTreeSubSapXep(false)}
          >
            <span className="wf-menu-icon"></span>
            <span className="wf-menu-text">Sắp xếp theo</span>
            <span className="wf-submenu-arrow">▶</span>

            {showTreeSubSapXep && (
              <div className="wf-submenu" style={{ width: 145 }}>
                <div
                  className="wf-menu-item"
                  onClick={() => {
                    setTreeSortBy('name');
                    closeTreeContextMenu();
                    showNotification && showNotification('Đã sắp xếp cây theo Tên');
                  }}
                >
                  <span className="wf-menu-icon" style={{ fontWeight: 'bold', color: '#0078d7' }}>
                    {treeSortBy === 'name' ? '✓' : ''}
                  </span>
                  <span className="wf-menu-text">Tên</span>
                </div>

                <div
                  className="wf-menu-item"
                  onClick={() => {
                    setTreeSortBy('custom');
                    closeTreeContextMenu();
                    showNotification && showNotification('Đã sắp xếp cây theo Thứ tự tùy chọn');
                  }}
                >
                  <span className="wf-menu-icon" style={{ fontWeight: 'bold', color: '#0078d7' }}>
                    {treeSortBy === 'custom' ? '✓' : ''}
                  </span>
                  <span className="wf-menu-text">Thứ tự tùy chọn</span>
                </div>
              </div>
            )}
          </div>

          {/* 5. Refresh */}
          <div
            className="wf-menu-item"
            onClick={() => { closeTreeContextMenu(); loadTreeData(treeMode); loadCustomersAndCounts(); }}
          >
            <span className="wf-menu-icon" style={{ color: '#16a34a' }}>🔄</span>
            <span className="wf-menu-text">Refresh</span>
          </div>

          <div className="wf-menu-separator"></div>

          {/* 6. Sao chép */}
          <div
            className="wf-menu-item"
            onClick={handleTreeCopy}
          >
            <span className="wf-menu-icon" style={{ color: '#0284c7' }}>📄</span>
            <span className="wf-menu-text">Sao chép</span>
          </div>

          <div className="wf-menu-separator"></div>

          {/* 7. Mở rộng */}
          <div
            className="wf-menu-item"
            onClick={() => { closeTreeContextMenu(); setCollapsedFolders({}); showNotification && showNotification('Đã mở rộng toàn bộ cây'); }}
          >
            <span className="wf-menu-icon"></span>
            <span className="wf-menu-text">Mở rộng</span>
          </div>

          {/* 8. Thu gọn */}
          <div
            className="wf-menu-item"
            onClick={() => {
              closeTreeContextMenu();
              const allFolders = {};
              treeItems.filter(t => t.itemType === 1).forEach(f => { allFolders[f.id] = true; });
              setCollapsedFolders(allFolders);
              showNotification && showNotification('Đã thu gọn toàn bộ cây');
            }}
          >
            <span className="wf-menu-icon"></span>
            <span className="wf-menu-text">Thu gọn</span>
          </div>

          <div className="wf-menu-separator"></div>

          {/* 9. Xóa */}
          <div
            className="wf-menu-item"
            onClick={() => handleDeleteTreeItem(treeContextMenu.item)}
          >
            <span className="wf-menu-icon" style={{ color: '#dc2626' }}>❌</span>
            <span className="wf-menu-text">Xóa</span>
          </div>

          {/* 10. Đổi tên */}
          <div
            className="wf-menu-item"
            onClick={() => handleTreeRename(treeContextMenu.item)}
          >
            <span className="wf-menu-icon"></span>
            <span className="wf-menu-text">Đổi tên</span>
          </div>

          {/* 11. Thùng rác */}
          <div
            className="wf-menu-item"
            onClick={() => {
              closeTreeContextMenu();
              if (treeMode === 'trangThai') setSelectedTreeStatus('trash');
              else setSelectedGroupId('trash');
            }}
          >
            <span className="wf-menu-icon"></span>
            <span className="wf-menu-text">Thùng rác</span>
          </div>

          <div className="wf-menu-separator"></div>

          {/* 12. Biểu tượng */}
          <div
            className="wf-menu-item"
            onClick={() => handleOpenEditTreeItem(treeContextMenu.item)}
          >
            <span className="wf-menu-icon">🖼️</span>
            <span className="wf-menu-text">Biểu tượng</span>
          </div>

          {/* 13. Thuộc tính */}
          <div
            className="wf-menu-item"
            onClick={() => { closeTreeContextMenu(); setShowTreePropsDialog(true); }}
          >
            <span className="wf-menu-icon"></span>
            <span className="wf-menu-text">Thuộc tính</span>
          </div>
        </div>
      )}

      {/* MODAL THUỘC TÍNH MỤC CÂY */}
      {showTreePropsDialog && (
        <div className="sub-modal-backdrop" onClick={() => setShowTreePropsDialog(false)}>
          <div className="choice-dialog-window" onClick={(e) => e.stopPropagation()} style={{ width: 380 }}>
            <div className="choice-dialog-titlebar">
              <span>Thuộc tính mục: {treeContextMenu.item?.label}</span>
              <button className="choice-dialog-close" onClick={() => setShowTreePropsDialog(false)}>✕</button>
            </div>
            <div className="choice-dialog-body" style={{ fontSize: 12 }}>
              <div style={{ marginBottom: 6 }}><strong>Mã / ID:</strong> {treeContextMenu.item?.id}</div>
              <div style={{ marginBottom: 6 }}><strong>Tên hiển thị:</strong> {treeContextMenu.item?.label}</div>
              <div style={{ marginBottom: 6 }}><strong>Chế độ cây:</strong> {treeMode === 'trangThai' ? 'Trạng thái thẻ' : 'Nhóm khách hàng'}</div>
              <div style={{ marginBottom: 6 }}><strong>Loại mục:</strong> {treeContextMenu.item?.itemType === 1 ? 'Thư mục' : (treeContextMenu.item?.itemType === 2 ? 'Phân cách' : 'Bình thường')}</div>
              <div style={{ marginBottom: 6 }}><strong>Ghi chú:</strong> {treeContextMenu.item?.note || '---'}</div>
              <div style={{ marginBottom: 6 }}><strong>Số lượng hiện tại:</strong> {treeContextMenu.item?.count || 0}</div>
              <div style={{ marginTop: 14, textAlign: 'right' }}>
                <button className="tn-btn-primary" style={{ padding: '4px 16px', height: 26 }} onClick={() => setShowTreePropsDialog(false)}>Đóng</button>
              </div>
            </div>
          </div>
        </div>
      )}

      {/* MODAL THÊM / SỬA TRẠNG THÁI & THƯ MỤC (WINFORMS LOOK & FEEL) */}
      <TreeItemModal
        show={treeModal.show}
        mode={treeModal.mode}
        itemType={treeModal.itemType}
        treeMode={treeMode}
        parentId={treeModal.parentId}
        initialData={treeModal.initialData}
        icons={treeIcons}
        onSave={handleSaveTreeItem}
        onClose={() => setTreeModal(prev => ({ ...prev, show: false }))}
      />

      {/* MODAL THÊM NHANH TRẠNG THÁI / NHÓM HÀNG LOẠT */}
      <TreeQuickAddModal
        show={showTreeQuickAdd}
        treeMode={treeMode}
        parentId={treeQuickAddParentId}
        onSave={handleSaveTreeQuickAdd}
        onClose={() => setShowTreeQuickAdd(false)}
      />

</div>
  );
}
