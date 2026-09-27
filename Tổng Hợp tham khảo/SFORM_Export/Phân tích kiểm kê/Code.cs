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
    public partial class PhanTichKiemKe
    {
		public void tvNhom_OnFocusedNodeChanged(TreeNode node, DataRow r, String ID, TreeItemType type)
		{
            grMatHang.GridView.Filter = "";
		}


		public void grMatHang_CustomLoadData(Object sender, CustomLoadDataArgs e)
		{
            e.Select += ", DNHOMMATHANGID";
            e.Where = "NOT EXISTS(SELECT * FROM TDONHANGCHITIET WHERE TDONHANGID = '" + TKIEMKEID + "' AND DMATHANGID = DMATHANG.ID)";
		}

        private string TKIEMKEID;
        internal void SetData(string TKIEMKEID)
        {
            this.TKIEMKEID = TKIEMKEID;
            grMatHang.GridView.OnCustomFilter += new MiscDataGridView.OnCustomFilterHandler(GridView_OnCustomFilter);
            grMatHang.LoadData();
        }

        void GridView_OnCustomFilter(ref string filter)
        {
            if (tvNhom.SelectedID.Length == 0) return;
            if (filter.Length > 0) filter += " AND ";
            filter += "DNHOMMATHANGID = '" + tvNhom.SelectedID + "'";
        }
    }
}
