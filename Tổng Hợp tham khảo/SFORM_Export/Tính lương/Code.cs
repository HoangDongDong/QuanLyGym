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
    public partial class TinhLuong
    {
        bool showLuong = true;
        bool loaded = false;
		public void No1UserControl1_Load(Object sender, EventArgs e)
		{
            showLuong = No1UserControl1.CallerMenuID.NAME == "Tính lương";
            if (!showLuong)
            {
                btnChiLuong.Visible = false;
                btnRefresh.Visible = false;
            }
            loaded = true;
            if (grMain.SelectedID.Length > 0)
            {
                LoadDetail(grMain.SelectedID);
                tmrLoadDetail.Enabled = true;       
            }
		}

		public void grMain_CustomLoadData(Object sender, CustomLoadDataArgs e)
		{
            e.OrderBy = "NAM,THANG";
		}


		public void grMain_OnDeleting(String ID, Boolean Permanent, CancelEventArgs e)
		{
            tTinhLuongDelID = ID;
		}

		public void grMain_OnDeleted(String ID, Boolean Permanent)
		{
            Config.Db.ExecSql("DELETE FROM TBANGLUONGCHITIET WHERE TBANGLUONGID = '" + tTinhLuongDelID + "'");
		}

		public void grMain_OnFocusedRowChanged(Boolean CanRemove, Boolean canAdd, Boolean CanEdit)
		{
            if (!loaded) return;            
            SelID = grMain.GridView.SelectedRows.Count == 0 ? "" : (grMain.GridView.SelectedRows[0].DataBoundItem as DataRowView).Row["ID"].ToString();            
            tmrLoadDetail.Enabled = false;
            tmrLoadDetail.Enabled = true;
		}

        private DataTable dtCa, dtNhanVien;
        private DateTime startDate, stopDate;
        Dictionary<string, DataRow> dic;
        DataGridView grChiTiet;
        private void LoadDetail(string ID)
        {
            if (grChiTiet != null && !grChiTiet.IsDisposed)
                grChiTiet.Dispose();
            grChiTiet = new DataGridView();
            grChiTiet.CurrentCellChanged += new EventHandler(grChiTiet_CurrentCellChanged);
            grChiTiet.RowHeadersVisible = false;
            grChiTiet.CellDoubleClick += new DataGridViewCellEventHandler(grChiTiet_CellDoubleClick);
            grChiTiet.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            grChiTiet.AllowUserToResizeRows = false;
            No1System.SetDoubleBuffered(grChiTiet);
            grChiTiet.AllowUserToAddRows = false;
            grChiTiet.AllowUserToDeleteRows = false;
            grChiTiet.Dock = DockStyle.Fill;
            SelID = ID;
            KryptonSplitContainer1.Panel2.Controls.Add(grChiTiet);
            grChiTiet.BringToFront();                      

            workerLoadDetail_DoWork(null, null);
        }

        void grChiTiet_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {            
            if (e.RowIndex >= 0 && e.ColumnIndex >= 0)
            {
                string field = grChiTiet.Columns[e.ColumnIndex].DataPropertyName;
                int index = (int)(e.RowIndex / dtCa.Rows.Count);
                DataRow r = dtNhanVien.Rows[index];
                string DNHANVIENID = r["DNHANVIENID"].ToString();
                //nhân viên
                if (field == "NHANVIEN" || field == "LUONGCOBAN")
                {
                    No1Lib.Sys.DynamicAeForm form = (No1Lib.Sys.DynamicAeForm)Config.CreateAeForm(Tables.DNHANVIEN, -1, DNHANVIENID);
                    form.ReLoad(DNHANVIENID);
                    (form.CodeRunner as DNHANVIENAe).LockChangeNhanVien();
                    if (form.ShowDialog() == DialogResult.OK)
                    {
                        RefreshData(DNHANVIENID);
                    }
                }
                else if (field == "PHAT" || field == "THUONG" || field == "TAMUNG")
                {
                    LuongChiTiet form = (LuongChiTiet)Config.CreateForm(Forms.LuongChiTiet);
                    form.SetData(DNHANVIENID, field == "PHAT" ? LuongChiTietMode.Phat :
                        field == "THUONG" ? LuongChiTietMode.Thuong : LuongChiTietMode.TamUng, SelID);
                    form.form.ShowDialog();
                    if (form.Changed)
                    {
                        RefreshData(DNHANVIENID);
                    }
                }
            }
        }

        DataGridViewCell lastActiveCell;
        void grChiTiet_CurrentCellChanged(object sender, EventArgs e)
        {
            bool redraw = false;
            if (grChiTiet.CurrentCell is VMergedCell || lastActiveCell is VMergedCell) redraw = true;
            if (redraw)
            {
                grChiTiet.InvalidateColumn(grChiTiet.CurrentCell.ColumnIndex);
                if (lastActiveCell != null)
                    grChiTiet.InvalidateColumn(lastActiveCell.ColumnIndex);
            }
            lastActiveCell = grChiTiet.CurrentCell;
        }


        void workerLoadDetail_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            waitingForm.Close();
            grChiTiet.Visible = true;
        }

        int totalDays;
        private string SelID;

        int tongNgayTruCN;
        int tongNgayTruT7;
        int tongNgayTruT7CN;

        void workerLoadDetail_DoWork(object sender, DoWorkEventArgs e)
        {
            //load detail
            TBANGLUONGRow row = new TBANGLUONGRow(SelID);

            string sql = "SELECT * FROM (SELECT TBANGLUONGTONGHOP.ID AS TBANGLUONGTONGHOPID, DNHANVIENID, " +
                         "DNHANVIEN.NAME AS NHANVIEN, " +
                         "TBANGLUONGTONGHOP.LUONGCA, TONGLUONG, " +
                         "(SELECT COALESCE(SUM(PHAT), 0) FROM TTHUONGPHAT WHERE TTHUONGPHAT.DNHANVIENID =  TBANGLUONGTONGHOP.DNHANVIENID AND NGAY BETWEEN @FromDate AND @ToDate) AS PHAT," +
                         "(SELECT COALESCE(SUM(THUONG), 0) FROM TTHUONGPHAT WHERE TTHUONGPHAT.DNHANVIENID =  TBANGLUONGTONGHOP.DNHANVIENID AND NGAY BETWEEN @FromDate AND @ToDate) AS THUONG," +
                         "(SELECT CASE WHEN SUM(CHI) IS NULL THEN 0 ELSE SUM(CHI) END FROM TTHUCHI WHERE DNHANVIENID = TBANGLUONGTONGHOP.DNHANVIENID AND NGAY BETWEEN @FromDate AND @ToDate AND LATAMUNG = 30) AS TAMUNG, THUCNHAN," +
                         "TBANGLUONGTONGHOP.LUONGTHANG, TBANGLUONGTONGHOP.CACHTINHLUONG,NGHICHUNHAT,NGHITHU7," +
                         "CASE WHEN TBANGLUONGTONGHOP.CACHTINHLUONG = 30 THEN TBANGLUONGTONGHOP.LUONGCA ELSE TBANGLUONGTONGHOP.LUONGTHANG END AS LUONGCOBAN " +
                         "FROM TBANGLUONGTONGHOP INNER JOIN DNHANVIEN ON DNHANVIEN.ID = TBANGLUONGTONGHOP.DNHANVIENID WHERE TBANGLUONGID = '" + SelID + "' " +
                         "UNION ALL " +
                         "SELECT '', ID, NAME, LUONGCA, 0, " +
                         "(SELECT COALESCE(SUM(PHAT), 0) FROM TTHUONGPHAT WHERE TTHUONGPHAT.DNHANVIENID =  DNHANVIEN.ID AND NGAY BETWEEN @FromDate AND @ToDate) AS PHAT," +
                         "(SELECT COALESCE(SUM(THUONG), 0) FROM TTHUONGPHAT WHERE TTHUONGPHAT.DNHANVIENID =  DNHANVIEN.ID AND NGAY BETWEEN @FromDate AND @ToDate) AS THUONG," +
                         "(SELECT CASE WHEN SUM(CHI) IS NULL THEN 0 ELSE SUM(CHI) END FROM TTHUCHI WHERE DNHANVIENID = DNHANVIEN.ID AND NGAY BETWEEN @FromDate AND @ToDate AND LATAMUNG = 30) AS TAMUNG, 0," +
                         "LUONGTHANG, CACHTINHLUONG,NGHICHUNHAT,NGHITHU7," +
                         "CASE WHEN CACHTINHLUONG = 30 THEN LUONGCA ELSE LUONGTHANG END AS LUONGCOBAN " +
                         "FROM DNHANVIEN " +
                         "WHERE " +
                         "STATUS = 30 AND COALESCE(ITEMTYPE, 0) = 0 AND " +
                         "ID NOT IN (SELECT DNHANVIENID FROM TBANGLUONGTONGHOP WHERE TBANGLUONGID = '" + SelID + "')) A ORDER BY A.NHANVIEN";
            startDate = new DateTime(row.NAM, row.THANG, 1);
            stopDate = startDate.AddMonths(1).AddDays(-1);

            FbCommand cmd = Config.Db.GetCommand(sql);
            cmd.Parameters.Add("@FromDate", startDate);
            cmd.Parameters.Add("@ToDate", stopDate);

            dtNhanVien = Config.Db.GetTable(cmd);
            sql = "SELECT ID, NAME, TILELUONG FROM DCALAMVIEC WHERE STATUS = 30 AND (ITEMTYPE IS NULL OR ITEMTYPE = 0)";
            dtCa = Config.Db.GetTable(sql);

            AddColumns(startDate, stopDate);

            sql = "SELECT * FROM TBANGLUONGCHITIET WHERE TBANGLUONGID = '" + SelID + "'";
            DataTable dtChiTiet = Config.Db.GetTable(sql);
            dic = new Dictionary<string, DataRow>();
            foreach (DataRow r in dtChiTiet.Rows)
            {
                TBANGLUONGCHITIETRow ctRow = new TBANGLUONGCHITIETRow(r);
                try
                {
                    dic.Add(ctRow.DNHANVIENID.ToString() + ctRow.DCALAMVIECID.ToString() + ctRow.NGAY.ToString("ddMMyy"), r);
                }
                catch
                {
                }
            }

            DataTable dtSource = CreateDataSource();

            MixTable(dtSource, dtNhanVien, dtCa, startDate, stopDate, dic);

            grChiTiet.AutoGenerateColumns = false;
            grChiTiet.DataSource = dtSource;

            TimeSpan ts = stopDate - startDate;
            totalDays = (int)ts.TotalDays + 1;

            for (int i = 0; i < dtNhanVien.Rows.Count; i++)
            {
                for (int j = 0; j < dtCa.Rows.Count; j++)
                {
                    VMergedCell cell = (grChiTiet.Rows[i * dtCa.Rows.Count + j].Cells[0] as VMergedCell);
                    cell.BottomRow = (i + 1) * dtCa.Rows.Count - 1;
                    cell.TopRow = i * dtCa.Rows.Count;

                    for (int col = totalDays + 2; col < grChiTiet.Columns.Count; col++)
                    {
                        cell = (grChiTiet.Rows[i * dtCa.Rows.Count + j].Cells[col] as VMergedCell);
                        cell.BottomRow = (i + 1) * dtCa.Rows.Count - 1;
                        cell.TopRow = i * dtCa.Rows.Count;
                        cell.Numeric = true;
                    }
                }

                UpdateThucNhan(i);
            }
        }

        private void MixTable(DataTable dtSource, DataTable dtNhanVien, DataTable dtCa, DateTime startDate, DateTime stopDate, Dictionary<string, DataRow> dic)
        {
            foreach (DataRow rNhanVien in dtNhanVien.Rows)
            {
                foreach (DataRow rCa in dtCa.Rows)
                {
                    //create row
                    DataRow r = dtSource.NewRow();
                    r["CA"] = rCa["NAME"];
                    r["DCALAMVIECID"] = rCa["ID"];
                    r["TILELUONG"] = ConvertTo.Decimal(rCa["TILELUONG"]) == 0 ? 100 : ConvertTo.Decimal(rCa["TILELUONG"]);
                    for (DateTime date = startDate; date <= stopDate; date = date.AddDays(1))
                    {
                        string dateStr = date.ToString("ddMMyy");
                        string key = rNhanVien["DNHANVIENID"].ToString() + rCa["ID"].ToString() + dateStr;
                        if (dic.ContainsKey(key))
                        {
                            r[dateStr] = dic[key]["TRANGTHAI"];
                        }
                        else
                        {
                            r[dateStr] = 0;
                        }
                    }
                    foreach (DataColumn col in dtNhanVien.Columns)
                    {
                        r[col.ColumnName] = rNhanVien[col];
                    }
                    dtSource.Rows.Add(r);
                }
            }
        }

        private DataTable CreateDataSource()
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("TBANGLUONGTONGHOPID", typeof(string));
            dt.Columns.Add("DNHANVIENID", typeof(string));
            dt.Columns.Add("NHANVIEN", typeof(string));
            dt.Columns.Add("DCALAMVIECID", typeof(string));
            dt.Columns.Add("CA", typeof(string));
            for (DateTime date = startDate; date <= stopDate; date = date.AddDays(1))
            {
                dt.Columns.Add(date.ToString("ddMMyy"), typeof(int));
            }
            dt.Columns.Add("LUONGCOBAN", typeof(decimal));
            dt.Columns.Add("LUONGCA", typeof(decimal));
            dt.Columns.Add("LUONGTHANG", typeof(decimal));
            dt.Columns.Add("CACHTINHLUONG", typeof(int));
            dt.Columns.Add("NGHICHUNHAT", typeof(int));
            dt.Columns.Add("NGHITHU7", typeof(int));
            dt.Columns.Add("TONGLUONG", typeof(decimal));
            dt.Columns.Add("PHAT", typeof(decimal));
            dt.Columns.Add("TAMUNG", typeof(decimal));
            dt.Columns.Add("THUONG", typeof(decimal));
            dt.Columns.Add("THUCNHAN", typeof(decimal));
            dt.Columns.Add("TILELUONG", typeof(decimal));
            return dt;
        }

        private DataGridViewColumn AddMergedColumn(string header, string field, int width)
        {
            VMergedColumn column = new VMergedColumn();
            column.HeaderText = header;
            column.DataPropertyName = field;
            column.Width = width;
            grChiTiet.Columns.Add(column);
            column.DefaultCellStyle.Font = new Font(grChiTiet.Font, FontStyle.Bold);
            column.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            column.SortMode = DataGridViewColumnSortMode.NotSortable;
            return column;
        }

        private DataGridViewColumn AddTextColumn(string header, string field, int width)
        {
            DataGridViewTextBoxColumn column = new DataGridViewTextBoxColumn();
            column.HeaderText = header;
            column.DataPropertyName = field;
            column.Width = width;
            column.SortMode = DataGridViewColumnSortMode.NotSortable;
            column.DefaultCellStyle.SelectionBackColor = Color.FromArgb(183, 219, 255);
            column.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            grChiTiet.Columns.Add(column);
            return column;
        }

        private DataGridViewColumn AddColorColumn(string header, string field, int width)
        {
            ColorColumn column = new ColorColumn();
            column.HeaderText = header;
            column.DataPropertyName = field;
            column.SortMode = DataGridViewColumnSortMode.NotSortable;
            column.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            column.HeaderCell.Style.WrapMode = DataGridViewTriState.True;
            column.Width = width;
            grChiTiet.Columns.Add(column);
            return column;
        }

        private void AddColumns(DateTime startDate, DateTime stopDate)
        {
            DataGridViewColumn col = AddMergedColumn("Nhân viên", "NHANVIEN", 150);
            col.Frozen = true;
            AddTextColumn("Ca", "CA", 80).Frozen = true;

            tongNgayTruCN = 0;
            tongNgayTruT7CN = 0;
            for (DateTime date = startDate; date <= stopDate; date = date.AddDays(1))
            {
                string day = "";
                switch (date.DayOfWeek)
                {
                    case DayOfWeek.Monday: day = "T2"; break;
                    case DayOfWeek.Tuesday: day = "T3"; break;
                    case DayOfWeek.Wednesday: day = "T4"; break;
                    case DayOfWeek.Thursday: day = "T5"; break;
                    case DayOfWeek.Friday: day = "T6"; break;
                    case DayOfWeek.Saturday: day = "T7"; break;
                    case DayOfWeek.Sunday: day = "CN"; break;
                }

                if (date.DayOfWeek != DayOfWeek.Sunday)
                {
                    tongNgayTruCN++;
                    if (date.DayOfWeek != DayOfWeek.Saturday)
                    {
                        tongNgayTruT7CN++;
                    }
                }

                if (date.DayOfWeek != DayOfWeek.Saturday)
                {
                    tongNgayTruT7++;
                }

                AddColorColumn(date.Day.ToString() + Environment.NewLine + day, date.ToString("ddMMyy"), 27);
            }            
            AddMergedColumn("Lương cơ bản", "LUONGCOBAN", 80).Visible = showLuong;
            AddMergedColumn("Tổng lương", "TONGLUONG", 80).Visible = showLuong;
            AddMergedColumn("Thưởng", "THUONG", 80).Visible = showLuong;
            AddMergedColumn("Phạt", "PHAT", 80).Visible = showLuong;
            AddMergedColumn("Tạm ứng", "TAMUNG", 80).Visible = showLuong;
            AddMergedColumn("Thực nhận", "THUCNHAN", 80).Visible = showLuong;
        }

        private void UpdateThucNhan(int RowIndex)
        {
            decimal luongCa = ConvertTo.Decimal(dtNhanVien.Rows[RowIndex]["LUONGCA"]);
            decimal soCaDiLam = 0;
            decimal soNgayDiLam = 0;
            decimal phat = ConvertTo.Decimal(dtNhanVien.Rows[RowIndex]["PHAT"]);
            decimal thuong = ConvertTo.Decimal(dtNhanVien.Rows[RowIndex]["THUONG"]);
            decimal tamUng = ConvertTo.Decimal(dtNhanVien.Rows[RowIndex]["TAMUNG"]);

            decimal luongThang = ConvertTo.Decimal(dtNhanVien.Rows[RowIndex]["LUONGTHANG"]);
            int cachTinhLuong = ConvertTo.Int(dtNhanVien.Rows[RowIndex]["CACHTINHLUONG"]);
            int NGHICHUNHAT = ConvertTo.Int(dtNhanVien.Rows[RowIndex]["NGHICHUNHAT"]);
            int NGHITHU7 = ConvertTo.Int(dtNhanVien.Rows[RowIndex]["NGHITHU7"]);

            for (int i = 2; i < 3 + totalDays; i++)
            {
                bool hasDiLam = false;
                for (int j = 0; j < dtCa.Rows.Count; j++)
                {
                    decimal tiLeLuong = ConvertTo.Decimal(dtCa.Rows[j]["TILELUONG"]);

                    int value = ConvertTo.Int(grChiTiet.Rows[RowIndex * dtCa.Rows.Count + j].Cells[i].Value);
                    if (value == (int)DiLamMode.DiLam)
                    {
                        soCaDiLam += tiLeLuong / 100;
                        hasDiLam = true;
                    }
                    else if (value == (int)DiLamMode.NghiCoPhep)
                    {
                        soCaDiLam += tiLeLuong / 100;
                        hasDiLam = true;
                    }
                    else if (value == (int)DiLamMode.NghiKhongPhep)
                    {
                        //phat += luongCa;
                    }
                }
                if (hasDiLam) soNgayDiLam++;
            }

            TBANGLUONGTONGHOPRow rTongHop = new TBANGLUONGTONGHOPRow(dtNhanVien.Rows[RowIndex]);

            decimal tongLuong = 0;

            if (cachTinhLuong == (int)CachTinhLuong.TheoCa)
            {
                tongLuong = luongCa * soCaDiLam;
            }
            else if (cachTinhLuong == (int)CachTinhLuong.ThangTheoCa)
            {
                tongLuong = soCaDiLam * luongThang / totalDays;
            }
            else
            {
                decimal soNgayTrongThang = totalDays;
                if (NGHICHUNHAT == 30)
                {
                    if (NGHITHU7 == 30)
                    {
                        soNgayTrongThang = tongNgayTruT7CN;
                    }
                    else
                    {
                        soNgayTrongThang = tongNgayTruCN;
                    }
                }
                else
                {
                    if (NGHITHU7 == 30)
                    {
                        soNgayTrongThang = tongNgayTruT7;
                    }
                    else
                    {
                        soNgayTrongThang = totalDays;
                    }
                }
                tongLuong = soNgayDiLam * luongThang / soNgayTrongThang;
            }

            rTongHop.TONGLUONG = tongLuong;
            decimal thucNhan = tongLuong + thuong - phat - rTongHop.TAMUNG;
            rTongHop.THUCNHAN = thucNhan;
            DataTable dtSource = grChiTiet.DataSource as DataTable;
            for (int i = 0; i < dtCa.Rows.Count; i++)
            {
                DataRow updateRow = dtSource.Rows[RowIndex * dtCa.Rows.Count + i];
                updateRow["TONGLUONG"] = tongLuong;
                updateRow["THUCNHAN"] = thucNhan;
            }
        }


		public void btnKhongLich_Click(Object sender, EventArgs e)
		{
            SetSelectionColor(DiLamMode.KhongCoLich);
		}

        private void SetSelectionColor(DiLamMode value)
        {
            int val = (int)value;
            List<int> lst = new List<int>();
            foreach (DataGridViewCell cell in grChiTiet.SelectedCells)
            {
                if (cell is ColorCell)
                {
                    cell.Value = val;
                    if (!lst.Contains(cell.RowIndex)) lst.Add((int)(cell.RowIndex / dtCa.Rows.Count));
                }
            }

            foreach (int rowIndex in lst) UpdateThucNhan(rowIndex);

            grChiTiet.Refresh();
        }

		public void btnDiLam_Click(Object sender, EventArgs e)
		{
            SetSelectionColor(DiLamMode.DiLam);
		}


		public void btnNghiCoPhep_Click(Object sender, EventArgs e)
		{
            SetSelectionColor(DiLamMode.NghiCoPhep);
		}


		public void btnNghiKhongPhep_Click(Object sender, EventArgs e)
		{
            SetSelectionColor(DiLamMode.NghiKhongPhep);
		}


		public void btnRefresh_Click(Object sender, EventArgs e)
		{
            if (Msg.ShowYesNo("Hệ thống sẽ cập nhật giá trị lương hiện tại của nhân viên để tính lương" + Environment.NewLine +
                              "Bạn có muốn thực hiện không?") == DialogResult.Yes)
            {
                RefreshData("");
                Msg.ShowInfo("Đã tải lại thành công");
            }
		}

        private void RefreshData(string ID)
        {
            string sql = "SELECT ID, LUONGCA, LUONGTHANG, CACHTINHLUONG, NGHICHUNHAT, NGHITHU7," +
                         "(SELECT COALESCE(SUM(PHAT), 0) FROM TTHUONGPHAT WHERE TTHUONGPHAT.DNHANVIENID =  DNHANVIEN.ID AND NGAY BETWEEN @FromDate AND @ToDate) AS PHAT," +
                         "(SELECT COALESCE(SUM(THUONG), 0) FROM TTHUONGPHAT WHERE TTHUONGPHAT.DNHANVIENID =  DNHANVIEN.ID AND NGAY BETWEEN @FromDate AND @ToDate) AS THUONG," +
                         "(SELECT CASE WHEN SUM(CHI) IS NULL THEN 0 ELSE SUM(CHI) END FROM TTHUCHI WHERE DNHANVIENID = DNHANVIEN.ID AND NGAY BETWEEN @FromDate AND @ToDate AND LATAMUNG = 30) AS TAMUNG " +
                         " FROM DNHANVIEN";
            if (ID.Length > 0) sql += " WHERE ID = '" + ID + "'";
            FbCommand cmd = Config.Db.GetCommand(sql);
            cmd.Parameters.Add("@FromDate", startDate);
            cmd.Parameters.Add("@ToDate", stopDate);
            DataTable dt = Config.Db.GetTable(cmd);
            DataTable dtSource = grChiTiet.DataSource as DataTable;
            foreach (DataRow r in dtNhanVien.Rows)
            {
                string DNHANVIENID = r["DNHANVIENID"].ToString();
                DataRow[] rows = dt.Select("ID='" + DNHANVIENID + "'");
                if (rows.Length > 0)
                {
                    DataRow dataRow = rows[0];
                    r["LUONGCA"] = dataRow["LUONGCA"];
                    r["LUONGTHANG"] = dataRow["LUONGTHANG"];
                    r["CACHTINHLUONG"] = dataRow["CACHTINHLUONG"];
                    r["NGHICHUNHAT"] = dataRow["NGHICHUNHAT"];
                    r["NGHITHU7"] = dataRow["NGHITHU7"];
                    r["THUONG"] = dataRow["THUONG"];
                    r["PHAT"] = dataRow["PHAT"];
                    r["TAMUNG"] = dataRow["TAMUNG"];
                    r["LUONGCOBAN"] = ConvertTo.Int(dataRow["CACHTINHLUONG"]) == 30 ? dataRow["LUONGCA"] : dataRow["LUONGTHANG"];
                }
            }

            foreach (DataRow r in dtSource.Rows)
            {
                string DNHANVIENID = r["DNHANVIENID"].ToString();
                DataRow[] rows = dt.Select("ID='" + DNHANVIENID + "'");
                if (rows.Length > 0)
                {
                    DataRow dataRow = rows[0];
                    r["LUONGCA"] = dataRow["LUONGCA"];
                    r["LUONGTHANG"] = dataRow["LUONGTHANG"];
                    r["CACHTINHLUONG"] = dataRow["CACHTINHLUONG"];
                    r["NGHICHUNHAT"] = dataRow["NGHICHUNHAT"];
                    r["NGHITHU7"] = dataRow["NGHITHU7"];
                    r["THUONG"] = dataRow["THUONG"];
                    r["PHAT"] = dataRow["PHAT"];
                    r["TAMUNG"] = dataRow["TAMUNG"];
                    r["LUONGCOBAN"] = ConvertTo.Int(dataRow["CACHTINHLUONG"]) == 30 ? dataRow["LUONGCA"] : dataRow["LUONGTHANG"];
                }
            }

            for (int i = 0; i < dtNhanVien.Rows.Count; i++) UpdateThucNhan(i);
            grChiTiet.Refresh();
        }


		public void btnChiLuong_Click(Object sender, EventArgs e)
		{
            ChiLuong form = (ChiLuong)Config.CreateForm(Forms.ChiLuong);
            form.SetData(SelID);
            form.form.ShowDialog();
		}

        WaitingForm waitingForm;
		public void btnCapNhat_Click(Object sender, EventArgs e)
		{
            waitingForm = new WaitingForm();
            waitingForm.worker.DoWork += new DoWorkEventHandler(workerSave_DoWork);
            waitingForm.worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(worker_RunWorkerCompleted);
            waitingForm.ShowDialog();
		}

        void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            waitingForm.Close();
        }

        void workerSave_DoWork(object sender, DoWorkEventArgs e)
        {
            //ghi vao co so du lieu
            int i = 0;
            int total = dtNhanVien.Rows.Count * dtCa.Rows.Count;
            foreach (DataRow rNhanVien in dtNhanVien.Rows)
            {
                int startRow = i * dtCa.Rows.Count;
                int j = 0;
                TBANGLUONGTONGHOPRow oldRow = new TBANGLUONGTONGHOPRow(rNhanVien);
                //cap nhat tong hop
                string TBANGLUONGTONGHOPID = rNhanVien["TBANGLUONGTONGHOPID"].ToString();
                TBANGLUONGTONGHOPRow thRow = TBANGLUONGTONGHOPID.Length == 0 ? new TBANGLUONGTONGHOPRow() : new TBANGLUONGTONGHOPRow(TBANGLUONGTONGHOPID);
                thRow.DNHANVIENID = oldRow.DNHANVIENID;
                thRow.LUONGCA = oldRow.LUONGCA;
                thRow.THUCNHAN = oldRow.THUCNHAN;
                thRow.TONGLUONG = oldRow.TONGLUONG;
                thRow.TAMUNG = oldRow.TAMUNG;
                thRow.LUONGTHANG = oldRow.LUONGTHANG;
                thRow.CACHTINHLUONG = oldRow.CACHTINHLUONG;
                thRow.PHAT = oldRow.PHAT;
                thRow.THUONG = oldRow.THUONG;
                thRow.TBANGLUONGID = SelID;
                thRow.Update();
                if (TBANGLUONGTONGHOPID.Length == 0)
                    rNhanVien["TBANGLUONGTONGHOPID"] = thRow.ID;
                foreach (DataRow rCa in dtCa.Rows)
                {
                    //duyet qua 30 ngay trong thang
                    int k = 0;
                    waitingForm.worker.ReportProgress(100 * (startRow + j) / total);
                    for (DateTime date = startDate; date <= stopDate; date = date.AddDays(1))
                    {
                        string key = rNhanVien["DNHANVIENID"].ToString() + rCa["ID"].ToString() + date.ToString("ddMMyy");
                        int val = 0;
                        DiLamMode mode = (DiLamMode)ConvertTo.Int(grChiTiet.Rows[startRow + j].Cells[2 + k].Value);
                        if (mode == DiLamMode.DiLam)
                        {
                            val = 1;
                        }
                        else if (mode == DiLamMode.NghiCoPhep)
                        {
                            val = 2;
                        }
                        else if (mode == DiLamMode.NghiKhongPhep)
                        {
                            val = 3;
                        }
                        if (dic.ContainsKey(key))
                        {
                            string ID = dic[key]["ID"].ToString();
                            TBANGLUONGCHITIETRow upRow = new TBANGLUONGCHITIETRow(ID);
                            upRow.TRANGTHAI = val;
                            upRow.Update();
                        }
                        else
                        {
                            if (val != 0)
                            {
                                TBANGLUONGCHITIETRow upRow = new TBANGLUONGCHITIETRow();
                                upRow.TBANGLUONGID = SelID;
                                upRow.DNHANVIENID = rNhanVien["DNHANVIENID"].ToString();
                                upRow.DCALAMVIECID = rCa["ID"].ToString();
                                upRow.NGAY = date;
                                upRow.TRANGTHAI = val;
                                upRow.Update();

                                rNhanVien["TBANGLUONGTONGHOPID"] = upRow.ID;
                                TBANGLUONGCHITIETRow ctRow = new TBANGLUONGCHITIETRow(upRow.ID);
                                dic.Add(ctRow.DNHANVIENID.ToString() + ctRow.DCALAMVIECID.ToString() + ctRow.NGAY.ToString("ddMMyy"), ctRow.Row);
                            }
                        }

                        k++;
                    }
                    j++;
                }
                i++;
            }
        }

        string tTinhLuongDelID;
		public void tmrLoadDetail_Tick(Object sender, EventArgs e)
		{
            tmrLoadDetail.Enabled = false;

            if (SelID != null && SelID.Length > 0)
            {
                kryptonPanel1.Enabled = true;
                LoadDetail(SelID);
            }
            else
            {
                if (grChiTiet != null && !grChiTiet.IsDisposed) grChiTiet.Visible = false;
                kryptonPanel1.Enabled = false;
            }
		}
    }

    enum DiLamMode
    {
        KhongCoLich = 0,
        DiLam = 1,
        NghiCoPhep = 2,
        NghiKhongPhep = 3
    }

    enum CachTinhLuong
    {
        TheoThang = 0,
        TheoCa = 30,
        ThangTheoCa = 60
    }
}
