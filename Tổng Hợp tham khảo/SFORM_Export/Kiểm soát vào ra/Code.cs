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
using AxZKFPEngXControl;
using System.Runtime.InteropServices;

namespace No1Run
{
    public partial class KiemSoatVaoRa
    {
        private AxZKFPEngX zkPrinter;        
        private int dbHandle = -1;

        public static KiemSoatVaoRa kiemSoatVaoRa;

		public void txtMaThe_KeyPress(object sender, KeyPressEventArgs e)
		{
            if (e.KeyChar == (int)Keys.Enter)
            {
                btnNhap.PerformClick();
            }
		}

		public void btnNhap_Click(object sender, EventArgs e)
		{
            LoadCustomer(txtMaThe.Text, "", "");
            txtMaThe.Text = "";
		}

		public void btnRefresh_Click(object sender, EventArgs e)
		{
			grChiTiet.LoadData();
		}

        private string currentID = "";
        
        private delegate void LoadCustomerHandler(string maThe, string tenMay, string mayID);
        private void LoadCustomer(string maThe, string tenMay, string mayID)
        {            
            if (No1UserControl1.InvokeRequired)
            {
                No1UserControl1.Invoke(new LoadCustomerHandler(LoadCustomer), new object[] { maThe, tenMay, mayID });
            }
            else
            {
                lblMay.Text = tenMay;
                //kiểm tra xem có mã thẻ trong hệ thống không?
                string sql = "SELECT * FROM DKHACHHANG WHERE MAKHACH = '" + maThe.Replace("'", "''") + "'";
                DataRow r = Config.Db.GetFirstRow(sql);
                if (r == null)
                {
                    EmptyData();
                    SetError("KHÁCH HÀNG VỚI MÃ THẺ '" + maThe + "' CHƯA ĐĂNG KÝ TRONG HỆ THỐNG");
                }
                else
                {
                    DataRow vrRow = Config.Db.GetFirstRow(string.Format("SELECT FIRST 1 TIMECREATED, CURRENT_TIMESTAMP AS HIENTAI FROM TVAORA WHERE DMAYVANTAYID='{0}' AND DKHACHHANGID='{1}' ORDER BY GIO DESC", mayID, r["ID"].ToString()));
                    if (vrRow != null)
                    {
                        DateTime now = ConvertTo.Date(vrRow["HIENTAI"]);
                        DateTime lanTruoc = ConvertTo.Date(vrRow["TIMECREATED"]);
                        TimeSpan ts = now - lanTruoc;
                        if (ts.TotalSeconds <= 2) return;
                    }
                    
                    Color c = Color.FromArgb(187, 206, 230);

                    lblSoNgayCon.BackColor = Color.Transparent;
                    DKHACHHANGRow row = new DKHACHHANGRow(r);
                    LoadThongTinKhachHang(row);                    

                    //kiểm tra khách có đang bảo lưu không?
                    if (row.DTRANGTHAIID == TrangThaiIds.BaoLuu)
                    {
                        SetError("KHÁCH HÀNG ĐANG BẢO LƯU");
                        c = Color.Red;
                        SetThongTinVaoRa("", "", "");
                    }
                    else if (row.DENNGAY < toDay)
                    {
                        SetError("KHÁCH QUÁ HẠN SỬ DỤNG DỊCH VỤ");
                        c = Color.Red;
                        SetThongTinVaoRa("", "", "");
                    }
                    else if (SystemConfig.CoSuDungTheTheoLan == 30 && row.SOLAN <= row.DATAP && row.SOLAN > 0)
                    {
                        SetError("KHÁCH QUÁ SỐ LẦN SỬ DỤNG DỊCH VỤ");
                        c = Color.Red;
                        SetThongTinVaoRa("", "", "");
                        TGIAHANTHE0Ae.CapNhatThongTinKhachHangVaKhoaMoThe(row.ID);
                    }
                    else
                    {
                        int TapHomNay = Config.Db.GetFirstFieldInt("SELECT COUNT(*) FROM TVAORA WHERE NGAY = CURRENT_DATE AND DKHACHHANGID = '" + row.ID + "'") + 1;
                        SetThongTinVaoRa(row.SOLAN == 0 ? "" : ((row.SOLAN - row.DATAP - 1).ToString() + "/" + row.SOLAN.ToString()),
                            (row.DATAP + 1).ToString(), TapHomNay.ToString());

                        TimeSpan ts = row.DENNGAY - toDay;
                        lblSoNgayCon.Text = ts.TotalDays.ToString();

                        if (ts.TotalDays <= 7)
                        {
                            lblSoNgayCon.BackColor = Color.Red;
                        }

                        //cập nhật vào ra
                        TVAORARow upRow = new TVAORARow();

                        //kiểm tra xem co ghi chú gì không?
                        //string note = Config.Db.GetFirstFieldString("SELECT NOTE FROM TVAORA WHERE DKHACHHANGID = '" + row.ID + "' ORDER BY NGAY DESC, GIO DESC");
                        string note = row.NOTE;

                        DateTime gioHienTai = Config.Db.DbDateTime;
                        upRow.NGAY = gioHienTai.Date;
                        upRow.GIO = gioHienTai;
                        upRow.DKHACHHANGID = row.ID;
                        upRow.TGIAHANTHEID = row.TGIAHANTHEID;
                        upRow.NOTE = note;
                        upRow.DMAYVANTAYID = mayID;
                        upRow.NAME = DbUtils.GenSoHoaDon(gioHienTai.Date, "VR(dd)(MM)(yy)/(****)", Tables.TVAORA, TVAORAInfo.NAME, TVAORAInfo.NGAY);
                        upRow.Update();

                        if (note.Length > 0)
                        {                            
                            lblError.Text = "GHI CHÚ: " + note;
                            lblError.Visible = true;
                            lblError.BackColor = Color.DarkGreen;
                            tmrHideMessage.Enabled = true;
                        }
                        
                        currentID = upRow.ID;
                        grChiTiet.LoadData();

                        //giảm số lần
                        if (SystemConfig.CoSuDungTheTheoLan == 30)
                        {
                            Config.Db.ExecSql("UPDATE TGIAHANTHE SET DATAP = COALESCE(DATAP, 0) + 1 WHERE ID = '" + row.TGIAHANTHEID + "'");

                            //cập nhật lại trạng thái của khách
                            TGIAHANTHE0Ae.CapNhatThongTinKhachHangVaKhoaMoThe(row.ID);
                        }                        
                    }

                    if (row.ANH != null && row.ANH.Length > 0)
                    {
                        try
                        {
                            MemoryStream stream = new MemoryStream();
                            stream.Write(row.ANH, 0, row.ANH.Length);
                            stream.Seek(0, SeekOrigin.Begin);
                            ptImage.Image = Image.FromStream(stream);
                        }
                        catch (Exception ex)
                        {
                            ptImage.Image = null;
                        }
                    }
                    else
                    {
                        ptImage.Image = null;
                    }

                    No1UserControl1.BackColor = c;

                    UpdateManHinhPhu();
                }
            }
        }

        private void SetThongTinVaoRa(string SoLanCon, string LanToi, string HomNay)
        {
            lblSoLanCon.Text = SoLanCon;
            lblLanToi.Text = LanToi;
            lblTapHomNay.Text = HomNay;
        }

        private void LoadThongTinKhachHang(DKHACHHANGRow row)
        {
            lblDiaChi.Text = row.DIACHI;
            lblDienThoai.Text = row.DIENTHOAI;
            lblNgaySinh.Text = row.IsNullValue(DKHACHHANGInfo.NGAYSINH) ? "" : row.NGAYSINH.ToString("dd/MM/yyyy");
            lblTenKhach.Text = row.NAME + " - " + row.MAKHACH;
            lblTuNgay.Text = row.IsNullValue(DKHACHHANGInfo.TUNGAY) ? "" : row.TUNGAY.ToString("dd/MM/yyyy");
            lblError.Visible = false;
            lblDenNgay.Text = row.IsNullValue(DKHACHHANGInfo.DENNGAY) ? "" : row.DENNGAY.ToString("dd/MM/yyyy");

            if (row.DCATAPID.Length > 0)
                lblCaTap.Text = new DCATAPRow(row.DCATAPID).NAME;
            else
                lblCaTap.Text = "";

            if (row.DNHOMKHACHHANGID.Length > 0)
                lblNhom.Text = new DNHOMKHACHHANGRow(row.DNHOMKHACHHANGID).NAME;
            else
                lblNhom.Text = "";
            if (row.DLOAITHEID.Length > 0)
            {
                lblLoai.Text = new DLOAITHERow(row.DLOAITHEID).NAME;
            }
            else
            {
                lblLoai.Text = "";
            }            
        }

        private void UpdateManHinhPhu()
        {
            try
            {
                if (Shared.frmBangHieu != null)
                {
                    Shared.frmBangHieu.lblDiaChi.Text = lblDiaChi.Text;
                    Shared.frmBangHieu.lblDienThoai.Text = lblDienThoai.Text;
                    Shared.frmBangHieu.lblTenKhach.Text = lblTenKhach.Text;
                    Shared.frmBangHieu.lblTuNgay.Text = lblTuNgay.Text;
                    Shared.frmBangHieu.lblError.Visible = lblError.Visible;
                    Shared.frmBangHieu.lblDenNgay.Text = lblDenNgay.Text;
                    Shared.frmBangHieu.lblNhom.Text = lblNhom.Text;
                    Shared.frmBangHieu.lblLoai.Text = lblLoai.Text;
                    Shared.frmBangHieu.lblSoNgayCon.Text = lblSoNgayCon.Text;
                    Shared.frmBangHieu.lblLanToi.Text = lblLanToi.Text;
                    Shared.frmBangHieu.lblTapHomNay.Text = lblTapHomNay.Text;
                    Shared.frmBangHieu.lblSoLanCon.Text = lblSoLanCon.Text;
                    Shared.frmBangHieu.ptImage.Image = ptImage.Image;
                }
            }
            catch
            {
            }
        }
        
        private void EmptyData()
        {
            lblDiaChi.Text = "";
            lblTenKhach.Text = "";
            lblDienThoai.Text = "";
            lblNgaySinh.Text = "";
            lblNhom.Text = "";
            lblTuNgay.Text = "";
            lblCaTap.Text = "";
            lblDenNgay.Text = "";
            lblLoai.Text = "";
            lblSoNgayCon.Text = "";
            lblLanToi.Text = "";
            lblMay.Text = "";
            lblSoLanCon.Text = "";
            lblTapHomNay.Text = "";

            UpdateManHinhPhu();
        }
        
        private void SetError(string msg)
        {
            lblError.BackColor = Color.Red;
            lblError.Text = msg;
            lblError.Visible = true;
            tmrHideMessage.Enabled = false;
            tmrHideMessage.Enabled = true;

            if (Shared.frmBangHieu != null)
            {
                Shared.frmBangHieu.lblError.BackColor = Color.Red;
                Shared.frmBangHieu.lblError.Text = msg;
                Shared.frmBangHieu.lblError.Visible = true;
            }
        }

        private DataTable dtKhachHang;
        public void LoadTemplate()
        {
            if (zkPrinter == null) return;

            zkPrinter.EndEngine();
            zkPrinter.InitEngine();

            if (dbHandle != -1)
                zkPrinter.FreeFPCacheDB(dbHandle);
            dbHandle = zkPrinter.CreateFPCacheDB();
            dtKhachHang = Config.Db.GetTable("SELECT * FROM DKHACHHANG WHERE STATUS = 30 AND MAVANTAY IS NOT NULL");
            for (int i = 0; i < dtKhachHang.Rows.Count; i++)
            {
                string MAVANTAY = dtKhachHang.Rows[i]["MAVANTAY"].ToString();
                zkPrinter.AddRegTemplateStrToFPCacheDB(dbHandle, i, MAVANTAY);
            }
        }

        private void OnDispose()
        {
            zkPrinter.FreeFPCacheDB(dbHandle);
        }

        ThietBiSuDung thietBiSuDung;
        DataTable thietbiDt = null;
        DateTime toDay;
		public void No1UserControl1_OnInit(object sender, EventArgs e)
		{            
            Shared.QuanLyThietBi.OnSuccess += new OnSuccessHandler(IMay_OnSuccess);
            Shared.QuanLyThietBi.OnError += new OnSuccessHandler(QuanLyThietBi_OnError);

            toDay = Config.Db.DbDate;
            No1UserControl1.BackColor = Color.FromArgb(187, 206, 230);
            bool GhiNhanVaoRa = IniFile.GetString(Application.StartupPath + "\\App.dat", "OTHER", "Update", "") != "30";

            Shared.ShowBangGia();

            EmptyData();

            if (SystemConfig.ThietBiSuDung != (int)ThietBiSuDung.CuaTu)
            {
                pageMayVanTay.Visible = false;
                lblLuyY.Visible = false;
            }
            
            thietBiSuDung = (ThietBiSuDung)SystemConfig.ThietBiSuDung;
            bool nhapLieuBangBanPhim = thietBiSuDung == ThietBiSuDung.DauDocTheTu || SystemConfig.ChoPhepNhapBangBanPhim == 30;

            if (thietBiSuDung == ThietBiSuDung.MayDocVanTay)
            {
                KiemSoatVaoRa.kiemSoatVaoRa = this;
                zkPrinter = new AxZKFPEngX();
                zkPrinter.CreateControl();
                zkPrinter.OnCapture += new IZKFPEngXEvents_OnCaptureEventHandler(zkPrinter_OnCapture);
                try
                {
                    if (zkPrinter.InitEngine() == 0)
                    {
                        zkPrinter.FPEngineVersion = "10";
                        LoadTemplate();
                    }
                    else
                    {
                        SetError("KHÔNG THỂ KẾT NỐI TỚI THIẾT BỊ VÂN TAY USB");
                    }
                }
                catch
                {
                    SetError("KHÔNG THỂ KẾT NỐI TỚI THIẾT BỊ VÂN TAY USB");
                }
            }            

            txtMaThe.Enabled = nhapLieuBangBanPhim;
            btnNhap.Enabled = nhapLieuBangBanPhim;
            
            grChiTiet.LoadData();
            DataGridViewColumn col = grChiTiet.Columns["GIO"];
            if (col != null)
            {
                (col as CalendarColumn).Format = "HH:mm";
            }

            if (GhiNhanVaoRa)
            {
                StringBuilder msg = new StringBuilder();
                if (thietBiSuDung == ThietBiSuDung.CuaTu)
                {
                    thietbiDt = new DataTable();
                    thietbiDt.Columns.Add("ID", typeof(string));
                    thietbiDt.Columns.Add("MAY", typeof(string));
                    thietbiDt.Columns.Add("TRANGTHAI", typeof(string));
                    thietbiDt.Columns.Add("IP", typeof(string));
                    thietbiDt.Columns.Add("STATUS", typeof(int));
                    thietbiDt.Columns.Add("PORT", typeof(string));

                    foreach (ThietBiInfo info in Shared.QuanLyThietBi.lstThietBi)
                    {
                        DataRow row = thietbiDt.NewRow();
                        row["ID"] = info.MayID;
                        row["MAY"] = info.TenMay;
                        row["IP"] = info.IP;
                        row["PORT"] = info.Port.ToString();
                        row["TRANGTHAI"] = "Chưa kết nối";
                        thietbiDt.Rows.Add(row);                                              
                    }
                    grMayVanTay.DataSource = thietbiDt;
                    /*Shared.QuanLyThietBi.KetNoi(ref msg);
                    RefreshStatusThietBi();*/
                    DoConnect();
                }
                if (msg.Length > 0) Msg.ShowWarning(msg.ToString());
                txtMaThe.Select();
            }
            else
            {
                foreach (Control c in No1UserControl1.Controls)
                {
                    c.Visible = c == tabMain;
                }

                tabMain.Dock = DockStyle.Fill;
            }   
   
            if (thietBiSuDung != ThietBiSuDung.CuaTu)
            {
                if (SystemConfig.NgayCapNhatTrangThaiGanNhat != toDay)
                {
                    TuDongMoKhoaThe form = (TuDongMoKhoaThe)Config.CreateForm(Forms.TuDongMoKhoaThe);
                    form.LoadData(toDay);
                    form.No1Form1.ShowDialog();
                }
            }         
		}

        void zkPrinter_OnCapture(object sender, IZKFPEngXEvents_OnCaptureEvent e)
        {
            string template = zkPrinter.GetTemplateAsStringEx("9");
            int Score = 9;
            int ProcessNum = new int();
            int ID = zkPrinter.IdentificationFromStrInFPCacheDB(dbHandle, template, ref Score, ref ProcessNum);
            No1UserControl1.Invoke(new MethodInvoker(delegate
            {
                if (ID < 0)
                {
                    SetError("KHÁCH HÀNG CHƯA ĐĂNG KÝ TRONG HỆ THỐNG");
                }
                else
                {
                    DataRow r = dtKhachHang.Rows[ID];
                    DKHACHHANGRow khRow = new DKHACHHANGRow(r);
                    LoadCustomer(khRow.MAKHACH, "", "");
                }
            }));
        }

        void QuanLyThietBi_OnError(IMayVanTay IMay, string MayID, string TenMay, string MaThe)
        {
            
        }

        void IMay_OnSuccess(IMayVanTay IMay, string MayID, string TenMay, string MaThe)
        {
            if (IMay is ZkTechco)
                LoadCustomer(MaThe, TenMay, MayID);
            else
            {
                (IMay as BioStar).trmVerify.Tick += new EventHandler(trmVerify_Tick);
                (IMay as BioStar).trmVerify.Enabled = true;
            }
        }
        
		public void Timer1_Tick(object sender, EventArgs e)
		{
            tmrHideMessage.Enabled = false;
            lblError.Visible = false;
            if (Shared.frmBangHieu != null) Shared.frmBangHieu.lblError.Visible = false;
            No1UserControl1.BackColor = Color.FromArgb(187, 206, 230);
		}

		public void grChiTiet_CustomLoadData(object sender, CustomLoadDataArgs e)
		{
            e.Where = "NGAY = CURRENT_DATE";
            e.OrderBy = "GIO DESC";
            e.Select = e.Select.Replace("\"DKHACHHANGID\"", "(SELECT NAME FROM DKHACHHANG WHERE ID = DKHACHHANGID) AS DKHACHHANGID");
		}

		public void tmrFocusTextBox_Tick(object sender, EventArgs e)
		{
            tmrFocusTextBox.Enabled = false;
            txtMaThe.Focus();
            txtMaThe.Select();
		}

        public void trmVerify_Tick(object sender, EventArgs e)
        {
            Timer trm = (sender as Timer);
            BioStar IMay = trm.Tag as BioStar;
            IMay.trmVerify.Enabled = false;
            if (IMay.IsConnected)
            {
                int logCount = 0;
                if (IMay.lastVerify == 0)
                {
                    int val = 0;
                    if (BSSDK.BS_GetTime(IMay.Handle, ref val) == BSSDK.BS_SUCCESS)
                    {
                        IMay.lastVerify = val;
                    }
                }

                int numofLog = 10;
                IntPtr logRecord = Marshal.AllocHGlobal(numofLog * Marshal.SizeOf(typeof(BSSDK.BSLogRecord))); //Chỉ lấy một log mới nhất                    
                int result = BSSDK.BS_ReadLog(IMay.Handle, IMay.lastVerify, 0, ref logCount, logRecord);

                if (result == BSSDK.BS_SUCCESS && logCount > 0)
                {
                    BSSDK.BSLogRecord record = (BSSDK.BSLogRecord)Marshal.PtrToStructure(new IntPtr(logRecord.ToInt32() + (logCount - 1) * Marshal.SizeOf(typeof(BSSDK.BSLogRecord))), typeof(BSSDK.BSLogRecord));
                    /*if ((
                        ((record.eventType & BSSDK.BE_EVENT_VERIFY_SUCCESS) == BSSDK.BE_EVENT_VERIFY_SUCCESS) || 
                        ((record.eventType & BSSDK.BE_EVENT_VERIFY_FAIL) == BSSDK.BE_EVENT_VERIFY_FAIL)
                        ) 
                        && record.eventTime != lastVerify)*/
                    if ((record.eventType == BSSDK.BE_EVENT_VERIFY_SUCCESS || record.eventType == BSSDK.BE_EVENT_VERIFY_FAIL) && record.eventTime != IMay.lastVerify)
                    {
                        IMay.lastVerify = record.eventTime;
                        uint val = record.userID;
                        LoadCustomer(val.ToString(), "", "");
                    }
                }
                Marshal.FreeHGlobal(logRecord);
            }
            IMay.trmVerify.Enabled = true;
        }        
	
        private void RefreshStatusThietBi()
        {
            bool ShowKetNoi = false;
            foreach (ThietBiInfo info in Shared.QuanLyThietBi.lstThietBi)
            {
                foreach (DataRow r in thietbiDt.Rows)
                {
                    if (r["ID"].ToString() == info.MayID)
                    {
                        if (info.IMay.IsConnected)
                        {
                            r["STATUS"]=1;
                            r["TRANGTHAI"] = "Đã kết nối";                            
                        }
                        else
                        {
                            r["STATUS"] = 0;
                            r["TRANGTHAI"] = "Chưa kết nối";                            
                            if (!ShowKetNoi) ShowKetNoi = true;
                        }
                        break;
                    }
                }
            }
            btnKetNoi.Visible = ShowKetNoi;
            btnKetNoi.BringToFront();
        }

        public void grMayVanTay_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            DataRow Row = (grMayVanTay.Rows[e.RowIndex].DataBoundItem as DataRowView).Row;
            if (grMayVanTay.Columns[e.ColumnIndex].DataPropertyName == "HANHDONG")
            {                
                if (Row != null && ConvertTo.Int(Row["STATUS"]) == 1)
                {
                    e.PaintBackground(e.ClipBounds, true);
                    e.Handled = true;
                }
            }
            if (ConvertTo.Int(Row["STATUS"]) == 0)
            {
                e.CellStyle.BackColor = Color.Red;
            }
        }

        BackgroundWorker bwKetNoi = null;
		public void btnKetNoi_Click(object sender, EventArgs e)
		{
            btnKetNoi.Enabled = false;
            DoConnect();
		}

        private void DoConnect()
        {
            if (bwKetNoi == null)
                bwKetNoi = new BackgroundWorker();
            bwKetNoi.DoWork += new DoWorkEventHandler(bwKetNoi_DoWork);
            bwKetNoi.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bwKetNoi_RunWorkerCompleted);

            //progressBar.Location = new Point((KryptonNavigator1.Width - progressBar.Width) / 2, (KryptonNavigator1.Height - progressBar.Height) / 2);
            progressBar.Visible = true;
            lblKetNoi.Visible = true;
            bwKetNoi.RunWorkerAsync();
        }

        void bwKetNoi_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            progressBar.Visible = false;
            lblKetNoi.Visible = false;
            btnKetNoi.Enabled = true;
            RefreshStatusThietBi();
            trmPing.Enabled = true;

            //sau khi kết nối xong thì thực hiện cập nhật lại trạng thái khách hàng
            //chỉ chạy cập nhật lần đầu tiên trong ngày
            if (SystemConfig.NgayCapNhatTrangThaiGanNhat != toDay)
            {
                TuDongMoKhoaThe form = (TuDongMoKhoaThe)Config.CreateForm(Forms.TuDongMoKhoaThe);
                form.LoadData(toDay);
                form.No1Form1.ShowDialog();
            }
        }

        void bwKetNoi_DoWork(object sender, DoWorkEventArgs e)
        {
            Shared.QuanLyThietBi.KetNoi();
        }


		public void No1UserControl1_OnTabClosing(object sender, CancelEventArgs e)
		{
            Shared.QuanLyThietBi.OnSuccess -= IMay_OnSuccess;
            Shared.QuanLyThietBi.OnError -= QuanLyThietBi_OnError;
            trmPing.Enabled = false;
		}	


		public void tmrHideNote_Tick(object sender, EventArgs e)
		{
            tmrHideNote.Enabled = false;
            lblLuyY.Visible = false;
		}

        StringBuilder sb = new StringBuilder();
        BackgroundWorker bwPing;
        public void trmPing_Tick(object sender, EventArgs e)
        {
            trmPing.Enabled = false;
            if (bwPing == null)
            {
                bwPing = new BackgroundWorker();
                bwPing.DoWork += new DoWorkEventHandler(bwPing_DoWork);
                bwPing.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bwPing_RunWorkerCompleted);
            }
            bwPing.RunWorkerAsync();
        }

        void bwPing_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            trmPing.Enabled = true;
            lblLoiKetNoi.Visible = sb.Length > 0;
            lblLoiKetNoi.Text = sb.ToString();
            sb = new StringBuilder();
        }

        void bwPing_DoWork(object sender, DoWorkEventArgs e)
        {
            foreach (ThietBiInfo info in Shared.QuanLyThietBi.lstThietBi)
            {
                if (info.IMay.IsConnected)
                {
                    bool ret = ZkTechco.PingHost(info.IP);
                    if (!ret)
                    {
                        sb.AppendLine("Không kết nối được thiết bị " + info.TenMay);
                    }
                }
            }
        }
    }
}
