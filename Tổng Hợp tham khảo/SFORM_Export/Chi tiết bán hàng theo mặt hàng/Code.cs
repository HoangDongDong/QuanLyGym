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
    public partial class ChiTietBanHangTheoMatHang
    {
		public void grMain_CustomLoadData(Object sender, CustomLoadDataArgs e)
		{
            e.Where += " AND EXISTS(SELECT * FROM TDONHANGCHITIET WHERE TDONHANGID = TDONHANG.ID AND TILEGIAMGIA = @TILEGIAMGIA AND DONGIA = @DONGIA AND DMATHANGID = '" + DMATHANGID + "')";
            e.Command.Parameters.Add("@FromDate", FbDbType.TimeStamp).Value = tuNgay;
            e.Command.Parameters.Add("@ToDate", FbDbType.TimeStamp).Value = denNgay;

            e.Command.Parameters.Add("@TILEGIAMGIA", FbDbType.Decimal).Value = tilegiamgia;
            e.Command.Parameters.Add("@DONGIA", FbDbType.Decimal).Value = dongia;
		}

        string DMATHANGID;
        decimal dongia;
        decimal giavon;
        decimal tilegiamgia;
        DateTime tuNgay;
        DateTime denNgay;
        internal void SetData(DateTime tuNgay, DateTime denNgay, string DMATHANGID, decimal dongia, decimal giavon, decimal tilegiamgia)
        {
            this.tuNgay = tuNgay;
            this.denNgay = denNgay;
            this.DMATHANGID = DMATHANGID;
            this.dongia = dongia;
            this.giavon = giavon;
            this.tilegiamgia = tilegiamgia;
            grMain.GridView.SelectionChanged += new EventHandler(GridView_SelectionChanged);
            grDetail.GridView.CellFormatting += new DataGridViewCellFormattingEventHandler(GridView_CellFormatting);
            grDetail.CustomLoadData += new CustomLoadDataHandler(grDetail_CustomLoadData);
            grMain.LoadData();
        }

        void GridView_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            try
            {
                string DMATHANGID = grDetail.GridView.GetDataRow(e.RowIndex)["DMATHANGID"].ToString();
                if (DMATHANGID == this.DMATHANGID)
                {
                    e.CellStyle.Font = new Font(e.CellStyle.Font, FontStyle.Bold);
                    e.CellStyle.SelectionBackColor = Color.Red;
                    e.CellStyle.ForeColor = Color.Red;
                }
            }
            catch
            {
            }
        }

        void grDetail_CustomLoadData(object sender, CustomLoadDataArgs e)
        {
            e.Where = "TDONHANGID = '" + grMain.SelectedID + "'";
        }

        void GridView_SelectionChanged(object sender, EventArgs e)
        {            
            grDetail.LoadData();
        }
    }
}
