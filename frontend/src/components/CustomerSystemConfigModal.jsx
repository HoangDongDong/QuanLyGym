import React, { useState, useEffect } from 'react';

export default function CustomerSystemConfigModal({
  show,
  onClose,
  initialConfigs = {},
  onSave,
  canManageConfig = true
}) {
  const [activeTab, setActiveTab] = useState('khachHang'); // 'khachHang' | 'theGym' | 'thietBi'
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
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    if (show && initialConfigs) {
      setConfigs(prev => ({
        ...prev,
        ...initialConfigs,
        // Đảm bảo kiểu dữ liệu chuẩn
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

  if (!show) return null;

  const handleChange = (key, value) => {
    setConfigs(prev => ({ ...prev, [key]: value }));
  };

  const handleSave = async () => {
    try {
      setSaving(true);
      await onSave(configs);
      onClose();
    } catch (err) {
      console.error('Lỗi lưu cấu hình:', err);
    } finally {
      setSaving(false);
    }
  };

  return (
    <div className="cust-modal-overlay">
      <div className="cust-modal-box" style={{ width: 680, maxWidth: '95vw' }}>
        {/* MODAL HEADER */}
        <div className="cust-modal-header" style={{ background: 'linear-gradient(180deg, #1e3a8a 0%, #1e40af 100%)', color: '#fff' }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: 8, fontSize: 14, fontWeight: 700 }}>
            <i className="fa-solid fa-sliders"></i>
            <span>Cấu hình hệ thống - Khách hàng & Thẻ Gym</span>
          </div>
          <button className="cust-modal-close" onClick={onClose} title="Đóng">
            <i className="fa-solid fa-xmark"></i>
          </button>
        </div>

        {/* TAB STRIP */}
        <div style={{ display: 'flex', background: '#e2e8f0', borderBottom: '1px solid #cbd5e1', padding: '4px 8px 0', gap: 4 }}>
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
        <div style={{ padding: '20px 24px', maxHeight: '65vh', overflowY: 'auto', background: '#f8fafc' }}>
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
        <div className="cust-modal-footer" style={{ display: 'flex', justifyContent: 'flex-end', gap: 8, padding: '12px 18px', background: '#f1f5f9', borderTop: '1px solid #e2e8f0' }}>
          <button
            type="button"
            className="cust-btn-cancel"
            onClick={onClose}
            disabled={saving}
          >
            Đóng
          </button>
          {canManageConfig && (
            <button
              type="button"
              className="cust-btn-save"
              onClick={handleSave}
              disabled={saving}
              style={{ background: '#1e3a8a', color: '#fff', border: 'none', padding: '6px 18px', borderRadius: 3, fontWeight: 600 }}
            >
              {saving ? <><i className="fa-solid fa-spinner fa-spin"></i> Đang lưu...</> : <><i className="fa-solid fa-floppy-disk"></i> Lưu cấu hình (SCONFIG)</>}
            </button>
          )}
        </div>
      </div>
    </div>
  );
}
