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
    public partial class ThongKe
    {       
		public void form_Load(Object sender, EventArgs e)
		{
            lblTaiKhoan.Text = lblTaiKhoan.Text + " " + DbConfig.UserName;
            dtNgay.DateTime = Config.Db.DbDate;
            grHoaDon.GridView.SelectionChanged += new EventHandler(GridView_SelectionChanged);
            grHoaDon.GridView.CellMouseDoubleClick += new DataGridViewCellMouseEventHandler(GridView_CellMouseDoubleClick);

            LoadData(dtNgay.DateTime);
		}

        void GridView_CellMouseDoubleClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex >= 0)
            {
                btnOpen.PerformClick();
            }
        }

        void GridView_SelectionChanged(object sender, EventArgs e)
        {
            btnOpen.Enabled = grHoaDon.GridView.SelectedRows.Count > 0;
        }

        private DateTime ngayLoc;
		public void dtNgay_OnEditValueChanged(Object sender, Object value)
		{
			 LoadData(dtNgay.DateTime);
		}

        public string SelectedID
        {
            get { return grHoaDon.SelectedID; }
        }

        private void LoadData(DateTime Ngay)
        {
            lblNgay.Text = lblNgay.Text + " " + Ngay.ToString("dd/MM/yyyy");
            ngayLoc = Ngay;
            //tính tổng doanh thu trên account
            grHoaDon.LoadData();
            grMatHang.LoadData();

            decimal tongDoanhSo = grHoaDon.CalcSum("TONGCONG");
            decimal tienMat = grHoaDon.CalcSum("TIENMAT", "");
            decimal chuyenKhoan = grHoaDon.CalcSum("CHUYENKHOAN", "");
            decimal tienThe = grHoaDon.CalcSum("THE", "");
            decimal voucher = grHoaDon.CalcSum("VOUCHER", "");
            decimal theTraTruoc = grHoaDon.CalcSum("THETRATRUOC", "");
            decimal truTichLuy = grHoaDon.CalcSum("TRUTICHLUY", "");
            
            decimal thanhToan = grHoaDon.CalcSum("TIENTHANHTOAN", "");
            decimal tienNo = tongDoanhSo - thanhToan;

            lblGTConNo.Text = tienNo.ToString("n0");
            lblGTTienMat.Text = tienMat.ToString("n0");
            lblGTChuyenKhoan.Text = chuyenKhoan.ToString("n0");
            lblTheATM.Text = tienThe.ToString("n0");

            lblVoucher.Text = voucher.ToString("n0");            
            lblTheTraTruoc.Text = theTraTruoc.ToString("n0");
            lblTruTichLuy.Text = truTichLuy.ToString("n0");

            lblGTTong.Text = tongDoanhSo.ToString("n0");
        }


		public void grHoaDon_CustomLoadData(Object sender, CustomLoadDataArgs e)
		{
            e.Where += " AND TDONHANG.NGAY = @NGAY AND USERTHANHTOANID = '" + DbConfig.UserID + "'";
            e.Command.Parameters.Add("@NGAY", FbDbType.Date).Value = ngayLoc;
		}


		public void grMatHang_CustomLoadData(Object sender, CustomLoadDataArgs e)
		{
            e.Where += " AND TDONHANG.NGAY = @NGAY AND USERTHANHTOANID = '" + DbConfig.UserID + "'";
            e.Command.Parameters.Add("@NGAY", FbDbType.Date).Value = ngayLoc;
		}


		public void grHoaDon_grMain_SelectionChanged(Object sender, EventArgs e)
		{
            btnOpen.Enabled = grHoaDon.GridView.SelectedRows.Count > 0;
		}


		public void btnInBaoCao_Click(Object sender, EventArgs e)
		{
            Config.PrintInvoice(null, Forms.ThongKe, "", true, true, new CustomReportHandler(delegate(DataSet ds, Dictionary<string, object> dic)
            {
                grHoaDon.CopyToDataSet(ds);
                dic.Add("Ngày", ngayLoc);
            }));
		}
    }
}
