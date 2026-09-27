import React, { useState, useEffect } from 'react';
import './TreeItemModal.css';

/**
 * TreeItemModal - Form Thêm mới / Chỉnh sửa Trạng thái hoặc Thư mục
 * Thiết kế chuẩn 100% theo giao diện WinForms Desktop (media_1790500547536.png)
 */
export default function TreeItemModal({
  show,
  mode = 'create', // 'create' | 'edit'
  itemType = 0,    // 0: Trạng thái / Nhóm, 1: Thư mục
  treeMode = 'trangThai', // 'trangThai' | 'nhomKhach'
  parentId = null,
  initialData = null,
  icons = [],
  onSave,
  onClose
}) {
  if (!show) return null;

  const isFolder = itemType === 1 || initialData?.itemType === 1;
  const isTrangThai = treeMode === 'trangThai';

  const typeName = isFolder ? 'thư mục' : (isTrangThai ? 'trạng thái' : 'nhóm khách hàng');
  const modalTitle = isFolder ? 'Thư mục' : (isTrangThai ? 'Trạng thái' : 'Nhóm khách hàng');
  const bannerTitle = mode === 'edit' 
    ? `Chỉnh sửa ${typeName}` 
    : `Thêm ${typeName}${parentId ? ' con' : ''}`;

  const [name, setName] = useState('');
  const [note, setNote] = useState('');
  const [selectedImageId, setSelectedImageId] = useState('');
  const [showIconDropdown, setShowIconDropdown] = useState(false);
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    if (initialData && mode === 'edit') {
      setName(initialData.name || '');
      setNote(initialData.note || '');
      setSelectedImageId(initialData.simageId || '');
    } else {
      setName('');
      setNote('');
      // Default icon: if folder -> folder icon if available, else first or empty
      if (isFolder) {
        const folderIcon = icons.find(i => i.name?.toLowerCase().includes('thư mục') || i.name?.toLowerCase().includes('folder'));
        setSelectedImageId(folderIcon ? folderIcon.id : (icons[0]?.id || ''));
      } else {
        setSelectedImageId(icons[0]?.id || '');
      }
    }
  }, [initialData, mode, itemType, icons]);

  const selectedIconObj = icons.find(i => i.id === selectedImageId);

  const handleSubmit = async (e) => {
    if (e) e.preventDefault();
    if (!name.trim()) {
      alert(`Vui lòng nhập tên ${typeName}!`);
      return;
    }

    setSaving(true);
    try {
      await onSave({
        name: name.trim(),
        note: note.trim(),
        simageId: selectedImageId || null,
        itemType: isFolder ? 1 : 0,
        parentId: parentId || initialData?.parentId || null,
        mode: treeMode,
        id: initialData?.id
      });
      onClose();
    } catch (err) {
      console.error('Lỗi lưu dữ liệu cây:', err);
      alert('Lỗi lưu dữ liệu: ' + (err.message || 'Vui lòng thử lại'));
    } finally {
      setSaving(false);
    }
  };

  return (
    <div className="tree-modal-overlay" onClick={onClose}>
      <div className="tree-modal-window" onClick={(e) => e.stopPropagation()}>
        {/* Title bar WinForms */}
        <div className="tree-modal-titlebar">
          <div className="tree-modal-title">
            <span className="win-icon">⚙️</span>
            <span>{modalTitle}</span>
          </div>
          <div className="tree-modal-controls">
            <button type="button" className="tree-modal-winbtn" onClick={onClose}>✕</button>
          </div>
        </div>

        {/* Header Banner */}
        <div className="tree-modal-header">
          <div className="tree-modal-header-icon">
            {selectedIconObj?.image ? (
              <img 
                src={`data:image/png;base64,${selectedIconObj.image}`} 
                alt="Icon" 
                style={{ width: 28, height: 28, objectFit: 'contain' }}
              />
            ) : (
              <span style={{ fontSize: 24 }}>💬</span>
            )}
          </div>
          <div className="tree-modal-header-text">
            <div className="tree-modal-banner-title">{bannerTitle}</div>
          </div>
        </div>

        {/* Form Body */}
        <div className="tree-modal-body">
          <div className="tree-form-row">
            <label className="tree-label">Tên {typeName}:</label>
            <input
              type="text"
              className="tree-input tree-name-input"
              value={name}
              onChange={(e) => setName(e.target.value)}
              placeholder={`Nhập tên ${typeName}...`}
              autoFocus
            />

            <label className="tree-label tree-label-img">Ảnh:</label>
            <div className="tree-icon-picker-container">
              <div 
                className="tree-icon-selector"
                onClick={() => setShowIconDropdown(!showIconDropdown)}
                title="Chọn biểu tượng"
              >
                <div className="tree-icon-preview">
                  {selectedIconObj?.image ? (
                    <img 
                      src={`data:image/png;base64,${selectedIconObj.image}`} 
                      alt="" 
                      style={{ width: 16, height: 16 }}
                    />
                  ) : (
                    <span className="tree-icon-empty"></span>
                  )}
                </div>
                <span className="tree-icon-arrow">▾</span>
              </div>

              {showIconDropdown && (
                <div className="tree-icon-dropdown-menu">
                  <div 
                    className="tree-icon-dropdown-item tree-icon-dropdown-none"
                    onClick={() => {
                      setSelectedImageId('');
                      setShowIconDropdown(false);
                    }}
                  >
                    (Không dùng ảnh)
                  </div>
                  <div className="tree-icon-grid">
                    {icons.map((ic) => (
                      <div
                        key={ic.id}
                        className={`tree-icon-grid-item ${selectedImageId === ic.id ? 'selected' : ''}`}
                        onClick={() => {
                          setSelectedImageId(ic.id);
                          setShowIconDropdown(false);
                        }}
                        title={ic.name || 'Biểu tượng'}
                      >
                        <img 
                          src={`data:image/png;base64,${ic.image}`} 
                          alt={ic.name || ''} 
                          style={{ width: 16, height: 16 }}
                        />
                      </div>
                    ))}
                  </div>
                </div>
              )}
            </div>
          </div>

          <div className="tree-form-row tree-form-row-note">
            <label className="tree-label">Ghi chú:</label>
            <textarea
              className="tree-textarea"
              value={note}
              onChange={(e) => setNote(e.target.value)}
              rows={6}
              placeholder="Nhập ghi chú (nếu có)..."
            />
          </div>
        </div>

        {/* Footer WinForms style buttons */}
        <div className="tree-modal-footer">
          <button 
            type="button" 
            className="tree-btn tree-btn-save"
            onClick={handleSubmit}
            disabled={saving}
          >
            {saving ? 'Đang lưu...' : 'Ghi dữ liệu'}
          </button>
          <button 
            type="button" 
            className="tree-btn tree-btn-cancel"
            onClick={onClose}
            disabled={saving}
          >
            Thoát
          </button>
        </div>
      </div>
    </div>
  );
}
