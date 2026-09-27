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
    public partial class TruTichLuy
    {
		public void btnOK_Click(object sender, EventArgs e)
		{
            No1Form1.DialogResult = DialogResult.OK;
		}

        internal void LoadData(string DKHACHHANGID)
        {
            DKHACHHANGRow khRow = new DKHACHHANGRow(DKHACHHANGID);
            lblKhachHang.Text = lblKhachHang.Text + ": " + khRow.NAME;
            //hiển thị số điểm tích lũy
            decimal soDiem = HoaDonBanHang.GetDiemTichLuy(DKHACHHANGID);
            numDiem.Value = soDiem;
            decimal giaTri = soDiem * SystemConfig.QuyDoi1DiemSangTien;
            numGiaTri.Value = giaTri;
        }
    }
}
