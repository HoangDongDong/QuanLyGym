import React, { useState, useEffect } from 'react';
import { khachHangService } from './services/khachHangService';
import { authService } from './services/authService';
import LoginModal from './components/auth/LoginModal';
import AccessControl from './components/AccessControl';
import AuditLogsView from './components/admin/AuditLogsView';
import UserPermissionsView from './components/admin/UserPermissionsView';
import SystemConfigView from './components/admin/SystemConfigView';
import CustomerManagementView from './components/CustomerManagementView';
import './components/admin/AdminViews.css';

// Định nghĩa bảng ánh xạ đường dẫn URL tương ứng với từng màn hình giao diện
const ROUTE_CONFIG = {
  '/': { section: 'accessControl', title: 'Kiểm soát ra vào', breadcrumb: 'Hoạt động / Kiểm soát ra vào', submenu: 'hoatDong' },
  '/kiem-soat-vao-ra': { section: 'accessControl', title: 'Kiểm soát ra vào', breadcrumb: 'Hoạt động / Kiểm soát ra vào', submenu: 'hoatDong' },
  '/access-control': { section: 'accessControl', title: 'Kiểm soát ra vào', breadcrumb: 'Hoạt động / Kiểm soát ra vào', submenu: 'hoatDong' },
  '/dashboard': { section: 'dashboard', title: 'Dashboard', breadcrumb: 'Dashboard', submenu: null },
  '/hoi-vien': { section: 'members', title: 'Danh mục Khách hàng', breadcrumb: 'Hoạt động / Khách hàng', submenu: 'hoatDong' },
  '/members': { section: 'members', title: 'Danh mục Khách hàng', breadcrumb: 'Hoạt động / Khách hàng', submenu: 'hoatDong' },
  '/goi-tap': { section: 'packages', title: 'Danh mục Loại Thẻ', breadcrumb: 'Hoạt động / Loại thẻ', submenu: 'hoatDong' },
  '/packages': { section: 'packages', title: 'Danh mục Loại Thẻ', breadcrumb: 'Hoạt động / Loại thẻ', submenu: 'hoatDong' },
  '/ton-quy': { section: 'revenue', title: 'Quản lý Thu Chi & Tồn Quỹ', breadcrumb: 'Tài chính / Tồn quỹ', submenu: 'quy' },
  '/revenue': { section: 'revenue', title: 'Quản lý Thu Chi & Tồn Quỹ', breadcrumb: 'Tài chính / Tồn quỹ', submenu: 'quy' },
  '/admin/audit-logs': { section: 'auditLogs', title: 'Lịch sử tương tác hệ thống', breadcrumb: 'Quản trị / Lịch sử tương tác', submenu: 'quanTri' },
  '/admin/user-permissions': { section: 'users', title: 'Người dùng và phân quyền', breadcrumb: 'Quản trị / Người dùng & Phân quyền', submenu: 'quanTri' },
  '/admin/system-config': { section: 'systemConfig', title: 'Cấu hình toàn hệ thống', breadcrumb: 'Quản trị / Cấu hình toàn hệ thống', submenu: 'quanTri' },
};

const SECTION_PATHS = {
  accessControl: '/kiem-soat-vao-ra',
  dashboard: '/dashboard',
  members: '/hoi-vien',
  packages: '/goi-tap',
  revenue: '/ton-quy',
  auditLogs: '/admin/audit-logs',
  users: '/admin/user-permissions',
  systemConfig: '/admin/system-config',
};

function getRouteByPath(pathname) {
  const path = (pathname || '/').toLowerCase().replace(/\/+$/, '') || '/';
  if (ROUTE_CONFIG[path]) {
    return { path, ...ROUTE_CONFIG[path] };
  }
  if (path.includes('system-config') || path.includes('cau-hinh')) {
    return { path: '/admin/system-config', ...ROUTE_CONFIG['/admin/system-config'] };
  }
  if (path.includes('user-permissions') || path.includes('phan-quyen') || path.includes('nguoi-dung')) {
    return { path: '/admin/user-permissions', ...ROUTE_CONFIG['/admin/user-permissions'] };
  }
  if (path.includes('audit-log') || path.includes('lich-su')) {
    return { path: '/admin/audit-logs', ...ROUTE_CONFIG['/admin/audit-logs'] };
  }
  if (path.includes('dashboard')) {
    return { path: '/dashboard', ...ROUTE_CONFIG['/dashboard'] };
  }
  if (path.includes('hoi-vien') || path.includes('khach-hang') || path.includes('members')) {
    return { path: '/hoi-vien', ...ROUTE_CONFIG['/hoi-vien'] };
  }
  if (path.includes('goi-tap') || path.includes('loai-the') || path.includes('packages')) {
    return { path: '/goi-tap', ...ROUTE_CONFIG['/goi-tap'] };
  }
  if (path.includes('ton-quy') || path.includes('thu-chi') || path.includes('revenue')) {
    return { path: '/ton-quy', ...ROUTE_CONFIG['/ton-quy'] };
  }
  return { path: '/kiem-soat-vao-ra', ...ROUTE_CONFIG['/kiem-soat-vao-ra'] };
}

export default function App() {
  const initialRoute = getRouteByPath(window.location.pathname);

  // Navigation & Page State - Khởi tạo chính xác theo URL trình duyệt
  const [activeSection, setActiveSection] = useState(initialRoute.section);
  const [activePageTitle, setActivePageTitle] = useState(initialRoute.title);
  const [activeBreadcrumb, setActiveBreadcrumb] = useState(initialRoute.breadcrumb);
  const [isSidebarCollapsed, setIsSidebarCollapsed] = useState(false);
  const [isMobileSidebarOpen, setIsMobileSidebarOpen] = useState(false);
  const [timeFilter, setTimeFilter] = useState('Nov');

  // Co dãn độ rộng thanh Sidebar điều hướng (Hộp Đỏ theo ảnh người dùng)
  const [sidebarWidth, setSidebarWidth] = useState(() => {
    const saved = localStorage.getItem('admindek_sidebar_width');
    return saved ? Math.max(160, Math.min(420, parseInt(saved, 10))) : 240;
  });
  const [isDraggingSidebarSplitter, setIsDraggingSidebarSplitter] = useState(false);

  const handleSidebarSplitterPointerDown = (e) => {
    e.preventDefault();
    if (isSidebarCollapsed) return;
    setIsDraggingSidebarSplitter(true);
    const startX = e.clientX;
    const startWidth = sidebarWidth;

    const handlePointerMove = (moveEvent) => {
      const deltaX = moveEvent.clientX - startX;
      const newWidth = Math.max(160, Math.min(420, startWidth + deltaX));
      setSidebarWidth(newWidth);
    };

    const handlePointerUp = () => {
      setIsDraggingSidebarSplitter(false);
      window.removeEventListener('pointermove', handlePointerMove);
      window.removeEventListener('pointerup', handlePointerUp);
      setSidebarWidth((w) => {
        localStorage.setItem('admindek_sidebar_width', String(w));
        return w;
      });
    };

    window.addEventListener('pointermove', handlePointerMove);
    window.addEventListener('pointerup', handlePointerUp);
  };

  // Submenu toggle states - Mở sẵn nhóm tương ứng với URL hiện tại
  const [openSubmenu, setOpenSubmenu] = useState({
    hoatDong: initialRoute.submenu === 'hoatDong' || !initialRoute.submenu,
    banHang: false,
    khoHang: false,
    quy: initialRoute.submenu === 'quy',
    nhanSu: false,
    quanTri: initialRoute.submenu === 'quanTri',
    duLieuBanDau: false
  });

  // Data State from Firebird Database
  const [customers, setCustomers] = useState([]);
  const [loading, setLoading] = useState(false);
  const [searchTerm, setSearchTerm] = useState('');
  const [memberFilter, setMemberFilter] = useState('all');

  // Auth & Dialog State - Ban đầu chưa đăng nhập (chỉ nạp khi đã lưu trong storage)
  const [currentUser, setCurrentUser] = useState(() => {
    return authService.getCurrentUser();
  });
  const isAdmin = currentUser?.isAdmin === true || currentUser?.role?.toLowerCase() === 'admin';
  const [showLoginModal, setShowLoginModal] = useState(false);
  const [toastMsg, setToastMsg] = useState('');
  const [showToast, setShowToast] = useState(false);

  // New Member Modal State
  const [showNewMemberModal, setShowNewMemberModal] = useState(false);
  const [newMemberForm, setNewMemberForm] = useState({
    name: '',
    phone: '',
    address: '',
    packageId: 1
  });

  const showNotification = (msg) => {
    setToastMsg(msg);
    setShowToast(true);
    setTimeout(() => setShowToast(false), 2400);
  };

  const handleLoginSuccess = (user) => {
    setCurrentUser(user);
    setShowLoginModal(false);
    showNotification(`Xin chào ${user.fullName || user.username}! Đăng nhập thành công.`);
  };

  const handleLogout = () => {
    authService.logout();
    setCurrentUser(null);
    setShowLoginModal(true);
    showNotification('Bạn đã đăng xuất khỏi hệ thống.');
  };

  // Load live data from Firebird
  const loadCustomers = async (search = '', filter = 'all') => {
    setLoading(true);
    try {
      const res = await khachHangService.getAll(search, filter);
      if (res && res.data) {
        setCustomers(res.data);
      }
    } catch (err) {
      console.error('Lỗi kết nối API Firebird:', err);
      showNotification('Không thể kết nối đến máy chủ API Firebird');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadCustomers(searchTerm, memberFilter);
  }, [memberFilter]);

  const handleSearch = (e) => {
    if (e.key === 'Enter') {
      loadCustomers(searchTerm, memberFilter);
    }
  };

  const navigateTo = (section, title, breadcrumb, explicitPath = null) => {
    setActiveSection(section);
    setActivePageTitle(title);
    setActiveBreadcrumb(breadcrumb);
    setIsMobileSidebarOpen(false);

    // Cập nhật đường link trên thanh URL của trình duyệt tương ứng với từng trang
    const targetPath = explicitPath || SECTION_PATHS[section] || '/';
    if (window.location.pathname !== targetPath) {
      window.history.pushState({ section, title, breadcrumb }, '', targetPath);
    }
  };

  // Lắng nghe sự kiện Back / Forward trên trình duyệt để đổi trang tương ứng
  useEffect(() => {
    const handlePopState = () => {
      const route = getRouteByPath(window.location.pathname);
      setActiveSection(route.section);
      setActivePageTitle(route.title);
      setActiveBreadcrumb(route.breadcrumb);
      if (route.submenu) {
        setOpenSubmenu(prev => ({
          hoatDong: false,
          banHang: false,
          khoHang: false,
          quy: false,
          nhanSu: false,
          quanTri: false,
          duLieuBanDau: prev.duLieuBanDau,
          [route.submenu]: true
        }));
      }
    };

    window.addEventListener('popstate', handlePopState);
    return () => window.removeEventListener('popstate', handlePopState);
  }, []);

  const toggleSubmenu = (menuKey) => {
    setOpenSubmenu(prev => {
      const willOpen = !prev[menuKey];
      if (!willOpen) {
        return { ...prev, [menuKey]: false };
      }
      return {
        hoatDong: false,
        banHang: false,
        khoHang: false,
        quy: false,
        nhanSu: false,
        quanTri: false,
        duLieuBanDau: prev.duLieuBanDau,
        [menuKey]: true
      };
    });
  };

  const getStatusBadge = (statusName, statusId) => {
    const id = Number(statusId);
    if (id === 1) {
      return <span className="badge-pill active">Đang sử dụng</span>;
    } else if (id === 0) {
      return <span className="badge-pill pending">Chưa kích hoạt</span>;
    } else if (id === 2) {
      return <span className="badge-pill pending">Bảo lưu</span>;
    } else if (id === 3 || id === 4) {
      return <span className="badge-pill expired">Quá hạn</span>;
    }
    return <span className="badge-pill pending">{statusName || 'Chưa kích hoạt'}</span>;
  };

  // Ban đầu: Bắt buộc đăng nhập trước khi vào hệ thống!
  if (!currentUser) {
    return (
      <LoginModal
        isOpen={true}
        isFullScreen={true}
        onLoginSuccess={handleLoginSuccess}
      />
    );
  }

  return (
    <div className="admindek-app">
      {/* Mobile Backdrop Overlay */}
      {isMobileSidebarOpen && (
        <div
          className="sidebar-backdrop"
          onClick={() => setIsMobileSidebarOpen(false)}
        />
      )}

      {/* ================= 1. LEFT SIDEBAR (ADMINDEK STYLE) ================= */}
      <aside
        className={`admindek-sidebar ${isSidebarCollapsed ? 'collapsed' : ''} ${isMobileSidebarOpen ? 'mobile-open' : ''} ${isDraggingSidebarSplitter ? 'resizing' : ''}`}
        style={!isSidebarCollapsed ? { width: `${sidebarWidth}px`, minWidth: `${sidebarWidth}px`, transition: isDraggingSidebarSplitter ? 'none' : undefined } : undefined}
      >
        {/* Brand Header */}
        <div className="sidebar-header">
          <div className="sidebar-brand" onClick={() => navigateTo('dashboard', 'Dashboard', 'Dashboard')}>
            <div className="brand-icon-wrap">
              <div className="brand-icon-inner"></div>
            </div>
            {!isSidebarCollapsed && (
              <span className="brand-title">ADMINDEK</span>
            )}
          </div>
          <button
            className="sidebar-toggle-btn desktop-only"
            title="Thu gọn / Mở rộng Sidebar"
            onClick={() => setIsSidebarCollapsed(!isSidebarCollapsed)}
          >
            <i className={`fa-solid ${isSidebarCollapsed ? 'fa-bars' : 'fa-bars-staggered'}`}></i>
          </button>
          <button
            className="sidebar-mobile-close-btn"
            title="Đóng menu"
            onClick={() => setIsMobileSidebarOpen(false)}
          >
            <i className="fa-solid fa-xmark"></i>
          </button>
        </div>

        {/* Sidebar Nav Items */}
        <div className="sidebar-body">
          {/* GROUP 1: NAVIGATION */}
          <div className="nav-group-title">NAVIGATION</div>

          {/* Dashboard */}
          <div
            className={`nav-item ${activeSection === 'dashboard' ? 'active' : ''}`}
            onClick={() => navigateTo('dashboard', 'Dashboard', 'Dashboard')}
          >
            <i className="fa-solid fa-house nav-icon" style={{ color: '#2f80ed' }}></i>
            {!isSidebarCollapsed && (
              <>
                <span className="nav-text" style={{ fontWeight: 600, color: '#ffffff' }}>Dashboard</span>
                <i className="fa-solid fa-chevron-right nav-arrow"></i>
              </>
            )}
          </div>

          {/* GROUP 1: HOẠT ĐỘNG */}
          <div
            className={`nav-item ${openSubmenu.hoatDong ? 'open' : ''}`}
            onClick={() => toggleSubmenu('hoatDong')}
          >
            <i className={`fa-regular ${openSubmenu.hoatDong ? 'fa-square-minus' : 'fa-square-plus'} tree-square-icon`}></i>
            {!isSidebarCollapsed && (
              <>
                <span className="nav-text" style={{ fontWeight: 700, color: '#e2e8f0' }}>HOẠT ĐỘNG</span>
                <i className="fa-solid fa-chevron-right nav-arrow"></i>
              </>
            )}
          </div>

          {!isSidebarCollapsed && openSubmenu.hoatDong && (
            <div className="nav-submenu">
              <div className="submenu-item" onClick={() => showNotification('Danh mục máy vân tay')}>
                <i className="fa-solid fa-display sub-icon" style={{ color: '#0ea5e9' }}></i>
                <span>Danh mục máy vân tay</span>
              </div>
              <div
                className="submenu-item"
                onClick={() => { navigateTo('packages', 'Danh mục Loại Thẻ', 'Hoạt động / Loại thẻ'); showNotification('Danh mục loại thẻ'); }}
              >
                <i className="fa-solid fa-id-card sub-icon" style={{ color: '#0284c7' }}></i>
                <span>Danh mục loại thẻ</span>
              </div>
              <div className="submenu-item" onClick={() => showNotification('Danh mục ca tập')}>
                <i className="fa-regular fa-clock sub-icon" style={{ color: '#38bdf8' }}></i>
                <span>Danh mục ca tập</span>
              </div>

              <div className="submenu-divider"></div>

              <div
                className={`submenu-item ${activeSection === 'members' && memberFilter === 'all' ? 'active' : ''}`}
                onClick={() => { navigateTo('members', 'Danh mục Khách hàng', 'Hoạt động / Khách hàng'); setMemberFilter('all'); }}
              >
                <i className="fa-solid fa-face-smile sub-icon" style={{ color: '#eab308' }}></i>
                <span>Danh mục khách hàng</span>
              </div>
              <div
                className={`submenu-item ${activeSection === 'members' && memberFilter === '0' ? 'active' : ''}`}
                onClick={() => { navigateTo('members', 'Danh mục Thẻ chưa kích hoạt', 'Hoạt động / Thẻ chưa kích hoạt'); setMemberFilter('0'); }}
              >
                <i className="fa-solid fa-check sub-icon" style={{ color: '#94a3b8' }}></i>
                <span>Danh mục thẻ chưa kích hoạt</span>
              </div>
              <div className="submenu-item" onClick={() => showNotification('Đang đẩy thông tin thẻ lên thiết bị...')}>
                <i className="fa-solid fa-rotate sub-icon" style={{ color: '#0284c7' }}></i>
                <span>Đẩy thông tin thẻ lên thiết bị</span>
              </div>
              <div className="submenu-item" onClick={() => showNotification('Danh mục đợt khuyến mại gym')}>
                <i className="fa-solid fa-thumbs-up sub-icon" style={{ color: '#f59e0b' }}></i>
                <span>Danh mục đợt khuyến mại gym</span>
              </div>

              <div className="submenu-divider"></div>

              <div className="submenu-item" onClick={() => showNotification('Đặt cọc')}>
                <i className="fa-solid fa-file-invoice-dollar sub-icon" style={{ color: '#10b981' }}></i>
                <span>Đặt cọc</span>
              </div>
              <div className="submenu-item" onClick={() => showNotification('Danh sách đặt cọc')}>
                <i className="fa-solid fa-clipboard-user sub-icon" style={{ color: '#0284c7' }}></i>
                <span>Danh sách đặt cọc</span>
              </div>
              <div className="submenu-item" onClick={() => showNotification('Gia hạn thẻ')}>
                <i className="fa-solid fa-user-plus sub-icon" style={{ color: '#0284c7' }}></i>
                <span>Gia hạn thẻ</span>
              </div>
              <div className="submenu-item" onClick={() => showNotification('Quản lý gia hạn thẻ')}>
                <i className="fa-solid fa-id-card-clip sub-icon" style={{ color: '#0284c7' }}></i>
                <span>Quản lý gia hạn thẻ</span>
              </div>
              <div className="submenu-item" onClick={() => showNotification('Quản lý đổi loại thẻ')}>
                <i className="fa-solid fa-right-left sub-icon" style={{ color: '#0284c7' }}></i>
                <span>Quản lý đổi loại thẻ</span>
              </div>
              <div className="submenu-item" onClick={() => showNotification('Quản lý bảo lưu thẻ')}>
                <i className="fa-solid fa-box-archive sub-icon" style={{ color: '#06b6d4' }}></i>
                <span>Quản lý bảo lưu thẻ</span>
              </div>

              <div className="submenu-divider"></div>

              <div
                className={`submenu-item ${activeSection === 'accessControl' ? 'active' : ''}`}
                onClick={() => navigateTo('accessControl', 'Kiểm soát ra vào', 'Hoạt động / Kiểm soát ra vào')}
              >
                <i className="fa-solid fa-door-open sub-icon" style={{ color: '#06b6d4' }}></i>
                <span>Kiểm soát vào ra</span>
              </div>
              <div className="submenu-item" onClick={() => showNotification('Lịch sử vào ra')}>
                <i className="fa-solid fa-arrows-left-right sub-icon" style={{ color: '#0284c7' }}></i>
                <span>Lịch sử vào ra</span>
              </div>

              <div className="submenu-divider"></div>

              <div className="submenu-item" onClick={() => showNotification('Tùy chọn khác')}>
                <i className="fa-solid fa-screwdriver-wrench sub-icon" style={{ color: '#ef4444' }}></i>
                <span>Tùy chọn khác</span>
              </div>
            </div>
          )}

          {/* GROUP 2: BÁN HÀNG */}
          <div
            className={`nav-item ${openSubmenu.banHang ? 'open' : ''}`}
            onClick={() => toggleSubmenu('banHang')}
          >
            <i className={`fa-regular ${openSubmenu.banHang ? 'fa-square-minus' : 'fa-square-plus'} tree-square-icon`}></i>
            {!isSidebarCollapsed && (
              <>
                <span className="nav-text" style={{ fontWeight: 700, color: '#e2e8f0' }}>BÁN HÀNG</span>
                <i className="fa-solid fa-chevron-right nav-arrow"></i>
              </>
            )}
          </div>

          {!isSidebarCollapsed && openSubmenu.banHang && (
            <div className="nav-submenu">
              <div className="submenu-item" onClick={() => showNotification('Danh mục cửa hàng')}>
                <i className="fa-solid fa-store sub-icon" style={{ color: '#f59e0b' }}></i>
                <span>Danh mục cửa hàng</span>
              </div>
              <div className="submenu-item" onClick={() => showNotification('Danh mục mặt hàng')}>
                <i className="fa-solid fa-cubes sub-icon" style={{ color: '#94a3b8' }}></i>
                <span>Danh mục mặt hàng</span>
              </div>
              <div className="submenu-item" onClick={() => showNotification('Gửi tin nhắn tới khách hàng')}>
                <i className="fa-solid fa-mobile-screen-button sub-icon" style={{ color: '#eab308' }}></i>
                <span>Gửi tin nhắn tới khách hàng</span>
              </div>

              <div className="submenu-divider"></div>

              <div className="submenu-item" onClick={() => showNotification('Hóa đơn bán hàng')}>
                <i className="fa-solid fa-file-invoice sub-icon" style={{ color: '#0284c7' }}></i>
                <span>Hóa đơn bán hàng</span>
              </div>
              <div className="submenu-item" onClick={() => showNotification('Quản lý bán hàng')}>
                <i className="fa-solid fa-chart-column sub-icon" style={{ color: '#0284c7' }}></i>
                <span>Quản lý bán hàng</span>
              </div>
              <div className="submenu-item" onClick={() => showNotification('Công nợ bán hàng')}>
                <i className="fa-solid fa-dollar-sign sub-icon" style={{ color: '#16a34a' }}></i>
                <span>Công nợ bán hàng</span>
              </div>

              <div className="submenu-divider"></div>

              <div className="submenu-item" onClick={() => showNotification('Lưu vết hoạt động')}>
                <i className="fa-solid fa-shoe-prints sub-icon" style={{ color: '#eab308' }}></i>
                <span>Lưu vết hoạt động</span>
              </div>

              <div className="submenu-divider"></div>

              <div
                className={`submenu-item ${activeSection === 'revenue' ? 'active' : ''}`}
                onClick={() => navigateTo('revenue', 'Thống kê Doanh Thu', 'Bán hàng / Thống kê doanh thu')}
              >
                <i className="fa-solid fa-chart-line sub-icon" style={{ color: '#ef4444' }}></i>
                <span>Thống kê doanh thu</span>
              </div>
              <div className="submenu-item" onClick={() => showNotification('Thống kê mặt hàng bán')}>
                <i className="fa-solid fa-chart-simple sub-icon" style={{ color: '#10b981' }}></i>
                <span>Thống kê mặt hàng bán</span>
              </div>
              <div className="submenu-item" onClick={() => showNotification('Tổng hợp kết quả kinh doanh')}>
                <i className="fa-solid fa-chart-pie sub-icon" style={{ color: '#06b6d4' }}></i>
                <span>Tổng hợp kết quả kinh doanh</span>
              </div>
              <div className="submenu-item" onClick={() => showNotification('Chi tiết hoạt động ngày')}>
                <i className="fa-solid fa-calendar-day sub-icon" style={{ color: '#0284c7' }}></i>
                <span>Chi tiết hoạt động ngày</span>
              </div>
            </div>
          )}

          {/* GROUP 3: KHO HÀNG (Exact tree from screenshot) */}
          <div
            className={`nav-item ${openSubmenu.khoHang ? 'open' : ''}`}
            onClick={() => toggleSubmenu('khoHang')}
          >
            <i className={`fa-regular ${openSubmenu.khoHang ? 'fa-square-minus' : 'fa-square-plus'} tree-square-icon`}></i>
            {!isSidebarCollapsed && (
              <>
                <span className="nav-text" style={{ fontWeight: 700, color: '#e2e8f0' }}>KHO HÀNG</span>
                <i className="fa-solid fa-chevron-right nav-arrow"></i>
              </>
            )}
          </div>

          {!isSidebarCollapsed && openSubmenu.khoHang && (
            <div className="nav-submenu">
              <div className="submenu-item" onClick={() => { navigateTo('packages', 'Danh mục Kho Hàng', 'Kho hàng / Danh mục'); showNotification('Danh mục kho hàng'); }}>
                <i className="fa-solid fa-house-chimney sub-icon" style={{ color: '#d97706' }}></i>
                <span>Danh mục kho hàng</span>
              </div>
              <div className="submenu-item" onClick={() => showNotification('Danh mục nhà cung cấp')}>
                <i className="fa-solid fa-user-tie sub-icon" style={{ color: '#0284c7' }}></i>
                <span>Danh mục nhà cung cấp</span>
              </div>
              <div className="submenu-item" onClick={() => showNotification('Công nợ nhà cung cấp')}>
                <i className="fa-solid fa-coins sub-icon" style={{ color: '#eab308' }}></i>
                <span>Công nợ nhà cung cấp</span>
              </div>

              <div className="submenu-divider"></div>

              <div className="submenu-item" onClick={() => showNotification('Nhập hàng vào kho')}>
                <i className="fa-solid fa-arrow-right-to-bracket sub-icon" style={{ color: '#64748b' }}></i>
                <span>Nhập hàng vào kho</span>
              </div>
              <div className="submenu-item" onClick={() => showNotification('Xuất khác')}>
                <i className="fa-solid fa-arrow-left sub-icon" style={{ color: '#64748b' }}></i>
                <span>Xuất khác</span>
              </div>
              <div className="submenu-item" onClick={() => showNotification('Chuyển kho')}>
                <i className="fa-solid fa-arrows-rotate sub-icon" style={{ color: '#06b6d4' }}></i>
                <span>Chuyển kho</span>
              </div>
              <div className="submenu-item" onClick={() => showNotification('Kiểm kê kho')}>
                <i className="fa-solid fa-clipboard-check sub-icon" style={{ color: '#84cc16' }}></i>
                <span>Kiểm kê kho</span>
              </div>
              <div className="submenu-item" onClick={() => showNotification('Tính lại giá vốn')}>
                <i className="fa-solid fa-rotate sub-icon" style={{ color: '#0284c7' }}></i>
                <span>Tính lại giá vốn</span>
              </div>

              <div className="submenu-divider"></div>

              <div className="submenu-item" onClick={() => showNotification('Tồn kho')}>
                <i className="fa-solid fa-boxes-stacked sub-icon" style={{ color: '#d97706' }}></i>
                <span>Tồn kho</span>
              </div>
            </div>
          )}

          {/* GROUP 4: QUỸ (Exact tree from screenshot) */}
          <div
            className={`nav-item ${openSubmenu.quy ? 'open' : ''}`}
            onClick={() => toggleSubmenu('quy')}
          >
            <i className={`fa-regular ${openSubmenu.quy ? 'fa-square-minus' : 'fa-square-plus'} tree-square-icon`}></i>
            {!isSidebarCollapsed && (
              <>
                <span className="nav-text" style={{ fontWeight: 700, color: '#e2e8f0' }}>QUỸ</span>
                <i className="fa-solid fa-chevron-right nav-arrow"></i>
              </>
            )}
          </div>

          {!isSidebarCollapsed && openSubmenu.quy && (
            <div className="nav-submenu">
              <div className="submenu-item" onClick={() => showNotification('Danh mục lý do thu chi')}>
                <i className="fa-solid fa-tag sub-icon" style={{ color: '#10b981' }}></i>
                <span>Danh mục lý do thu chi</span>
              </div>
              <div className="submenu-item" onClick={() => showNotification('Danh mục tài khoản ngân hàng')}>
                <i className="fa-solid fa-building-columns sub-icon" style={{ color: '#64748b' }}></i>
                <span>Danh mục tài khoản ngân hàng</span>
              </div>

              <div className="submenu-divider"></div>

              <div className="submenu-item" onClick={() => showNotification('Tạo phiếu thu')}>
                <i className="fa-solid fa-file-circle-plus sub-icon" style={{ color: '#06b6d4' }}></i>
                <span>Tạo phiếu thu</span>
              </div>
              <div className="submenu-item" onClick={() => showNotification('Tạo phiếu chi')}>
                <i className="fa-solid fa-file-circle-minus sub-icon" style={{ color: '#06b6d4' }}></i>
                <span>Tạo phiếu chi</span>
              </div>
              <div className="submenu-item" onClick={() => showNotification('Danh mục phiếu thu')}>
                <i className="fa-solid fa-receipt sub-icon" style={{ color: '#10b981' }}></i>
                <span>Danh mục phiếu thu</span>
              </div>
              <div className="submenu-item" onClick={() => showNotification('Danh mục phiếu chi')}>
                <i className="fa-solid fa-hourglass-half sub-icon" style={{ color: '#3b82f6' }}></i>
                <span>Danh mục phiếu chi</span>
              </div>

              <div className="submenu-divider"></div>

              <div className="submenu-item" onClick={() => navigateTo('revenue', 'Quản lý Thu Chi & Tồn Quỹ', 'Tài chính / Tồn quỹ')}>
                <i className="fa-solid fa-money-bill-wave sub-icon" style={{ color: '#16a34a' }}></i>
                <span>Tồn quỹ</span>
              </div>
            </div>
          )}

          {/* GROUP 5: NHÂN SỰ (Exact tree from screenshot) */}
          <div
            className={`nav-item ${openSubmenu.nhanSu ? 'open' : ''}`}
            onClick={() => toggleSubmenu('nhanSu')}
          >
            <i className={`fa-regular ${openSubmenu.nhanSu ? 'fa-square-minus' : 'fa-square-plus'} tree-square-icon`}></i>
            {!isSidebarCollapsed && (
              <>
                <span className="nav-text" style={{ fontWeight: 700, color: '#e2e8f0' }}>NHÂN SỰ</span>
                <i className="fa-solid fa-chevron-right nav-arrow"></i>
              </>
            )}
          </div>

          {!isSidebarCollapsed && openSubmenu.nhanSu && (
            <div className="nav-submenu">
              <div className="submenu-item" onClick={() => showNotification('Danh mục nhân viên')}>
                <i className="fa-solid fa-user-nurse sub-icon" style={{ color: '#eab308' }}></i>
                <span>Danh mục nhân viên</span>
              </div>
              <div className="submenu-item" onClick={() => showNotification('Danh mục ca làm việc')}>
                <i className="fa-solid fa-clock-rotate-left sub-icon" style={{ color: '#06b6d4' }}></i>
                <span>Danh mục ca làm việc</span>
              </div>

              <div className="submenu-divider"></div>

              <div className="submenu-item" onClick={() => showNotification('Tạm ứng lương')}>
                <i className="fa-solid fa-money-check sub-icon" style={{ color: '#64748b' }}></i>
                <span>Tạm ứng lương</span>
              </div>
              <div className="submenu-item" onClick={() => showNotification('Thưởng phạt')}>
                <i className="fa-solid fa-scale-balanced sub-icon" style={{ color: '#ef4444' }}></i>
                <span>Thưởng phạt</span>
              </div>
              <div className="submenu-item" onClick={() => showNotification('Chấm công')}>
                <i className="fa-solid fa-calendar-check sub-icon" style={{ color: '#f59e0b' }}></i>
                <span>Chấm công</span>
              </div>
              <div className="submenu-item" onClick={() => showNotification('Tính lương')}>
                <i className="fa-solid fa-calculator sub-icon" style={{ color: '#10b981' }}></i>
                <span>Tính lương</span>
              </div>
            </div>
          )}

          {/* GROUP 6: QUẢN TRỊ (CHỈ DÀNH CHO TÀI KHOẢN ADMIN) */}
          {isAdmin && (
            <>
              <div
                className={`nav-item ${openSubmenu.quanTri ? 'open' : ''}`}
                onClick={() => toggleSubmenu('quanTri')}
              >
                <i className={`fa-regular ${openSubmenu.quanTri ? 'fa-square-minus' : 'fa-square-plus'} tree-square-icon`}></i>
                {!isSidebarCollapsed && (
                  <>
                    <span className="nav-text" style={{ fontWeight: 700, color: '#e2e8f0' }}>QUẢN TRỊ</span>
                    <i className="fa-solid fa-chevron-right nav-arrow"></i>
                  </>
                )}
              </div>

              {!isSidebarCollapsed && openSubmenu.quanTri && (
                <div className="nav-submenu">
                  {/* 1. Lịch sử tương tác hệ thống */}
                  <div
                    className={`submenu-item ${activeSection === 'auditLogs' ? 'active' : ''}`}
                    onClick={() => navigateTo('auditLogs', 'Lịch sử tương tác hệ thống', 'Quản trị / Lịch sử tương tác')}
                  >
                    <i className="fa-solid fa-clock-rotate-left sub-icon" style={{ color: '#0ea5e9' }}></i>
                    <span>Lịch sử tương tác hệ thống</span>
                  </div>

                  {/* 2. Người dùng và phân quyền */}
                  <div
                    className={`submenu-item ${activeSection === 'users' ? 'active' : ''}`}
                    onClick={() => navigateTo('users', 'Người dùng và phân quyền', 'Quản trị / Người dùng & Phân quyền')}
                  >
                    <i className="fa-solid fa-users-gear sub-icon" style={{ color: '#6366f1' }}></i>
                    <span>Người dùng và phân quyền</span>
                  </div>

                  {/* 3. Cấu hình toàn hệ thống */}
                  <div
                    className={`submenu-item ${activeSection === 'systemConfig' ? 'active' : ''}`}
                    onClick={() => navigateTo('systemConfig', 'Cấu hình toàn hệ thống', 'Quản trị / Cấu hình toàn hệ thống')}
                  >
                    <i className="fa-solid fa-sliders sub-icon" style={{ color: '#10b981' }}></i>
                    <span>Cấu hình toàn hệ thống</span>
                  </div>

                  <div className="submenu-divider"></div>

                  {/* Dữ liệu ban đầu (nested) */}
                  <div
                    className="submenu-nested-header"
                    onClick={(e) => { e.stopPropagation(); toggleSubmenu('duLieuBanDau'); }}
                  >
                    <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
                      <i className="fa-regular fa-square-plus sub-icon" style={{ color: '#3a82ee' }}></i>
                      <span>Dữ liệu ban đầu</span>
                    </div>
                    <i className={`fa-solid fa-chevron-right ${openSubmenu.duLieuBanDau ? 'fa-rotate-90' : ''}`} style={{ fontSize: 9, color: '#64748b' }}></i>
                  </div>

                  {openSubmenu.duLieuBanDau && (
                    <div className="submenu-nested-body">
                      <div className="submenu-item" onClick={() => showNotification('Dữ liệu thẻ ban đầu')}>
                        <i className="fa-solid fa-id-card sub-icon" style={{ color: '#64748b' }}></i>
                        <span>Dữ liệu thẻ ban đầu</span>
                      </div>

                      <div className="submenu-divider"></div>

                      <div className="submenu-item" onClick={() => showNotification('Công nợ khách hàng ban đầu')}>
                        <i className="fa-solid fa-file-invoice sub-icon" style={{ color: '#64748b' }}></i>
                        <span>Công nợ khách hàng ban đầu</span>
                      </div>
                      <div className="submenu-item" onClick={() => showNotification('Công nợ nhà cung cấp ban đầu')}>
                        <i className="fa-solid fa-truck-ramp-box sub-icon" style={{ color: '#64748b' }}></i>
                        <span>Công nợ nhà cung cấp ban đầu</span>
                      </div>
                      <div className="submenu-item" onClick={() => showNotification('Tồn kho ban đầu')}>
                        <i className="fa-solid fa-boxes-packing sub-icon" style={{ color: '#64748b' }}></i>
                        <span>Tồn kho ban đầu</span>
                      </div>
                    </div>
                  )}
                </div>
              )}
            </>
          )}

          {/* GROUP 6: HỆ THỐNG */}
          <div className="nav-group-title">HỆ THỐNG</div>

          <div
            className="nav-item"
            onClick={() => showNotification('CSDL Firebird: DATA.fdb (Firebird 2.5)')}
          >
            <i className="fa-solid fa-database nav-icon"></i>
            {!isSidebarCollapsed && (
              <>
                <span className="nav-text">CSDL Firebird</span>
                <span className="nav-badge badge-green">2.5</span>
              </>
            )}
          </div>

          <div
            className="nav-item"
            onClick={handleLogout}
            title="Đăng xuất khỏi tài khoản hiện tại"
          >
            <i className="fa-solid fa-arrow-right-from-bracket nav-icon"></i>
            {!isSidebarCollapsed && (
              <>
                <span className="nav-text">Đăng xuất</span>
                <i className="fa-solid fa-chevron-right nav-arrow"></i>
              </>
            )}
          </div>
        </div>

        {!isSidebarCollapsed && (
          <div
            className={`sidebar-splitter ${isDraggingSidebarSplitter ? 'dragging' : ''}`}
            onPointerDown={handleSidebarSplitterPointerDown}
            title="Kéo sang trái/phải để co dãn thanh menu điều hướng (Hộp Đỏ)"
          />
        )}
      </aside>

      {/* ================= 2. MAIN CONTENT AREA ================= */}
      <div className="admindek-main">
        {/* Top Navbar */}
        <header className="admindek-topbar">
          <div className="topbar-left">
            <button
              className="mobile-hamburger-btn"
              title="Mở menu điều hướng"
              onClick={() => setIsMobileSidebarOpen(true)}
            >
              <i className="fa-solid fa-bars"></i>
            </button>
            <div className="topbar-search">
              <i className="fa-solid fa-magnifying-glass"></i>
              <input
                placeholder="Search here..."
                value={searchTerm}
                onChange={(e) => setSearchTerm(e.target.value)}
                onKeyDown={handleSearch}
              />
            </div>
            <button
              className="topbar-icon-btn desktop-only"
              title="Toàn màn hình"
              onClick={() => {
                if (!document.fullscreenElement) {
                  document.documentElement.requestFullscreen();
                } else {
                  document.exitFullscreen();
                }
              }}
            >
              <i className="fa-solid fa-expand"></i>
            </button>
          </div>

          <div className="topbar-right">
            {/* Notification Bell */}
            <button
              className="topbar-badge-btn"
              title="Thông báo"
              onClick={() => showNotification('Hệ thống hoạt động ổn định • CSDL Firebird OK')}
            >
              <i className="fa-regular fa-bell"></i>
              <span className="badge-counter red">5</span>
            </button>

            {/* Chat / Messages */}
            <button
              className="topbar-badge-btn"
              title="Tin nhắn hội viên"
              onClick={() => showNotification('3 tin nhắn mới từ khách hàng')}
            >
              <i className="fa-regular fa-comment-dots"></i>
              <span className="badge-counter teal">3</span>
            </button>

            {/* User Profile */}
            <div
              className="topbar-user"
              title={currentUser ? `Đang đăng nhập: ${currentUser.fullName || currentUser.username} (${currentUser.role || 'User'})` : 'Chưa đăng nhập'}
              onClick={() => setShowLoginModal(true)}
            >
              <div className="user-avatar-img">
                {currentUser?.fullName
                  ? currentUser.fullName.split(' ').map(w => w[0]).filter(Boolean).slice(-2).join('').toUpperCase()
                  : (currentUser?.username ? currentUser.username.substring(0, 2).toUpperCase() : 'AD')}
              </div>
              <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'flex-start', lineHeight: 1.2 }}>
                <span className="user-name-text">{currentUser?.fullName || currentUser?.username || 'Chưa đăng nhập'}</span>
                <span style={{ fontSize: '10px', color: '#3a82ee', fontWeight: 700 }}>
                  {currentUser?.role === 'Admin' ? 'Quản trị viên' : (currentUser?.role === 'Member' ? 'Hội viên' : 'Nhân viên')}
                </span>
              </div>
              <div className="user-status-dot">✓</div>
            </div>
          </div>
        </header>

        {/* Page Header Card (Breadcrumb) - Ẩn khi xem giao diện quản trị hoặc khách hàng toàn màn hình */}
        {activeSection !== 'users' && activeSection !== 'systemConfig' && activeSection !== 'members' && (
          <div className="admindek-page-header">
            <div className="header-title-box">
              <div className="header-icon-square">
                <i className="fa-solid fa-house"></i>
              </div>
              <div className="header-text-meta">
                <h2>{activePageTitle}</h2>
                <p>lorem ipsum dolor sit amet, consectetur adipisicing elit</p>
              </div>
            </div>

            <div className="header-breadcrumb">
              <i className="fa-solid fa-house"></i>
              <span>/ {activeBreadcrumb}</span>
            </div>
          </div>
        )}

        {/* ================= VIEW 1: DASHBOARD ================= */}
        {activeSection === 'dashboard' && (
          <>
            {/* 2-Column Grid: Deals Analytics + KPI Cards */}
            <div className="admindek-grid">
              {/* Left Column: Deals Analytics */}
              <div className="deals-analytics-card">
                <div className="card-header-bar">
                  <span className="card-title-text">Deals Analytics</span>
                  <div className="chart-time-filter">
                    {['Aug', 'Sep', 'Oct', 'Nov', 'Dec', '2026'].map((month) => (
                      <button
                        key={month}
                        className={timeFilter === month ? 'active' : ''}
                        onClick={() => {
                          setTimeFilter(month);
                          showNotification(`Hiển thị dữ liệu tháng ${month}`);
                        }}
                      >
                        {month}
                      </button>
                    ))}
                  </div>
                </div>

                <div className="chart-visual-area">
                  <div className="chart-meta-row">
                    <span>JS chart by <strong>amCharts</strong></span>
                    <span style={{ cursor: 'pointer', color: '#3a82ee' }} onClick={() => showNotification('Hiển thị toàn bộ lịch sử')}>
                      Show all
                    </span>
                  </div>

                  {/* SVG Smooth Blue Area Wave Chart (Admindek exact style) */}
                  <svg viewBox="0 0 700 240" style={{ width: '100%', height: '220px', overflow: 'visible' }}>
                    <defs>
                      <linearGradient id="admindekBlueGrad" x1="0" y1="0" x2="0" y2="1">
                        <stop offset="0%" stopColor="#3a82ee" stopOpacity="0.45" />
                        <stop offset="60%" stopColor="#60a5fa" stopOpacity="0.2" />
                        <stop offset="100%" stopColor="#ffffff" stopOpacity="0.02" />
                      </linearGradient>
                      {/* Grid lines pattern */}
                      <pattern id="gridLines" width="700" height="40" patternUnits="userSpaceOnUse">
                        <line x1="0" y1="0" x2="700" y2="0" stroke="#f1f5f9" strokeWidth="1" />
                      </pattern>
                    </defs>

                    {/* Background Grid */}
                    <rect width="700" height="200" fill="url(#gridLines)" />

                    {/* Y-Axis Value Labels */}
                    <text x="10" y="25" fill="#94a3b8" fontSize="10">90</text>
                    <text x="10" y="65" fill="#94a3b8" fontSize="10">85</text>
                    <text x="10" y="105" fill="#94a3b8" fontSize="10">80</text>
                    <text x="10" y="145" fill="#94a3b8" fontSize="10">75</text>
                    <text x="10" y="185" fill="#94a3b8" fontSize="10">70</text>

                    {/* Wave Path Filled Area */}
                    <path
                      d="M 40,180
                         C 70,170 90,150 120,130
                         C 150,110 170,150 200,140
                         C 230,130 250,165 280,150
                         C 310,135 340,115 370,120
                         C 400,125 430,95 460,85
                         C 490,75 520,110 550,60
                         C 580,20 620,95 670,70
                         L 670,200 L 40,200 Z"
                      fill="url(#admindekBlueGrad)"
                    />

                    {/* Wave Line Stroke */}
                    <path
                      d="M 40,180
                         C 70,170 90,150 120,130
                         C 150,110 170,150 200,140
                         C 230,130 250,165 280,150
                         C 310,135 340,115 370,120
                         C 400,125 430,95 460,85
                         C 490,75 520,110 550,60
                         C 580,20 620,95 670,70"
                      fill="none"
                      stroke="#3a82ee"
                      strokeWidth="3"
                    />

                    {/* Data Points */}
                    <circle cx="120" cy="130" r="4" fill="#ffffff" stroke="#3a82ee" strokeWidth="2.5" />
                    <circle cx="200" cy="140" r="4" fill="#ffffff" stroke="#3a82ee" strokeWidth="2.5" />
                    <circle cx="280" cy="150" r="4" fill="#ffffff" stroke="#3a82ee" strokeWidth="2.5" />
                    <circle cx="370" cy="120" r="4" fill="#ffffff" stroke="#3a82ee" strokeWidth="2.5" />
                    <circle cx="460" cy="85" r="4" fill="#ffffff" stroke="#3a82ee" strokeWidth="2.5" />
                    <circle cx="550" cy="60" r="5" fill="#ffffff" stroke="#3a82ee" strokeWidth="3" />
                    <circle cx="670" cy="70" r="4" fill="#ffffff" stroke="#3a82ee" strokeWidth="2.5" />

                    {/* Timeline X-Axis Dates */}
                    <text x="100" y="222" fill="#64748b" fontSize="11" textAnchor="middle">Oct 23</text>
                    <text x="190" y="222" fill="#64748b" fontSize="11" textAnchor="middle">Oct 27</text>
                    <text x="280" y="222" fill="#64748b" fontSize="11" textAnchor="middle">Oct 31</text>
                    <text x="370" y="222" fill="#1e293b" fontSize="11" fontWeight="700" textAnchor="middle">Nov</text>
                    <text x="460" y="222" fill="#64748b" fontSize="11" textAnchor="middle">Nov 08</text>
                    <text x="550" y="222" fill="#64748b" fontSize="11" textAnchor="middle">Nov 12</text>
                    <text x="640" y="222" fill="#64748b" fontSize="11" textAnchor="middle">Nov 16</text>
                  </svg>
                </div>
              </div>

              {/* Right Column: Stacked KPI Stat Cards */}
              <div className="admindek-kpi-stack">
                {/* 1. Impressions */}
                <div className="kpi-card-admindek" onClick={() => navigateTo('members', 'Hội viên', 'Hội viên')}>
                  <div className="kpi-info-side">
                    <span className="kpi-label-text">Impressions</span>
                    <div className="kpi-big-value">{customers.length > 0 ? customers.length.toLocaleString('vi-VN') : '1,563'}</div>
                    <span className="kpi-date-sub">May 23 - June 01 (2026)</span>
                  </div>
                  <div className="kpi-icon-square blue">
                    <i className="fa-solid fa-eye"></i>
                  </div>
                </div>

                {/* 2. Goal */}
                <div className="kpi-card-admindek" onClick={() => navigateTo('revenue', 'Quản lý Thu Chi & Quỹ', 'Tài chính')}>
                  <div className="kpi-info-side">
                    <span className="kpi-label-text">Goal</span>
                    <div className="kpi-big-value" style={{ color: '#10b981' }}>30,564 tr</div>
                    <span className="kpi-date-sub">May 23 - June 01 (2026)</span>
                  </div>
                  <div className="kpi-icon-square teal">
                    <i className="fa-solid fa-bullseye"></i>
                  </div>
                </div>

                {/* 3. Impact */}
                <div className="kpi-card-admindek" onClick={() => showNotification('Tần suất tập luyện: 42.6%')}>
                  <div className="kpi-info-side">
                    <span className="kpi-label-text">Impact</span>
                    <div className="kpi-big-value" style={{ color: '#f59e0b' }}>42.6%</div>
                    <span className="kpi-date-sub">May 23 - June 01 (2026)</span>
                  </div>
                  <div className="kpi-icon-square orange">
                    <i className="fa-solid fa-hand"></i>
                  </div>
                </div>
              </div>
            </div>

            {/* Bottom Table Card: Recent Members from Firebird */}
            <div className="admindek-table-card">
              <div className="table-header-flex">
                <h4>Danh sách hội viên mới nhất (Dữ liệu từ Firebird DATA.fdb)</h4>
                <button
                  className="btn-admindek primary"
                  onClick={() => navigateTo('members', 'Danh sách Hội viên', 'Hội viên')}
                >
                  Xem tất cả hội viên <i className="fa-solid fa-arrow-right"></i>
                </button>
              </div>

              <div className="admindek-table-wrap">
                <table className="admindek-table">
                  <thead>
                    <tr>
                      <th>Mã Thẻ</th>
                      <th>Họ và tên</th>
                      <th>Số điện thoại</th>
                      <th>Địa chỉ</th>
                      <th>Gói tập</th>
                      <th>Số lần</th>
                      <th>Đã tập</th>
                      <th>Trạng thái</th>
                      <th style={{ textAlign: 'right' }}>Thao tác</th>
                    </tr>
                  </thead>
                  <tbody>
                    {loading ? (
                      <tr>
                        <td colSpan="9" style={{ textAlign: 'center', padding: '30px', color: '#64748b' }}>
                          <i className="fa-solid fa-spinner fa-spin"></i> Đang tải dữ liệu từ Firebird...
                        </td>
                      </tr>
                    ) : customers.length === 0 ? (
                      <tr>
                        <td colSpan="9" style={{ textAlign: 'center', padding: '30px', color: '#64748b' }}>
                          Chưa có dữ liệu hội viên
                        </td>
                      </tr>
                    ) : (
                      customers.slice(0, 5).map((c, idx) => (
                        <tr key={c.id || idx}>
                          <td><strong>#{c.maThe || c.maKhach || `HV0${idx + 1}`}</strong></td>
                          <td><span style={{ fontWeight: 600, color: '#1e3a8a' }}>{c.tenKhachHang || c.name}</span></td>
                          <td>{c.dienThoai || 'Chưa có'}</td>
                          <td>{c.diaChi || '---'}</td>
                          <td>{c.loaiThe || c.tenLoaiThe || 'Thẻ thường'}</td>
                          <td>{c.soLan ?? 0}</td>
                          <td>{c.daTap ?? 0}</td>
                          <td>{getStatusBadge(c.trangThai || c.tenTrangThai, c.trangThaiId || c.dTrangThaiId)}</td>
                          <td style={{ textAlign: 'right' }}>
                            <button
                              className="action-icon-btn"
                              title="Xem chi tiết"
                              onClick={() => showNotification(`Hội viên: ${c.tenKhachHang || c.name}`)}
                            >
                              <i className="fa-solid fa-eye"></i>
                            </button>
                            <button
                              className="action-icon-btn"
                              title="Chỉnh sửa"
                              onClick={() => showNotification(`Sửa: ${c.tenKhachHang || c.name}`)}
                            >
                              <i className="fa-solid fa-pen"></i>
                            </button>
                          </td>
                        </tr>
                      ))
                    )}
                  </tbody>
                </table>
              </div>
            </div>
          </>
        )}

        {/* ================= VIEW 2: DANH MỤC KHÁCH HÀNG (DESKTOP THEME) ================= */}
        {activeSection === 'members' && (
          <CustomerManagementView
            onSwitchToAccessControl={() => navigateTo('accessControl', 'Kiểm soát ra vào', 'Hoạt động / Kiểm soát ra vào')}
            showNotification={showNotification}
          />
        )}

        {/* ================= VIEW 3: GÓI TẬP ================= */}
        {activeSection === 'packages' && (
          <div style={{ padding: '0 28px 28px' }}>
            <div className="admindek-table-card" style={{ margin: 0, padding: 24 }}>
              <h3 style={{ marginBottom: 16, fontSize: 16, fontWeight: 700 }}>Thiết lập cấu hình gói tập</h3>
              <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 16 }}>
                <div>
                  <label className="login-label">Tên gói tập *</label>
                  <input className="login-input" defaultValue="Gói Gym 3 tháng VIP" />
                </div>
                <div>
                  <label className="login-label">Loại gói</label>
                  <select className="login-input" defaultValue="VIP">
                    <option>Standard</option>
                    <option>Plus</option>
                    <option>VIP</option>
                    <option>PT 1-1</option>
                  </select>
                </div>
                <div>
                  <label className="login-label">Thời hạn (ngày) *</label>
                  <input className="login-input" type="number" defaultValue="90" />
                </div>
                <div>
                  <label className="login-label">Giá bán (VNĐ) *</label>
                  <input className="login-input" type="number" defaultValue="1800000" />
                </div>
                <div style={{ gridColumn: '1 / -1' }}>
                  <label className="login-label">Mô tả quyền lợi</label>
                  <textarea className="login-input" style={{ height: 60, padding: '8px 12px' }} defaultValue="Tập không giới hạn mọi khung giờ, miễn phí tủ đồ locker và phòng xông hơi."></textarea>
                </div>
              </div>
              <div style={{ display: 'flex', justifyContent: 'flex-end', marginTop: 16, gap: 10 }}>
                <button className="btn-admindek" onClick={() => showNotification('Đã hủy thay đổi')}>Hủy</button>
                <button className="btn-admindek primary" onClick={() => showNotification('Lưu cấu hình gói tập thành công!')}>
                  <i className="fa-solid fa-floppy-disk"></i> Lưu cấu hình
                </button>
              </div>
            </div>
          </div>
        )}

        {/* ================= VIEW 4: TÀI CHÍNH ================= */}
        {activeSection === 'revenue' && (
          <div style={{ padding: '0 28px 28px' }}>
            <div className="admindek-table-card" style={{ margin: 0 }}>
              <div className="table-header-flex">
                <h4>Sổ quỹ thu chi gần nhất</h4>
                <button className="btn-admindek primary" onClick={() => showNotification('Tạo phiếu thu mới')}>
                  <i className="fa-solid fa-plus"></i> Tạo phiếu thu mới
                </button>
              </div>
              <div className="admindek-table-wrap">
                <table className="admindek-table">
                  <thead>
                    <tr>
                      <th>Mã Phiếu</th>
                      <th>Ngày lập</th>
                      <th>Loại</th>
                      <th>Lý do</th>
                      <th>Số tiền</th>
                      <th>Người nộp / nhận</th>
                      <th>Tài khoản</th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr>
                      <td><strong>PT-2026-0925</strong></td>
                      <td>25/09/2026 14:30</td>
                      <td><span className="badge-pill active">Thu phí gói tập</span></td>
                      <td>Hội viên Hoàng Văn Đông gia hạn thẻ</td>
                      <td><strong style={{ color: '#16a34a' }}>+600,000 đ</strong></td>
                      <td>Hoàng Văn Đông</td>
                      <td>Tiền mặt</td>
                    </tr>
                    <tr>
                      <td><strong>PC-2026-0924</strong></td>
                      <td>24/09/2026 09:15</td>
                      <td><span className="badge-pill expired">Chi tiền điện</span></td>
                      <td>Thanh toán điện phòng tập tháng 9</td>
                      <td><strong style={{ color: '#dc2626' }}>-4,850,000 đ</strong></td>
                      <td>Công ty Điện lực</td>
                      <td>Techcombank</td>
                    </tr>
                    <tr>
                      <td><strong>PT-2026-0923</strong></td>
                      <td>23/09/2026 18:20</td>
                      <td><span className="badge-pill active">Thu phí thẻ VIP</span></td>
                      <td>Hội viên Minh đăng ký mới thẻ năm</td>
                      <td><strong style={{ color: '#16a34a' }}>+3,500,000 đ</strong></td>
                      <td>Minh</td>
                      <td>Chuyển khoản VCB</td>
                    </tr>
                  </tbody>
                </table>
              </div>
            </div>
          </div>
        )}

        {/* ================= VIEW 5: KIỂM SOÁT RA VÀO ================= */}
        {activeSection === 'accessControl' && (
          <AccessControl
            showNotification={showNotification}
            onSwitchToCustomers={() => navigateTo('members', 'Danh mục Khách hàng', 'Hoạt động / Khách hàng')}
          />
        )}

        {/* ================= VIEW 6: LỊCH SỬ TƯƠNG TÁC HỆ THỐNG (CHỈ ADMIN) ================= */}
        {activeSection === 'auditLogs' && (
          <div style={{ padding: '0 28px 28px' }}>
            {isAdmin ? (
              <AuditLogsView showNotification={showNotification} />
            ) : (
              <div className="tap-admin-card" style={{ padding: 40, textAlign: 'center' }}>
                <i className="fa-solid fa-shield-halved" style={{ fontSize: 44, color: '#ef4444', marginBottom: 12 }}></i>
                <h3 style={{ fontSize: 18, color: '#0f172a', fontWeight: 800 }}>QUYỀN HẠN BỊ TỪ CHỐI</h3>
                <p style={{ color: '#64748b', marginTop: 6 }}>Chỉ có tài khoản Quản trị viên (Admin) mới có quyền truy cập Lịch sử tương tác hệ thống.</p>
              </div>
            )}
          </div>
        )}

        {/* ================= VIEW 7: NGƯỜI DÙNG VÀ PHÂN QUYỀN (CHỈ ADMIN - TOÀN MÀN HÌNH) ================= */}
        {activeSection === 'users' && (
          <div className="admin-fullscreen-container">
            {isAdmin ? (
              <UserPermissionsView showNotification={showNotification} />
            ) : (
              <div className="tap-admin-card" style={{ padding: 40, textAlign: 'center', margin: 28 }}>
                <i className="fa-solid fa-shield-halved" style={{ fontSize: 44, color: '#ef4444', marginBottom: 12 }}></i>
                <h3 style={{ fontSize: 18, color: '#0f172a', fontWeight: 800 }}>QUYỀN HẠN BỊ TỪ CHỐI</h3>
                <p style={{ color: '#64748b', marginTop: 6 }}>Chỉ có tài khoản Quản trị viên (Admin) mới có quyền quản lý người dùng và phân quyền.</p>
              </div>
            )}
          </div>
        )}

        {/* ================= VIEW 8: CẤU HÌNH TOÀN HỆ THỐNG (CHỈ ADMIN - TOÀN MÀN HÌNH) ================= */}
        {activeSection === 'systemConfig' && (
          <div className="admin-fullscreen-container">
            {isAdmin ? (
              <SystemConfigView showNotification={showNotification} />
            ) : (
              <div className="tap-admin-card" style={{ padding: 40, textAlign: 'center', margin: 28 }}>
                <i className="fa-solid fa-shield-halved" style={{ fontSize: 44, color: '#ef4444', marginBottom: 12 }}></i>
                <h3 style={{ fontSize: 18, color: '#0f172a', fontWeight: 800 }}>QUYỀN HẠN BỊ TỪ CHỐI</h3>
                <p style={{ color: '#64748b', marginTop: 6 }}>Chỉ có tài khoản Quản trị viên (Admin) mới có quyền thay đổi cấu hình toàn hệ thống.</p>
              </div>
            )}
          </div>
        )}
      </div>

      {/* ================= 3. MODAL THÊM HỘI VIÊN MỚI ================= */}
      {showNewMemberModal && (
        <div className="login-modal-overlay" onClick={() => setShowNewMemberModal(false)}>
          <div className="login-dialog" onClick={(e) => e.stopPropagation()}>
            <div className="login-banner">
              <div className="login-logo-circle">
                <i className="fa-solid fa-user-plus"></i>
              </div>
              <div className="login-banner-text">
                <h2>THÊM HỘI VIÊN MỚI</h2>
                <p>Hệ thống tự động lưu vào Firebird DATA.fdb</p>
              </div>
            </div>
            <div className="login-body">
              <div className="login-field-row">
                <label className="login-label">Họ và tên *</label>
                <input
                  type="text"
                  className="login-input"
                  placeholder="Nhập tên hội viên..."
                  value={newMemberForm.name}
                  onChange={(e) => setNewMemberForm({ ...newMemberForm, name: e.target.value })}
                />
              </div>
              <div className="login-field-row">
                <label className="login-label">Số điện thoại</label>
                <input
                  type="text"
                  className="login-input"
                  placeholder="VD: 0987654321"
                  value={newMemberForm.phone}
                  onChange={(e) => setNewMemberForm({ ...newMemberForm, phone: e.target.value })}
                />
              </div>
              <div className="login-field-row">
                <label className="login-label">Địa chỉ</label>
                <input
                  type="text"
                  className="login-input"
                  placeholder="Địa chỉ liên hệ..."
                  value={newMemberForm.address}
                  onChange={(e) => setNewMemberForm({ ...newMemberForm, address: e.target.value })}
                />
              </div>
              <div className="login-field-row">
                <label className="login-label">Loại gói thẻ</label>
                <select
                  className="login-input"
                  value={newMemberForm.packageId}
                  onChange={(e) => setNewMemberForm({ ...newMemberForm, packageId: Number(e.target.value) })}
                >
                  <option value="1">Thẻ tháng tiêu chuẩn (Standard)</option>
                  <option value="2">Thẻ VIP 3 tháng</option>
                  <option value="3">Thẻ 1 năm Unlimited</option>
                </select>
              </div>
            </div>
            <div className="login-footer">
              <button
                type="button"
                className="btn-admindek"
                onClick={() => setShowNewMemberModal(false)}
              >
                Hủy bỏ
              </button>
              <button
                type="button"
                className="login-btn-primary"
                onClick={async () => {
                  if (!newMemberForm.name.trim()) {
                    showNotification('Vui lòng nhập tên hội viên');
                    return;
                  }
                  try {
                    await khachHangService.create({
                      name: newMemberForm.name,
                      dienThoai: newMemberForm.phone,
                      diaChi: newMemberForm.address,
                      dLoaiTheId: newMemberForm.packageId,
                      dTrangThaiId: '0'
                    });
                    showNotification(`Đã thêm thành công hội viên: ${newMemberForm.name}`);
                    setShowNewMemberModal(false);
                    setNewMemberForm({ name: '', phone: '', address: '', packageId: 1 });
                    loadCustomers();
                  } catch (err) {
                    showNotification('Lỗi khi thêm hội viên');
                  }
                }}
              >
                <i className="fa-solid fa-floppy-disk"></i> Lưu hội viên
              </button>
            </div>
          </div>
        </div>
      )}

      {/* ================= 4. MODAL ĐĂNG NHẬP CHUẨN TÂN AN PHÁT ================= */}
      <LoginModal
        isOpen={showLoginModal || !currentUser}
        onClose={() => {
          if (currentUser) {
            setShowLoginModal(false);
          } else {
            showNotification('Vui lòng đăng nhập để sử dụng phần mềm!');
          }
        }}
        onLoginSuccess={handleLoginSuccess}
      />

      {/* ================= 5. TOAST NOTIFICATION ================= */}
      <div className={`toast ${showToast ? 'show' : ''}`}>
        <i className="fa-solid fa-circle-check" style={{ color: '#10b981' }}></i>
        <span>{toastMsg}</span>
      </div>
    </div>
  );
}
