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
    public partial class ThemMua1Tang1
    {


		public void tvNhom_OnCustomNode()
		{
            tvNhom.AddAllNode();
		}


		public void form_Load(Object sender, EventArgs e)
		{
            grMua.GridView.SearchTextBox = txtLocMua;
            grTang.GridView.SearchTextBox = txtLocTang;
            grTang.GridView.OnCustomFilter += new MiscDataGridView.OnCustomFilterHandler(GridView_OnCustomFilter);
            grMua.GridView.OnCustomFilter += new MiscDataGridView.OnCustomFilterHandler(GridView_OnCustomFilter);
            btnOk.Click += new EventHandler(btnOk_Click);
            chkTangCung.CheckedChanged += new EventHandler(chkTangCung_CheckedChanged);
            grTang.LoadData();
            grMua.LoadData();
		}

        void chkTangCung_CheckedChanged(object sender, EventArgs e)
        {
            grTang.Enabled = !chkTangCung.Checked;
        }

        void btnOk_Click(object sender, EventArgs e)
        {
            if (grMua.SelectedID.Length == 0)
            {
                Msg.ShowWarning("Mời bạn chọn mặt hàng mua trước");
                return;
            }

            if (!chkTangCung.Checked)
            {
                if (grTang.SelectedID.Length == 0)
                {
                    Msg.ShowWarning("Mời bạn chọn mặt hàng tặng trước");
                    return;
                }
            }
            form.DialogResult = DialogResult.OK;
        }

        void GridView_OnCustomFilter(ref string filter)
        {
            if (tvNhom.SelectedID.Length > 0)
            {
                if (filter.Length > 0) filter += " AND ";
                filter += " DNHOMMATHANGID = '" + tvNhom.SelectedID + "'";
            }
        }


		public void grMua_CustomLoadData(Object sender, CustomLoadDataArgs e)
		{
            e.Select += ", " + Tables.DNHOMMATHANG + "ID";
		}


		public void grTang_CustomLoadData(Object sender, CustomLoadDataArgs e)
		{
            e.Select += ", " + Tables.DNHOMMATHANG + "ID";
		}


		public void tvNhom_OnFocusedNodeChanged(TreeNode node, DataRow r, String ID, TreeItemType type)
		{
            grMua.GridView.Filter = txtLocMua.Text;
            grTang.GridView.Filter = txtLocTang.Text;
		}
    }
}
