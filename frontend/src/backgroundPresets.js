/**
 * Bộ sưu tập mẫu nền Wave 3D hiện đại cho phần Nội dung (Bên phải) và Sidebar (Bên trái)
 * Chuẩn xác 100% theo các mã màu HEX và họa tiết sóng lượn người dùng cung cấp.
 */

// Hàm tạo SVG Wave cho Nền Nội Dung Sáng (Light Pastel Wave)
function generateContentWaveSvg(c1, c2, c3, c4) {
  const svg = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 1440 900" width="100%" height="100%" preserveAspectRatio="none">
    <defs>
      <linearGradient id="baseGrad" x1="0%" y1="0%" x2="100%" y2="100%">
        <stop offset="0%" stop-color="${c1}" />
        <stop offset="50%" stop-color="${c2}" />
        <stop offset="100%" stop-color="${c3}" />
      </linearGradient>
      <linearGradient id="wave1Grad" x1="100%" y1="0%" x2="0%" y2="100%">
        <stop offset="0%" stop-color="${c4}" stop-opacity="0.8" />
        <stop offset="60%" stop-color="${c3}" stop-opacity="0.5" />
        <stop offset="100%" stop-color="${c2}" stop-opacity="0" />
      </linearGradient>
      <linearGradient id="wave2Grad" x1="0%" y1="50%" x2="100%" y2="50%">
        <stop offset="0%" stop-color="${c3}" stop-opacity="0.6" />
        <stop offset="100%" stop-color="${c4}" stop-opacity="0.2" />
      </linearGradient>
    </defs>
    <rect width="100%" height="100%" fill="url(#baseGrad)" />
    <!-- Sóng cong nền lớn -->
    <path d="M-100,500 C300,750 600,300 1100,650 C1300,780 1500,600 1600,550 L1600,950 L-100,950 Z" fill="url(#wave1Grad)" />
    <path d="M-100,300 C400,200 700,700 1200,400 C1400,280 1550,450 1600,480 L1600,950 L-100,950 Z" fill="url(#wave2Grad)" />
    <!-- Các đường line sóng 3D mượt mà -->
    <path d="M-50,420 C350,680 650,220 1150,580 C1320,700 1480,560 1600,520" fill="none" stroke="${c4}" stroke-width="1.8" stroke-opacity="0.45" />
    <path d="M-50,440 C350,700 650,240 1150,600 C1320,720 1480,580 1600,540" fill="none" stroke="${c4}" stroke-width="1.2" stroke-opacity="0.3" />
    <path d="M-50,460 C350,720 650,260 1150,620 C1320,740 1480,600 1600,560" fill="none" stroke="${c4}" stroke-width="0.8" stroke-opacity="0.2" />
    <path d="M-50,280 C420,180 720,680 1220,380 C1420,260 1550,430 1600,460" fill="none" stroke="${c3}" stroke-width="1.5" stroke-opacity="0.4" />
    <path d="M-50,300 C420,200 720,700 1220,400 C1420,280 1550,450 1600,480" fill="none" stroke="${c3}" stroke-width="1" stroke-opacity="0.25" />
  </svg>`;
  return `data:image/svg+xml;utf8,${encodeURIComponent(svg)}`;
}

// Hàm tạo SVG Wave cho Nền Sidebar Tối (Dark Glow Neon Wave)
function generateSidebarWaveSvg(c1, c2, c3, c4) {
  const svg = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 320 900" width="100%" height="100%" preserveAspectRatio="none">
    <defs>
      <linearGradient id="darkBase" x1="0%" y1="0%" x2="0%" y2="100%">
        <stop offset="0%" stop-color="${c1}" />
        <stop offset="60%" stop-color="${c1}" />
        <stop offset="100%" stop-color="${c2}" />
      </linearGradient>
      <linearGradient id="glowWave1" x1="0%" y1="100%" x2="100%" y2="0%">
        <stop offset="0%" stop-color="${c3}" stop-opacity="0.75" />
        <stop offset="50%" stop-color="${c4}" stop-opacity="0.85" />
        <stop offset="100%" stop-color="${c2}" stop-opacity="0" />
      </linearGradient>
      <linearGradient id="glowWave2" x1="100%" y1="100%" x2="0%" y2="0%">
        <stop offset="0%" stop-color="${c4}" stop-opacity="0.7" />
        <stop offset="70%" stop-color="${c3}" stop-opacity="0.3" />
        <stop offset="100%" stop-color="${c1}" stop-opacity="0" />
      </linearGradient>
      <filter id="neonGlow" x="-20%" y="-20%" width="140%" height="140%">
        <feGaussianBlur stdDeviation="8" result="blur" />
        <feMerge>
          <feMergeNode in="blur" />
          <feMergeNode in="SourceGraphic" />
        </feMerge>
      </filter>
    </defs>
    <rect width="100%" height="100%" fill="url(#darkBase)" />
    
    <!-- Dải sóng phát sáng lớn phía dưới -->
    <path d="M-50,600 C80,500 120,780 350,650 L350,950 L-50,950 Z" fill="url(#glowWave1)" filter="url(#neonGlow)" />
    <path d="M-50,720 C100,680 180,560 350,780 L350,950 L-50,950 Z" fill="url(#glowWave2)" />
    
    <!-- Các đường chùm sáng (fiber optic / wave stream) 3D neon -->
    <path d="M-30,580 C90,480 130,760 340,630" fill="none" stroke="${c4}" stroke-width="2.5" stroke-opacity="0.9" filter="url(#neonGlow)" />
    <path d="M-30,595 C90,495 130,775 340,645" fill="none" stroke="${c4}" stroke-width="1.8" stroke-opacity="0.7" />
    <path d="M-30,610 C90,510 130,790 340,660" fill="none" stroke="${c3}" stroke-width="1.4" stroke-opacity="0.55" />
    <path d="M-30,625 C90,525 130,805 340,675" fill="none" stroke="${c3}" stroke-width="1" stroke-opacity="0.4" />
    <path d="M-30,640 C90,540 130,820 340,690" fill="none" stroke="${c4}" stroke-width="0.8" stroke-opacity="0.3" />
    <path d="M-30,655 C90,555 130,835 340,705" fill="none" stroke="${c3}" stroke-width="0.6" stroke-opacity="0.2" />

    <!-- Dải lượn phụ tạo chiều sâu -->
    <path d="M-30,750 C120,700 180,600 340,810" fill="none" stroke="${c4}" stroke-width="1.5" stroke-opacity="0.6" />
    <path d="M-30,765 C120,715 180,615 340,825" fill="none" stroke="${c3}" stroke-width="1" stroke-opacity="0.4" />
  </svg>`;
  return `data:image/svg+xml;utf8,${encodeURIComponent(svg)}`;
}

// ==============================================================
// 1. DANH SÁCH MẪU NỀN PHẦN NỘI DUNG (BÊN PHẢI)
// ==============================================================
export const CONTENT_BG_PRESETS = [
  {
    id: 'content_wave_1',
    name: 'Mẫu 1',
    description: 'Xanh dương sóng lượn tươi sáng',
    colors: ['#F4F8FF', '#E8F1FF', '#DCEAFF', '#CFE3FF'],
    textColor: '#0f2942',
    bgUrl: generateContentWaveSvg('#F4F8FF', '#E8F1FF', '#DCEAFF', '#CFE3FF')
  },
  {
    id: 'content_wave_2',
    name: 'Mẫu 2',
    description: 'Xanh dương mềm êm dịu',
    colors: ['#F8FCFF', '#E6F2FF', '#D5E6FF', '#BFD9FF'],
    textColor: '#0f2942',
    bgUrl: generateContentWaveSvg('#F8FCFF', '#E6F2FF', '#D5E6FF', '#BFD9FF')
  },
  {
    id: 'content_wave_3',
    name: 'Mẫu 3',
    description: 'Tím pastel thanh lịch, cao cấp',
    colors: ['#FAF8FF', '#F0EFFF', '#E5DBFF', '#D6C8FF'],
    textColor: '#2e1065',
    bgUrl: generateContentWaveSvg('#FAF8FF', '#F0EFFF', '#E5DBFF', '#D6C8FF')
  },
  {
    id: 'content_wave_4',
    name: 'Mẫu 4',
    description: 'Cam & hổ phách năng động, ấm áp',
    colors: ['#FFF9F2', '#FFE7D1', '#FFD4A8', '#FFC07A'],
    textColor: '#431407',
    bgUrl: generateContentWaveSvg('#FFF9F2', '#FFE7D1', '#FFD4A8', '#FFC07A')
  },
  {
    id: 'content_wave_5',
    name: 'Mẫu 5',
    description: 'Xanh bạc hà & ngọc lục bảo mát mẻ',
    colors: ['#F3FDF9', '#DFF7ED', '#C8EFDF', '#A7E2CB'],
    textColor: '#064e3b',
    bgUrl: generateContentWaveSvg('#F3FDF9', '#DFF7ED', '#C8EFDF', '#A7E2CB')
  },
  {
    id: 'content_wave_6',
    name: 'Mẫu 6',
    description: 'Hồng phấn dịu dàng, quý phái',
    colors: ['#FFF5F7', '#FFE2E8', '#FFC9D6', '#FFAEC2'],
    textColor: '#831843',
    bgUrl: generateContentWaveSvg('#FFF5F7', '#FFE2E8', '#FFC9D6', '#FFAEC2')
  },
  // Bổ sung từ ảnh 2
  {
    id: 'content_wave_cyan',
    name: 'Mẫu 7 (Cyan)',
    description: 'Xanh băng tuyết tinh khôi',
    colors: ['#F6FDFF', '#E8F6FF', '#D6F0FF', '#BAE6FD'],
    textColor: '#0c4a6e',
    bgUrl: generateContentWaveSvg('#F6FDFF', '#E8F6FF', '#D6F0FF', '#BAE6FD')
  },
  {
    id: 'content_wave_white',
    name: 'Mẫu 8 (Trắng tinh)',
    description: 'Trắng sứ thanh khiết tối giản',
    colors: ['#FFFFFF', '#F1F7FF', '#DCEBFF', '#BFDBFE'],
    textColor: '#1e293b',
    bgUrl: generateContentWaveSvg('#FFFFFF', '#F1F7FF', '#DCEBFF', '#BFDBFE')
  }
];

// ==============================================================
// 2. DANH SÁCH MẪU NỀN SIDEBAR (BÊN TRÁI)
// ==============================================================
export const SIDEBAR_BG_PRESETS = [
  {
    id: 'sidebar_wave_blue',
    name: 'Mẫu 1 (Xanh dương)',
    tag: 'Xanh dương',
    description: 'Xanh dương dạ quang công nghệ hiện đại',
    colors: ['#0A1F44', '#0F2A5F', '#1E40AF', '#3B82F6'],
    primaryColor: '#3B82F6',
    activeBg: 'rgba(59, 130, 246, 0.28)',
    textColor: '#e0edff',
    bgUrl: generateSidebarWaveSvg('#0A1F44', '#0F2A5F', '#1E40AF', '#3B82F6')
  },
  {
    id: 'sidebar_wave_purple',
    name: 'Mẫu 2 (Tím)',
    tag: 'Tím',
    description: 'Tím huyền bí sang trọng, đẳng cấp',
    colors: ['#1A0B3D', '#4C1D95', '#7C3AED', '#A855F7'],
    primaryColor: '#A855F7',
    activeBg: 'rgba(168, 85, 247, 0.28)',
    textColor: '#f3e8ff',
    bgUrl: generateSidebarWaveSvg('#1A0B3D', '#4C1D95', '#7C3AED', '#A855F7')
  },
  {
    id: 'sidebar_wave_orange',
    name: 'Mẫu 3 (Cam)',
    tag: 'Cam',
    description: 'Cam lửa mạnh mẽ, nhiệt huyết thể thao',
    colors: ['#3B1E0A', '#7C2D12', '#EA580C', '#F97316'],
    primaryColor: '#F97316',
    activeBg: 'rgba(249, 115, 22, 0.28)',
    textColor: '#ffedd5',
    bgUrl: generateSidebarWaveSvg('#3B1E0A', '#7C2D12', '#EA580C', '#F97316')
  },
  {
    id: 'sidebar_wave_green',
    name: 'Mẫu 4 (Xanh lá)',
    tag: 'Xanh lá',
    description: 'Xanh ngọc lục bảo tươi mát, sinh lực',
    colors: ['#052E2B', '#065F46', '#10B981', '#34D399'],
    primaryColor: '#34D399',
    activeBg: 'rgba(52, 211, 153, 0.28)',
    textColor: '#d1fae5',
    bgUrl: generateSidebarWaveSvg('#052E2B', '#065F46', '#10B981', '#34D399')
  },
  {
    id: 'sidebar_wave_red',
    name: 'Mẫu 5 (Đỏ/Hồng)',
    tag: 'Đỏ/Hồng',
    description: 'Đỏ hồng Ruby cuốn hút, quyền lực',
    colors: ['#4C0519', '#9F1239', '#E11D48', '#F43F5E'],
    primaryColor: '#F43F5E',
    activeBg: 'rgba(244, 63, 94, 0.28)',
    textColor: '#ffe4e6',
    bgUrl: generateSidebarWaveSvg('#4C0519', '#9F1239', '#E11D48', '#F43F5E')
  },
  {
    id: 'sidebar_wave_gray',
    name: 'Mẫu 6 (Xám/Trung tính)',
    tag: 'Xám/Trung tính',
    description: 'Xám Titan trung tính, trầm ổn & tinh tế',
    colors: ['#111827', '#374151', '#6B7280', '#9CA3AF'],
    primaryColor: '#9CA3AF',
    activeBg: 'rgba(156, 163, 175, 0.28)',
    textColor: '#f3f4f6',
    bgUrl: generateSidebarWaveSvg('#111827', '#374151', '#6B7280', '#9CA3AF')
  },
  // Bổ sung từ ảnh 2
  {
    id: 'sidebar_wave_cyan',
    name: 'Mẫu 7 (Cyan Biển sâu)',
    tag: 'Cyan',
    description: 'Xanh biển sâu sâu thẳm, phát sáng neon',
    colors: ['#081C2E', '#0E3A5E', '#0284C7', '#22D3EE'],
    primaryColor: '#22D3EE',
    activeBg: 'rgba(34, 211, 238, 0.28)',
    textColor: '#e0f2fe',
    bgUrl: generateSidebarWaveSvg('#081C2E', '#0E3A5E', '#0284C7', '#22D3EE')
  },
  {
    id: 'sidebar_wave_indigo',
    name: 'Mẫu 8 (Midnight Navy)',
    tag: 'Indigo',
    description: 'Xanh tím Midnight chuyển màu sống động',
    colors: ['#0B1A3A', '#14245A', '#3B82F6', '#6366F1'],
    primaryColor: '#6366F1',
    activeBg: 'rgba(99, 102, 241, 0.28)',
    textColor: '#e0e7ff',
    bgUrl: generateSidebarWaveSvg('#0B1A3A', '#14245A', '#3B82F6', '#6366F1')
  }
];

export const STORAGE_KEY_CONTENT_BG = 'gym_preset_content_bg_id';
export const STORAGE_KEY_SIDEBAR_BG = 'gym_preset_sidebar_bg_id';

/**
 * Áp dụng hình nền lên hệ thống
 */
export function applyWaveBackgroundPreset({ contentBgId, sidebarBgId, persist = false } = {}) {
  const root = document.documentElement;

  if (contentBgId) {
    const contentPreset = CONTENT_BG_PRESETS.find(p => p.id === contentBgId) || CONTENT_BG_PRESETS[0];
    root.style.setProperty('--theme-content-bg', `url("${contentPreset.bgUrl}")`);
    root.style.setProperty('--theme-main-bg', contentPreset.colors[0]);
    root.style.setProperty('--theme-main-bg-text', contentPreset.textColor);
    if (persist) {
      localStorage.setItem(STORAGE_KEY_CONTENT_BG, contentPreset.id);
    }
  }

  if (sidebarBgId) {
    const sidebarPreset = SIDEBAR_BG_PRESETS.find(p => p.id === sidebarBgId) || SIDEBAR_BG_PRESETS[0];
    root.style.setProperty('--theme-sidebar-bg', `url("${sidebarPreset.bgUrl}")`);
    root.style.setProperty('--theme-sidebar', sidebarPreset.colors[0]);
    root.style.setProperty('--theme-sidebar-header', sidebarPreset.colors[1]);
    root.style.setProperty('--theme-sidebar-active', sidebarPreset.activeBg);
    root.style.setProperty('--theme-sidebar-text', sidebarPreset.textColor);
    root.style.setProperty('--theme-primary', sidebarPreset.primaryColor);
    if (persist) {
      localStorage.setItem(STORAGE_KEY_SIDEBAR_BG, sidebarPreset.id);
    }
  }
}

/**
 * Khởi tạo hình nền từ localStorage khi vào app
 */
export function initializeWaveBackgrounds() {
  const savedContentBgId = localStorage.getItem(STORAGE_KEY_CONTENT_BG) || 'content_wave_1';
  const savedSidebarBgId = localStorage.getItem(STORAGE_KEY_SIDEBAR_BG) || 'sidebar_wave_blue';
  applyWaveBackgroundPreset({
    contentBgId: savedContentBgId,
    sidebarBgId: savedSidebarBgId,
    persist: false
  });
  return {
    contentBgId: savedContentBgId,
    sidebarBgId: savedSidebarBgId
  };
}
