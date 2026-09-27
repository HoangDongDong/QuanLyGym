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
    public partial class DangThucHien
    {
        private object Data;
        public void PostData(object data)
        {
            this.Data = data;
        }

        private DateTime Start;
        private Timer Timer1;
        BackgroundWorker bw;
        private string title;
		public void No1Form1_Shown(object sender, EventArgs e)
		{
            this.title = No1Form1.Text;
            if (bw == null)
            {
                bw = new BackgroundWorker();
                bw.DoWork += new DoWorkEventHandler(bw_DoWork);
                bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bw_RunWorkerCompleted);                
                bw.RunWorkerAsync(Data);
            }

            Start = DateTime.Now;
            Timer1 = new Timer();
            Timer1.Interval = 1000;            
            Timer1.Tick +=new EventHandler(Timer1_Tick);
            Timer1.Enabled = true;
            this.No1Form1.FormClosing += new FormClosingEventHandler(No1Form1_FormClosing);
		}

        void No1Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            Timer1.Enabled = false;
            Timer1.Dispose();
        }

        public event DoWorkEventHandler DoWork;
        public event RunWorkerCompletedEventHandler RunWorkerCompleted;

        void bw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (RunWorkerCompleted != null) RunWorkerCompleted(this, e);
            if (e.Error != null)
                Msg.ShowWarning("Có lỗi xảy ra trong quá trình thực hiện: " + e.Error.Message + Environment.NewLine + e.Error.StackTrace);
            No1Form1.Close();
        }

        void bw_DoWork(object sender, DoWorkEventArgs e)
        {
            if (DoWork != null) DoWork(this, e);
        }

        public bool NeedUpdateTimer = false;
        public void UpdateStatus(string detail, int tile)
        {
            if (No1Form1.InvokeRequired)
            {
                No1Form1.Invoke(new UpdateStatusHandler(UpdateStatus), detail, tile);
            }
            else
            {
                lblChiTiet.Text = detail;
                prMain.Value = tile;
                if (tile > 0)
                    No1Form1.Text = title + " " + tile.ToString() + "%";
                else
                    No1Form1.Text = title;

                if (NeedUpdateTimer)
                {
                    TimeSpan ts = DateTime.Now - Start;
                    totalRunTime = (int)ts.TotalSeconds;

                    UpdateTimer();
                }
            }
        }

        private delegate void UpdateStatusHandler(string detail, int tiLe);    

      
        int totalRunTime = 0;
		public void Timer1_Tick(object sender, EventArgs e)
		{
            totalRunTime++;
            UpdateTimer();
		}

        public void UpdateTimer()
        {
            int giay = totalRunTime % 60;
            int phut = (totalRunTime - giay) / 60;
            lblThoiGian.Text = "Thời gian: " + (phut > 0 ? (phut.ToString() + " phút ") : "") + giay.ToString() + " giây";
        }
    }
}
