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
    public partial class TuyChonKhac
    {


		public void btnOK_Click(object sender, EventArgs e)
		{
            IniFile.WriteString(Application.StartupPath + "\\App.dat", "OTHER", "Update", chkKhongCapNhat.Checked ? "30" : "0");
            this.No1Form1.Close();
		}


		public void No1Form1_Load(object sender, EventArgs e)
		{
            chkKhongCapNhat.Checked = IniFile.GetString(Application.StartupPath + "\\App.dat", "OTHER", "Update", "") == "30";            
		}
    }
}
