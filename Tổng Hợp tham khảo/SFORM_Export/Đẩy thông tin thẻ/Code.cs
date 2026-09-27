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
    public partial class DayThongTinThe
    {
        BackgroundWorker bwUpdate = null;
		public void btnOK_Click(object sender, EventArgs e)
		{
			//kiểm tra xem có nội dung gì không
            if (!chkXoaTatGiaoDichVaoRa.Checked && !chkCapNhatThe.Checked && !chkCapNhatNhom.Checked && !chkXoaTheQuaHan.Checked)
            {
                Msg.ShowWarning("Mời bạn lựa chọn nội dung để cập nhật");
                return;
            }

            if (bwUpdate == null)
                bwUpdate = new BackgroundWorker();
            bwUpdate.DoWork += new DoWorkEventHandler(bwUpdate_DoWork);
            bwUpdate.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bwUpdate_RunWorkerCompleted);
            progressBar.Visible = true;
            btnOK.Enabled = btnHuyBo.Enabled = false;
            bwUpdate.RunWorkerAsync();
		}

        void bwUpdate_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            progressBar.Visible = false;
            btnHuyBo.Enabled = btnOK.Enabled = true;
            if (e.Error != null)
            {
                Msg.ShowError(e.Error.Message);
            }
            else if (msg.Length > 0)
            {
                Msg.ShowWarning(msg.ToString());
            }
            else
            {
                Msg.ShowInfo("Hoàn thành thực hiện");
                No1Form1.DialogResult = DialogResult.OK;
            }
        }

        StringBuilder msg;
        void bwUpdate_DoWork(object sender, DoWorkEventArgs e)
        {
            msg = new StringBuilder();
            //khi bật tùy chọn này, tạo các nhóm tương ứng
            if (chkCapNhatNhom.Checked)
            {
                //kiểm tra xem có bị trùng nhóm không?
                //lưu ý: các thiết bị khóa thẻ bằng cách xóa thẻ thì không cần cập nhật
                List<NhomInfo> lstNhom = new List<NhomInfo>();    
                int BiKhoa = SystemConfig.NhomBiKhoa;
                //cập nhật nhóm bị khóa
                lstNhom.Add(new NhomInfo(BiKhoa, new DateTime(2000, 1, 1, 0, 0, 0),
                                        new DateTime(2000, 1, 1, 0, 0, 0)));
                int KhongBiKhoa = SystemConfig.NhomKhongBiKhoa;
                //cập nhật nhóm không khóa
                lstNhom.Add(new NhomInfo(KhongBiKhoa, new DateTime(2000, 1, 1, 0, 0, 0),
                                        new DateTime(2000, 1, 1, 23, 59, 0)));

                DataTable dtCaTap = Config.Db.GetTable(Tables.DCATAP, "STATUS = 30");                
                foreach (DataRow r in dtCaTap.Rows)
                {                    
                    DCATAPRow ca = new DCATAPRow(r);
                    if (ca.NHOMTRENMAY > 0 && ca.NHOMTRENMAY != BiKhoa && ca.NHOMTRENMAY != KhongBiKhoa)
                    {
                        lstNhom.Add(new NhomInfo(ca.NHOMTRENMAY, ca.TUGIO, ca.DENGIO));
                    }
                    else
                    {
                        msg.AppendLine("Lỗi tạo nhóm trên máy cho ca '" + ca.NAME + "'");
                    }
                }

                //cập nhật nhóm theo ca
                Shared.QuanLyThietBi.CapNhatNhom(lstNhom);
            }

            if (chkCapNhatThe.Checked)
            {
                Shared.QuanLyThietBi.XoaTatCaThe();

                //lấy ra các thẻ có trạng thái đang sử dụng và đưa vào máy
                string sql = String.Format("SELECT MAKHACH, NAME, (SELECT NHOMTRENMAY FROM DCATAP WHERE ID = DCATAPID) AS NHOMTRENMAY, MAVANTAY FROM DKHACHHANG WHERE DTRANGTHAIID = '{0}'", TrangThaiIds.DangSuDung);
                DataTable dtKhachHang = Config.Db.GetTable(sql);
                int NhomKhongBiKhoa = SystemConfig.NhomKhongBiKhoa;
                string msgOk = "";
                string msgError = "";
                foreach (DataRow r in dtKhachHang.Rows)
                {
                    DKHACHHANGRow khRow = new DKHACHHANGRow(r);
                    int maThe = ConvertTo.Int(khRow.MAKHACH);                    
                    int maNhom = ConvertTo.Int(khRow["NHOMTRENMAY"]);
                    if (maNhom == 0)
                    {
                        maNhom = NhomKhongBiKhoa;
                    }
                    Shared.QuanLyThietBi.TaoThe(maThe, khRow.MAVANTAY, maNhom, ref msgOk, ref msgError, khRow.NAME);
                }
            }

            if (chkXoaTatGiaoDichVaoRa.Checked)
            {
                Shared.QuanLyThietBi.XoaLog();
            }

            if (chkXoaTheQuaHan.Checked)
            {
                //lấy ra các thẻ có trạng thái quá hạn
                XoaTheQuaHan();
            }
        }

        private void XoaTheQuaHan()
        {
            string sql = "SELECT ID, MAKHACH FROM DKHACHHANG WHERE STATUS = 0";
            string MaxMaKhach = Config.Db.GetFirstFieldString("SELECT MAX(MAKHACH) FROM DKHACHHANG WHERE MAKHACH LIKE 'XOA%'").Trim();

            int MaKhach = 1;
            if (MaxMaKhach.Length > 3)
            {
                MaKhach = ConvertTo.Int(MaxMaKhach.Substring(3)) + 1;
            }

            DataTable dtQuaHan = Config.Db.GetTable(sql);
            foreach (DataRow r in dtQuaHan.Rows)
            {                                
                DKHACHHANGRow khRow = new DKHACHHANGRow(r);
                string msg = "";
                Shared.QuanLyThietBi.XoaThe(ConvertTo.Int(khRow.MAKHACH), ref msg);

                khRow = new DKHACHHANGRow(khRow.ID);
                khRow.MAKHACH = "XOA" + MaKhach.ToString("0000000");
                khRow.STATUS = 60;
                khRow.Update();

                MaKhach = MaKhach + 1;
            }
        }

		public void No1Form1_OnInit(object sender, EventArgs e)
		{
			//đưa nội dung các nhóm lên ghi chú
            StringBuilder builder = new StringBuilder();            
            builder.AppendLine("Nhóm bị khóa: " + SystemConfig.NhomBiKhoa.ToString());
            builder.AppendLine("Nhóm không bị khóa: " + SystemConfig.NhomKhongBiKhoa.ToString());
            string sql = "SELECT ID, NAME, TUGIO, DENGIO, NHOMTRENMAY FROM DCATAP WHERE STATUS = 30";
            DataTable dt = Config.Db.GetTable(sql);
            foreach (DataRow r in dt.Rows)
            {
                DCATAPRow row = new DCATAPRow(r);
                builder.AppendLine(row.NAME + " (" + row.TUGIO.ToString("HH:mm") + " - " + row.DENGIO.ToString("HH:mm") + "): " + row.NHOMTRENMAY);
            }
            txtNoiDung.Text = builder.ToString();
		}
    }
}
