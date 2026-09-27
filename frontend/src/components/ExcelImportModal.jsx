import React, { useState, useRef } from 'react';
import * as XLSX from 'xlsx';
import { khachHangService } from '../services/khachHangService';

export const ExcelImportModal = ({
  show,
  onClose,
  onSuccess,
  metadata = {},
  showNotification
}) => {
  if (!show) return null;

  // Định nghĩa ĐẦY ĐỦ 21 TRƯỜNG theo 100% ẢNH THỰC TẾ WinForms Tân An Phát:
  const propDefinitions = [
    { key: 'maThe', label: 'Mã thẻ:', colTitle: 'Mã thẻ', type: 'text', minWidth: 95, defaultCommon: false },
    { key: 'tenKhachHang', label: 'Tên khách hàng:', colTitle: 'Tên khách hàng', type: 'text', minWidth: 160, isYellow: true, defaultCommon: true },
    { key: 'diaChi', label: 'Địa chỉ:', colTitle: 'Địa chỉ', type: 'text', minWidth: 150, defaultCommon: true },
    { key: 'dienThoai', label: 'Điện thoại:', colTitle: 'Điện thoại', type: 'text', minWidth: 105, defaultCommon: true },
    { key: 'loaiThe', label: 'Loại thẻ:', colTitle: 'Loại thẻ', type: 'select', minWidth: 110, defaultCommon: true },
    { key: 'tuNgay', label: 'Từ ngày:', colTitle: 'Từ ngày', type: 'date', minWidth: 100, defaultCommon: true },
    { key: 'denNgay', label: 'Đến ngày:', colTitle: 'Đến ngày', type: 'date', minWidth: 100, defaultCommon: true },
    { key: 'trangThai', label: 'Trạng thái:', colTitle: 'Trạng thái', type: 'select', minWidth: 110, defaultCommon: true, defaultValue: 'Chưa kích hoạt' },
    { key: 'soLan', label: 'Số lần:', colTitle: 'Số lần', type: 'number', minWidth: 65, defaultCommon: true, defaultValue: 0 },
    { key: 'daTap', label: 'Đã tập:', colTitle: 'Đã tập', type: 'number', minWidth: 65, defaultCommon: true, defaultValue: 0 },
    { key: 'conLai', label: 'Còn lại:', colTitle: 'Còn lại', type: 'number', minWidth: 65, defaultCommon: true, defaultValue: 0 },
    { key: 'facebook', label: 'Facebook:', colTitle: 'Facebook', type: 'text', minWidth: 100, defaultCommon: true },
    { key: 'note', label: 'Ghi chú:', colTitle: 'Ghi chú', type: 'text', minWidth: 120, defaultCommon: true },
    { key: 'ngaySinh', label: 'Ngày thành lập/sinh nhật:', colTitle: 'Ngày sinh', type: 'date', minWidth: 100, defaultCommon: true },
    { key: 'nhomKhachHang', label: 'Nhóm khách hàng:', colTitle: 'Nhóm khách', type: 'select', minWidth: 130, defaultCommon: true },
    { key: 'email', label: 'Email:', colTitle: 'Email', type: 'text', minWidth: 120, defaultCommon: true },
    { key: 'maSoThue', label: 'Mã số thuế:', colTitle: 'Mã số thuế', type: 'text', minWidth: 105, defaultCommon: true },
    { key: 'nhanVien', label: 'Nhân viên:', colTitle: 'Nhân viên', type: 'select', minWidth: 120, defaultCommon: true },
    { key: 'diemTichLuyBanDau', label: 'Điểm tích lũy ban đầu:', colTitle: 'Điểm tích lũy ban đầu', type: 'number', minWidth: 95, defaultCommon: true, defaultValue: 0 },
    { key: 'tinhThanh', label: 'Tỉnh thành:', colTitle: 'Tỉnh thành', type: 'select', minWidth: 120, defaultCommon: true },
    { key: 'anh', label: 'Ảnh:', colTitle: 'Ảnh', type: 'text', minWidth: 100, defaultCommon: true }
  ];

  // Trạng thái các thuộc tính chung:
  // isCommon = true (TÍCH CHỌN) -> Thuộc tính chung, ẨN KHỎI LƯỚI
  // isCommon = false (BỎ TÍCH) -> HIỆN THÀNH CỘT TRONG LƯỚI!
  const [commonProps, setCommonProps] = useState(() => {
    const init = {};
    propDefinitions.forEach(p => {
      init[p.key] = {
        isCommon: p.defaultCommon,
        value: p.defaultValue !== undefined ? p.defaultValue : ''
      };
    });
    return init;
  });

  const [numRowsToAdd, setNumRowsToAdd] = useState(1);

  // Danh sách dòng trong lưới
  const [gridRows, setGridRows] = useState([
    {
      id: 1,
      maThe: '',
      tenKhachHang: '',
      diaChi: '',
      dienThoai: '',
      loaiThe: '',
      tuNgay: '',
      denNgay: '',
      trangThai: 'Chưa kích hoạt',
      soLan: 0,
      daTap: 0,
      conLai: 0,
      facebook: '',
      note: '',
      ngaySinh: '',
      nhomKhachHang: '',
      email: '',
      maSoThue: '',
      nhanVien: '',
      diemTichLuyBanDau: 0,
      tinhThanh: '',
      anh: ''
    }
  ]);

  const [selectedRowIndex, setSelectedRowIndex] = useState(0);
  const [loading, setLoading] = useState(false);
  const fileInputRef = useRef(null);

  // --- SUB-MODAL STATES ---
  // Modal 1: "Chọn file excel hoặc xuất file mẫu"
  const [showChoiceModal, setShowChoiceModal] = useState(false);

  // Modal 2: "Thêm từ excel" (Ghép cột)
  const [showMappingModal, setShowMappingModal] = useState(false);
  // Danh sách cột đọc từ file Excel (khởi tạo mặc định khớp 100% ảnh screenshot 1)
  const defaultExcelCols = [
    'Mã thẻ', 'Tên khách hàng', 'Địa chỉ', 'Điện thoại', 'Loại thẻ',
    'Từ ngày', 'Đến ngày', 'Trạng thái', 'Số lần', 'Đã tập', 'Còn lại',
    'Facebook', 'Ghi chú', 'Ngày thành lập/sinh nhật'
  ];
  const [excelColumns, setExcelColumns] = useState(defaultExcelCols);
  const [columnMapping, setColumnMapping] = useState(() => {
    return {
      'Mã thẻ': 'maThe',
      'Tên khách hàng': 'tenKhachHang',
      'Địa chỉ': 'diaChi',
      'Điện thoại': 'dienThoai',
      'Loại thẻ': 'loaiThe',
      'Từ ngày': 'tuNgay',
      'Đến ngày': 'denNgay',
      'Trạng thái': 'trangThai',
      'Số lần': 'soLan',
      'Đã tập': 'daTap',
      'Còn lại': 'conLai',
      'Facebook': 'facebook',
      'Ghi chú': 'note',
      'Ngày thành lập/sinh nhật': 'ngaySinh'
    };
  });
  const [rawExcelJson, setRawExcelJson] = useState([]); // Dữ liệu thô từ file Excel
  const [selectedMappingIndex, setSelectedMappingIndex] = useState(0);

  // Toggle thuộc tính chung
  const handleToggleCommon = (key) => {
    setCommonProps(prev => ({
      ...prev,
      [key]: {
        ...prev[key],
        isCommon: !prev[key].isCommon
      }
    }));
  };

  const handleCommonValueChange = (key, value) => {
    setCommonProps(prev => ({
      ...prev,
      [key]: {
        ...prev[key],
        value: value
      }
    }));
  };

  // CÁC CỘT HIỂN THỊ TRÊN LƯỚI: TẤT CẢ CÁC TRƯỜNG ĐƯỢC BỎ TÍCH!
  const gridColumns = propDefinitions.filter(p => !commonProps[p.key].isCommon);

  // Thêm N dòng vào lưới
  const handleAddRows = () => {
    const n = Math.max(1, parseInt(numRowsToAdd, 10) || 1);
    const newRows = [];
    const baseId = gridRows.length > 0 ? Math.max(...gridRows.map(r => r.id)) : 0;

    for (let i = 1; i <= n; i++) {
      const row = { id: baseId + i };
      propDefinitions.forEach(p => {
        row[p.key] = p.defaultValue !== undefined ? p.defaultValue : '';
      });
      newRows.push(row);
    }

    setGridRows(prev => [...prev, ...newRows]);
  };

  // Xóa dòng đang chọn
  const handleDeleteRow = () => {
    if (gridRows.length === 0) return;
    if (selectedRowIndex >= 0 && selectedRowIndex < gridRows.length) {
      setGridRows(prev => prev.filter((_, idx) => idx !== selectedRowIndex));
      setSelectedRowIndex(prev => Math.max(0, prev - 1));
    } else {
      setGridRows(prev => prev.slice(0, -1));
    }
  };

  // Xóa các dòng lỗi (trống dữ liệu ở các cột hiển thị)
  const handleRemoveInvalidRows = () => {
    const valid = gridRows.filter(r => {
      if (gridColumns.length === 0) return true;
      return gridColumns.some(col => String(r[col.key] || '').trim().length > 0);
    });
    const removedCount = gridRows.length - valid.length;
    setGridRows(valid);
    alert(`Đã xóa ${removedCount} dòng dữ liệu lỗi / trống!`);
  };

  // Sửa giá trị 1 ô trong lưới
  const handleCellChange = (rowIndex, key, value) => {
    setGridRows(prev => {
      const copy = [...prev];
      copy[rowIndex] = { ...copy[rowIndex], [key]: value };
      return copy;
    });
  };

  // Dán dữ liệu từ Clipboard (Ctrl+V từ Excel)
  const handlePasteClipboard = async () => {
    try {
      const text = await navigator.clipboard.readText();
      if (!text || text.trim().length === 0) {
        alert('Không có nội dung dạng bảng trong bộ nhớ tạm (Clipboard)!');
        return;
      }

      const lines = text.trim().split('\n');
      const pastedRows = lines.map((line, idx) => {
        const parts = line.split('\t');
        const row = { id: gridRows.length + idx + 1 };

        gridColumns.forEach((col, colIdx) => {
          row[col.key] = (parts[colIdx] || '').trim();
        });

        propDefinitions.forEach(p => {
          if (row[p.key] === undefined) {
            row[p.key] = '';
          }
        });

        return row;
      });

      setGridRows(prev => [...prev, ...pastedRows]);
      alert(`Đã dán thành công ${pastedRows.length} dòng từ Clipboard!`);
    } catch (err) {
      alert('Không thể đọc dữ liệu Clipboard (Hãy kiểm tra quyền truy cập Clipboard của trình duyệt).');
    }
  };

  // =========================================================================
  // SUB-MODAL 1: XUẤT FILE MẪU EXCEL
  // =========================================================================
  // 1. Xuất file mẫu các cột đang hiển thị trong danh sách (các cột bỏ tích)
  const handleExportTemplateVisible = () => {
    const headers = gridColumns.length > 0 ? gridColumns : propDefinitions;
    const sampleRow = {};
    headers.forEach(h => {
      sampleRow[h.colTitle] = '';
    });
    const ws = XLSX.utils.json_to_sheet([sampleRow]);
    const wb = XLSX.utils.book_new();
    XLSX.utils.book_append_sheet(wb, ws, 'MauHoiVien');
    XLSX.writeFile(wb, 'Mau_Nhap_HoiVien_CacCotHienThi.xlsx');
    setShowChoiceModal(false);
  };

  // 2. Xuất file mẫu tất cả các cột (bao gồm cả cột ẩn)
  const handleExportTemplateAll = () => {
    const sampleRow = {};
    propDefinitions.forEach(h => {
      sampleRow[h.colTitle] = '';
    });
    const ws = XLSX.utils.json_to_sheet([sampleRow]);
    const wb = XLSX.utils.book_new();
    XLSX.utils.book_append_sheet(wb, ws, 'MauHoiVien');
    XLSX.writeFile(wb, 'Mau_Nhap_HoiVien_TatCaCacCot.xlsx');
    setShowChoiceModal(false);
  };

  // =========================================================================
  // SUB-MODAL 2: ĐỌC FILE EXCEL & MỞ BẢNG GHÉP CỘT "THÊM TỪ EXCEL"
  // =========================================================================
  const handleFileSelected = (e) => {
    const file = e.target.files?.[0];
    if (!file) return;

    setShowChoiceModal(false);

    const reader = new FileReader();
    reader.onload = (evt) => {
      try {
        const buffer = evt.target.result;
        const wb = XLSX.read(buffer, { type: 'binary' });
        const sheet = wb.Sheets[wb.SheetNames[0]];
        const json = XLSX.utils.sheet_to_json(sheet);

        if (!json || json.length === 0) {
          alert('File Excel không có dữ liệu!');
          return;
        }

        // Lấy danh sách tên cột từ dòng đầu tiên của file Excel
        const detectedCols = Object.keys(json[0]);
        setExcelColumns(detectedCols);
        setRawExcelJson(json);

        // Khởi tạo ghép cột tự động
        const initialMap = {};
        detectedCols.forEach(eCol => {
          const cleanECol = eCol.toLowerCase().replace(/[^a-z0-9]/g, '');
          const match = propDefinitions.find(p => {
            const cleanSys = p.colTitle.toLowerCase().replace(/[^a-z0-9]/g, '');
            const cleanKey = p.key.toLowerCase();
            return cleanECol === cleanSys || cleanECol.includes(cleanSys) || cleanSys.includes(cleanECol) || cleanECol === cleanKey;
          });
          initialMap[eCol] = match ? match.key : '';
        });

        setColumnMapping(initialMap);
        setShowMappingModal(true);
      } catch (err) {
        console.error(err);
        alert('Lỗi đọc file Excel: ' + err.message);
      }
    };
    reader.readAsBinaryString(file);
    e.target.value = '';
  };

  // Nút "Tự động chọn" trong modal "Thêm từ excel"
  const handleAutoMatchMapping = () => {
    const newMap = {};
    excelColumns.forEach(eCol => {
      const cleanECol = eCol.toLowerCase().replace(/[^a-z0-9]/g, '');
      const match = propDefinitions.find(p => {
        const cleanSys = p.colTitle.toLowerCase().replace(/[^a-z0-9]/g, '');
        const cleanKey = p.key.toLowerCase();
        return cleanECol === cleanSys || cleanECol.includes(cleanSys) || cleanSys.includes(cleanECol) || cleanECol === cleanKey;
      });
      newMap[eCol] = match ? match.key : '';
    });
    setColumnMapping(newMap);
    alert('Đã tự động nhận diện và ghép các cột khớp nhau!');
  };

  // Xác nhận ghép cột -> Đổ vào lưới Thêm nhanh
  const handleApplyMappingToGrid = () => {
    // Tự động bỏ tích các trường đã được ghép cột từ Excel để cột đó hiện ra trên lưới!
    const mappedSysKeys = new Set(Object.values(columnMapping).filter(Boolean));
    setCommonProps(prev => {
      const updated = { ...prev };
      mappedSysKeys.forEach(sysKey => {
        if (updated[sysKey]) {
          updated[sysKey] = { ...updated[sysKey], isCommon: false }; // Bỏ tích -> Hiện trong lưới!
        }
      });
      return updated;
    });

    // Chuyển đổi dữ liệu từng dòng theo mapping đã ghép
    const mappedRows = rawExcelJson.map((row, idx) => {
      const r = { id: idx + 1 };
      propDefinitions.forEach(p => {
        r[p.key] = p.defaultValue !== undefined ? p.defaultValue : '';
      });

      // Gán dữ liệu từ các cột Excel theo mapping
      Object.keys(columnMapping).forEach(eCol => {
        const sysKey = columnMapping[eCol];
        if (sysKey && row[eCol] !== undefined) {
          r[sysKey] = String(row[eCol]).trim();
        }
      });

      return r;
    });

    setGridRows(mappedRows);
    setShowMappingModal(false);
    alert(`Đã nạp thành công ${mappedRows.length} dòng từ Excel vào lưới với đúng cấu hình cột đã ghép!`);
  };

  // =========================================================================
  // LƯU CƠ SỞ DỮ LIỆU
  // =========================================================================
  const handleAcceptSubmit = async () => {
    const finalRows = gridRows.map(row => {
      const r = { ...row };
      propDefinitions.forEach(p => {
        if (commonProps[p.key].isCommon) {
          r[p.key] = commonProps[p.key].value;
        }
      });
      return r;
    });

    const validRows = finalRows.filter(r => (r.tenKhachHang && r.tenKhachHang.trim().length > 0) || (r.maThe && r.maThe.trim().length > 0));
    if (validRows.length === 0) {
      alert('Không có dòng dữ liệu nào hợp lệ để nhập! Vui lòng điền thông tin khách hàng.');
      return;
    }

    setLoading(true);
    try {
      const payload = validRows.map(r => ({
        maThe: r.maThe || '',
        name: r.tenKhachHang || (r.maThe ? `Hội viên ${r.maThe}` : 'Khách hàng mới'),
        dienThoai: r.dienThoai || '',
        diaChi: r.diaChi || '',
        email: r.email || '',
        facebook: r.facebook || '',
        dLoaiTheId: r.loaiThe || '',
        dTrangThaiId: r.trangThai === 'Đang sử dụng' ? '1' : '0',
        dNhomKhachHangId: r.nhomKhachHang || '',
        dTinhThanhId: r.tinhThanh || '',
        maSoThue: r.maSoThue || '',
        diemTichLuyBanDau: Number(r.diemTichLuyBanDau) || 0,
        soLan: Number(r.soLan) || 0,
        daTap: Number(r.daTap) || 0,
        conLai: Number(r.conLai) || Number(r.soLan) || 0,
        tuNgay: r.tuNgay || '',
        denNgay: r.denNgay || '',
        ngaySinh: r.ngaySinh || '',
        anh: r.anh || '',
        note: r.note || ''
      }));

      const res = await khachHangService.batchImport(payload);
      if (res.success) {
        alert(res.message || `Đã nhập thành công ${res.importedCount || validRows.length} khách hàng!`);
        showNotification && showNotification(res.message || `Đã nhập thành công ${res.importedCount || validRows.length} khách hàng!`);
        onSuccess && onSuccess();
        onClose();
      } else {
        alert(res.message || 'Lỗi khi nhập khách hàng!');
      }
    } catch (err) {
      console.error(err);
      alert('Lỗi kết nối máy chủ khi lưu khách hàng!');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="fr-overlay">
      {/* ẨN FILE INPUT THỰC SỰ */}
      <input
        type="file"
        ref={fileInputRef}
        accept=".xlsx, .xls, .csv"
        style={{ display: 'none' }}
        onChange={handleFileSelected}
      />

      {/* ========================================================================= */}
      {/* CỬA SỔ CHÍNH: FORM THÊM NHANH (WINFORMS TÂN AN PHÁT)                      */}
      {/* ========================================================================= */}
      <div className="them-nhanh-window">
        {/* Title bar Windows Thêm nhanh */}
        <div className="them-nhanh-titlebar">
          <div className="them-nhanh-title">Thêm nhanh</div>
          <button className="them-nhanh-win-btn" onClick={onClose} title="Đóng">✕</button>
        </div>

        {/* Sub-header hướng dẫn */}
        <div className="them-nhanh-sub-header">
          <span className="star-icon">⭐</span>
          <span>Mời bạn điền danh sách Khách hàng vào lưới phía dưới.</span>
        </div>

        {/* Nội dung chính chia 2 cột: Trái (Lưới dữ liệu), Phải (Các thuộc tính chung) */}
        <div className="them-nhanh-main-split">
          {/* CỘT TRÁI: LƯỚI & TOOLBAR */}
          <div className="them-nhanh-left-pane">
            <div className="them-nhanh-toolbar">
              <span className="tn-tb-label">Số dòng</span>
              <input
                type="number"
                className="tn-tb-num-input"
                min="1"
                max="500"
                value={numRowsToAdd}
                onChange={(e) => setNumRowsToAdd(e.target.value)}
              />
              <button className="tn-tb-btn" onClick={handleAddRows} title="Thêm dòng trống">
                <i className="fa-solid fa-plus" style={{ color: '#16a34a', marginRight: 4 }}></i>
                Thêm dữ liệu
              </button>

              <button className="tn-tb-btn" onClick={handleDeleteRow} title="Xóa dòng đang chọn">
                <i className="fa-solid fa-xmark" style={{ color: '#dc2626', marginRight: 4 }}></i>
                Xóa dữ liệu
              </button>

              <button className="tn-tb-btn" onClick={handlePasteClipboard} title="Dán dữ liệu từ Clipboard (Ctrl+V)">
                <i className="fa-regular fa-clipboard" style={{ color: '#475569', marginRight: 4 }}></i>
                Dán dữ liệu
              </button>

              {/* BẤM NÚT NÀY MỞ FORM "CHỌN FILE EXCEL HOẶC XUẤT FILE MẪU" CHUẨN XÁC THEO ẢNH */}
              <button className="tn-tb-btn" onClick={() => setShowChoiceModal(true)} title="Chọn file Excel hoặc xuất file mẫu">
                <i className="fa-solid fa-file-excel" style={{ color: '#16a34a', marginRight: 4 }}></i>
                Chọn file Excel
              </button>
            </div>

            {/* BẢNG LƯỚI: CHỈ HIỂN THỊ CÁC CỘT BỊ 'BỎ TÍCH' Ở BẢNG BÊN PHẢI! */}
            <div className="them-nhanh-grid-box">
              <table className="them-nhanh-table">
                <thead>
                  <tr>
                    <th style={{ width: 35, textAlign: 'center' }}>#</th>
                    {gridColumns.map(col => (
                      <th key={col.key} style={{ minWidth: col.minWidth }}>
                        <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
                          <span>{col.colTitle}</span>
                          <span style={{ fontSize: 9, color: '#64748b' }}>▼</span>
                        </div>
                      </th>
                    ))}
                    {gridColumns.length === 0 && (
                      <th style={{ color: '#94a3b8', fontStyle: 'italic', padding: '12px' }}>
                        (Tất cả thuộc tính đang được tích chọn là Thuộc tính chung bên phải. Bỏ tích thuộc tính nào thì cột đó sẽ hiện ra tại đây để nhập từng dòng)
                      </th>
                    )}
                  </tr>
                </thead>
                <tbody>
                  {gridRows.map((r, idx) => (
                    <tr
                      key={r.id || idx}
                      className={selectedRowIndex === idx ? 'row-selected' : ''}
                      onClick={() => setSelectedRowIndex(idx)}
                    >
                      <td className="row-selector" style={{ textAlign: 'center' }}>
                        {selectedRowIndex === idx ? '►' : idx + 1}
                      </td>
                      {gridColumns.map(col => (
                        <td key={col.key}>
                          {col.type === 'select' ? (
                            <select
                              className="tn-cell-input"
                              value={r[col.key] || ''}
                              onChange={(e) => handleCellChange(idx, col.key, e.target.value)}
                            >
                              <option value="">-- Chọn --</option>
                              {col.key === 'loaiThe' && metadata.loaiThe?.map(lt => (
                                <option key={lt.id} value={lt.id}>{lt.name}</option>
                              ))}
                              {col.key === 'nhomKhachHang' && metadata.nhomKhach?.map(nk => (
                                <option key={nk.id} value={nk.id}>{nk.name}</option>
                              ))}
                              {col.key === 'nhanVien' && metadata.nhanVien?.map(nv => (
                                <option key={nv.id} value={nv.id}>{nv.name}</option>
                              ))}
                              {col.key === 'tinhThanh' && metadata.tinhThanh?.map(tt => (
                                <option key={tt.id} value={tt.id}>{tt.name}</option>
                              ))}
                              {col.key === 'trangThai' && (
                                <>
                                  <option value="Đang sử dụng">Đang sử dụng</option>
                                  <option value="Chưa kích hoạt">Chưa kích hoạt</option>
                                  <option value="Bảo lưu">Bảo lưu</option>
                                  <option value="Quá hạn">Quá hạn</option>
                                </>
                              )}
                            </select>
                          ) : (
                            <input
                              type={col.type === 'number' ? 'number' : col.type === 'date' ? 'date' : 'text'}
                              className={`tn-cell-input ${col.isYellow ? 'req-cell' : ''}`}
                              value={r[col.key] !== undefined ? r[col.key] : ''}
                              onChange={(e) => handleCellChange(idx, col.key, e.target.value)}
                            />
                          )}
                        </td>
                      ))}
                      {gridColumns.length === 0 && (
                        <td style={{ color: '#94a3b8', fontStyle: 'italic', textAlign: 'center' }}>
                          Dòng {idx + 1} sẽ áp dụng toàn bộ giá trị chung bên phải
                        </td>
                      )}
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>

            {/* Nút Xóa dữ liệu lỗi góc dưới bên trái */}
            <div className="them-nhanh-bottom-left">
              <button className="tn-btn-sub" onClick={handleRemoveInvalidRows}>
                Xóa dữ liệu lỗi
              </button>
            </div>
          </div>

          {/* CỘT PHẢI: CÁC THUỘC TÍNH CHUNG (ĐẦY ĐỦ 21 TRƯỜNG KÈM SCROLLBAR CHUẨN) */}
          <div className="them-nhanh-right-pane">
            <div className="tn-common-title">Các thuộc tính chung</div>

            <div className="tn-common-fields-list">
              {propDefinitions.map(prop => {
                const isChecked = commonProps[prop.key].isCommon;
                const val = commonProps[prop.key].value;

                return (
                  <div key={prop.key} className="tn-prop-row">
                    <input
                      type="checkbox"
                      checked={isChecked}
                      onChange={() => handleToggleCommon(prop.key)}
                      title={isChecked ? 'Bỏ tích để hiển thị cột này trong lưới' : 'Tích chọn để làm thuộc tính chung cho toàn bộ dòng'}
                    />
                    <span className="tn-prop-label">{prop.label}</span>

                    {prop.type === 'select' ? (
                      <select
                        className="tn-prop-select"
                        disabled={!isChecked}
                        value={val}
                        onChange={(e) => handleCommonValueChange(prop.key, e.target.value)}
                      >
                        <option value="">{prop.key === 'trangThai' ? 'Đang sử dụng' : ''}</option>
                        {prop.key === 'loaiThe' && metadata.loaiThe?.map(lt => (
                          <option key={lt.id} value={lt.id}>{lt.name}</option>
                        ))}
                        {prop.key === 'nhomKhachHang' && metadata.nhomKhach?.map(nk => (
                          <option key={nk.id} value={nk.id}>{nk.name}</option>
                        ))}
                        {prop.key === 'nhanVien' && metadata.nhanVien?.map(nv => (
                          <option key={nv.id} value={nv.id}>{nv.name}</option>
                        ))}
                        {prop.key === 'tinhThanh' && metadata.tinhThanh?.map(tt => (
                          <option key={tt.id} value={tt.id}>{tt.name}</option>
                        ))}
                        {prop.key === 'trangThai' && (
                          <>
                            <option value="Đang sử dụng">Đang sử dụng</option>
                            <option value="Chưa kích hoạt">Chưa kích hoạt</option>
                            <option value="Bảo lưu">Bảo lưu</option>
                            <option value="Quá hạn">Quá hạn</option>
                          </>
                        )}
                      </select>
                    ) : (
                      <input
                        type={prop.type === 'number' ? 'number' : prop.type === 'date' ? 'date' : 'text'}
                        className={`tn-prop-input ${prop.isYellow ? 'highlight-yellow' : ''}`}
                        disabled={!isChecked}
                        value={val}
                        onChange={(e) => handleCommonValueChange(prop.key, e.target.value)}
                      />
                    )}
                  </div>
                );
              })}
            </div>
          </div>
        </div>

        {/* Footer WinForms: Chấp nhận / Hủy bỏ */}
        <div className="them-nhanh-footer">
          <button
            className="tn-btn-primary"
            onClick={handleAcceptSubmit}
            disabled={loading}
          >
            {loading ? 'Đang lưu...' : 'Chấp nhận'}
          </button>
          <button className="tn-btn-cancel" onClick={onClose}>
            Hủy bỏ
          </button>
        </div>
      </div>

      {/* ========================================================================= */}
      {/* HỘP THOẠI 1: CHỌN FILE EXCEL HOẶC XUẤT FILE MẪU (KHỚP 100% ẢNH 2)         */}
      {/* ========================================================================= */}
      {showChoiceModal && (
        <div className="sub-modal-backdrop">
          <div className="choice-dialog-window">
            <div className="choice-dialog-titlebar">
              <span>Chọn file excel hoặc xuất file mẫu</span>
              <button className="choice-dialog-close" onClick={() => setShowChoiceModal(false)}>✕</button>
            </div>

            <div className="choice-dialog-body">
              <div className="choice-section-hint">Nếu bạn chưa có file excel, click vào "Xuất file mẫu"</div>

              <div className="choice-action-buttons">
                <button className="choice-action-btn" onClick={handleExportTemplateVisible}>
                  Xuất file mẫu các cột đang hiển thị trong danh sách
                </button>
                <button className="choice-action-btn" onClick={handleExportTemplateAll}>
                  Xuất file mẫu tất cả các cột (bao gồm cả cột ẩn)
                </button>
              </div>

              <div className="choice-section-hint" style={{ marginTop: 14 }}>
                Nếu đã có mẫu, click "Chọn file excel"
              </div>

              <div className="choice-footer-buttons">
                <button
                  className="choice-btn-primary"
                  onClick={() => fileInputRef.current?.click()}
                >
                  Chọn file excel
                </button>
                <button
                  className="choice-btn-cancel"
                  onClick={() => setShowChoiceModal(false)}
                >
                  Hủy bỏ
                </button>
              </div>
            </div>
          </div>
        </div>
      )}

      {/* ========================================================================= */}
      {/* HỘP THOẠI 2: THÊM TỪ EXCEL - GHÉP CỘT (KHỚP 100% ẢNH 1)                   */}
      {/* ========================================================================= */}
      {showMappingModal && (
        <div className="sub-modal-backdrop">
          <div className="mapping-dialog-window">
            <div className="mapping-dialog-titlebar">
              <span>Thêm từ excel</span>
              <button className="mapping-dialog-close" onClick={() => setShowMappingModal(false)}>✕</button>
            </div>

            <div className="mapping-dialog-body">
              <div className="mapping-guide">
                <p><strong>Hướng dẫn:</strong> Cột bên trái là cột sẵn có trong file excel của bạn.</p>
                <p>Bạn lựa chọn cột bên phải để hệ thống hiểu được cột trong file excel là dữ liệu nào</p>
              </div>

              {/* BẢNG ÁNH XẠ CỘT EXCEL -> HỆ THỐNG */}
              <div className="mapping-table-container">
                <table className="mapping-table">
                  <thead>
                    <tr>
                      <th style={{ width: '50%' }}>Cột excel</th>
                      <th style={{ width: '50%' }}>Dữ liệu</th>
                    </tr>
                  </thead>
                  <tbody>
                    {excelColumns.map((colName, idx) => (
                      <tr
                        key={colName}
                        className={selectedMappingIndex === idx ? 'mapping-row-selected' : ''}
                        onClick={() => setSelectedMappingIndex(idx)}
                      >
                        <td className="mapping-excel-cell">
                          {colName}
                        </td>
                        <td className="mapping-sys-cell">
                          <select
                            className="mapping-select"
                            value={columnMapping[colName] || ''}
                            onChange={(e) => setColumnMapping(prev => ({ ...prev, [colName]: e.target.value }))}
                          >
                            <option value="">-- Bỏ qua cột này --</option>
                            {propDefinitions.map(p => (
                              <option key={p.key} value={p.key}>
                                {p.colTitle}
                              </option>
                            ))}
                          </select>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>

              {/* HÀNG NÚT DƯỚI CÙNG CỦA THÊM TỪ EXCEL */}
              <div className="mapping-dialog-footer">
                <button
                  className="mapping-btn-auto"
                  onClick={handleAutoMatchMapping}
                >
                  Tự động chọn
                </button>

                <div className="mapping-btn-right-group">
                  <button
                    className="mapping-btn-accept"
                    onClick={handleApplyMappingToGrid}
                  >
                    Chấp nhận
                  </button>
                  <button
                    className="mapping-btn-cancel"
                    onClick={() => setShowMappingModal(false)}
                  >
                    Hủy bỏ
                  </button>
                </div>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default ExcelImportModal;
