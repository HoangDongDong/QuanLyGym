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
using DbMapping;
using System.IO;

namespace No1Run
{
    public partial class QuanLyBanHang:IRefreshable
    {
        bool suDung2DVT = false;
        private int lamTronTien;
        bool coLuuVet = false;
        private INo1Control paymentControl;
        public void UserControl1_Load(Object sender, EventArgs e)
        {
            dtNgayLoc.LockEvent = true;
            DateTime date = Config.Db.DbDate;
            dtNgayLoc.FromDate = date;
            dtNgayLoc.ToDate = date;
            dtNgayLoc.LockEvent = false;
            coLuuVet = SystemConfig.KichHoatLuuVetHoatDong == 30;            

            lamTronTien = SystemConfig.LamTronTien;
            suDung2DVT = SystemConfig.SuDung2DonViTinh == 30;
            lueCuaHang.LoadData(Tables.DCUAHANG);
            grMain.GridView.SelectionChanged += new EventHandler(GridView_SelectionChanged);
            grTraLai.GridView.SelectionChanged += new EventHandler(grTraLai_grMain_SelectionChanged);
            grMua.GridView.SelectionChanged += new EventHandler(grMua_grMain_SelectionChanged);
            grMua.GridView.CellValueChanged += new DataGridViewCellEventHandler(GridView_CellValueChanged);

            SetupControls();

            mapper.LoadConfigSchema();
            mapper.EndInit();

            numTHETRATRUOC.Enabled = SystemConfig.SuDungTheTraTruoc == 30;
            numCHUYENKHOAN.Enabled = SystemConfig.CoThanhToanChuyenKhoan == 30;
            numTHE.Enabled = SystemConfig.CoThanhToanThe == 30;
            lueDTHETRATRUOCID.Enabled = SystemConfig.SuDungTheTraTruoc == 30;
            numVOUCHER.Enabled = SystemConfig.CoThanhToanVoucher == 30;
            lueDVOUCHERID.Enabled = SystemConfig.CoThanhToanVoucher == 30 && SystemConfig.LuaChonVoucherTuDanhSach == 30;
            lueDTAIKHOANNGANHANGID.Enabled = SystemConfig.CoThanhToanChuyenKhoan == 30;
            numTRUTICHLUY.Enabled = SystemConfig.SuDungDiemTichLuyDeThanhToan == 30;

            LoadData();
        }

        void GridView_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (grMua.GridView.Columns[e.ColumnIndex].DataPropertyName == "SLXUATCHUAQUYDOI")
            {
                TDONHANGCHITIETRow ctRow = new TDONHANGCHITIETRow(grMua.GridView.SelectedRow);
                Track("QLBH: Sửa số lượng '" + ctRow["DMATHANG_NAME"] + "' thành " + ctRow.SLXUATCHUAQUYDOI);
            }
        }

        void grMua_grMain_SelectionChanged(object sender, EventArgs e)
        {
            tsbXoa.Enabled = grMua.GridView.SelectedRows.Count > 0;
        }

        private void SetupControls()
        {
            List<ArrangeItem> lst = new List<ArrangeItem>();
            lst.Add(new ArrangeItem(SystemConfig.CoThueSuat == 30, lblTILETHUE, numTILETHUE, numTIENTHUE));
            lst.Add(new ArrangeItem(SystemConfig.CoPhiVanChuyen == 30, lblPHIVANCHUYEN, numPHIVANCHUYEN));
            lst.Add(new ArrangeItem(true, lblDOITRA, numDOITRA));
            lst.Add(new ArrangeItem(true, lblTONGCONG, numTONGCONG));
            lst.Add(new ArrangeItem(true, lblTIENTHANHTOAN, numTIENTHANHTOAN));

            int top = numTILETHUE.Top;
            int h = UiUtils.ArrangeControl(lst, 5, top) + 5;
            Panel2.Height = h;
        }

        void GridView_SelectionChanged(object sender, EventArgs e)
        {
            Timer1.Enabled = false;
            Timer1.Enabled = true;
        }

        private void LoadData()
        {
            grMain.LoadData();
        }
        
        private void PrintInvoice(bool preview)
        {
            bool showPreview = SystemConfig.HienThiTruocKhiIn == 30;
            bool chonMau = SystemConfig.LuaChonMauKhiIn == 30;
            Config.PrintInvoice(Config.GetTableDesc(Tables.TDONHANG), Forms.HoaDonBanHang, mapper.ID, chonMau, showPreview, new CustomReportHandler(CustomReportParam));
        }

        private void CustomReportParam(DataSet ds, Dictionary<string, object> dic)
        {
            decimal NoCu = ConvertTo.Decimal(dic["NOCU"]);
            decimal TongCong = ConvertTo.Decimal(dic["TONGCONG"]);
            decimal ThanhToan = ConvertTo.Decimal(dic["TIENTHANHTOAN"]);
            dic.Add("Nợ mới", TongCong + NoCu - ThanhToan);
            dic.Add("Đặt trước", GetDatTruoc());
            dic.Add("Điểm tích lũy", HoaDonBanHang.GetDiemTichLuyTrenHoaDon(lueDKHACHHANGID.StringValue));
        }

        private decimal GetDatTruoc()
        {
            string TDATHANGID = mapper[TDONHANGInfo.TDATHANGID].ToStringValue();
            decimal datTruoc = 0;
            if (TDATHANGID.Length > 0)
            {
                string sql = "SELECT THU FROM TTHUCHI WHERE TDATHANGID = '" + TDATHANGID + "'";
                datTruoc = Config.Db.GetFirstFieldDec(sql);
            }
            return datTruoc;
        }

		public void btnInLaiBill_Click(Object sender, EventArgs e)
		{
            if (btnLuu.Enabled)
            {
                if (!DoSave()) return;
            }
            PrintInvoice(false);
            Track("In lại bill số " + txtNAME.Text + ", ngày: " + dtNGAY.DateTime.ToString("dd/MM/yyyy"));
		}

        private void Track(string content)
        {
            if (!coLuuVet) return;

            TLUUVETRow row = new TLUUVETRow();
            row.GIO = Config.Db.DbDateTime;
            row.SODONHANG = mapper.ID;
            row.TAIKHOAN = DbConfig.UserName;
            row.NGAY = Config.Db.DbDate;
            row.NOTE = content;
            row.Update();
        }		

        private bool reloading = false;
        public void ReLoad(string ID)
        {
            reloading = true;

            mapper.Fill(ID);
            if (ID.Length == 0)
                mapper.EmptyControl();
            
            if (ID.Length > 0)
            {
                int val = mapper[TDONHANGInfo.LOAIGIA].ToInt();
                btnLoaiGia.Text = GetDienGiai(val);

                //kiểm tra thanh toán bằng một loại tiền hay nhiều?
                //trường hợp thanh toán nhiều loại thì ko cho nhâp trực tiếp vào ô tiền thanh toán
                int dem = KiemTraThanhToanMotLoaiTien();
                numTIENTHANHTOAN.Enabled = dem <= 1;
            }

            dtNGAY.Enabled = SystemConfig.ChoPhepThayDoiNgayTrenHoaDon == 30;
            btnLuu.Enabled = false;
            reloading = false;
        }

        private int KiemTraThanhToanMotLoaiTien()
        {
            No1NumericUpDown[] array = new No1NumericUpDown[] { numTHETRATRUOC, numTIENMAT, numTHE, numVOUCHER, numTRUTICHLUY, numCHUYENKHOAN };
            int count = 0;
            foreach (No1NumericUpDown num in array)
            {
                if (num.Value > 0)
                {
                    count++;
                    paymentControl = num;
                }
            }
            return count;
        }

        private string GetDienGiai(int loaiGia)
        {
            if (loaiGia == 1)
                return SystemConfig.DienGiaiGia2;
            else if (loaiGia == 2)
                return SystemConfig.DienGiaiGia3;
            else if (loaiGia == 3)
                return SystemConfig.DienGiaiGia4;
            return "Giá bán";
        }


		public void mapper_OnCalculation(Object sender, EventArgs e)
		{
            decimal thanhTien = grMua.CalcSum("THANHTIEN");
            decimal doiTra = grTraLai.CalcSum("THANHTIEN");
            numTIENHANG.Value = thanhTien;
            bool giamTheoTien = mapper["GIAMTHEOTIEN"].ToInt() == 30;
            if (giamTheoTien)
            {
                numTILEGIAMGIA.Value = numTIENHANG.Value == 0 ? 0 : 100 * numTIENGIAMGIA.Value / numTIENHANG.Value;
            }
            else
            {
                numTIENGIAMGIA.Value = 100 * Math.Round(thanhTien * numTILEGIAMGIA.Value / (100 * 100));
            }

            numDOITRA.Value = -doiTra;
            numTIENTHUE.Value = numTILETHUE.Value * (numTIENHANG.Value - numTIENGIAMGIA.Value) / 100;
            
            decimal tongCong = numTIENHANG.Value - numTIENGIAMGIA.Value + numTIENTHUE.Value + numPHIVANCHUYEN.Value - numDOITRA.Value;
            if (lamTronTien > 0) tongCong = lamTronTien * Math.Round(tongCong / lamTronTien);
            numTONGCONG.Value = tongCong;
		}

		public void btnLoaiGia_Click(Object sender, EventArgs e)
		{
            ChonLoaiGia form = (ChonLoaiGia)Config.CreateForm(Forms.ChonLoaiGia);
            if (form.form.ShowDialog() == DialogResult.OK)
            {
                int val = mapper[TDONHANGInfo.LOAIGIA].ToInt();
                if (form.LoaiGia != val)
                {
                    if (grMua.RowCount > 0)
                    {
                        if (Msg.ShowYesNo("Bạn có muốn chuyển mặt hàng từ '" + GetDienGiai(val) + "' thành '" + GetDienGiai(form.LoaiGia) + "' không?") == DialogResult.Yes)
                        {
                            mapper[TDONHANGInfo.LOAIGIA].Value = form.LoaiGia;
                            foreach (DataGridViewRow r in grMua.GridView.Rows)
                            {
                                if (!form.chkApDungTatCa.Checked && !r.Selected) continue;

                                TDONHANGCHITIETRow row = new TDONHANGCHITIETRow((r.DataBoundItem as DataRowView).Row);
                                DMATHANGRow mhRow = new DMATHANGRow(row.DMATHANGID);
                                switch (form.LoaiGia)
                                {
                                    case 1:
                                        row.DONGIA = mhRow.GIABAN2;
                                        break;
                                    case 2:
                                        row.DONGIA = mhRow.GIABAN3;
                                        break;
                                    case 3:
                                        row.DONGIA = mhRow.GIABAN4;
                                        break;
                                    default:
                                        row.DONGIA = mhRow.GIABAN;
                                        break;
                                }
                                grMua.CalculateRow(row.Row);
                            }                            
                            btnLoaiGia.Text = GetDienGiai(form.LoaiGia);

                            Track("Đổi loại giá bán từ '" + GetDienGiai(mapper[TDONHANGInfo.LOAIGIA].ToInt()) + "' sang '" + GetDienGiai(form.LoaiGia) + "'");
                        }
                    }
                    else
                    {
                        mapper[TDONHANGInfo.LOAIGIA].Value = form.LoaiGia;
                        btnLoaiGia.Text = GetDienGiai(form.LoaiGia);
                        Track("Đổi loại giá bán từ '" + GetDienGiai(mapper[TDONHANGInfo.LOAIGIA].ToInt()) + "' sang '" + GetDienGiai(form.LoaiGia) + "'");                        
                    }

                    mapper.RaiseOnCalculation();
                }
            }
		}

		public void tsbChonMatHang_Click(Object sender, EventArgs e)
		{
            ChonMatHangTra form = (ChonMatHangTra)Config.CreateForm(Forms.ChonMatHangTra);
            form.Load(grTraLai.DataSource);
            if (form.form.ShowDialog() == DialogResult.OK)
            {
                DataTable dt = grTraLai.DataSource as DataTable;
                if (form.DoiTheoDon)
                {
                    foreach (DataRow r in form.Rows)
                    {
                        string CHITIETID = r["ID"].ToString();
                        DataRow[] rows = dt.Select("TDONHANGTRAID='" + CHITIETID + "'");
                        if (rows.Length == 0)
                        {
                            DataRow newRow = dt.NewRow();
                            TDONHANGCHITIETRow refRow = new TDONHANGCHITIETRow(CHITIETID);
                            TDONHANGRow refDhRow = new TDONHANGRow(refRow.TDONHANGID);

                            DMATHANGRow spRow = new DMATHANGRow(refRow.DMATHANGID);

                            newRow["DMATHANGID"] = spRow.ID;
                            newRow["DMATHANG_NAME"] = spRow.NAME;
                            newRow["DMATHANG_CODE"] = spRow.CODE;
                            newRow["DMATHANG_MASANCO"] = spRow.MASANCO;

                            newRow["DDONVITINHID"] = spRow.DDONVITINHID;
                            newRow["DDONVITINH_NAME"] = spRow.DDONVITINHID.Length == 0 ? "" : new DDONVITINHRow(spRow.DDONVITINHID).NAME;

                            newRow["DONGIA"] = refRow.DONGIA * (1 + refDhRow.TILETHUE / 100);
                            newRow["TILEGIAMGIA"] = 100 - (100 - refRow.TILEGIAMGIA) / 100 * (100 - refDhRow.TILEGIAMGIA);

                            newRow["DDONVITINHID"] = refRow.DDONVITINHID;
                            newRow["DDONVITINH_NAME"] = refRow.DDONVITINHID.Length == 0 ? "" : new DDONVITINHRow(refRow.DDONVITINHID).NAME;
                            decimal quyDoi = 1;
                            if (refRow.DDONVITINHID != spRow.DDONVITINHID)
                            {
                                quyDoi = spRow.QUYDOI;
                            }
                            newRow["SLNHAPCHUAQUYDOI"] = ConvertTo.Decimal(r["SLTRA"]);
                            newRow["SLNHAP"] = quyDoi * ConvertTo.Decimal(r["SLTRA"]);
                            newRow["TDONHANGTRAID"] = refRow.ID;
                            if (!refRow.IsNullValue(TDONHANGCHITIETInfo.HANSUDUNG))
                                newRow["HANSUDUNG"] = refRow.HANSUDUNG;
                            newRow["KICHTHUOC"] = refRow.KICHTHUOC;
                            dt.Rows.Add(newRow);
                            grTraLai.CalculateRow(newRow);
                        }
                        else
                        {
                            rows[0]["SLNHAPCHUAQUYDOI"] = ConvertTo.Decimal(r["SLTRA"]);
                            grTraLai.CalculateRow(rows[0]);
                        }
                    }
                }
                else
                {
                    DataRow newRow = dt.NewRow();
                    string dMatHangID = form.DMATHANGID;
                    DMATHANGRow spRow = new DMATHANGRow(dMatHangID);

                    newRow["DMATHANGID"] = spRow.ID;
                    newRow["DMATHANG_NAME"] = spRow.NAME;
                    newRow["DMATHANG_CODE"] = spRow.CODE;
                    newRow["DMATHANG_MASANCO"] = spRow.MASANCO;
                    newRow["DONGIA"] = spRow.GIABAN;
                    newRow["SLNHAPCHUAQUYDOI"] = 1;

                    newRow["DDONVITINHID"] = spRow.DDONVITINHID;
                    newRow["DDONVITINH_NAME"] = spRow.DDONVITINHID.Length == 0 ? "" : new DDONVITINHRow(spRow.DDONVITINHID).NAME;
                    newRow["SLNHAP"] = 1;

                    dt.Rows.Add(newRow);
                    grTraLai.CalculateRow(newRow);
                }

                mapper.RaiseOnCalculation();
            }
		}

		public void grTraLai_grMain_SelectionChanged(Object sender, EventArgs e)
		{
            tsbXoaTraLai.Enabled = grTraLai.GridView.SelectedRows.Count > 0;
		}
        
		public void UserControl1_KeyDownEx(Object sender, KeyEventArgs e)
		{
            
		}

		public void grTraLai_OnRowCalculate(Object sender, DataRow r, String fieldName)
		{
            decimal quyDoi = 1;
            if (suDung2DVT)
            {
                string DMATHANGID = r["DMATHANGID"].ToString();
                DMATHANGRow mhRow = new DMATHANGRow(DMATHANGID);
                if (TonKhoHandler.Has2DonViTinh(mhRow) && r["DDONVITINHID"].ToString() != mhRow.DDONVITINHID)
                {
                    quyDoi = mhRow.QUYDOI;
                }
            }
            r["SLNHAP"] = quyDoi * ConvertTo.Decimal(r["SLNHAPCHUAQUYDOI"]);

            decimal thanhTien = ConvertTo.Decimal(r["SLNHAPCHUAQUYDOI"]) * ConvertTo.Decimal(r["DONGIA"]) * (1 - ConvertTo.Decimal(r["TILEGIAMGIA"]) / 100);
            r["THANHTIEN"] = -thanhTien;
            r["THANHTIENDUONG"] = thanhTien;
		}		


		public void btnRefresh_Click(Object sender, EventArgs e)
		{
            LoadData();
		}


		public void dtNgayLoc_OnEditValueChanged(Object sender, Object value)
		{
            LoadData();
		}


		public void grMain_CustomLoadData(Object sender, CustomLoadDataArgs e)
		{
            e.Command.Parameters.Add("@FromDate", FbDbType.Date).Value = dtNgayLoc.TuNgay;
            e.Command.Parameters.Add("@ToDate", FbDbType.Date).Value = dtNgayLoc.DenNgay;
            if (lueCuaHang.StringValue.Length == 0)
            {
                if (!DbConfig.IsAdmin)
                {
                    e.Where += " AND EXISTS(SELECT * FROM TNGUOIDUNGTHEOCUAHANG WHERE TNGUOIDUNGTHEOCUAHANG.DCUAHANGID = TDONHANG.DCUAHANGID AND SUSERID = '" + DbConfig.UserID + "')";
                }
            }
            else
            {
                e.Where += " AND DCUAHANGID = '" + lueCuaHang.StringValue + "'";
            }
		}


		public void mapper_OnChanged(Object sender, EventArgs e)
		{
            btnLuu.Enabled = true;
		}

        private bool DoSave()
        {
            //kiểm tra xem có nợ không?
            if (lueDKHACHHANGID.StringValue.Length == 0 && numTONGCONG.Value > numTIENTHANHTOAN.Value)
            {
                Msg.ShowWarning("Khách lẻ không được phép nợ, tiền thanh toán phải lớn hơn tổng cộng");
                return false;
            }

            //kiểm tra nếu thanh toán ngân hàng phải chọn tài khoản
            if (numCHUYENKHOAN.Value != 0 && lueDTAIKHOANNGANHANGID.StringValue.Length == 0)
            {
                Msg.ShowWarning("Mời bạn chọn tài khoản ngân hàng");
                return false;
            }

            if (numVOUCHER.Value != 0 && SystemConfig.LuaChonVoucherTuDanhSach == 30)
            {
                Msg.ShowWarning("Mời bạn chọn voucher");
                return false;
            }

            if (numTHETRATRUOC.Value != 0)
            {
                if (lueDTHETRATRUOCID.StringValue.Length == 0)
                {
                    Msg.ShowWarning("Mời bạn chọn thẻ trả trước");
                    return false;
                }
                else
                {
                    //kiểm tra số tiền còn lại trong thẻ
                    decimal SoTienConLai = NhapTheTraTruoc.GetSoTienConLai(lueDTHETRATRUOCID.StringValue);
                    //cộng lại số tiền đã tiêu nếu có
                    SoTienConLai += new TDONHANGRow(mapper.ID).THETRATRUOC;
                    if (SoTienConLai < numTHETRATRUOC.Value)
                    {
                        Msg.ShowWarning("Bạn sử dụng quá số tiền còn lại trong thẻ");
                        return false;
                    }
                }
            }

            //tính lại điểm trừ
            decimal giaTri1Diem = SystemConfig.QuyDoi1DiemSangTien;
            if (giaTri1Diem != 0)
            {
                mapper[TDONHANGInfo.DIEMGIAM].Value = numTRUTICHLUY.Value / giaTri1Diem;
            }

            if (mapper.Update())
            {
                if (SystemConfig.SuDung2DonViTinh == 0)
                {
                    Config.Db.ExecSql("UPDATE TDONHANGCHITIET SET SLNHAP = COALESCE(SLNHAPCHUAQUYDOI, 0), SLXUAT = COALESCE(SLXUATCHUAQUYDOI, 0) WHERE TDONHANGID = '" + mapper.ID + "'");
                }
                                
                Config.Db.ExecSql("UPDATE TDONHANGCHITIET SET DKHOHANGID = '" + lueDKHOXUATID.StringValue + "' WHERE TDONHANGID = '" + mapper.ID + "'");

                //xuất lại vật tư
                HoaDonBanHang.XuatVatTu(mapper.ID, lueDKHOXUATID.StringValue);                    

                return true;
            }
            else return false;
        }
        

		public void btnLuu_Click(Object sender, EventArgs e)
		{
            if (DoSave())
            {
                btnLuu.Enabled = false;
            }
		}


		public void numTIENGIAMGIA_OnEditValueChanged(Object sender, Object value)
		{            
            if (numTIENGIAMGIA.Focused)
            {
                mapper["GIAMTHEOTIEN"].Value = 30;
            }
		}


		public void numTILEGIAMGIA_OnEditValueChanged(Object sender, Object value)
		{
            if (numTILEGIAMGIA.Focused)
            {
                mapper["GIAMTHEOTIEN"].Value = 0;
            }
		}


		public void btnHuyPhieu_Click(Object sender, EventArgs e)
		{
            if (Msg.ShowYesNo("BẠN CÓ MUỐN HỦY PHIẾU ĐANG CHỌN KHÔNG?") == DialogResult.Yes)
            {
                string ID = mapper.ID;

                //luu lai hoa don huy
                TDONHANGRow dhRow = new TDONHANGRow(mapper.ID);
                if (dhRow.IsNull)
                {
                    Msg.ShowWarning("Đơn hàng này đã hủy");
                    return;
                }
                TDONHANGHUYRow dhHuyRow = new TDONHANGHUYRow();
                dhHuyRow.DATHANHTOAN = dhRow.DATHANHTOAN;
                dhHuyRow.DOITRA = dhRow.DOITRA;
                dhHuyRow.GIOHUY = Config.Db.DbDateTime;
                dhHuyRow.GIOTHANHTOAN = dhRow.GIOTHANHTOAN;
                dhHuyRow.KHACHHANG = dhRow.DKHACHHANGID.Length == 0 ? "" : new DKHACHHANGRow(dhRow.DKHACHHANGID).NAME;
                dhHuyRow.NAME = dhRow.NAME;
                dhHuyRow.NOTE = dhRow.NOTE;
                dhHuyRow.NGAY = dhRow.NGAY;
                dhHuyRow.NHANVEN = dhRow.DNHANVIENXUATID.Length == 0 ? "" : new DNHANVIENRow(dhRow.DNHANVIENXUATID).NAME;
                dhHuyRow.PHIVANCHUYEN = dhRow.PHIVANCHUYEN;
                dhHuyRow.TIENTHUE = dhRow.TIENTHUE;
                dhHuyRow.TIENGIAMGIA = dhRow.TILEGIAMGIA;
                dhHuyRow.THANHTOANBOI = new SUSERRow(dhRow.USERTHANHTOANID).NAME;
                dhHuyRow.TILETHUE = dhRow.TILETHUE;
                dhHuyRow.TILEGIAMGIA = dhRow.TILEGIAMGIA;
                dhHuyRow.TIENHANG = dhRow.TIENHANG;                
                dhHuyRow.THUNGAN = DbConfig.UserName;
                dhHuyRow.TRALAI = dhRow.TRALAI;
                dhHuyRow.NGAYHUY = Config.Db.DbDate;
                dhHuyRow.Update();
                DataTable dt = Config.Db.GetTable("SELECT * FROM TDONHANGCHITIET WHERE TDONHANGID = '" + ID + "'");
                foreach (DataRow r in dt.Rows)
                {
                    TDONHANGCHITIETRow ctRow = new TDONHANGCHITIETRow(r);
                    TDONHANGHUYCHITIETRow ctHuyRow = new TDONHANGHUYCHITIETRow();
                    ctHuyRow.DONGIA = ctRow.DONGIA;
                    DMATHANGRow mhRow = new DMATHANGRow(ctRow.DMATHANGID);
                    ctHuyRow.DVT = mhRow.DDONVITINHID.Length == 0 ? "" : new DDONVITINHRow(mhRow.DDONVITINHID).NAME;
                    ctHuyRow.MAHANG = mhRow.CODE;
                    ctHuyRow.NOTE = mhRow.NOTE;
                    ctHuyRow.SOLUONG = ctRow.SLXUAT - ctRow.SLNHAP;
                    ctHuyRow.TDONHANGHUYID = dhHuyRow.ID;
                    ctHuyRow.TENHANG = mhRow.NAME;
                    ctHuyRow.THANHTIEN = ctRow.THANHTIEN;
                    ctHuyRow.Update();
                }
                Config.Db.ExecSql("DELETE FROM TDONHANG WHERE ID = '" + ID + "'");
                Config.Db.ExecSql("DELETE FROM TDONHANGCHITIET WHERE TDONHANGID = '" + ID + "'");
                LoadData();
            }
		}


		public void tsbXoa_Click(Object sender, EventArgs e)
		{
            foreach (DataGridViewRow r in grMua.GridView.SelectedRows)
            {
                DataRow row = (r.DataBoundItem as DataRowView).Row;
                TDONHANGCHITIETRow ctRow = new TDONHANGCHITIETRow(row);
                Track("QLBH: Xóa mặt hàng '" + ctRow["DMATHANG_NAME"].ToString() + "'");
            }
            grMua.DeleteSelectedRows();
		}


		public void tsbXoaTraLai_Click(Object sender, EventArgs e)
		{
            grTraLai.DeleteSelectedRows();
		}


		public void tsbThemHang_Click(Object sender, EventArgs e)
		{
            Search form = new Search(Tables.DMATHANG, Tables.DNHOMMATHANG, DMATHANGInfo.DNHOMMATHANGID.ToString());
            if (SystemConfig.MatHangCoVatTu == 30)
            {
                form.CustomLoadData += new CustomLoadDataHandler(form_CustomLoadData);
                form.LoadData(Tables.DMATHANG);
            }
            //TimKiem form = new TimKiem(Config.GetTableDesc(Tables.DMATHANG), "COALESCE(LOAIDINHLUONG, 0) <> 4");
            if (form.ShowDialog() == DialogResult.OK)
            {
                TDONHANGRow dhRow = new TDONHANGRow(mapper.ID);

                DataTable dt = grMua.DataSource as DataTable;
                TDONHANGCHITIETRow row = new TDONHANGCHITIETRow(dt.NewRow());
                row.DMATHANGID = form.SelectedID;
                DMATHANGRow spRow = new DMATHANGRow(form.SelectedID);                
                row.DKHOHANGID = lueDKHOXUATID.StringValue;
                row["DMATHANG_NAME"] = spRow.NAME;

                string DDONVITINHID = spRow.DDONVITINHID;
                if (suDung2DVT && TonKhoHandler.Has2DonViTinh(spRow))
                {
                    ChonDonViTinh frmDvt = (ChonDonViTinh)Config.CreateForm(Forms.ChonDonViTinh);
                    frmDvt.SetData(spRow);
                    if (frmDvt.form.ShowDialog() == DialogResult.OK)
                    {
                        DDONVITINHID = frmDvt.SelectedID;
                    }
                }

                row.SLXUATCHUAQUYDOI = 1;
                row.SLXUAT = DDONVITINHID == spRow.DDONVITINHID ? 1 : spRow.QUYDOI;
                row.DONGIA = DDONVITINHID == spRow.DDONVITINHID ? spRow.GIABAN : spRow.GIABANCHAN;
                if (DDONVITINHID.Length > 0)
                    row["DDONVITINH_NAME"] = new DDONVITINHRow(DDONVITINHID).NAME;

                row["DDONVITINHID"] = DDONVITINHID;
                row["DMATHANG_CODE"] = spRow.CODE;
                row["TILEGIAMGIA"] = 0;
                row["DMATHANG_MASANCO"] = spRow.MASANCO;

                Track("QLBH: Thêm mặt hàng '" + spRow.NAME + "', SL: " + 1);

                dt.Rows.Add(row.Row);
                grMua.CalculateRow(row.Row);
                mapper.RaiseOnCalculation();
            }
		}

        void form_CustomLoadData(object sender, CustomLoadDataArgs e)
        {
            if (e.Where.Length > 0) e.Where += " AND ";
            e.Where += "COALESCE(LOAIDINHLUONG, 0) <> 4";
        }

        #region IRefreshable Members

        public void DoRefresh()
        {
            LoadData();
        }

        #endregion


		public void btnBillHuy_Click(Object sender, EventArgs e)
		{
            DanhSachBillHuy form = (DanhSachBillHuy)Config.CreateForm(Forms.DanhSachBillHuy);
            form.form.ShowDialog();
		}


		public void grMua_OnRowCalculate(Object sender, DataRow r, String fieldName)
		{
            decimal quyDoi = 1;
            //tính toán lại số lượng quy đổi
            if (suDung2DVT && fieldName == "SLXUATCHUAQUYDOI")
            {
                string DMATHANGID = r["DMATHANGID"].ToString();
                DMATHANGRow mhRow = new DMATHANGRow(DMATHANGID);                
                if (TonKhoHandler.Has2DonViTinh(mhRow) && r["DDONVITINHID"].ToString() != mhRow.DDONVITINHID)
                {
                    quyDoi = mhRow.QUYDOI;
                }                
            }
            r["SLXUAT"] = quyDoi * ConvertTo.Decimal(r["SLXUATCHUAQUYDOI"]);

            if (fieldName == "TILEGIAMGIA")
            {
                r["GIAMTHEOTIEN"] = 0;
            }
            else if (fieldName == "TIENGIAMGIA")
            {
                r["GIAMTHEOTIEN"] = 30;
            }

            if (ConvertTo.Int(r["GIAMTHEOTIEN"]) == 30)
            {
                decimal soLuong = ConvertTo.Decimal(r["SLXUATCHUAQUYDOI"]);
                decimal donGia = ConvertTo.Decimal(r["DONGIA"]);
                decimal tienGiam = ConvertTo.Decimal(r["TIENGIAMGIA"]);
                r["TILEGIAMGIA"] = donGia * soLuong == 0 ? 0 : Math.Round(tienGiam * 100 / (donGia * soLuong), 2);
                r["THANHTIEN"] = soLuong * donGia - tienGiam;
            }
            else
            {
                decimal thanhTien = ConvertTo.Decimal(r["SLXUATCHUAQUYDOI"]) * ConvertTo.Decimal(r["DONGIA"]);
                r["TIENGIAMGIA"] = thanhTien * ConvertTo.Decimal(r["TILEGIAMGIA"]) / 100;
                r["THANHTIEN"] = thanhTien * (1 - ConvertTo.Decimal(r["TILEGIAMGIA"]) / 100);
            }
		}


		public void lueCuaHang_OnEditValueChanged(object sender, object value)
		{
            LoadData();
		}


		public void lueCuaHang_CustomLoadData(object sender, CustomLoadDataArgs e)
		{
            if (!DbConfig.IsAdmin)
            {
                e.Where += " AND ID IN (SELECT DCUAHANGID FROM TNGUOIDUNGTHEOCUAHANG WHERE SUSERID = '" + DbConfig.UserID + "')";
            }
		}


		public void Timer1_Tick(object sender, EventArgs e)
		{
            Timer1.Enabled = false; 
            string ID = grMain.SelectedID;
            if (ID.Length == 0)
            {
                KryptonSplitContainer1.Panel2.Enabled = false;
            }
            else
            {
                KryptonSplitContainer1.Panel2.Enabled = true;
                ReLoad(ID);
            }
		}


		public void txtLoc_TextChanged(object sender, EventArgs e)
		{
            grMain.GridView.Filter = txtLoc.Text;
		}


		public void numTIENTHANHTOAN_OnEditValueChanged(object sender, object value)
		{
			//khi thay đổi tiền thanh toán, chỉ 1 nội dung được thay đổi theo, các nội dung khác về 0            
            No1NumericUpDown[] array = new No1NumericUpDown[] { numTHETRATRUOC, numTIENMAT, numTHE, numVOUCHER, numTRUTICHLUY, numCHUYENKHOAN };
            foreach (No1NumericUpDown num in array)
            {
                num.LockEvent = true;
            }

            paymentControl.EditValue = numTIENTHANHTOAN.Value;
            foreach (No1NumericUpDown num in array)
            {
                if (num != paymentControl) num.Value = 0;
            }

            foreach (No1NumericUpDown num in array)
            {
                num.LockEvent = false;
            }
		}


		public void numTIENMAT_OnEditValueChanged(object sender, object value)
		{
			//khi thay đổi giá trị, cập nhật lại tiền thanh toán
            numTIENTHANHTOAN.LockEvent = true;
            numTIENTHANHTOAN.Value = numTIENMAT.Value + numTHE.Value + numCHUYENKHOAN.Value + numVOUCHER.Value + numTRUTICHLUY.Value + numTHETRATRUOC.Value;
            numTIENTHANHTOAN.LockEvent = false;
		}
    }
}
