import React, { useState, useEffect } from 'react';

export default function SlipPatternConfigModal({
  show,
  item = null, // { key, name, template, sample }
  onSave,
  onClose
}) {
  const [pattern, setPattern] = useState('');
  const [infoText, setInfoText] = useState('');
  const [previewSample, setPreviewSample] = useState('');

  useEffect(() => {
    if (show && item) {
      setPattern(item.template || '');
    }
  }, [show, item]);

  // Tính toán số chuỗi và ví dụ dải số
  useEffect(() => {
    if (!pattern) {
      setInfoText('Chưa thiết lập mẫu');
      setPreviewSample('');
      return;
    }

    const starMatch = pattern.match(/\(\*+\)/);
    if (!starMatch) {
      setInfoText('Mẫu cố định (không có phần tự tăng (*))');
    } else {
      const starLen = starMatch[0].length - 2; // Số lượng dấu *
      const maxVal = Math.pow(10, starLen) - 1;
      const fromSample = pattern.replace(starMatch[0], '1'.padStart(starLen, '0'));
      const toSample = pattern.replace(starMatch[0], String(maxVal).padStart(starLen, '0'));
      setInfoText(`Có ${maxVal} chuỗi từ ${fromSample} đến ${toSample}`);
    }

    // Xem trước kết quả theo ngày giờ hiện tại
    const now = new Date();
    const yyyy = now.getFullYear().toString();
    const yy = yyyy.slice(-2);
    const MM = (now.getMonth() + 1).toString().padStart(2, '0');
    const dd = now.getDate().toString().padStart(2, '0');

    let preview = pattern
      .replace(/\(yyyy\)/g, yyyy)
      .replace(/\(yy\)/g, yy)
      .replace(/\(MM\)/g, MM)
      .replace(/\(dd\)/g, dd);

    if (starMatch) {
      const starLen = starMatch[0].length - 2;
      preview = preview.replace(starMatch[0], '1'.padStart(starLen, '0'));
    }
    setPreviewSample(preview);
  }, [pattern]);

  if (!show || !item) return null;

  const handleSave = () => {
    onSave?.(item.key, pattern);
  };

  return (
    <div className="cust-modal-overlay" style={{ zIndex: 1100 }}>
      <div
        className="cust-modal-box"
        style={{
          width: 580,
          maxWidth: '95vw',
          backgroundColor: '#bed5ee',
          border: '1px solid #7092be',
          boxShadow: '0 8px 30px rgba(0,0,0,0.3)',
          borderRadius: 6,
          overflow: 'hidden',
          fontFamily: 'Segoe UI, Tahoma, Geneva, Verdana, sans-serif'
        }}
      >
        {/* WINDOW TITLEBAR */}
        <div
          style={{
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'space-between',
            background: 'linear-gradient(180deg, #ffffff 0%, #d4e3f5 100%)',
            padding: '6px 12px',
            borderBottom: '1px solid #9fb9db',
            userSelect: 'none'
          }}
        >
          <div style={{ fontSize: 13, fontWeight: 600, color: '#1e3a8a' }}>
            Cấu hình cách sinh {item.name || 'số phiếu'}
          </div>
          <button
            onClick={onClose}
            style={{
              background: 'transparent',
              border: 'none',
              cursor: 'pointer',
              fontSize: 14,
              color: '#475569',
              padding: '2px 6px',
              borderRadius: 3
            }}
            title="Đóng"
          >
            ✕
          </button>
        </div>

        {/* MODAL BODY */}
        <div style={{ padding: '16px 20px', fontSize: 13, color: '#1e293b' }}>
          {/* HEADER ICON & TITLE */}
          <div style={{ display: 'flex', alignItems: 'center', gap: 10, marginBottom: 16 }}>
            <span style={{ fontSize: 24, color: '#2563eb' }}>⚓</span>
            <span style={{ fontSize: 15, fontWeight: 700, color: '#0f172a' }}>
              Thiết lập cách sinh {item.name || 'số phiếu'}
            </span>
          </div>

          {/* INPUT ROW */}
          <div style={{ display: 'flex', alignItems: 'center', gap: 10, marginBottom: 14 }}>
            <label style={{ fontWeight: 600, minWidth: 42 }}>Mẫu:</label>
            <input
              type="text"
              value={pattern}
              onChange={(e) => setPattern(e.target.value)}
              style={{
                width: 200,
                height: 28,
                padding: '0 8px',
                border: '1px solid #3b82f6',
                borderRadius: 2,
                fontSize: 13,
                fontFamily: 'Consolas, monospace',
                fontWeight: 600,
                color: '#1e40af',
                backgroundColor: '#ffffff',
                outline: 'none'
              }}
              placeholder="VD: BG(yy)/(*****)"
            />
            <span style={{ fontSize: 12, color: '#334155', fontWeight: 500 }}>
              {infoText}
            </span>
          </div>

          {/* LIVE PREVIEW BADGE */}
          <div
            style={{
              background: '#ffffff',
              border: '1px solid #93c5fd',
              borderRadius: 4,
              padding: '8px 12px',
              marginBottom: 16,
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'space-between'
            }}
          >
            <span style={{ fontSize: 12, color: '#475569' }}>
              Số phiếu sinh ra cho lượt kế tiếp:
            </span>
            <span
              style={{
                fontSize: 14,
                fontWeight: 700,
                color: '#1d4ed8',
                fontFamily: 'Consolas, monospace',
                background: '#eff6ff',
                padding: '2px 8px',
                borderRadius: 4,
                border: '1px dashed #60a5fa'
              }}
            >
              {previewSample || '---'}
            </span>
          </div>

          {/* INSTRUCTIONS / GUIDELINES */}
          <div
            style={{
              background: 'rgba(255, 255, 255, 0.45)',
              border: '1px solid #a8c5e5',
              borderRadius: 4,
              padding: '12px 14px',
              lineHeight: 1.6,
              fontSize: 12.5,
              color: '#0f172a'
            }}
          >
            <p style={{ margin: '0 0 6px 0' }}>
              Sử dụng <strong>(*)</strong> dành cho phần số tự động tăng do hệ thống sinh ra, độ dài do số lượng dấu sao quy định, tối đa là 9.
            </p>
            <div style={{ marginLeft: 8, color: '#334155', marginBottom: 10 }}>
              <div>Ví dụ:</div>
              <div style={{ fontFamily: 'Consolas, monospace' }}>• HD(**) gồm 99 số chạy từ HD01, HD02 đến HD99</div>
              <div style={{ fontFamily: 'Consolas, monospace' }}>• HD(***) gồm 999 số chạy từ HD001, HD002 đến HD999</div>
            </div>

            <div style={{ margin: '8px 0', borderTop: '1px dashed #93b7df', paddingTop: 8 }}>
              <div>Sử dụng <strong>(MM)</strong> để lấy tháng hiện tại</div>
              <div>Sử dụng <strong>(yy)</strong> để lấy năm hiện tại (2 số cuối của năm)</div>
              <div>Sử dụng <strong>(yyyy)</strong> để lấy năm hiện tại (cả 4 số)</div>
              <div>Sử dụng <strong>(dd)</strong> để lấy ngày hiện tại</div>
            </div>

            <div style={{ margin: '8px 0 0 0', borderTop: '1px dashed #93b7df', paddingTop: 8, fontStyle: 'italic', color: '#1e3a8a' }}>
              <div>• Nếu số có chứa năm, (*) sẽ chạy lại từ 01 ở đầu năm mới</div>
              <div>• Nếu số có chứa tháng, (*) sẽ chạy lại từ 01 ở đầu tháng mới</div>
              <div>• Nếu số có chứa ngày, (*) sẽ chạy lại từ 01 ở đầu ngày mới</div>
            </div>
          </div>

          {/* FOOTER ACTIONS */}
          <div style={{ display: 'flex', justifyContent: 'flex-end', gap: 10, marginTop: 18 }}>
            <button
              type="button"
              onClick={handleSave}
              style={{
                minWidth: 90,
                height: 30,
                backgroundColor: '#ffffff',
                border: '1px solid #2563eb',
                color: '#1d4ed8',
                borderRadius: 4,
                fontWeight: 600,
                fontSize: 13,
                cursor: 'pointer',
                boxShadow: '0 1px 3px rgba(0,0,0,0.1)'
              }}
              onMouseEnter={(e) => {
                e.currentTarget.style.backgroundColor = '#2563eb';
                e.currentTarget.style.color = '#ffffff';
              }}
              onMouseLeave={(e) => {
                e.currentTarget.style.backgroundColor = '#ffffff';
                e.currentTarget.style.color = '#1d4ed8';
              }}
            >
              Ghi dữ liệu
            </button>
            <button
              type="button"
              onClick={onClose}
              style={{
                minWidth: 80,
                height: 30,
                backgroundColor: '#ffffff',
                border: '1px solid #94a3b8',
                color: '#334155',
                borderRadius: 4,
                fontWeight: 500,
                fontSize: 13,
                cursor: 'pointer'
              }}
              onMouseEnter={(e) => {
                e.currentTarget.style.backgroundColor = '#f1f5f9';
              }}
              onMouseLeave={(e) => {
                e.currentTarget.style.backgroundColor = '#ffffff';
              }}
            >
              Thoát
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}
