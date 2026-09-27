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
    public partial class DanhMucTheChuaKichHoat
    {
		public void tsbKichHoat_Click(object sender, EventArgs e)
		{
			//kiểm tra xem đã kích hoạt chưa?
            string ID = grMain.SelectedID;
            if (ID.Length > 0)
            {
                TGIAHANTHERow row = new TGIAHANTHERow(ID);
                if (row.IsNullValue(TGIAHANTHEInfo.TUNGAY) && row.IsNullValue(TGIAHANTHEInfo.DENNGAY) && row.CHUAKICHHOAT == 30)
                {
                    DKHACHHANGRow khRow = new DKHACHHANGRow(row.DKHACHHANGID);
                    if (Msg.ShowYesNo("Bạn có muốn kích hoạt khách hàng '" + khRow.NAME + "' với mã thẻ '" + khRow.MAKHACH + "'?") == DialogResult.Yes)
                    {
                        DateTime today = Config.Db.DbDate;
                        TGIAHANTHERow upRow = new TGIAHANTHERow(ID);
                        upRow.TUNGAY = today;

                        int SoThang = row.SOTHANG;
                        int SoNgay = row.SONGAY + row.NGAYTANGTHEM;
                        DateTime denNgayThuc = today.AddMonths(SoThang).AddDays(SoNgay - 1);
                        upRow.DENNGAY = denNgayThuc;
                        upRow.DENNGAYTHUC = denNgayThuc;
                        upRow.CHUAKICHHOAT = 0;
                        upRow.Update();

                        TGIAHANTHE0Ae.CapNhatThongTinKhachHangVaKhoaMoThe(row.DKHACHHANGID);

                        LoadData();
                    }
                }
                else
                {
                    Msg.ShowWarning("Khách hàng này đã được kích hoạt");
                    LoadData();
                }
            }
            else
            {
                Msg.ShowWarning("Mời bạn chọn khách hàng để kích hoạt");
            }
		}


		public void tsbRefresh_Click(object sender, EventArgs e)
		{
            LoadData();
		}

        private void LoadData()
        {
            grMain.LoadData();
        }


		public void grMain_SelectionChanged(object sender, EventArgs e)
		{
            tsbKichHoat.Enabled = grMain.SelectedRows.Count > 0;
		}
    }
}
