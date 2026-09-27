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
    public partial class ChupWebcam
    {
        private WebCam webcam;
        public void Load()
        {
            this.webcam = new WebCam();
            this.webcam.InitializeWebCam(ref imgVideo);
        }

		public void bntSave_Click(object sender, EventArgs e)
		{
            if (this.Image == null)
            {
                Msg.ShowWarning("Chưa c\x00f3 ảnh để thực hiện");
            }
            else
            {                
                No1Form1.DialogResult = DialogResult.OK;
            }
		}


		public void No1Form1_FormClosing(object sender, FormClosingEventArgs e)
		{
			this.webcam.Stop();
		}


		public void bntVideoFormat_Click(object sender, EventArgs e)
		{
			this.webcam.ResolutionSetting();
		}


		public void bntVideoSource_Click(object sender, EventArgs e)
		{
			this.webcam.AdvanceSetting();
		}


		public void bntStart_Click(object sender, EventArgs e)
		{
			this.webcam.Start();
		}


		public void bntStop_Click(object sender, EventArgs e)
		{
			this.webcam.Stop();
		}


		public void bntContinue_Click(object sender, EventArgs e)
		{
			this.webcam.Continue();
		}


		public void bntCapture_Click(object sender, EventArgs e)
		{
			this.imgCapture.Image = this.imgVideo.Image;
		}
        
        public System.Drawing.Image Image
        {
            get
            {
                return this.imgCapture.Image;
            }
        }
    }
}
