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
    public partial class DangKyVanTay
    {
        AxZKFPEngXControl.AxZKFPEngX zkPrinter;
		public void btnThuLai_Click(object sender, EventArgs e)
		{
            btnThuLai.Visible = false;

            zkPrinter.CancelEnroll();
            zkPrinter.EnrollCount = 3;
            zkPrinter.BeginEnroll(); 
            ShowHintInfo("Vui lòng đưa tay vào máy");
		}


		public void No1Form1_FormClosing(object sender, FormClosingEventArgs e)
		{
			zkPrinter.EndEngine();
		}
        
        private void ShowHintImage(int iType)
        {
            if (iType == 0)
            {
                imgNO.Visible = false;
                imgOK.Visible = false;
            }
            else if (iType == 1)
            {
                imgNO.Visible = false;
                imgOK.Visible = true;
            }
            else if (iType == 2)
            {
                imgNO.Visible = true;
                imgOK.Visible = false;
            }
            this.No1Form1.Refresh();
        }
        
        public string TemplateStr = "";
        
        private void ShowHintInfo(string message)
        {
            label1.Text = message;
        }


		public void No1Form1_Load(object sender, EventArgs e)
		{
            zkPrinter = new AxZKFPEngXControl.AxZKFPEngX();
            zkPrinter.CreateControl();
            zkPrinter.OnImageReceived += new AxZKFPEngXControl.IZKFPEngXEvents_OnImageReceivedEventHandler(this.axzkPrinter_OnImageReceived);
            zkPrinter.OnFeatureInfo += new AxZKFPEngXControl.IZKFPEngXEvents_OnFeatureInfoEventHandler(this.zkPrinter_OnFeatureInfo);
            zkPrinter.OnEnroll += new AxZKFPEngXControl.IZKFPEngXEvents_OnEnrollEventHandler(this.axzkPrinter_OnEnroll);

            if (zkPrinter.InitEngine() == 0)
            {
                zkPrinter.FPEngineVersion = "10";
                zkPrinter.CancelEnroll();
                zkPrinter.EnrollCount = 3;
                zkPrinter.BeginEnroll();
                ShowHintInfo("Vui lòng đưa tay vào máy");
            }
            else
            {
                ShowHintInfo("Không thể kết nối tới máy vân tay");
            }
		}
        
        private void axzkPrinter_OnImageReceived(object sender, AxZKFPEngXControl.IZKFPEngXEvents_OnImageReceivedEvent e)
        {
            ShowHintImage(0);
            Graphics g = ptImage.CreateGraphics();
            Bitmap bmp = new Bitmap(ptImage.Width, ptImage.Height);
            g = Graphics.FromImage(bmp);
            int dc = g.GetHdc().ToInt32();
            zkPrinter.PrintImageAt(dc, 0, 0, bmp.Width, bmp.Height);
            g.Dispose();
            ptImage.Image = bmp;
        }
        
        private void axzkPrinter_OnEnroll(object sender, AxZKFPEngXControl.IZKFPEngXEvents_OnEnrollEvent e)
        {
            if (e.actionResult)
            {
                btnOK.Enabled = true;
                TemplateStr = zkPrinter.GetTemplateAsStringEx("10");                
                //zkPrinter.AddRegTemplateStrToFPCacheDBEx(fpcHandle, 1, zkPrinter.GetTemplateAsStringEx("9"), zkPrinter.GetTemplateAsStringEx("10"));
                //MessageBox.Show(zkPrinter.GetTemplateAsStringEx("9").Length.ToString() + Environment.NewLine +
                //                zkPrinter.GetTemplateAsStringEx("10").Length.ToString());
                ShowHintInfo("Lấy vân tay thành công!");
            }
            else
            {
                ShowHintInfo("Không thể lấy vân tay");
                btnThuLai.Visible = true;
            }
        }
        
        private void zkPrinter_OnFeatureInfo(object sender, AxZKFPEngXControl.IZKFPEngXEvents_OnFeatureInfoEvent e)
        {
            if (zkPrinter.EnrollIndex != 1)
            {
                if (zkPrinter.IsRegister)
                {
                    if (zkPrinter.EnrollIndex - 1 > 0)
                    {
                        ShowHintInfo("Vui lòng nhấn vân tay thêm " + Convert.ToString(zkPrinter.EnrollIndex - 1) + " lần!");
                    }
                }
            }
        }
    }
}
