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
    public partial class DuLieuTheBanDau
    {
        DataTable dt;
        Dictionary<string, string> dicLoaiThe;
        Dictionary<string, string> dicCaTap;
        bool coPhanCaTap;
        bool coTheoLan;
        public void No1Form1_OnInit(object sender, EventArgs e)
        {
            coPhanCaTap = SystemConfig.CoPhanCaTap == 30;
            coTheoLan = SystemConfig.CoSuDungTheTheoLan == 30;

            dicLoaiThe = new Dictionary<string, string>();
            DataTable dtLoaiThe = Config.Db.GetTable("SELECT ID, NAME FROM DLOAITHE");
            foreach (DataRow r in dtLoaiThe.Rows)
            {
                dicLoaiThe.Add(r["ID"].ToString(), r["NAME"].ToString());
            }

            colCaTap.TableName = Tables.DCATAP;
            colLoaiThe.TableName = Tables.DLOAITHE;

            dicCaTap = new Dictionary<string, string>();
            DataTable dtCaTap = Config.Db.GetTable("SELECT ID, NAME FROM DCATAP");
            foreach (DataRow r in dtCaTap.Rows)
            {
                dicCaTap.Add(r["ID"].ToString(), r["NAME"].ToString());
            }

            //lấy danh sách khách hàng ra
            string sql = @"SELECT DKHACHHANG.ID AS DKHACHHANGID, TGIAHANTHE.ID AS TGIAHANTHEID, DKHACHHANG.NAME, MAKHACH, 
DKHACHHANG.DIACHI, DKHACHHANG.DIENTHOAI, TGIAHANTHE.TUNGAY, TGIAHANTHE.DENNGAY, TGIAHANTHE.SOLAN, TGIAHANTHE.DLOAITHEID,
TGIAHANTHE.DCATAPID
FROM DKHACHHANG LEFT OUTER JOIN TGIAHANTHE ON DKHACHHANG.ID = TGIAHANTHE.DKHACHHANGID 
AND DLOAIGIAODICHID = '5'
WHERE DKHACHHANG.STATUS = 30";

            dt = Config.Db.GetTable(sql);
            grMain.LoadDataSearchable(dt);

            if (!coPhanCaTap)
            {
                colCaTap.Visible = false;
            }

            if (!coTheoLan)
            {
                colSoLan.Visible = false;
            }

            if (Control.ModifierKeys == (Keys.Shift | Keys.Control))
            {
            }
            else
            {
                //kiểm tra xem có giao dịch nào khác chưa? Nếu có thì khóa lại không cho vào dữ liệu ban đầu
                int SoGiaoDich = Config.Db.GetFirstFieldInt("SELECT COUNT(*) FROM TGIAHANTHE WHERE DLOAIGIAODICHID <> '5'");
                if (SoGiaoDich > 0)
                {
                    Msg.ShowWarning("Chức năng dữ liệu ban đầu chỉ sử dụng khi chưa có phát sinh giao dịch đăng ký mới, gia hạn thẻ...");
                    grMain.Enabled = false;
                    ToolStrip1.Enabled = false;
                    btnXoaTatCa.Enabled = false;
                    dtNgay.Enabled = false;
                }
            }
        }

		public void tsbImportExcel_Click(object sender, EventArgs e)
		{
            StringBuilder errors = new StringBuilder();

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
                lst.Add("Mã thẻ");
                lst.Add("Loại thẻ");
                lst.Add("Từ ngày");
                lst.Add("Đến ngày");
                if (coPhanCaTap)
                    lst.Add("Ca tập");
                if (coTheoLan)
                    lst.Add("Số lần còn lại");
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
                    foreach (string field in lst)
                    {
                        if (!dic.ContainsKey(field))
                        {
                            Msg.ShowWarning("Bạn chưa chọn cột '" + field + "'");
                            return;
                        }
                    }

                    //lấy ra loại thẻ
                    Dictionary<string, string> dicThe = new Dictionary<string,string>();
                    DataTable dtLoaiThe = Config.Db.GetTable("SELECT ID, NAME FROM DLOAITHE");
                    foreach (DataRow r in dtLoaiThe.Rows)
                    {
                        dicThe.Add(r["NAME"].ToString(), r["ID"].ToString());
                    }

                    Dictionary<string, string> dicCa = new Dictionary<string, string>();
                    DataTable dtCa = Config.Db.GetTable("SELECT ID, NAME FROM DCATAP");
                    foreach (DataRow r in dtCa.Rows)
                    {
                        dicCa.Add(r["NAME"].ToString(), r["ID"].ToString());
                    }

                    //checking for validating
                    for (int row = 1; row < sh.Cells.MaxRow + 1; row++)
                    {
                        int maHangCol = dic["Mã thẻ"];
                        string maHang = ConvertTo.String(sh.Cells[row, maHangCol].Value).Trim();
                        if (maHang.Length > 0)
                        {
                            DataRow[] rows = dt.Select("MAKHACH='" + maHang + "'");
                            if (rows.Length == 0)
                            {
                                errors.AppendLine("Mã đối tác '" + maHang + "' không tồn tại");
                            }
                            else
                            {
                                DataRow r = rows[0];
                                //đưa các thông tin khác vào
                                UpThongTin(sh, row, dic, "Loại thẻ", "DLOAITHEID", dicThe, r);
                                UpThongTin(sh, row, dic, "Từ ngày", "TUNGAY", null, r);
                                UpThongTin(sh, row, dic, "Đến ngày", "DENNGAY", null, r);
                                if (coPhanCaTap)
                                    UpThongTin(sh, row, dic, "Ca tập", "DCATAPID", dicCa, r);
                                if (coTheoLan)
                                    UpThongTin(sh, row, dic, "Số lần còn lại", "SOLAN", null, r);
                            }
                        }
                    }

                    if (errors.Length > 0)
                    {
                        Msg.ShowWarning(errors.ToString());
                    }
                }                
            }
		}

        private void UpThongTin(Worksheet sh, int row, Dictionary<string, int> dic, string caption, string field, Dictionary<string, string> dicNameId, DataRow r)
        {
            if (!dic.ContainsKey(caption)) return;
            int colIndex = dic[caption];
            //loại thẻ
            try
            {
                if (dicNameId != null)
                {
                    if (sh.Cells[row, colIndex].Type == CellValueType.IsString)
                    {
                        string Name = sh.Cells[row, colIndex].StringValue;
                        if (dicNameId.ContainsKey(Name))
                        {
                            r[field] = dicNameId[Name];
                        }
                    }
                }
                else
                {
                    if (caption == "Số lần còn lại")
                    {
                        if (sh.Cells[row, colIndex].Type == CellValueType.IsNumeric)
                        {
                            r[field] = sh.Cells[row, colIndex].FloatValue;
                        }
                    }
                    else
                    {
                        if (sh.Cells[row, colIndex].Type == CellValueType.IsDateTime)
                        {
                            r[field] = sh.Cells[row, colIndex].DateTimeValue;
                        }
                    }
                }
            }
            catch
            {
            }
        }


		public void tsbExport_Click(object sender, EventArgs e)
		{
            SaveFileDialog saveDlg = new SaveFileDialog();
            saveDlg.Filter = "Excel file (*.xls, *.xlsx)|*.xls;*.xlsx";
            if (saveDlg.ShowDialog() == DialogResult.OK)
            {
                Workbook workbook = new Workbook();
                Worksheet worksheet = workbook.Worksheets[0];
                worksheet.Cells[0, 0].Value = "Mã thẻ";
                worksheet.Cells[0, 1].Value = "Tên khách";                
                worksheet.Cells[0, 2].Value = "Loại thẻ";                
                worksheet.Cells[0, 3].Value = "Từ ngày";
                worksheet.Cells[0, 4].Value = "Đến ngày";
                if (coPhanCaTap)
                    worksheet.Cells[0, 5].Value = "Ca tập";
                if (coTheoLan)
                    worksheet.Cells[0, 6 - (coPhanCaTap ? 0 : 1)].Value = "Số lần còn lại";                
                int i = 1;
                
                foreach (DataRow r in dt.Rows)
                {
                    worksheet.Cells[i, 0].Value = r["MAKHACH"].ToString();
                    worksheet.Cells[i, 1].Value = r["NAME"].ToString();
                    string DLOAITHEID = r["DLOAITHEID"].ToString();
                    worksheet.Cells[i, 2].Value = dicLoaiThe.ContainsKey(DLOAITHEID) ? dicLoaiThe[DLOAITHEID] : "";
                    if (r["TUNGAY"] != DBNull.Value)
                        worksheet.Cells[i, 3].Value = ConvertTo.Date(r["TUNGAY"]);
                    if (r["DENNGAY"] != DBNull.Value)
                        worksheet.Cells[i, 4].Value = ConvertTo.Date(r["DENNGAY"]);
                    if (coPhanCaTap)
                    {
                        string DCATAPID = r["DCATAPID"].ToString();
                        worksheet.Cells[i, 5].Value = dicCaTap.ContainsKey(DCATAPID) ? dicCaTap[DCATAPID] : "";
                    }
                    if (coTheoLan)
                    {
                        worksheet.Cells[i, 6 - (coPhanCaTap ? 0 : 1)].Value = ConvertTo.Int(r["SOLAN"]);
                    }
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


		public void txtLoc_TextChanged(object sender, EventArgs e)
		{
            grMain.Filter = txtLoc.Text;
		}

        bool daThayDoiNgay = false;
		public void btnOK_Click(object sender, EventArgs e)
		{
            if (dtNgay.EditValue == null)
            {
                Msg.ShowWarning("Mời bạn nhập ngày chốt dữ liệu ban đầu");
                return;
            }

            //kiểm tra xem có nhập đầy đủ thông tin không?
            List<string> fields = new List<string>();
            fields.Add("DLOAITHEID");
            if (coPhanCaTap)
                fields.Add("DCATAPID");            
            fields.Add("TUNGAY");
            fields.Add("DENNGAY");
            foreach (DataGridViewRow r in grMain.Rows)
            {
                DataRow row = (r.DataBoundItem as DataRowView).Row;
                if (!IsValidRow(row, fields))
                {
                    Msg.ShowWarning("Mời bạn nhập đầy đủ các thông tin hoặc để trống tất cả");
                    grMain.ClearSelection();
                    r.Selected = true;
                    if (!r.Displayed) grMain.FirstDisplayedScrollingRowIndex = r.Index;
                    return;
                }
                //kiểm tra thẻ có theo lần không?
                if (coTheoLan)
                {
                    decimal soLan = ConvertTo.Decimal(row["SOLAN"]);
                    if (soLan == 0)
                    {
                        string DLOAITHEID = row["DLOAITHEID"].ToString();
                        DLOAITHERow theRow = new DLOAITHERow(DLOAITHEID);
                        if (theRow.SOLAN > 0)
                        {
                            Msg.ShowWarning("Mời bạn nhập số lần còn lại");
                            grMain.ClearSelection();
                            r.Selected = true;
                            if (!r.Displayed) grMain.FirstDisplayedScrollingRowIndex = r.Index;
                            return;
                        }
                    }
                }
            }

            foreach (DataRow r in dt.Rows)
            {
                string TGIAHANTHEID = r["TGIAHANTHEID"].ToString();
                string DLOAITHEID = r["DLOAITHEID"].ToString();
                if (DLOAITHEID.Length > 0)
                {
                    //tạo mới
                    TGIAHANTHERow uRow = null;
                    bool needUpdate = false;
                    if (TGIAHANTHEID.Length == 0)
                    {
                        needUpdate = true;
                        uRow = new TGIAHANTHERow();
                    }
                    else
                    {
                        if (r.RowState == DataRowState.Modified || daThayDoiNgay)
                        {
                            //cập nhật
                            needUpdate = true;
                            uRow = new TGIAHANTHERow(TGIAHANTHEID);
                        }
                    }

                    if (needUpdate)
                    {
                        uRow.DKHACHHANGID = r["DKHACHHANGID"].ToString();
                        uRow.LOAI = 99;
                        uRow.DLOAIGIAODICHID = LoaiGiaoDichIds.TheBanDau;
                        uRow.NGAY = dtNgay.DateTime;
                        uRow.TONGCONG = 0;
                        uRow.DATAP = 0;
                        uRow.DCATAPID = r["DCATAPID"].ToString();
                        uRow.NAME = "DULIEUDAU";
                        uRow.DENNGAYTHUC = ConvertTo.Date(r["DENNGAY"]);
                        uRow.DENNGAY = ConvertTo.Date(r["DENNGAY"]);
                        uRow.DLOAITHEID = DLOAITHEID;
                        uRow.GIAMTHEOTIEN = 0;
                        uRow.LANTANGTHEM = 0;
                        uRow.NGAYTANGTHEM = 0;
                        uRow.SOLAN = ConvertTo.Int(r["SOLAN"]);
                        uRow.SONGAY = 0;
                        uRow.SOTHANG = 0;
                        uRow.SOTIEN = 0;
                        uRow.THANHTOAN = 0;
                        uRow.TIENGIAMGIA = 0;
                        uRow.TILEGIAMGIA = 0;
                        uRow.TONGCONG = 0;
                        uRow.TUNGAY = ConvertTo.Date(r["TUNGAY"]);
                        uRow.Update();
                    }
                }
                else
                {
                    //xóa phiếu
                    if (TGIAHANTHEID.Length > 0)
                    {
                        Config.Db.ExecSql("DELETE FROM TGIAHANTHE WHERE ID = '" + TGIAHANTHEID + "'");
                    }
                }
            }

            //cập nhật lại tất cả trạng thái
            TGIAHANTHE0Ae.CapNhatThongTinKhachHang("");

            if (Msg.ShowYesNo("Bạn có muốn thực hiện cập nhật mã thẻ lên máy không?") == DialogResult.Yes)
            {
                DayThongTinThe form = (DayThongTinThe)Config.CreateForm(Forms.DayThongTinThe);
                form.No1Form1_OnInit(null, null);
                form.No1Form1.ShowDialog();
            }

            No1Form1.DialogResult = DialogResult.OK;
		}

        private bool IsValidRow(DataRow row, List<string> fields)
        {
            //kiem tra xem tat ca cac truong co trong khong?
            bool bEmpty = true;
            foreach (string field in fields)
            {
                string val = row[field].ToString();
                if (val.Length > 0 && val != "0")
                {                    
                    bEmpty = false;
                    break;                    
                }
            }

            //nếu một trong các ô có dữ liệu
            if (!bEmpty)
            {
                //kiểm tra tất cả các ô phải nhập liệu
                foreach (string field in fields)
                {
                    string val = row[field].ToString();
                    if (val.Length == 0 || val == "0")
                    {
                        return false;
                    }
                }
            }

            return true;
        }       


		public void dtNgay_OnEditValueChanged(object sender, object value)
		{
            daThayDoiNgay = true;
		}


		public void btnXoaTatCa_Click(object sender, EventArgs e)
		{
            if (Msg.ShowYesNo("Bạn có muốn xóa tất cả dữ liệu ban đầu và thực hiện lại không?" + Environment.NewLine +
                "Bạn không thể khôi phục dữ liệu sau khi xóa") == DialogResult.Yes)
            {
                Config.Db.ExecSql("DELETE FROM TGIAHANTHE WHERE DLOAIGIAODICHID = '" + LoaiGiaoDichIds.TheBanDau + "'");                
                TGIAHANTHE0Ae.CapNhatThongTinKhachHang("");
                Msg.ShowInfo("Đã thực hiện xóa xong");
                No1Form1.Close();
            }
		}
    }
}
