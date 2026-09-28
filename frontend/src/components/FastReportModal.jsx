import React, { useEffect, useMemo, useState } from 'react';
import * as XLSX from 'xlsx';

const CUSTOMER_PRINT_COLUMNS = [
  { key: 'maThe', label: 'Mã thẻ', width: '85px', align: 'left', defaultChecked: true },
  { key: 'tenKhachHang', label: 'Tên khách hàng', width: '160px', align: 'left', defaultChecked: true },
  { key: 'diaChi', label: 'Địa chỉ', width: '140px', align: 'left', defaultChecked: true },
  { key: 'dienThoai', label: 'Điện thoại', width: '100px', align: 'left', defaultChecked: true },
  { key: 'loaiThe', label: 'Loại thẻ', width: '100px', align: 'left', defaultChecked: true },
  { key: 'tuNgay', label: 'Từ ngày', width: '85px', align: 'center', defaultChecked: true },
  { key: 'denNgay', label: 'Đến ngày', width: '85px', align: 'center', defaultChecked: true },
  { key: 'trangThai', label: 'Trạng thái', width: '110px', align: 'left', defaultChecked: true },
  { key: 'soLan', label: 'Số lần', width: '60px', align: 'right', defaultChecked: true },
  { key: 'daTap', label: 'Đã tập', width: '60px', align: 'right', defaultChecked: true },
  { key: 'conLai', label: 'Còn lại', width: '60px', align: 'right', defaultChecked: true },
  { key: 'facebook', label: 'Facebook', width: '100px', align: 'left', defaultChecked: true },
  { key: 'note', label: 'Ghi chú', width: '100px', align: 'left', defaultChecked: true },
  { key: 'ngaySinh', label: 'Ngày thành lập/sinh nhật', width: '110px', align: 'center', defaultChecked: true }
];

export const FastReportModal = ({
  show,
  onClose,
  customers = [],
  selectedCustomer = null,
  showNotification,
  rows = null,
  columns = null,
  initialTitle = 'Khách hàng',
  sheetName = 'KhachHang',
  companyInfo = null
}) => {
  // View state: 'in_luoi' (Form In lưới) | 'fastreport_preview' (Cửa sổ In danh sách)
  const [viewState, setViewState] = useState('in_luoi');

  // Form In lưới State (Khớp 100% ảnh chụp màn hình WinForms của Tân An Phát)
  const [reportTitle, setReportTitle] = useState(initialTitle);
  const [reportNote, setReportNote] = useState('');
  const [printOrientation, setPrintOrientation] = useState('landscape'); // 'landscape' (A4 nằm ngang) | 'portrait' (A4 thẳng đứng)
  const [includeStt, setIncludeStt] = useState(true);

  const availableColumns = useMemo(
    () => (Array.isArray(columns) && columns.length > 0 ? columns : CUSTOMER_PRINT_COLUMNS),
    [columns]
  );
  const printRows = Array.isArray(rows) ? rows : customers;

  const [selectedColumns, setSelectedColumns] = useState(() => {
    const init = {};
    availableColumns.forEach(c => {
      init[c.key] = c.defaultChecked;
    });
    return init;
  });

  useEffect(() => {
    if (!show) return;
    const nextSelection = {};
    availableColumns.forEach((column) => {
      nextSelection[column.key] = column.defaultChecked !== false;
    });
    setSelectedColumns(nextSelection);
    setReportTitle(initialTitle || 'Khách hàng');
    setReportNote('');
    setPrintOrientation('landscape');
    setIncludeStt(true);
    setViewState('in_luoi');
  }, [show, initialTitle, availableColumns]);

  if (!show) return null;

  const toggleColumn = (key) => {
    setSelectedColumns(prev => ({
      ...prev,
      [key]: !prev[key]
    }));
  };

  const handlePrint = () => {
    window.print();
  };

  const handleExportExcel = () => {
    if (printRows.length === 0) {
      alert('Không có dữ liệu để xuất Excel!');
      return;
    }
    const activeCols = availableColumns.filter(c => selectedColumns[c.key]);
    const exportRows = printRows.map((c, idx) => {
      const row = {};
      if (includeStt) row['STT'] = idx + 1;
      activeCols.forEach(col => {
        row[col.label] = typeof col.format === 'function'
          ? col.format(c[col.key], c)
          : (c[col.key] ?? '');
      });
      return row;
    });

    const ws = XLSX.utils.json_to_sheet(exportRows);
    const wb = XLSX.utils.book_new();
    XLSX.utils.book_append_sheet(wb, ws, String(sheetName || 'DuLieu').slice(0, 31));
    XLSX.writeFile(wb, `In_Danh_Sach_${new Date().toISOString().slice(0, 10)}.xlsx`);
  };

  const activeCols = availableColumns.filter(c => selectedColumns[c.key]);
  const companyLogoSrc = companyInfo?.logoBase64
    ? (companyInfo.logoBase64.startsWith('data:')
        ? companyInfo.logoBase64
        : `data:image/png;base64,${companyInfo.logoBase64}`)
    : '';

  return (
    <div className="fr-overlay">
      {/* ========================================================================= */}
      {/* FORM 1: HỘP THOẠI "IN LƯỚI" (CẤU HÌNH TRANG IN) CHUẨN XÁC THEO DESKTOP   */}
      {/* ========================================================================= */}
      {viewState === 'in_luoi' && (
        <div className="in-luoi-dialog">
          {/* Header thanh tiêu đề WinForms In lưới */}
          <div className="in-luoi-titlebar">
            <div className="in-luoi-title">
              <i className="fa-solid fa-print" style={{ marginRight: 6, color: '#475569' }}></i>
              In lưới
            </div>
            <button className="in-luoi-close-btn" onClick={onClose} title="Thoát">✕</button>
          </div>

          {/* Tab Cấu hình trang in */}
          <div className="in-luoi-tabs">
            <div className="in-luoi-tab active">
              <i className="fa-solid fa-print" style={{ marginRight: 6, color: '#64748b' }}></i>
              Cấu hình trang in
            </div>
          </div>

          <div className="in-luoi-body">
            <fieldset className="in-luoi-fieldset">
              <legend>Thiết lập</legend>

              <div className="in-luoi-row">
                <label className="in-luoi-label">Tiêu đề:</label>
                <input
                  type="text"
                  className="in-luoi-input"
                  value={reportTitle}
                  onChange={(e) => setReportTitle(e.target.value)}
                />
              </div>

              <div className="in-luoi-row">
                <label className="in-luoi-label">Ghi chú:</label>
                <input
                  type="text"
                  className="in-luoi-input"
                  value={reportNote}
                  onChange={(e) => setReportNote(e.target.value)}
                />
              </div>

              <div className="in-luoi-row align-top">
                <label className="in-luoi-label">Mẫu in:</label>
                <div className="in-luoi-mauin-box">
                  <div
                    className={`in-luoi-mauin-item ${printOrientation === 'landscape' ? 'selected' : ''}`}
                    onClick={() => setPrintOrientation('landscape')}
                  >
                    <i className="fa-regular fa-file" style={{ marginRight: 8, color: '#3b82f6' }}></i>
                    <span>Mẫu A4 nằm ngang</span>
                  </div>
                  <div
                    className={`in-luoi-mauin-item ${printOrientation === 'portrait' ? 'selected' : ''}`}
                    onClick={() => setPrintOrientation('portrait')}
                  >
                    <i className="fa-regular fa-file" style={{ marginRight: 8, color: '#3b82f6' }}></i>
                    <span>Mẫu A4 thẳng đứng</span>
                  </div>
                </div>
              </div>

              <div className="in-luoi-row align-top">
                <label className="in-luoi-label">Cột hiển thị:</label>
                <div className="in-luoi-columns-box">
                  {availableColumns.map(col => (
                    <label key={col.key} className="in-luoi-checkbox-row">
                      <input
                        type="checkbox"
                        checked={!!selectedColumns[col.key]}
                        onChange={() => toggleColumn(col.key)}
                      />
                      <span>{col.label}</span>
                    </label>
                  ))}
                </div>
              </div>

              <div className="in-luoi-row" style={{ paddingLeft: 95, marginTop: 4 }}>
                <label className="in-luoi-checkbox-row" style={{ fontWeight: 500 }}>
                  <input
                    type="checkbox"
                    checked={includeStt}
                    onChange={(e) => setIncludeStt(e.target.checked)}
                  />
                  <span>In cột số thứ tự</span>
                </label>
              </div>
            </fieldset>
          </div>

          {/* Action buttons ở dưới cùng */}
          <div className="in-luoi-footer">
            <button
              className="in-luoi-btn primary"
              onClick={() => setViewState('fastreport_preview')}
            >
              Hiển thị
            </button>
            <button className="in-luoi-btn" onClick={onClose}>
              Thoát
            </button>
          </div>
        </div>
      )}

      {/* ========================================================================= */}
      {/* FORM 2: CỬA SỔ FASTREPORT "IN DANH SÁCH" CHUẨN XÁC THEO ẢNH CHỤP 1       */}
      {/* ========================================================================= */}
      {viewState === 'fastreport_preview' && (
        <div className="fr-viewer-window">
          {/* Thanh tiêu đề Windows Form: In danh sách */}
          <div className="fr-window-titlebar-classic">
            <div className="fr-titlebar-left-classic">
              <i className="fa-solid fa-magnifying-glass" style={{ marginRight: 6, fontSize: 13, color: '#3b82f6' }}></i>
              <span>In danh sách</span>
            </div>
            <div className="fr-titlebar-right-classic">
              <button className="fr-back-btn" onClick={() => setViewState('in_luoi')} title="Quay lại Cấu hình trang in">
                <i className="fa-solid fa-arrow-left"></i> Cấu hình
              </button>
              <button className="fr-win-close-btn" onClick={onClose} title="Close">✕</button>
            </div>
          </div>

          {/* Thanh Toolbar FastReport chuẩn (Print, Save, Export, Zoom, Navigation...) */}
          <div className="fr-toolbar-classic">
            <button className="fr-tb-item" onClick={handlePrint} title="Print (In)">
              <i className="fa-solid fa-print"></i>
              <span>Print</span>
            </button>

            <button className="fr-tb-item" onClick={handleExportExcel} title="Save (Xuất Excel)">
              <i className="fa-solid fa-floppy-disk"></i>
              <span>Save</span>
              <i className="fa-solid fa-caret-down" style={{ fontSize: 9, marginLeft: 2 }}></i>
            </button>

            <button className="fr-tb-item icon-btn" onClick={handlePrint} title="Xuất PDF">
              <i className="fa-solid fa-file-pdf" style={{ color: '#dc2626' }}></i>
            </button>

            <button className="fr-tb-item icon-btn" title="Gửi Email">
              <i className="fa-regular fa-envelope"></i>
            </button>

            <div className="fr-tb-sep"></div>

            <button className="fr-tb-item icon-btn" title="Cài đặt trang">
              <i className="fa-solid fa-sliders"></i>
            </button>

            <button className="fr-tb-item icon-btn" title="Chỉnh sửa bản in">
              <i className="fa-solid fa-pen-to-square"></i>
            </button>

            <div className="fr-tb-sep"></div>

            {/* Điều hướng trang */}
            <button className="fr-tb-item icon-btn" disabled title="Trang đầu">
              <i className="fa-solid fa-backward-fast"></i>
            </button>
            <button className="fr-tb-item icon-btn" disabled title="Trang trước">
              <i className="fa-solid fa-caret-left"></i>
            </button>
            <span className="fr-tb-page">1 of 1</span>
            <button className="fr-tb-item icon-btn" disabled title="Trang sau">
              <i className="fa-solid fa-caret-right"></i>
            </button>
            <button className="fr-tb-item icon-btn" disabled title="Trang cuối">
              <i className="fa-solid fa-forward-fast"></i>
            </button>

            <div className="fr-tb-sep"></div>

            <button className="fr-tb-item" onClick={onClose} title="Close (Đóng)">
              <span>Close</span>
            </button>
          </div>

          {/* Vùng hiển thị giấy in FastReport trên nền xanh xám chuẩn */}
          <div className="fr-canvas-classic">
            <div
              className={`fr-sheet-classic ${printOrientation}`}
              id="fastreport-printable-area"
            >
              {/* Header Công Ty (Căn giữa) */}
              <div className="fr-company-header-classic">
                {companyLogoSrc && (
                  <img
                    src={companyLogoSrc}
                    alt="Logo công ty"
                    className="fr-company-logo-classic"
                  />
                )}
                <div className="fr-company-info-classic">
                  <div className="fr-company-name-classic">{companyInfo?.name || '(TÊN CÔNG TY)'}</div>
                  <div className="fr-company-meta-classic">Địa chỉ: {companyInfo?.address || '(ĐỊA CHỈ)'}</div>
                  <div className="fr-company-meta-classic">
                    Điện thoại: {companyInfo?.phone || '(ĐIỆN THOẠI)'}, Email: {companyInfo?.email || '(EMAIL)'}
                  </div>
                </div>
              </div>

              {/* Đường kẻ cam ngang đặc trưng */}
              <div className="fr-orange-line"></div>

              {/* Tiêu đề căn phải theo đúng ảnh */}
              <div className="fr-report-title-right">
                {reportTitle || 'Khách hàng'}
              </div>

              {/* Bảng dữ liệu in danh sách theo đúng cột đã chọn */}
              <table className="fr-table-classic">
                <thead>
                  <tr>
                    {includeStt && <th style={{ width: '38px', textAlign: 'center' }}>STT</th>}
                    {activeCols.map(col => (
                      <th
                        key={col.key}
                        style={{ width: col.width, textAlign: col.align }}
                      >
                        {col.label}
                      </th>
                    ))}
                  </tr>
                </thead>
                <tbody>
                  {printRows.length === 0 ? (
                    <tr>
                      <td colSpan={activeCols.length + (includeStt ? 1 : 0)} style={{ textAlign: 'center', padding: '16px' }}>
                        Không có dữ liệu nào để in.
                      </td>
                    </tr>
                  ) : (
                    printRows.map((c, idx) => (
                      <tr key={c.id || idx}>
                        {includeStt && <td style={{ textAlign: 'center' }}>{idx + 1}</td>}
                        {activeCols.map(col => {
                          const val = typeof col.format === 'function'
                            ? col.format(c[col.key], c)
                            : (c[col.key] ?? '');
                          return (
                            <td
                              key={col.key}
                              style={{ textAlign: col.align }}
                            >
                              {val}
                            </td>
                          );
                        })}
                      </tr>
                    ))
                  )}
                </tbody>
              </table>

              {/* Chữ ký 2 bên: Trưởng phòng & Người lập (Khớp 100% ảnh 1) */}
              <div className="fr-signatures-classic">
                <div className="fr-sig-box-left">
                  <div className="fr-sig-title-classic">Trưởng phòng</div>
                  <div className="fr-sig-hint-classic">(Ký, họ tên)</div>
                </div>

                <div className="fr-sig-box-right">
                  <div className="fr-sig-title-classic">Người lập</div>
                  <div className="fr-sig-hint-classic">(Ký, họ tên)</div>
                </div>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default FastReportModal;
