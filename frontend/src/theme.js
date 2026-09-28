const THEME_STORAGE_KEY = 'gym_app_theme_index';
const MAIN_BACKGROUND_STORAGE_KEY = 'gym_main_background_index';

export const PROGRAM_THEME_CONFIG_NAME = 'GiaoDienChuongTrinh';
export const MAIN_BACKGROUND_CONFIG_NAME = 'MauNenGiaoDienChinh';

// Hàm tạo SVG Dải Sóng Lụa 3D Tinh Xảo (3D Silk Ribbon Mesh Wave) chuẩn như ảnh mẫu
function generateContentWaveSvg(c1, c2, c3, c4) {
  const svg = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 1440 900" width="100%" height="100%" preserveAspectRatio="none">
    <defs>
      <linearGradient id="baseGrad" x1="0%" y1="0%" x2="100%" y2="100%">
        <stop offset="0%" stop-color="${c1}" />
        <stop offset="50%" stop-color="${c1}" />
        <stop offset="100%" stop-color="${c2}" />
      </linearGradient>

      <!-- Dải lụa góc dưới bên trái (vươn cao từ y:280, rộng từ x:-60 đến 780, uốn lượn mềm mại) -->
      <linearGradient id="ribbonLeft1" x1="0%" y1="100%" x2="85%" y2="15%">
        <stop offset="0%" stop-color="${c4}" stop-opacity="0.95" />
        <stop offset="35%" stop-color="${c3}" stop-opacity="0.8" />
        <stop offset="75%" stop-color="${c2}" stop-opacity="0.3" />
        <stop offset="100%" stop-color="${c1}" stop-opacity="0" />
      </linearGradient>
      <linearGradient id="ribbonLeft2" x1="10%" y1="100%" x2="90%" y2="10%">
        <stop offset="0%" stop-color="${c3}" stop-opacity="0.85" />
        <stop offset="45%" stop-color="${c4}" stop-opacity="0.68" />
        <stop offset="85%" stop-color="${c2}" stop-opacity="0.2" />
        <stop offset="100%" stop-color="${c1}" stop-opacity="0" />
      </linearGradient>
      <linearGradient id="ribbonLeft3" x1="0%" y1="60%" x2="75%" y2="0%">
        <stop offset="0%" stop-color="${c4}" stop-opacity="0.75" />
        <stop offset="45%" stop-color="${c3}" stop-opacity="0.5" />
        <stop offset="100%" stop-color="${c1}" stop-opacity="0" />
      </linearGradient>
      <linearGradient id="ribbonLeft4" x1="0%" y1="100%" x2="70%" y2="30%">
        <stop offset="0%" stop-color="${c3}" stop-opacity="0.9" />
        <stop offset="50%" stop-color="${c4}" stop-opacity="0.75" />
        <stop offset="100%" stop-color="${c2}" stop-opacity="0" />
      </linearGradient>

      <!-- Dải lụa góc trên bên phải -->
      <linearGradient id="ribbonRight1" x1="100%" y1="0%" x2="15%" y2="85%">
        <stop offset="0%" stop-color="${c4}" stop-opacity="0.9" />
        <stop offset="40%" stop-color="${c3}" stop-opacity="0.65" />
        <stop offset="80%" stop-color="${c2}" stop-opacity="0.2" />
        <stop offset="100%" stop-color="${c1}" stop-opacity="0" />
      </linearGradient>
      <linearGradient id="ribbonRight2" x1="100%" y1="20%" x2="25%" y2="95%">
        <stop offset="0%" stop-color="${c3}" stop-opacity="0.75" />
        <stop offset="55%" stop-color="${c4}" stop-opacity="0.45" />
        <stop offset="100%" stop-color="${c1}" stop-opacity="0" />
      </linearGradient>
    </defs>

    <!-- Nền chính -->
    <rect width="100%" height="100%" fill="url(#baseGrad)" />

    <!-- 1. CỤM SÓNG LỤA MỀM MẠI Ở GÓC DƯỚI BÊN TRÁI (Vươn từ y: 280, x: -60 đến 780, chạy xuyên qua cột Trạng thái) -->
    <path d="M-60,320 C140,390 310,540 510,530 C660,520 760,680 680,880 C620,950 480,950 -60,950 Z" fill="url(#ribbonLeft3)" />
    <path d="M-80,420 C120,480 290,610 440,660 C580,710 650,860 520,940 L-80,940 Z" fill="url(#ribbonLeft1)" />
    <path d="M-90,520 C90,560 250,680 380,750 C490,820 460,950 280,950 L-90,950 Z" fill="url(#ribbonLeft2)" />
    <path d="M-100,640 C60,660 210,750 320,830 C410,900 360,960 160,960 L-100,960 Z" fill="url(#ribbonLeft4)" />

    <!-- Các đường sợi sóng mảnh 3D (contour strands) uốn lượn sắc sảo mềm mại xuyên qua cột Trạng thái -->
    <path d="M-50,340 C150,410 320,560 520,550 C670,540 770,700 690,890" fill="none" stroke="${c4}" stroke-width="2.8" stroke-opacity="0.8" />
    <path d="M-50,370 C160,435 335,580 530,570 C675,565 765,720 675,900" fill="none" stroke="${c3}" stroke-width="2.4" stroke-opacity="0.72" />
    <path d="M-50,400 C170,460 350,600 540,590 C680,590 760,740 660,910" fill="none" stroke="${c4}" stroke-width="2.0" stroke-opacity="0.62" />
    <path d="M-50,430 C180,485 365,620 550,610 C685,615 755,760 645,920" fill="none" stroke="${c3}" stroke-width="1.7" stroke-opacity="0.52" />
    <path d="M-50,460 C190,510 380,640 560,630 C690,640 750,780 630,930" fill="none" stroke="${c4}" stroke-width="1.4" stroke-opacity="0.45" />
    <path d="M-50,490 C200,535 395,660 570,650 C695,665 745,800 615,940" fill="none" stroke="${c3}" stroke-width="1.1" stroke-opacity="0.38" />
    <path d="M-50,520 C210,560 410,680 580,670 C700,690 740,820 600,950" fill="none" stroke="${c4}" stroke-width="0.9" stroke-opacity="0.32" />
    <path d="M-50,550 C220,585 425,700 590,690 C705,715 735,840 585,960" fill="none" stroke="${c3}" stroke-width="0.7" stroke-opacity="0.25" />

    <!-- 2. CỤM SÓNG LỤA MỀM MẠI Ở GÓC TRÊN BÊN PHẢI (Vươn từ x: 750 đến 1490, y: -60 đến 380) -->
    <path d="M780,-60 C960,150 1190,260 1500,180 L1500,-60 Z" fill="url(#ribbonRight2)" />
    <path d="M880,-60 C1040,120 1250,220 1500,130 L1500,-60 Z" fill="url(#ribbonRight1)" />

    <!-- Các đường sợi sóng mảnh 3D ở góc trên phải -->
    <path d="M800,-50 C980,140 1210,250 1490,170" fill="none" stroke="${c4}" stroke-width="2.6" stroke-opacity="0.75" />
    <path d="M835,-50 C1010,130 1225,235 1490,155" fill="none" stroke="${c3}" stroke-width="2.1" stroke-opacity="0.65" />
    <path d="M870,-50 C1035,120 1240,220 1490,140" fill="none" stroke="${c4}" stroke-width="1.6" stroke-opacity="0.52" />
    <path d="M905,-50 C1060,110 1255,205 1490,125" fill="none" stroke="${c3}" stroke-width="1.3" stroke-opacity="0.42" />
    <path d="M940,-50 C1085,100 1270,190 1490,110" fill="none" stroke="${c4}" stroke-width="1.0" stroke-opacity="0.35" />
    <path d="M975,-50 C1110,90 1285,175 1490,95" fill="none" stroke="${c3}" stroke-width="0.8" stroke-opacity="0.28" />
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
    <path d="M-50,600 C80,500 120,780 350,650 L350,950 L-50,950 Z" fill="url(#glowWave1)" filter="url(#neonGlow)" />
    <path d="M-50,720 C100,680 180,560 350,780 L350,950 L-50,950 Z" fill="url(#glowWave2)" />
    <path d="M-30,580 C90,480 130,760 340,630" fill="none" stroke="${c4}" stroke-width="2.5" stroke-opacity="0.9" filter="url(#neonGlow)" />
    <path d="M-30,595 C90,495 130,775 340,645" fill="none" stroke="${c4}" stroke-width="1.8" stroke-opacity="0.7" />
    <path d="M-30,610 C90,510 130,790 340,660" fill="none" stroke="${c3}" stroke-width="1.4" stroke-opacity="0.55" />
    <path d="M-30,625 C90,525 130,805 340,675" fill="none" stroke="${c3}" stroke-width="1" stroke-opacity="0.4" />
    <path d="M-30,750 C120,700 180,600 340,810" fill="none" stroke="${c4}" stroke-width="1.5" stroke-opacity="0.6" />
  </svg>`;
  return `data:image/svg+xml;utf8,${encodeURIComponent(svg)}`;
}

// ==============================================================
// 1. NỀN PHẦN NỘI DUNG (BÊN PHẢI) - Khớp thứ tự OTHERCONFIG trong CSDL
// ==============================================================
export const MAIN_BACKGROUND_COLORS = [
  // 8 Mẫu Wave Sóng Lượn Hiện Đại (Light Pastel)
  {
    group: 'SÓNG 3D',
    name: 'Mẫu 1 (Xanh dương)',
    hex: '#F5F9FF',
    textColor: '#1e3a8a',
    bgUrl: generateContentWaveSvg('#F5F9FF', '#E0EDFF', '#60A5FA', '#2563EB')
  },
  {
    group: 'SÓNG 3D',
    name: 'Mẫu 2 (Xanh mềm)',
    hex: '#F4FAFA',
    textColor: '#0f766e',
    bgUrl: generateContentWaveSvg('#F4FAFA', '#E0F2F1', '#38BDF8', '#0284C7')
  },
  {
    group: 'SÓNG 3D',
    name: 'Mẫu 3 (Tím)',
    hex: '#FAF8FF',
    textColor: '#2e1065',
    bgUrl: generateContentWaveSvg('#FAF8FF', '#EDE9FE', '#C084FC', '#9333EA')
  },
  {
    group: 'SÓNG 3D',
    name: 'Mẫu 4 (Cam)',
    hex: '#FFF9F2',
    textColor: '#431407',
    bgUrl: generateContentWaveSvg('#FFF9F2', '#FFEDD5', '#FB923C', '#EA580C')
  },
  {
    group: 'SÓNG 3D',
    name: 'Mẫu 5 (Xanh bạc hà)',
    hex: '#F3FDF9',
    textColor: '#064e3b',
    bgUrl: generateContentWaveSvg('#F3FDF9', '#DCFCE7', '#34D399', '#059669')
  },
  {
    group: 'SÓNG 3D',
    name: 'Mẫu 6 (Hồng phấn)',
    hex: '#FFF5F7',
    textColor: '#831843',
    bgUrl: generateContentWaveSvg('#FFF5F7', '#FFE4E9', '#FF8FA3', '#F43F5E')
  },
  {
    group: 'SÓNG 3D',
    name: 'Mẫu 7 (Cyan)',
    hex: '#F6FDFF',
    textColor: '#0c4a6e',
    bgUrl: generateContentWaveSvg('#F6FDFF', '#E0F2FE', '#38BDF8', '#0284C7')
  },
  {
    group: 'SÓNG 3D',
    name: 'Mẫu 8 (Trắng)',
    hex: '#FFFFFF',
    textColor: '#1e293b',
    bgUrl: generateContentWaveSvg('#FFFFFF', '#EFF6FF', '#93C5FD', '#3B82F6')
  },
  // Các màu đơn sắc cổ điển
  { group: 'ĐƠN SẮC', name: 'Trắng', hex: '#FFFFFF', textColor: '#172033' },
  { group: 'ĐƠN SẮC', name: 'Bạc', hex: '#C0C0C0', textColor: '#172033' },
  { group: 'ĐƠN SẮC', name: 'Xám', hex: '#808080', textColor: '#FFFFFF' },
  { group: 'ĐƠN SẮC', name: 'Xanh lá', hex: '#22C55E', textColor: '#FFFFFF' },
  { group: 'ĐƠN SẮC', name: 'Xanh dương', hex: '#0066FF', textColor: '#FFFFFF' },
  { group: 'ĐƠN SẮC', name: 'Đỏ', hex: '#FF0000', textColor: '#FFFFFF' },
  { group: 'ĐƠN SẮC', name: 'Cam', hex: '#FF7A00', textColor: '#FFFFFF' },
  { group: 'ĐƠN SẮC', name: 'Hồng', hex: '#FF69B4', textColor: '#172033' },
  { group: 'ĐƠN SẮC', name: 'Tím', hex: '#800080', textColor: '#FFFFFF' },
  { group: 'ĐƠN SẮC', name: 'Vàng', hex: '#FFFF00', textColor: '#172033' }
];

// ==============================================================
// 2. NỀN SIDEBAR (BÊN TRÁI) - Khớp thứ tự OTHERCONFIG trong CSDL
// ==============================================================
export const PROGRAM_THEMES = [
  // 8 Mẫu Wave Sóng Lượn Neon Phát Sáng Tối Màu (Dark Neon)
  {
    name: 'Mẫu 1 (Xanh dương)',
    slug: 'sidebar-wave-blue',
    colors: {
      primary: '#3B82F6', primaryHover: '#2563eb', primarySoft: '#e0edff',
      appBg: '#0A1F44', surface: '#ffffff', surfaceMuted: '#f5f8fb',
      border: '#c7d3e1', text: '#1e293b', textMuted: '#64748b',
      sidebar: '#0A1F44', sidebarHeader: '#0F2A5F', sidebarActive: 'rgba(59, 130, 246, 0.28)', sidebarText: '#e0edff',
      sidebarBgUrl: generateSidebarWaveSvg('#0A1F44', '#0F2A5F', '#1E40AF', '#3B82F6')
    }
  },
  {
    name: 'Mẫu 2 (Tím)',
    slug: 'sidebar-wave-purple',
    colors: {
      primary: '#A855F7', primaryHover: '#9333ea', primarySoft: '#f3e8ff',
      appBg: '#1A0B3D', surface: '#ffffff', surfaceMuted: '#f5f8fb',
      border: '#c7d3e1', text: '#1e293b', textMuted: '#64748b',
      sidebar: '#1A0B3D', sidebarHeader: '#4C1D95', sidebarActive: 'rgba(168, 85, 247, 0.28)', sidebarText: '#f3e8ff',
      sidebarBgUrl: generateSidebarWaveSvg('#1A0B3D', '#4C1D95', '#7C3AED', '#A855F7')
    }
  },
  {
    name: 'Mẫu 3 (Cam)',
    slug: 'sidebar-wave-orange',
    colors: {
      primary: '#F97316', primaryHover: '#ea580c', primarySoft: '#ffedd5',
      appBg: '#3B1E0A', surface: '#ffffff', surfaceMuted: '#f5f8fb',
      border: '#c7d3e1', text: '#1e293b', textMuted: '#64748b',
      sidebar: '#3B1E0A', sidebarHeader: '#7C2D12', sidebarActive: 'rgba(249, 115, 22, 0.28)', sidebarText: '#ffedd5',
      sidebarBgUrl: generateSidebarWaveSvg('#3B1E0A', '#7C2D12', '#EA580C', '#F97316')
    }
  },
  {
    name: 'Mẫu 4 (Xanh lá)',
    slug: 'sidebar-wave-green',
    colors: {
      primary: '#34D399', primaryHover: '#10b981', primarySoft: '#d1fae5',
      appBg: '#052E2B', surface: '#ffffff', surfaceMuted: '#f5f8fb',
      border: '#c7d3e1', text: '#1e293b', textMuted: '#64748b',
      sidebar: '#052E2B', sidebarHeader: '#065F46', sidebarActive: 'rgba(52, 211, 153, 0.28)', sidebarText: '#d1fae5',
      sidebarBgUrl: generateSidebarWaveSvg('#052E2B', '#065F46', '#10B981', '#34D399')
    }
  },
  {
    name: 'Mẫu 5 (Đỏ/Hồng)',
    slug: 'sidebar-wave-red',
    colors: {
      primary: '#F43F5E', primaryHover: '#e11d48', primarySoft: '#ffe4e6',
      appBg: '#4C0519', surface: '#ffffff', surfaceMuted: '#f5f8fb',
      border: '#c7d3e1', text: '#1e293b', textMuted: '#64748b',
      sidebar: '#4C0519', sidebarHeader: '#9F1239', sidebarActive: 'rgba(244, 63, 94, 0.28)', sidebarText: '#ffe4e6',
      sidebarBgUrl: generateSidebarWaveSvg('#4C0519', '#9F1239', '#E11D48', '#F43F5E')
    }
  },
  {
    name: 'Mẫu 6 (Xám/Trung tính)',
    slug: 'sidebar-wave-gray',
    colors: {
      primary: '#9CA3AF', primaryHover: '#6b7280', primarySoft: '#f3f4f6',
      appBg: '#111827', surface: '#ffffff', surfaceMuted: '#f5f8fb',
      border: '#c7d3e1', text: '#1e293b', textMuted: '#64748b',
      sidebar: '#111827', sidebarHeader: '#374151', sidebarActive: 'rgba(156, 163, 175, 0.28)', sidebarText: '#f3f4f6',
      sidebarBgUrl: generateSidebarWaveSvg('#111827', '#374151', '#6B7280', '#9CA3AF')
    }
  },
  {
    name: 'Mẫu 7 (Cyan)',
    slug: 'sidebar-wave-cyan',
    colors: {
      primary: '#22D3EE', primaryHover: '#0284c7', primarySoft: '#e0f2fe',
      appBg: '#081C2E', surface: '#ffffff', surfaceMuted: '#f5f8fb',
      border: '#c7d3e1', text: '#1e293b', textMuted: '#64748b',
      sidebar: '#081C2E', sidebarHeader: '#0E3A5E', sidebarActive: 'rgba(34, 211, 238, 0.28)', sidebarText: '#e0f2fe',
      sidebarBgUrl: generateSidebarWaveSvg('#081C2E', '#0E3A5E', '#0284C7', '#22D3EE')
    }
  },
  {
    name: 'Mẫu 8 (Midnight)',
    slug: 'sidebar-wave-indigo',
    colors: {
      primary: '#6366F1', primaryHover: '#3b82f6', primarySoft: '#e0e7ff',
      appBg: '#0B1A3A', surface: '#ffffff', surfaceMuted: '#f5f8fb',
      border: '#c7d3e1', text: '#1e293b', textMuted: '#64748b',
      sidebar: '#0B1A3A', sidebarHeader: '#14245A', sidebarActive: 'rgba(99, 102, 241, 0.28)', sidebarText: '#e0e7ff',
      sidebarBgUrl: generateSidebarWaveSvg('#0B1A3A', '#14245A', '#3B82F6', '#6366F1')
    }
  },
  // Các giao diện cổ điển
  {
    name: 'Office 2010 Blue',
    slug: 'office-2010-blue',
    colors: {
      primary: '#2f74d0', primaryHover: '#245da8', primarySoft: '#e8f1fc',
      appBg: '#eef3f8', surface: '#ffffff', surfaceMuted: '#f5f8fb',
      border: '#c7d3e1', text: '#1e293b', textMuted: '#64748b',
      sidebar: '#19283d', sidebarHeader: '#142033', sidebarActive: '#274a75', sidebarText: '#aebed1'
    }
  },
  {
    name: 'Office 2010 Silver',
    slug: 'office-2010-silver',
    colors: {
      primary: '#667b91', primaryHover: '#4f6174', primarySoft: '#e9edf1',
      appBg: '#e8ebef', surface: '#ffffff', surfaceMuted: '#f3f4f6',
      border: '#b9c1ca', text: '#27313b', textMuted: '#66717d',
      sidebar: '#3f4852', sidebarHeader: '#343c45', sidebarActive: '#596774', sidebarText: '#d1d7dd'
    }
  },
  {
    name: 'Office 2010 Black',
    slug: 'office-2010-black',
    colors: {
      primary: '#3f4852', primaryHover: '#252b31', primarySoft: '#e7e9eb',
      appBg: '#dfe2e5', surface: '#ffffff', surfaceMuted: '#f0f1f2',
      border: '#aeb4ba', text: '#202428', textMuted: '#626a72',
      sidebar: '#1f2328', sidebarHeader: '#15181c', sidebarActive: '#3a4148', sidebarText: '#c4c9ce'
    }
  }
];

const clampThemeIndex = (value) => {
  const index = Number.parseInt(value, 10);
  return Number.isInteger(index) && index >= 0 && index < PROGRAM_THEMES.length ? index : 0;
};

const clampBackgroundIndex = (value) => {
  const index = Number.parseInt(value, 10);
  return Number.isInteger(index) && index >= 0 && index < MAIN_BACKGROUND_COLORS.length ? index : 0;
};

const getContrastColor = (hex) => {
  if (!hex || typeof hex !== 'string') return '#172033';
  const value = hex.replace('#', '');
  const red = Number.parseInt(value.slice(0, 2), 16) || 0;
  const green = Number.parseInt(value.slice(2, 4), 16) || 0;
  const blue = Number.parseInt(value.slice(4, 6), 16) || 0;
  const luminance = (red * 299 + green * 587 + blue * 114) / 1000;
  return luminance >= 145 ? '#172033' : '#FFFFFF';
};

/**
 * Áp dụng Nền Sidebar (Giao diện chương trình)
 */
export function applyProgramTheme(value, { persist = false } = {}) {
  const index = clampThemeIndex(value);
  const theme = PROGRAM_THEMES[index];
  const root = document.documentElement;

  root.dataset.appTheme = theme.slug;
  root.style.setProperty('--theme-primary', theme.colors.primary);
  root.style.setProperty('--theme-primary-hover', theme.colors.primaryHover);
  root.style.setProperty('--theme-primary-soft', theme.colors.primarySoft);
  root.style.setProperty('--theme-app-bg', theme.colors.appBg);
  root.style.setProperty('--theme-surface', theme.colors.surface);
  root.style.setProperty('--theme-surface-muted', theme.colors.surfaceMuted);
  root.style.setProperty('--theme-border', theme.colors.border);
  root.style.setProperty('--theme-text', theme.colors.text);
  root.style.setProperty('--theme-text-muted', theme.colors.textMuted);
  root.style.setProperty('--theme-sidebar', theme.colors.sidebar);
  root.style.setProperty('--theme-sidebar-header', theme.colors.sidebarHeader);
  root.style.setProperty('--theme-sidebar-active', theme.colors.sidebarActive);
  root.style.setProperty('--theme-sidebar-text', theme.colors.sidebarText);

  if (theme.colors.sidebarBgUrl) {
    root.style.setProperty('--theme-sidebar-bg', `url("${theme.colors.sidebarBgUrl}")`);
  } else {
    root.style.removeProperty('--theme-sidebar-bg');
  }

  if (persist) localStorage.setItem(THEME_STORAGE_KEY, String(index));
  return theme;
}

/**
 * Áp dụng Nền Nội Dung (Màu nền giao diện chính bên phải)
 */
export function applyMainBackground(value, { persist = false } = {}) {
  const index = clampBackgroundIndex(value);
  const color = MAIN_BACKGROUND_COLORS[index];
  const root = document.documentElement;

  root.dataset.mainBackground = `bg-preset-${index}`;
  root.style.setProperty('--theme-main-bg', color.hex);
  root.style.setProperty('--theme-main-bg-text', color.textColor || getContrastColor(color.hex));

  if (color.bgUrl) {
    root.style.setProperty('--theme-content-bg', `url("${color.bgUrl}")`);
  } else {
    root.style.removeProperty('--theme-content-bg');
  }

  if (persist) localStorage.setItem(MAIN_BACKGROUND_STORAGE_KEY, String(index));
  return color;
}

export function initializeProgramTheme() {
  applyMainBackground(localStorage.getItem(MAIN_BACKGROUND_STORAGE_KEY) ?? 5);
  return applyProgramTheme(localStorage.getItem(THEME_STORAGE_KEY) ?? 4);
}

export function findProgramThemeItem(groups = []) {
  for (const group of groups) {
    const item = group.items?.find((config) => config.name === PROGRAM_THEME_CONFIG_NAME);
    if (item) return item;
  }
  return null;
}

export function findMainBackgroundItem(groups = []) {
  for (const group of groups) {
    const item = group.items?.find((config) => config.name === MAIN_BACKGROUND_CONFIG_NAME);
    if (item) return item;
  }
  return null;
}

export function applyProgramThemeFromConfigs(groups = [], values = null, options = {}) {
  const item = findProgramThemeItem(groups);
  if (!item) return null;
  const value = values?.[item.id]?.intValue ?? item.intValue ?? 0;
  return applyProgramTheme(value, options);
}

export function applyMainBackgroundFromConfigs(groups = [], values = null, options = {}) {
  const item = findMainBackgroundItem(groups);
  if (!item) return null;
  const value = values?.[item.id]?.intValue ?? item.intValue ?? 0;
  return applyMainBackground(value, options);
}
