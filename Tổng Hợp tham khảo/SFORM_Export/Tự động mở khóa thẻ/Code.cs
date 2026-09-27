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
    public partial class TuDongMoKhoaThe
    {
        private DateTime toDay;
        public void LoadData(DateTime toDay)
        {
            this.toDay = toDay;
            No1Form1.Load += new EventHandler(No1Form1_Load);
        }

        BackgroundWorker bw;
        void No1Form1_Load(object sender, EventArgs e)
        {
            bw = new BackgroundWorker();
            bw.DoWork += new DoWorkEventHandler(bw_DoWork);
            bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bw_RunWorkerCompleted);            
            bw.RunWorkerAsync();
        }

        void bw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            SystemConfig.NgayCapNhatTrangThaiGanNhat = toDay;
            No1Form1.Close();
        }

        void bw_DoWork(object sender, DoWorkEventArgs e)
        {
            TuDongKhoaMoThe();
        }

        private void SetStatus(string status, int value)
        {
            lblTrangThai.Invoke(new MethodInvoker(delegate
            {
                lblTrangThai.Text = status;
                prMain.Value = value;
            }));
        }

        /// <summary>
        /// Thực hiện mở khóa thẻ hàng ngày
        /// </summary>
        public void TuDongKhoaMoThe()
        {
            //cập nhật trạng thái khách hàng và lấy danh sách khách hàng thay đổi trạng thái
            string sql = "SELECT * FROM CAPNHATTRANGTHAI('', 30)";
            DataTable dt = Config.Db.GetTable(sql);

            prMain.Invoke(new MethodInvoker(delegate
            {
                prMain.Style = ProgressBarStyle.Blocks;
            }));

            int i = 0;
            foreach (DataRow r in dt.Rows)
            {
                i++;
                int maThe = ConvertTo.Int(r["MATHE"].ToString());
                if (maThe > 0)
                {
                    string msgOk = "";
                    string msgError = "";

                    SetStatus("Mã thẻ: " + maThe.ToString(), (int)(i * 100 / dt.Rows.Count));

                    string DTRANGTHAIMOIID = r["DTRANGTHAIMOIID"].ToString();
                    switch (DTRANGTHAIMOIID)
                    {
                        case TrangThaiIds.BaoLuu:
                        case TrangThaiIds.ChuaKichHoat:
                        case TrangThaiIds.QuaHan:
                        case TrangThaiIds.QuaLanTap:
                            //khóa thẻ lại
                            Shared.QuanLyThietBi.KhoaThe(maThe, ref msgOk, ref msgError);
                            break;
                        case TrangThaiIds.DangSuDung:
                            //mở lại thẻ
                            Shared.QuanLyThietBi.MoThe(maThe, ref msgOk, ref msgError);
                            break;
                    }
                }
            }
        }
    }
}
