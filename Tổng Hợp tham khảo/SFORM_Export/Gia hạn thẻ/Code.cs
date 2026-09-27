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
    /// <summary>
    /// ĐĂNG KÝ MỚI, GIA HẠN THẺ
    /// </summary>
    public partial class TGIAHANTHE0Ae
    {
		public void mapper_AfterFillData(object sender, EventArgs e)
		{
            UpdateDatCoc();

            if (mapper.ID.Length > 0)
            {
                TaiThongTinKhachHang();
                txtMaThe.ReadOnly = true;
            }
            else
            {
                mapper[TGIAHANTHEInfo.LOAI].Value = 0;
                mapper[TGIAHANTHEInfo.DLOAIGIAODICHID].Value = LoaiGiaoDichIds.GiaHanThe;
                //không cho đổi lại ngày thực hiện
                dtNGAY.Enabled = false;
                
                if (dtTUNGAY.IsEmpty)
                {
                    dtTUNGAY.DateTime = Config.Db.DbDate;
                }
            }

            lueDKHACHHANGID.Select();
		}

		public void mapper_OnLoad(object sender, EventArgs e)
		{
			//phân quyền: được phép sửa hay không?
            if (!DbUtils.CanView(Functions.ChoPhepThayDoiThongTinNgaySoLanSoTienKhiGiaHan))
            {
                dtTUNGAY.Enabled = false;
                dtDENNGAY.Enabled = false;
                numNGAYTANGTHEM.ReadOnly = true;
                numLANTANGTHEM.ReadOnly = true;
                numSOLAN.ReadOnly = true;
                numSOTIEN.ReadOnly = true;
                numTILEGIAMGIA.ReadOnly = true;
                numTIENGIAMGIA.ReadOnly = true;
            }

            //ẩn số lần nếu không sử dụng
            if (SystemConfig.CoSuDungTheTheoLan == 0)
            {
                numSOLAN.Visible = false;
                lblSOLAN.Visible = false;
                numLANTANGTHEM.Visible = false;
                lblLANTANGTHEM.Visible = false;
            }

            if (SystemConfig.ChoPhepKhachNoGym != 30)
            {
                numTHANHTOAN.ReadOnly = true;
            }
		}

        public static int GetSoLanDaTap(string TGIAHANTHEID)
        {
            TGIAHANTHERow row = new TGIAHANTHERow(TGIAHANTHEID);
            return row.DATAP;
        }

		public void lueDKHACHHANGID_OnEditValueChanged(object sender, object value)
		{
			//lấy ngày gần nhất
            RefreshNgayGanNhat();

            //hiển thị thông tin khách hàng
            TaiThongTinKhachHang();
		}

        private void RefreshNgayGanNhat()
        {
            bool DangKyMoi = false;
            dtTUNGAY.EditValue = GetDenNgayLonNhat(lueDKHACHHANGID.StringValue, out DangKyMoi);
            mapper[TGIAHANTHEInfo.DLOAIGIAODICHID].Value = DangKyMoi ? LoaiGiaoDichIds.DangKyMoi : LoaiGiaoDichIds.GiaHanThe;
            chkCHUAKICHHOAT.Enabled = DangKyMoi;
            UpdateDenNgay();
        }

        private void TaiThongTinKhachHang()
        {
            if (lueDKHACHHANGID.StringValue.Length > 0)
            {
                DKHACHHANGRow khRow = new DKHACHHANGRow(lueDKHACHHANGID.StringValue);
                txtTenKhach.Text = khRow.NAME;
                txtDiaChi.Text = khRow.DIACHI;
                txtDienThoai.Text = khRow.DIENTHOAI;
                if (SystemConfig.ThietBiSuDung == (int)ThietBiSuDung.DauDocTheTu)
                {
                    txtMaThe.Text = khRow.MAKHACH;                
                }
                else                       
                {
                    long maThe = ConvertTo.Long(khRow.MAKHACH);
                    if (maThe > 0)
                    {
                        txtMaThe.Text = maThe.ToString();
                    }
                }                               
            }
            else
            {
                txtTenKhach.Text = "";
                txtDiaChi.Text = "";
                txtDienThoai.Text = "";
            }
        }

        private void UpdateDenNgay()
        {
            if (dtTUNGAY.IsEmpty) dtDENNGAY.EditValue = null;
            else
            {
                int SoThang = (int) numSOTHANG.Value;
                int SoNgay = (int) numSONGAY.Value + (int) numNGAYTANGTHEM.Value;
                dtDENNGAY.DateTime = dtTUNGAY.DateTime.AddMonths(SoThang).AddDays(SoNgay - 1);
            }
        }

        /// <summary>
        /// Trả lại ngày thực tế khách tiến hành giao dịch. Ví dụ khách đăng ký ngày 5/7, từ ngày 10/7 đến 9/8
        /// Hệ thống sẽ trả lại ngày 5/7
        /// </summary>
        /// <param name="DKHACHHANGID">Mã khách hàng</param>
        /// <returns></returns>
        public static DateTime GetNgayGiaoDichLonNhat(string DKHACHHANGID)
        {
            string sql = String.Format("SELECT MAX(NGAY) FROM TGIAHANTHE WHERE DKHACHHANGID = '{0}'", DKHACHHANGID);
            object val = Config.Db.GetFirstField(sql);
            if (val == null) return DateTime.MinValue;
            return ConvertTo.Date(val);
        }

        /// <summary>
        /// Trả về ngày được tập lớn nhất của hội viên. Ví dụ khách đăng ký ngày 5/7, từ ngày 10/7 đến 9/8
        /// Hệ thống trả lại 9/8
        /// </summary>
        /// <param name="DKHACHHANGID"></param>
        /// <param name="DangKyMoi"></param>
        /// <returns></returns>
        private static DateTime GetDenNgayLonNhat(string DKHACHHANGID, out bool DangKyMoi)
        {            
            DangKyMoi = true;
            if (DKHACHHANGID.Length == 0) return Config.Db.DbDate;

            //nếu thẻ theo lần
            if (SystemConfig.CoSuDungTheTheoLan == 30)
            {
                DKHACHHANGRow khRow = new DKHACHHANGRow(DKHACHHANGID);
                if (khRow.SOLAN > 0)
                {
                    DangKyMoi = false;
                    return Config.Db.DbDate;
                }
            }

            string sql = String.Format("SELECT MAX(COALESCE(DENNGAYTHUC, DENNGAY)) FROM TGIAHANTHE WHERE DKHACHHANGID = '{0}'", DKHACHHANGID);
            object val = Config.Db.GetFirstField(sql);
            //nếu chưa có thông tin đến ngày (chưa có lần đăng ký nào)
            if (val == null || val == DBNull.Value)
            {
                DangKyMoi = true;
                return Config.Db.DbDate;
            }
            else
            {
                DangKyMoi = false;
                //lấy ngày hôm sau
                return ConvertTo.Date(val).AddDays(1);
            }
        }


		public void lueDLOAITHEID_OnEditValueChanged(object sender, object value)
		{
            if (lueDLOAITHEID.StringValue.Length == 0)
            {
                numSOLAN.Value = 0;
                numSOTIEN.Value = 0;
                numNGAYTANGTHEM.Value = 0;
                numLANTANGTHEM.Value = 0;
                numTILEGIAMGIA.Value = 0;
                numTIENGIAMGIA.Value = 0;
            }
            else
            {
                DLOAITHERow loaiThe = new DLOAITHERow(lueDLOAITHEID.StringValue);
                numSOLAN.Value = loaiThe.SOLAN + numLANTANGTHEM.Value + GetSoLanConDu();
                numSOTIEN.Value = loaiThe.GIABAN;
                numSOTHANG.Value = loaiThe.SOTHANG;
                numSONGAY.Value = loaiThe.SONGAY;
                lueDCATAPID.EditValue = loaiThe.DCATAPID;
                //lấy lần tặng thêm, ngày tặng thêm theo khuyến mại tự động
                DKHUYENMAIGYMCHITIETRow kmRow = GetKhuyenMaiDangSuDung(lueDLOAITHEID.StringValue);
                if (kmRow == null)
                {
                    numTILEGIAMGIA.Value = 0;
                    numTIENGIAMGIA.Value = 0;
                    numLANTANGTHEM.Value = 0;
                    numNGAYTANGTHEM.Value = 0;
                }
                else
                {
                    if (kmRow.TILEGIAMGIA != 0)
                    {
                        mapper[TGIAHANTHEInfo.GIAMTHEOTIEN].Value = 0;
                        numTILEGIAMGIA.Value = kmRow.TILEGIAMGIA;                        
                    }
                    else
                    {
                        mapper[TGIAHANTHEInfo.GIAMTHEOTIEN].Value = 30;
                        numTIENGIAMGIA.Value = kmRow.TIENGIAMGIA;
                    }
                    
                    numLANTANGTHEM.Value = kmRow.TANGLAN;
                    numNGAYTANGTHEM.Value = kmRow.TANGNGAY;
                }

                mapper.RaiseOnCalculation();
            }

            UpdateDenNgay();
		}

        private decimal GetSoLanConDu()
        {
            if (SystemConfig.CoSuDungTheTheoLan == 0) return 0;
            DKHACHHANGRow khRow = new DKHACHHANGRow(lueDKHACHHANGID.StringValue);
            if (khRow.DLOAITHEID.Length == 0) return 0;
            DLOAITHERow loaiThe = new DLOAITHERow(khRow.DLOAITHEID);
            //thẻ không theo lần
            if (loaiThe.SOLAN== 0) return 0;
            //lấy giao dịch gần nhất
            string sql = @"SELECT FIRST 1 * FROM TGIAHANTHE WHERE DKHACHHANGID = '" + lueDKHACHHANGID.StringValue + "'";
            FbCommand cmd = Config.Db.GetCommand("");
            if (mapper.ID.Length > 0)
            {
                TGIAHANTHERow row = new TGIAHANTHERow(mapper.ID);
                sql += " AND ID <> '{1}' AND ((NGAY < @NGAY) OR (NGAY = @NGAY AND TIMECREATED < @TIMECREATED))";
                cmd.Parameters.Add("@NGAY", row.NGAY);
                cmd.Parameters.Add("@TIMECREATED", row["TIMECREATED"]);
            }
            sql += " ORDER BY NGAY DESC, TIMECREATED DESC";
            cmd.CommandText = sql;
            DataRow firstRow = Config.Db.GetFirstRow(cmd);
            if (firstRow == null) return 0;
            TGIAHANTHERow r = new TGIAHANTHERow(firstRow);
            return r.SOLAN - r.DATAP;
        }

        internal static DKHUYENMAIGYMCHITIETRow GetKhuyenMaiDangSuDung(string DLOAITHEID)
        {
            DataRow r = Config.Db.GetFirstRow("SELECT * FROM DDOTKHUYENMAI WHERE CURRENT_DATE BETWEEN TUNGAY AND DENNGAY AND STATUS = 30 AND COALESCE(NGUNGAPDUNG, 0) = 0");
            if (r == null) return null;
            return new DKHUYENMAIGYMCHITIETRow(r);
        }


		public void dtTUNGAY_OnEditValueChanged(object sender, object value)
		{
            UpdateDenNgay();
		}


		public void numTILEGIAMGIA_OnEditValueChanged(object sender, object value)
		{
            if (numTILEGIAMGIA.Focused)
            {
                mapper[TGIAHANTHEInfo.GIAMTHEOTIEN].Value = 0;
            }
		}


		public void numTIENGIAMGIA_OnEditValueChanged(object sender, object value)
		{
            if (numTIENGIAMGIA.Focused)
            {
                mapper[TGIAHANTHEInfo.GIAMTHEOTIEN].Value = 30;
            }
		}


		public void mapper_OnCalculation(object sender, EventArgs e)
		{            
            int value = mapper[TGIAHANTHEInfo.GIAMTHEOTIEN].ToInt();
            if (value == 0)
            {
                numTIENGIAMGIA.Value = numSOTIEN.Value * numTILEGIAMGIA.Value / 100;
            }
            else
            {
                if (numSOTIEN.Value == 0) numTILEGIAMGIA.Value = 0;
                else numTILEGIAMGIA.Value = numTIENGIAMGIA.Value * 100 / numSOTIEN.Value;
            }

            numTONGCONG.Value = numSOTIEN.Value - numTIENGIAMGIA.Value;

            if (!numTHANHTOAN.Focused)
            {
                numTHANHTOAN.Value = numTONGCONG.Value - numDatTruoc.Value;
            }
		}


		public void numNGAYTANGTHEM_OnEditValueChanged(object sender, object value)
		{
            UpdateDenNgay();
		}


		public void numLANTANGTHEM_OnEditValueChanged(object sender, object value)
		{
            int SoLan = 0;
            if (lueDLOAITHEID.StringValue.Length > 0)
            {
                SoLan = new DLOAITHERow(lueDLOAITHEID.StringValue).SOLAN;
            }
            numSOLAN.Value = SoLan + (int)numLANTANGTHEM.Value + GetSoLanConDu();
		}


		public void mapper_AfterSave(object sender, EventArgs e)
		{
            //cập nhật thông tin khách
            CapNhatThongTinKhachHang(lueDKHACHHANGID.StringValue);

            //cập nhật thông tin thẻ nếu có
            DKHACHHANGRow khRow = new DKHACHHANGRow(lueDKHACHHANGID.StringValue);                        
            string maKhach = txtMaThe.Text.Trim();
            if (SystemConfig.ThietBiSuDung == (int)ThietBiSuDung.CuaTu)
            {
                maKhach = ConvertTo.Long(maKhach).ToString();
            }
            if (khRow.MAKHACH != maKhach)
            {
                DKHACHHANGRow upRow = new DKHACHHANGRow(lueDKHACHHANGID.StringValue);
                upRow.MAKHACH = maKhach;
                khRow.MAKHACH = maKhach;
                upRow.Update();                
            }

            if (SystemConfig.TuDongTaoXoaThe == 30 && !chkCHUAKICHHOAT.Checked)
            {
                TaoTheKhachHang(khRow);
            }
		}

        internal static void TaoTheKhachHang(DKHACHHANGRow khRow)
        {
            long maKhach = ConvertTo.Long(khRow.MAKHACH);

            if (maKhach < 0)
            {
                Msg.ShowWarning("Mã thẻ phải là số nguyên dương");
                return;
            }
            //tạo mã thẻ trên máy
            string msgOk = "";
            string msgError = "";

            int nhom = SystemConfig.NhomKhongBiKhoa;
            if (khRow.DCATAPID.Length > 0)
            {
                nhom = new DCATAPRow(khRow.DCATAPID).NHOMTRENMAY;
            }

            //Chỉ mở thẻ trong trường hợp trạng thái đang sử dụng
            //Ngược lại khóa thẻ ngay
            if (khRow.DTRANGTHAIID == TrangThaiIds.DangSuDung)
            {
                Shared.QuanLyThietBi.TaoThe((int)maKhach, khRow.MAVANTAY, nhom, ref msgOk, ref msgError, khRow.NAME);
            }
            else
            {
                Shared.QuanLyThietBi.KhoaThe((int)maKhach, ref msgOk, ref msgError);
            }
        } 

        internal static void CapNhatThongTinKhachHang(string DKHACHHANGID)
        {
            string sql = string.Format("SELECT * FROM CAPNHATTRANGTHAI('{0}', 0)", DKHACHHANGID);
            Config.Db.GetTable(sql);
        }

        internal static void CapNhatThongTinKhachHangVaKhoaMoThe(string DKHACHHANGID)
        {
            CapNhatThongTinKhachHang(DKHACHHANGID);
            if (SystemConfig.TuDongTaoXoaThe == 30)
            {
                TaoTheKhachHang(new DKHACHHANGRow(DKHACHHANGID));
            }
        }


		public void mapper_OnSetValue(string fieldName, object fieldID)
		{
            if (fieldName == "TDATCOCID")
            {
                UpdateDatCoc();
            }
		}

        private void UpdateDatCoc()
        {
            string TDATCOCID = mapper[TGIAHANTHEInfo.TDATCOCID].ToStringValue();
            if (TDATCOCID.Length > 0)
            {
                lblDatTruoc.Visible = true;
                numDatTruoc.Visible = true;
                numDatTruoc.Value = new TTHUCHIRow(TDATCOCID).THU;
            }
            else
            {
                lblDatTruoc.Visible = false;
                numDatTruoc.Visible = false;
            }
        }

        internal void LoadDatTruoc(string TTHUCHIID)
        {
            TTHUCHIRow row = new TTHUCHIRow(TTHUCHIID);

            if (row.DKHACHHANGID.Length > 0)
            {
                lueDKHACHHANGID.EditValue = row.DKHACHHANGID;
            }
            else
            {
                mapper[TGIAHANTHEInfo.DKHACHHANGID].AllowEmpty = true;
                txtTenKhach.Text = row.TENDOITUONG;
                txtDiaChi.Text = row.DIACHI;
                txtDienThoai.Text = row.DIENTHOAI;
            }

            mapper[TGIAHANTHEInfo.TDATCOCID].Value = TTHUCHIID;
            UpdateDatCoc();
            lueDKHACHHANGID.Enabled = false;
            txtDienThoai.TabStop = true;
            txtDiaChi.TabStop = true;
            txtTenKhach.TabStop = true;            
            mapper[TGIAHANTHEInfo.GIAMTHEOTIEN].Value = 0;            
            mapper[TGIAHANTHEInfo.DLOAIGIAODICHID].Value = LoaiGiaoDichIds.DangKyMoi;
            dtTUNGAY.DateTime = Config.Db.DbDate;
            lueDLOAITHEID.EditValue = row.DLOAITHEID;

            numTILEGIAMGIA.LockEvent = true;
            numTILEGIAMGIA.Value = row.GIAMGIA;
            numTILEGIAMGIA.LockEvent = false;
            numTIENGIAMGIA.LockEvent = true;
            numTIENGIAMGIA.Value = row.TIENGIAM;
            numTIENGIAMGIA.LockEvent = false;
            mapper[TGIAHANTHEInfo.GIAMTHEOTIEN].Value = row.GIAMTHEOTIEN;

            mapper.RaiseOnCalculation();
       
            txtMaThe.Select();
        }

		public void mapper_BeforeSave(object sender, SaveCancelEventArgs e)
		{
            mapper[TGIAHANTHEInfo.TUNGAY].AllowEmpty = chkCHUAKICHHOAT.Checked;
            mapper[TGIAHANTHEInfo.DENNGAY].AllowEmpty = chkCHUAKICHHOAT.Checked;

            if (!chkCHUAKICHHOAT.Checked)
            {                
                if (dtTUNGAY.DateTime > dtDENNGAY.DateTime)
                {
                    Msg.ShowWarning("Từ ngày phải trước đến ngày");
                    e.Cancel = true;
                    return;
                }
            }

            //bắt buộc phải nhập mã thẻ khi gia hạn thẻ
            long maThe = ConvertTo.Long(txtMaThe.Text);
            if (maThe == 0)
            {
                Msg.ShowWarning("Mời bạn nhập mã thẻ");
                e.Cancel = true;
                txtMaThe.Select();
                return;
            }

            //kiểm tra ngày phải sau ngày giao dich gần nhất
            if (lueDKHACHHANGID.StringValue.Length > 0)
            {
                DateTime ngayGanNhat = GetNgayGiaoDichLonNhat(lueDKHACHHANGID.StringValue);
                if (dtNGAY.DateTime < ngayGanNhat)
                {
                    Msg.ShowWarning("Ngày thực hiện giao dịch phải nhỏ hơn ngày thực hiện giao dịch gần nhất (" + ngayGanNhat.ToString("dd/MM/yyyy") + ")");
                    e.Cancel = true;
                    return;
                }
            }

            int soNgayBaoLuu = 0;
            //Chỉ xử lý tạo mới khách trường hợp thêm mới
            if (mapper.ID.Length == 0)
            {
                if (!mapper.IsValid())
                {
                    e.Cancel = true;
                    return;
                }

                //tạo mới khách hàng
                if (lueDKHACHHANGID.StringValue.Length == 0)
                {
                    if (txtTenKhach.Text.Trim().Length == 0)
                    {
                        Msg.ShowWarning("Mời bạn nhập họ tên khách hàng");
                        e.Cancel = true;
                        return;
                    }

                    if (txtDienThoai.Text.Trim().Length == 0)
                    {
                        Msg.ShowWarning("Mời bạn nhập địa chỉ khách hàng");
                        e.Cancel = true;
                        return;
                    }

                    //kiểm tra mã thẻ phải là số nguyên đồng thời chưa có trong hệ thống
                    if (!IsMaTheOk())
                    {
                        e.Cancel = true;
                        return;
                    }

                    DKHACHHANGRow khRow = new DKHACHHANGRow();
                    khRow.DIACHI = txtDiaChi.Text;
                    khRow.DIENTHOAI = txtDienThoai.Text;
                    khRow.NAME = txtTenKhach.Text;
                    khRow.MAKHACH = txtMaThe.Text;
                    khRow.Update();
                    lueDKHACHHANGID.LoadData(Tables.DKHACHHANG);
                    lueDKHACHHANGID.EditValue = khRow.ID;
                    lueDKHACHHANGID_OnEditValueChanged(null, null);
                }
                else
                {
                    //ngày phải trước ngày cuối cùng                    
                    DateTime ngayCuoi = TGIAHANTHE0Ae.GetNgayGiaoDichLonNhat(lueDKHACHHANGID.StringValue);
                    if (dtNGAY.DateTime < ngayCuoi)
                    {
                        Msg.ShowWarning("Ngày thực hiện phải sau ngày cuối khách hàng giao dịch (" + ngayCuoi.ToString("dd/MM/yyyy") + ")");
                        e.Cancel = true;
                        return;
                    }                    
                }
            }
            else
            {
                //kiểm tra xem có phải giao dịch cuối cùng không?
                //chỉ cho sửa giao dịch cuối cùng
                if (!LaGiaoDichCuoi(mapper.ID, lueDKHACHHANGID.StringValue))
                {
                    Msg.ShowWarning("Không thể cập nhật vì đã phát sinh các giao dịch gia hạn, đổi thẻ, bảo lưu phía sau giao dịch này");
                    e.Cancel = true;
                    return;
                }

                DKHACHHANGRow khRow = new DKHACHHANGRow(lueDKHACHHANGID.StringValue);
                if (khRow.MAKHACH != maThe.ToString())
                {
                    //với trường hợp điều chỉnh mã thẻ thì kiểm tra xem mã thẻ ok không?                
                    if (!IsMaTheOk())
                    {
                        e.Cancel = true;
                        return;
                    }
                }

                string sql = string.Format("SELECT SONGAY FROM TGIAHANTHE WHERE DLOAIGIAODICHID = '{0}' AND REFID = '{1}'", LoaiGiaoDichIds.BaoLuuThe, mapper.ID);
                soNgayBaoLuu = Config.Db.GetFirstFieldInt(sql);
            }

            mapper[TGIAHANTHEInfo.DENNGAYTHUC].Value = dtDENNGAY.DateTime.AddDays(soNgayBaoLuu);
            mapper[TGIAHANTHEInfo.DOANHSO].Value = numTONGCONG.Value;
		}

        public static bool LaGiaoDichCuoi(string TGIAHANTHEID, string DKHACHHANGID)
        {
            //nếu chưa lưu
            if (TGIAHANTHEID.Length == 0) return true;

            return GetGiaoDichCuoiID(DKHACHHANGID, "") == TGIAHANTHEID;
        }

        /// <summary>
        /// Hàm kiểm tra xem giao dịch có phải là giao dịch cuối cùng không?
        /// Ví dụ:
        /// Ngày 1/5 đăng ký mới 6 tháng từ 1/5 đến 30/10
        /// Ngày 10/5 bảo lưu từ 1/7 đến 31/7
        /// Giao dịch cuối là giao dịch bảo lưu.
        /// </summary>
        /// <param name="DKHACHHANGID"></param>
        /// <returns></returns>
        public static string GetGiaoDichCuoiID(string DKHACHHANGID, string CustomWhere)
        {
            string sql = String.Format("SELECT FIRST 1 ID FROM TGIAHANTHE WHERE DKHACHHANGID = '{0}'", DKHACHHANGID);
            if (CustomWhere.Length > 0) sql += " AND " + CustomWhere;
            sql += " ORDER BY NGAY DESC, TIMECREATED DESC";
            string lastID = Config.Db.GetFirstFieldString(sql);
            return lastID;
        }

        /// <summary>
        /// Hàm kiểm tra xem mã thẻ có ok không
        /// - Mã thẻ không được trống
        /// - Mã thẻ phải là số nguyên
        /// - Mã thẻ không được trùng với khách khác
        /// </summary>
        /// <returns></returns>
        private bool IsMaTheOk()
        {
            if (txtMaThe.Text.Trim().Length == 0)
            {
                Msg.ShowWarning("Mã thẻ không được phép trống");
                return false;
            }

            long maThe = ConvertTo.Long(txtMaThe.Text);
            if (maThe == 0 && txtMaThe.Text.Trim() != "0")
            {
                Msg.ShowWarning("Mã thẻ phải là số nguyên dương");
                return false;
            }

            string sql = String.Format("SELECT COUNT(*) FROM DKHACHHANG WHERE MAKHACH = '{0}' AND ID <> '{1}'", maThe, lueDKHACHHANGID.StringValue);
            int val = Config.Db.GetFirstFieldInt(sql);
            if (val > 0)
            {
                Msg.ShowWarning("Mã thẻ '" + txtMaThe.Text + "' đã tồn tại trong hệ thống");
                return false;
            }

            return true;
        }


		public void chkCHUAKICHHOAT_OnEditValueChanged(object sender, object value)
		{
            if (chkCHUAKICHHOAT.Focused)
            {
                dtTUNGAY.Enabled = !chkCHUAKICHHOAT.Checked;
                dtDENNGAY.Enabled = !chkCHUAKICHHOAT.Checked;
                if (chkCHUAKICHHOAT.Checked)
                {
                    dtTUNGAY.EditValue = null;
                    dtDENNGAY.EditValue = null;
                }
                else
                {
                    RefreshNgayGanNhat();
                }
            }
		}
    }
}
