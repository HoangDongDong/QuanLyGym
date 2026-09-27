using System;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using No1Lib.Sys;
using No1Lib.Db;
using No1Lib.Utils;
using FirebirdSql.Data.FirebirdClient;
using System.ComponentModel;
using System.Collections.Generic;

namespace No1Run
{
    public partial class XacNhanThanhToan
    {        
        private decimal TONGTIEN;
        private string DKHACHHANGID;

        public void SetData(decimal TONGTIEN, decimal datTruoc, decimal NoCu, string DKHACHHANGID)
        {            
            this.DKHACHHANGID = DKHACHHANGID;
            numNoCu.Value = NoCu;
            numDatTruoc.DecimalPlaces = 0;
            numDatTruoc.Value = datTruoc;
            this.TONGTIEN = TONGTIEN + NoCu - datTruoc;
            numTienHoaDon.Value = TONGTIEN;            

            lueDTAIKHOAN.LoadData(Tables.DTAIKHOANNGANHANG);

            numTongTien.Value = this.TONGTIEN;
            numKhachDua_OnEditValueChanged(null, null);

            numNoCu.DecimalPlaces = 0;
            numTienHoaDon.DecimalPlaces = 0;
            numTongTien.DecimalPlaces = 0;
            numKhachDua.DecimalPlaces = 0;
            numTraLai.DecimalPlaces = 0;

            List<ArrangeItem> lst = new List<ArrangeItem>();
            lst.Add(new ArrangeItem(numDatTruoc.Value != 0, lblDatTruoc, numDatTruoc));
            lst.Add(new ArrangeItem(numNoCu.Value != 0, lblNoCu, numNoCu));
            lst.Add(new ArrangeItem(numDatTruoc.Value != 0 || numNoCu.Value != 0, lblTienHoaDon, numTienHoaDon));
            lst.Add(new ArrangeItem(true, lblTongTien, numTongTien));
            lst.Add(new ArrangeItem(true, lblKhachDua, numKhachDua));
            lst.Add(new ArrangeItem(SystemConfig.CoThanhToanThe == 30, lblTheATM, numATM));
            lst.Add(new ArrangeItem(SystemConfig.CoThanhToanChuyenKhoan == 30, lblChuyenKhoan, numChuyenKhoan));
            lst.Add(new ArrangeItem(SystemConfig.CoThanhToanChuyenKhoan == 30, lblTaiKhoan, lueDTAIKHOAN));
            lst.Add(new ArrangeItem(SystemConfig.CoThanhToanVoucher == 30, lblCoupon, numCoupon));
            lst.Add(new ArrangeItem(SystemConfig.SuDungTheTraTruoc == 30, lblTheTraTruoc, numTheTraTruoc));
            lst.Add(new ArrangeItem(SystemConfig.SuDungDiemTichLuyDeThanhToan == 30, lblTruTichLuy, numTichLuy));
            lst.Add(new ArrangeItem(true, lblTraLai, numTraLai));

            int oldVal = numTraLai.Top - numDatTruoc.Top;
            int h = UiUtils.ArrangeControl(lst, 5, numDatTruoc.Top);

            int newVal = numTraLai.Top - numDatTruoc.Top;
            this.form.Height -= oldVal - newVal;

            btnInThuBill.Visible = SystemConfig.ChoPhepInTamTinh == 30;

            numKhachDua.Value = TONGTIEN;

            if (SystemConfig.LuaChonVoucherTuDanhSach == 30)
            {
                numCoupon.ReadOnly = true;
                lblCoupon.Font = new Font(lblCoupon.Font, FontStyle.Bold | FontStyle.Underline);
                lblCoupon.Cursor = Cursors.Hand;
                lblCoupon.Click += new EventHandler(lblCoupon_Click);
            }

            numKhachDua.Select();
            numKhachDua.Select(0, numKhachDua.Text.Length);
            if (numTongTien.Value < 0) numKhachDua.Enabled = false;
            btnOK.Enabled = true;
            btnDongBillKhongIn.Enabled = true;
        }

        public string DVOUCHERID = "";
        void lblCoupon_Click(object sender, EventArgs e)
        {
            //hiển thị để nhập mã coupon
            NhapCoupon form = (NhapCoupon)Config.CreateForm(Forms.NhapCoupon);
            if (form.No1Form1.ShowDialog() == DialogResult.OK)
            {
                DVOUCHERID = form.VoucherID;
                numCoupon.Value = form.GiaTriVoucher;
                UpdateTraLai();
            }
        }

		public void btnOK_Click(Object sender, EventArgs e)
		{
            CheckAndClose(DialogResult.OK);
		}

        public decimal KhachDua
        {
            get { return numKhachDua.Value; }
        }

        public decimal Voucher
        {
            get { return numCoupon.Value; }
        }

        public decimal DatTruoc
        {
            get { return numDatTruoc.Value; }
        }

        public decimal TienThanhToan
        {
            get 
            {
                if (numTongTien.Value < 0)
                {
                    return numTongTien.Value;
                }
                else
                {
                    return numDatTruoc.Value + Math.Max(0, Math.Min(TienTra, TONGTIEN));
                }
            }
        }

        public int LoaiThanhToan
        {
            get { return 0; }
        }

        public decimal TraLai
        {
            get { return numTraLai.Value; }
        }

        public decimal TheTraTruoc
        {
            get { return numTheTraTruoc.Value; }
        }

        public decimal TruTichLuy
        {
            get { return numTichLuy.Value; }
        }

        public decimal TienMat
        {
            get { return Math.Max(0, numKhachDua.Value - numTraLai.Value); }
        }

        public decimal ChuyenKhoan
        {
            get { return numChuyenKhoan.Value; }
        }

        public decimal TienThe
        {
            get { return numATM.Value; }
        }

        public decimal TienTra
        {
            get
            {
                return KhachDua + Voucher + TheTraTruoc + TruTichLuy + TienThe + ChuyenKhoan;
            }
        }

        private void CheckAndClose(DialogResult result)
        {            
            if (numTongTien.Value > TienTra)
            {
                if (SystemConfig.ChoPhepKhachNo != 30)
                {
                    Msg.ShowWarning("Chức năng này hiện tại chưa được kích hoạt. Bạn có thể kích hoạt trong menu 'Quản trị'|'Cấu hình toàn hệ thống'");
                    return;
                }
                
                if (DKHACHHANGID.Length == 0)
                {
                    Msg.ShowWarning("Bạn chưa chọn khách hàng, mời bạn chọn khách hàng trước!");
                    return;
                }

                if (Msg.ShowYesNo("Bạn có muốn cho khách hàng '" + new DKHACHHANGRow(DKHACHHANGID).NAME + "' nợ số tiền '" + (numTongTien.Value - TienTra).ToString("n0") + "' không?") != DialogResult.Yes)
                {
                    return;
                }
            }

            if (numTheTraTruoc.Value > 0 && numTheTraTruoc.Value > SoTienConLai)
            {
                Msg.ShowWarning("Thẻ trả trước chỉ còn " + SoTienConLai.ToString("n0") + ", không thể trả quá số tiền còn lại");
                return;
            }

            if (numChuyenKhoan.Value != 0 && lueDTAIKHOAN.StringValue.Length == 0)
            {
                Msg.ShowWarning("Mời bạn chọn tài khoản");
                lueDTAIKHOAN.showDropDown();
                return;
            }

            form.DialogResult = result;
        }

		public void btnDongBillKhongIn_Click(Object sender, EventArgs e)
		{
            CheckAndClose(DialogResult.Retry);
		}


		public void chkKhachNo_CheckedChanged(Object sender, EventArgs e)
		{
            if (chkKhachNo.Checked)
            {
                numKhachDua.Value = 0;
                numKhachDua.Enabled = false;
            }
            else
            {
                numKhachDua.Enabled = true;
            }
		}


		public void numKhachDua_OnEditValueChanged(Object sender, Object value)
		{
            UpdateTraLai();
		}

        private void UpdateTraLai()
        {
            //truong hop con no cu (dat tien truoc)
            if (numNoCu.Value < 0)
            {
                numTraLai.Value = 0;
            }
            else
            {
                if (numCoupon.Value > TONGTIEN)
                {
                    numTraLai.Value = numKhachDua.Value;
                }
                else
                {
                    decimal val = numKhachDua.Value + numCoupon.Value + numATM.Value + numChuyenKhoan.Value + numTheTraTruoc.Value + numTichLuy .Value - TONGTIEN;
                    numTraLai.Value = Math.Max(0, val);
                }
            }
        }


		public void form_KeyDown(object sender, KeyEventArgs e)
		{
            if (e.KeyCode== Keys.F8) btnInThuBill.PerformClick();
            else if (e.KeyCode == Keys.F9) btnDongBillKhongIn.PerformClick();
		}
       

        public string DTHETRATRUOCID = "";
        private decimal SoTienConLai = 0;
		public void lblTheTraTruoc_Click(object sender, EventArgs e)
		{
            NhapTheTraTruoc form = (NhapTheTraTruoc)Config.CreateForm(Forms.NhapTheTraTruoc);
            if (form.No1Form1.ShowDialog() == DialogResult.OK)
            {
                DTHETRATRUOCID = form.TheTraTruocID;
                numTheTraTruoc.ReadOnly = false;

                SoTienConLai = form.SoTienConLai;
                //chuyển đổi để sử dụng
                decimal SuDung = Math.Min(SoTienConLai, TONGTIEN);
                numTheTraTruoc.Value = SuDung;
                UpdateKhachDua();
            }
		}

        private void UpdateKhachDua()
        {
            numKhachDua.Value = Math.Max(0, TONGTIEN - numTichLuy.Value - numTheTraTruoc.Value - numATM.Value - numChuyenKhoan.Value - numCoupon.Value);
            UpdateTraLai();
        }

		public void lblTruTichLuy_Click(object sender, EventArgs e)
		{
            if (DKHACHHANGID.Length == 0)
            {
                Msg.ShowWarning("Vui lòng chọn khách hàng trước");
            }
            else
            {
                TruTichLuy form = (TruTichLuy)Config.CreateForm(Forms.TruTichLuy);
                form.LoadData(DKHACHHANGID);
                if (form.No1Form1.ShowDialog() == DialogResult.OK)
                {
                    numTichLuy.Value = Math.Min(form.numGiaTri.Value, TONGTIEN);
                    UpdateKhachDua();
                }
            }
		}

		public void numATM_OnEditValueChanged(object sender, object value)
		{
            UpdateKhachDua();
		}
    }
}
