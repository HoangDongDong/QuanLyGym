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
    public partial class TaiKhoanNguoiDung
    {
		public void lueDNHANVIENID_OnEditValueChanged(object sender, object value)
		{
            if (lueDNHANVIENID.StringValue.Length == 0)
            {
                txtNAME.Enabled = true;
            }
            else
            {
                txtNAME.Enabled = false;
                txtNAME.Text = lueDNHANVIENID.DisplayText;
            }
		}


		public void mapper_AfterSave(object sender, EventArgs e)
		{
            Config.Db.ExecSql("DELETE FROM TNGUOIDUNGTHEOCUAHANG WHERE SUSERID = '" + mapper.ID + "'");
            DataTable dt = grMain.DataSource as DataTable;
            foreach (DataRow r in dt.Rows)
            {
                if (ConvertTo.Int(r["TRUYCAP"]) == 30)
                {
                    TNGUOIDUNGTHEOCUAHANGRow row = new TNGUOIDUNGTHEOCUAHANGRow();
                    row.SUSERID = mapper.ID;
                    row.DCUAHANGID = r["ID"].ToString();
                    row.Update();
                }
            }
		}


		public void mapper_BeforeSave(object sender, SaveCancelEventArgs e)
		{
			//kiem tra xem co cua hang nao khong?
            DataTable dt = grMain.DataSource as DataTable;
            if (dt.Select("TRUYCAP=30").Length == 0)
            {
                if (Msg.ShowYesNo("Bạn chưa phân quyền cho người dùng vào cửa hàng nào, bạn có muốn tiếp tục không?") != DialogResult.Yes)
                    e.Cancel = true;
            }
		}


		public void mapper_AfterFillData(object sender, EventArgs e)
		{
			//lay ra danh sach cac cua hang
            string sql = @"SELECT DCUAHANG.ID, TNGUOIDUNGTHEOCUAHANG.ID AS REFID, DCUAHANG.NAME AS CUAHANG, 
CASE WHEN TNGUOIDUNGTHEOCUAHANG.ID IS NULL THEN 0 ELSE 30 END AS TRUYCAP
FROM DCUAHANG LEFT OUTER JOIN TNGUOIDUNGTHEOCUAHANG ON DCUAHANG.ID = TNGUOIDUNGTHEOCUAHANG.DCUAHANGID
AND SUSERID = '" + mapper.ID + "'";
            DataTable dt = Config.Db.GetTable(sql);
            if (mapper.ID.Length == 0)
            {
                foreach (DataRow r in dt.Rows) r["TRUYCAP"] = 30;
            }
            grMain.DataSource = dt;
            colTruyCap.TrueValue = 30;
            colTruyCap.FalseValue = 0;
            txtNAME.Enabled = lueDNHANVIENID.StringValue.Length > 0;
		}


		public void grMain_CellMouseClick(object sender, DataGridViewCellMouseEventArgs e)
		{
            if (e.RowIndex >= 0 && e.ColumnIndex == colTruyCap.Index)
            {
                DataRow r = grMain.SelectedRow;
                r["TRUYCAP"] = ConvertTo.Int(r["TRUYCAP"]) == 0 ? 30 : 0;
                grMain.InvalidateRow(e.RowIndex);
                mapper.RaiseChangedEvent(sender);
            }
		}
    }
}
