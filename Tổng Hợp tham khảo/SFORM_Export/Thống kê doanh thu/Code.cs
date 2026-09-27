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
    public partial class ThongKeDoanhThu:IRefreshable
    {
		public void btnRefresh_Click(Object sender, EventArgs e)
		{
            LoadData();
		}

        private void LoadData()
        {
            grMain.LoadData();
        }

		public void btnIn_Click(Object sender, EventArgs e)
		{
            Config.PrintInvoice(null, Forms.ThongKeDoanhThu, "", true, true, new CustomReportHandler(delegate(DataSet ds, Dictionary<string, object> dic)
            {
                grMain.CopyToDataSet(ds);
                dic.Add("TuNgay", dtNgay.TuNgay);
                dic.Add("DenNgay", dtNgay.DenNgay);
            }));
		}

		public void dtNgay_OnEditValueChanged(Object sender, Object value)
		{
            LoadData();
		}

		public void No1UserControl1_Load(Object sender, EventArgs e)
		{
            dtNgay.LockEvent = true;
            DateTime date = Config.Db.DbDate;
            dtNgay.FromDate = date;
            dtNgay.ToDate = date;
            dtNgay.LockEvent = false;
            grDetail.LoadData();
            summary.GridView = grMain.GridView;
            grMain.GridView.SelectionChanged += new EventHandler(GridView_SelectionChanged);
            lueCuaHang.LoadData(Tables.DCUAHANG);
            LoadData();
		}

        void GridView_SelectionChanged(object sender, EventArgs e)
        {
            grDetail.LoadData();
        }

		public void grDetail_CustomLoadData(Object sender, CustomLoadDataArgs e)
		{
            if (grMain.SelectedID.Length == 0)
            {
                e.Where += "0 = 1";
                return;
            }
            else
            {
                if (e.Where.Length > 0) e.Where += " AND ";
                e.Where += "TDONHANGID = '" + grMain.SelectedID + "'";
            }

            if (e.Where.Length > 0) e.Where += " AND ";
            e.Where += String.Format("COALESCE(LOAIDINHLUONG, 0) <> {0}", (int)LoaiDinhLuong.VatTuNguyenLieu);
		}

		public void grMain_CustomLoadData(Object sender, CustomLoadDataArgs e)
		{
            e.Command.Parameters.Add("@FromDate", FbDbType.Date).Value = dtNgay.TuNgay;
            e.Command.Parameters.Add("@ToDate", FbDbType.Date).Value = dtNgay.DenNgay;

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

        #region IRefreshable Members

        public void DoRefresh()
        {
            LoadData();
        }

        #endregion


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
    }
}
