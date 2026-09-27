import React, { useState, useEffect } from 'react';
import { khachHangService } from '../services/khachHangService';
import SlipPatternConfigModal from './SlipPatternConfigModal';

export default function CustomerSystemConfigModal({
  show,
  onClose,
  initialConfigs = {},
  onSave,
  canManageConfig = true
}) {
  const [activeTab, setActiveTab] = useState('soPhieu'); // 'soPhieu' | 'khachHang' | 'theGym' | 'thietBi'
  const [configs, setConfigs] = useState({
    ChoPhepTrungTenKhachHang: false,
    ThongBaoKhachDenNgaySinhNhat: false,
    GiaHanTheKhiThemKhachHang: false,
    ChoPhepNhapBangBanPhim: true,
    CoPhanCaTap: false,
    TuDongTaoXoaThe: false,
    CoSuDungTheTheoLan: false,
    SoNgayCanhBaoSapHetHan: 7,
    SoLanCanhBaoSapHet: 3,
    ChoPhepKhachNoGym: true
  });
  const [slipConfigs, setSlipConfigs] = useState([]);
  const [slipFilter, setSlipFilter] = useState('');
  const [editingSlip, setEditingSlip] = useState(null);
  const [loadingSlips, setLoadingSlips] = useState(false);
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    if (show && initialConfigs) {
      setConfigs(prev => ({
        ...prev,
        ...initialConfigs,
        ChoPhepTrungTenKhachHang: !!initialConfigs.ChoPhepTrungTenKhachHang,
        ThongBaoKhachDenNgaySinhNhat: !!initialConfigs.ThongBaoKhachDenNgaySinhNhat,
        GiaHanTheKhiThemKhachHang: !!initialConfigs.GiaHanTheKhiThemKhachHang,
        ChoPhepNhapBangBanPhim: initialConfigs.ChoPhepNhapBangBanPhim !== false,
        CoPhanCaTap: !!initialConfigs.CoPhanCaTap,
        TuDongTaoXoaThe: !!initialConfigs.TuDongTaoXoaThe,
        CoSuDungTheTheoLan: !!initialConfigs.CoSuDungTheTheoLan,
        SoNgayCanhBaoSapHetHan: Number(initialConfigs.SoNgayCanhBaoSapHetHan) || 7,
        SoLanCanhBaoSapHet: Number(initialConfigs.SoLanCanhBaoSapHet) || 3,
        ChoPhepKhachNoGym: initialConfigs.ChoPhepKhachNoGym !== false
      }));
    }
  }, [show, initialConfigs]);

  // Tải danh sách cấu hình số phiếu khi mở modal
  useEffect(() => {
    if (show) {
      loadSlipConfigs();
    }
  }, [show]);

  const loadSlipConfigs = async () => {
    try {
      setLoadingSlips(true);
      const res = await khachHangService.getSlipConfigs();
      if (res && res.success && res.items) {
        setSlipConfigs(res.items);
      }
    } catch (err) {
      console.error('Lỗi tải cấu hình số phiếu:', err);
    } finally {
      setLoadingSlips(false);
    }
  };

  if (!show) return null;

  const handleChange = (key, value) => {
    setConfigs(prev => ({ ...prev, [key]: value }));
  };

  const handleSlipTemplateChange = (key, newTemplate) => {
    setSlipConfigs(prev =>
      prev.map(item => item.key === key ? { ...item, template: newTemplate } : item)
    );
  };

  const handleSaveSlipModal = (key, newTemplate) => {
    handleSlipTemplateChange(key, newTemplate);
    setEditingSlip(null);
  };

  const handleSave = async () => {
    try {
      setSaving(true);
      // 1. Lưu cấu hình chung SCONFIG
      if (onSave) {
        await onSave(configs);
      }

      // 2. Lưu cấu hình mẫu số phiếu (NOTEMPLATE trong STABLEDESC và SFORM)
      if (slipConfigs.length > 0) {
        await khachHangService.updateSlipConfigs(
          slipConfigs.map(s => ({
            key: s.key,
            source: s.source,
            sourceId: s.sourceId,
            template: s.template || ''
          }))
        );
      }

      onClose();
    } catch (err) {
      console.error('Lỗi lưu cấu hình:', err);
      alert('Lỗi lưu cấu hình: ' + (err.message || err));
    } finally {
      setSaving(false);
    }
  };

  const filteredSlips = slipConfigs.filter(s =>
    !slipFilter ||
    s.name.toLowerCase().includes(slipFilter.toLowerCase()) ||
    (s.template && s.template.toLowerCase().includes(slipFilter.toLowerCase()))
  );

  return (
    <>
      <div className="cust-modal-overlay">
        <div className="cust-modal-box" style={{ width: 750, maxWidth: '95vw', minHeight: 480 }}>
          {/* MODAL HEADER */}
          <div className="cust-modal-header" style={{ background: 'linear-gradient(180deg, #1e3a8a 0%, #1e40af 100%)', color: '#fff' }}>
            <div style={{ display: 'flex', alignItems: 'center', gap: 8, fontSize: 14, fontWeight: 700 }}>
              <i className="fa-solid fa-sliders"></i>
              <span>CẤU HÌNH TOÀN HỆ THỐNG</span>
            </div>
            <button className="cust-modal-close" onClick={onClose} title="Đóng">
              <i className="fa-solid fa-xmark"></i>
            </button>
          </div>

          {/* TAB STRIP */}
          <div style={{ display: 'flex', background: '#e2e8f0', borderBottom: '1px solid #cbd5e1', padding: '4px 8px 0', gap: 4 }}>
            <button
              type="button"
              className={`cust-pane-tab ${activeTab === 'soPhieu' ? 'active' : ''}`}
              onClick={() => setActiveTab('soPhieu')}
              style={{ height: 32, padding: '0 16px', fontSize: 13, fontWeight: 600 }}
            >
              <i className="fa-solid fa-receipt" style={{ marginRight: 6, color: '#0284c7' }}></i>
              Số phiếu
            </button>
            <button
              type="button"
              className={`cust-pane-tab ${activeTab === 'khachHang' ? 'active' : ''}`}
              onClick={() => setActiveTab('khachHang')}
              style={{ height: 32, padding: '0 16px', fontSize: 13, fontWeight: 600 }}
            >
              <i className="fa-solid fa-user-group" style={{ marginRight: 6, color: '#0284c7' }}></i>
              Khách hàng & Hội viên
            </button>
            <button
              type="button"
              className={`cust-pane-tab ${activeTab === 'theGym' ? 'active' : ''}`}
              onClick={() => setActiveTab('theGym')}
              style={{ height: 32, padding: '0 16px', fontSize: 13, fontWeight: 600 }}
            >
              <i className="fa-solid fa-id-card" style={{ marginRight: 6, color: '#16a34a' }}></i>
              Thẻ Gym & Gói tập
            </button>
            <button
              type="button"
              className={`cust-pane-tab ${activeTab === 'thietBi' ? 'active' : ''}`}
              onClick={() => setActiveTab('thietBi')}
              style={{ height: 32, padding: '0 16px', fontSize: 13, fontWeight: 600 }}
            >
              <i className="fa-solid fa-microchip" style={{ marginRight: 6, color: '#f59e0b' }}></i>
              Thiết bị & Tự động
            </button>
          </div>

          {/* TAB BODY */}
          <div style={{ padding: '16px 20px', maxHeight: '60vh', overflowY: 'auto', background: '#f8fafc' }}>
            {/* TAB: SỐ PHIẾU (NOTEMPLATE) */}
            {activeTab === 'soPhieu' && (
              <div>
                <div style={{ marginBottom: 12, display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
                  <div style={{ fontSize: 12.5, color: '#475569' }}>
                    Thiết lập định dạng sinh mã số tự động cho các loại phiếu, chứng từ trong toàn hệ thống.
                  </div>
                </div>

                {loadingSlips ? (
                  <div style={{ textAlign: 'center', padding: '30px 0', color: '#64748b' }}>
                    <i className="fa-solid fa-spinner fa-spin" style={{ marginRight: 8 }}></i>
                    Đang tải danh sách số phiếu...
                  </div>
                ) : (
                  <div
                    style={{
                      background: '#fff',
                      border: '1px solid #cbd5e1',
                      borderRadius: 4,
                      maxHeight: '44vh',
                      overflowY: 'auto'
                    }}
                  >
                    <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: 13 }}>
                      <thead>
                        <tr style={{ background: '#f1f5f9', borderBottom: '1px solid #cbd5e1', position: 'sticky', top: 0, zIndex: 1 }}>
                          <th style={{ textAlign: 'left', padding: '8px 12px', fontWeight: 600, color: '#334155', width: '32%' }}>
                            Tên phiếu / Nghiệp vụ
                          </th>
                          <th style={{ textAlign: 'left', padding: '8px 12px', fontWeight: 600, color: '#334155', width: '40%' }}>
                            Mẫu số phiếu
                          </th>
                          <th style={{ textAlign: 'left', padding: '8px 12px', fontWeight: 600, color: '#334155', width: '20%' }}>
                            Số kế tiếp
                          </th>
                          <th style={{ textAlign: 'center', padding: '8px 12px', fontWeight: 600, color: '#334155', width: '8%' }}>
                            ...
                          </th>
                        </tr>
                      </thead>
                      <tbody>
                        {filteredSlips.map((item, idx) => (
                          <tr
                            key={item.key}
                            style={{
                              borderBottom: '1px solid #f1f5f9',
                              backgroundColor: idx % 2 === 0 ? '#ffffff' : '#fafafa'
                            }}
                          >
                            <td style={{ padding: '6px 12px', fontWeight: 500, color: '#1e293b' }}>
                              {item.name}
                            </td>
                            <td style={{ padding: '6px 12px' }}>
                              <input
                                type="text"
                                value={item.template || ''}
                                onChange={(e) => handleSlipTemplateChange(item.key, e.target.value)}
                                style={{
                                  width: '100%',
                                  height: 26,
                                  padding: '0 8px',
                                  fontSize: 12.5,
                                  fontFamily: 'Consolas, monospace',
                                  border: '1px solid #cbd5e1',
                                  borderRadius: 3,
                                  backgroundColor: '#fff',
                                  color: '#0f172a'
                                }}
                                placeholder="VD: BG(yy)/(*****)"
                              />
                            </td>
                            <td style={{ padding: '6px 12px', color: '#2563eb', fontFamily: 'Consolas, monospace', fontWeight: 600, fontSize: 12 }}>
                              {item.sample || '---'}
                            </td>
                            <td style={{ padding: '6px 8px', textAlign: 'center' }}>
                              <button
                                type="button"
                                onClick={() => setEditingSlip(item)}
                                title={`Cấu hình chi tiết ${item.name}`}
                                style={{
                                  padding: '2px 8px',
                                  height: 24,
                                  fontSize: 12,
                                  border: '1px solid #cbd5e1',
                                  borderRadius: 3,
                                  backgroundColor: '#f8fafc',
                                  cursor: 'pointer',
                                  color: '#334155',
                                  fontWeight: 700
                                }}
                                onMouseEnter={(e) => e.currentTarget.style.backgroundColor = '#e2e8f0'}
                                onMouseLeave={(e) => e.currentTarget.style.backgroundColor = '#f8fafc'}
                              >
                                ...
                              </button>
                            </td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                )}
              </div>
            )}

            {/* TAB 1: KHÁCH HÀNG */}
            {activeTab === 'khachHang' && (
              <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
                <div style={{ background: '#fff', border: '1px solid #e2e8f0', borderRadius: 6, padding: '14px 16px' }}>
                  <label style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', cursor: 'pointer' }}>
                    <div>
                      <div style={{ fontWeight: 600, fontSize: 13, color: '#0f172a' }}>Cho phép trùng tên khách hàng</div>
                      <div style={{ fontSize: 12, color: '#64748b', marginTop: 2 }}>
                        Nếu tắt: Hệ thống sẽ cảnh báo khi tạo hoặc sửa khách hàng có họ tên đã tồn tại trong CSDL.
                      </div>
                    </div>
                    <input
                      type="checkbox"
                      checked={configs.ChoPhepTrungTenKhachHang}
                      onChange={(e) => handleChange('ChoPhepTrungTenKhachHang', e.target.checked)}
                      style={{ width: 18, height: 18, cursor: 'pointer', accentColor: '#0284c7' }}
                    />
                  </label>
                </div>

                <div style={{ background: '#fff', border: '1px solid #e2e8f0', borderRadius: 6, padding: '14px 16px' }}>
                  <label style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', cursor: 'pointer' }}>
                    <div>
                      <div style={{ fontWeight: 600, fontSize: 13, color: '#0f172a' }}>Cho phép nhập mã khách bằng bàn phím</div>
                      <div style={{ fontSize: 12, color: '#64748b', marginTop: 2 }}>
                        Cho phép nhân viên gõ tay mã thẻ/mã khách. Nếu tắt, bắt buộc phải quẹt thẻ hoặc quét đầu đọc.
                      </div>
                    </div>
                    <input
                      type="checkbox"
                      checked={configs.ChoPhepNhapBangBanPhim}
                      onChange={(e) => handleChange('ChoPhepNhapBangBanPhim', e.target.checked)}
                      style={{ width: 18, height: 18, cursor: 'pointer', accentColor: '#0284c7' }}
                    />
                  </label>
                </div>

                <div style={{ background: '#fff', border: '1px solid #e2e8f0', borderRadius: 6, padding: '14px 16px' }}>
                  <label style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', cursor: 'pointer' }}>
                    <div>
                      <div style={{ fontWeight: 600, fontSize: 13, color: '#0f172a' }}>Thông báo khách đến ngày sinh nhật</div>
                      <div style={{ fontSize: 12, color: '#64748b', marginTop: 2 }}>
                        Tự động hiển thị lời nhắc chúc mừng hoặc ưu đãi khi khách hàng đến phòng tập vào ngày sinh nhật.
                      </div>
                    </div>
                    <input
                      type="checkbox"
                      checked={configs.ThongBaoKhachDenNgaySinhNhat}
                      onChange={(e) => handleChange('ThongBaoKhachDenNgaySinhNhat', e.target.checked)}
                      style={{ width: 18, height: 18, cursor: 'pointer', accentColor: '#0284c7' }}
                    />
                  </label>
                </div>
              </div>
            )}

            {/* TAB 2: THẺ GYM & GÓI TẬP */}
            {activeTab === 'theGym' && (
              <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
                <div style={{ background: '#fff', border: '1px solid #e2e8f0', borderRadius: 6, padding: '14px 16px' }}>
                  <label style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', cursor: 'pointer' }}>
                    <div>
                      <div style={{ fontWeight: 600, fontSize: 13, color: '#0f172a' }}>Gia hạn thẻ khi thêm khách hàng</div>
                      <div style={{ fontSize: 12, color: '#64748b', marginTop: 2 }}>
                        Khi thêm mới hội viên thành công, tự động mở cửa sổ Gia hạn thẻ/Đăng ký gói tập ngay.
                      </div>
                    </div>
                    <input
                      type="checkbox"
                      checked={configs.GiaHanTheKhiThemKhachHang}
                      onChange={(e) => handleChange('GiaHanTheKhiThemKhachHang', e.target.checked)}
                      style={{ width: 18, height: 18, cursor: 'pointer', accentColor: '#0284c7' }}
                    />
                  </label>
                </div>

                <div style={{ background: '#fff', border: '1px solid #e2e8f0', borderRadius: 6, padding: '14px 16px' }}>
                  <label style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', cursor: 'pointer' }}>
                    <div>
                      <div style={{ fontWeight: 600, fontSize: 13, color: '#0f172a' }}>Cho phép khách nợ khi đăng ký, gia hạn gym</div>
                      <div style={{ fontSize: 12, color: '#64748b', marginTop: 2 }}>
                        Cho phép thanh toán một phần hoặc ghi nợ vào sổ công nợ khi đăng ký/gia hạn gói tập.
                      </div>
                    </div>
                    <input
                      type="checkbox"
                      checked={configs.ChoPhepKhachNoGym}
                      onChange={(e) => handleChange('ChoPhepKhachNoGym', e.target.checked)}
                      style={{ width: 18, height: 18, cursor: 'pointer', accentColor: '#0284c7' }}
                    />
                  </label>
                </div>

                <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 12 }}>
                  <div style={{ background: '#fff', border: '1px solid #e2e8f0', borderRadius: 6, padding: '14px 16px' }}>
                    <label style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', cursor: 'pointer' }}>
                      <div>
                        <div style={{ fontWeight: 600, fontSize: 13, color: '#0f172a' }}>Có phân ca tập</div>
                        <div style={{ fontSize: 12, color: '#64748b', marginTop: 2 }}>Bắt buộc chọn ca tập cho hội viên</div>
                      </div>
                      <input
                        type="checkbox"
                        checked={configs.CoPhanCaTap}
                        onChange={(e) => handleChange('CoPhanCaTap', e.target.checked)}
                        style={{ width: 18, height: 18, cursor: 'pointer', accentColor: '#0284c7' }}
                      />
                    </label>
                  </div>

                  <div style={{ background: '#fff', border: '1px solid #e2e8f0', borderRadius: 6, padding: '14px 16px' }}>
                    <label style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', cursor: 'pointer' }}>
                      <div>
                        <div style={{ fontWeight: 600, fontSize: 13, color: '#0f172a' }}>Có sử dụng thẻ theo lần</div>
                        <div style={{ fontSize: 12, color: '#64748b', marginTop: 2 }}>Quản lý số buổi tập còn lại</div>
                      </div>
                      <input
                        type="checkbox"
                        checked={configs.CoSuDungTheTheoLan}
                        onChange={(e) => handleChange('CoSuDungTheTheoLan', e.target.checked)}
                        style={{ width: 18, height: 18, cursor: 'pointer', accentColor: '#0284c7' }}
                      />
                    </label>
                  </div>
                </div>

                <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 12 }}>
                  <div style={{ background: '#fff', border: '1px solid #e2e8f0', borderRadius: 6, padding: '14px 16px' }}>
                    <div style={{ fontWeight: 600, fontSize: 13, color: '#0f172a' }}>Cảnh báo sắp hết hạn trước (ngày)</div>
                    <div style={{ fontSize: 12, color: '#64748b', marginTop: 2, marginBottom: 8 }}>
                      Hiển thị cảnh báo màu cam trên bảng khi thẻ hội viên sắp hết hạn
                    </div>
                    <input
                      type="number"
                      min="0"
                      max="90"
                      value={configs.SoNgayCanhBaoSapHetHan}
                      onChange={(e) => handleChange('SoNgayCanhBaoSapHetHan', parseInt(e.target.value) || 0)}
                      style={{ width: '100%', height: 32, padding: '0 10px', border: '1px solid #cbd5e1', borderRadius: 4, fontSize: 13 }}
                    />
                  </div>

                  <div style={{ background: '#fff', border: '1px solid #e2e8f0', borderRadius: 6, padding: '14px 16px' }}>
                    <div style={{ fontWeight: 600, fontSize: 13, color: '#0f172a' }}>Cảnh báo sắp hết lần tập (lần)</div>
                    <div style={{ fontSize: 12, color: '#64748b', marginTop: 2, marginBottom: 8 }}>
                      Cảnh báo khi số buổi tập còn lại ít hơn hoặc bằng mức này
                    </div>
                    <input
                      type="number"
                      min="0"
                      max="50"
                      value={configs.SoLanCanhBaoSapHet}
                      onChange={(e) => handleChange('SoLanCanhBaoSapHet', parseInt(e.target.value) || 0)}
                      style={{ width: '100%', height: 32, padding: '0 10px', border: '1px solid #cbd5e1', borderRadius: 4, fontSize: 13 }}
                    />
                  </div>
                </div>
              </div>
            )}

            {/* TAB 3: THIẾT BỊ */}
            {activeTab === 'thietBi' && (
              <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
                <div style={{ background: '#fff', border: '1px solid #e2e8f0', borderRadius: 6, padding: '14px 16px' }}>
                  <label style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', cursor: 'pointer' }}>
                    <div>
                      <div style={{ fontWeight: 600, fontSize: 13, color: '#0f172a' }}>
                        Tự động tạo, xóa thẻ trên thiết bị khi gia hạn, xóa khách
                      </div>
                      <div style={{ fontSize: 12, color: '#64748b', marginTop: 2 }}>
                        Khi gia hạn gói tập hoặc xóa hội viên, tự động gửi lệnh cập nhật trực tiếp tới cổng xoay/máy vân tay.
                      </div>
                    </div>
                    <input
                      type="checkbox"
                      checked={configs.TuDongTaoXoaThe}
                      onChange={(e) => handleChange('TuDongTaoXoaThe', e.target.checked)}
                      style={{ width: 18, height: 18, cursor: 'pointer', accentColor: '#0284c7' }}
                    />
                  </label>
                </div>
              </div>
            )}
          </div>

          {/* MODAL FOOTER */}
          <div
            className="cust-modal-footer"
            style={{
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'space-between',
              padding: '10px 18px',
              background: '#f1f5f9',
              borderTop: '1px solid #e2e8f0'
            }}
          >
            {/* SEARCH BOX AT BOTTOM LEFT MATCHING ORIGINAL APP */}
            {activeTab === 'soPhieu' ? (
              <div style={{ display: 'flex', alignItems: 'center', gap: 6 }}>
                <i className="fa-solid fa-magnifying-glass" style={{ color: '#64748b', fontSize: 12 }}></i>
                <input
                  type="text"
                  placeholder="Tìm kiếm phiếu (Ctrl + F)..."
                  value={slipFilter}
                  onChange={(e) => setSlipFilter(e.target.value)}
                  style={{
                    height: 28,
                    width: 210,
                    padding: '0 8px',
                    fontSize: 12,
                    border: '1px solid #cbd5e1',
                    borderRadius: 3,
                    backgroundColor: '#fff'
                  }}
                />
              </div>
            ) : <div />}

            <div style={{ display: 'flex', gap: 8 }}>
              {canManageConfig && (
                <button
                  type="button"
                  className="cust-btn-save"
                  onClick={handleSave}
                  disabled={saving}
                  style={{
                    background: '#1e3a8a',
                    color: '#fff',
                    border: 'none',
                    padding: '6px 20px',
                    borderRadius: 3,
                    fontWeight: 600,
                    fontSize: 13,
                    cursor: 'pointer'
                  }}
                >
                  {saving ? (
                    <><i className="fa-solid fa-spinner fa-spin" style={{ marginRight: 6 }}></i> Đang ghi...</>
                  ) : (
                    <><i className="fa-solid fa-floppy-disk" style={{ marginRight: 6 }}></i> Ghi dữ liệu</>
                  )}
                </button>
              )}
              <button
                type="button"
                className="cust-btn-cancel"
                onClick={onClose}
                disabled={saving}
                style={{
                  background: '#ffffff',
                  border: '1px solid #cbd5e1',
                  color: '#334155',
                  padding: '6px 16px',
                  borderRadius: 3,
                  fontWeight: 500,
                  fontSize: 13,
                  cursor: 'pointer'
                }}
              >
                Thoát
              </button>
            </div>
          </div>
        </div>
      </div>

      {/* POPUP THIẾT LẬP CÁCH SINH SỐ PHIẾU */}
      <SlipPatternConfigModal
        show={!!editingSlip}
        item={editingSlip}
        onSave={handleSaveSlipModal}
        onClose={() => setEditingSlip(null)}
      />
    </>
  );
}
