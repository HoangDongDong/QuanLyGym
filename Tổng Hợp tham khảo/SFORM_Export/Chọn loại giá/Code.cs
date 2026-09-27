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
    public partial class ChonLoaiGia
    {
		public void form_Load(Object sender, EventArgs e)
		{
            btnDonGia2.Enabled = SystemConfig.SuDungGia2 == 30;
            if (btnDonGia2.Enabled) btnDonGia2.Text = SystemConfig.DienGiaiGia2;
            btnDonGia3.Enabled = SystemConfig.SuDungGia3 == 30;
            if (btnDonGia3.Enabled) btnDonGia3.Text = SystemConfig.DienGiaiGia3;
            btnDonGia4.Enabled = SystemConfig.SuDungGia4 == 30;
            if (btnDonGia4.Enabled) btnDonGia4.Text = SystemConfig.DienGiaiGia4;

            if (row != null)
            {
                btnDonGia.Text = btnDonGia.Text + ": " + row.GIABAN.ToString("n0");
                btnDonGia2.Text = btnDonGia2.Text + ": " + row.GIABAN2.ToString("n0");
                btnDonGia3.Text = btnDonGia3.Text + ": " + row.GIABAN3.ToString("n0");
                btnDonGia4.Text = btnDonGia4.Text + ": " + row.GIABAN4.ToString("n0");
            }
		}

        DMATHANGRow row;
        public void SetMatHang(DMATHANGRow row)
        {
            this.row = row;
        }

        public int LoaiGia;

		public void btnDonGia_Click(Object sender, EventArgs e)
		{
            LoaiGia = ConvertTo.Int((sender as Button).Tag);
            form.DialogResult = DialogResult.OK;
		}


		public void form_OnAutoTest(Object sender, EventArgs e)
		{
			//select random gia
            List<Button> lst = new List<Button>();
            if (btnDonGia.Enabled) lst.Add(btnDonGia);
            if (btnDonGia2.Enabled) lst.Add(btnDonGia2);
            if (btnDonGia3.Enabled) lst.Add(btnDonGia3);
            if (btnDonGia4.Enabled) lst.Add(btnDonGia4);
            Random rand = new Random();
            lst[rand.Next(0, lst.Count)].PerformClick();
		}
    }
}
