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
using System.IO;
using Aspose.Cells;
using System.Diagnostics;

namespace No1Run
{
    /// <summary>
    /// NHẬP KHO
    /// </summary>
    public partial class TDONHANG1Ae
    {
        bool suDung2DVT = false;
        bool nhapKhoBangDauDoc = false;
        bool coSize = false;
        bool coHanSd = false;
		public void grMatHang_OnAddingRowToGrid(DataRow selRow, DataRow newRow, ref bool cancel)
		{
            if (numSoLuong.Value == 0)
            {
                Msg.ShowWarning("Mời bạn nhập số lượng");
                numSoLuong.SelectAllEx();
                cancel = true;
            }
            else
            {
                decimal quyDoi = 1;
                if (suDung2DVT)
                {
                    if (selRow["DDONVITINHCHANID"].ToString().Length == 0)
                    {
                        quyDoi = 1;
                        newRow["DDONVITINH_NAME"] = selRow["DDONVITINH_NAME"];
                        newRow["DDONVITINHID"] = selRow["DDONVITINHID"];
                    }
                    else
                    {
                        quyDoi = ConvertTo.Decimal(selRow["QUYDOI"]);
                        newRow["DDONVITINH_NAME"] = selRow["DDONVITINH2_NAME"];
                        newRow["DDONVITINHID"] = selRow["DDONVITINHCHANID"];
                    }
                }
                else
                {
                    newRow["DDONVITINH_NAME"] = selRow["DDONVITINH_NAME"];
                    newRow["DDONVITINHID"] = selRow["DDONVITINHID"];
                }
                newRow["DMATHANGID"] = selRow["ID"];                
                newRow["DMATHANG_NAME"] = selRow["NAME"];
                newRow["DMATHANG_CODE"] = selRow["CODE"];
                newRow["DMATHANG_MASANCO"] = selRow["MASANCO"];
                newRow["DONGIA"] = numDonGia.Value;
                newRow["SLNHAPCHUAQUYDOI"] = numSoLuong.Value;
                newRow["SLNHAP"] = numSoLuong.Value * quyDoi;
                if (coSize) newRow["KICHTHUOC"] = lastSize;
                if (coHanSd && lastHsd != null) newRow["HANSUDUNG"] = ConvertTo.Date(lastHsd);
            }
		}

		public void mapper_OnCalculation(Object sender, EventArgs e)
		{
            decimal thanhTien = detail.CalcSum("THANHTIEN");
            numTIENHANG.Value = thanhTien;
            bool giamTheoTien = mapper["GIAMTHEOTIEN"].ToInt() == 30;
            if (giamTheoTien)
            {                
                numTILEGIAMGIA.Value = numTIENHANG.Value == 0 ? 0 : 100 * numTIENGIAMGIA.Value / numTIENHANG.Value;
            }
            else
            {
                numTIENGIAMGIA.Value = 100 * Math.Round(thanhTien * numTILEGIAMGIA.Value / (100 * 100));
            }
            
            numTIENTHUE.Value = numTILETHUE.Value * (numTIENHANG.Value - numTIENGIAMGIA.Value) / 100;
            numTONGCONG.Value = numTIENHANG.Value - numTIENGIAMGIA.Value + numTIENTHUE.Value + numPHIVANCHUYEN.Value;

		}


		public void numTILEGIAMGIA_OnEditValueChanged(Object sender, Object value)
		{
            if (numTILEGIAMGIA.Focused)
            {
                mapper["GIAMTHEOTIEN"].Value = 0;
            }
		}


		public void numTIENGIAMGIA_OnEditValueChanged(Object sender, Object value)
		{
            if (numTIENGIAMGIA.Focused)
            {
                mapper["GIAMTHEOTIEN"].Value = 30;
                mapper.RaiseOnCalculation();
            }
		}

		public void mapper_OnKeyDown(Object sender, KeyEventArgs e)
		{
            if (e.KeyCode == Keys.F3)               
            {
                txtTim.SelectAllEx();
            }
		}


		public Boolean grMatHang_OnAddSameItem(DataRow selRow, DataRow row)
		{
            if (SystemConfig.NhapMotMatHangNhieuLanTrongPhieu == 30)
            {
                return false;
            }

            if (selRow["ID"].ToString() == row["DMATHANGID"].ToString() && ConvertTo.Decimal(row["DONGIA"]) == numDonGia.Value)
            {
                if (coSize && row["KICHTHUOC"].ToString() != lastSize)
                {
                    return false;
                }

                if (coHanSd && ConvertTo.Date(row["HANSUDUNG"]) != ConvertTo.Date(lastHsd))
                {
                    return false;
                }

                row["SLNHAPCHUAQUYDOI"] = ConvertTo.Decimal(row["SLNHAPCHUAQUYDOI"]) + numSoLuong.Value;
                UpdateSLNhap(row);
                return true;
            }
            return false;
		}


        List<string> lstUpdate;
		public void mapper_BeforeSave(Object sender, SaveCancelEventArgs e)
		{
            if (SystemConfig.TuDongTinhGiaVon == 30) lstUpdate = new List<string>();
            foreach (DataRow r in detail.Rows)
            {
                if (r.RowState == DataRowState.Deleted) continue;
                TDONHANGCHITIETRow row = new TDONHANGCHITIETRow(r);
                if (lstUpdate != null &&(r.RowState == DataRowState.Modified || r.RowState == DataRowState.Added))
                {
                    if (!lstUpdate.Contains(row.DMATHANGID))
                        lstUpdate.Add(row.DMATHANGID);
                }
                if (row.DKHOHANGID != lueDKHONHAPID.StringValue)
                {
                    row.DKHOHANGID = lueDKHONHAPID.StringValue;
                }

                if (row.SLNHAP <= 0)
                {
                    Msg.ShowWarning("Số lượng hàng nhập phải lớn hơn 0");
                    e.Cancel = true;
                    return;
                }
            }

            if (coHanSd)
            {
                for (int i = 0; i < detail.GridView.Rows.Count; i++)
                {
                    DataGridViewRow dataRow = detail.GridView.Rows[i];
                    DataRow r = (dataRow.DataBoundItem as DataRowView).Row;
                    TDONHANGCHITIETRow ctRow = new TDONHANGCHITIETRow(r);
                    if (ctRow.IsNullValue(TDONHANGCHITIETInfo.HANSUDUNG))
                    {
                        //kiem tra xem mat hang co han su dung khong?
                        DMATHANGRow mhRow = new DMATHANGRow(ctRow.DMATHANGID);
                        if (mhRow.COHANSUDUNG == 30)
                        {
                            Msg.ShowWarning("Mời bạn nhập hạn dùng cho mặt hàng '" + mhRow.CODE + "'-'" + mhRow.NAME + "'");
                            e.Cancel = true;
                            detail.GridView.ClearSelection();
                            dataRow.Selected = true;
                            if (!dataRow.Displayed)
                                detail.GridView.FirstDisplayedScrollingRowIndex = dataRow.Index;
                            return;
                        }
                    }
                }
            }
		}


		public void mapper_AfterFillData(Object sender, EventArgs e)
		{
            if (mapper.ID.Length ==0)
            {
                lueDKHONHAPID.EditValue = Shared.DKHOHANGID;
                txtDIENGIAI.Text = "Nhập mua hàng";
            }            
            numDonGia.DecimalPlaces = 0;
            if (!DbUtils.CanView(Functions.XemGiaNhap))
            {
                detail.GetColumnByField("DONGIA").Visible = false;
                detail.GetColumnByField("THANHTIEN").Visible = false;
            }
		}


		public void numDonGia_KeyDown(Object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Enter) btnThem.PerformClick();
		}


		public Boolean grMatHang_OnAddedRowToGrid(DataRow selRow, DataRow row)
		{            
            numSoLuong.Value = 1;
            return true;
		}


		public void detail_OnRowCalculate(Object sender, DataRow r, String fieldName)
		{
            if (fieldName == "SLNHAPCHUAQUYDOI" || fieldName.Length == 0 || fieldName == "DDONVITINHID")
            {
                UpdateSLNhap(r);
            }
		}

        private void UpdateSLNhap(DataRow r)
        {
            decimal quyDoi = 1;
            if (suDung2DVT)
            {
                string ID = r["DMATHANGID"].ToString();
                DMATHANGRow row = new DMATHANGRow(ID);
                if (TonKhoHandler.Has2DonViTinh(row) && row.DDONVITINHID != r["DDONVITINHID"].ToString() && row.QUYDOI != 0) quyDoi = row.QUYDOI;
            }

            r["SLNHAP"] = ConvertTo.Decimal(r["SLNHAPCHUAQUYDOI"]) * quyDoi;
        }


		public void mapper_OnLoad(Object sender, EventArgs e)
		{
            suDung2DVT = SystemConfig.SuDung2DonViTinh == 30;
            coSize = SystemConfig.MatHangCoKichThuoc == 30;
            coHanSd = SystemConfig.MatHangCoHanSuDung == 30;
            nhapKhoBangDauDoc = SystemConfig.NhapKhoBangDauDocMaVach == 30;
            if (suDung2DVT)
            {
                detail.GridView.CellMouseClick += new DataGridViewCellMouseEventHandler(GridView_CellMouseClick);
                detail.GridView.CellMouseEnter += new DataGridViewCellEventHandler(GridView_CellMouseEnter);
                detail.GridView.CellMouseLeave += new DataGridViewCellEventHandler(GridView_CellMouseLeave);                
            }
            grMatHang.SelectionChanged += new EventHandler(GridView_SelectionChanged);
            mapper.Controller.Maximize();
            mapper.Controller.AllowMaxWindows = true;
            mapper.Controller.SetSaveByEnterKey(false);

            if (!DbUtils.CanView(Functions.XemGiaNhap))
            {
                KryptonGroupBox1.Visible = false;
                mapper.Controller.SetPrintFeature(false);
            }

            if (nhapKhoBangDauDoc)
            {
                txtTim.EnterKeyNextControl = false;
                txtTim.KeyPress += new KeyPressEventHandler(txtTim_KeyPress);
            }
		}

        void txtTim_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (int)Keys.Enter)
            {
                btnThem.PerformClick();
            }
        }

        void GridView_SelectionChanged(object sender, EventArgs e)
        {
            if (grMatHang.SelectedRows.Count > 0)
            {
                numDonGia.Value = ConvertTo.Decimal(grMatHang.SelectedRow["GIANHAP"]);
            }
            else
            {
                numDonGia.Value = 0;
            }
        }        

        void GridView_CellMouseLeave(object sender, DataGridViewCellEventArgs e)
        {
            detail.GridView.Cursor = Cursors.Default;
        }

        void GridView_CellMouseEnter(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex >= 0 && detail.GridView.Columns[e.ColumnIndex].DataPropertyName == "DDONVITINH_NAME")
            {
                detail.GridView.Cursor = Cursors.Hand;
            }
        }

        void GridView_CellMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (suDung2DVT && detail.GridView.Columns[e.ColumnIndex].DataPropertyName == "DDONVITINH_NAME")
            {
                //kiem tra xem co ho tro khong?
                DataRow r = detail.GridView.SelectedRow;
                string DMATHANGID = r["DMATHANGID"].ToString();
                DMATHANGRow row = new DMATHANGRow(DMATHANGID);
                //hien thi de lua chon
                if (TonKhoHandler.Has2DonViTinh(row) && row.QUYDOI != 0)
                {
                    ChonDonViTinh form = (ChonDonViTinh)Config.CreateForm(Forms.ChonDonViTinh);
                    form.SetData(row);
                    if (form.form.ShowDialog() == DialogResult.OK)
                    {
                        DDONVITINHRow dvtRow = new DDONVITINHRow(form.SelectedID);
                        //cap nhat don vi tinh
                        r["DDONVITINH_NAME"] = dvtRow.NAME;
                        r["DDONVITINHID"] = dvtRow.ID;
                        UpdateSLNhap(r);
                        detail.CalculateRow(r);
                    }                    
                }
                else
                {
                    Msg.ShowInfo("Mặt hàng này chỉ có 1 đơn vị tính");
                }
            }
        }


		public void UserControl1_Load(Object sender, EventArgs e)
		{
            if (suDung2DVT)
            {
                DataGridViewColumn col = detail.GridView.Columns["DDONVITINH_NAME"];
                if (col != null)
                {
                    col.DefaultCellStyle.Font = new System.Drawing.Font(detail.GridView.Font, FontStyle.Underline);
                    col.DefaultCellStyle.ForeColor = Color.Blue;
                }
            }
		}


		public void mapper_AfterSave(Object sender, EventArgs e)
		{
            if (lstUpdate != null && lstUpdate.Count > 0)
            {
                string where = "";
                foreach (string id in lstUpdate)
                {
                    if (where.Length > 0) where += " OR ";
                    where += "ID = '" + id + "'";
                }
                UpdateGiaVon(where);
            }
            
            if (SystemConfig.SuDung2DonViTinh == 0)
            {
                Config.Db.ExecSql("UPDATE TDONHANGCHITIET SET SLNHAP = COALESCE(SLNHAPCHUAQUYDOI, 0), SLXUAT = COALESCE(SLXUATCHUAQUYDOI, 0) WHERE TDONHANGID = '" + mapper.ID + "'");
            }
		}

        public static void UpdateGiaVon(string whereIds)
        {
            string sql = "UPDATE DMATHANG SET GIAVON = (" + Environment.NewLine +
                      "SELECT FIRST 1 CASE WHEN SUM(COALESCE(SLNHAP, 0)) = 0 THEN DMATHANG.GIAVON ELSE SUM(COALESCE(CAST(SLNHAPCHUAQUYDOI * DONGIA * (1 - COALESCE(TDONHANG.TILEGIAMGIA, 0) / 100) AS DECIMAL(18, 2)) * (1 - COALESCE(TDONHANGCHITIET.TILEGIAMGIA, 0) / 100), 0)) / SUM(COALESCE(SLNHAP, 0)) END FROM TDONHANGCHITIET INNER JOIN TDONHANG ON TDONHANG.ID = TDONHANGCHITIET.TDONHANGID" + Environment.NewLine +
                      "WHERE (LOAI = 1 OR LOAI = 97) AND DMATHANGID = DMATHANG.ID)";
            if (whereIds.Length < 500 * 36)
            {
                sql += " WHERE " + whereIds;
            }
            Config.Db.ExecSql(sql);
        }

		public void detail_OnImpExcelColumnShowing(Object sender, List<ImportColumn> lst)
		{
            if (SystemConfig.SuDung2DonViTinh == 30)
                lst.Insert(0, new ImportColumn("ĐVT", "DVT", typeof(string)));
            
            if (SystemConfig.SuDungMaHang == 30)
                lst.Insert(0, new ImportColumn("Mã hàng", "MAHANG", typeof(string)));
            else
                lst.Insert(0, new ImportColumn("Tên hàng", "MAHANG", typeof(string)));            
		}


		public void detail_OnImpExcelRowValidate(Object sender, DataRow row, Boolean hasData, CancelEventArgs e)
		{
			//bỏ qua các dòng không có mặt hàng và số lượng
            if (ConvertTo.Decimal(row["SLNHAPCHUAQUYDOI"]) == 0) e.Cancel = true;
            else
            {
                string code = row["MAHANG"].ToString().Trim();
                if (code.Length == 0) e.Cancel = true;
                else
                {
                    //kiem tra xem co ton tai mat hang khong?
                    if (dicMatHang.ContainsKey(code.ToLower()))
                    {
                        DataRow r = dicMatHang[code.ToLower()];
                        row["DMATHANGID"] = r["ID"];
                        row["DMATHANG_NAME"] = r["NAME"];
                        row["DMATHANG_CODE"] = r["CODE"];
                        row["DMATHANG_MASANCO"] = r["MASANCO"];

                        row["QUYDOI"] = 1;
                        row["DDONVITINHID"] = r["DDONVITINHID"];
                        row["DDONVITINH_NAME"] = r["DDONVITINH_NAME"];

                        if (coHanSd)
                        {
                            if (ConvertTo.Int(r["COHANSUDUNG"]) == 30)
                            {
                                if (row["HANSUDUNG"] == DBNull.Value)
                                {
                                    error.AppendLine("Mặt hàng '" + r["NAME"].ToString() + "' phải có hạn dùng khi nhập kho");
                                    e.Cancel = true;
                                }
                            }
                            else
                            {
                                row["HANSUDUNG"] = DBNull.Value;
                            }
                        }

                        //kiem tra xem don vi tinh co dung khong?
                        string dvt = row["DVT"].ToString();
                        if (dvt.Length > 0)
                        {
                            if (dicDvt.ContainsKey(dvt.ToLower()))
                            {
                                DataRow rDvt = dicDvt[dvt.ToLower()];
                                string DVTID = rDvt["ID"].ToString();
                                if (DVTID == r["DDONVITINHCHANID"].ToString())
                                {
                                    row["QUYDOI"] = r["QUYDOI"];
                                    row["DDONVITINHID"] = DVTID;
                                    row["DDONVITINH_NAME"] = dvt;
                                }
                                else if (DVTID != r["DDONVITINHID"].ToString())
                                {
                                    error.AppendLine("Mặt hàng '" + r["NAME"].ToString() + "' không có đơn vị tính '" + dvt + "'");
                                    e.Cancel = true;
                                }
                            }
                            else
                            {
                                error.AppendLine("Đơn vị tính '" + dvt + "' ở mặt hàng '" + r["NAME"].ToString() + "' không tồn tại trong hệ thống");
                                e.Cancel = true;
                            }
                        }                        
                    }
                    else
                    {
                        error.AppendLine("Mã hàng '" + code + "' không tồn tại trong hệ thống");
                        e.Cancel = true;
                    }
                }
            }
		}


		public void detail_OnImpExcelRowAdding(Object sender, DataRow row)
		{
            DataTable dt = detail.DataSource;
            DataRow newRow = dt.NewRow();
            newRow["DDONVITINH_NAME"] = row["DDONVITINH_NAME"];
            newRow["DDONVITINHID"] = row["DDONVITINHID"];
            newRow["DMATHANGID"] = row["DMATHANGID"];
            newRow["DMATHANG_NAME"] = row["DMATHANG_NAME"];
            newRow["DMATHANG_CODE"] = row["DMATHANG_CODE"];
            newRow["TILEGIAMGIA"] = ConvertTo.Decimal(row["TILEGIAMGIA"]);
            newRow["DMATHANG_MASANCO"] = row["DMATHANG_MASANCO"];
            newRow["DONGIA"] = ConvertTo.Decimal(row["DONGIA"]);
            newRow["NOTE"] = row["NOTE"];
            newRow["SLNHAPCHUAQUYDOI"] = row["SLNHAPCHUAQUYDOI"];
            newRow["SLNHAP"] = ConvertTo.Decimal(row["SLNHAPCHUAQUYDOI"]) * ConvertTo.Decimal(row["QUYDOI"]);
            if (row["HANSUDUNG"] != DBNull.Value) newRow["HANSUDUNG"] = ConvertTo.Date(row["HANSUDUNG"]);
            newRow["KICHTHUOC"] = row["KICHTHUOC"];
            dt.Rows.Add(newRow);
            detail.CalculateRow(newRow);
		}

		public void detail_OnImpExcelDataValidate(Object sender, DataTable dt, CancelEventArgs e)
		{
			//kiểm tra xem
            if (error.Length > 0)
            {
                Msg.ShowWarning(error.ToString());
                e.Cancel = true;
            }
		}

        Dictionary<string, DataRow> dicMatHang;
        Dictionary<string, DataRow> dicDvt;
        StringBuilder error;
		public void detail_OnImpExcelDataPrepare(Object sender, DataTable dt)
		{
            //lấy danh sách mặt hàng
            error = new StringBuilder();
            dicMatHang = new Dictionary<string, DataRow>();
            dicDvt = new Dictionary<string, DataRow>();
            DataTable dtMatHang = Config.Db.GetTable("SELECT ID, CODE, COHANSUDUNG,  NAME, MASANCO, QUYDOI, DDONVITINHID, DDONVITINHCHANID, (SELECT NAME FROM DDONVITINH WHERE ID = DDONVITINHID) AS DDONVITINH_NAME FROM DMATHANG WHERE STATUS = 30");
            foreach (DataRow r in dtMatHang.Rows)
            {
                string code = r[suDungMa ? "CODE" : "NAME"].ToString().ToLower();
                if (!dicMatHang.ContainsKey(code)) dicMatHang.Add(code, r);
            }
            DataTable dtDvt = DbUtils.Select("ID, NAME", Tables.DDONVITINH, "STATUS = 30");
            foreach (DataRow r in dtDvt.Rows)
            {
                string code = r["NAME"].ToString().ToLower();
                if (!dicDvt.ContainsKey(code)) dicDvt.Add(code, r);
            }
            
			//thêm các cột dự trữ
            dt.Columns.Add("DMATHANGID", typeof(string));
            dt.Columns.Add("DMATHANG_NAME", typeof(string));
            dt.Columns.Add("DMATHANG_CODE", typeof(string));
            dt.Columns.Add("DMATHANG_MASANCO", typeof(string));
            dt.Columns.Add("DDONVITINHID", typeof(string));
            dt.Columns.Add("DDONVITINH_NAME", typeof(string));
            dt.Columns.Add("QUYDOI", typeof(decimal));
            if (dt.Columns["DVT"] == null) dt.Columns.Add("DVT", typeof(string));
            if (dt.Columns["DONGIA"] == null) dt.Columns.Add("DONGIA", typeof(decimal));
            if (dt.Columns["HANSUDUNG"] == null) dt.Columns.Add("HANSUDUNG", typeof(DateTime));
            if (dt.Columns["KICHTHUOC"] == null) dt.Columns.Add("KICHTHUOC", typeof(string));
		}

        bool suDungMa = false;
        public void detail_OnImpExcelColumnSelectionValidate(Object sender, List<string> lstSelected, CancelEventArgs e)
		{
            suDungMa = SystemConfig.SuDungMaHang == 30;
			//bắt buộc phải có cột "Mã hàng" hoặc "Tên hàng", "Số lượng" và "Đơn giá"
            if (suDungMa)
            {
                if (!lstSelected.Contains("Mã hàng"))
                {
                    Msg.ShowWarning("Mời bạn chọn cột 'Mã hàng'");
                    e.Cancel = true;
                }
            }
            else
            {
                if (!lstSelected.Contains("Tên hàng"))
                {
                    Msg.ShowWarning("Mời bạn chọn cột 'Tên hàng'");
                    e.Cancel = true;
                }
            }

            if (!lstSelected.Contains("Số lượng"))
            {
                Msg.ShowWarning("Mời bạn chọn cột 'Số lượng'");
                e.Cancel = true;
            }
		}


		public void detail_OnImpExcelFinished(Object sender, EventArgs e)
		{
            mapper.RaiseOnCalculation();
		}


		public void lueDKHONHAPID_CustomLoadData(object sender, CustomLoadDataArgs e)
		{
            if (!DbConfig.IsAdmin)
            {
                e.Where += " AND DCUAHANGID IN (SELECT DCUAHANGID FROM TNGUOIDUNGTHEOCUAHANG WHERE SUSERID = '" + DbConfig.UserID + "')";
            }
		}


		public void btnLuuVaInMaVach_Click(object sender, EventArgs e)
		{
            if (mapper.Controller.DoSave(SenderType.SAVE))
            {
                List<string> lstIds = new List<string>();
                List<int> lstQty = new List<int>();
                foreach (DataGridViewRow r in detail.GridView.Rows)
                {
                    DataRow row = (r.DataBoundItem as DataRowView).Row;
                    lstIds.Add(row["DMATHANGID"].ToString());
                    lstQty.Add((int)ConvertTo.Decimal(row["SLNHAPCHUAQUYDOI"]));
                }

                UiUtils.PrintBarCode(Tables.DMATHANG, "", lstIds, lstQty, null);
            }
		}

		public void btnXuatExcel_Click(object sender, EventArgs e)
		{
            SaveFileDialog saveDlg = new SaveFileDialog();
            saveDlg.Filter = "Excel file (*.xls, *.xlsx)|*.xls;*.xlsx";
            if (saveDlg.ShowDialog() == DialogResult.OK)
            {
                Workbook workbook = new Workbook();
                Worksheet worksheet = workbook.Worksheets[0];
                worksheet.Cells[0, 0].Value = "Mã";
                worksheet.Cells[0, 1].Value = "Mã sẵn có";
                worksheet.Cells[0, 2].Value = "Tên";
                worksheet.Cells[0, 3].Value = "ĐVT";
                worksheet.Cells[0, 4].Value = "Giá bán";

                string sql = @"SELECT DMATHANG.CODE, DMATHANG.NAME, DMATHANG.MASANCO, 
(SELECT NAME FROM DDONVITINH WHERE ID = TDONHANGCHITIET.DDONVITINHID) AS DVT,
DMATHANG.GIABAN,
SLNHAP
FROM TDONHANG INNER JOIN TDONHANGCHITIET ON TDONHANG.ID = TDONHANGCHITIET.TDONHANGID
AND TDONHANGID = '" + mapper.ID + "' INNER JOIN DMATHANG ON TDONHANGCHITIET.DMATHANGID = DMATHANG.ID";

                DataTable dt = Config.Db.GetTable(sql);
                int k = 1;
                foreach (DataRow r in dt.Rows)
                {
                    int slNhap = (int)ConvertTo.Decimal(r["SLNHAP"]);
                    for (int i = 0; i < slNhap; i++)
                    {
                        worksheet.Cells[k, 0].Value = r["CODE"].ToString();
                        worksheet.Cells[k, 1].Value = r["MASANCO"].ToString();
                        worksheet.Cells[k, 2].Value = r["NAME"].ToString();
                        worksheet.Cells[k, 3].Value = r["DVT"].ToString();
                        worksheet.Cells[k, 4].Value = r["GIABAN"].ToString();
                        k++;
                    }
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


		public void btnInBartender_Click(object sender, EventArgs e)
		{
            if (mapper.Controller.DoSave(SenderType.SAVE))
            {
                List<string> lstIds = new List<string>();
                List<int> lstQty = new List<int>();
                foreach (DataGridViewRow r in detail.GridView.Rows)
                {
                    DataRow row = (r.DataBoundItem as DataRowView).Row;
                    lstIds.Add(row["DMATHANGID"].ToString());
                    lstQty.Add((int) ConvertTo.Decimal(row["SLNHAPCHUAQUYDOI"]));
                }
               
                UiUtils.PrintBarCodeByBartender(Tables.DMATHANG, lstIds, lstQty, "", "");
            }
		}


		public void grMatHang_CustomLoadData(object sender, CustomLoadDataArgs e)
		{
            if (SystemConfig.SapXepThuTuTheo == 0)
            {
                e.OrderBy = "CODE";
            }
            else
            {
                e.OrderBy = "NAME";
            }
		}


		public void grMatHang_OnCustomFilter(ref String filter)
		{
            filter = "";
            if (txtTim.Text.Length > 0)
            {
                string orgVal = DbUtils.LoaiBoDauTiengViet(txtTim.Text);

                string txt = SystemConfig.CoKyTuKiemTra == 30 ? orgVal.Substring(0, orgVal.Length - 1).Replace("'", "''") : orgVal.Replace("'", "''");
                if (SystemConfig.SuDungCanDienTu == 30 && txt.StartsWith("29") && orgVal.Length > 6)
                {
                    txt = orgVal.Substring(0, orgVal.Length - 6);
                }

                string timFull = orgVal.Replace("'", "''").Trim();
                string op = "";
                string opFull = "";
                int cachTim = SystemConfig.CachTim;
                switch (cachTim)
                {
                    case 0: //co chua
                        op = " LIKE '%" + txt + "%'";
                        opFull = " LIKE '%" + timFull + "%'";
                        break;
                    case 1: //bat dau bang
                        op = " LIKE '" + txt + "%'";
                        opFull = " LIKE '" + timFull + "%'";
                        break;
                    default: //chinh xac
                        op = " = '" + txt + "'";
                        opFull = " = '" + timFull + "'";
                        break;
                }

                int timTheo = SystemConfig.TimTheo;
                if (timTheo == 0 || timTheo == 2)
                {
                    filter = "CODE" + op;
                    if (SystemConfig.SuDungMaSanCo == 30)
                    {
                        filter += " OR MASANCO" + opFull;
                    }
                }
                if (timTheo == 1 || timTheo == 0)
                {
                    DataTable dtMatHang = grMatHang.DataTable;
                    if (dtMatHang.Columns["NAMEKODAU"] == null)
                    {
                        UiUtils.ThemCotKhongDau(dtMatHang, "NAME", "NAMEKODAU");
                    }
                    if (filter.Length > 0) filter += " OR ";
                    filter += "NAMEKODAU" + opFull;
                }
            }            
		}

        string lastSize = "";
        object lastHsd = null;
		public void grMatHang_OnThemClicked(object sender, CancelEventArgs e)
		{
			//kiểm tra xem có size không?
            if (coSize)
            {                
                ChonKichThuocNhapKho form = (ChonKichThuocNhapKho)Config.CreateForm(Forms.ChonKichThuocNhapKho);
                form.Load(grMatHang.SelectedRow);
                if (form.No1Form1.ShowDialog() == DialogResult.OK)
                {
                    lastSize = form.KichThuoc;
                }
                else
                {
                    e.Cancel = true;
                    return;
                }
            }
            //kiểm tra xem có hạn dùng không?
            lastHsd = null;
            if (coHanSd)
            {
                DMATHANGRow mhRow = new DMATHANGRow(grMatHang.SelectedRow);
                if (mhRow.COHANSUDUNG == 30)
                {
                    //mặt hàng có hạn dùng       
                    NhapHanSuDung form = (NhapHanSuDung)Config.CreateForm(Forms.NhapHanSuDung);
                    form.SetMatHang(mhRow);
                    if (form.No1Form1.ShowDialog() == DialogResult.OK)
                    {
                        lastHsd = form.dtHanDung.EditValue;
                    }
                    else
                    {
                        e.Cancel = true;
                    }
                }
            }
		}
    }
}
