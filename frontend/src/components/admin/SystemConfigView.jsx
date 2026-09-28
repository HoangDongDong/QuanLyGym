import React, { useState, useEffect } from 'react';
import { adminService } from '../../services/adminService';
import {
  applyMainBackground,
  applyMainBackgroundFromConfigs,
  applyProgramTheme,
  applyProgramThemeFromConfigs,
  findMainBackgroundItem,
  findProgramThemeItem,
  MAIN_BACKGROUND_COLORS,
  MAIN_BACKGROUND_CONFIG_NAME
} from '../../theme';

// Icon tương ứng cho từng nhóm theo bản vẽ desktop
const GROUP_ICONS = {
  'Thông tin chung': 'fa-solid fa-house',
  'Mẫu hóa đơn': 'fa-solid fa-receipt',
  'Máy in': 'fa-solid fa-print',
  'Số phiếu': 'fa-solid fa-hashtag',
  'Gym': 'fa-solid fa-dumbbell',
  'Ghi chú hoá đơn': 'fa-regular fa-note-sticky',
  'Bán hàng': 'fa-solid fa-cart-shopping',
  'Thanh toán': 'fa-solid fa-money-bill-wave',
  'Tích điểm': 'fa-solid fa-coins',
  'Kho hàng': 'fa-solid fa-warehouse',
  'Mặt hàng': 'fa-solid fa-boxes-stacked',
  'Cây hiển thị giá': 'fa-solid fa-tv',
  'Cân điện tử': 'fa-solid fa-scale-balanced',
  'Cảnh báo': 'fa-solid fa-bell',
  'Bảo mật': 'fa-solid fa-lock',
  'Quản trị': 'fa-solid fa-user-shield',
  'Thiết bị': 'fa-solid fa-network-wired'
};

// Danh mục định dạng sinh mã số phiếu theo chuẩn hệ thống
const DEFAULT_TICKET_MASKS = [
  { id: 'ticket_kh', name: 'SoPhieuKhachHang', caption: 'Khách hàng', controlType: 5, textValue: '' },
  { id: 'ticket_bg', name: 'SoPhieuBaoGia', caption: 'Báo giá', controlType: 5, textValue: 'BG(yy)/(*****)' },
  { id: 'ticket_dh', name: 'SoPhieuDonHang', caption: 'Đơn hàng', controlType: 5, textValue: '(yy)(******)' },
  { id: 'ticket_pn', name: 'SoPhieuNhapKho', caption: 'Phiếu nhập kho', controlType: 5, textValue: 'PN(yy)/(*****)' },
  { id: 'ticket_px', name: 'SoPhieuXuatKho', caption: 'Phiếu xuất kho', controlType: 5, textValue: 'PX(yy)/(*****)' },
  { id: 'ticket_pck', name: 'SoPhieuChuyenKho', caption: 'Phiếu chuyển kho', controlType: 5, textValue: 'PCK(yy)/(*****)' },
  { id: 'ticket_pkk', name: 'SoPhieuKiemKe', caption: 'Phiếu kiểm kê', controlType: 5, textValue: 'PKK(yy)/(*****)' },
  { id: 'ticket_dathang', name: 'SoPhieuDatHang', caption: 'Đặt hàng', controlType: 5, textValue: 'DH(yy)/(*****)' },
  { id: 'ticket_pt', name: 'SoPhieuThu', caption: 'Phiếu thu', controlType: 5, textValue: 'PT(yy)/(*****)' },
  { id: 'ticket_pc', name: 'SoPhieuChi', caption: 'Phiếu chi', controlType: 5, textValue: 'PC(yy)/(*****)' },
  { id: 'ticket_ptcn', name: 'SoPhieuThuCongNo', caption: 'Phiếu thu công nợ', controlType: 5, textValue: 'PTCN(yy)/(*****)' },
  { id: 'ticket_dc', name: 'SoPhieuDatCoc', caption: 'Đặt cọc', controlType: 5, textValue: 'DC(yy)/(******)' },
  { id: 'ticket_bluong', name: 'SoPhieuBangLuong', caption: 'Bảng lương', controlType: 5, textValue: '' },
  { id: 'ticket_gh', name: 'SoPhieuGiaHan', caption: 'Gia hạn thẻ', controlType: 5, textValue: 'GH(yy)/(*****)' },
  { id: 'ticket_bl', name: 'SoPhieuBaoLuu', caption: 'Bảo lưu thẻ', controlType: 5, textValue: 'BL(yy)/(*****)' },
  { id: 'ticket_dt', name: 'SoPhieuDoiLoai', caption: 'Đổi loại thẻ', controlType: 5, textValue: 'DT(yy)/(*****)' },
];

export default function SystemConfigView({ showNotification }) {
  const [configGroups, setConfigGroups] = useState([]);
  const [loading, setLoading] = useState(false);
  const [activeGroupIndex, setActiveGroupIndex] = useState(0);
  const [configValues, setConfigValues] = useState({});
  const [isSaving, setIsSaving] = useState(false);
  const [searchFilter, setSearchFilter] = useState('');
  const [modifiedIds, setModifiedIds] = useState(new Set());

  const fileInputRef = React.useRef(null);

  const fetchConfigs = async () => {
    setLoading(true);
    try {
      const res = await adminService.getConfigs();
      if (res && res.data) {
        setConfigGroups(res.data);

        // Khởi tạo map giá trị ban đầu cho tất cả các field
        const map = {};
        res.data.forEach((group) => {
          group.items?.forEach((item) => {
            map[item.id] = {
              id: item.id,
              textValue: item.textValue ?? '',
              intValue: item.intValue ?? 0,
              decimalValue: item.decimalValue ?? 0,
              dateTimeValue: item.dateTimeValue ?? '',
              blobValue: item.blobValue ?? null
            };
          });
        });

        // Điền giá trị mặc định cho số phiếu nếu chưa có
        DEFAULT_TICKET_MASKS.forEach((tm) => {
          if (!map[tm.id]) {
            map[tm.id] = {
              id: tm.id,
              textValue: tm.textValue,
              intValue: 0,
              decimalValue: 0,
              dateTimeValue: '',
              blobValue: null
            };
          }
        });

        setConfigValues(map);
        setModifiedIds(new Set());
        applyProgramThemeFromConfigs(res.data, map, { persist: true });
        applyMainBackgroundFromConfigs(res.data, map, { persist: true });
      }
    } catch {
      showNotification && showNotification('❌ Không thể tải tham số cấu hình từ Firebird!');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchConfigs();
  }, []);

  // Xử lý thay đổi giá trị theo từng loại dữ liệu
  const handleChange = (id, fieldName, value) => {
    setConfigValues((prev) => ({
      ...prev,
      [id]: {
        ...(prev[id] || { id }),
        [fieldName]: value
      }
    }));
    setModifiedIds((prev) => new Set(prev).add(id));

    const themeItem = findProgramThemeItem(configGroups);
    if (themeItem?.id === id && fieldName === 'intValue') {
      applyProgramTheme(value);
    }

    const backgroundItem = findMainBackgroundItem(configGroups);
    if (backgroundItem?.id === id && fieldName === 'intValue') {
      applyMainBackground(value);
    }
  };

  // Xử lý chọn ảnh Logo
  const handleLogoFileChange = (e, logoItemId) => {
    const file = e.target.files?.[0];
    if (!file) return;

    if (!file.type.startsWith('image/')) {
      showNotification && showNotification('⚠️ Vui lòng chọn file hình ảnh (PNG, JPG, BMP)!');
      return;
    }

    const reader = new FileReader();
    reader.onload = (event) => {
      const dataUrl = event.target.result;
      const base64 = typeof dataUrl === 'string' ? dataUrl.split(',')[1] : null;
      if (base64 && logoItemId) {
        handleChange(logoItemId, 'blobValue', base64);
        showNotification && showNotification('Đã tải ảnh Logo. Nhấn "Ghi dữ liệu" để lưu vào CSDL.');
      }
    };
    reader.readAsDataURL(file);
  };

  // Lưu toàn bộ các tham số đã thay đổi vào Firebird
  const handleSaveConfigs = async () => {
    const itemsToUpdate = Array.from(modifiedIds).map((id) => configValues[id]).filter(Boolean);

    if (itemsToUpdate.length === 0) {
      showNotification && showNotification('ℹ️ Không có thay đổi nào cần lưu!');
      return;
    }

    setIsSaving(true);
    try {
      const res = await adminService.updateConfigs(itemsToUpdate);
      if (res && res.success) {
        applyProgramThemeFromConfigs(configGroups, configValues, { persist: true });
        applyMainBackgroundFromConfigs(configGroups, configValues, { persist: true });
        showNotification && showNotification(`✅ ${res.message}`);
        setModifiedIds(new Set());
      }
    } catch (err) {
      showNotification && showNotification(err.response?.data?.message || '❌ Lỗi khi ghi dữ liệu cấu hình!');
    } finally {
      setIsSaving(false);
    }
  };

  const currentGroup = configGroups[activeGroupIndex] || null;

  // Tách các options từ chuỗi OTHERCONFIG (ngăn cách bởi dấu xuống dòng)
  const parseOptions = (otherConfigStr = '') => {
    if (!otherConfigStr) return [];
    return otherConfigStr
      .split(/\r?\n/)
      .map((opt) => opt.trim())
      .filter((opt) => opt.length > 0);
  };

  // Render từng control cụ thể theo CONTROLTYPE chuẩn của hệ thống
  const renderControl = (item, isPaired = false) => {
    const valObj = configValues[item.id] || {
      textValue: item.textValue ?? '',
      intValue: item.intValue ?? 0,
      decimalValue: item.decimalValue ?? 0,
      dateTimeValue: item.dateTimeValue ?? '',
      blobValue: item.blobValue ?? null
    };

    const cType = item.controlType;
    const isMainBackgroundConfig = item.name === MAIN_BACKGROUND_CONFIG_NAME;
    const options = isMainBackgroundConfig
      ? MAIN_BACKGROUND_COLORS.map((color) => `${color.group} — ${color.name} — ${color.hex}`)
      : parseOptions(item.otherConfig);

    // 1. Checkbox (CONTROLTYPE = 9) - Lưu 30 hoặc 1 vào INTVALUE
    if (cType === 9) {
      const isChecked = valObj.intValue === 30 || valObj.intValue === 1;
      return (
        <div className="sconf-field-checkbox-row">
          <label className="sconf-checkbox-label">
            <input
              type="checkbox"
              className="sconf-checkbox"
              checked={isChecked}
              onChange={(e) => handleChange(item.id, 'intValue', e.target.checked ? 30 : 0)}
            />
            <span className="sconf-checkbox-text">{item.caption || item.name}</span>
          </label>
          {item.moreDetail && (
            <div className="sconf-more-detail">
              <em>{item.moreDetail}</em>
            </div>
          )}
        </div>
      );
    }

    // 2. Combobox / Dropdown (CONTROLTYPE = 10) - Các nút mũi tên sổ xuống từ OTHERCONFIG
    if (cType === 10) {
      return (
        <div className="sconf-field-row">
          <label className="sconf-label">{item.caption || item.name}</label>
          <div className="sconf-input-wrap">
            <select
              className="sconf-select"
              value={valObj.intValue}
              onChange={(e) => handleChange(item.id, 'intValue', parseInt(e.target.value, 10))}
            >
              {options.length > 0 ? (
                options.map((opt, idx) => (
                  <option key={idx} value={idx}>
                    {opt}
                  </option>
                ))
              ) : (
                <option value={0}>{valObj.textValue || '-- Mặc định --'}</option>
              )}
            </select>
          </div>
          {item.moreDetail && (
            <div className="sconf-more-detail">
              <em>{item.moreDetail}</em>
            </div>
          )}
        </div>
      );
    }

    // 3. Number / Spinbox (CONTROLTYPE = 3) - Lưu vào INTVALUE hoặc DECIMALVALUE
    if (cType === 3) {
      return (
        <div className={`sconf-field-row sconf-field-number ${isPaired ? 'sconf-number-paired' : ''}`}>
          <label className="sconf-label">{item.caption || item.name}</label>
          <div className="sconf-input-wrap">
            <input
              type="number"
              className={isPaired ? 'sconf-input-num-paired' : 'sconf-input-num'}
              value={valObj.intValue ?? 0}
              onChange={(e) => handleChange(item.id, 'intValue', parseInt(e.target.value, 10) || 0)}
            />
          </div>
          {item.moreDetail && (
            <div className="sconf-more-detail">
              <em>{item.moreDetail}</em>
            </div>
          )}
        </div>
      );
    }

    // 4. Multiline Textarea (CONTROLTYPE = 6) - Lưu vào TEXTVALUE (VD: Địa chỉ)
    if (cType === 6) {
      return (
        <div className="sconf-field-row">
          <label className="sconf-label">{item.caption || item.name}</label>
          <div className="sconf-input-wrap">
            <textarea
              className="sconf-textarea"
              rows={3}
              value={valObj.textValue}
              onChange={(e) => handleChange(item.id, 'textValue', e.target.value)}
            />
          </div>
          {item.moreDetail && (
            <div className="sconf-more-detail">
              <em>{item.moreDetail}</em>
            </div>
          )}
        </div>
      );
    }

    // 5. Date / Time (CONTROLTYPE = 1) - Lưu vào DATETIMEVALUE
    if (cType === 1) {
      const formattedDate = valObj.dateTimeValue ? valObj.dateTimeValue.substring(0, 16) : '';
      return (
        <div className="sconf-field-row">
          <label className="sconf-label">{item.caption || item.name}</label>
          <div className="sconf-input-wrap">
            <input
              type="datetime-local"
              className="sconf-input-date"
              value={formattedDate}
              onChange={(e) => handleChange(item.id, 'dateTimeValue', e.target.value)}
            />
          </div>
          {item.moreDetail && (
            <div className="sconf-more-detail">
              <em>{item.moreDetail}</em>
            </div>
          )}
        </div>
      );
    }

    // 6. Reference / Table Combobox (CONTROLTYPE = 8)
    if (cType === 8) {
      return (
        <div className="sconf-field-row">
          <label className="sconf-label">{item.caption || item.name}</label>
          <div className="sconf-input-wrap">
            <select
              className="sconf-select"
              value={valObj.textValue}
              onChange={(e) => handleChange(item.id, 'textValue', e.target.value)}
            >
              <option value="Mẫu in bill 80mm">⭐ Mẫu in bill 80mm</option>
              <option value="Mẫu in bill 58mm">Mẫu in bill 58mm</option>
              <option value="Mẫu in A4 / A5">Mẫu in A4 / A5 tiêu chuẩn</option>
            </select>
          </div>
        </div>
      );
    }

    // 7. Textbox thông thường (CONTROLTYPE = 5 hoặc khác) - Lưu vào TEXTVALUE
    return (
      <div className="sconf-field-row">
        <label className="sconf-label">{item.caption || item.name}</label>
        <div className="sconf-input-wrap sconf-with-ellipsis-btn">
          <input
            type="text"
            className="sconf-input-text"
            value={valObj.textValue}
            onChange={(e) => handleChange(item.id, 'textValue', e.target.value)}
            placeholder={`Nhập ${item.caption || item.name}...`}
          />
          {/* Nút ... cho các mẫu số phiếu hoặc cấu hình nâng cao */}
          {currentGroup?.groupName === 'Số phiếu' && (
            <button
              type="button"
              className="sconf-ellipsis-btn"
              onClick={() => showNotification && showNotification(`Cấu hình sinh mã cho: ${item.caption}`)}
              title="Thiết lập mẫu sinh mã"
            >
              ...
            </button>
          )}
        </div>
        {item.moreDetail && (
          <div className="sconf-more-detail">
            <em>{item.moreDetail}</em>
          </div>
        )}
      </div>
    );
  };

  // Lọc items theo từ khóa tìm kiếm
  const getFilteredItems = (items = []) => {
    if (!searchFilter.trim()) return items;
    const term = searchFilter.toLowerCase();
    return items.filter(
      (it) =>
        (it.caption && it.caption.toLowerCase().includes(term)) ||
        (it.name && it.name.toLowerCase().includes(term)) ||
        (it.moreDetail && it.moreDetail.toLowerCase().includes(term)) ||
        (it.textValue && it.textValue.toLowerCase().includes(term))
    );
  };

  return (
    <div className="sconf-window-container">
      {/* 1. THANH TIÊU ĐỀ CỬA SỔ CHUẨN WINFORM / MODERN DESKTOP */}
      <div className="sconf-window-header">
        <div className="sconf-header-left">
          <div className="sconf-header-icon">
            <i className="fa-solid fa-screwdriver-wrench"></i>
          </div>
          <h2 className="sconf-header-title">Thông tin cấu hình toàn bộ hệ thống</h2>
        </div>
        <div className="sconf-header-right">
          {modifiedIds.size > 0 && (
            <span className="sconf-unsaved-badge">
              <i className="fa-solid fa-circle-exclamation"></i> Có {modifiedIds.size} mục chưa lưu
            </span>
          )}
        </div>
      </div>

      {loading ? (
        <div className="sconf-loading-state">
          <i className="fa-solid fa-spinner fa-spin"></i>
          <span>Đang nạp dữ liệu cấu hình từ bảng SCONFIG & SCONFIGGROUP...</span>
        </div>
      ) : (
        <div className="sconf-window-body">
          {/* 2. CỘT TRÁI: DANH SÁCH NHÓM CẤU HÌNH (TABS) */}
          <div className="sconf-sidebar">
            <div className="sconf-sidebar-list">
              {configGroups.map((g, idx) => {
                const iconClass = GROUP_ICONS[g.groupName] || 'fa-solid fa-folder';
                const isActive = idx === activeGroupIndex;
                return (
                  <div
                    key={g.groupId || idx}
                    className={`sconf-tab-item ${isActive ? 'active' : ''}`}
                    onClick={() => setActiveGroupIndex(idx)}
                  >
                    <i className={`${iconClass} sconf-tab-icon`}></i>
                    <span className="sconf-tab-name">{g.groupName}</span>
                  </div>
                );
              })}
            </div>
          </div>

          {/* 3. CỘT PHẢI: FORM CẤU HÌNH CHI TIẾT */}
          <div className="sconf-form-pane">
            <div className="sconf-form-scroll-area">
              {/* Riêng Tab 'Mẫu hóa đơn' có giao diện khóa như ảnh chụp */}
              {currentGroup?.groupName === 'Mẫu hóa đơn' ? (
                <div className="sconf-dev-lock-box">
                  <p>Phần này dành cho nhà phát triển, mời bạn nhập mật khẩu:</p>
                  <div className="sconf-dev-lock-row">
                    <input type="password" placeholder="Mật khẩu nhà phát triển..." className="sconf-dev-input" />
                    <button
                      className="sconf-btn-unlock"
                      onClick={() => showNotification && showNotification('Mật khẩu nhà phát triển không chính xác')}
                    >
                      Mở khóa
                    </button>
                  </div>
                </div>
              ) : currentGroup?.groupName === 'Thông tin chung' ? (
                /* Layout đặc biệt của 'Thông tin chung' khớp chính xác với ảnh mẫu */
                (() => {
                  const logoItem = currentGroup.items?.find(
                    (it) => it.name?.toLowerCase() === 'logo' || it.caption?.toLowerCase() === 'logo'
                  );
                  const filteredGeneralItems = getFilteredItems(currentGroup.items).filter(
                    (it) => it.id !== logoItem?.id
                  );
                  const logoBase64 = logoItem ? configValues[logoItem.id]?.blobValue : null;

                  return (
                    <div className="sconf-general-layout">
                      <div className="sconf-general-fields">
                        {filteredGeneralItems.map((item) => (
                          <div key={item.id} className="sconf-item-wrapper">
                            {renderControl(item)}
                          </div>
                        ))}
                      </div>

                      {/* Hộp Logo công ty bên phải */}
                      <div className="sconf-logo-box-col">
                        <label className="sconf-logo-label">Logo:</label>
                        <div className="sconf-logo-preview-box">
                          {logoBase64 ? (
                            <img
                              src={`data:image/png;base64,${logoBase64}`}
                              alt="Logo Công Ty"
                              className="sconf-logo-img"
                            />
                          ) : (
                            <i className="fa-regular fa-image sconf-logo-placeholder"></i>
                          )}
                        </div>
                        <input
                          type="file"
                          ref={fileInputRef}
                          style={{ display: 'none' }}
                          accept="image/*"
                          onChange={(e) => handleLogoFileChange(e, logoItem?.id)}
                        />
                        <div className="sconf-logo-btn-group">
                          <button
                            type="button"
                            className="sconf-btn-choose-logo"
                            onClick={() => fileInputRef.current?.click()}
                          >
                            Chọn
                          </button>
                          {logoBase64 && logoItem && (
                            <button
                              type="button"
                              className="sconf-btn-clear-logo"
                              onClick={() => handleChange(logoItem.id, 'blobValue', '')}
                              title="Xóa logo hiện tại"
                            >
                              <i className="fa-solid fa-trash-can"></i>
                            </button>
                          )}
                        </div>
                      </div>
                    </div>
                  );
                })()
              ) : (
                /* Các nhóm cấu hình khác: Gym, Bán hàng, Số phiếu, v.v. */
                <div className="sconf-dynamic-fields-container">
                  {(() => {
                    const rawItems =
                      currentGroup?.groupName === 'Số phiếu' &&
                      (!currentGroup?.items || currentGroup.items.length === 0)
                        ? DEFAULT_TICKET_MASKS
                        : currentGroup?.items || [];
                    const items = getFilteredItems(rawItems);

                    if (items.length === 0) {
                      return (
                        <div className="sconf-empty-filter">
                          <i className="fa-regular fa-folder-open"></i>
                          <span>Không có tham số nào phù hợp với từ khóa tìm kiếm</span>
                        </div>
                      );
                    }

                    // Nhóm các trường có SOCOT = 2 lại cạnh nhau trên cùng một hàng
                    const elements = [];
                    let i = 0;
                    while (i < items.length) {
                      const item1 = items[i];
                      const item2 = items[i + 1];

                      const isPair =
                        item2 &&
                        ((item1.soCot === 2 && item2.soCot === 2) ||
                          (item1.controlType === 3 && item2.controlType === 3 && item1.name?.includes('Nhom') && item2.name?.includes('Nhom')) ||
                          (item1.caption?.includes('Nhóm bị khóa') && item2.caption?.includes('Nhóm không bị')) ||
                          (item1.caption?.includes('Cảnh báo sắp hết hạn') && item2.caption?.includes('Cảnh báo sắp hết lần')));

                      if (isPair) {
                        elements.push(
                          <div key={`pair_${item1.id}_${item2.id}`} className="sconf-paired-row">
                            <div className="sconf-paired-col">{renderControl(item1, true)}</div>
                            <div className="sconf-paired-col">{renderControl(item2, true)}</div>
                          </div>
                        );
                        i += 2;
                      } else {
                        elements.push(
                          <div key={item1.id} className="sconf-item-wrapper">
                            {renderControl(item1, false)}
                          </div>
                        );
                        i += 1;
                      }
                    }

                    return elements;
                  })()}
                </div>
              )}
            </div>
          </div>
        </div>
      )}

      {/* 4. THANH CHỨC NĂNG DƯỚI ĐÁY: TÌM KIẾM, GHI DỮ LIỆU, THOÁT */}
      <div className="sconf-window-footer">
        <div className="sconf-footer-search">
          <i className="fa-solid fa-magnifying-glass"></i>
          <input
            type="text"
            className="sconf-search-input"
            placeholder="Tìm kiếm (Ctrl + F)"
            value={searchFilter}
            onChange={(e) => setSearchFilter(e.target.value)}
          />
          {searchFilter && (
            <button className="sconf-search-clear" onClick={() => setSearchFilter('')}>
              <i className="fa-solid fa-xmark"></i>
            </button>
          )}
        </div>

        <div className="sconf-footer-actions">
          <button
            type="button"
            className="sconf-btn-save"
            onClick={handleSaveConfigs}
            disabled={isSaving || loading}
          >
            <i className={`fa-solid ${isSaving ? 'fa-spinner fa-spin' : 'fa-floppy-disk'}`}></i>
            <span>{isSaving ? 'Đang lưu...' : 'Ghi dữ liệu'}</span>
          </button>
        </div>
      </div>
    </div>
  );
}
