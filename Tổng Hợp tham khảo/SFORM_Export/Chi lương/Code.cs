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
    public partial class ChiLuong
    {
        string TBANGLUONGID;
        internal void SetData(string TBANGLUONGID)
        {
            this.TBANGLUONGID = TBANGLUONGID;

            TBANGLUONGRow row = new TBANGLUONGRow(TBANGLUONGID);
            lblTitle.Text = lblTitle.Text + " " + row.THANG.ToString() + " năm " + row.NAM.ToString();

            LoadData("");
        }

        private void LoadData(string DNHANVIENID)
        {
            string sql = @"SELECT TTHUCHI.NAME, TTHUCHI.ID, TBANGLUONGTONGHOP.DNHANVIENID, (SELECT NAME FROM DNHANVIEN WHERE ID = TBANGLUONGTONGHOP.DNHANVIENID) AS NHANVIEN,
                            TBANGLUONGTONGHOP.THUCNHAN, COALESCE(TTHUCHI.CHI, 0) AS SOTIEN, TTHUCHI.NAME AS SOPHIEU
                            FROM TBANGLUONGTONGHOP LEFT OUTER JOIN TTHUCHI ON TTHUCHI.TBANGLUONGID = TBANGLUONGTONGHOP.TBANGLUONGID
                            AND TTHUCHI.DNHANVIENID = TBANGLUONGTONGHOP.DNHANVIENID WHERE TBANGLUONGTONGHOP.TBANGLUONGID = '" + TBANGLUONGID + "'";
            if (DNHANVIENID.Length == 0)
            {
                grChiLuong.AutoGenerateColumns = false;
                grChiLuong.DataSource = Config.Db.GetTable(sql);
            }
            else
            {
                sql += " AND TBANGLUONGTONGHOP.DNHANVIENID = '" + DNHANVIENID + "'";
                DataRow r = Config.Db.GetFirstRow(sql);
                DataRow selRow = (grChiLuong.SelectedRows[0].DataBoundItem as DataRowView).Row;
                foreach (DataColumn col in r.Table.Columns)
                {
                    selRow[col.ColumnName] = r[col];
                }
                grChiLuong.Update();
                UpdateToolbar();
            }
        }

        private void UpdateToolbar()
        {
            if (grChiLuong.SelectedRows.Count > 0)
            {
                tsbPhieuChi.Enabled = true;
                DataRow r = (grChiLuong.SelectedRows[0].DataBoundItem as DataRowView).Row;
                if (r["ID"].ToString().Length > 0)
                {
                    tsbPhieuChi.Text = "Xem phiếu chi lương cho '" + r["NHANVIEN"].ToString() + "'";
                }
                else
                {
                    tsbPhieuChi.Text = "Tạo phiếu chi lương cho '" + r["NHANVIEN"].ToString() + "'";
                }
            }
            else
            {
                tsbPhieuChi.Enabled = false;
            }
        }

		public void tsbPhieuChi_Click(Object sender, EventArgs e)
		{
            DataRow r = (grChiLuong.SelectedRows[0].DataBoundItem as DataRowView).Row;
            decimal thucNhan = ConvertTo.Decimal(r["THUCNHAN"]);

            if (thucNhan == 0)
            {
                if (Msg.ShowYesNo("Dường như bạn chưa thiết lập cách tính lương (lương = 0), bạn có muốn tiếp tục chi lương không?") != DialogResult.Yes)
                {
                    return;
                } 
            }
            string ID = r["ID"].ToString();
            DynamicAeForm form = (DynamicAeForm)Config.CreateAeForm(Tables.TTHUCHI, 1, ID);
            form.ReLoad(ID);
            if (ID.Length == 0)
            {
                ((TTHUCHI1Ae) form.CodeRunner).SetLuong(r["DNHANVIENID"].ToString(), thucNhan, TBANGLUONGID);
            }
            form.ShowDialog();
            if (form.IsDataSaved())
            {
                LoadData(r["DNHANVIENID"].ToString());
            }
		}


		public void grChiLuong_SelectionChanged(Object sender, EventArgs e)
		{
            UpdateToolbar();
		}


		public void grChiLuong_CellMouseDoubleClick(Object sender, DataGridViewCellMouseEventArgs e)
		{
            tsbPhieuChi.PerformClick();
		}
    }
}
