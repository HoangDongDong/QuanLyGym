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
    public partial class LuuVetHoatDong:ITestingSupport
    {
		public void btnRefresh_Click(Object sender, EventArgs e)
		{
            LoadData();
		}

        private void LoadData()
        {
            grMain.LoadData();
        }


		public void No1UserControl1_Load(Object sender, EventArgs e)
		{
            dtNgay.DateTime = Config.Db.DbDate;
            lueNhanVien.LoadData(Tables.SUSER);
            LoadData();
		}


		public void grMain_CustomLoadData(Object sender, CustomLoadDataArgs e)
		{
            if (dtNgay.EditValue != null)
            {
                if (e.Where.Length > 0) e.Where += " AND ";
                e.Where += "NGAY = @NGAY";
                e.Command.Parameters.Add("@NGAY", FbDbType.Date).Value = dtNgay.DateTime;
            }
            if (lueNhanVien.StringValue.Length > 0)
            {
                if (e.Where.Length > 0) e.Where += " AND ";
                e.Where += "TAIKHOAN = @TAIKHOAN";
                e.Command.Parameters.Add("@TAIKHOAN", FbDbType.VarChar).Value = lueNhanVien.StringValue;
            }
            if (txtSoPhieu.Text.Length > 0)
            {
                if (e.Where.Length > 0) e.Where += " AND ";
                e.Where += "SODONHANG = (SELECT ID FROM TDONHANG WHERE LOAI = 0 AND NAME = @SODONHANG)";
                e.Command.Parameters.Add("@SODONHANG", FbDbType.VarChar).Value = txtSoPhieu.Text;
            }            
            if (txtNoiDung.Text.Length > 0)
            {
                if (e.Where.Length > 0) e.Where += " AND ";
                e.Where += "TAIKHOAN LIKE '%" + txtNoiDung.Text.Replace("'", "''") + "%'";                
            }
            e.Select = e.Select.Replace("TLUUVET.SODONHANG", "(SELECT NAME FROM TDONHANG WHERE ID = TLUUVET.SODONHANG)");
            e.OrderBy = "NGAY, GIO";
		}


		public void btnXoaLuuVet_Click(Object sender, EventArgs e)
		{
            if (Msg.ShowYesNo("Bạn có muốn xóa hết các lưu vết không?") == DialogResult.Yes)
            {
                ThucHienXoaLuuVet();
            }
		}

        private void ThucHienXoaLuuVet()
        {
            Config.Db.ExecSql("DELETE FROM TLUUVET");
            LoadData();
        }

        #region ITestingSupport Members

        public void DoAutoTest()
        {
            dtNgay.InputRandomData();
            lueNhanVien.InputRandomData();
            txtNoiDung.InputRandomData();
            LoadData();
            ThucHienXoaLuuVet();           
            SystemTesting.SetLog("OK", false);
            UiUtils.CloseActiveTab();
        }

        #endregion
    }
}
