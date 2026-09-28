import React, { useState, useEffect, useRef } from 'react';

/**
 * Component Input nhập số WinForms với Tooltip 3D hiển thị định dạng dấu phẩy liên tục khi gõ
 * Khớp 100% theo hình ảnh của Tân An Phát WinForms
 */
export default function Number3DInput({
  value,
  onChange,
  onFocus,
  onBlur,
  style = {},
  readOnly = false,
  disabled = false,
  placeholder = '',
  isYellow = false,
  show3DTooltip = true,
  ...restProps
}) {
  const [isFocused, setIsFocused] = useState(false);
  const [rawText, setRawText] = useState(() => (value != null && value !== '' ? String(value) : '0'));
  const inputRef = useRef(null);

  // Đồng bộ giá trị từ ngoài vào khi không đang focus
  useEffect(() => {
    if (!isFocused) {
      setRawText(value != null && value !== '' ? String(value) : '0');
    }
  }, [value, isFocused]);

  // Format số có dấu phẩy phân tách hàng nghìn (ví dụ: 2000 -> 2,000)
  const formatComma = (val) => {
    if (val == null || val === '') return '0';
    const str = String(val).replace(/,/g, '');
    const num = parseFloat(str);
    if (isNaN(num)) return '0';
    const parts = str.split('.');
    parts[0] = parts[0].replace(/\B(?=(\d{3})+(?!\d))/g, ',');
    return parts.join('.');
  };

  const handleInputChange = (e) => {
    const val = e.target.value;
    // Cho phép nhập số tự do, dấu âm, dấu chấm
    const clean = val.replace(/[^0-9.-]/g, '');
    setRawText(clean);

    const parsed = parseFloat(clean.replace(/,/g, ''));
    if (onChange) {
      onChange(isNaN(parsed) ? 0 : parsed, clean);
    }
  };

  // Trích xuất vị trí tuyệt đối từ style
  const {
    left,
    top,
    width = 120,
    height = 21,
    position = 'absolute',
    zIndex,
    textAlign = 'left',
    background,
    ...otherStyles
  } = style;

  const bg = isYellow ? '#ffffd5' : (background || '#ffffff');
  const currentFormatted = formatComma(rawText);

  return (
    <div
      style={{
        position,
        left,
        top,
        width,
        height,
        zIndex: isFocused ? 1000 : (zIndex || 1)
      }}
    >
      <input
        ref={inputRef}
        type="text"
        inputMode="numeric"
        readOnly={readOnly}
        disabled={disabled}
        placeholder={placeholder}
        value={rawText}
        onChange={handleInputChange}
        onFocus={(e) => {
          setIsFocused(true);
          // Tự động bôi đen toàn bộ số khi focus để gõ số mới nhanh
          try {
            e.target.select();
          } catch {
            // ignore
          }
          onFocus?.(e);
        }}
        onBlur={(e) => {
          setIsFocused(false);
          // Khi rời khỏi ô, format lại text chuẩn số
          const parsed = parseFloat(rawText.replace(/,/g, ''));
          setRawText(!isNaN(parsed) ? String(parsed) : '0');
          onBlur?.(e);
        }}
        style={{
          width: '100%',
          height: '100%',
          boxSizing: 'border-box',
          border: '1px solid #7f9db9',
          padding: '1px 5px',
          fontFamily: "Tahoma, 'Segoe UI', Arial, sans-serif",
          fontSize: '11px',
          color: '#000000',
          background: bg,
          outline: 'none',
          textAlign,
          ...otherStyles
        }}
        {...restProps}
      />

      {/* TOOLTIP 3D HIỂN THỊ DẤU PHẨY LIÊN TỤC KHI BẤM SỐ (KHỚP 100% ẢNH NGƯỜI DÙNG) */}
      {show3DTooltip && isFocused && !readOnly && !disabled && (
        <div
          className="wf-3d-number-callout"
          style={{
            position: 'absolute',
            left: 0,
            top: 'calc(100% + 2px)',
            background: 'linear-gradient(180deg, #ffffff 0%, #f6f8fa 100%)',
            border: '1px solid #9aa0a6',
            borderRadius: 3,
            boxShadow: '1px 2px 4px rgba(0, 0, 0, 0.35)',
            padding: '1px 6px',
            fontSize: '11px',
            fontFamily: "Tahoma, 'Segoe UI', Arial, sans-serif",
            fontWeight: 600,
            color: '#1e3a8a',
            whiteSpace: 'nowrap',
            zIndex: 99999,
            pointerEvents: 'none',
            display: 'flex',
            alignItems: 'center',
            minWidth: 32
          }}
        >
          {currentFormatted}
        </div>
      )}
    </div>
  );
}
