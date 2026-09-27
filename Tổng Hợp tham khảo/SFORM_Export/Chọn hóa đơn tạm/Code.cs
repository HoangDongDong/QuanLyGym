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
    public partial class ChonHoaDonTam
    {
		public void grDetail_CustomLoadData(Object sender, CustomLoadDataArgs e)
		{
            if (grMaster.GridView.SelectedRows.Count == 0)
            {
                e.Where = "0 = 1";
            }
            else
            {
                e.Where = "TLUUTAMID = '" + SelectedID + "'";
            }
		}

        public string SelectedID
        {
            get { return grMaster.GridView.SelectedRow["ID"].ToString(); }
        }

		public void form_Load(Object sender, EventArgs e)
		{
            grMaster.GridView.SelectionChanged += new EventHandler(grMain_SelectionChanged);
            grMaster.GridView.CellMouseDoubleClick += new DataGridViewCellMouseEventHandler(grMain_CellMouseDoubleClick);
            grMaster.LoadData();
            txtLoc.Select(); 
		}

        void grMain_CellMouseDoubleClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex >= 0) btnOK.PerformClick();
        }

        void grMain_SelectionChanged(object sender, EventArgs e)
        {
            grDetail.LoadData();
            btnOK.Enabled = grMaster.GridView.SelectedRows.Count > 0;
        }


		public void txtLoc_TextChanged(object sender, EventArgs e)
		{
			grMaster.GridView.Filter = txtLoc.Text;
		}
    }
}
