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
    public partial class ChonMatHangTra
    {
		public void tblNhom_OnFocusedNodeChanged(TreeNode node, DataRow r, String ID, TreeItemType type)
		{
            Filter();
		}

		public void tblNhom_OnCustomNode()
		{
            tblNhom.AddAllNode();
		}

        public string DMATHANGID
        {
            get 
            {
                if (DoiTheoDon)
                    return grDonHang.GridView.SelectedRow["DMATHANGID"].ToString();
                else
                    return grDetail.SelectedID;                 
            }
        }

        public DataRow[] Rows
        {
            get { return grDonHang.DataSource.Select("CHON=30"); }
        }


		public void txtTim_TextChanged(Object sender, EventArgs e)
		{
            
		}

        public bool DoiTheoDon;

        DataTable dtTraLai;
        public void Load(DataTable dtTraLai)
        {            
            DoiTheoDon = SystemConfig.BatBuocDoiTraHangTheoDon == 30;
            grDetail.Visible = !DoiTheoDon;
            grDetail.CustomLoadData += new CustomLoadDataHandler(grDetail_CustomLoadData);
            grDetail.GridView.OnCustomFilter += new MiscDataGridView.OnCustomFilterHandler(GridView_OnCustomFilter);
            grDetail.GridView.SelectionChanged += new EventHandler(GridView_SelectionChanged);
            grDetail.GridView.CellMouseDoubleClick += new DataGridViewCellMouseEventHandler(GridView_CellMouseDoubleClick);

            grDonHang.CustomLoadData += new CustomLoadDataHandler(grDetail_CustomLoadData);
            grDonHang.GridView.OnCustomFilter += new MiscDataGridView.OnCustomFilterHandler(GridView_OnCustomFilter);
            grDonHang.GridView.SelectionChanged += new EventHandler(GridView_SelectionChanged);
            grDonHang.GridView.CellMouseDoubleClick += new DataGridViewCellMouseEventHandler(GridView_CellMouseDoubleClick);

            if (DoiTheoDon)
            {
                this.dtTraLai = dtTraLai;
                txtSoHoaDon.Select();
                grDonHang.Visible = true;
                grDetail.Visible = false;
                grDonHang.Dock = DockStyle.Fill;
                grDonHang.GridView.SearchTextBox = txtTim;
                grDonHang.LoadData();
                DataGridViewCheckBoxColumn col = new DataGridViewCheckBoxColumn();
                col.HeaderText = "Chọn";
                col.Width = 40;
                col.DataPropertyName = "CHON";                

                grDonHang.GridView.Columns.Insert(0, col);
                //thêm cột chọn
                grDonHang.GridView.ReadOnly = false;
                foreach (DataGridViewColumn c in grDonHang.GridView.Columns)
                {
                    c.ReadOnly = true;
                }
                foreach (DataGridViewColumn c in grDonHang.GridView.Columns)
                {
                    if (c.DataPropertyName == "SLXUATCHUAQUYDOI")
                    {
                        NumericDataGridViewColumn colTra = new NumericDataGridViewColumn();
                        colTra.DataPropertyName = "SLTRA";
                        colTra.HeaderText = "Sl trả";
                        colTra.Width = 60;
                        colTra.DisplayIndex = c.DisplayIndex + 1;
                        grDonHang.GridView.Columns.Insert(colTra.Index + 1, colTra);
                        break;
                    }
                }                
                grDonHang.GridView.CellClick += new DataGridViewCellEventHandler(GridView_CellClick);
            }
            else
            {
                lblSoHoaDon.Visible = false;
                txtSoHoaDon.Visible = false;
                btnLoc.Visible = false;
                txtTim.Left = txtSoHoaDon.Left;
                lblTim.Left = lblSoHoaDon.Left;
                txtTim.Select();
                grDetail.GridView.SearchTextBox = txtTim;
                grDonHang.Visible = false;
                grDetail.LoadData();
            }
        }

        void GridView_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex >= 0 && e.RowIndex >= 0 && grDonHang.GridView.Columns[e.ColumnIndex].DataPropertyName == "CHON")
            {
                DataRow r = grDonHang.GridView.SelectedRow;                
                r["CHON"] = ConvertTo.Int(r["CHON"]) == 30 ? 0 : 30;
                if (ConvertTo.Int(r["SLTRA"]) == 0 && ConvertTo.Int(r["CHON"]) == 30)
                {
                    r["SLTRA"] = 1;
                }
                else
                {
                    r["SLTRA"] = 0;
                }
                grDonHang.GridView.InvalidateRow(e.RowIndex);
            }
        }

        void GridView_CellMouseDoubleClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            //if (e.RowIndex >= 0 && e.ColumnIndex >= 0)
            //    btnOK.PerformClick();
        }

        void GridView_SelectionChanged(object sender, EventArgs e)
        {
            btnOK.Enabled = (grDonHang.Visible ? grDonHang : grDetail).GridView.SelectedRows.Count > 0;
        }

        void GridView_OnCustomFilter(ref string filter)
        {
            if (tblNhom.SelectedID.Length > 0)
            {
                if (filter.Length > 0) filter += " AND ";
                filter += Tables.DNHOMMATHANG + "ID = '" + tblNhom.SelectedID + "'";
            }
        }

        private string TDONHANGID = "";
        void grDetail_CustomLoadData(object sender, CustomLoadDataArgs e)
        {
            if (sender == grDonHang)
            {
                e.Select += ", " + Tables.DNHOMMATHANG + "ID";
                if (e.Where.Length > 0) e.Where += " AND ";
                e.Where += "TDONHANGID = '" + TDONHANGID + "' AND SLXUAT > 0";
            }
            else
            {
                e.Select += ", " + Tables.DNHOMMATHANG + "ID";
                
                if (SystemConfig.SapXepThuTuTheo == 0)
                {
                    e.OrderBy = "CODE";
                }
                else
                {
                    e.OrderBy = "NAME";
                }
            }
        }

        private void Filter()
        {
            (grDonHang.Visible ? grDonHang : grDetail).GridView.Filter = txtTim.Text;
        }


		public void txtSoHoaDon_KeyDown(Object sender, KeyEventArgs e)
		{
            if (e.KeyCode == Keys.Enter) btnLoc.PerformClick();
		}


		public void btnLoc_Click(Object sender, EventArgs e)
		{
            //kiem tra xem hoa don co ton tai khong?
            if (txtSoHoaDon.Text.Length == 0)
            {
                Msg.ShowWarning("Mời bạn nhập số hóa đơn");
                return;
            }

            string sql = "SELECT FIRST 1 ID FROM TDONHANG WHERE NAME = '" + txtSoHoaDon.Text.Replace("'", "''") + "'";
            TDONHANGID = Config.Db.GetFirstFieldString(sql);
            if (TDONHANGID.Length == 0)
            {
                Msg.ShowWarning("Đơn hàng '" + txtSoHoaDon.Text + "' không tồn tại.");
                return;
            }

            //kiem tra xem ngay co duoc phep khong?            
            int soNgay = SystemConfig.SoNgayChoPhepDoiTra;            
            TDONHANGRow dhRow = new TDONHANGRow(TDONHANGID);
            if (dhRow.NGAY.AddDays(soNgay) < Config.Db.DbDate)
            {
                Msg.ShowWarning("Chỉ có thể đổi trả trong " + soNgay.ToString() + " ngày. Hóa đơn số '" + dhRow.NAME + "' mua ngày " + dhRow.NGAY.ToString("dd/MM/yyyy") + " quá số ngày có thể đổi trả");
                return;
            }

            grDonHang.LoadData();
            foreach (DataRow r in grDonHang.DataSource.Rows)
            {
                DataRow[] rows = dtTraLai.Select("TDONHANGTRAID='" + r["ID"].ToString() + "'");
                if (rows.Length > 0)
                {
                    r["SLTRA"] = ConvertTo.Decimal(rows[0]["SLNHAPCHUAQUYDOI"]);
                    r["CHON"] = 30;
                }
            }

            if (grDonHang.RowCount == 0)
            {
                Msg.ShowWarning("Đơn hàng '" + txtSoHoaDon.Text + "' không tồn tại hoặc không có mặt hàng mua");
            }
		}


		public void btnOK_Click(Object sender, EventArgs e)
		{
            if (DoiTheoDon)
            {
                //kiểm tra xem có chọn vào dòng nào không?
                DataTable dt = grDonHang.DataSource;
                if (dt.Select("CHON=30").Length == 0)
                {
                    Msg.ShowWarning("Mời bạn chọn mặt hàng trả lại");
                    return;
                }

                //kiem tra xem co nhap so luong khong?
                foreach (DataGridViewRow dataRow in grDonHang.GridView.Rows)
                {
                    DataRow r = (dataRow.DataBoundItem as DataRowView).Row;
                    if (ConvertTo.Decimal(r["CHON"]) == 30)
                    {
                        if (ConvertTo.Decimal(r["SLTRA"]) == 0)
                        {
                            Msg.ShowWarning("Mời bạn nhập số lượng trả");
                            grDonHang.GridView.ClearSelection();
                            dataRow.Selected = true;
                            if (!dataRow.Displayed) grDonHang.GridView.FirstDisplayedScrollingRowIndex = dataRow.Index;
                            return;
                        }

                        if (ConvertTo.Decimal(r["SLTRA"]) > ConvertTo.Decimal(r["SLXUATCHUAQUYDOI"]))
                        {
                            Msg.ShowWarning("Số lượng trả vượt quá số lượng bán");
                            grDonHang.GridView.ClearSelection();
                            dataRow.Selected = true;
                            if (!dataRow.Displayed) grDonHang.GridView.FirstDisplayedScrollingRowIndex = dataRow.Index;
                            return;
                        }
                    }
                }
            }
            form.DialogResult = DialogResult.OK;
		}


		public void grDonHang_CustomLoadData(object sender, CustomLoadDataArgs e)
		{
            e.Select += ", 0 AS CHON, NULL AS SLTRA";
		}
    }
}
