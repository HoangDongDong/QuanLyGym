import React, { useState } from 'react';
import './TreeItemModal.css';

/**
 * TreeQuickAddModal - Form "Thêm nhanh"
 * Cho phép nhập nhiều trạng thái / nhóm một lúc (mỗi dòng 1 mục)
 */
export default function TreeQuickAddModal({
  show,
  treeMode = 'trangThai', // 'trangThai' | 'nhomKhach'
  parentId = null,
  onSave,
  onClose
}) {
  if (!show) return null;

  const isTrangThai = treeMode === 'trangThai';
  const typeName = isTrangThai ? 'trạng thái' : 'nhóm khách hàng';
  const [text, setText] = useState('');
  const [saving, setSaving] = useState(false);

  const handleSubmit = async (e) => {
    if (e) e.preventDefault();
    const lines = text
      .split('\n')
      .map(l => l.trim())
      .filter(l => l.length > 0);

    if (lines.length === 0) {
      alert(`Vui lòng nhập ít nhất một tên ${typeName}!`);
      return;
    }

    setSaving(true);
    try {
      await onSave({
        names: lines,
        parentId: parentId || null,
        mode: treeMode
      });
      onClose();
    } catch (err) {
      console.error('Lỗi thêm nhanh:', err);
      alert('Lỗi thêm nhanh: ' + (err.message || 'Vui lòng thử lại'));
    } finally {
      setSaving(false);
    }
  };

  return (
    <div className="tree-modal-overlay" onClick={onClose}>
      <div className="tree-modal-window" style={{ width: 480 }} onClick={(e) => e.stopPropagation()}>
        {/* Title bar WinForms */}
        <div className="tree-modal-titlebar">
          <div className="tree-modal-title">
            <span className="win-icon">⚡</span>
            <span>Thêm nhanh {typeName}</span>
          </div>
          <div className="tree-modal-controls">
            <button type="button" className="tree-modal-winbtn" onClick={onClose}>✕</button>
          </div>
        </div>

        {/* Header Banner */}
        <div className="tree-modal-header">
          <div className="tree-modal-header-icon" style={{ fontSize: 24 }}>
            ⚡
          </div>
          <div className="tree-modal-header-text">
            <div className="tree-modal-banner-title">Thêm nhanh danh sách {typeName}</div>
          </div>
        </div>

        {/* Form Body */}
        <div className="tree-modal-body">
          <div style={{ fontSize: 12, color: '#475569', marginBottom: 4 }}>
            Nhập danh sách tên (mỗi tên trên 1 dòng):
          </div>
          <textarea
            className="tree-textarea"
            style={{ height: 160 }}
            value={text}
            onChange={(e) => setText(e.target.value)}
            placeholder={`Ví dụ:\nKhách VIP\nKhách Tập Thử\nKhách Tiềm Năng`}
            autoFocus
          />
        </div>

        {/* Footer */}
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
