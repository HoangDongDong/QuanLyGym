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
    public partial class ChonKichThuocNhapKho
    {
        private string DMATHANGID;
        internal void Load(DataRow dataRow)
        {
            DMATHANGID = dataRow["ID"].ToString();
            lblItem.Text = dataRow["NAME"].ToString();
            //bóc tách size
            string Size = Config.Db.GetFirstFieldString(String.Format("SELECT SIZE FROM DMATHANG WHERE ID = '{0}'", dataRow["ID"]));
            string[] Items = Size.Split(new char[]{',', ';'}, StringSplitOptions.RemoveEmptyEntries);

            DataTable dt = new DataTable();
            dt.Columns.Add("KICHTHUOC", typeof(string));
            foreach (string Item in Items)
            {
                DataRow r = dt.NewRow();
                r["KICHTHUOC"] = Item.Trim();
                dt.Rows.Add(r);
            }

            grKichThuoc.LoadDataSearchable(dt);
        }

        public string KichThuoc = "";


		public void btnOK_Click(object sender, EventArgs e)
		{
            if (grKichThuoc.SelectedRows.Count > 0)
            {
                KichThuoc = grKichThuoc.SelectedRow["KICHTHUOC"].ToString();
                No1Form1.DialogResult = DialogResult.OK;
            }
            else if (txtKichThuoc.Text.Trim().Length == 0)
            {
                if (Msg.ShowYesNo("Bạn có muốn nhập không kích thước?") == DialogResult.Yes)
                {
                    No1Form1.DialogResult = DialogResult.OK;
                }
            }
            else
            {
                string msg = String.Format("Bạn có muốn thêm kích thước '{0}' vào danh sách kích thước của mặt hàng '{1}' không?",
                                   txtKichThuoc.Text.Trim(), lblItem.Text);
                if (Msg.ShowYesNo(msg) == DialogResult.Yes)
                {
                    DMATHANGRow mhRow = new DMATHANGRow(DMATHANGID);
                    if (mhRow.SIZE.Length > 0) mhRow.SIZE = mhRow.SIZE + ", " + txtKichThuoc.Text.Trim();
                    else mhRow.SIZE = txtKichThuoc.Text.Trim();
                    mhRow.Update();

                    KichThuoc = txtKichThuoc.Text.Trim();
                    No1Form1.DialogResult = DialogResult.OK;
                }
            }
		}


		public void grKichThuoc_CellMouseDoubleClick(object sender, DataGridViewCellMouseEventArgs e)
		{
            if (e.RowIndex >= 0 && e.ColumnIndex >= 0)
            {
                btnOK.PerformClick();
            }
		}
    }
}
