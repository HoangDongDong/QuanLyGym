const THEME_STORAGE_KEY = 'gym_app_theme_index';
const MAIN_BACKGROUND_STORAGE_KEY = 'gym_main_background_index';

export const PROGRAM_THEME_CONFIG_NAME = 'GiaoDienChuongTrinh';
export const MAIN_BACKGROUND_CONFIG_NAME = 'MauNenGiaoDienChinh';

export const MAIN_BACKGROUND_COLORS = [
  { group: 'KIM', name: 'Trắng', hex: '#FFFFFF' },
  { group: 'KIM', name: 'Bạc', hex: '#C0C0C0' },
  { group: 'KIM', name: 'Xám', hex: '#808080' },
  { group: 'KIM', name: 'Ghi sáng', hex: '#D3D3D3' },
  { group: 'KIM', name: 'Vàng kim', hex: '#FFD700' },
  { group: 'MỘC', name: 'Xanh lá', hex: '#22C55E' },
  { group: 'MỘC', name: 'Xanh lục', hex: '#008000' },
  { group: 'MỘC', name: 'Xanh olive', hex: '#808000' },
  { group: 'MỘC', name: 'Xanh rêu', hex: '#556B2F' },
  { group: 'MỘC', name: 'Xanh ngọc', hex: '#00A86B' },
  { group: 'THỦY', name: 'Đen', hex: '#000000' },
  { group: 'THỦY', name: 'Xanh dương', hex: '#0066FF' },
  { group: 'THỦY', name: 'Xanh biển', hex: '#0077BE' },
  { group: 'THỦY', name: 'Navy', hex: '#000080' },
  { group: 'THỦY', name: 'Xanh da trời', hex: '#87CEEB' },
  { group: 'HỎA', name: 'Đỏ', hex: '#FF0000' },
  { group: 'HỎA', name: 'Đỏ đô', hex: '#800020' },
  { group: 'HỎA', name: 'Cam', hex: '#FF7A00' },
  { group: 'HỎA', name: 'Hồng', hex: '#FF69B4' },
  { group: 'HỎA', name: 'Tím', hex: '#800080' },
  { group: 'HỎA', name: 'Tím violet', hex: '#8A2BE2' },
  { group: 'THỔ', name: 'Vàng', hex: '#FFFF00' },
  { group: 'THỔ', name: 'Vàng đất', hex: '#D4A017' },
  { group: 'THỔ', name: 'Nâu', hex: '#8B4513' },
  { group: 'THỔ', name: 'Nâu đất', hex: '#795548' },
  { group: 'THỔ', name: 'Be', hex: '#F5F5DC' },
  { group: 'THỔ', name: 'Kem', hex: '#FFFDD0' }
];

export const PROGRAM_THEMES = [
  {
    name: 'Office 2010 - Blue',
    slug: 'office-2010-blue',
    colors: {
      primary: '#2f74d0', primaryHover: '#245da8', primarySoft: '#e8f1fc',
      appBg: '#eef3f8', surface: '#ffffff', surfaceMuted: '#f5f8fb',
      border: '#c7d3e1', text: '#1e293b', textMuted: '#64748b',
      sidebar: '#19283d', sidebarHeader: '#142033', sidebarActive: '#274a75', sidebarText: '#aebed1'
    }
  },
  {
    name: 'Office 2010 - Silver',
    slug: 'office-2010-silver',
    colors: {
      primary: '#667b91', primaryHover: '#4f6174', primarySoft: '#e9edf1',
      appBg: '#e8ebef', surface: '#ffffff', surfaceMuted: '#f3f4f6',
      border: '#b9c1ca', text: '#27313b', textMuted: '#66717d',
      sidebar: '#3f4852', sidebarHeader: '#343c45', sidebarActive: '#596774', sidebarText: '#d1d7dd'
    }
  },
  {
    name: 'Office 2010 - Black',
    slug: 'office-2010-black',
    colors: {
      primary: '#3f4852', primaryHover: '#252b31', primarySoft: '#e7e9eb',
      appBg: '#dfe2e5', surface: '#ffffff', surfaceMuted: '#f0f1f2',
      border: '#aeb4ba', text: '#202428', textMuted: '#626a72',
      sidebar: '#1f2328', sidebarHeader: '#15181c', sidebarActive: '#3a4148', sidebarText: '#c4c9ce'
    }
  },
  {
    name: 'Professional - System',
    slug: 'professional-system',
    colors: {
      primary: '#0b67a3', primaryHover: '#084d7b', primarySoft: '#e2f0f8',
      appBg: '#edf1f4', surface: '#ffffff', surfaceMuted: '#f5f7f8',
      border: '#bcc8d0', text: '#1f2d36', textMuted: '#60717d',
      sidebar: '#263b49', sidebarHeader: '#1e303b', sidebarActive: '#31566d', sidebarText: '#bfd0da'
    }
  },
  {
    name: 'Professional - Office 2003',
    slug: 'professional-office-2003',
    colors: {
      primary: '#5577b5', primaryHover: '#3e5d96', primarySoft: '#edf1fa',
      appBg: '#f0eee4', surface: '#fffef9', surfaceMuted: '#f6f3e8',
      border: '#c8c3a8', text: '#292d35', textMuted: '#6d6c61',
      sidebar: '#3f5277', sidebarHeader: '#344564', sidebarActive: '#657aa3', sidebarText: '#e1e7f2'
    }
  },
  {
    name: 'Office 2007 - Blue',
    slug: 'office-2007-blue',
    colors: {
      primary: '#3b73b9', primaryHover: '#2b5991', primarySoft: '#e6eff9',
      appBg: '#eaf0f7', surface: '#ffffff', surfaceMuted: '#f3f7fb',
      border: '#b6c8dc', text: '#1d2b3a', textMuted: '#60758b',
      sidebar: '#243f61', sidebarHeader: '#1c334f', sidebarActive: '#315b89', sidebarText: '#bed0e3'
    }
  },
  {
    name: 'Office 2007 - Silver',
    slug: 'office-2007-silver',
    colors: {
      primary: '#71849a', primaryHover: '#56687b', primarySoft: '#e9edf2',
      appBg: '#e9ecef', surface: '#ffffff', surfaceMuted: '#f4f5f6',
      border: '#bdc5cd', text: '#27313a', textMuted: '#68737e',
      sidebar: '#45515e', sidebarHeader: '#39434d', sidebarActive: '#5e6d7c', sidebarText: '#d3d9df'
    }
  },
  {
    name: 'Office 2007 - Black',
    slug: 'office-2007-black',
    colors: {
      primary: '#4b535c', primaryHover: '#30363c', primarySoft: '#e8eaec',
      appBg: '#e1e3e5', surface: '#ffffff', surfaceMuted: '#f1f2f3',
      border: '#b2b7bc', text: '#202428', textMuted: '#646b72',
      sidebar: '#22262b', sidebarHeader: '#171a1e', sidebarActive: '#424950', sidebarText: '#c8cdd2'
    }
  },
  {
    name: 'Sparkle - Blue',
    slug: 'sparkle-blue',
    colors: {
      primary: '#0099bd', primaryHover: '#007792', primarySoft: '#e1f6fa',
      appBg: '#eaf5f7', surface: '#ffffff', surfaceMuted: '#f2fbfc',
      border: '#aed5dd', text: '#15323a', textMuted: '#57737a',
      sidebar: '#163c49', sidebarHeader: '#102f3a', sidebarActive: '#176179', sidebarText: '#b6d9e1'
    }
  },
  {
    name: 'Sparkle - Orange',
    slug: 'sparkle-orange',
    colors: {
      primary: '#e36f22', primaryHover: '#b95416', primarySoft: '#fff0e5',
      appBg: '#f8f0e9', surface: '#ffffff', surfaceMuted: '#fff8f3',
      border: '#e3c4ad', text: '#38271d', textMuted: '#7d6759',
      sidebar: '#4a3024', sidebarHeader: '#38231a', sidebarActive: '#7a452a', sidebarText: '#ead0bf'
    }
  },
  {
    name: 'Office 2010 - Blue - Không áp dụng viền cửa',
    slug: 'office-2010-blue-borderless',
    colors: {
      primary: '#2f74d0', primaryHover: '#245da8', primarySoft: '#e8f1fc',
      appBg: '#eef3f8', surface: '#ffffff', surfaceMuted: '#f5f8fb',
      border: 'transparent', text: '#1e293b', textMuted: '#64748b',
      sidebar: '#19283d', sidebarHeader: '#142033', sidebarActive: '#274a75', sidebarText: '#aebed1'
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
  const value = hex.replace('#', '');
  const red = Number.parseInt(value.slice(0, 2), 16);
  const green = Number.parseInt(value.slice(2, 4), 16);
  const blue = Number.parseInt(value.slice(4, 6), 16);
  const luminance = (red * 299 + green * 587 + blue * 114) / 1000;
  return luminance >= 145 ? '#172033' : '#FFFFFF';
};

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

  if (persist) localStorage.setItem(THEME_STORAGE_KEY, String(index));
  return theme;
}

export function initializeProgramTheme() {
  applyMainBackground(localStorage.getItem(MAIN_BACKGROUND_STORAGE_KEY) ?? 0);
  return applyProgramTheme(localStorage.getItem(THEME_STORAGE_KEY) ?? 0);
}

export function applyMainBackground(value, { persist = false } = {}) {
  const index = clampBackgroundIndex(value);
  const color = MAIN_BACKGROUND_COLORS[index];
  const root = document.documentElement;

  root.dataset.mainBackground = `${color.group.toLowerCase()}-${index}`;
  root.style.setProperty('--theme-main-bg', color.hex);
  root.style.setProperty('--theme-main-bg-text', getContrastColor(color.hex));

  if (persist) localStorage.setItem(MAIN_BACKGROUND_STORAGE_KEY, String(index));
  return color;
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
