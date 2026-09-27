import React, { useState, useEffect, useMemo } from 'react';
import { adminService } from '../../services/adminService';

// Bitmask flags for SGROUPROLE
const PERM_VIEW = 16;  // 0x10
const PERM_ADD = 32;   // 0x20
const PERM_EDIT = 64;  // 0x40
const PERM_DEL = 128;  // 0x80
const PERM_ALL = 240;  // 0xF0

export default function UserPermissionsView({ showNotification }) {
  const [loading, setLoading] = useState(false);
  const [groups, setGroups] = useState([]);
  const [users, setUsers] = useState([]);
  const [functions, setFunctions] = useState([]);
  const [reports, setReports] = useState([]);

  // Selected state
  const [selectedGroupId, setSelectedGroupId] = useState(null); // null = 'all'
  const [selectedUserId, setSelectedUserId] = useState(null);
  const [activeTab, setActiveTab] = useState('functions'); // 'functions' | 'reports'

  // Sub-tree filters
  const [selectedFuncCategory, setSelectedFuncCategory] = useState('Tất cả');
  const [funcSearchFilter, setFuncSearchFilter] = useState('');
  const [selectedReportCategory, setSelectedReportCategory] = useState('Tất cả');
  const [reportSearchFilter, setReportSearchFilter] = useState('');

  // Local state of permissions for the selected group
  // Map: functionId -> mode (int)
  const [groupRoleMap, setGroupRoleMap] = useState({});
  // Map: reportId -> mode (int: 0 = khóa, 30 = xem)
  const [reportRoleMap, setReportRoleMap] = useState({});

  // Unsaved changes tracking
  const [isSavingRoles, setIsSavingRoles] = useState(false);
  const [isSavingReports, setIsSavingReports] = useState(false);
  const [hasUnsavedRoles, setHasUnsavedRoles] = useState(false);
  const [hasUnsavedReports, setHasUnsavedReports] = useState(false);

  // Images from database (SIMAGE)
  const [dbImages, setDbImages] = useState([]);
  const [showImageDropdown, setShowImageDropdown] = useState(false);
  const [imageSearch, setImageSearch] = useState('');
  const [isUploadingImage, setIsUploadingImage] = useState(false);

  const imageMap = useMemo(() => {
    const map = {};
    dbImages.forEach((img) => {
      if (img.id) map[img.id] = img.data;
    });
    return map;
  }, [dbImages]);

  // Employees & Stores metadata
  const [employees, setEmployees] = useState([]);
  const [stores, setStores] = useState([]);

  // Modals
  const [groupModal, setGroupModal] = useState({ show: false, mode: 'create', data: { id: '', name: '', note: '', sImageId: '' } });
  const [userModal, setUserModal] = useState({
    show: false,
    mode: 'create',
    data: {
      id: '',
      username: '',
      fullName: '',
      email: '',
      password: '',
      groupId: '',
      employeeId: '',
      storeAccess: {}
    }
  });

  const [employeeModal, setEmployeeModal] = useState({
    show: false,
    data: {
      name: '',
      diaChi: '',
      dienThoai: '',
      note: '',
      sImageId: '',
      cachTinhLuong: 30, // 30 = Lương theo ca, 60 = Lương tháng theo ca, 0 = Lương tháng theo ngày
      luongCa: 0,
      luongThang: 0,
      nghiThu7: false,
      nghiChuNhat: false
    }
  });

  // Dropdown states
  const [showEmployeeDropdown, setShowEmployeeDropdown] = useState(false);
  const [showUserGroupDropdown, setShowUserGroupDropdown] = useState(false);
  const [showEmpImageDropdown, setShowEmpImageDropdown] = useState(false);
  const [selectedStoreRowId, setSelectedStoreRowId] = useState(null);

  // 1. Tải toàn bộ dữ liệu phân quyền từ CSDL
  const fetchSummary = async () => {
    setLoading(true);
    try {
      const [res, metaRes] = await Promise.all([
        adminService.getPermissionsSummary(),
        adminService.getUserMetadata()
      ]);

      if (res && res.data) {
        setGroups(res.data.groups || []);
        setUsers(res.data.users || []);
        setFunctions(res.data.functions || []);
        setReports(res.data.reports || []);
        setDbImages(res.data.images || []);

        // Khởi tạo map role ban đầu
        const gRoles = {};
        res.data.groupRoles?.forEach((gr) => {
          if (!gRoles[gr.groupId]) gRoles[gr.groupId] = {};
          gRoles[gr.groupId][gr.functionId] = gr.mode;
        });

        const rRoles = {};
        res.data.reportRoles?.forEach((rr) => {
          if (!rRoles[rr.groupId]) rRoles[rr.groupId] = {};
          rRoles[rr.groupId][rr.reportId] = rr.mode;
        });

        // Lưu raw map vào ref/state
        window.__allGroupRoles = gRoles;
        window.__allReportRoles = rRoles;

        // Nếu đã chọn nhóm, load map tương ứng
        if (selectedGroupId && selectedGroupId !== 'all') {
          setGroupRoleMap(gRoles[selectedGroupId] || {});
          setReportRoleMap(rRoles[selectedGroupId] || {});
        }
      }

      if (metaRes && metaRes.success) {
        setEmployees(metaRes.employees || []);
        setStores(metaRes.stores || []);
      }
    } catch {
      showNotification && showNotification('❌ Không thể tải dữ liệu phân quyền từ Firebird!');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchSummary();
  }, []);

  // Xử lý khi chọn nhóm người dùng
  const handleSelectGroup = (gId) => {
    if (hasUnsavedRoles || hasUnsavedReports) {
      if (!window.confirm('Bạn có thay đổi phân quyền chưa lưu! Bạn có muốn tiếp tục mà không lưu?')) {
        return;
      }
    }

    setSelectedGroupId(gId);
    setSelectedUserId(null);
    setHasUnsavedRoles(false);
    setHasUnsavedReports(false);

    if (gId && gId !== 'all') {
      const gRoles = window.__allGroupRoles?.[gId] || {};
      const rRoles = window.__allReportRoles?.[gId] || {};
      setGroupRoleMap({ ...gRoles });
      setReportRoleMap({ ...rRoles });
    } else {
      setGroupRoleMap({});
      setReportRoleMap({});
    }
  };

  // Danh sách tài khoản thuộc nhóm được chọn (hoặc tất cả)
  const displayedUsers = useMemo(() => {
    if (!selectedGroupId || selectedGroupId === 'all') return users;
    return users.filter((u) => u.groupId === selectedGroupId);
  }, [users, selectedGroupId]);

  // Danh mục nhóm chức năng
  const functionCategories = useMemo(() => {
    const set = new Set();
    functions.forEach((f) => {
      if (f.groupName) set.add(f.groupName);
    });
    return ['Tất cả', ...Array.from(set).sort()];
  }, [functions]);

  // Lọc danh sách chức năng
  const displayedFunctions = useMemo(() => {
    return functions.filter((f) => {
      if (selectedFuncCategory !== 'Tất cả' && f.groupName !== selectedFuncCategory) return false;
      if (funcSearchFilter.trim()) {
        const term = funcSearchFilter.toLowerCase();
        return f.name.toLowerCase().includes(term) || (f.groupName && f.groupName.toLowerCase().includes(term));
      }
      return true;
    });
  }, [functions, selectedFuncCategory, funcSearchFilter]);

  // Danh mục nhóm báo cáo (thư mục cha)
  const reportCategories = useMemo(() => {
    // Tìm các báo cáo cha hoặc nhóm
    const parents = reports.filter((r) => r.itemType === 1 || !r.parentId);
    const names = parents.map((p) => p.name).filter(Boolean);
    return ['Tất cả', ...Array.from(new Set(names))];
  }, [reports]);

  // Lọc danh sách báo cáo
  const displayedReports = useMemo(() => {
    // Chỉ hiển thị các báo cáo chi tiết (không phải thư mục chỉ mục)
    return reports.filter((r) => {
      if (r.itemType === 1) return false; // Thư mục cha
      if (selectedReportCategory !== 'Tất cả') {
        // Tìm parent của báo cáo này
        const parent = reports.find((p) => p.id === r.parentId);
        if (!parent || parent.name !== selectedReportCategory) return false;
      }
      if (reportSearchFilter.trim()) {
        const term = reportSearchFilter.toLowerCase();
        return r.name.toLowerCase().includes(term);
      }
      return true;
    });
  }, [reports, selectedReportCategory, reportSearchFilter]);

  // ==========================================
  // LOGIC PHÂN QUYỀN CHỨC NĂNG (TAB 1)
  // ==========================================
  const getFuncMode = (funcId) => {
    return groupRoleMap[funcId] ?? 0;
  };

  const handleToggleAction = (func, actionBit) => {
    if (!selectedGroupId || selectedGroupId === 'all') return;
    const currentMode = getFuncMode(func.id);
    let newMode;

    if (actionBit === 0) {
      // Nhấn 'Khóa' -> xóa toàn bộ quyền
      newMode = 0;
    } else if (actionBit === PERM_ALL) {
      // Nhấn 'Tất cả'
      const isAlreadyAll = (currentMode & PERM_ALL) === PERM_ALL;
      newMode = isAlreadyAll ? 0 : (func.isViewOnly ? PERM_VIEW : PERM_ALL);
    } else {
      // Toggle từng bit riêng lẻ
      const hasBit = (currentMode & actionBit) === actionBit;
      newMode = hasBit ? (currentMode & ~actionBit) : (currentMode | actionBit);
    }

    setGroupRoleMap((prev) => ({
      ...prev,
      [func.id]: newMode
    }));
    setHasUnsavedRoles(true);
  };

  const handleLockAllFunctions = () => {
    if (!selectedGroupId || selectedGroupId === 'all') return;
    const nextMap = { ...groupRoleMap };
    displayedFunctions.forEach((f) => {
      nextMap[f.id] = 0;
    });
    setGroupRoleMap(nextMap);
    setHasUnsavedRoles(true);
  };

  const handleAllowAllFunctions = () => {
    if (!selectedGroupId || selectedGroupId === 'all') return;
    const nextMap = { ...groupRoleMap };
    displayedFunctions.forEach((f) => {
      nextMap[f.id] = f.isViewOnly ? PERM_VIEW : PERM_ALL;
    });
    setGroupRoleMap(nextMap);
    setHasUnsavedRoles(true);
  };

  const handleSaveRoles = async () => {
    if (!selectedGroupId || selectedGroupId === 'all') return;
    setIsSavingRoles(true);
    try {
      const rolesPayload = functions.map((f) => ({
        functionId: f.id,
        mode: groupRoleMap[f.id] ?? 0
      }));

      const res = await adminService.saveGroupRoles(selectedGroupId, rolesPayload);
      if (res && res.success) {
        showNotification && showNotification(`✅ ${res.message}`);
        setHasUnsavedRoles(false);
        if (!window.__allGroupRoles) window.__allGroupRoles = {};
        window.__allGroupRoles[selectedGroupId] = { ...groupRoleMap };
      }
    } catch (err) {
      showNotification && showNotification(err.response?.data?.message || '❌ Lỗi khi lưu quyền chức năng!');
    } finally {
      setIsSavingRoles(false);
    }
  };

  // ==========================================
  // LOGIC PHÂN QUYỀN BÁO CÁO (TAB 2)
  // ==========================================
  const getReportMode = (repId) => {
    return reportRoleMap[repId] ?? 0;
  };

  const handleToggleReportMode = (repId, mode) => {
    if (!selectedGroupId || selectedGroupId === 'all') return;
    setReportRoleMap((prev) => ({
      ...prev,
      [repId]: mode
    }));
    setHasUnsavedReports(true);
  };

  const handleLockAllReports = () => {
    if (!selectedGroupId || selectedGroupId === 'all') return;
    const nextMap = { ...reportRoleMap };
    displayedReports.forEach((r) => {
      nextMap[r.id] = 0;
    });
    setReportRoleMap(nextMap);
    setHasUnsavedReports(true);
  };

  const handleAllowAllReports = () => {
    if (!selectedGroupId || selectedGroupId === 'all') return;
    const nextMap = { ...reportRoleMap };
    displayedReports.forEach((r) => {
      nextMap[r.id] = 30;
    });
    setReportRoleMap(nextMap);
    setHasUnsavedReports(true);
  };

  const handleSaveReportRoles = async () => {
    if (!selectedGroupId || selectedGroupId === 'all') return;
    setIsSavingReports(true);
    try {
      const repPayload = reports.map((r) => ({
        reportId: r.id,
        mode: reportRoleMap[r.id] ?? 0
      }));

      const res = await adminService.saveReportRoles(selectedGroupId, repPayload);
      if (res && res.success) {
        showNotification && showNotification(`✅ ${res.message}`);
        setHasUnsavedReports(false);
        if (!window.__allReportRoles) window.__allReportRoles = {};
        window.__allReportRoles[selectedGroupId] = { ...reportRoleMap };
      }
    } catch (err) {
      showNotification && showNotification(err.response?.data?.message || '❌ Lỗi khi lưu quyền xem báo cáo!');
    } finally {
      setIsSavingReports(false);
    }
  };

  // ==========================================
  // QUẢN LÝ NHÓM (THÊM, SỬA, XÓA)
  // ==========================================
  const handleOpenAddGroup = () => {
    const defaultImgId = dbImages.find(img => img.name?.includes('Sao') || img.name?.includes('Người'))?.id || dbImages[0]?.id || '';
    setGroupModal({ show: true, mode: 'create', data: { id: '', name: '', note: '', sImageId: defaultImgId } });
    setShowImageDropdown(false);
  };

  const handleOpenEditGroup = () => {
    if (!selectedGroupId || selectedGroupId === 'all') {
      showNotification && showNotification('⚠️ Vui lòng chọn một nhóm để sửa!');
      return;
    }
    const grp = groups.find((g) => g.id === selectedGroupId);
    if (!grp) return;
    setGroupModal({ show: true, mode: 'edit', data: { id: grp.id, name: grp.name, note: grp.note || '', sImageId: grp.sImageId || '' } });
    setShowImageDropdown(false);
  };

  const handleUploadGroupImage = async (e) => {
    const file = e.target.files?.[0];
    if (!file) return;
    setIsUploadingImage(true);
    try {
      const reader = new FileReader();
      reader.onload = async (evt) => {
        try {
          const base64 = evt.target.result;
          const res = await adminService.uploadImage(file.name.replace(/\.[^/.]+$/, ""), base64);
          if (res && res.success && res.image) {
            setDbImages((prev) => [res.image, ...prev]);
            setGroupModal((prev) => ({
              ...prev,
              data: { ...prev.data, sImageId: res.image.id }
            }));
            showNotification && showNotification('✅ Đã tải ảnh lên và lưu vào CSDL!');
            setShowImageDropdown(false);
          }
        } catch {
          showNotification && showNotification('❌ Lỗi khi tải ảnh lên máy chủ!');
        } finally {
          setIsUploadingImage(false);
        }
      };
      reader.readAsDataURL(file);
    } catch {
      setIsUploadingImage(false);
    }
  };

  const handleDeleteGroup = async () => {
    if (!selectedGroupId || selectedGroupId === 'all') {
      showNotification && showNotification('⚠️ Vui lòng chọn một nhóm để xóa!');
      return;
    }
    const grp = groups.find((g) => g.id === selectedGroupId);
    if (!grp) return;

    if (!window.confirm(`Bạn có chắc chắn muốn xóa nhóm người dùng "${grp.name}"?`)) return;

    try {
      const res = await adminService.deleteGroup(grp.id);
      if (res && res.success) {
        showNotification && showNotification(`✅ ${res.message}`);
        setSelectedGroupId(null);
        fetchSummary();
      }
    } catch (err) {
      showNotification && showNotification(err.response?.data?.message || '❌ Lỗi xóa nhóm!');
    }
  };

  const handleSaveGroupModal = async (e) => {
    e.preventDefault();
    if (!groupModal.data.name.trim()) {
      showNotification && showNotification('⚠️ Tên nhóm không được để trống!');
      return;
    }

    try {
      if (groupModal.mode === 'create') {
        const res = await adminService.createGroup(groupModal.data);
        if (res && res.success) {
          showNotification && showNotification(`✅ ${res.message}`);
          setGroupModal({ show: false, mode: 'create', data: { id: '', name: '', note: '', sImageId: '' } });
          if (res.groupId) {
            handleSelectGroup(res.groupId);
            setUserModal((prev) => ({
              ...prev,
              data: { ...prev.data, groupId: res.groupId }
            }));
          }
        }
      } else {
        const res = await adminService.updateGroup(groupModal.data.id, groupModal.data);
        if (res && res.success) {
          showNotification && showNotification(`✅ ${res.message}`);
          setGroupModal({ show: false, mode: 'edit', data: { id: '', name: '', note: '', sImageId: '' } });
          fetchSummary();
        }
      }
    } catch (err) {
      showNotification && showNotification(err.response?.data?.message || '❌ Lỗi xử lý nhóm!');
    }
  };

  // ==========================================
  // QUẢN LÝ TÀI KHOẢN (THÊM, SỬA, XÓA) & NHÂN VIÊN
  // ==========================================
  const handleOpenAddUser = () => {
    const defaultStores = {};
    stores.forEach((s) => {
      defaultStores[s.id] = true;
    });

    setUserModal({
      show: true,
      mode: 'create',
      data: {
        id: '',
        username: '',
        fullName: '',
        email: '',
        password: '',
        employeeId: '',
        groupId: selectedGroupId && selectedGroupId !== 'all' ? selectedGroupId : (groups[0]?.id || ''),
        storeAccess: defaultStores
      }
    });
    setSelectedStoreRowId(stores[0]?.id || null);
    setShowEmployeeDropdown(false);
    setShowUserGroupDropdown(false);
  };

  const handleOpenEditUser = async () => {
    if (!selectedUserId) {
      showNotification && showNotification('⚠️ Vui lòng chọn một tài khoản để sửa!');
      return;
    }
    const usr = users.find((u) => u.id === selectedUserId);
    if (!usr) return;

    try {
      const detailRes = await adminService.getUserDetail(usr.id);
      const storeMap = {};
      if (detailRes && detailRes.storeAccess) {
        detailRes.storeAccess.forEach((sa) => {
          storeMap[sa.id] = sa.truyCap;
        });
      } else {
        stores.forEach((s) => { storeMap[s.id] = true; });
      }

      setUserModal({
        show: true,
        mode: 'edit',
        data: {
          id: usr.id,
          username: usr.username,
          fullName: usr.fullName || '',
          email: usr.email || '',
          password: '',
          employeeId: detailRes?.user?.employeeId || '',
          groupId: usr.groupId || (groups[0]?.id || ''),
          storeAccess: storeMap
        }
      });
      setSelectedStoreRowId(stores[0]?.id || null);
      setShowEmployeeDropdown(false);
      setShowUserGroupDropdown(false);
    } catch {
      showNotification && showNotification('❌ Lỗi khi tải chi tiết tài khoản!');
    }
  };

  const handleDeleteUser = async () => {
    if (!selectedUserId) {
      showNotification && showNotification('⚠️ Vui lòng chọn một tài khoản để xóa!');
      return;
    }
    const usr = users.find((u) => u.id === selectedUserId);
    if (!usr) return;

    if (!window.confirm(`Bạn có chắc chắn muốn xóa tài khoản "${usr.username}"?`)) return;

    try {
      const res = await adminService.deleteUser(usr.id);
      if (res && res.success) {
        showNotification && showNotification(`✅ ${res.message}`);
        setSelectedUserId(null);
        fetchSummary();
      }
    } catch (err) {
      showNotification && showNotification(err.response?.data?.message || '❌ Lỗi xóa tài khoản!');
    }
  };

  const executeSaveUser = async (closeAfter = true, newAfter = false) => {
    if (!userModal.data.username?.trim()) {
      showNotification && showNotification('⚠️ Tên tài khoản không được để trống!');
      return false;
    }

    const checkedStoreIds = Object.keys(userModal.data.storeAccess || {}).filter(
      (k) => userModal.data.storeAccess[k]
    );

    if (checkedStoreIds.length === 0) {
      if (!window.confirm('Bạn chưa phân quyền cho người dùng vào cửa hàng nào, bạn có muốn tiếp tục không?')) {
        return false;
      }
    }

    try {
      const payload = {
        username: userModal.data.username.trim(),
        fullName: userModal.data.fullName?.trim() || '',
        email: userModal.data.email?.trim() || '',
        password: userModal.data.password?.trim() || '',
        groupId: userModal.data.groupId || '',
        employeeId: userModal.data.employeeId || '',
        storeIds: checkedStoreIds,
        isAdmin: false
      };

      let res;
      if (userModal.mode === 'create') {
        res = await adminService.createUser(payload);
      } else {
        res = await adminService.updateUser(userModal.data.id, payload);
      }

      if (res && res.success) {
        showNotification && showNotification(`✅ ${res.message}`);
        await fetchSummary();

        if (newAfter) {
          handleOpenAddUser();
        } else if (closeAfter) {
          setUserModal((prev) => ({ ...prev, show: false }));
        } else {
          if (res.id) {
            setUserModal((prev) => ({
              ...prev,
              mode: 'edit',
              data: { ...prev.data, id: res.id, password: '' }
            }));
          }
        }
        return true;
      }
      return false;
    } catch (err) {
      showNotification && showNotification(err.response?.data?.message || '❌ Lỗi lưu tài khoản!');
      return false;
    }
  };

  const executeSaveEmployee = async (closeAfter = true, newAfter = false) => {
    if (!employeeModal.data.name?.trim()) {
      showNotification && showNotification('⚠️ Tên nhân viên không được để trống!');
      return;
    }

    try {
      const res = await adminService.createEmployee(employeeModal.data);
      if (res && res.success) {
        showNotification && showNotification(`✅ ${res.message}`);

        const metaRes = await adminService.getUserMetadata();
        if (metaRes && metaRes.employees) {
          setEmployees(metaRes.employees);
        }

        if (res.employee) {
          setUserModal((prev) => ({
            ...prev,
            data: {
              ...prev.data,
              employeeId: res.employee.id,
              fullName: res.employee.name
            }
          }));
        }

        if (newAfter) {
          setEmployeeModal({
            show: true,
            data: {
              name: '',
              diaChi: '',
              dienThoai: '',
              note: '',
              sImageId: '',
              cachTinhLuong: 30,
              luongCa: 0,
              luongThang: 0,
              nghiThu7: false,
              nghiChuNhat: false
            }
          });
        } else if (closeAfter) {
          setEmployeeModal((prev) => ({ ...prev, show: false }));
        }
      }
    } catch (err) {
      showNotification && showNotification(err.response?.data?.message || '❌ Lỗi thêm nhân viên!');
    }
  };

  const selectedGroup = groups.find((g) => g.id === selectedGroupId);

  return (
    <div className="perm-window-container">
      {/* 1. HEADER WINDOW CHUẨN WINFORM */}
      <div className="perm-window-header">
        <div className="perm-header-left">
          <div className="perm-header-icon">
            <i className="fa-solid fa-users-line"></i>
          </div>
          <div className="perm-header-titles">
            <h2 className="perm-title-main">Quản lý tài khoản người sử dụng và phân quyền</h2>
          </div>
        </div>
      </div>

      {loading ? (
        <div className="perm-loading-state">
          <i className="fa-solid fa-spinner fa-spin"></i>
          <span>Đang nạp dữ liệu phân quyền từ SGROUPUSER, SUSER, SFUNCTION, SREPORT...</span>
        </div>
      ) : (
        <div className="perm-window-body">
          {/* 2. CỘT TRÁI (LEFT PANEL): NHÓM NGƯỜI DÙNG & TÀI KHOẢN */}
          <div className="perm-left-pane">
            {/* 2A. KHUNG TRÊN: NHÓM NGƯỜI DÙNG */}
            <div className="perm-group-section">
              <div className="perm-pane-toolbar">
                <button type="button" className="perm-btn-tool perm-btn-add" onClick={handleOpenAddGroup} title="Thêm nhóm mới">
                  <i className="fa-solid fa-plus"></i> Thêm nhóm
                </button>
                <button type="button" className="perm-btn-tool" onClick={handleOpenEditGroup} title="Sửa tên nhóm">
                  <i className="fa-solid fa-pen"></i> Sửa
                </button>
                <button type="button" className="perm-btn-tool perm-btn-del" onClick={handleDeleteGroup} title="Xóa nhóm">
                  <i className="fa-solid fa-xmark"></i> Xóa
                </button>
              </div>

              <div className="perm-group-list">
                <div
                  className={`perm-group-item ${selectedGroupId === 'all' || selectedGroupId === null ? 'active' : ''}`}
                  onClick={() => handleSelectGroup('all')}
                >
                  <i className="fa-solid fa-earth-americas perm-item-icon icon-globe"></i>
                  <span>Tất cả</span>
                </div>

                {groups.map((g) => {
                  const isActive = g.id === selectedGroupId;
                  const isManager = g.name.toLowerCase().includes('quản lý');
                  const hasCustomImg = g.sImageId && imageMap[g.sImageId];
                  return (
                    <div
                      key={g.id}
                      className={`perm-group-item ${isActive ? 'active' : ''}`}
                      onClick={() => handleSelectGroup(g.id)}
                    >
                      {hasCustomImg ? (
                        <img
                          src={`data:image/png;base64,${imageMap[g.sImageId]}`}
                          alt=""
                          className="perm-group-img-icon"
                        />
                      ) : (
                        <i className={`fa-solid ${isManager ? 'fa-user-tie icon-manager' : 'fa-star icon-reception'} perm-item-icon`}></i>
                      )}
                      <span>{g.name}</span>
                    </div>
                  );
                })}
              </div>
            </div>

            {/* 2B. KHUNG DƯỚI: TÀI KHOẢN NGƯỜI DÙNG */}
            <div className="perm-user-section">
              <div className="perm-pane-toolbar">
                <button type="button" className="perm-btn-tool perm-btn-add" onClick={handleOpenAddUser} title="Thêm tài khoản mới">
                  <i className="fa-solid fa-plus"></i> Thêm tài khoản
                </button>
                <button type="button" className="perm-btn-tool" onClick={handleOpenEditUser} title="Sửa thông tin tài khoản">
                  <i className="fa-solid fa-pen"></i> Sửa
                </button>
                <button type="button" className="perm-btn-tool perm-btn-del" onClick={handleDeleteUser} title="Xóa tài khoản">
                  <i className="fa-solid fa-xmark"></i> Xóa
                </button>
              </div>

              <div className="perm-user-table-wrap">
                <table className="perm-user-table">
                  <thead>
                    <tr>
                      <th>Tài khoản</th>
                    </tr>
                  </thead>
                  <tbody>
                    {displayedUsers.length > 0 ? (
                      displayedUsers.map((u) => {
                        const isSelected = u.id === selectedUserId;
                        return (
                          <tr
                            key={u.id}
                            className={isSelected ? 'row-selected' : ''}
                            onClick={() => setSelectedUserId(u.id)}
                          >
                            <td>
                              <div className="perm-user-name-cell">
                                <i className="fa-regular fa-user perm-user-avatar-icon"></i>
                                <span>{u.username}</span>
                              </div>
                            </td>
                          </tr>
                        );
                      })
                    ) : (
                      <tr>
                        <td className="perm-empty-users">(Không có tài khoản)</td>
                      </tr>
                    )}
                  </tbody>
                </table>
              </div>
            </div>
          </div>

          {/* 3. CỘT PHẢI (RIGHT PANEL): PHÂN QUYỀN HOẶC THÔNG BÁO HƯỚNG DẪN */}
          <div className="perm-right-pane">
            {!selectedGroupId || selectedGroupId === 'all' ? (
              <div className="perm-empty-selection-placeholder">
                <h3>MỜI BẠN CHỌN NHÓM NGƯỜI DÙNG BÊN PHÍA TRÁI ĐỂ PHÂN QUYỀN</h3>
              </div>
            ) : (
              <div className="perm-role-editor-container">
                {/* 3A. TABS: QUYỀN SỬ DỤNG & QUYỀN XEM BÁO CÁO */}
                <div className="perm-tabs-header">
                  <button
                    type="button"
                    className={`perm-tab-btn ${activeTab === 'functions' ? 'active' : ''}`}
                    onClick={() => setActiveTab('functions')}
                  >
                    Quyền sử dụng
                  </button>
                  <button
                    type="button"
                    className={`perm-tab-btn ${activeTab === 'reports' ? 'active' : ''}`}
                    onClick={() => setActiveTab('reports')}
                  >
                    Quyền xem báo cáo
                  </button>
                </div>

                {/* 3B. NỘI DUNG TAB 1: QUYỀN SỬ DỤNG (SFUNCTION & SGROUPROLE) */}
                {activeTab === 'functions' && (
                  <div className="perm-tab-content-split">
                    {/* Cây phân loại chức năng bên trái */}
                    <div className="perm-sub-tree-pane">
                      <div
                        className={`perm-tree-item ${selectedFuncCategory === 'Tất cả' ? 'active' : ''}`}
                        onClick={() => setSelectedFuncCategory('Tất cả')}
                      >
                        <i className="fa-solid fa-earth-americas perm-tree-icon icon-globe"></i>
                        <span>Tất cả</span>
                      </div>
                      {functionCategories
                        .filter((c) => c !== 'Tất cả')
                        .map((cat) => (
                          <div
                            key={cat}
                            className={`perm-tree-item ${selectedFuncCategory === cat ? 'active' : ''}`}
                            onClick={() => setSelectedFuncCategory(cat)}
                          >
                            <i className="fa-regular fa-folder perm-tree-icon icon-folder"></i>
                            <span>{cat}</span>
                          </div>
                        ))}
                    </div>

                    {/* Bảng phân quyền chi tiết bên phải */}
                    <div className="perm-grid-pane">
                      {/* Thanh Lọc & Các nút chức năng */}
                      <div className="perm-grid-toolbar">
                        <div className="perm-filter-box">
                          <label className="perm-filter-label">Lọc:</label>
                          <input
                            type="text"
                            className="perm-filter-input"
                            value={funcSearchFilter}
                            onChange={(e) => setFuncSearchFilter(e.target.value)}
                            placeholder="Nhập tên chức năng..."
                          />
                        </div>

                        <div className="perm-quick-actions">
                          <button
                            type="button"
                            className="perm-btn-batch"
                            onClick={handleLockAllFunctions}
                            title="Khóa toàn bộ quyền của các chức năng hiển thị"
                          >
                            <i className="fa-regular fa-square"></i> Khóa tất cả
                          </button>
                          <button
                            type="button"
                            className="perm-btn-batch"
                            onClick={handleAllowAllFunctions}
                            title="Cho phép toàn bộ quyền của các chức năng hiển thị"
                          >
                            <i className="fa-regular fa-square-check"></i> Cho phép tất cả
                          </button>
                          <button
                            type="button"
                            className="perm-btn-save-grid"
                            onClick={handleSaveRoles}
                            disabled={isSavingRoles}
                          >
                            <i className={`fa-solid ${isSavingRoles ? 'fa-spinner fa-spin' : 'fa-floppy-disk'}`}></i>
                            <span>{isSavingRoles ? 'Đang lưu...' : 'Ghi dữ liệu'}</span>
                          </button>
                        </div>
                      </div>

                      {/* Bảng dữ liệu quyền chức năng */}
                      <div className="perm-data-table-wrap">
                        <table className="perm-role-table">
                          <thead>
                            <tr>
                              <th className="th-func-name">Chức năng</th>
                              <th className="th-action-col">Khóa</th>
                              <th className="th-action-col">Xem</th>
                              <th className="th-action-col">Thêm</th>
                              <th className="th-action-col">Sửa</th>
                              <th className="th-action-col">Xóa</th>
                              <th className="th-action-col">Tất cả</th>
                            </tr>
                          </thead>
                          <tbody>
                            {displayedFunctions.map((f) => {
                              const mode = getFuncMode(f.id);
                              const isLocked = mode === 0;
                              const canView = (mode & PERM_VIEW) === PERM_VIEW;
                              const canAdd = (mode & PERM_ADD) === PERM_ADD;
                              const canEdit = (mode & PERM_EDIT) === PERM_EDIT;
                              const canDel = (mode & PERM_DEL) === PERM_DEL;
                              const isAll = (mode & PERM_ALL) === PERM_ALL || (f.isViewOnly && canView);

                              return (
                                <tr key={f.id} className={isLocked ? 'row-locked' : 'row-allowed'}>
                                  <td className="td-func-name">{f.name}</td>
                                  <td className="td-action-check">
                                    <input
                                      type="checkbox"
                                      className="perm-checkbox perm-chk-lock"
                                      checked={isLocked}
                                      onChange={() => handleToggleAction(f, 0)}
                                    />
                                  </td>
                                  <td className="td-action-check">
                                    <input
                                      type="checkbox"
                                      className="perm-checkbox"
                                      checked={canView}
                                      onChange={() => handleToggleAction(f, PERM_VIEW)}
                                    />
                                  </td>
                                  <td className="td-action-check">
                                    {!f.isViewOnly && (
                                      <input
                                        type="checkbox"
                                        className="perm-checkbox"
                                        checked={canAdd}
                                        onChange={() => handleToggleAction(f, PERM_ADD)}
                                      />
                                    )}
                                  </td>
                                  <td className="td-action-check">
                                    {!f.isViewOnly && (
                                      <input
                                        type="checkbox"
                                        className="perm-checkbox"
                                        checked={canEdit}
                                        onChange={() => handleToggleAction(f, PERM_EDIT)}
                                      />
                                    )}
                                  </td>
                                  <td className="td-action-check">
                                    {!f.isViewOnly && (
                                      <input
                                        type="checkbox"
                                        className="perm-checkbox"
                                        checked={canDel}
                                        onChange={() => handleToggleAction(f, PERM_DEL)}
                                      />
                                    )}
                                  </td>
                                  <td className="td-action-check">
                                    <input
                                      type="checkbox"
                                      className="perm-checkbox perm-chk-all"
                                      checked={isAll}
                                      onChange={() => handleToggleAction(f, PERM_ALL)}
                                    />
                                  </td>
                                </tr>
                              );
                            })}
                          </tbody>
                        </table>
                      </div>
                    </div>
                  </div>
                )}

                {/* 3C. NỘI DUNG TAB 2: QUYỀN XEM BÁO CÁO (SREPORT & SREPORTROLE) */}
                {activeTab === 'reports' && (
                  <div className="perm-tab-content-split">
                    {/* Cây phân loại báo cáo bên trái */}
                    <div className="perm-sub-tree-pane">
                      <div
                        className={`perm-tree-item ${selectedReportCategory === 'Tất cả' ? 'active' : ''}`}
                        onClick={() => setSelectedReportCategory('Tất cả')}
                      >
                        <i className="fa-solid fa-earth-americas perm-tree-icon icon-globe"></i>
                        <span>Tất cả</span>
                      </div>
                      {reportCategories
                        .filter((c) => c !== 'Tất cả')
                        .map((cat) => (
                          <div
                            key={cat}
                            className={`perm-tree-item ${selectedReportCategory === cat ? 'active' : ''}`}
                            onClick={() => setSelectedReportCategory(cat)}
                          >
                            <i className="fa-regular fa-folder perm-tree-icon icon-folder"></i>
                            <span>{cat}</span>
                          </div>
                        ))}
                    </div>

                    {/* Bảng quyền xem báo cáo bên phải */}
                    <div className="perm-grid-pane">
                      {/* Thanh Lọc & Các nút chức năng */}
                      <div className="perm-grid-toolbar">
                        <div className="perm-filter-box">
                          <label className="perm-filter-label">Lọc:</label>
                          <input
                            type="text"
                            className="perm-filter-input"
                            value={reportSearchFilter}
                            onChange={(e) => setReportSearchFilter(e.target.value)}
                            placeholder="Nhập tên báo cáo..."
                          />
                        </div>

                        <div className="perm-quick-actions">
                          <button
                            type="button"
                            className="perm-btn-batch"
                            onClick={handleLockAllReports}
                            title="Khóa quyền xem của các báo cáo hiển thị"
                          >
                            <i className="fa-regular fa-square"></i> Khóa tất cả
                          </button>
                          <button
                            type="button"
                            className="perm-btn-batch"
                            onClick={handleAllowAllReports}
                            title="Cho phép xem các báo cáo hiển thị"
                          >
                            <i className="fa-regular fa-square-check"></i> Cho phép tất cả
                          </button>
                          <button
                            type="button"
                            className="perm-btn-save-grid"
                            onClick={handleSaveReportRoles}
                            disabled={isSavingReports}
                          >
                            <i className={`fa-solid ${isSavingReports ? 'fa-spinner fa-spin' : 'fa-floppy-disk'}`}></i>
                            <span>{isSavingReports ? 'Đang lưu...' : 'Ghi dữ liệu'}</span>
                          </button>
                        </div>
                      </div>

                      {/* Bảng dữ liệu quyền xem báo cáo */}
                      <div className="perm-data-table-wrap">
                        <table className="perm-role-table">
                          <thead>
                            <tr>
                              <th className="th-func-name">Báo cáo</th>
                              <th className="th-report-col">Không được xem</th>
                              <th className="th-report-col">Được phép xem</th>
                            </tr>
                          </thead>
                          <tbody>
                            {displayedReports.map((r) => {
                              const mode = getReportMode(r.id);
                              const isForbidden = mode === 0;
                              const isAllowed = mode > 0;

                              return (
                                <tr key={r.id} className={isForbidden ? 'row-locked' : 'row-allowed'}>
                                  <td className="td-func-name">{r.name}</td>
                                  <td className="td-action-check">
                                    <input
                                      type="checkbox"
                                      className="perm-checkbox perm-chk-lock"
                                      checked={isForbidden}
                                      onChange={() => handleToggleReportMode(r.id, 0)}
                                    />
                                  </td>
                                  <td className="td-action-check">
                                    <input
                                      type="checkbox"
                                      className="perm-checkbox"
                                      checked={isAllowed}
                                      onChange={() => handleToggleReportMode(r.id, 30)}
                                    />
                                  </td>
                                </tr>
                              );
                            })}
                          </tbody>
                        </table>
                      </div>
                    </div>
                  </div>
                )}
              </div>
            )}
          </div>
        </div>
      )}

      {/* 4. FOOTER DƯỚI CÙNG: GHI CHÚ CHÚ Ý */}
      <div className="perm-window-footer">
        <div className="perm-footer-note">
          <em>Chú ý: Tài khoản hệ thống Admin không được hiển thị trong danh sách</em>
        </div>
      </div>

      {/* 5. MODAL THÊM / SỬA NHÓM (CHUẨN GIAO DIỆN WINFORMS THEO ẢNH NGƯỜI DÙNG) */}
      {groupModal.show && (
        <div className="win-group-modal-overlay" style={{ zIndex: 12000 }} onClick={() => { setShowImageDropdown(false); setGroupModal({ ...groupModal, show: false }); }}>
          <div className="win-group-modal-window" onClick={(e) => e.stopPropagation()}>
            {/* Title bar */}
            <div className="win-modal-titlebar">
              <span className="win-titlebar-text">Nhóm người dùng</span>
              <button
                type="button"
                className="win-titlebar-close"
                onClick={() => setGroupModal({ ...groupModal, show: false })}
                title="Đóng"
              >
                ✕
              </button>
            </div>

            {/* Sub-banner */}
            <div className="win-modal-banner">
              <div className="win-banner-icon-box">
                <i className="fa-regular fa-comment-dots" style={{ color: '#0284c7', fontSize: 26, position: 'relative', display: 'inline-block' }}>
                  <i className="fa-solid fa-pencil" style={{ color: '#16a34a', fontSize: 13, position: 'absolute', right: -4, top: -2 }}></i>
                </i>
              </div>
              <h3 className="win-banner-title">
                {groupModal.mode === 'create' ? 'Thêm mới nhóm người dùng' : 'Chỉnh sửa nhóm người dùng'}
              </h3>
            </div>

            {/* Form body */}
            <form onSubmit={handleSaveGroupModal}>
              <div className="win-modal-body">
                {/* Row 1: Tên nhóm người dùng + Ảnh */}
                <div className="win-form-row">
                  <label className="win-label" style={{ minWidth: 130 }}>Tên nhóm người dùng</label>
                  <input
                    type="text"
                    className="win-input-text"
                    value={groupModal.data.name}
                    onChange={(e) => setGroupModal({ ...groupModal, data: { ...groupModal.data, name: e.target.value } })}
                    placeholder="VD: Lễ tân, Thu ngân, Quản lý..."
                    required
                    autoFocus
                  />

                  <label className="win-label" style={{ marginLeft: 12 }}>Ảnh:</label>
                  <div className="win-img-combo" style={{ position: 'relative' }}>
                    <button
                      type="button"
                      className="win-img-combo-btn"
                      onClick={() => setShowImageDropdown(!showImageDropdown)}
                      title="Chọn ảnh biểu tượng cho nhóm từ CSDL"
                    >
                      {groupModal.data.sImageId && imageMap[groupModal.data.sImageId] ? (
                        <img
                          src={`data:image/png;base64,${imageMap[groupModal.data.sImageId]}`}
                          alt=""
                          className="win-img-preview"
                        />
                      ) : (
                        <div className="win-img-placeholder">
                          <i className="fa-regular fa-image" style={{ color: '#94a3b8', fontSize: 14 }}></i>
                        </div>
                      )}
                      <i className="fa-solid fa-chevron-down" style={{ fontSize: 10, color: '#64748b' }}></i>
                    </button>

                    {/* Popover chọn ảnh từ CSDL SIMAGE */}
                    {showImageDropdown && (
                      <div className="win-img-dropdown-popover">
                        <div className="win-img-popover-header">
                          <input
                            type="text"
                            placeholder="Lọc ảnh..."
                            className="win-img-search-input"
                            value={imageSearch}
                            onChange={(e) => setImageSearch(e.target.value)}
                            onClick={(e) => e.stopPropagation()}
                          />
                          <label className="win-btn-upload-file" title="Tải ảnh mới từ máy tính vào CSDL">
                            <i className="fa-solid fa-arrow-up-from-bracket"></i> Tải ảnh
                            <input
                              type="file"
                              accept="image/*"
                              style={{ display: 'none' }}
                              onChange={handleUploadGroupImage}
                              disabled={isUploadingImage}
                            />
                          </label>
                        </div>

                        <div className="win-img-grid">
                          {dbImages
                            .filter((img) => !imageSearch || img.name?.toLowerCase().includes(imageSearch.toLowerCase()))
                            .map((img) => (
                              <div
                                key={img.id}
                                className={`win-img-grid-item ${groupModal.data.sImageId === img.id ? 'selected' : ''}`}
                                onClick={() => {
                                  setGroupModal({ ...groupModal, data: { ...groupModal.data, sImageId: img.id } });
                                  setShowImageDropdown(false);
                                }}
                                title={img.name}
                              >
                                <img src={`data:image/png;base64,${img.data}`} alt={img.name} />
                              </div>
                            ))}
                        </div>
                      </div>
                    )}
                  </div>
                </div>

                {/* Row 2: Ghi chú */}
                <div className="win-form-row" style={{ alignItems: 'flex-start' }}>
                  <label className="win-label" style={{ minWidth: 130, paddingTop: 4 }}>Ghi chú:</label>
                  <textarea
                    className="win-textarea"
                    rows={6}
                    value={groupModal.data.note}
                    onChange={(e) => setGroupModal({ ...groupModal, data: { ...groupModal.data, note: e.target.value } })}
                    placeholder="Ghi chú về nhóm người dùng..."
                  />
                </div>
              </div>

              {/* Footer */}
              <div className="win-modal-footer">
                <button type="submit" className="win-btn-submit">
                  Ghi dữ liệu
                </button>
                <button
                  type="button"
                  className="win-btn-cancel"
                  onClick={() => { setShowImageDropdown(false); setGroupModal({ ...groupModal, show: false }); }}
                >
                  Thoát
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* 6. MODAL TÀI KHOẢN NGƯỜI DÙNG - THÊM MỚI / CẬP NHẬT */}
      {userModal.show && (
        <div className="win-modal-overlay" style={{ zIndex: 10000 }} onClick={() => setUserModal((prev) => ({ ...prev, show: false }))}>
          <div className="win-modal-box" style={{ width: 440 }} onClick={(e) => e.stopPropagation()}>
            {/* Titlebar */}
            <div className="win-modal-titlebar">
              <span className="win-titlebar-title">
                <i className="fa-solid fa-user" style={{ color: '#0284c7', marginRight: 6 }}></i>
                {userModal.mode === 'create' ? 'TÀI KHOẢN NGƯỜI DÙNG - THÊM MỚI' : 'TÀI KHOẢN NGƯỜI DÙNG - CẬP NHẬT'}
              </span>
              <button
                type="button"
                className="win-titlebar-close"
                onClick={() => setUserModal((prev) => ({ ...prev, show: false }))}
              >
                ✕
              </button>
            </div>

            {/* Banner */}
            <div className="win-modal-banner">
              <div className="win-banner-icon-box">
                <i className="fa-solid fa-user-circle" style={{ fontSize: 36, color: '#f59e0b' }}></i>
              </div>
              <h3 className="win-banner-title">Tài khoản người dùng</h3>
            </div>

            {/* Ribbon Action Bar */}
            <div className="win-ribbon-bar">
              <button type="button" className="win-ribbon-btn" title="Phím tắt">
                <i className="fa-solid fa-arrow-up-right-from-square"></i> Phím tắt <i className="fa-solid fa-caret-down" style={{ fontSize: 9 }}></i>
              </button>
              <button type="button" className="win-ribbon-btn" title="Trước (F10)">
                <i className="fa-solid fa-arrow-left"></i> Trước (F10)
              </button>
              <button type="button" className="win-ribbon-btn" title="Sau (F11)">
                Sau (F11) <i className="fa-solid fa-arrow-right"></i>
              </button>
              <button
                type="button"
                className="win-ribbon-btn"
                title="Tạo mới"
                onClick={handleOpenAddUser}
              >
                <i className="fa-solid fa-plus" style={{ color: '#16a34a' }}></i> Tạo mới
              </button>
            </div>

            {/* Form Body */}
            <div className="win-modal-body" style={{ gap: 8, padding: '12px 16px' }}>
              {/* Row 1: Nhân viên Lookup */}
              <div className="win-form-row">
                <label className="win-label" style={{ minWidth: 105 }}>Nhân viên</label>
                <div className="win-lookup-wrap">
                  <div
                    className="win-lookup-input-box"
                    onClick={() => {
                      setShowEmployeeDropdown(!showEmployeeDropdown);
                      setShowUserGroupDropdown(false);
                    }}
                  >
                    <input
                      type="text"
                      className="win-lookup-input"
                      readOnly
                      placeholder="-- Chọn nhân viên --"
                      value={employees.find((e) => e.id === userModal.data.employeeId)?.name || ''}
                    />
                    <div className="win-lookup-arrow">
                      <i className="fa-solid fa-chevron-down"></i>
                    </div>
                  </div>

                  {/* Dropdown popup nhân viên */}
                  {showEmployeeDropdown && (
                    <div className="win-lookup-popup" style={{ width: '100%' }}>
                      <div className="win-lookup-list">
                        <div
                          className={`win-lookup-item ${!userModal.data.employeeId ? 'selected' : ''}`}
                          onClick={() => {
                            setUserModal((prev) => ({
                              ...prev,
                              data: { ...prev.data, employeeId: '' }
                            }));
                            setShowEmployeeDropdown(false);
                          }}
                        >
                          <i className="fa-regular fa-user" style={{ color: '#94a3b8' }}></i>
                          <span>-- Không gán nhân viên --</span>
                        </div>
                        {employees.map((emp) => (
                          <div
                            key={emp.id}
                            className={`win-lookup-item ${userModal.data.employeeId === emp.id ? 'selected' : ''}`}
                            onClick={() => {
                              setUserModal((prev) => ({
                                ...prev,
                                data: {
                                  ...prev.data,
                                  employeeId: emp.id,
                                  fullName: emp.name // Auto-fill họ và tên
                                }
                              }));
                              setShowEmployeeDropdown(false);
                            }}
                          >
                            <i className="fa-solid fa-user-tie" style={{ color: '#0284c7' }}></i>
                            <span>{emp.name}</span>
                          </div>
                        ))}
                      </div>

                      {/* Dropdown footer bar */}
                      <div className="win-lookup-footer">
                        <button
                          type="button"
                          className="win-lookup-foot-btn add"
                          onClick={() => {
                            setShowEmployeeDropdown(false);
                            setEmployeeModal({
                              show: true,
                              data: {
                                name: '',
                                diaChi: '',
                                dienThoai: '',
                                note: '',
                                sImageId: '',
                                cachTinhLuong: 30,
                                luongCa: 0,
                                luongThang: 0,
                                nghiThu7: false,
                                nghiChuNhat: false
                              }
                            });
                          }}
                        >
                          <i className="fa-solid fa-plus"></i> Thêm
                        </button>
                        <button
                          type="button"
                          className="win-lookup-foot-btn reload"
                          onClick={async () => {
                            const metaRes = await adminService.getUserMetadata();
                            if (metaRes && metaRes.employees) setEmployees(metaRes.employees);
                            showNotification && showNotification('🔄 Đã tải lại danh sách nhân viên!');
                          }}
                        >
                          <i className="fa-solid fa-rotate"></i> Tải
                        </button>
                        <button
                          type="button"
                          className="win-lookup-foot-btn"
                          onClick={() => showNotification && showNotification(`📋 Tổng số ${employees.length} nhân viên trong CSDL`)}
                        >
                          Danh mục
                        </button>
                      </div>
                    </div>
                  )}
                </div>
              </div>

              {/* Row 2: Họ và tên */}
              <div className="win-form-row">
                <label className="win-label" style={{ minWidth: 105 }}>Họ và tên</label>
                <input
                  type="text"
                  className="win-input-text"
                  value={userModal.data.fullName}
                  onChange={(e) =>
                    setUserModal((prev) => ({
                      ...prev,
                      data: { ...prev.data, fullName: e.target.value }
                    }))
                  }
                  placeholder="Nhập họ và tên..."
                />
              </div>

              {/* Row 3: Tài khoản (light cyan background) */}
              <div className="win-form-row">
                <label className="win-label" style={{ minWidth: 105 }}>Tài khoản</label>
                <input
                  type="text"
                  className="win-input-text win-input-cyan"
                  value={userModal.data.username}
                  onChange={(e) =>
                    setUserModal((prev) => ({
                      ...prev,
                      data: { ...prev.data, username: e.target.value }
                    }))
                  }
                  placeholder="Tên đăng nhập..."
                  disabled={userModal.mode === 'edit'}
                  required
                />
              </div>

              {/* Row 4: Mật khẩu */}
              <div className="win-form-row">
                <label className="win-label" style={{ minWidth: 105 }}>Mật khẩu</label>
                <input
                  type="password"
                  className="win-input-text"
                  value={userModal.data.password}
                  onChange={(e) =>
                    setUserModal((prev) => ({
                      ...prev,
                      data: { ...prev.data, password: e.target.value }
                    }))
                  }
                  placeholder={userModal.mode === 'edit' ? '(Để trống nếu không đổi)' : 'Nhập mật khẩu...'}
                />
              </div>

              {/* Row 5: Email */}
              <div className="win-form-row">
                <label className="win-label" style={{ minWidth: 105 }}>Email</label>
                <input
                  type="email"
                  className="win-input-text"
                  value={userModal.data.email}
                  onChange={(e) =>
                    setUserModal((prev) => ({
                      ...prev,
                      data: { ...prev.data, email: e.target.value }
                    }))
                  }
                  placeholder="Email liên hệ..."
                />
              </div>

              {/* Row 6: Nhóm người dùng (yellow background) */}
              <div className="win-form-row">
                <label className="win-label" style={{ minWidth: 105 }}>Nhóm người dùng</label>
                <div className="win-lookup-wrap">
                  <div
                    className="win-lookup-input-box win-input-yellow"
                    onClick={() => {
                      setShowUserGroupDropdown(!showUserGroupDropdown);
                      setShowEmployeeDropdown(false);
                    }}
                  >
                    <div style={{ padding: '0 6px', display: 'flex', alignItems: 'center' }}>
                      {(() => {
                        const selGrp = groups.find((g) => g.id === userModal.data.groupId);
                        if (!selGrp) return <i className="fa-solid fa-users" style={{ color: '#ca8a04', fontSize: 13 }}></i>;
                        if (selGrp.sImageId && imageMap[selGrp.sImageId]) {
                          return (
                            <img
                              src={`data:image/png;base64,${imageMap[selGrp.sImageId]}`}
                              alt=""
                              style={{ width: 16, height: 16, objectFit: 'contain' }}
                            />
                          );
                        }
                        const isMgr = selGrp.name.toLowerCase().includes('quản lý');
                        return <i className={`fa-solid ${isMgr ? 'fa-user-tie' : 'fa-star'}`} style={{ color: isMgr ? '#ea580c' : '#ca8a04', fontSize: 13 }}></i>;
                      })()}
                    </div>
                    <input
                      type="text"
                      className="win-lookup-input win-input-yellow"
                      readOnly
                      value={groups.find((g) => g.id === userModal.data.groupId)?.name || '-- Chọn nhóm --'}
                    />
                    <div className="win-lookup-arrow">
                      <i className="fa-solid fa-chevron-down"></i>
                    </div>
                  </div>

                  {/* Dropdown popup nhóm */}
                  {showUserGroupDropdown && (
                    <div className="win-lookup-popup" style={{ width: '100%' }}>
                      <div className="win-lookup-list">
                        {groups.map((grp) => {
                          const hasImg = grp.sImageId && imageMap[grp.sImageId];
                          const isMgr = grp.name.toLowerCase().includes('quản lý');
                          return (
                            <div
                              key={grp.id}
                              className={`win-lookup-item ${userModal.data.groupId === grp.id ? 'selected' : ''}`}
                              onClick={() => {
                                setUserModal((prev) => ({
                                  ...prev,
                                  data: { ...prev.data, groupId: grp.id }
                                }));
                                setShowUserGroupDropdown(false);
                              }}
                            >
                              {hasImg ? (
                                <img
                                  src={`data:image/png;base64,${imageMap[grp.sImageId]}`}
                                  alt=""
                                  style={{ width: 16, height: 16, objectFit: 'contain' }}
                                />
                              ) : (
                                <i className={`fa-solid ${isMgr ? 'fa-user-tie' : 'fa-star'}`} style={{ color: isMgr ? '#ea580c' : '#eab308' }}></i>
                              )}
                              <span>{grp.name}</span>
                            </div>
                          );
                        })}
                      </div>

                      <div className="win-lookup-footer">
                        <button
                          type="button"
                          className="win-lookup-foot-btn add"
                          onClick={() => {
                            setShowUserGroupDropdown(false);
                            handleOpenAddGroup();
                          }}
                        >
                          <i className="fa-solid fa-plus"></i> Thêm
                        </button>
                        <button
                          type="button"
                          className="win-lookup-foot-btn reload"
                          onClick={fetchSummary}
                        >
                          <i className="fa-solid fa-rotate"></i> Tải
                        </button>
                        <button
                          type="button"
                          className="win-lookup-foot-btn"
                          onClick={() => showNotification && showNotification(`📋 ${groups.length} nhóm quyền đang có`)}
                        >
                          Danh mục
                        </button>
                      </div>
                    </div>
                  )}
                </div>
              </div>

              {/* Row 7: Cửa hàng truy cập (Table Grid) */}
              <div className="win-form-row" style={{ alignItems: 'flex-start', marginTop: 4 }}>
                <label className="win-label" style={{ minWidth: 105, paddingTop: 4 }}>Cửa hàng truy cập</label>
                <div className="win-store-grid-wrap">
                  <table className="win-store-table">
                    <thead>
                      <tr>
                        <th>Cửa hàng</th>
                        <th>Truy cập</th>
                      </tr>
                    </thead>
                    <tbody>
                      {stores.length === 0 ? (
                        <tr>
                          <td colSpan={2} style={{ textAlign: 'center', color: '#94a3b8', padding: 10 }}>
                            Không có cửa hàng nào
                          </td>
                        </tr>
                      ) : (
                        stores.map((st) => {
                          const isSelected = selectedStoreRowId === st.id;
                          const isChecked = !!userModal.data.storeAccess?.[st.id];
                          return (
                            <tr
                              key={st.id}
                              className={isSelected ? 'row-selected' : ''}
                              onClick={() => setSelectedStoreRowId(st.id)}
                            >
                              <td>{st.name}</td>
                              <td>
                                <input
                                  type="checkbox"
                                  className="win-checkbox-access"
                                  checked={isChecked}
                                  onChange={(e) => {
                                    e.stopPropagation();
                                    setUserModal((prev) => ({
                                      ...prev,
                                      data: {
                                        ...prev.data,
                                        storeAccess: {
                                          ...prev.data.storeAccess,
                                          [st.id]: e.target.checked
                                        }
                                      }
                                    }));
                                  }}
                                />
                              </td>
                            </tr>
                          );
                        })
                      )}
                    </tbody>
                  </table>
                </div>
              </div>
            </div>

            {/* Modal Multi Buttons Footer */}
            <div className="win-modal-footer-multi">
              <button
                type="button"
                className="win-foot-btn primary"
                onClick={() => executeSaveUser(false, false)}
                title="Lưu dữ liệu"
              >
                <i className="fa-solid fa-floppy-disk"></i> Lưu
              </button>
              <button
                type="button"
                className="win-foot-btn"
                onClick={() => executeSaveUser(false, true)}
                title="Lưu và thêm mới tài khoản khác"
              >
                Lưu &amp; Mới
              </button>
              <button
                type="button"
                className="win-foot-btn"
                onClick={() => executeSaveUser(true, false)}
                title="Lưu và đóng cửa sổ"
              >
                Lưu &amp; thoát
              </button>
              <button
                type="button"
                className="win-foot-btn exit"
                onClick={() => setUserModal((prev) => ({ ...prev, show: false }))}
                title="Đóng cửa sổ"
              >
                Thoát
              </button>
            </div>
          </div>
        </div>
      )}

      {/* 7. MODAL NHÂN VIÊN - THÊM MỚI (CHUẨN DESKTOP WINFORMS) */}
      {employeeModal.show && (
        <div className="win-modal-overlay" style={{ zIndex: 11000 }} onClick={() => setEmployeeModal((prev) => ({ ...prev, show: false }))}>
          <div className="win-modal-box" style={{ width: 450 }} onClick={(e) => e.stopPropagation()}>
            {/* Titlebar */}
            <div className="win-modal-titlebar">
              <span className="win-titlebar-title">
                👩 NHÂN VIÊN - THÊM MỚI
              </span>
              <button
                type="button"
                className="win-titlebar-close"
                onClick={() => setEmployeeModal((prev) => ({ ...prev, show: false }))}
              >
                ✕
              </button>
            </div>

            {/* Banner */}
            <div className="win-modal-banner">
              <div className="win-banner-icon-box">
                <i className="fa-solid fa-user-tie" style={{ fontSize: 36, color: '#0284c7' }}></i>
              </div>
              <h3 className="win-banner-title">Nhân viên</h3>
            </div>

            {/* Form Body */}
            <div className="win-modal-body" style={{ gap: 8, padding: '12px 16px' }}>
              {/* Row 1: Tên nhân viên + Ảnh combobox */}
              <div className="win-form-row">
                <label className="win-label" style={{ minWidth: 90 }}>Tên nhân viên</label>
                <input
                  type="text"
                  className="win-input-text win-input-cyan"
                  value={employeeModal.data.name}
                  onChange={(e) =>
                    setEmployeeModal((prev) => ({
                      ...prev,
                      data: { ...prev.data, name: e.target.value }
                    }))
                  }
                  placeholder="Nhập tên nhân viên..."
                  required
                  autoFocus
                />

                <label className="win-label" style={{ marginLeft: 6 }}>Ảnh:</label>
                <div className="win-img-combo" style={{ position: 'relative' }}>
                  <button
                    type="button"
                    className="win-img-combo-btn"
                    onClick={() => setShowEmpImageDropdown(!showEmpImageDropdown)}
                    title="Chọn ảnh nhân viên"
                  >
                    {employeeModal.data.sImageId && imageMap[employeeModal.data.sImageId] ? (
                      <img
                        src={`data:image/png;base64,${imageMap[employeeModal.data.sImageId]}`}
                        alt=""
                        className="win-img-preview"
                      />
                    ) : (
                      <div className="win-img-placeholder">
                        <i className="fa-regular fa-image" style={{ color: '#94a3b8', fontSize: 13 }}></i>
                      </div>
                    )}
                    <i className="fa-solid fa-chevron-down" style={{ fontSize: 9, color: '#64748b' }}></i>
                  </button>

                  {/* Popover chọn ảnh SIMAGE */}
                  {showEmpImageDropdown && (
                    <div className="win-img-dropdown-popover" style={{ right: 0, width: 280 }}>
                      <div className="win-img-grid">
                        {dbImages.map((img) => (
                          <div
                            key={img.id}
                            className={`win-img-grid-item ${employeeModal.data.sImageId === img.id ? 'selected' : ''}`}
                            onClick={() => {
                              setEmployeeModal((prev) => ({
                                ...prev,
                                data: { ...prev.data, sImageId: img.id }
                              }));
                              setShowEmpImageDropdown(false);
                            }}
                            title={img.name}
                          >
                            <img src={`data:image/png;base64,${img.data}`} alt={img.name} />
                          </div>
                        ))}
                      </div>
                    </div>
                  )}
                </div>
              </div>

              {/* Row 2: Địa chỉ */}
              <div className="win-form-row">
                <label className="win-label" style={{ minWidth: 90 }}>Địa chỉ</label>
                <input
                  type="text"
                  className="win-input-text"
                  value={employeeModal.data.diaChi}
                  onChange={(e) =>
                    setEmployeeModal((prev) => ({
                      ...prev,
                      data: { ...prev.data, diaChi: e.target.value }
                    }))
                  }
                  placeholder="Địa chỉ cư trú..."
                />
              </div>

              {/* Row 3: Điện thoại */}
              <div className="win-form-row">
                <label className="win-label" style={{ minWidth: 90 }}>Điện thoại</label>
                <input
                  type="text"
                  className="win-input-text"
                  value={employeeModal.data.dienThoai}
                  onChange={(e) =>
                    setEmployeeModal((prev) => ({
                      ...prev,
                      data: { ...prev.data, dienThoai: e.target.value }
                    }))
                  }
                  placeholder="Số điện thoại..."
                />
              </div>

              {/* GroupBox: Cách tính lương */}
              <fieldset className="win-fieldset">
                <legend className="win-legend">Cách tính lương</legend>
                <div className="win-salary-calc-group">
                  {/* Option 1: Lương theo ca (CACHTINHLUONG = 30) */}
                  <div className="win-calc-item">
                    <div className="win-calc-row">
                      <label className="win-radio-label">
                        <input
                          type="radio"
                          name="cachTinhLuong"
                          checked={employeeModal.data.cachTinhLuong === 30}
                          onChange={() =>
                            setEmployeeModal((prev) => ({
                              ...prev,
                              data: { ...prev.data, cachTinhLuong: 30 }
                            }))
                          }
                        />
                        Lương theo ca
                      </label>
                      <span className="win-label" style={{ fontWeight: 400 }}>Lương ca</span>
                      <input
                        type="number"
                        className="win-num-input"
                        value={employeeModal.data.luongCa}
                        onChange={(e) =>
                          setEmployeeModal((prev) => ({
                            ...prev,
                            data: { ...prev.data, luongCa: parseFloat(e.target.value) || 0 }
                          }))
                        }
                      />
                    </div>
                    <span className="win-calc-desc">
                      (Cách tính: Lương = Tổng ca làm việc x Lương ca)
                    </span>
                  </div>

                  {/* Option 2: Lương tháng theo ca (CACHTINHLUONG = 60) */}
                  <div className="win-calc-item">
                    <div className="win-calc-row">
                      <label className="win-radio-label">
                        <input
                          type="radio"
                          name="cachTinhLuong"
                          checked={employeeModal.data.cachTinhLuong === 60}
                          onChange={() =>
                            setEmployeeModal((prev) => ({
                              ...prev,
                              data: { ...prev.data, cachTinhLuong: 60 }
                            }))
                          }
                        />
                        Lương tháng theo ca
                      </label>
                      <span className="win-label" style={{ fontWeight: 400 }}>Lương tháng</span>
                      <input
                        type="number"
                        className="win-num-input"
                        value={employeeModal.data.luongThang}
                        onChange={(e) =>
                          setEmployeeModal((prev) => ({
                            ...prev,
                            data: { ...prev.data, luongThang: parseFloat(e.target.value) || 0 }
                          }))
                        }
                      />
                    </div>
                    <span className="win-calc-desc">
                      (Cách tính: Lương = Tổng ca làm việc x Lương tháng / số ngày tháng)
                    </span>
                  </div>

                  {/* Option 3: Lương tháng theo ngày (CACHTINHLUONG = 0) */}
                  <div className="win-calc-item">
                    <div className="win-calc-row">
                      <label className="win-radio-label">
                        <input
                          type="radio"
                          name="cachTinhLuong"
                          checked={employeeModal.data.cachTinhLuong === 0}
                          onChange={() =>
                            setEmployeeModal((prev) => ({
                              ...prev,
                              data: { ...prev.data, cachTinhLuong: 0 }
                            }))
                          }
                        />
                        Lương tháng theo ngày
                      </label>
                      <label className="win-check-label">
                        <input
                          type="checkbox"
                          checked={employeeModal.data.nghiThu7}
                          onChange={(e) =>
                            setEmployeeModal((prev) => ({
                              ...prev,
                              data: { ...prev.data, nghiThu7: e.target.checked }
                            }))
                          }
                        />
                        Nghỉ thứ 7
                      </label>
                      <label className="win-check-label" style={{ marginLeft: 8 }}>
                        <input
                          type="checkbox"
                          checked={employeeModal.data.nghiChuNhat}
                          onChange={(e) =>
                            setEmployeeModal((prev) => ({
                              ...prev,
                              data: { ...prev.data, nghiChuNhat: e.target.checked }
                            }))
                          }
                        />
                        Nghỉ chủ nhật
                      </label>
                    </div>
                    <span className="win-calc-desc">
                      (Cách tính: Lương = Tổng ngày làm việc x Lương tháng / số ngày tháng)
                    </span>
                  </div>
                </div>
              </fieldset>

              {/* Row 4: Ghi chú */}
              <div className="win-form-row" style={{ marginTop: 4 }}>
                <label className="win-label" style={{ minWidth: 90 }}>Ghi chú</label>
                <input
                  type="text"
                  className="win-input-text"
                  value={employeeModal.data.note}
                  onChange={(e) =>
                    setEmployeeModal((prev) => ({
                      ...prev,
                      data: { ...prev.data, note: e.target.value }
                    }))
                  }
                  placeholder="Ghi chú thêm..."
                />
              </div>
            </div>

            {/* Modal Footer Buttons */}
            <div className="win-modal-footer-multi">
              <button
                type="button"
                className="win-foot-btn primary"
                onClick={() => executeSaveEmployee(false, false)}
                title="Lưu thông tin nhân viên"
              >
                <i className="fa-solid fa-floppy-disk"></i> Lưu
              </button>
              <button
                type="button"
                className="win-foot-btn"
                onClick={() => executeSaveEmployee(false, true)}
                title="Lưu và tiếp tục thêm mới nhân viên khác"
              >
                Lưu &amp; Mới
              </button>
              <button
                type="button"
                className="win-foot-btn"
                onClick={() => executeSaveEmployee(true, false)}
                title="Lưu và đóng cửa sổ"
              >
                Lưu &amp; thoát
              </button>
              <button
                type="button"
                className="win-foot-btn exit"
                onClick={() => setEmployeeModal((prev) => ({ ...prev, show: false }))}
                title="Đóng cửa sổ"
              >
                Thoát
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
