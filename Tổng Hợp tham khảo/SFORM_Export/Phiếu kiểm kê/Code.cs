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

namespace No1Run
{
    /// <summary>
    /// KIỂM KÊ
    /// </summary>
    public partial class TDONHANG4Ae
    {
        bool coSize = false;
        bool coHanSd = false;
		public void grMatHang_OnAddingRowToGrid(DataRow selRow, DataRow newRow, ref bool cancel)
		{
            if (lueDKHONHAPID.StringValue.Length == 0)
            {
                Msg.ShowWarning("Mời bạn chọn kho để kiểm trước");
                cancel = true;
            }
            newRow["DMATHANGID"] = selRow["ID"];
            newRow["DDONVITINH_NAME"] = selRow["DDONVITINH_NAME"];
            newRow["DDONVITINHID"] = selRow["DDONVITINHID"];
            newRow["DMATHANG_NAME"] = selRow["NAME"];
            newRow["DMATHANG_CODE"] = selRow["CODE"];
            newRow["SLTHUCTE"] = numSoLuong.Enabled ? numSoLuong.Value : 1;
            newRow["KICHTHUOC"] = lastSize;
            if (lastHanSuDung != null) newRow["HANSUDUNG"] = lastHanSuDung;
            newRow["SLHETHONG"] = TonKhoHandler.GetTonKhoEx(selRow["ID"].ToString(), lueDKHONHAPID.StringValue, mapper.ID, lastSize, lastHanSuDung);
		}

		public void numSoLuong_KeyDown(Object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Enter) btnThem.PerformClick();
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
            if (selRow["ID"].ToString() == row["DMATHANGID"].ToString())
            {
                if (coSize && lastSize != row["KICHTHUOC"].ToString())
                {
                    return false;
                }

                if (coHanSd && ConvertTo.Date(lastHanSuDung) != ConvertTo.Date(row["HANSUDUNG"]))
                {
                    return false;
                }
                row["SLTHUCTE"] = ConvertTo.Decimal(row["SLTHUCTE"]) + (numSoLuong.Enabled ? numSoLuong.Value : 1);
                return true;
            }
            return false;
		}


		public void mapper_BeforeSave(Object sender, SaveCancelEventArgs e)
		{
            foreach (DataRow r in detail.Rows)
            {
                if (r.RowState == DataRowState.Deleted) continue;
                TDONHANGCHITIETRow row = new TDONHANGCHITIETRow(r);
                if (row.DKHOHANGID != lueDKHONHAPID.StringValue)
                {
                    row.DKHOHANGID = lueDKHONHAPID.StringValue;
                }
                row.SLXUATCHUAQUYDOI = Math.Max(0, row.SLHETHONG - row.SLTHUCTE);
                row.SLNHAPCHUAQUYDOI = Math.Max(0, row.SLTHUCTE - row.SLHETHONG);
            }
		}

		public void mapper_AfterFillData(Object sender, EventArgs e)
		{
            if (mapper.ID.Length ==0)
            {
                lueDKHONHAPID.EditValue = Shared.DKHOHANGID;
                txtDIENGIAI.Text = "Kiểm kê";
            }

            btnMatHangChuaKiemKe.Visible = mapper.ID.Length > 0;
            lueDKHONHAPID.Enabled = detail.GridView.Rows.Count == 0;
		}


		public void mapper_OnCalculation(Object sender, EventArgs e)
		{
            lueDKHONHAPID.Enabled = detail.GridView.Rows.Count == 0;
		}


		public void btnMatHangChuaKiemKe_Click(Object sender, EventArgs e)
		{
            PhanTichKiemKe form = (PhanTichKiemKe)Config.CreateForm(Forms.PhanTichKiemKe);
            form.SetData(mapper.ID);
            form.form.ShowDialog();
		}


		public void mapper_OnLoad(Object sender, EventArgs e)
		{
            mapper.Controller.Maximize();
            mapper.Controller.AllowMaxWindows = true;
            mapper.Controller.SetSaveByEnterKey(false);

            coSize = SystemConfig.MatHangCoKichThuoc == 30;
            coHanSd = SystemConfig.MatHangCoHanSuDung == 30;

            ToolStripButton itm = new ToolStripButton("Nhập từ máy kiểm kho");
            itm.Click += new EventHandler(itm_Click);
            detail.toolbar.Items.Insert(1, itm);
            detail.GridView.CellFormatting += new DataGridViewCellFormattingEventHandler(GridView_CellFormatting);
		}

        void GridView_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            try
            {
                DataRow r = detail.GridView.GetDataRow(e.RowIndex);
                if (ConvertTo.Decimal(r["SLNHAP"]) != 0 || ConvertTo.Decimal(r["SLXUAT"]) != 0)
                {
                    e.CellStyle.BackColor = Color.LightYellow;
                    e.CellStyle.ForeColor = Color.Red;
                }
            }
            catch
            {
            }
        }

        void itm_Click(object sender, EventArgs e)
        {
            if (detail.RowCount > 0)
            {
                Msg.ShowWarning("Chỉ có thể sử dụng chức năng này khi chưa có mặt hàng trong phiếu");
                return;
            }
            if (lueDKHONHAPID.StringValue.Length == 0)
            {
                Msg.ShowWarning("Mời bạn chọn kho để kiểm trước");
                return;
            }
            OpenFileDialog openDlg = new OpenFileDialog();
            openDlg.Filter = "Text file (*.txt)|*.txt";
            StringBuilder msg = new StringBuilder();
            if (openDlg.ShowDialog() == DialogResult.OK)
            {
                DataTable dt = Config.Db.GetTable("SELECT ID, CODE, NAME, DDONVITINHID, (SELECT NAME FROM DDONVITINH WHERE ID = DDONVITINHID) AS DVT FROM DMATHANG WHERE STATUS = 30");
                string[] lines = File.ReadAllLines(openDlg.FileName);
                Dictionary<string, decimal> dic = new Dictionary<string, decimal>();
                foreach (string line in lines)
                {
                    string[] arr = line.Split(new char[] { ',' });
                    if (arr.Length == 2)
                    {
                        string code = arr[0];
                        decimal qty = ConvertTo.Decimal(arr[1].Trim());
                        DataRow[] rows = dt.Select("CODE='" + code.Replace("'", "''") + "'");
                        if (rows.Length == 0)
                        {
                            Msg.ShowWarning("Mã hàng '" + code + "' không tồn tại trong hệ thống");
                            return;
                        }
                        string ID = rows[0]["ID"].ToString();
                        if (dic.ContainsKey(ID)) dic[ID] = dic[ID] + qty;
                        else dic[ID] = qty;
                    }
                    else if (arr.Length > 0)
                    {
                        msg.AppendLine(line);
                    }
                }

                foreach (string ID in dic.Keys)
                {
                    decimal qty = dic[ID];
                    if (qty != 0)
                    {
                        DataRow newRow = detail.DataSource.NewRow();
                        DataRow selRow = dt.Select("ID='" + ID + "'")[0];
                        newRow["DMATHANGID"] = selRow["ID"];
                        newRow["DDONVITINH_NAME"] = selRow["DVT"];
                        newRow["DDONVITINHID"] = selRow["DDONVITINHID"];
                        newRow["DMATHANG_NAME"] = selRow["NAME"];
                        newRow["DMATHANG_CODE"] = selRow["CODE"];
                        newRow["SLTHUCTE"] = qty;
                        decimal ton = TonKhoHandler.GetTonKho(selRow["ID"].ToString(), lueDKHONHAPID.StringValue, mapper.ID);
                        newRow["SLHETHONG"] = ton;
                        newRow["SLNHAP"] = ton > qty ? 0 : qty - ton;
                        newRow["SLXUAT"] = ton < qty ? 0 : ton - qty;

                        newRow["SLNHAPCHUAQUYDOI"] = ton > qty ? 0 : qty - ton;
                        newRow["SLXUATCHUAQUYDOI"] = ton < qty ? 0 : ton - qty;
                        detail.Rows.Add(newRow);                       
                    }
                }
            }

            if (msg.Length > 0)
            {
                Msg.ShowWarning("Một số dữ liệu không đúng định dạng: " + Environment.NewLine + msg.ToString());
            }

            mapper.IsChanged = true;
            mapper.Controller.SetSaveControl(true);
            lueDKHONHAPID.Enabled = detail.GridView.Rows.Count == 0;
        }


		public void lueDKHONHAPID_CustomLoadData(object sender, CustomLoadDataArgs e)
		{
            if (!DbConfig.IsAdmin)
            {
                e.Where += " AND DCUAHANGID IN (SELECT DCUAHANGID FROM TNGUOIDUNGTHEOCUAHANG WHERE SUSERID = '" + DbConfig.UserID + "')";
            }
		}


		public void mapper_AfterSave(object sender, EventArgs e)
		{
            if (SystemConfig.SuDung2DonViTinh == 0)
            {
                Config.Db.ExecSql("UPDATE TDONHANGCHITIET SET SLNHAP = COALESCE(SLNHAPCHUAQUYDOI, 0), SLXUAT = COALESCE(SLXUATCHUAQUYDOI, 0) WHERE TDONHANGID = '" + mapper.ID + "'");
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


		public void chkKiemBangMayKhongDay_CheckedChanged(object sender, EventArgs e)
		{
            numSoLuong.Enabled = !chkKiemBangMayKhongDay.Checked;
            txtTim.EnterKeyNextControl = false;
		}


		public void txtTim_KeyDown(object sender, KeyEventArgs e)
		{
            if (e.KeyCode == Keys.Enter)
            {
                if (chkKiemBangMayKhongDay.Checked)
                {
                    btnThem.PerformClick();
                    txtTim.Text = "";
                }
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
            if (newRow.Table.Columns["DMATHANG_MASANCO"] != null)
                newRow["DMATHANG_MASANCO"] = row["DMATHANG_MASANCO"];            
            newRow["SLTHUCTE"] = ConvertTo.Decimal(row["SLTHUCTE"]) * ConvertTo.Decimal(row["QUYDOI"]);
            newRow["SLHETHONG"] = TonKhoHandler.GetTonKho(row["DMATHANGID"].ToString(), lueDKHONHAPID.StringValue, mapper.ID);
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
            DataTable dtMatHang = Config.Db.GetTable("SELECT ID, CODE, NAME, MASANCO, QUYDOI, DDONVITINHID, DDONVITINHCHANID, (SELECT NAME FROM DDONVITINH WHERE ID = DDONVITINHID) AS DDONVITINH_NAME FROM DMATHANG WHERE STATUS = 30");
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

            if (!lstSelected.Contains("Số lượng thực tế"))
            {
                Msg.ShowWarning("Mời bạn chọn cột 'Số lượng thực tế'");
                e.Cancel = true;
            }
        }

        public void detail_OnImpExcelFinished(Object sender, EventArgs e)
        {
            mapper.RaiseOnCalculation();
        }

        private string lastSize;
        private object lastHanSuDung;
		public void grMatHang_OnThemClicked(object sender, CancelEventArgs e)
		{
            lastSize = "";
            lastHanSuDung = null;

			//trường hợp có kích thước thì hiển thị ô để chọn
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

            //trường hợp có hạn sử dụng
            if (coHanSd && ConvertTo.Int(grMatHang.SelectedRow["COHANSUDUNG"]) == 30)
            {
                NhapHanSuDung form = (NhapHanSuDung)Config.CreateForm(Forms.NhapHanSuDung);
                form.SetMatHang(new DMATHANGRow(grMatHang.SelectedRow));
                if (form.No1Form1.ShowDialog() == DialogResult.OK)
                {
                    lastHanSuDung = form.dtHanDung.EditValue;
                }
                else
                {
                    e.Cancel = true;
                }
            }
		}
    }
}
