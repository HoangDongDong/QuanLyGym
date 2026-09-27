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
    public partial class NhapSoLuong
    {
        public void SetMatHang(string DMATHANGID, string DKHOID, decimal donGia, decimal soLuong)
        {
            DMATHANGRow mhRow = new DMATHANGRow(Config.Db.GetFirstRow("SELECT CODE, NAME, COHANSUDUNG, DDONVITINHCHANID, DDONVITINHID, (SELECT NAME FROM DDONVITINH WHERE ID = DDONVITINHID) AS DVTLE, (SELECT NAME FROM DDONVITINH WHERE ID = DDONVITINHCHANID) AS DVTCHAN FROM DMATHANG WHERE ID = '" + DMATHANGID + "'"));
            lblDVTLe.Text = mhRow["DVTLE"].ToString();
            spSoLuong.Value = soLuong;
            numCK.Text = "";
            spSLChan.Value = 0;
            spSLChan.Text = "";
            if (TonKhoHandler.Has2DonViTinh(mhRow))
            {                
                lblDVTChan.Text = mhRow["DVTCHAN"].ToString();                
            }
            else
            {                
                lblDVTChan.Visible = false;
                spSLChan.Visible = false;
            }
            lblDonGia.Text = donGia.ToString("n0");
            lblItem.Text = mhRow.CODE + ((mhRow.CODE.Length > 0) ? " - " : "") + mhRow.NAME;

            spSoLuong.Select();
            spSoLuong.Select(0, spSoLuong.Text.Length);

            bool coGrid = false;
            if (DKHOID.Length > 0)
            {                
                if (SystemConfig.MatHangCoKichThuoc == 30)
                {
                    //tải kích thước và số tồn                    
                    string sql = @"SELECT SUM(COALESCE(SLNHAP, 0) - COALESCE(SLXUAT, 0)) AS TON, KICHTHUOC FROM TDONHANG INNER JOIN TDONHANGCHITIET ON TDONHANG.ID = TDONHANGCHITIET.TDONHANGID
WHERE DMATHANGID = @DMATHANGID AND ((LOAI = 0 AND DATHANHTOAN = 30) OR LOAI <> 0) AND DKHOHANGID = @DKHOHANGID
GROUP BY KICHTHUOC
HAVING SUM(COALESCE(SLNHAP, 0) - COALESCE(SLXUAT, 0)) <> 0";
                    FbCommand cmd = Config.Db.GetCommand(sql);
                    cmd.Parameters.Add("@DMATHANGID", FbDbType.VarChar).Value = DMATHANGID;
                    cmd.Parameters.Add("@DKHOHANGID", FbDbType.VarChar).Value = DKHOID;
                    grKichThuoc.LoadDataSearchable(Config.Db.GetTable(cmd));
                    coGrid = true;
                    lblKichThuoc.Visible = true;
                    txtKichThuoc.Visible = true;
                    grKichThuoc.Visible = true;               
                }

                if (SystemConfig.MatHangCoHanSuDung == 30 && mhRow.COHANSUDUNG == 30)
                {
                    string sql = @"SELECT SUM(SLNHAP - SLXUAT) AS TON, HANSUDUNG, 0 AS SOLUONG
FROM TDONHANG INNER JOIN TDONHANGCHITIET ON TDONHANG.ID = TDONHANGCHITIET.TDONHANGID
WHERE DMATHANGID = @DMATHANGID
AND DKHOHANGID = @DKHOHANGID
GROUP BY HANSUDUNG
HAVING SUM(SLNHAP - SLXUAT) <> 0
ORDER BY HANSUDUNG";
                    FbCommand cmd = Config.Db.GetCommand(sql);
                    cmd.Parameters.Add("@DMATHANGID", FbDbType.VarChar).Value = DMATHANGID;
                    cmd.Parameters.Add("@DKHOHANGID", FbDbType.VarChar).Value = DKHOID;
                    grHanSuDung.Visible = true;
                    grHanSuDung.DataSource = Config.Db.GetTable(cmd);
                    coGrid = true;                
                }
            }
            //xử lý trường hợp có size hoặc không có size
            
            if (!coGrid)
            {                
                No1Form1.Height = No1Form1.Height - grKichThuoc.Height;
            }
        }

        public object HanSuDung;

        public string KichThuoc = "";
		public void btnOK_Click(object sender, EventArgs e)
		{
            //kiểm tra xem có kích thước không?
            if (grKichThuoc.Visible)
            {
                if (grKichThuoc.SelectedRows.Count == 0)
                {
                    Msg.ShowWarning("Mời bạn chọn kích thước");
                    return;
                }

                KichThuoc = grKichThuoc.SelectedRow["KICHTHUOC"].ToString();
            }

            if (grHanSuDung.Visible)
            {
                if (grHanSuDung.SelectedRows.Count == 0)
                {
                    Msg.ShowWarning("Mời bạn chọn hạn sử dụng");
                    return;
                }

                object value = grHanSuDung.SelectedRow["HANSUDUNG"];
                if (value != DBNull.Value)
                    HanSuDung = value;
            }

            No1Form1.DialogResult = DialogResult.OK;
		}


		public void grHanSuDung_CellMouseDoubleClick(object sender, DataGridViewCellMouseEventArgs e)
		{
            if (e.RowIndex >= 0 && e.ColumnIndex >= 0)
            {
                btnOK.PerformClick();
            }
		}


		public void grKichThuoc_CellMouseDoubleClick(object sender, DataGridViewCellMouseEventArgs e)
		{
            if (e.RowIndex >= 0 && e.ColumnIndex >= 0)
            {
                btnOK.PerformClick();
            }
		}
    }
}
