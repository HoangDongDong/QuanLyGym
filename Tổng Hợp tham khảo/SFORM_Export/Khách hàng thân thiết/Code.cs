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
    public partial class KhachHangThanThiet
    {


		public void No1UserControl1_Load(Object sender, EventArgs e)
		{
            grTangDiem.sFunctionID = Functions.KhachHangThanThiet;
            grKhachHang.CustomLoadData += new CustomLoadDataHandler(grKhachHang_CustomLoadData);
            grKhachHang.GridView.SearchTextBox = txtTim;
            grKhachHang.GridView.SelectionChanged += new EventHandler(GridView_SelectionChanged);
            grKhachHang.GridView.OnCustomFilter += new MiscDataGridView.OnCustomFilterHandler(GridView_OnCustomFilter);
            grKhachHang.LoadData();
		}

        void grKhachHang_CustomLoadData(object sender, CustomLoadDataArgs e)
        {
            e.Command.Parameters.Add("@CACHTINH", FbDbType.Integer).Value = SystemConfig.CachTinhDiem;
            e.Command.Parameters.Add("@DIEM", FbDbType.Integer).Value = Math.Max(SystemConfig.DoanhSoTuongUngVoi1Diem, 1);            
        }

        void GridView_OnCustomFilter(ref string filter)
        {
            if (tvNhom.SelectedID.Length > 0)
            {
                if (filter.Length > 0) filter += " AND ";
                filter += DKHACHHANGInfo.DNHOMKHACHHANGID.ToString() + "='" + tvNhom.SelectedID + "'";
            }
        }

        void GridView_SelectionChanged(object sender, EventArgs e)
        {
            Timer1.Enabled = false;
            Timer1.Enabled = true;
        }


		public void tvNhom_OnCustomNode()
        {
            tvNhom.AddAllNode();
		}


		public void tvNhom_OnFocusedNodeChanged(TreeNode node, DataRow r, String ID, TreeItemType type)
		{
            grKhachHang.GridView.Filter = txtTim.Text;
		}


		public void No1UserControl1_KeyDownEx(Object sender, KeyEventArgs e)
		{
            if (e.KeyCode == Keys.F3) txtTim.Select();
		}


		public void grTangDiem_AfterCreatedEditForm(IAddEditForm form, String ID)
		{
            if (ID.Length == 0)
            {
                form.SetValue(TTANGGIAMDIEMInfo.DKHACHHANGID.ToString(), grKhachHang.SelectedID);
            }
		}


		public void grTangDiem_CustomLoadData(Object sender, CustomLoadDataArgs e)
		{
            if (e.Where.Length > 0) e.Where += " AND ";
            e.Where += "DKHACHHANGID = '" + grKhachHang.SelectedID + "'";
		}


		public void grHoaDon_CustomLoadData(Object sender, CustomLoadDataArgs e)
		{
            if (e.Where.Length > 0) e.Where += " AND ";
            e.Where += "DKHACHHANGID = '" + grKhachHang.SelectedID + "' AND DATHANHTOAN = 30";
		}


		public void Timer1_Tick(object sender, EventArgs e)
		{
            Timer1.Enabled = false;
            grHoaDon.LoadData("");
            grTangDiem.LoadData("");
            tabDetail.Enabled = grKhachHang.SelectedID.Length > 0;
		}
    }
}
