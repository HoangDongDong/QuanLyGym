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
using ComponentFactory.Krypton.Toolkit;
using System.Collections;

namespace No1Run
{
    public partial class ChonCotExcel
    {
        private DataTable dt;
        private DataGridView gv;

        private static Dictionary<string, string> dic;
        
        public void SetData(DataTable dt, List<string> lst)
        {            
            grConfig.AutoGenerateColumns = false;
            grConfig.DataSource = dt;
            this.dt = dt;
            this.gv = gv;

            KryptonDataGridViewComboBoxColumn comboBoxCol = new KryptonDataGridViewComboBoxColumn();
            comboBoxCol.Name = "colComboBox";
            comboBoxCol.DataPropertyName = "Dữ liệu";
            comboBoxCol.DataPropertyName = "DULIEU";
            comboBoxCol.HeaderText = "Dữ liệu";
            comboBoxCol.DropDownStyle = ComboBoxStyle.DropDownList;

            comboBoxCol.Items.Add(" ");
            Hashtable hstCols = new Hashtable();
            foreach (string col in lst)
            {
                comboBoxCol.Items.Add(col);
                if (!hstCols.ContainsKey(col)) hstCols.Add(col, null);
            }
            if (dic != null)
            {
                foreach (DataRow r in dt.Rows)
                {
                    string colName = r["EXCEL"].ToString();
                    if (dic.ContainsKey(colName))
                    {
                        string mapCol = dic[colName];
                        if (hstCols.ContainsKey(mapCol))
                            r["DULIEU"] = mapCol;
                    }
                }
            }

            grConfig.Columns.Add(comboBoxCol);
        }
        
		public void btnOK_Click(object sender, EventArgs e)
		{
            //kiểm tra xem có bị trùng cột không?
            grConfig.CommitEdit(DataGridViewDataErrorContexts.Commit);
            Hashtable hst = new Hashtable();

            DataTable dtSrc = grConfig.DataSource as DataTable;

            //kiểm tra xem có cột nào không?
            foreach (DataRow r in dtSrc.Rows)
            {
                string colName = r["DULIEU"].ToString();
                if (colName.Length > 0)
                {
                    if (hst.ContainsKey(colName.ToLower()))
                    {
                        Msg.ShowWarning("1 cột dữ liệu chỉ xuất hiện 1 lần '" + colName + "'");
                        return;
                    }
                    else
                        hst.Add(colName.ToLower(), null);
                }
            }

            if (hst.Count > 0)
            {
                dic = new Dictionary<string, string>();
                foreach (DataRow r in dtSrc.Rows)
                {
                    string colName = r["DULIEU"].ToString();
                    if (colName.Length > 0)
                    {
                        string excelColName = r["EXCEL"].ToString();
                        if (dic.ContainsKey(excelColName))
                            dic[excelColName] = colName;
                        else
                            dic.Add(excelColName, colName);
                    }
                }

                form.DialogResult = DialogResult.OK;
                return;
            }
            Msg.ShowWarning("Mời bạn chọn cột để điền dữ liệu trước!");
		}
    }
}
