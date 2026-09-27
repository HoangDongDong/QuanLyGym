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
    public partial class TinhLaiGiaVon
    {        
		public void No1Form1_Load(Object sender, EventArgs e)
		{
            dtNgay.ToDate = Config.Db.DbDate;
            dtNgay.FromDate = Config.Db.DbDate.AddMonths(-1);            
		}

        private FbCommand cmd;
		public void rdBinhQuanTheoGiaiDoan_CheckedChanged(Object sender, EventArgs e)
		{
			 dtNgay.Enabled = rdBinhQuanTheoGiaiDoan.Checked;
		}

        private void Prepare()
        {
            cmd = Config.Db.GetCommand("");
            string sql = "";
            if (rdGiaNhapGanNhat.Checked)
            {
                sql = "UPDATE DMATHANG SET GIAVON = (SELECT FIRST 1 CASE WHEN DONGIA IS NULL THEN DMATHANG.GIAVON ELSE DONGIA END FROM TDONHANGCHITIET INNER JOIN TDONHANG ON TDONHANG.ID = TDONHANGCHITIET.TDONHANGID " + Environment.NewLine +
                        "WHERE DMATHANGID = DMATHANG.ID AND (LOAI = 1 OR LOAI = 97)" + Environment.NewLine +
                        "ORDER BY NGAY DESC)";
            }
            else if (rdBinhQuanCuoiKy.Checked)
            {
                sql = "UPDATE DMATHANG SET GIAVON = (" + Environment.NewLine +
                      "SELECT FIRST 1 CASE WHEN SUM(COALESCE(SLNHAP, 0)) = 0 THEN DMATHANG.GIAVON ELSE SUM(COALESCE(CAST(SLNHAPCHUAQUYDOI * DONGIA * (1 - COALESCE(TDONHANG.TILEGIAMGIA, 0) / 100) AS DECIMAL(18, 2)) * (1 - COALESCE(TDONHANGCHITIET.TILEGIAMGIA, 0) / 100), 0)) / SUM(COALESCE(SLNHAP, 0)) END FROM TDONHANGCHITIET INNER JOIN TDONHANG ON TDONHANG.ID = TDONHANGCHITIET.TDONHANGID" + Environment.NewLine +
                      "WHERE (LOAI = 1 OR LOAI = 97) AND DMATHANGID = DMATHANG.ID)";
            }
            else
            {
                sql = "UPDATE DMATHANG SET GIAVON = (" + Environment.NewLine +
                      "SELECT FIRST 1 CASE WHEN SUM(COALESCE(SLNHAP, 0)) = 0 THEN DMATHANG.GIAVON ELSE SUM(COALESCE(SLNHAPCHUAQUYDOI, 0) * DONGIA) / SUM(COALESCE(SLNHAP, 0)) END FROM TDONHANGCHITIET INNER JOIN TDONHANG ON TDONHANG.ID = TDONHANGCHITIET.TDONHANGID" + Environment.NewLine +
                      "WHERE (LOAI = 1 OR LOAI = 97) AND DMATHANGID = DMATHANG.ID";
                if (dtNgay.FromDate != null)
                {
                    sql += " AND NGAY >= @FromDate";
                    cmd.Parameters.Add("@FromDate", FbDbType.Date).Value = dtNgay.FromDate;
                }
                if (dtNgay.ToDate != null)
                {
                    sql += " AND NGAY <= @ToDate";
                    cmd.Parameters.Add("@ToDate", FbDbType.Date).Value = dtNgay.ToDate;
                }
                sql += ")";
            }
            cmd.CommandText = sql;
        }

		public void btnOK_Click(Object sender, EventArgs e)
		{
            btnOK.Enabled = false;
            btnCancel.Enabled = false;
            Prepare();
            prMain.Visible = true;
            BackgroundWorker bw = new BackgroundWorker();
            bw.DoWork += new DoWorkEventHandler(bw_DoWork);
            bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bw_RunWorkerCompleted);
            bw.RunWorkerAsync();
		}

        void bw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            prMain.Visible = false;
            btnCancel.Enabled = true;
            btnOK.Enabled = true;
            Msg.ShowInfo("Chương trình đã thực hiện tính toán xong");
            No1Form1.Close();
        }

        void bw_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                ThucHien();
            }
            catch (Exception ex)
            {
                Msg.ShowWarning("Đã có lỗi xảy ra trong quá trình tính giá vốn" + Environment.NewLine + ex.Message);
            }
        }

        private void ThucHien()
        {
            Config.Db.ExecSql(cmd);
        }


		public void No1Form1_FormClosing(Object sender, FormClosingEventArgs e)
		{
            e.Cancel = prMain.Visible;
		}


		public void No1Form1_OnAutoTest(Object sender, EventArgs e)
		{
            Prepare();
            ThucHien();
            rdBinhQuanTheoGiaiDoan.Checked = true;
            Prepare();
            ThucHien();
            rdGiaNhapGanNhat.Checked = true;
            Prepare();
            ThucHien();
            SystemTesting.SetLog("OK", false);
            btnCancel.PerformClick();
		}
    }
}
