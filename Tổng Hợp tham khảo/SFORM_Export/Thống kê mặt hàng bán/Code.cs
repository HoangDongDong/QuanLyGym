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
    public partial class ThongKeMatHangBan:IRefreshable
    {
		public void btnRefresh_Click(Object sender, EventArgs e)
		{
            LoadData();
		}

        decimal tongTienBan;
        decimal tongTienNhap;
        decimal tongGiamGia;
        decimal tongLai;
        decimal thucLai;
        private void LoadData()
        {
            grDetail.LoadData();
            DataTable dt = grDetail.DataSource;
            foreach (DataRow r in dt.Rows)
            {
                decimal thanhTien = ConvertTo.Decimal(r["THANHTIEN"]);
                decimal lai = thanhTien - ConvertTo.Decimal(r["THANHTIENNHAP"]);
                r["LAI"] = lai;
                r["TILELAI"] = thanhTien == 0 ? 0 : 100 * lai / thanhTien;
            }
            //tính tổng tiền bán
            tongTienBan = grDetail.CalcSum("THANHTIEN");
            lblTongTienBan.Text = tongTienBan.ToString("n0");
            //tính tổng tiền nhập
            tongTienNhap = grDetail.CalcSum("THANHTIENNHAP");
            lblTongTienNhap.Text = tongTienNhap.ToString("n0");
            //tính tổng tiền giảm giá trên đơn hàng
            string sql = "SELECT SUM(TIENGIAMGIA) FROM TDONHANG WHERE LOAI = 0 AND DATHANHTOAN = 30 AND NGAY BETWEEN @FromDate AND @ToDate";
            if (lueCuaHang.StringValue.Length > 0)
            {
                if (!DbConfig.IsAdmin)
                {
                    sql += " AND EXISTS(SELECT * FROM TNGUOIDUNGTHEOCUAHANG WHERE TNGUOIDUNGTHEOCUAHANG.DCUAHANGID = TDONHANG.DCUAHANGID AND SUSERID = '" + DbConfig.UserID + "')";
                }
                else
                {
                    sql += " AND DCUAHANGID = '" + lueCuaHang.StringValue + "'";
                }
            }

            FbCommand cmd = Config.Db.GetCommand(sql);
            cmd.Parameters.Add("@FromDate", FbDbType.Date).Value = FilterDateRange1.TuNgay;
            cmd.Parameters.Add("@ToDate", FbDbType.Date).Value = FilterDateRange1.DenNgay;
            tongGiamGia = Config.Db.GetFirstFieldDec(cmd);
            lblGiamGia.Text = tongGiamGia.ToString("n0");
            tongLai = tongTienBan - tongTienNhap;
            lblTongLai.Text = tongLai.ToString("n0");
            thucLai = tongLai - tongGiamGia;
            lblThucLai.Text = thucLai.ToString("n0");

            if (!xemGiaNhap)
            {
                HideColumn("DMATHANG_GIAVON");
                HideColumn("THANHTIENNHAP");
                HideColumn("THANHTIEN");
                HideColumn("LAI");
                HideColumn("TILELAI");
                pnlBottom.Visible = false;
                btnIn.Visible = false;
            }
        }

		public void btnIn_Click(Object sender, EventArgs e)
		{
            Config.PrintInvoice(null, Forms.ThongKeMatHangBan, "", true, true, new CustomReportHandler(delegate(DataSet ds, Dictionary<string, object> dic)
            {
                grDetail.CopyToDataSet(ds);
                dic.Add("TuNgay", FilterDateRange1.TuNgay);
                dic.Add("DenNgay", FilterDateRange1.DenNgay);

                dic.Add("TongTienBan", tongTienBan);
                dic.Add("TongTienNhap", tongTienNhap);
                dic.Add("TongGiamGia", tongGiamGia);
                dic.Add("TongLai", tongLai);
                dic.Add("ThucLai", thucLai);
            }));
		}

		public void FilterDateRange1_OnEditValueChanged(Object sender, Object value)
		{
            LoadData();
		}
         
		public void grDetail_CustomLoadData(Object sender, CustomLoadDataArgs e)
		{
            e.Command.Parameters.Add("@FromDate", FbDbType.Date).Value = FilterDateRange1.TuNgay;
            e.Command.Parameters.Add("@ToDate", FbDbType.Date).Value = FilterDateRange1.DenNgay;
            e.Select += ", DNHOMMATHANGID, DMATHANGID, (SELECT NAME FROM DNHOMMATHANG WHERE ID = DNHOMMATHANGID) AS NHOMHANG";
            e.GroupBy += ", DNHOMMATHANGID, DMATHANGID";

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

            if (SystemConfig.SapXepThuTuTheo == 0)
            {
                e.OrderBy = "DMATHANG.CODE";
            }
            else
            {
                e.OrderBy = "DMATHANG.NAME";
            }

            if (e.Where.Length > 0) e.Where += " AND ";
            e.Where += String.Format("COALESCE(LOAIDINHLUONG, 0) <> {0}", (int)LoaiDinhLuong.VatTuNguyenLieu);

            if (e.Where.Length > 0) e.Where += " AND ";
            e.Where += "COALESCE(XUATVATTU, 0) = 0";
		}

		public void No1UserControl1_KeyDownEx(Object sender, KeyEventArgs e)
		{
            if (e.KeyCode == Keys.F5) btnRefresh.PerformClick();
		}

        bool xemGiaNhap;
		public void No1UserControl1_Load(Object sender, EventArgs e)
		{
            xemGiaNhap = DbUtils.CanView(Functions.XemGiaNhap);

            FilterDateRange1.LockEvent = true;
            DateTime date = Config.Db.DbDate;
            FilterDateRange1.FromDate = date;
            FilterDateRange1.ToDate = date;
            FilterDateRange1.LockEvent = false;
            grDetail.GridView.OnCustomFilter += new MiscDataGridView.OnCustomFilterHandler(GridView_OnCustomFilter);
            grDetail.GridView.CellDoubleClick += new DataGridViewCellEventHandler(GridView_CellDoubleClick);
            lueCuaHang.LoadData(Tables.DCUAHANG);
            LoadData();
		}

        private void HideColumn(string colName)
        {
            DataGridViewColumn c = grDetail.GridView.Columns[colName];
            if (c != null) c.Visible = false;
        }

        void GridView_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            //hien thi chi tiet
            if (e.RowIndex >= 0 && e.ColumnIndex >= 0)
            {
                ChiTietBanHangTheoMatHang form = (ChiTietBanHangTheoMatHang)Config.CreateForm(Forms.ChiTietBanHangTheoMatHang);
                DataRow row = grDetail.GridView.SelectedRow;
                form.SetData(FilterDateRange1.TuNgay, FilterDateRange1.DenNgay, row["DMATHANGID"].ToString(), ConvertTo.Decimal(row["DONGIA"]), ConvertTo.Decimal(row["DMATHANG_GIAVON"]), ConvertTo.Decimal(row["TILEGIAMGIA"]));
                form.form.ShowDialog();
            }
        }

        void GridView_OnCustomFilter(ref string filter)
        {
            if (tvNhom.SelectedID.Length > 0) 
            {
                if (filter.Length > 0) filter += " AND ";
                filter += "DNHOMMATHANGID = '" + tvNhom.SelectedID + "'";
            }
        }


		public void tvNhom_OnFocusedNodeChanged(TreeNode node, DataRow r, String ID, TreeItemType type)
		{
            grDetail.GridView.Filter = "";
		}


		public void tvNhom_OnCustomNode()
		{
            tvNhom.AddAllNode();
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
