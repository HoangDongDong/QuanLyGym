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
    public partial class CongNoBanDau
    {
        private bool khachHang;

        public void SetData(bool khachHang)
        {            
            this.khachHang = khachHang;
            dtNgay.DateTime = new DateTime(Config.Db.DbDate.Year, 1, 1).AddDays(-1);
            LoadData();
            daThayDoiNgay = false;
        }

        DataTable dt;
        int CONGNODAU = 99;
        private void LoadData()
        {
            CONGNODAU = khachHang ? 99 : 98;
            string sql = @"SELECT DKHACHHANG.ID AS DKHACHHANGID, TDONHANG.ID AS TDONHANGID, DKHACHHANG.NAME, MAKHACH AS CODE, DKHACHHANG.DIACHI, DKHACHHANG.DIENTHOAI, COALESCE(TONGCONG, 0) AS SOTIEN 
                FROM DKHACHHANG LEFT OUTER JOIN TDONHANG ON DKHACHHANG.ID = TDONHANG.DKHACHHANGID AND TDONHANG.LOAI = " + CONGNODAU.ToString() + " AND DKHACHHANG.STATUS = 30";
            if (!khachHang)
            {
                sql = @"SELECT DNHACUNGCAP.ID AS DNHACUNGCAPID, TDONHANG.ID AS TDONHANGID, DNHACUNGCAP.NAME, MANHACUNGCAP AS CODE, DNHACUNGCAP.DIACHI, DNHACUNGCAP.DIENTHOAI, COALESCE(TONGCONG, 0) AS SOTIEN 
                FROM DNHACUNGCAP LEFT OUTER JOIN TDONHANG ON DNHACUNGCAP.ID = TDONHANG.DNHACUNGCAPID AND TDONHANG.LOAI = 98 AND DNHACUNGCAP.STATUS = 30";
            }
            dt = Config.Db.GetTable(sql);

            grMain.LoadDataSearchable(dt);
        }

		public void txtLoc_TextChanged(object sender, EventArgs e)
		{
            grMain.Filter = txtLoc.Text;
		}

		public void tsbFileMau_Click(object sender, EventArgs e)
		{
            SaveFileDialog saveDlg = new SaveFileDialog();
            saveDlg.Filter = "Excel file (*.xls, *.xlsx)|*.xls;*.xlsx";
            if (saveDlg.ShowDialog() == DialogResult.OK)
            {
                Workbook workbook = new Workbook();
                Worksheet worksheet = workbook.Worksheets[0];
                worksheet.Cells[0, 0].Value = "Mã";
                worksheet.Cells[0, 1].Value = "Tên";
                worksheet.Cells[0, 2].Value = "Công nợ đầu";
                int i = 1;
                foreach (DataRow r in dt.Rows)
                {
                    worksheet.Cells[i, 0].Value = r["CODE"].ToString();
                    worksheet.Cells[i, 1].Value = r["NAME"].ToString();
                    worksheet.Cells[i, 2].Value = 0;
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
                lst.Add("Mã đối tác");
                lst.Add("Công nợ đầu");
                ChonCotExcel form = (ChonCotExcel) Config.CreateForm(Forms.ChonCotExcel);
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
                    if (!dic.ContainsKey("Mã đối tác"))
                    {
                        Msg.ShowWarning("Bạn chưa chọn cột mã đối tác");
                        return;
                    }

                    if (!dic.ContainsKey("Công nợ đầu"))
                    {
                        Msg.ShowWarning("Bạn chưa chọn cột Công nợ đầu");
                        return;
                    }

                    //checking for validating
                    for (int row = 1; row < sh.Cells.MaxRow + 1; row++)
                    {
                        int maHangCol = dic["Mã đối tác"];
                        string maHang = ConvertTo.String(sh.Cells[row, maHangCol].Value).Trim();
                        if (maHang.Length > 0)
                        {
                            DataRow[] rows = dt.Select("CODE='" + maHang + "'");
                            if (rows.Length == 0)
                            {
                                Msg.ShowWarning("Mã đối tác '" + maHang + "' không tồn tại");
                            }
                            else
                            {

                                try
                                {
                                    object val = sh.Cells[row, dic["Công nợ đầu"]].Value;
                                    rows[0]["SOTIEN"] = ConvertTo.Decimal(val);
                                }
                                catch
                                {
                                    rows[0]["SOTIEN"] = 0;
                                }
                            }
                        }
                    }
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

            foreach (DataRow r in dt.Rows)
            {
                decimal SoTien = ConvertTo.Decimal(r["SOTIEN"]);
                string TDONHANGID = r["TDONHANGID"].ToString();
                if (SoTien > 0)
                {
                    if (TDONHANGID.Length == 0)
                    {
                        //tạo mới
                        TDONHANGRow uRow = new TDONHANGRow(TDONHANGID);
                        if (khachHang)
                            uRow.DKHACHHANGID = r["DKHACHHANGID"].ToString();
                        else
                            uRow.DNHACUNGCAPID = r["DNHACUNGCAPID"].ToString();
                        uRow.LOAI = CONGNODAU;                        
                        uRow.NGAY = dtNgay.DateTime;
                        uRow.DATHANHTOAN = 30;
                        uRow.DIENGIAI = "Công nợ ban đầu khi dùng phần mềm";                        
                        uRow.TONGCONG = SoTien;
                        uRow.TILETHUE = 0;                        
                        uRow.TIENHANG = SoTien;                        
                        uRow.NAME = "CONGNODAU";
                        uRow.Update();
                    }
                    else
                    {
                        if (r.RowState == DataRowState.Modified || daThayDoiNgay)
                        {
                            //cập nhật
                            TDONHANGRow uRow = new TDONHANGRow(TDONHANGID);
                            uRow.TONGCONG = SoTien;                            
                            uRow.NGAY = dtNgay.DateTime;
                            uRow.Update();
                        }
                    }
                }
                else if (SoTien == 0)
                {
                    //xóa phiếu
                    if (TDONHANGID.Length > 0)
                    {
                        Config.Db.ExecSql("DELETE FROM TDONHANG WHERE ID = '" + TDONHANGID + "'");
                    }
                }
            }
            form.DialogResult = DialogResult.OK;
		}

        bool daThayDoiNgay = false;
		public void dtNgay_OnEditValueChanged(object sender, object value)
		{
            daThayDoiNgay = true;
		}


		public void form_Load(object sender, EventArgs e)
		{
			
		}


		public void form_OnInit(object sender, EventArgs e)
		{
            string CONG_NO_NCC_ID = "3771b8bf-e417-4162-850a-0394867a9bee";
            SetData(form.CallerMenuID.ID != CONG_NO_NCC_ID);
		}
    }
}
