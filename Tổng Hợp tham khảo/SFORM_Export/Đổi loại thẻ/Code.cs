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
    /// ĐỔI LOẠI THẺ
    /// </summary>
    public partial class TGIAHANTHE2Ae
    {
		public void mapper_OnLoad(object sender, EventArgs e)
		{
            lueLoaiTheRef.LoadData(Tables.DLOAITHE);
            lueCaTapRef.LoadData(Tables.DCATAP);

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

		public void mapper_AfterFillData(object sender, EventArgs e)
		{
            if (mapper.ID.Length == 0)
            {
                mapper[TGIAHANTHEInfo.LOAI].Value = 2;
                mapper[TGIAHANTHEInfo.DLOAIGIAODICHID].Value = LoaiGiaoDichIds.DoiThe;
            }
            else
            {
                //không cho đổi lại ngày thực hiện
                dtNGAY.Enabled = false;
                theCu.Fill(mapper[TGIAHANTHEInfo.REFID].ToStringValue());
                numTruTheCu.Value = numTONGCONGRef.Value;
                TaiThongTinKhachHang();
            }
		}


		public void lueDKHACHHANGID_OnEditValueChanged(object sender, object value)
		{
            //tải thông tin loại thẻ hiện tại nếu có
            string REFID = "";
            TGIAHANTHERow checkRow = null;
            if (lueDKHACHHANGID.StringValue.Length > 0)
            {
                //lấy ra giao dịch gần nhất không phải bảo lưu
                REFID = TGIAHANTHE0Ae.GetGiaoDichCuoiID(lueDKHACHHANGID.StringValue, "ID <> '" + mapper.ID + "'");
                checkRow = new TGIAHANTHERow(REFID);
                //kiểm tra, không phải là thẻ quá hạn và bảo lưu
                if (checkRow.DLOAIGIAODICHID == LoaiGiaoDichIds.BaoLuuThe)
                {
                    Msg.ShowWarning("Lần giao dịch gần nhất là giao dịch bảo lưu, không thể đổi loại thẻ");
                    REFID = "";
                }

                if (checkRow.DENNGAYTHUC < Config.Db.DbDate)
                {
                    Msg.ShowWarning("Thẻ này đã quá hạn sử dụng, không thể đổi");
                    REFID = "";
                }
                //kiểm tra đổi loại thẻ
            }

            TaiThongTinKhachHang();

            theCu.Fill(REFID);
            mapper[TGIAHANTHEInfo.REFID].Value = REFID;

            numTruTheCu.Value = numTONGCONGRef.Value;
            if (REFID.Length > 0)
            {
                dtTUNGAY.DateTime = checkRow.TUNGAY;
            }
            else
            {
                dtTUNGAY.EditValue = null;
            }
		}

        private void TaiThongTinKhachHang()
        {
            if (lueDKHACHHANGID.StringValue.Length > 0)
            {
                DKHACHHANGRow khRow = new DKHACHHANGRow(lueDKHACHHANGID.StringValue);                
                txtDiaChi.Text = khRow.DIACHI;
                txtDienThoai.Text = khRow.DIENTHOAI;
            }
            else
            {                
                txtDiaChi.Text = "";
                txtDienThoai.Text = "";
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
                CapNhatSoLan(loaiThe);
                numSOTIEN.Value = loaiThe.GIABAN;
                numSOTHANG.Value = loaiThe.SOTHANG;
                numSONGAY.Value = loaiThe.SONGAY;
                //lấy lần tặng thêm, ngày tặng thêm theo khuyến mại tự động
                DKHUYENMAIGYMCHITIETRow kmRow = TGIAHANTHE0Ae.GetKhuyenMaiDangSuDung(lueDLOAITHEID.StringValue);
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

        private void UpdateDenNgay()
        {
            if (dtTUNGAY.IsEmpty) dtDENNGAY.EditValue = null;
            else
            {
                int SoThang = (int)numSOTHANG.Value;
                int SoNgay = (int)numSONGAY.Value + (int)numNGAYTANGTHEM.Value;
                dtDENNGAY.DateTime = dtTUNGAY.DateTime.AddMonths(SoThang).AddDays(SoNgay - 1);
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

            numTONGCONG.Value = numSOTIEN.Value - numTIENGIAMGIA.Value - numTONGCONGRef.Value;

            if (!numTHANHTOAN.Focused)
            {
                numTHANHTOAN.Value = numTONGCONG.Value;
            }
		}


		public void mapper_AfterSave(object sender, EventArgs e)
		{
            //cập nhật thông tin khách
            TGIAHANTHE0Ae.CapNhatThongTinKhachHang(lueDKHACHHANGID.StringValue);

            if (SystemConfig.TuDongTaoXoaThe == 30)
            {
                DKHACHHANGRow khRow = new DKHACHHANGRow(lueDKHACHHANGID.StringValue);
                int MaThe = ConvertTo.Int(khRow.MAKHACH);
                if (MaThe > 0)
                {
                    string msgOk = "";
                    string msgError = ""; 
                    if (khRow.DTRANGTHAIID == TrangThaiIds.DangSuDung)
                    {
                        Shared.QuanLyThietBi.MoThe(MaThe, ref msgOk, ref msgError);
                    }
                    else
                    {
                        Shared.QuanLyThietBi.XoaThe(MaThe, ref msgOk);
                    }
                }
            }
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


		public void numLANTANGTHEM_OnEditValueChanged(object sender, object value)
		{
            CapNhatSoLan(null);
		}

        private void CapNhatSoLan(DLOAITHERow row)
        {
            int SoLan = 0;
            if (row == null && lueDLOAITHEID.StringValue.Length > 0)
            {
                row = new DLOAITHERow(lueDLOAITHEID.StringValue);
            }

            if (row != null)
            {
                SoLan = row.SOLAN;
            }
            numSOLAN.Value = SoLan + (int)numLANTANGTHEM.Value;
        }


		public void numNGAYTANGTHEM_OnEditValueChanged(object sender, object value)
		{
            UpdateDenNgay();
		}


		public void mapper_BeforeSave(object sender, SaveCancelEventArgs e)
		{
            //kiểm tra xem có phải giao dịch cuối cùng không?
            //chỉ cho sửa giao dịch cuối cùng
            if (!TGIAHANTHE0Ae.LaGiaoDichCuoi(mapper.ID, lueDKHACHHANGID.StringValue))
            {
                Msg.ShowWarning("Không thể cập nhật vì đã phát sinh các giao dịch gia hạn, đổi thẻ, bảo lưu phía sau giao dịch này");
                e.Cancel = true;
                return;
            }

            //số tiền phải > 0
            if (numTONGCONG.Value < 0)
            {
                Msg.ShowWarning("Chỉ cho phép đổi sang loại thẻ mới với số tiền lớn hơn, không cho phép đổi ngược lại");
                e.Cancel = true;
                return;
            }

            //ngày phải trước ngày cuối cùng
            if (mapper.ID.Length == 0 && lueDKHACHHANGID.StringValue.Length > 0)
            {
                DateTime ngayCuoi = TGIAHANTHE0Ae.GetNgayGiaoDichLonNhat(lueDKHACHHANGID.StringValue);
                if (dtNGAY.DateTime < ngayCuoi)
                {
                    Msg.ShowWarning("Ngày thực hiện phải sau ngày cuối khách hàng giao dịch (" + ngayCuoi.ToString("dd/MM/yyyy") + ")");
                    e.Cancel = true;
                    return;
                }
            }

            int soNgayBaoLuu = 0;
            if (mapper.ID.Length > 0)
            {
                string sql = string.Format("SELECT SONGAY FROM TGIAHANTHE WHERE DLOAIGIAODICHID = '{0}' AND REFID = '{1}'", LoaiGiaoDichIds.BaoLuuThe, mapper.ID);
                soNgayBaoLuu = Config.Db.GetFirstFieldInt(sql);
            }

            mapper[TGIAHANTHEInfo.DENNGAYTHUC].Value = dtDENNGAY.DateTime.AddDays(soNgayBaoLuu);
            mapper[TGIAHANTHEInfo.DOANHSO].Value = numTONGCONG.Value;
            if (mapper.ID.Length == 0)
                mapper[TGIAHANTHEInfo.DATAP].Value = TGIAHANTHE0Ae.GetSoLanDaTap(theCu.ID);
		}


		public void mapper_CustomDictionaryForInvoice(object sender, Dictionary<string, object> dic)
		{
			//đưa thêm thông tin loại thẻ cũ
            TGIAHANTHERow row = new TGIAHANTHERow(theCu.ID);
            dic.Add("NAME_CU", row.NAME);
            dic.Add("NGAY_CU", row.NGAY);
            dic.Add("TUNGAY_CU", row.TUNGAY);
            dic.Add("DENNGAY_CU", row.DENNGAY);
            dic.Add("SOTIEN_CU", row.SOTIEN);
            dic.Add("TONGCONG_CU", row.TONGCONG);
            dic.Add("LOAITHE_CU", new DLOAITHERow(row.DLOAITHEID).NAME);
            dic.Add("CATAP_CU", row.DCATAPID.Length == 0 ? "" : new DCATAPRow(row.DCATAPID).NAME);
		}
    }
}
