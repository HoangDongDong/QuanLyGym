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
    public partial class MatHangKhuyenMai
    {        
		public void chkKhongHienThi_CheckedChanged(Object sender, EventArgs e)
		{
            if (DbUtils.CanLogin(Functions.CauHinhToanHeThong))
            {
                SystemConfig.HienThiMatHangKhuyenMai = chkKhongHienThi.Checked ? 0 : 30;
            }
		}
    }
}
