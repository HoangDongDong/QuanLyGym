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
    public partial class DanhLaiMaHang
    {


		public void btnMaNhom_Click(object sender, EventArgs e)
		{
            dtNhom = grMain.DataSource as DataTable;
            Dictionary<string, int> dic = new Dictionary<string, int>();
            foreach (DataRow r in dtNhom.Rows)
            {
                string pID = r["PARENTID"].ToString();
                int val = dic.ContainsKey(pID) ? dic[pID] + 1 : 10;
                r["MA"] = val.ToString();
                if (!dic.ContainsKey(pID))
                {
                    dic.Add(pID, val);
                }
                else
                {
                    dic[pID] = val;
                }
            }
            grMain.Refresh();
		}

        No1TreeTable tbl;
        DataTable dtNhom;
        Dictionary<string, string> dic;
		public void No1Form1_Load(object sender, EventArgs e)
		{
            dic = new Dictionary<string, string>();
            DataTable dtNhom = Config.Db.GetTable("SELECT ID, CODE AS MACODE FROM DNHOMMATHANG");
            foreach (DataRow r in dtNhom.Rows)
            {
                dic.Add(r["ID"].ToString(), r["MACODE"].ToString());
            }

            tbl = new No1TreeTable();
            tbl.Table = Tables.DNHOMMATHANG.ToString();
            tbl.LoadData();

            DataTable dt = new DataTable();
            dt.Columns.Add("ID", typeof(string));
            dt.Columns.Add("PARENTID", typeof(string));
            dt.Columns.Add("NHOM", typeof(string));
            dt.Columns.Add("MA", typeof(string));
            dt.Columns.Add("LEVEL", typeof(int));
            dt.Columns.Add("CHILDCOUND", typeof(int));

            foreach (TreeNode node in tbl.tvMain.Nodes)
            {
                string tab = "";
                LoadTree(dt, node, tab, 0);
            }

            grMain.DataSource = dt;
            label1.Text = label1.Text + " Độ dài mã: " + SystemConfig.ChieuDaiMaVach.ToString();
		}

        private void LoadTree(DataTable dt, TreeNode node, string tab, int level)
        {
            string ID = tbl.GetID(node);
            DataRow r = dt.NewRow();
            r["ID"] = ID;
            r["NHOM"] = tab + node.Text;
            r["MA"] = dic[ID];
            r["LEVEL"] = level;
            r["CHILDCOUND"] = node.Nodes.Count;
            if (node.Parent != null)
            {
                r["PARENTID"] = tbl.GetID(node.Parent);
            }
            else
            {
                r["PARENTID"] = "";
            }
            dt.Rows.Add(r);
            foreach (TreeNode child in node.Nodes)
            {
                LoadTree(dt, child, tab + "    ", level + 1);
            }
        }

		public void btnOK_Click(object sender, EventArgs e)
		{
            if (txtMatKhau.Text != "4321")
            {
                Msg.ShowWarning("Mật khẩu không đúng");
                return;
            }
            dtNhom = grMain.DataSource as DataTable;
            //thực hiện
            if (!IsValid()) return;
            BackgroundWorker bw = new BackgroundWorker();
            bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bw_RunWorkerCompleted);
            bw.DoWork += new DoWorkEventHandler(bw_DoWork);
            prMain.Visible = true;
            prDetail.Visible = true;
            lblTrangThai.Visible = true;
            btnOK.Enabled = false;
            btnMaNhom.Visible = false;
            bw.RunWorkerAsync();
		}

        private bool IsValid()
        {
            if (chkMaNhom.Checked)
            {
                DataTable dt = grMain.DataSource as DataTable;
                Dictionary<string, string> dicTest = new Dictionary<string, string>();
                foreach (DataRow r in dt.Rows)
                {
                    string key = r["MA"].ToString().ToUpper() + "++" + r["PARENTID"].ToString();
                    if (dicTest.ContainsKey(key))
                    {
                        string msg = "Mã '" + r["MA"].ToString() + "', nhóm '" + r["NHOM"].ToString() + "' ";
                        if (r["PARENTID"].ToString().Length > 0)
                        {
                            msg += "trong nhóm '" + new DNHOMMATHANGRow(r["PARENTID"].ToString()).NAME + "' ";
                        }
                        msg += "bị trùng lặp, bạn có muốn tiếp tục không?";
                        if (Msg.ShowYesNo(msg) != DialogResult.Yes)
                        {
                            return false;
                        }
                    }
                    else
                    {
                        dicTest.Add(key, "");
                    }
                }
            }
            return true;
        }

        void bw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            prMain.Visible = false;
            prDetail.Visible = false;
            btnOK.Enabled = true;
            btnMaNhom.Visible = true;
            lblTrangThai.Visible = false;
            Msg.ShowWarning("Đã đánh lại mã thành công");
            No1Form1.DialogResult = DialogResult.OK;
        }

        void bw_DoWork(object sender, DoWorkEventArgs e)
        {
            int len = SystemConfig.ChieuDaiMaVach;

            int i = 0;
            if (chkMaNhom.Checked)
            {
                //cập nhật mã nhóm trước
                UpdateStatus(0, 0, "Đang chuẩn bị...", "");
                foreach (DataRow r in dtNhom.Rows)
                {
                    DNHOMMATHANGRow upRow = new DNHOMMATHANGRow(r["ID"].ToString());
                    upRow.CODE = r["MA"].ToString();
                    upRow.Update();
                    UpdateStatus(i * 100 / dtNhom.Rows.Count, 0, r["NHOM"].ToString(), "");
                    i++;
                }
            }
            Dictionary<string, int> dicInt = new Dictionary<string, int>();
            UpdateStatus(0, 0, "Cập nhật mã hàng...", "");
            i = 0;
            dtNhom.Columns.Add("FULLCODE");
            Dictionary<string, string> dicFullCode = new Dictionary<string, string>();
            foreach (DataRow r in dtNhom.Rows)
            {
                if (dicFullCode.ContainsKey(r["PARENTID"].ToString()))
                {
                    r["FULLCODE"] = dicFullCode[r["PARENTID"].ToString()] + r["MA"].ToString();
                }
                else
                {
                    r["FULLCODE"] = r["MA"].ToString();
                }
                dicFullCode.Add(r["ID"].ToString(), r["FULLCODE"].ToString().ToUpper());
            }

            foreach (DataRow r in dtNhom.Rows)
            {
                UpdateStatus(i * 100 / dtNhom.Rows.Count, 0, r["NHOM"].ToString(), "");
                //lấy danh sách mặt hàng trong nhóm
                DataTable dtChild = Config.Db.GetTable("SELECT ID, NAME FROM DMATHANG WHERE DNHOMMATHANGID = '" + r["ID"].ToString() + "' ORDER BY NAME");
                string code = r["FULLCODE"].ToString();
                int num = dicInt.ContainsKey(code) ? dicInt[code] : 1;
                int j = 0;
                foreach (DataRow rChild in dtChild.Rows)
                {
                    UpdateStatus(i * 100 / dtNhom.Rows.Count, j * 100 / dtChild.Rows.Count, r["NHOM"].ToString(), rChild["NAME"].ToString());
                    j++;
                    string uCode = code;
                    int curLen = code.Length + num.ToString().Length;
                    for (int k = curLen; k < len; k++)
                    {
                        uCode += "0";
                    }
                    uCode = uCode + num.ToString();
                    DMATHANGRow mhRow = new DMATHANGRow(rChild["ID"].ToString());
                    mhRow.CODE = uCode;
                    mhRow.Update();
                    num++;
                }
                i++;
            }
        }

        private void UpdateStatus(int percent, int percentDetail, string label, string detail)
        {
            prMain.Invoke(new MethodInvoker(delegate()
            {
                prMain.Value = percent;
                prDetail.Value = percentDetail;
                lblTrangThai.Text = label.Trim() + (detail.Length > 0 ? Environment.NewLine : "") + detail;
            }));
        }

		public void grMain_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
		{
            DataRow r = (grMain.Rows[e.RowIndex].DataBoundItem as DataRowView).Row;
            int childcount = ConvertTo.Int(r["CHILDCOUND"]);
            if (childcount > 0)
            {
                e.CellStyle.Font = new Font(e.CellStyle.Font, FontStyle.Bold);
            }
            int level = ConvertTo.Int(r["LEVEL"]);
            if (level == 0)
            {
                e.CellStyle.BackColor = Color.LightBlue;
            }
            else if (level == 1)
            {
                e.CellStyle.BackColor = Color.LightPink;
            }
		}


		public void chkMaNhom_CheckedChanged(object sender, EventArgs e)
		{
            grMain.ReadOnly = !chkMaNhom.Checked;
		}
    }
}
