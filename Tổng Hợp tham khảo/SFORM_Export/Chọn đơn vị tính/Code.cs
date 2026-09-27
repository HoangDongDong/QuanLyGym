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
    public partial class ChonDonViTinh
    {


        DMATHANGRow row;
        public void SetData(DMATHANGRow row)
        {
            this.row = row;
            lstData.Items.Add(new DDONVITINHRow(row.DDONVITINHID).NAME);
            lstData.Items.Add(new DDONVITINHRow(row.DDONVITINHCHANID).NAME);
            lstData.SelectedIndex = 0;
            lstData.Select();
        }

        public string SelectedID
        {
            get 
            {
                if (lstData.SelectedIndex == 0) return row.DDONVITINHID;
                return row.DDONVITINHCHANID;
            }
        }
        
		public void lstData_MouseDoubleClick(Object sender, MouseEventArgs e)
		{
			btnOK.PerformClick();
		}


		public void lstData_SelectedIndexChanged(Object sender, EventArgs e)
		{
			 btnOK.Enabled = lstData.SelectedIndex >= 0;
		}
    }
}
