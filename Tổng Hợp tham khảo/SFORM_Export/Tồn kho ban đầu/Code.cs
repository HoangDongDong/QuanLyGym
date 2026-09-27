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
using Aspose.Cells;
using System.Diagnostics;

namespace No1Run
{
    public partial class TonKhoBanDau
    {
        public void SetData(string DKHOID)
        {            
            lueDKHO.LoadData(Tables.DKHOHANG.ToString());
            lueDKHO.EditValue = DKHOID;            
            LoadData();
        }

        string TDONHANGID = "";
        DataTable dt;
        private void LoadData()
        {
            string sql = "SELECT ID, NGAY FROM TDONHANG WHERE DKHONHAPID = '" + lueDKHO.StringValue + "' AND LOAI = 97";
            DataRow row = Config.Db.GetFirstRow(sql);
            if (row == null)
            {
                dtNgay.DateTime = new DateTime(Config.Db.DbDate.Year, 1, 1).AddDays(-1);
            }
            else
            {
                dtNgay.DateTime = ConvertTo.Date(row["NGAY"]);
                TDONHANGID = row["ID"].ToString();
            }
            //load detail data
            sql = "SELECT * FROM( " +
                  "SELECT DNHOMMATHANGID, DMATHANG.DDONVITINHID, DMATHANGID, TDONHANGCHITIET.ID, CODE, MASANCO AS CODE2, NAME, (SELECT NAME FROM DDONVITINH WHERE ID = TDONHANGCHITIET.DDONVITINHID) AS DVT, SLNHAP, TDONHANGCHITIET.DONGIA, THANHTIEN FROM TDONHANGCHITIET INNER JOIN DMATHANG ON DMATHANG.ID = TDONHANGCHITIET.DMATHANGID WHERE TDONHANGID = '" + TDONHANGID + "' " +
                  "UNION ALL " +
                  "SELECT DNHOMMATHANGID, DDONVITINHID, ID, '', CODE, MASANCO AS CODE2, NAME, (SELECT NAME FROM DDONVITINH WHERE ID = DDONVITINHID), 0, 0, 0 FROM DMATHANG WHERE STATUS = 30 AND ID NOT IN (SELECT DMATHANGID FROM TDONHANGCHITIET WHERE TDONHANGID = '" + TDONHANGID + "')) " +
                  " A ORDER BY CODE";

            dt = Config.Db.GetTable(sql);

            grMain.LoadDataSearchable(dt);

            UpdateThongTin();
        }

		public void txtLoc_TextChanged(object sender, EventArgs e)
		{
            grMain.Filter = txtLoc.Text;
		}


		public void tsbImport_Click(object sender, EventArgs e)
		{
            OpenFileDialog openDlg = new OpenFileDialog();
            openDlg.Filter = "Excel file (*.xls, *.xlsx)|*.xls;*.xlsx";
            if (openDlg.ShowDialog() == DialogResult.OK)
            {
                Workbook wb = new Workbook();
                try
                {
                    wb.Open(openDlg.FileName);
                }
                catch
                {
                    Msg.ShowWarning("Lỗi không thể đọc file!");
                    return;
                }
                Worksheet sh = wb.Worksheets[0];

                DataTable dtExcel = new DataTable();
                dtExcel.Columns.Add("EXCEL", typeof(string));
                dtExcel.Columns.Add("DULIEU", typeof(string));

                for (int i = 0; i < sh.Cells.MaxColumn + 1; i++)
                {
                    DataRow r = dtExcel.NewRow();
                    r["EXCEL"] = sh.Cells[0, i].StringValue;
                    dtExcel.Rows.Add(r);
                }

                List<string> lst = new List<string>();
                lst.Add("Mã hàng hóa");
                lst.Add("Tồn");
                lst.Add("Giá vốn");
                ChonCotExcel form = (ChonCotExcel)Config.CreateForm(Forms.ChonCotExcel);
                form.SetData(dtExcel, lst);
                if (form.form.ShowDialog() == DialogResult.OK)
                {
                    Dictionary<string, int> dic = new Dictionary<string, int>();
                    int i = 0;
                    foreach (DataRow r in dtExcel.Rows)
                    {
                        if (r["DULIEU"].ToString().Length > 0)
                        {
                            string caption = r["DULIEU"].ToString();
                            dic.Add(caption, i);
                        }
                        i++;
                    }
                    if (!dic.ContainsKey("Mã hàng hóa"))
                    {
                        Msg.ShowWarning("Bạn chưa chọn cột mã hàng hóa");
                        return;
                    }

                    if (!dic.ContainsKey("Tồn"))
                    {
                        Msg.ShowWarning("Bạn chưa chọn cột tồn");
                        return;
                    }

                    if (!dic.ContainsKey("Giá vốn"))
                    {
                        Msg.ShowWarning("Bạn chưa chọn cột giá vốn");
                        return;
                    }

                    //checking for validating
                    for (int row = 1; row < sh.Cells.MaxRow + 1; row++)
                    {
                        int maHangCol = dic["Mã hàng hóa"];
                        string maHang = ConvertTo.String(sh.Cells[row, maHangCol].Value).Trim().Replace("'", "''");
                        if (maHang.Length > 0)
                        {
                            DataRow[] rows = dt.Select("CODE='" + maHang + "' OR CODE2 = '" + maHang + "'");
                            if (rows.Length == 0)
                            {
                                Msg.ShowWarning("Mã hàng hóa '" + maHang + "' không tồn tại");
                            }
                            else
                            {
                                DataRow datarow = rows[0];
                                try
                                {
                                    object val = sh.Cells[row, dic["Giá vốn"]].Value;
                                    datarow["DONGIA"] = ConvertTo.Decimal(val);
                                }
                                catch
                                {
                                    datarow["DONGIA"] = 0;
                                }

                                try
                                {
                                    object val = sh.Cells[row, dic["Tồn"]].Value;
                                    datarow["SLNHAP"] = ConvertTo.Decimal(val);
                                }
                                catch
                                {
                                    datarow["SLNHAP"] = 0;
                                }
                                datarow["THANHTIEN"] = ConvertTo.Decimal(datarow["DONGIA"]) * ConvertTo.Decimal(datarow["SLNHAP"]);
                            }
                        }
                    }
                }
                UpdateThongTin();
            }      
		}


		public void tsbFileMau_Click(object sender, EventArgs e)
		{
            SaveFileDialog saveDlg = new SaveFileDialog();
            saveDlg.Filter = "Excel file (*.xls, *.xlsx)|*.xls;*.xlsx";
            if (saveDlg.ShowDialog() == DialogResult.OK)
            {
                Workbook workbook = new Workbook();
                Worksheet worksheet = workbook.Worksheets[0];
                worksheet.Cells[0, 0].Value = "Mã sẵn có";
                worksheet.Cells[0, 1].Value = "Mã hàng";
                worksheet.Cells[0, 2].Value = "Tên hàng";
                worksheet.Cells[0, 3].Value = "Tồn";
                worksheet.Cells[0, 4].Value = "Giá vốn";
                int i = 1;
                foreach (DataRow r in dt.Rows)
                {
                    worksheet.Cells[i, 0].Value = r["CODE2"].ToString();
                    worksheet.Cells[i, 1].Value = r["CODE"].ToString();
                    worksheet.Cells[i, 2].Value = r["NAME"].ToString();
                    worksheet.Cells[i, 3].Value = 0;
                    worksheet.Cells[i, 4].Value = 0;
                    i++;
                }
                try
                {
                    workbook.Save(saveDlg.FileName);
                }
                catch (Exception ex)
                {
                    Msg.ShowWarning(ex.Message);
                    return;
                }
                if (Msg.ShowYesNo("Bạn có muốn mở file đã xuất không?") == DialogResult.Yes)
                {
                    Process.Start(saveDlg.FileName);
                }
            }
		}


		public void btnOK_Click(object sender, EventArgs e)
		{
            if (dtNgay.EditValue == null)
            {
                Msg.ShowWarning("Mời bạn nhập ngày chốt công nợ ban đầu");
                return;
            }
            TDONHANGRow dhRow = new TDONHANGRow(TDONHANGID);
            dhRow.NGAY = dtNgay.DateTime;
            dhRow.TONGCONG = numTongGiaTri.Value;
            dhRow.TILETHUE = 0;
            dhRow.TIENHANG = numTongGiaTri.Value;
            dhRow.DKHONHAPID = lueDKHO.StringValue;
            dhRow.LOAI = 97;
            dhRow.NAME = "TONDAU";
            dhRow.Update();
            //update detail      
            string whereIds = "";
            foreach (DataRow r in dt.Rows)
            {
                decimal soLuong = ConvertTo.Decimal(r["SLNHAP"]);
                string TDONHANGCHITIETID = r["ID"].ToString();
                if (r.RowState == DataRowState.Modified)
                {
                    if (soLuong == 0 && TDONHANGCHITIETID.Length > 0)
                    {
                        //xoa
                        Config.Db.ExecSql("DELETE FROM TDONHANGCHITIET WHERE ID = '" + TDONHANGCHITIETID + "'");
                    }
                    else if (soLuong > 0)
                    {
                        if (TDONHANGCHITIETID.Length > 0)
                        {
                            //update
                            TDONHANGCHITIETRow uRow = new TDONHANGCHITIETRow(TDONHANGCHITIETID);
                            uRow.SLNHAP = soLuong;
                            uRow.SLNHAPCHUAQUYDOI = soLuong;
                            uRow.DONGIA = ConvertTo.Decimal(r["DONGIA"]);
                            uRow.THANHTIEN = soLuong * uRow.DONGIA;
                            uRow.Update();
                        }
                        else
                        {
                            //insert
                            TDONHANGCHITIETRow uRow = new TDONHANGCHITIETRow();
                            uRow.SLNHAP = soLuong;
                            uRow.SLNHAPCHUAQUYDOI = soLuong;
                            uRow.DONGIA = ConvertTo.Decimal(r["DONGIA"]);
                            uRow.THANHTIEN = soLuong * uRow.DONGIA;
                            uRow.TILEGIAMGIA = 0;
                            uRow.SLXUAT = 0;
                            uRow.DMATHANGID = r["DMATHANGID"].ToString();
                            uRow.DDONVITINHID = r["DDONVITINHID"].ToString();
                            uRow.TDONHANGID = dhRow.ID;
                            uRow.DKHOHANGID = lueDKHO.StringValue;
                            uRow.Update();
                        }
                    }
                    if (whereIds.Length > 0) whereIds += " OR ";
                    whereIds += "ID = '" + r["DMATHANGID"].ToString() + "'";
                }
            }            
            
            if (whereIds.Length > 0)
                TDONHANG1Ae.UpdateGiaVon(whereIds);
            form.DialogResult = DialogResult.OK;
		}

        private void UpdateThongTin()
        {
            decimal soLuong = 0;
            decimal giaTri = 0;
            foreach (DataRow r in dt.Rows)
            {
                if (r.RowState == DataRowState.Deleted) continue;
                TDONHANGCHITIETRow ctRow = new TDONHANGCHITIETRow(r);
                soLuong += ctRow.SLNHAP;
                giaTri += ctRow.THANHTIEN;
            }
            lblCount.Text = "Tổng số lượng: " + soLuong.ToString("#,#.##");
            numTongGiaTri.Value = giaTri;
        }

		public void form_OnInit(object sender, EventArgs e)
		{
			//hien thi kho de lua chon
            string DKHOID = Shared.DKHOHANGID;
            TreeSelect form = new TreeSelect(Tables.DKHOHANG, "MỜI BẠN CHỌN KHO");            
            if (form.ShowDialog() == DialogResult.OK)
            {
                DKHOID = form.CategoryID;
            }
            SetData(DKHOID);
		}


		public void grMain_CellValueChanged(object sender, DataGridViewCellEventArgs e)
		{
            if (e.RowIndex >= 0 && e.ColumnIndex >= 0)
            {
                TDONHANGCHITIETRow ctRow = new TDONHANGCHITIETRow((grMain.Rows[e.RowIndex].DataBoundItem as DataRowView).Row);
                ctRow.THANHTIEN = ctRow.SLNHAP * ctRow.DONGIA;
                UpdateThongTin();
            }
		}
    }
}
