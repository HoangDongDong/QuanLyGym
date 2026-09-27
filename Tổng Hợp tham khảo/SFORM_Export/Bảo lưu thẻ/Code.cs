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
    /// BẢO LƯU THẺ
    /// </summary>
    public partial class TGIAHANTHE1Ae
    {
		public void lueDKHACHHANGID_OnEditValueChanged(object sender, object value)
		{
			//tải thông tin khách hàng gần nhất
            string REFID = TGIAHANTHE0Ae.GetGiaoDichCuoiID(lueDKHACHHANGID.StringValue, "ID <> '" + mapper.ID + "'");
            //nếu là bảo lưu thì không cho phép
            if (REFID.Length > 0)
            {
                TGIAHANTHERow row = new TGIAHANTHERow(REFID);
                if (row.DLOAIGIAODICHID == LoaiGiaoDichIds.BaoLuuThe)
                {
                    Msg.ShowWarning("Giao dịch gần nhất là bảo lưu, không thể thực hiện bảo lưu tiếp");
                    REFID = "";
                }

                //kiểm tra còn hạn không?
                if (row.DENNGAYTHUC < Config.Db.DbDate)
                {
                    Msg.ShowWarning("Thẻ này đã quá hạn, không thể thực hiện bảo lưu");
                    return;
                }
            }

            mapper[TGIAHANTHEInfo.REFID].Value = REFID;
            baoLuu.Fill(REFID);
		}

        public void CapNhatNgaySauBaoLuu(string TGIAHANTHEID, int SoNgay)
        {
            TGIAHANTHERow upRow = new TGIAHANTHERow(TGIAHANTHEID);
            upRow.DENNGAYTHUC = upRow.DENNGAY.AddDays(SoNgay);
            upRow.Update();
        }

		public void mapper_BeforeSave(object sender, SaveCancelEventArgs e)
		{
            if (dtTUNGAY.DateTime >= dtDENNGAY.DateTime)
            {
                Msg.ShowWarning("Từ ngày phải trước đến ngày");
                e.Cancel = true;
                return;
            }

            if (baoLuu.ID == null || baoLuu.ID.Length == 0)
            {
                Msg.ShowWarning("Vui lòng lựa chọn khách hàng có giao dịch gần nhất là gia hạn thẻ, đổi thẻ hoặc đăng ký thẻ mới");
                e.Cancel = true;
                return;
            }

            DateTime ngay = dtTuNgayRef.DateTime;
            DateTime homNay = Config.Db.DbDate;
            if (ngay > homNay)
            {
                ngay = homNay;
            }

            //ngày bảo lưu phải nằm trong khoảng từ ngày đến ngày
            if (dtTUNGAY.DateTime <= ngay || dtTUNGAY.DateTime >= dtDenNgayRef.DateTime)
            {
                Msg.ShowWarning("Ngày bảo lưu phải sau ngày " + ngay.ToString("dd/MM/yyyy") + " và trước ngày " + dtDenNgayRef.DateTime.ToString("dd/MM/yyyy"));
                e.Cancel = true;
                return;
            }
            
            //kiểm tra xem có phải giao dịch cuối cùng không?
            //chỉ cho sửa giao dịch cuối cùng
            if (!TGIAHANTHE0Ae.LaGiaoDichCuoi(mapper.ID, lueDKHACHHANGID.StringValue))
            {
                Msg.ShowWarning("Không thể cập nhật vì đã phát sinh các giao dịch gia hạn, đổi thẻ, bảo lưu phía sau giao dịch này");
                e.Cancel = true;
                return;
            }

            if (mapper.ID.Length == 0 && lueDKHACHHANGID.StringValue.Length > 0)
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

            mapper[TGIAHANTHEInfo.DENNGAYTHUC].Value = dtDENNGAY.EditValue;
		}


		public void numSONGAY_OnEditValueChanged(object sender, object value)
		{
            if (numSONGAY.Focused)
            {
                dtDENNGAY.DateTime = dtTUNGAY.DateTime.AddDays((int)numSONGAY.Value);
            }
		}


		public void dtTUNGAY_OnEditValueChanged(object sender, object value)
		{
            if (!dtTUNGAY.IsEmpty && !dtDENNGAY.IsEmpty)
            {
                TimeSpan ts = dtDENNGAY.DateTime - dtTUNGAY.DateTime;
                numSONGAY.Value = (int) ts.TotalDays;
            }
		}
         

		public void mapper_AfterSave(object sender, EventArgs e)
		{
			//sau khi bảo lưu thì cộng thêm số ngày vào thông tin cũ
            CapNhatNgaySauBaoLuu(baoLuu.ID, (int) numSONGAY.Value);

            TGIAHANTHE0Ae.CapNhatThongTinKhachHangVaKhoaMoThe(lueDKHACHHANGID.StringValue);
		}

		public void mapper_OnLoad(object sender, EventArgs e)
		{
            lueDCATAPID.LoadData(Tables.DCATAP);
            lueDLOAITHEID.LoadData(Tables.DLOAITHE);
		}


		public void mapper_AfterFillData(object sender, EventArgs e)
		{
            if (mapper.ID.Length == 0)
            {
                mapper[TGIAHANTHEInfo.LOAI].Value = 1;
                mapper[TGIAHANTHEInfo.DLOAIGIAODICHID].Value = LoaiGiaoDichIds.BaoLuuThe;
                mapper[TGIAHANTHEInfo.TONGCONG].Value = 0;
                mapper[TGIAHANTHEInfo.THANHTOAN].Value = 0;
                mapper[TGIAHANTHEInfo.DOANHSO].Value = 0;
            }
            else
            {
                baoLuu.Fill(mapper[TGIAHANTHEInfo.REFID].ToStringValue());
                //không cho đổi lại ngày thực hiện
                dtNGAY.Enabled = false;
            }
		}


		public void mapper_CustomDictionaryForInvoice(object sender, Dictionary<string, object> dic)
		{
            //đưa thêm thông tin loại thẻ cũ
            TGIAHANTHERow row = new TGIAHANTHERow(baoLuu.ID);
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
