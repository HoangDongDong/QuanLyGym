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
using DbMapping;
using System.IO;
using System.IO.Ports;
using System.Text.RegularExpressions;

namespace No1Run
{
    public partial class HoaDonBanHang: IRefreshable, ITestingSupport
    {
        private string SoHoaDon = "";
        private bool hienThiAnh;
        bool suDung2DVT = false;
        bool coLuuVet = false;
        bool nhieuMayTram = false;
        bool coManHienThi = false;
        private string congSuDung = "";
        private string canBatDau = "";
        private int lamTronTien;
        private bool printing = false;
        private bool hienThiCuaSoNhapSoLuong;
        private bool coSize = false;
        private bool coHanSd = false;
        private static SerialPort comport;
        private string lastKichThuoc;        
        private object lastHsd;

        public void UserControl1_Load(Object sender, EventArgs e)
        {
            lamTronTien = SystemConfig.LamTronTien;
            coLuuVet = SystemConfig.KichHoatLuuVetHoatDong == 30;            
            suDung2DVT = SystemConfig.SuDung2DonViTinh == 30;
            nhieuMayTram = SystemConfig.HeThongChayNhieuMayTram == 30;
            coHanSd = SystemConfig.MatHangCoHanSuDung == 30;
            coManHienThi = SystemConfig.CoCayHienThiGia == 30;
            canBatDau = SystemConfig.ChuoiBatDau;
            if (coManHienThi) congSuDung = SystemConfig.CongSuDung;
            hienThiCuaSoNhapSoLuong = SystemConfig.HienThiCuaSoNhapSoLuongKhiQuetMaVach == 30;
            coSize = SystemConfig.MatHangCoKichThuoc == 30;
            if (hienThiCuaSoNhapSoLuong)
            {
                lblSoLuong.Visible = false;
                numSL.Visible = false;
                btnThem.Left = lblSoLuong.Left;
            }            

            hienThiAnh = SystemConfig.HienThiAnhSanPham == 30;
            if (!hienThiAnh) lblTon.Visible = false;
            else lblTon.Text = "";

            grMatHang.SelectionChanged += new EventHandler(grMatHang_SelectionChanged);
            grTraLai.GridView.SelectionChanged += new EventHandler(grTraLai_grMain_SelectionChanged);
            grMua.GridView.SelectionChanged += new EventHandler(grTraLai_grMain_SelectionChanged);
            grMua.GridView.CellValueChanged += new DataGridViewCellEventHandler(GridView_CellValueChanged);

            //arrange controls by configuration
            SetupControls();

            mapper.LoadConfigSchema();
            mapper.EndInit();

            grMua.GridView.CellBeginEdit += new DataGridViewCellCancelEventHandler(GridView_CellBeginEdit);            

            ReLoad("", true);
            
            if (SystemConfig.KichHoatSuDungCanDienTu != 30)
            {
                pnlMaVach.Height = 33;
                txtCan.Visible = false;
                lblCan.Visible = false;
                numCan.Visible = false;
                numThanhTien.Visible = false;
            }
            else
            {
                DataGridViewColumn col = grMua.GridView.Columns["SLXUATCHUAQUYDOI"];
                (col as NumericDataGridViewColumn).DecimalLength = 3;
                gPos = SystemConfig.ViTri;
                gLen = SystemConfig.DoDai;
                txtCan.Visible = ((Control.ModifierKeys & Keys.Shift) == Keys.Shift);
                numCan.DecimalPlaces = 3;
                numThanhTien.DecimalPlaces = 0;

                OpenComport();
            }
        }

        BackgroundWorker bw;
        private void OpenComport()
        {
            try
            {
                if (comport != null)
                {
                    return;
                }
                else
                {
                    string com = SystemConfig.CongCom;

                    if (com.Length == 0)
                    {
                        Msg.ShowWarning("Bạn chưa thiết lập kết nối tới cân điện tử" + Environment.NewLine +
                            "Bạn vui lòng thiết lập trong 'Quản trị | Cấu hình toàn hệ thống'");
                    }
                    else
                    {
                        comport = new SerialPort();
                        comport.NewLine = "\r";

                        int baudRate = SystemConfig.BaudRate;
                        int dataBit = SystemConfig.Databit;
                        int timeOut = SystemConfig.Timeout;
                        int parity = SystemConfig.Parity;
                        int stopBit = SystemConfig.StopBits;

                        comport.BaudRate = baudRate == 0 ? 1200 : (baudRate == 1 ? 2400 : (baudRate == 2 ? 4800 : 9600));
                        comport.ReceivedBytesThreshold = 1;
                        comport.DataBits = dataBit;
                        comport.StopBits = (StopBits)stopBit;
                        comport.Parity = (Parity)parity;
                        comport.PortName = com;
                        comport.ReadTimeout = timeOut;
                        //comport.DataReceived += port_DataReceived;
                        comport.Open();
                        bw = new BackgroundWorker();
                        bw.DoWork += new DoWorkEventHandler(bw_DoWork);
                        bw.RunWorkerAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                comport = null;
                Msg.ShowWarning("Không thể kết nối tới cân: " + Environment.NewLine + ex.Message);
            }
        }

        bool closed = false;
        private void bw_DoWork(object sender, DoWorkEventArgs e)
        {
            while (!closed)
            {
                try
                {
                    string data = "";
                    data = comport.ReadLine();
                    //File.AppendAllText(file, DateTime.Now.ToString("HH:mm:ss") + " - " + data + Environment.NewLine);
                    decimal strTotalWeight = parsingdata(data);
                    numCan.Invoke(new MethodInvoker(delegate
                    {
                        decimal val = Math.Round(strTotalWeight / (decimal)1000, 3);
                        numCan.Value = val;
                        txtCan.Text = data;
                        if (grMatHang.SelectedRows.Count > 0)
                        {
                            decimal donGia = ConvertTo.Decimal(grMatHang.SelectedRow["DONGIA"]);
                            numThanhTien.Value = donGia * val;
                        }
                        //cboSoLuong.Text = (strTotalWeight / (decimal)1000).ToString("n3");
                    }));
                }
                catch (Exception ex)
                {
                    txtCan.Invoke(new MethodInvoker(delegate
                    {
                        txtCan.Text = "Lỗi";
                    }));
                }
            }
        }

        int iRounding = 3;
        int gPos;
        int gLen;
        private decimal parsingdata(string sData)
        {
            try
            {
                string strResult = "";
                double dWeight = 0.0;
                //if (sData.Contains("g") || sData.Contains("G"))
                {
                    strResult = sData.Substring(gPos, gLen).Trim();
                    dWeight = Math.Round(double.Parse(strResult), iRounding);
                    return (decimal)dWeight;
                }

                if ((iRounding == 0) || (iRounding == 2) || (iRounding == 1))
                {
                    dWeight = Math.Round(double.Parse(strResult), iRounding);
                }
                else if (iRounding == 3)
                {
                    dWeight = Math.Round((double)(double.Parse(strResult) / (double)10), 0) * 10;
                }
                else if (iRounding == 4)
                {
                    //dWeight = 100 * decimal.Parse(strResult) * Share.HSCan / 100);
                    dWeight = Math.Round((double)(double.Parse(strResult) / (double)100), 0) * 100;
                }
                else
                {
                    double sTMP = double.Parse(strResult);
                    if ((sTMP % Math.Truncate(sTMP)) < 0.5)
                    {
                        dWeight = Math.Truncate(sTMP) + 0.5;
                    }
                    else
                    {
                        dWeight = Math.Truncate(sTMP) + 1.0;
                    }
                }
                return (decimal)dWeight;
            }
            catch (Exception ex)
            {
                //File.AppendAllText(Application.StartupPath + "\\Data\\log.txt", DateTime.Now.ToString("HH:mm:ss") + "-" + ex.Message + Environment.NewLine);
                return 0;
            }
        }

        void GridView_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (grMua.GridView.Columns[e.ColumnIndex].DataPropertyName == "SLXUATCHUAQUYDOI")
            {
                TDONHANGCHITIETRow ctRow = new TDONHANGCHITIETRow(grMua.GridView.SelectedRow);
                Track("Sửa số lượng '" + ctRow["DMATHANG_NAME"] + "' thành " + ctRow.SLXUATCHUAQUYDOI);
            }
        }

        private void WriteToCustomerDisplay(string line1, string line2)
        {
            if (!coManHienThi) return;

            SerialPort sp = new SerialPort(congSuDung, 9600, Parity.None, 8, StopBits.One);
            try
            {
                sp.Open();
                // to clear the display
                sp.Write(Convert.ToString((char)12));

                // first line goes here
                string koDau = LoaiBoDauTiengViet(line1).ToUpper();
                if (koDau.Length > 19) koDau = koDau.Substring(0, 19);
                sp.WriteLine(koDau);

                // 2nd line goes here
                sp.WriteLine((char)13 + line2);
            }
            catch (Exception ex)
            {
            }
            finally
            {
                if (sp.IsOpen)
                    sp.Close();
                sp.Dispose();
            }
            sp = null;
        }

        public static string LoaiBoDauTiengViet(string ip_str_change)
        {
            Regex v_reg_regex = new Regex("\\p{IsCombiningDiacriticalMarks}+");
            string v_str_FormD = ip_str_change.Normalize(NormalizationForm.FormD);
            return v_reg_regex.Replace(v_str_FormD, String.Empty).Replace('\u0111', 'd').Replace('\u0110', 'D').ToLower();
        }

        void GridView_CellBeginEdit(object sender, DataGridViewCellCancelEventArgs e)
        {
            if (!btnThanhToan.Enabled || printing) 
            {
                e.Cancel = true;
            }
            
            if (!DbUtils.CanView(Functions.SuaDonGia))
            {
                e.Cancel = true;
            }
        }        

        private void SetupControls()
        {
            List<ArrangeItem> lst = new List<ArrangeItem>();
            lst.Add(new ArrangeItem(SystemConfig.CoThueSuat == 30, lblTILETHUE, numTILETHUE, numTIENTHUE, lblTIENTHUE));
            lst.Add(new ArrangeItem(SystemConfig.CoPhiVanChuyen == 30, lblPHIVANCHUYEN, numPHIVANCHUYEN));
            lst.Add(new ArrangeItem(true, lblDOITRA, numDOITRA));
            lst.Add(new ArrangeItem(true, lblTONGCONG, numTONGCONG));

            int top = numTILETHUE.Top;
            int h = UiUtils.ArrangeControl(lst, 5, top) + 5;
            Panel2.Height = h;

            if (SystemConfig.CoGiaoHang != 30)
            {
                lblDNHANVIENGIAOID.Visible = false;
                lueDNHANVIENGIAOID.Visible = false;
                lblTRICHNHANVIEN.Visible = false;
                numTRICHNHANVIEN.Visible = false;
                Panel1.Height = Panel1.Height - (txtNOTE.Top - numTRICHNHANVIEN.Top);
                lblNOTE.Top = lblTRICHNHANVIEN.Top;
                lblNOTE.Width = lueDKHACHHANGID.Width;
                txtNOTE.Top = numTRICHNHANVIEN.Top;
            }
        }

        void grMatHang_SelectionChanged(object sender, EventArgs e)
        {
            if (!hienThiAnh) return;
            if (tmrLoadImage == null)
            {
                tmrLoadImage = new Timer();
                tmrLoadImage.Interval = 500;
                tmrLoadImage.Tick += new EventHandler(tmrLoadImage_Tick);
            }
            tmrLoadImage.Enabled = false;
            tmrLoadImage.Enabled = true;
        }

        void tmrLoadImage_Tick(object sender, EventArgs e)
        {
            //load image va ton
            tmrLoadImage.Enabled = false;
            try
            {
                string ID = (grMatHang.SelectedRows[0].DataBoundItem as DataRowView).Row["ID"].ToString();
                object byteVal = Config.Db.GetFirstField("SELECT ANH FROM DMATHANG WHERE ID = '" + ID + "'");
                if (byteVal == null || byteVal == DBNull.Value)
                    ptImage.Image = null;
                else
                {
                    try
                    {
                        byte[] data = (byte[])byteVal;
                        MemoryStream stream = new MemoryStream(data, 0, data.Length);
                        Bitmap bmp = new Bitmap(stream);
                        ptImage.Image = bmp;
                    }
                    catch
                    {
                        ptImage.Image = null;
                    }
                }

                //load ton
                lblTon.Text = "TỒN: " + TonKhoHandler.GetTonKho(ID, lueDKHOXUATID.StringValue, "");                
            }
            catch
            {
                ptImage.Image = null;
            }
        }

		public void btnThongKe_Click(Object sender, EventArgs e)
		{
            if (DbUtils.CanLogin(Functions.ThongKeTrongHoaDonBanHang))
            {
                //hiển thị form thống kê các giao dịch đã bán trong ngày
                ThongKe form = (ThongKe)Config.CreateForm(Forms.ThongKe);
                if (form.form.ShowDialog() == DialogResult.OK)
                {
                    ReLoad(form.SelectedID, false);
                }
                else
                {
                    SelectMaVach();
                }
            }
		}

        private bool IsValidForThanhToan()
        {
            //kiem tra ban quyen
            if (No1System.IsTrial)
            {
                //dem so bill
                if (Config.Db.GetFirstFieldInt("SELECT COUNT(DISTINCT NGAY) FROM TDONHANG") > 30)
                {
                    Msg.ShowWarning("Phiên bản dùng thử, không được phép thêm nhiều dữ liệu");
                    return false;
                }
            }

            if (!nhieuMayTram)
            {
                ReLoad(mapper.ID, false);
                TDONHANGRow dhRow = new TDONHANGRow(mapper.ID);
                if (dhRow.DATHANHTOAN == 30)
                {
                    Msg.ShowWarning("Hóa đơn này đã thanh toán");
                    return false;
                }

                //tinh toan lai
                RefreshOrderNo();
            }            

            //thiet lap kho cho hang mua va hang tra lai
            Dictionary<string, decimal> dicCheck = null;

            bool khongAmKho = false;
            if (lueDKHOXUATID.StringValue.Length > 0)
            {
                khongAmKho = new DKHOHANGRow(lueDKHOXUATID.StringValue).CHOPHEPAMKHO == 0;
            }

            if (khongAmKho) dicCheck = new Dictionary<string, decimal>();

            DataTable dt = grMua.DataSource;
            string DKHOID = lueDKHOXUATID.StringValue;
            foreach (DataRow r in dt.Rows)
            {
                if (r.RowState == DataRowState.Deleted) continue;
                if (r["DKHOHANGID"].ToString() != DKHOID)
                {
                    r["DKHOHANGID"] = DKHOID;
                }

                if (dicCheck != null)
                {
                    decimal sl = ConvertTo.Decimal(r["SLXUAT"]);
                    string DMATHANGID = r["DMATHANGID"].ToString();
                    if (dicCheck.ContainsKey(DMATHANGID)) dicCheck[DMATHANGID] = dicCheck[DMATHANGID] + sl;
                    else dicCheck.Add(DMATHANGID, sl);
                }
            }

            dt = grTraLai.DataSource;            
            foreach (DataRow r in dt.Rows)
            {
                if (r.RowState == DataRowState.Deleted) continue;
                if (r["DKHOHANGID"].ToString() != DKHOID)
                {
                    r["DKHOHANGID"] = DKHOID;
                }
                if (dicCheck != null)
                {
                    decimal sl = ConvertTo.Decimal(r["SLNHAP"]);
                    string DMATHANGID = r["DMATHANGID"].ToString();
                    if (dicCheck.ContainsKey(DMATHANGID)) dicCheck[DMATHANGID] = dicCheck[DMATHANGID] - sl;                    
                }
                //kiem tra so luong tra lai co lon hon so luong ban ra
                string TDONHANGTRAID = r["TDONHANGTRAID"].ToString();
                if (TDONHANGTRAID.Length > 0)
                {
                    decimal slBan = Config.Db.GetFirstFieldDec("SELECT SLXUAT FROM TDONHANGCHITIET WHERE ID = '" + TDONHANGTRAID + "'");
                    decimal slDaTra = Config.Db.GetFirstFieldDec("SELECT SUM(COALESCE(SLNHAP, 0)) FROM TDONHANGCHITIET WHERE TDONHANGTRAID = '" + TDONHANGTRAID + "' AND TDONHANGID <> '" + mapper.ID + "'");
                    slBan = slBan - slDaTra;
                    decimal sl = ConvertTo.Decimal(r["SLNHAP"]);
                    if (sl > slBan)
                    {
                        decimal quyDoi = 1;
                        if (SystemConfig.SuDung2DonViTinh == 30)
                        {
                            DMATHANGRow mhRow = new DMATHANGRow(r["DMATHANGID"].ToString());
                            if (mhRow.DDONVITINHCHANID.Length > 0 && mhRow.DDONVITINHID != mhRow.DDONVITINHCHANID && mhRow.QUYDOI != 0)
                            {
                                quyDoi = mhRow.QUYDOI;
                            }
                        }
                        Msg.ShowWarning("Mặt hàng '" + r["DMATHANG_NAME"].ToString() + "' trả " + r["SLNHAPCHUAQUYDOI"].ToString() + " quá số lượng cho phép trả " + (slBan / quyDoi).ToString());
                        return false;
                    }
                }
            }

            if (khongAmKho && dicCheck != null)
            {
                foreach (string DMATHANGID in dicCheck.Keys)
                {
                    decimal sl = dicCheck[DMATHANGID];
                    decimal ton = TonKhoHandler.GetTonKho(DMATHANGID, lueDKHOXUATID.StringValue, mapper.ID);
                    if (ton < sl)
                    {
                        Msg.ShowWarning("Mặt hàng " + new DMATHANGRow(DMATHANGID).NAME + " trong kho chỉ còn tồn " + ton.ToString() + " không đủ xuất " + sl.ToString());
                        return false;
                    }
                }
            }

            mapper.RaiseOnCalculation();
            if (grMua.RowCount + grTraLai.RowCount == 0)
            {
                Msg.ShowWarning("Mời bạn thêm mặt hàng vào hóa đơn!");
                SelectMaVach();
                return false;
            }

            if (SystemConfig.BatBuocNhapKhachHang == 30 && lueDKHACHHANGID.StringValue.Length == 0)
            {
                Msg.ShowWarning("Mời bạn chọn khách hàng trước");
                lueDKHACHHANGID.showDropDown("");
                return false;
            }

            if (SystemConfig.BatBuocNhapNhanVienBanHang == 30 && lueDNHANVIENXUATID.StringValue.Length == 0)
            {
                Msg.ShowWarning("Mời bạn chọn nhân viên trước");
                lueDNHANVIENXUATID.showDropDown();
                return false;
            }

            if (lueDKHOXUATID.StringValue.Length == 0)
            {
                Msg.ShowWarning("Mời bạn chọn kho bán hàng");
                return false;
            }
            return true;
        }

		public void btnThanhToan_Click(Object sender, EventArgs e)
		{
            if (!IsValidForThanhToan()) return;

            mapper.RaiseOnCalculation();
            printing = true;

            //hien thi khuyen mai tu dong neu co
            DataTable dtKhuyenMai = null;
            if (SystemConfig.KichHoatKhuyenMaiTuDong == 30)
            {
                DataTable dt = GetKhuyenMaiDangSuDung(LOAIHINHKHUYENMAI.MUA1TANG1);
                if (dt.Rows.Count > 0)
                {
                    string where = "";
                    string TenChuongTrinh = "";
                    foreach (DataRow r in dt.Rows)
                    {
                        string ID = r["ID"].ToString();
                        if (where.Length > 0)
                        {
                            where += " OR ";
                            TenChuongTrinh += ", ";
                        }
                        where += "DDOTKHUYENMAIID = '" + ID + "'";
                        TenChuongTrinh += r["NAME"].ToString();
                    }
                    
                    string sql = @"SELECT SUM(SOLUONGTANG * FLOOR(SLXUAT / CASE WHEN SOLUONGMUA = 0 THEN 1 ELSE SOLUONGMUA END)) AS SOLUONG, 
    DMATHANGTANGID AS DMATHANGID, DMATHANG.CODE, DMATHANG.NAME,
    (SELECT NAME FROM DDONVITINH WHERE ID = DMATHANG.DDONVITINHID) AS DVT
    FROM TDONHANGCHITIET INNER JOIN DDOTKHUYENMAICHITIET ON TDONHANGCHITIET.DMATHANGID = DDOTKHUYENMAICHITIET.DMATHANGID
    AND TDONHANGID = '" + mapper.ID + "' AND (" + where + @") INNER JOIN DMATHANG ON DDOTKHUYENMAICHITIET.DMATHANGTANGID = DMATHANG.ID 
    GROUP BY DMATHANGTANGID, DMATHANG.CODE, DMATHANG.NAME, DMATHANG.DDONVITINHID HAVING SUM(SOLUONGTANG * FLOOR(SLXUAT / CASE WHEN SOLUONGMUA = 0 THEN 1 ELSE SOLUONGMUA END)) > 0";
                    dtKhuyenMai = Config.Db.GetTable(sql);
                    if (dtKhuyenMai.Rows.Count > 0 && SystemConfig.HienThiMatHangKhuyenMai == 30)
                    {
                        MatHangKhuyenMai frmKhuyenMai = (MatHangKhuyenMai)Config.CreateForm(Forms.MatHangKhuyenMai);
                        frmKhuyenMai.lblChuongTrinhKM.Text = frmKhuyenMai.lblChuongTrinhKM.Text + " " + TenChuongTrinh;
                        frmKhuyenMai.grMain.DataSource = dtKhuyenMai;
                        if (frmKhuyenMai.form.ShowDialog() != DialogResult.OK)
                        {
                            printing = false;
                            return;
                        }
                    }
                }
            }

            decimal noCu = 0;
            string DKHACHHANGID = lueDKHACHHANGID.StringValue;
            if (DKHACHHANGID.Length > 0)
            {
                noCu = Config.Db.GetFirstFieldDec("SELECT SUM(TONGCONG) - SUM(COALESCE(TIENTHANHTOAN, 0)) FROM TDONHANG WHERE DKHACHHANGID = '" + DKHACHHANGID + "' AND DATHANHTOAN = 30 AND ID <> '" + mapper.ID + "'");
                noCu -= Config.Db.GetFirstFieldDec("SELECT SUM(CASE WHEN LOAI = 0 THEN THU ELSE -CHI END) FROM TTHUCHI WHERE DKHACHHANGID = '" + DKHACHHANGID + "' AND COALESCE(KHONGTHAYDOICONGNO, 0) = 0");
            }

            if (SystemConfig.SuDung2DonViTinh == 0 && !nhieuMayTram)
            {
                Config.Db.ExecSql("UPDATE TDONHANGCHITIET SET SLNHAP = COALESCE(SLNHAPCHUAQUYDOI, 0), SLXUAT = COALESCE(SLXUATCHUAQUYDOI, 0) WHERE TDONHANGID = '" + mapper.ID + "'");
            }
            Config.Db.ExecSql("UPDATE TDONHANGCHITIET SET DKHOHANGID = '" + lueDKHOXUATID.StringValue + "' WHERE TDONHANGID = '" + mapper.ID + "'");
            XacNhanThanhToan form = (XacNhanThanhToan)Config.CreateForm(Forms.XacNhanThanhToan);
            decimal datTruoc = GetDatTruoc();
            WriteToCustomerDisplay("THANH TOAN", numTONGCONG.Value.ToString("n0"));
            //Nợ cũ đã bao gồm đặt trước
            form.SetData(numTONGCONG.Value, datTruoc, noCu + datTruoc, lueDKHACHHANGID.StringValue);
            DialogResult ret = form.form.ShowDialog();
            if (ret == DialogResult.OK || ret == DialogResult.Retry)
            {
                //if (LoginController.mode == LoginController.NOT_REGISTERED || LoginController.mode == LoginController.EXPIRED)
                //{
                //    string sql = "SELECT COUNT(DISTINCT NGAY) FROM TDONHANG";
                //    int count = ConvertTo.Int(Config.Db.GetFirstField(sql));
                //    if (count >= 30)
                //    {
                //        Msg.ShowWarning("Phiên bản dùng thử không cho phép bạn sử dụng dữ liệu nhiều!");
                //        return;
                //    }
                //}

                if (nhieuMayTram)
                {
                    //tinh toan lai
                    RefreshOrderNo();
                    mapper.Update();
                }

                //lưu lại khuyến mại
                if (dtKhuyenMai != null && dtKhuyenMai.Rows.Count > 0)
                {
                    foreach (DataRow r in dtKhuyenMai.Rows)
                    {                        
                        TDONHANGCHITIETRow ctRow = new TDONHANGCHITIETRow();
                        ctRow.TDONHANGID = mapper.ID;
                        ctRow.DMATHANGID = r["DMATHANGID"].ToString();
                        ctRow.DKHOHANGID = lueDKHOXUATID.StringValue;
                        ctRow.SLXUAT = ConvertTo.Decimal(r["SOLUONG"]);
                        ctRow.SLXUATCHUAQUYDOI = ConvertTo.Decimal(r["SOLUONG"]);
                        ctRow.SLNHAPCHUAQUYDOI = 0;
                        ctRow.SLNHAP = 0;
                        ctRow.DONGIA = 0;
                        ctRow.KHUYENMAI = 30;
                        ctRow.THANHTIEN = 0;                       
                        ctRow.Update();
                    }
                }

                string tDonHangID = mapper.ID;

                TDONHANGRow row = new TDONHANGRow(tDonHangID);
                row.TRALAI = form.TraLai < 0 ? 0 : form.TraLai;
                row.TIENTHANHTOAN = form.TienThanhToan - form.DatTruoc;
                row.CONLAI = Math.Max(0, numTONGCONG.Value - form.TienThanhToan);
                row.THETRATRUOC = form.TheTraTruoc;
                row.TRUTICHLUY = form.TruTichLuy;
                row.DTHETRATRUOCID = form.DTHETRATRUOCID;
                row.TIENMAT = form.TraLai < 0 ? form.TraLai : form.TienMat;
                row.CHUYENKHOAN = form.ChuyenKhoan;
                row.THE = form.TienThe;
                if (form.TruTichLuy != 0)
                {
                    decimal quyDoi = SystemConfig.QuyDoi1DiemSangTien;
                    if (quyDoi != 0)
                    {
                        row.DIEMGIAM = form.TruTichLuy / quyDoi;
                    }
                }
                row.KHACHDUA = form.KhachDua;
                row.NOCU = noCu;

                row.DTAIKHOANNGANHANGID = form.lueDTAIKHOAN.StringValue;
                row.VOUCHER = form.Voucher;
                if (form.DVOUCHERID.Length > 0) row.DVOUCHERID = form.DVOUCHERID;
                row.LOAITHANHTOAN = form.LoaiThanhToan;                
                DateTime now = Config.Db.DbDateTime;
                row.GIOTHANHTOAN = now;
                if (SystemConfig.ChoPhepThayDoiNgayTrenHoaDon == 0)
                    row.NGAY = now.Date;
                decimal diemQuyDoi = Math.Max(SystemConfig.DoanhSoTuongUngVoi1Diem, 1);
                row.DIEM = (int)(numTONGCONG.Value / diemQuyDoi);                                

                //đóng hóa đơn                
                row.USERTHANHTOANID = DbConfig.UserID;
                row.DATHANHTOAN = 30;
                row.CONNO = form.KhachDua < numTONGCONG.Value ? 30 : 0;
                row.Update();

                //xuất vật tư
                XuatVatTu(mapper.ID, lueDKHOXUATID.StringValue);

                //in hóa đơn
                if (ret == DialogResult.OK)
                {
                    PrintInvoice(false);
                }

                if (lueDKHACHHANGID.StringValue.Length > 0 && SystemConfig.TuDongNangCapThanhVienKhiDatHanMuc == 30)
                {
                    //tính lại doanh số và tăng thành viên nếu có
                    decimal diemTichLuy = 0;
                    string sql = @"SELECT COALESCE(DIEMTICHLUYBANDAU,0) + 
(SELECT COALESCE(CASE WHEN @CACHTINH = 1 THEN SUM(DIEM) ELSE SUM(TONGCONG) / CAST(@DIEM AS DECIMAL(18, 2)) END, 0) FROM TDONHANG WHERE DATHANHTOAN = 30 AND LOAI = 0 AND DKHACHHANGID=DKHACHHANG.ID) + 
(SELECT COALESCE(SUM(DIEMTANG-DIEMGIAM),0) FROM TTANGGIAMDIEM WHERE DKHACHHANGID = DKHACHHANG.ID) FROM DKHACHHANG WHERE ID = '" + lueDKHACHHANGID.StringValue + "'";
                    FbCommand cmd = Config.Db.GetCommand(sql);
                    cmd.Parameters.Add("@CACHTINH", FbDbType.Integer).Value = SystemConfig.CachTinhDiem;
                    cmd.Parameters.Add("@DIEM", FbDbType.Integer).Value = diemQuyDoi;

                    diemTichLuy = Config.Db.GetFirstFieldDec(cmd);
                    cmd = Config.Db.GetCommand("SELECT * FROM DNHOMKHACHHANG WHERE DIEMTICHLUY <= @DIEMTICHLUY AND DIEMTICHLUY > 0 ORDER BY DIEMTICHLUY DESC");
                    cmd.Parameters.Add("@DIEMTICHLUY", FbDbType.Decimal).Value = diemTichLuy;
                    DataRow rNhom = Config.Db.GetFirstRow(cmd);
                    if (rNhom != null)
                    {
                        DNHOMKHACHHANGRow nhomRow = new DNHOMKHACHHANGRow(rNhom);
                        DKHACHHANGRow khRow = new DKHACHHANGRow(lueDKHACHHANGID.StringValue);
                        if (nhomRow.ID != khRow.DNHOMKHACHHANGID)
                        {
                            DKHACHHANGRow upRow = new DKHACHHANGRow(lueDKHACHHANGID.StringValue);
                            upRow.DNHOMKHACHHANGID = nhomRow.ID;
                            upRow.Update();
                            Msg.ShowInfo("Khách hàng đã được chuyển sang nhóm '" + nhomRow.NAME + "' vì đạt tới số điểm '" + nhomRow.DIEMTICHLUY.ToString("N0") + "'");
                        }
                    }
                }

                txtMaVach.Text = "";
                //tạo mới bàn
                ReLoad("", false);
            }
            else if (ret == DialogResult.Ignore)
            {
                PrintInvoice(true);
            }
            printing = false;
		}

        internal static void XuatVatTu(string TDONHANGID, string DKHOHANGID)
        {
            if (SystemConfig.MatHangCoVatTu == 30)
            {
                Config.Db.ExecSql("DELETE FROM TDONHANGCHITIET WHERE XUATVATTU = 30 AND TDONHANGID = '" + TDONHANGID + "'");

                string sql = "SELECT DVATTUID, DMATHANG.DDONVITINHID, DMATHANG.GIAVON, SUM((COALESCE(TDONHANGCHITIET.SLXUAT, 0) - COALESCE(TDONHANGCHITIET.SLNHAP, 0)) * DDINHLUONG.SOLUONG) AS SOLUONG FROM TDONHANGCHITIET INNER JOIN DDINHLUONG ON TDONHANGCHITIET.TDONHANGID = '" + TDONHANGID + "' AND DDINHLUONG.DMATHANGID = TDONHANGCHITIET.DMATHANGID INNER JOIN DMATHANG ON DMATHANG.ID = DDINHLUONG.DVATTUID INNER JOIN DMATHANG B ON TDONHANGCHITIET.DMATHANGID = B.ID WHERE (B.LOAIDINHLUONG = 1 OR B.LOAIDINHLUONG = 2) GROUP BY DVATTUID, DMATHANG.DDONVITINHID, DMATHANG.GIAVON";
                DataTable dtVatTu = Config.Db.GetTable(sql);

                int index = 0;
                GetVatTu(dtVatTu, ref index);

                foreach (DataRow r in dtVatTu.Rows)
                {
                    TDONHANGCHITIETRow ctRow = new TDONHANGCHITIETRow();
                    ctRow.DDONVITINHID = r["DDONVITINHID"].ToString();
                    ctRow.DKHOHANGID = DKHOHANGID;
                    ctRow.DMATHANGID = r["DVATTUID"].ToString();
                    ctRow.SLNHAP = 0;
                    ctRow.SLXUAT = ConvertTo.Decimal(r["SOLUONG"]);
                    ctRow.SLXUATCHUAQUYDOI = ConvertTo.Decimal(r["SOLUONG"]);
                    ctRow.TDONHANGID = TDONHANGID;
                    ctRow.THANHTIEN = 0;
                    ctRow.DONGIA = 0;
                    ctRow.XUATVATTU = 30;
                    ctRow.Update();
                }
            }
        }

        private static void GetVatTu(DataTable dtVatTu, ref int index)
        {
            while (index < dtVatTu.Rows.Count)
            {
                DataRow rVatTu = dtVatTu.Rows[index];
                DataTable dt = GetDinhLuong(rVatTu["DVATTUID"].ToString());
                if (dt.Rows.Count > 0)
                {
                    decimal soLuong = ConvertTo.Decimal(rVatTu["SOLUONG"]);
                    //insert in to dtVatu
                    dtVatTu.Rows.RemoveAt(index);
                    foreach (DataRow r in dt.Rows)
                    {
                        DataRow newRow = dtVatTu.NewRow();
                        newRow["DVATTUID"] = r["DVATTUID"];
                        newRow["DDONVITINHID"] = r["DDONVITINHID"];
                        newRow["SOLUONG"] = ConvertTo.Decimal(r["SOLUONG"]) * soLuong;
                        dtVatTu.Rows.Add(newRow);
                    }
                }
                else
                {
                    index++;
                }
            }
        }

        private static DataTable GetDinhLuong(string DMATHANGID)
        {
            string sql = "SELECT DVATTUID, (SELECT DDONVITINHID FROM DMATHANG WHERE ID = DVATTUID) AS DDONVITINHID, SOLUONG FROM DDINHLUONG WHERE DMATHANGID = '" + DMATHANGID + "'";
            return Config.Db.GetTable(sql);
        }

        private void PrintInvoice(bool preview)
        {
            bool showPreview = SystemConfig.HienThiTruocKhiIn == 30;
            bool chonMau = SystemConfig.LuaChonMauKhiIn == 30;
            int soLan = showPreview ? 1 : Math.Max(1, Math.Min(5, SystemConfig.SoLanIn));

            string mauHoaDon = Config.Db.GetFirstFieldString("SELECT STEMPLATEID FROM DCUAHANG WHERE ID = (SELECT DCUAHANGID FROM DKHOHANG WHERE ID = '" + lueDKHOXUATID.StringValue + "')");
            if (mauHoaDon.Length == 0) mauHoaDon = SystemConfig.MauHoaDon;

            Config.PrintInvoice(Config.GetTableDesc(Tables.TDONHANG), Forms.HoaDonBanHang, mapper.ID, mauHoaDon, chonMau, showPreview, new CustomReportHandler(CustomReportParam), soLan);
        }

        private void CustomReportParam(DataSet ds, Dictionary<string, object> dic)
        {
            decimal NoCu = ConvertTo.Decimal(dic["NOCU"]);
            decimal TongCong = ConvertTo.Decimal(dic["TONGCONG"]);
            decimal ThanhToan = ConvertTo.Decimal(dic["TIENTHANHTOAN"]);            
            dic.Add("Nợ mới", TongCong + NoCu - ThanhToan);            
            dic.Add("Đặt trước", GetDatTruoc());
            dic.Add("Điểm tích lũy", GetDiemTichLuyTrenHoaDon(lueDKHACHHANGID.StringValue));
        }

        internal static decimal GetDiemTichLuy(string DKHACHHANGID)
        {
            if (DKHACHHANGID.Length == 0) return 0;

            DKHACHHANGRow khRow = new DKHACHHANGRow(DKHACHHANGID);
            decimal doanhSo1Diem = Math.Max(1, SystemConfig.DoanhSoTuongUngVoi1Diem);
            decimal soDiemTangGiam = Config.Db.GetFirstFieldDec("SELECT SUM(COALESCE(DIEMTANG, 0) - COALESCE(DIEMGIAM, 0)) FROM TTANGGIAMDIEM WHERE DKHACHHANGID = '" + DKHACHHANGID + "'");
            if (SystemConfig.CachTinhDiem == 0)
            {
                decimal tongDoanhSo = Config.Db.GetFirstFieldDec("SELECT SUM(TONGCONG) FROM TDONHANG WHERE DKHACHHANGID = '" + DKHACHHANGID + "'");
                return khRow.DIEMTICHLUYBANDAU + tongDoanhSo / doanhSo1Diem + soDiemTangGiam;
            }
            //tính theo từng hóa đơn
            else
            {
                decimal tongDiem = Config.Db.GetFirstFieldDec("SELECT SUM(COALESCE(DIEM, 0) - COALESCE(DIEMGIAM, 0)) FROM TDONHANG WHERE DKHACHHANGID = '" + DKHACHHANGID + "'");
                return khRow.DIEMTICHLUYBANDAU + tongDiem + soDiemTangGiam;
            }
        }

        internal static decimal GetDiemTichLuyTrenHoaDon(string DKHACHHANGID)
        {                        
            if (SystemConfig.HienThiDiemCuaKhachHangTrenHoaDon == 30)
            {
                //tính theo tổng doanh số
                return GetDiemTichLuy(DKHACHHANGID);
                
            }
            return 0;
        }

        private decimal GetDatTruoc()
        {
            string TDATHANGID = mapper[TDONHANGInfo.TDATHANGID].ToStringValue();
            decimal datTruoc = 0;
            if (TDATHANGID.Length > 0)
            {
                string sql = "SELECT THU FROM TTHUCHI WHERE TDATHANGID = '" + TDATHANGID + "'";
                datTruoc = Config.Db.GetFirstFieldDec(sql);
            }
            return datTruoc;
        }

        private void SelectMaVach()
        {
            txtMaVach.Select();
            txtMaVach.SelectAll();
        }


		public void btnInLaiBill_Click(Object sender, EventArgs e)
		{
            PrintInvoice(false);
            Track("In lại bill số " + txtNAME.Text + ", ngày: " + dtNGAY.DateTime.ToString("dd/MM/yyyy"));
		}


		public void btnTaoHoaDonMoi_Click(Object sender, EventArgs e)
		{
            if (btnThanhToan.Enabled)
            {
                if (grMua.RowCount + grTraLai.RowCount == 0)
                {
                    Msg.ShowInfo("Bạn đang ở trong hóa đơn mới");
                    SelectMaVach();
                    return;
                }

                //tạo hóa đơn nmới
                if (SystemConfig.HeThongChayNhieuMayTram == 0)
                ReLoad("", false);

                //xóa tất cả mặt hàng
                if (Msg.ShowYesNo("Bạn có muốn xóa hết các mặt hàng và nhập mới không?") == DialogResult.Yes)
                {
                    Track("Xóa tất cả các mặt hàng");
                    EmptyDonHang(mapper.ID);
                    ReLoad(mapper.ID, false);
                }
                else
                {
                    SelectMaVach();
                }
            }
            else
            {                
                ReLoad("", false);
            }
		}

        private void EmptyDonHang(string ID)
        {
            if (ID.Length == 0) return;
            Config.Db.ExecSql("DELETE FROM TDONHANGCHITIET WHERE TDONHANGID = '" + ID + "'");            
            TDONHANGRow upRow = new TDONHANGRow(ID);
            upRow.TONGCONG = 0;
            upRow.DOITRA = 0;
            upRow.TILETHUE = 0;
            upRow.TILEGIAMGIA = 0;
            upRow.TIENGIAMGIA = 0;
            upRow.TIENTHUE = 0;
            upRow.TIENHANG = 0;
            upRow.TDATHANGID = "";
            upRow.Update();
        }

        private void Track(string content)
        {
            if (!coLuuVet) return;

            TLUUVETRow row = new TLUUVETRow();
            row.GIO = Config.Db.DbDateTime;
            row.SODONHANG = mapper.ID;
            row.TAIKHOAN = DbConfig.UserName;
            row.NGAY = dtNGAY.DateTime;
            row.NOTE = content;
            row.Update();
        }


		public void btnLuuTam_Click(Object sender, EventArgs e)
		{
            if (btnThanhToan.Enabled)
            {
                if (grTraLai.RowCount + grMua.RowCount == 0)
                {
                    Msg.ShowWarning("Chỉ lưu tạm hóa đơn có mặt hàng");
                    return;
                }

                if (Msg.ShowYesNo("Bạn có muốn lưu lại hóa đơn và tạo hóa đơn mới không?") == DialogResult.Yes)
                {
                    if (mapper.ID.Length > 0)
                    {
                        //luu ra hoa don tam
                        TDONHANGRow row = new TDONHANGRow(mapper.ID);
                        TLUUTAMRow rowTam = new TLUUTAMRow();
                        rowTam.DNHANVIENXUATID = row.DNHANVIENXUATID;
                        rowTam.DKHOXUATID = row.DKHOXUATID;
                        rowTam.GIAOHANG = row.GIAOHANG;
                        rowTam.DIENGIAI = row.DIENGIAI;
                        rowTam.DKHACHHANGID = row.DKHACHHANGID;
                        rowTam.TILEGIAMGIA = row.TILEGIAMGIA;
                        rowTam.GIAMTHEOTIEN = row.GIAMTHEOTIEN;
                        rowTam.DOITRA = row.DOITRA;
                        rowTam.LOAIGIA = row.LOAIGIA;
                        rowTam.NAME = row.NAME;
                        rowTam.NOTE = row.NOTE;
                        rowTam.NGAY = row.NGAY;
                        rowTam.PHIVANCHUYEN = row.PHIVANCHUYEN;
                        rowTam.TDATHANGID = row.TDATHANGID;
                        rowTam.TIENTHUE = row.TIENTHUE;
                        rowTam.TIENGIAMGIA = row.TIENGIAMGIA;
                        rowTam.TIENHANG = row.TIENHANG;
                        row.TILEGIAMGIA = row.TILEGIAMGIA;
                        rowTam.TILETHUE = row.TILETHUE;
                        rowTam.TONGCONG = row.TONGCONG;
                        rowTam.Update();

                        DataTable dt = Config.Db.GetTable("SELECT * FROM TDONHANGCHITIET WHERE TDONHANGID = '" + mapper.ID + "'");
                        foreach (DataRow r in dt.Rows)
                        {
                            TLUUTAMCHITIETRow ctRowTam = new TLUUTAMCHITIETRow();
                            TDONHANGCHITIETRow ctRow = new TDONHANGCHITIETRow(r);
                            ctRowTam.BAOHANH = ctRow.BAOHANH;
                            ctRowTam.DMATHANGID = ctRow.DMATHANGID;
                            ctRowTam.DONGIA = ctRow.DONGIA;
                            ctRowTam.NOTE = ctRow.NOTE;
                            ctRowTam.SOLUONG = ctRow.SLXUAT - ctRow.SLNHAP;
                            ctRowTam.SOLUONGCHUAQUYDOI = ctRow.SLXUATCHUAQUYDOI - ctRow.SLNHAPCHUAQUYDOI;
                            ctRowTam.DDONVITINHID = ctRow.DDONVITINHID;
                            ctRowTam.TILEGIAMGIA = ctRow.TILEGIAMGIA;
                            ctRowTam.TLUUTAMID = rowTam.ID;
                            ctRowTam.THANHTIEN = ctRow.THANHTIEN;
                            ctRowTam.Update();
                        }                        
                    }
                    else
                    {
                        TLUUTAMRow rowTam = new TLUUTAMRow();
                        rowTam.DNHANVIENXUATID = lueDNHANVIENXUATID.StringValue;
                        rowTam.DKHOXUATID = lueDKHOXUATID.StringValue;
                        rowTam.GIAOHANG = txtGIAOHANG.Text;
                        rowTam.DIENGIAI = txtDIENGIAI.Text;
                        rowTam.DKHACHHANGID = lueDKHACHHANGID.StringValue;
                        rowTam.TILEGIAMGIA = numTILEGIAMGIA.Value;
                        rowTam.GIAMTHEOTIEN = mapper[TDONHANGInfo.GIAMTHEOTIEN].ToInt();
                        rowTam.DOITRA = numDOITRA.Value;
                        rowTam.LOAIGIA = mapper[TDONHANGInfo.LOAIGIA].ToInt();
                        rowTam.NAME = txtNAME.Text;
                        rowTam.NOTE = txtNOTE.Text;
                        rowTam.NGAY = dtNGAY.DateTime;
                        rowTam.PHIVANCHUYEN = numPHIVANCHUYEN.Value;
                        rowTam.TDATHANGID = "";
                        rowTam.TIENTHUE = numTIENTHUE.Value;
                        rowTam.TIENGIAMGIA = numTIENGIAMGIA.Value;
                        rowTam.TIENHANG = numTIENHANG.Value;                       
                        rowTam.TILETHUE = numTILETHUE.Value;
                        rowTam.TONGCONG = numTONGCONG.Value;
                        rowTam.Update();

                        DataTable dt = grMua.DataSource as DataTable;
                        foreach (DataRow r in dt.Rows)
                        {
                            if (r.RowState == DataRowState.Deleted) continue;
                            TLUUTAMCHITIETRow ctRowTam = new TLUUTAMCHITIETRow();
                            TDONHANGCHITIETRow ctRow = new TDONHANGCHITIETRow(r);
                            ctRowTam.BAOHANH = ctRow.BAOHANH;
                            ctRowTam.DMATHANGID = ctRow.DMATHANGID;
                            ctRowTam.DONGIA = ctRow.DONGIA;
                            ctRowTam.NOTE = ctRow.NOTE;
                            ctRowTam.SOLUONG = ctRow.SLXUAT;
                            ctRowTam.SOLUONGCHUAQUYDOI = ctRow.SLXUATCHUAQUYDOI;
                            ctRowTam.DDONVITINHID = ctRow.DDONVITINHID;
                            ctRowTam.TILEGIAMGIA = ctRow.TILEGIAMGIA;
                            ctRowTam.TLUUTAMID = rowTam.ID;
                            ctRowTam.THANHTIEN = ctRow.THANHTIEN;
                            ctRowTam.Update();
                        }

                        dt = grTraLai.DataSource as DataTable;
                        foreach (DataRow r in dt.Rows)
                        {
                            if (r.RowState == DataRowState.Deleted) continue;
                            TLUUTAMCHITIETRow ctRowTam = new TLUUTAMCHITIETRow();
                            TDONHANGCHITIETRow ctRow = new TDONHANGCHITIETRow(r);
                            ctRowTam.BAOHANH = ctRow.BAOHANH;
                            ctRowTam.DMATHANGID = ctRow.DMATHANGID;
                            ctRowTam.DONGIA = ctRow.DONGIA;
                            ctRowTam.NOTE = ctRow.NOTE;
                            ctRowTam.SOLUONG = -ctRow.SLNHAP;
                            ctRowTam.SOLUONGCHUAQUYDOI = -ctRow.SLNHAPCHUAQUYDOI;
                            ctRowTam.DDONVITINHID = ctRow.DDONVITINHID;
                            ctRowTam.TILEGIAMGIA = ctRow.TILEGIAMGIA;
                            ctRowTam.TLUUTAMID = rowTam.ID;
                            ctRowTam.THANHTIEN = ctRow.THANHTIEN;
                            ctRowTam.Update();
                        }       
                    }

                    EmptyDonHang(mapper.ID);
                    ReLoad(mapper.ID, false);
                }
            }
            else
            {
                Msg.ShowWarning("Chỉ có thể lưu tạm với hóa đơn chưa thanh toán");
            }
		}


		public void btnMoLai_Click(Object sender, EventArgs e)
		{
            if (grMua.RowCount + grTraLai.RowCount == 0)
            {
                ChonHoaDonTam form = (ChonHoaDonTam)Config.CreateForm(Forms.ChonHoaDonTam);
                if (form.form.ShowDialog() == DialogResult.OK)
                {
                    if (mapper.ID.Length == 0) mapper.RaiseOnCalculation();

                    string TLUUTAMID = form.SelectedID;
                    TLUUTAMRow row = new TLUUTAMRow(TLUUTAMID);
                    DataTable dt = Config.Db.GetTable("SELECT TLUUTAMCHITIET.*, (SELECT NAME FROM DMATHANG WHERE ID = DMATHANGID) AS DMATHANG_NAME, (SELECT CODE FROM DMATHANG WHERE ID = DMATHANGID) AS DMATHANG_CODE, (SELECT NAME FROM DDONVITINH WHERE ID = DDONVITINHID) AS DDONVITINH_NAME FROM TLUUTAMCHITIET WHERE TLUUTAMID = '" + TLUUTAMID + "'");

                    if (SystemConfig.HeThongChayNhieuMayTram == 30)
                    {
                        lueDNHANVIENXUATID.EditValue = row.DNHANVIENXUATID;
                        lueDKHOXUATID.EditValue = row.DKHOXUATID;
                        txtGIAOHANG.Text = row.GIAOHANG;
                        txtDIENGIAI.Text = row.DIENGIAI;
                        lueDKHACHHANGID.EditValue = row.DKHACHHANGID;
                        numTILEGIAMGIA.Value = row.TILEGIAMGIA;
                        mapper[TDONHANGInfo.GIAMTHEOTIEN].Value = row.GIAMTHEOTIEN;
                        mapper[TDONHANGInfo.LOAIGIA].Value = row.LOAIGIA;
                        txtNOTE.Text = row.NOTE;
                        numPHIVANCHUYEN.Value = row.PHIVANCHUYEN;
                        numTIENGIAMGIA.Value = row.TIENGIAMGIA;
                        numTIENHANG.Value = row.TIENHANG;
                        numTILEGIAMGIA.Value = row.TILEGIAMGIA;
                        numTILETHUE.Value = row.TILETHUE;
                        numTONGCONG.Value = row.TONGCONG;

                        foreach (DataRow r in dt.Rows)
                        {
                            TLUUTAMCHITIETRow ctRow = new TLUUTAMCHITIETRow(r);
                            if (ctRow.SOLUONGCHUAQUYDOI > 0)
                            {
                                DataRow newRow = grMua.DataSource.NewRow();
                                TDONHANGCHITIETRow newCtRow = new TDONHANGCHITIETRow(newRow);
                                newCtRow.DMATHANGID = ctRow.DMATHANGID;
                                newCtRow.DDONVITINHID = ctRow.DDONVITINHID;
                                newCtRow.BAOHANH = ctRow.BAOHANH;
                                newCtRow.DONGIA = ctRow.DONGIA;
                                newCtRow.SLXUAT = ctRow.SOLUONG;
                                newCtRow.TILEGIAMGIA = ctRow.TILEGIAMGIA;
                                newCtRow.SLXUATCHUAQUYDOI = ctRow.SOLUONGCHUAQUYDOI;
                                newCtRow.NOTE = ctRow.NOTE;
                                newCtRow["DMATHANG_NAME"] = ctRow["DMATHANG_NAME"];
                                newCtRow["DMATHANG_CODE"] = ctRow["DMATHANG_CODE"];
                                newCtRow["DDONVITINH_NAME"] = ctRow["DDONVITINH_NAME"];
                                grMua.AddNewRow(newRow);
                                grMua.CalculateRow(newRow);
                            }
                            else
                            {
                                DataRow newRow = grTraLai.DataSource.NewRow();
                                TDONHANGCHITIETRow newCtRow = new TDONHANGCHITIETRow(newRow);
                                newCtRow.DMATHANGID = ctRow.DMATHANGID;
                                newCtRow.DDONVITINHID = ctRow.DDONVITINHID;
                                newCtRow.BAOHANH = ctRow.BAOHANH;
                                newCtRow.DONGIA = ctRow.DONGIA;
                                newCtRow.SLNHAP = -ctRow.SOLUONG;
                                newCtRow.TILEGIAMGIA = ctRow.TILEGIAMGIA;
                                newCtRow.SLNHAPCHUAQUYDOI = -ctRow.SOLUONGCHUAQUYDOI;
                                newCtRow.NOTE = ctRow.NOTE;
                                newCtRow["DMATHANG_NAME"] = ctRow["DMATHANG_NAME"];
                                newCtRow["DMATHANG_CODE"] = ctRow["DMATHANG_CODE"];
                                newCtRow["DDONVITINH_NAME"] = ctRow["DDONVITINH_NAME"];
                                grTraLai.AddNewRow(newRow);
                                grTraLai.CalculateRow(newRow);
                            }
                        }

                        mapper.RaiseOnCalculation();
                    }
                    else
                    {
                        TDONHANGRow rowTam = new TDONHANGRow(mapper.ID);
                        rowTam.DNHANVIENXUATID = row.DNHANVIENXUATID;
                        rowTam.DKHOXUATID = row.DKHOXUATID;
                        rowTam.GIAOHANG = row.GIAOHANG;
                        rowTam.DIENGIAI = row.DIENGIAI;
                        rowTam.DKHACHHANGID = row.DKHACHHANGID;
                        rowTam.TILEGIAMGIA = row.TILEGIAMGIA;
                        rowTam.GIAMTHEOTIEN = row.GIAMTHEOTIEN;
                        rowTam.DOITRA = row.DOITRA;
                        rowTam.LOAIGIA = row.LOAIGIA;
                        rowTam.NAME = row.NAME;
                        rowTam.NOTE = row.NOTE;
                        rowTam.NGAY = row.NGAY;
                        rowTam.PHIVANCHUYEN = row.PHIVANCHUYEN;
                        rowTam.TDATHANGID = row.TDATHANGID;
                        rowTam.TIENTHUE = row.TIENTHUE;
                        rowTam.TIENGIAMGIA = row.TIENGIAMGIA;
                        rowTam.TIENHANG = row.TIENHANG;
                        row.TILEGIAMGIA = row.TILEGIAMGIA;
                        rowTam.TILETHUE = row.TILETHUE;
                        rowTam.TONGCONG = row.TONGCONG;
                        rowTam.Update();

                        foreach (DataRow r in dt.Rows)
                        {
                            TDONHANGCHITIETRow ctRowTam = new TDONHANGCHITIETRow();
                            TLUUTAMCHITIETRow ctRow = new TLUUTAMCHITIETRow(r);
                            ctRowTam.BAOHANH = ctRow.BAOHANH;
                            ctRowTam.DMATHANGID = ctRow.DMATHANGID;
                            ctRowTam.DONGIA = ctRow.DONGIA;
                            ctRowTam.NOTE = ctRow.NOTE;
                            ctRowTam.DDONVITINHID = ctRow.DDONVITINHID;
                            ctRowTam.SLXUAT = ctRow.SOLUONG > 0 ? ctRow.SOLUONG : 0;
                            ctRowTam.SLNHAP = ctRow.SOLUONG < 0 ? -ctRow.SOLUONG : 0;

                            ctRowTam.SLXUATCHUAQUYDOI = ctRow.SOLUONGCHUAQUYDOI > 0 ? ctRow.SOLUONGCHUAQUYDOI : 0;
                            ctRowTam.SLNHAPCHUAQUYDOI = ctRow.SOLUONGCHUAQUYDOI < 0 ? -ctRow.SOLUONGCHUAQUYDOI : 0;

                            ctRowTam.TILEGIAMGIA = ctRow.TILEGIAMGIA;
                            ctRowTam.TDONHANGID = rowTam.ID;
                            ctRowTam.THANHTIEN = ctRow.THANHTIEN;
                            ctRowTam.Update();
                        }

                        ReLoad(mapper.ID, false);
                    }                    

                    Config.Db.ExecSql("DELETE FROM TLUUTAM WHERE ID = '" + TLUUTAMID + "'");
                    Config.Db.ExecSql("DELETE FROM TLUUTAMCHITIET WHERE TLUUTAMID = '" + TLUUTAMID + "'");                    
                }
            }
            else
            {
                Msg.ShowWarning("Mời bạn tạo mới hóa đơn trước");
            }
		}

        /// <summary>
        /// Lấy số lượng mặt hàng
        /// </summary>
        /// <param name="cancel"></param>
        /// <returns>3 trường hợp: hỗ trợ cân điện tử, nhập trực tiếp hoặc hiển thị cửa sổ</returns>
        private void GetSoLuong(ref bool cancel, ref decimal soLuong, ref decimal soLuongChan, ref decimal chietKhau, ref decimal donGia, ref decimal donGiaChan, ref decimal QuyDoi, int theoCan, ref string KichThuoc, ref object hanSuDung)
        {
            chietKhau = 0;
            GetKhuyenMaiTheoMatHang(grMatHang.SelectedID, ref donGia, ref chietKhau);
            //kiem tra xem co 2 don vi tinh khong?
            DMATHANGRow mhRow = new DMATHANGRow(grMatHang.SelectedRow);            
            if (theoCan == 0 && ((coHanSd && mhRow.COHANSUDUNG == 30) || coSize || hienThiCuaSoNhapSoLuong || (suDung2DVT && TonKhoHandler.Has2DonViTinh(mhRow))))
            {
                NhapSoLuong obj = (NhapSoLuong)Config.CreateForm(Forms.NhapSoLuong);
                obj.SetMatHang(grMatHang.SelectedID, lueDKHOXUATID.StringValue, donGia, soLuong);
                obj.numCK.Value = chietKhau;
                if (obj.No1Form1.ShowDialog() == DialogResult.OK)
                {
                    soLuong = obj.spSoLuong.Value;
                    soLuongChan = obj.spSLChan.Value;
                    if (soLuongChan > 0) QuyDoi = mhRow.QUYDOI;
                    chietKhau = obj.numCK.Value;
                    KichThuoc = obj.KichThuoc;
                    hanSuDung = obj.HanSuDung;
                }
                else
                {
                    cancel = true;                   
                }
            }
            else
            {
                if (theoCan == 0)
                {
                    if (numSL.Value == 0) soLuong = 1;
                    else soLuong = numSL.Value;
                }
            }
        }

        private void GetKhuyenMaiTheoMatHang(string DMATHANGID, ref decimal DonGia, ref decimal chietKhau)
        {
            DataTable dtKm = GetKhuyenMaiDangSuDung(LOAIHINHKHUYENMAI.GIAMGIATHEOSANPHAM);
            if (dtKm.Rows.Count > 0)
            {
                string where = GetWhere(dtKm);
                //kiem tra xem san pham co trong chi tiet khong
                chietKhau = Config.Db.GetFirstFieldDec("SELECT MAX(TILEGIAMGIA) FROM DDOTKHUYENMAICHITIET WHERE DMATHANGID = '" + DMATHANGID + "' AND " + where);
            }

            dtKm = GetKhuyenMaiDangSuDung(LOAIHINHKHUYENMAI.GIAMGIATHEONHOM);
            if (dtKm.Rows.Count > 0)
            {
                string where = GetWhere(dtKm);
                decimal val = Config.Db.GetFirstFieldDec("SELECT MAX(TILEGIAMGIA) FROM DDOTKHUYENMAICHITIET WHERE DNHOMMATHANGID = (SELECT DNHOMMATHANGID FROM DMATHANG WHERE ID = '" + DMATHANGID + "') AND " + where);
                if (val > chietKhau)
                    chietKhau = val;
            }

            dtKm = GetKhuyenMaiDangSuDung(LOAIHINHKHUYENMAI.MATHANGDONGGIA);
            if (dtKm.Rows.Count > 0)
            {
                string where = GetWhere(dtKm);
                decimal val = Config.Db.GetFirstFieldDec("SELECT MIN(GIABAN) FROM DDOTKHUYENMAICHITIET WHERE DMATHANGID = '" + DMATHANGID + "' AND " + where);
                if (val > 0) DonGia = val;
            }
        }

        private string GetWhere(DataTable dtKm)
        {
            string where = "";
            foreach (DataRow r in dtKm.Rows)
            {
                if (where.Length > 0) where += " OR ";
                where += "DDOTKHUYENMAIID = '" + r["ID"].ToString() + "'";
            }
            return "(" + where + ")";
        }

        private DataTable GetKhuyenMaiDangSuDung(string DLOAIHINHKHUYENMAIID)
        {
            if (SystemConfig.KichHoatKhuyenMaiTuDong == 0) return new DataTable();
            //lấy ra khuyến mại đang áp dụng            
            DataTable dt = DbUtils.Select("*", Tables.DDOTKHUYENMAI, "CURRENT_DATE BETWEEN TUNGAY AND DENNGAY AND STATUS = 30 AND COALESCE(NGUNGAPDUNG, 0) = 0 AND DLOAIHINHKHUYENMAIID = '" + DLOAIHINHKHUYENMAIID + "'");
            return dt;
        }

        decimal lastSlLe;
        decimal lastSlChan;
        decimal lastCk;
        decimal lastDonGia;
        decimal lastDonGiaChan;
		public void grMatHang_OnAddingRowToGrid(DataRow selRow, DataRow newRow, ref bool cancel)
		{
            if (lastSlLe == 0)
            {
                cancel = true;
            }
            else
            {
                if (SystemConfig.SuDungCanDienTu == 30 && txtMaVach.Text.StartsWith(canBatDau) && txtMaVach.Text.Length > 6)
                {
                    lastSlLe = ConvertTo.Decimal(txtMaVach.Text.Substring(txtMaVach.Text.Length - 6, 5)) / 1000;
                }
                
                newRow["DMATHANGID"] = selRow["ID"];
                newRow["DDONVITINHID"] = selRow["DDONVITINHID"];
                newRow["DDONVITINH_NAME"] = selRow["DDONVITINH_NAME"];
                newRow["DMATHANG_NAME"] = selRow["NAME"];
                newRow["DMATHANG_CODE"] = selRow["CODE"];
                newRow["DMATHANG_MASANCO"] = selRow["MASANCO"];
                newRow["DONGIA"] = lastDonGia;
                newRow["SLXUATCHUAQUYDOI"] = lastSlLe;
                newRow["KICHTHUOC"] = lastKichThuoc;
                if (lastHsd != null) newRow["HANSUDUNG"] = lastHsd;
                newRow["SLXUAT"] = lastSlLe;                
                newRow["TILEGIAMGIA"] = lastCk;

                WriteToCustomerDisplay(selRow["NAME"].ToString(), lastSlLe.ToString() + "x" + lastDonGia.ToString("n0") + "=" + (lastSlLe * lastDonGia).ToString("n0"));

                if (SystemConfig.GiamGiaMacDinh == 30)
                {
                    DMATHANGRow mhRow = new DMATHANGRow(selRow["ID"].ToString());
                    if (mhRow.MACDINHGIAMGIA > 0)
                    {
                        newRow["TILEGIAMGIA"] = mhRow.MACDINHGIAMGIA;
                    }
                    else
                    {
                        newRow["TIENGIAMGIA"] = mhRow.MACDINHGIAMTIEN;
                        newRow["GIAMTHEOTIEN"] = 30;
                    }
                }
                newRow["DKHOHANGID"] = lueDKHOXUATID.StringValue;
                if (coLuuVet)
                    Track("Thêm " + lastSlLe.ToString() + " '" + selRow["NAME"].ToString() + "', đơn giá " + lastDonGia.ToString("n0") + ", chiết khấu " + lastCk + "%");
            }
            numSL.Value = 1;
		}


		public Boolean grMatHang_OnAddSameItem(DataRow selRow, DataRow row)
		{
            if (printing) return false;
            if (SystemConfig.SuDungCanDienTu == 30 && txtMaVach.Text.StartsWith(canBatDau) && txtMaVach.Text.Length > 6)
            {
                lastSlLe = ConvertTo.Decimal(txtMaVach.Text.Substring(txtMaVach.Text.Length - 6, 5)) / 1000;
            }

            if (lastSlLe != 0 && selRow["ID"].ToString() == row["DMATHANGID"].ToString() && row["DDONVITINHID"].ToString() == selRow["DDONVITINHID"].ToString() && ConvertTo.Decimal(row["TILEGIAMGIA"]) == lastCk && SystemConfig.NhapMotMatHangNhieuLanTrongPhieu != 30)
            {
                if (coSize && lastKichThuoc != row["KICHTHUOC"].ToString())
                {
                    return false;
                }
                if (coHanSd && lastHsd != row["HANSUDUNG"])
                {
                    return false;
                }

                row["SLXUATCHUAQUYDOI"] = ConvertTo.Decimal(row["SLXUATCHUAQUYDOI"]) + lastSlLe;
                row["SLXUAT"] = ConvertTo.Decimal(row["SLXUAT"]) + lastSlLe;
                Track("Thêm " + lastSlLe.ToString() + " '" + selRow["NAME"].ToString() + "', chiết khấu " + lastCk + "%");
                numSL.Value = 1;
                return true;
            }
            return false;
		}


		public void btnXoa_Click(Object sender, EventArgs e)
		{
            GridMapper grid = tabMuaTra.SelectedIndex == 0 ? grMua : grTraLai;

            if (grid.GridView.SelectedRows.Count == 0) return;
            if (!btnThanhToan.Enabled)
            {
                Msg.ShowWarning("Hóa đơn này đã thanh toán!");
                return;
            }
            if (Msg.ShowYesNo("Bạn có muốn xóa mặt hàng đang chọn ra khỏi hóa đơn không?") == DialogResult.Yes)
            {
                foreach (DataGridViewRow r in grid.GridView.SelectedRows)
                {
                    DataRow row = (r.DataBoundItem as DataRowView).Row;
                    TDONHANGCHITIETRow ctRow = new TDONHANGCHITIETRow(row);
                    Track("Xóa mặt hàng '" + ctRow["DMATHANG_NAME"].ToString() + "'");
                }

                grid.DeleteSelectedRows();
                SelectMaVach();
            }
		}		


		public void lblBack_Click(Object sender, EventArgs e)
		{
            LoadInvoice(-1);
		}

		public void lblNext_Click(Object sender, EventArgs e)
		{
			LoadInvoice(1);
		}

        private void LoadInvoice(int mode)
        {
            bool back = mode == -1;

            FbCommand cmd = Config.Db.GetCommand("");
            string sql = "SELECT FIRST 1 ID FROM TDONHANG WHERE STATUS = 30 AND LOAI = 0  AND USERCREATEDID = '" + DbConfig.UserID + "'";
            if (mapper.ID.Length > 0)
            {
                TDONHANGRow row = new TDONHANGRow(mapper.ID);
                if (!row.IsNull)
                {
                    if (back)
                        sql += " AND TIMECREATED < @TIMECREATED";
                    else
                        sql += " AND TIMECREATED > @TIMECREATED";
                    cmd.Parameters.Add("@TIMECREATED", FbDbType.TimeStamp).Value = row["TIMECREATED"];
                }
            }
            else if (!back) return;

            cmd.CommandText = sql + " ORDER BY TIMECREATED " + (back ? "DESC" : "ASC");
            DataRow r = Config.Db.GetFirstRow(cmd);
            if (r != null) ReLoad(r["ID"].ToString(), false);
            else
            {
                if (back)
                {
                    Msg.ShowWarning("Không còn đơn hàng nào ở phía trước đơn hàng hiện tại");
                }
                else
                {
                    //tải đơn hàng mới
                    ReLoad("", false);
                }
            }
        }

        private bool reloading = false;
        public void ReLoad(string ID, bool onLoad)
        {
            reloading = true;

            //update
            if (ID.Length == 0 && !nhieuMayTram)
            {
                object value = Config.Db.GetFirstField("SELECT FIRST 1 ID FROM TDONHANG WHERE (DATHANHTOAN IS NULL OR DATHANHTOAN = 0) AND LOAI = 0 AND USERCREATEDID = '" + DbConfig.UserID + "'");
                ID = value == null ? "" : value.ToString();
            }

            //end update
            mapper.Fill(ID);
            if (ID.Length == 0)
                mapper.EmptyControl();
            bool daThanhToan = false;

            //tính lại số hóa đơn
            if (SoHoaDon.Length == 0)
            {
                STABLEDESCRow tblRow = Config.GetTableDesc(Tables.TDONHANG);
                SoHoaDon = Config.Db.GetFirstFieldString("SELECT NOTEMPLATE FROM SFORM WHERE FORMTYPE = 0 AND LOAI = 0 AND STABLEDESCID = '" + tblRow.ID + "'");
            }

            if (ID.Length == 0)
            {
                //tải dữ liệu trống
                dtNGAY.EditValue = Config.Db.DbDate;                                

                lueDKHOXUATID.EditValue = Shared.DKHOHANGID;
                mapper[TDONHANGInfo.DCUAHANGID].Value = Shared.DCUAHANGID;
                txtDIENGIAI.Text = "Xuất bán hàng";                     
                
                btnThanhToan.Enabled = true;
                btnLuuTam.Enabled = true;
                btnMoLai.Enabled = true;
                btnInLaiBill.Visible = false;
                mapper[TDONHANGInfo.LOAI].Value = 0;
                btnLoaiGia.Text = "Giá bán";
                numTILEGIAMGIA.Value = SystemConfig.MacDinhGiamGia;
            }
            else
            {                
                TDONHANGRow dhRow = new TDONHANGRow(ID);
                if (dhRow.DATHANHTOAN == 30)
                {
                    daThanhToan = true;
                }
                else
                {
                    if (SystemConfig.ChoPhepThayDoiNgayTrenHoaDon == 0 || onLoad)
                        dtNGAY.DateTime = Config.Db.DbDate;
                }

                int val = mapper[TDONHANGInfo.LOAIGIA].ToInt();
                btnLoaiGia.Text = GetDienGiai(val);
            }

            if (!daThanhToan)
            {                
                RefreshOrderNo();
                grMua.CalculateAllRows();
                UpdateTotalWithoutSave(); 
            }
            
            btnLoaiGia.Enabled = !daThanhToan;
            txtDIENGIAI.Enabled = !daThanhToan;
            lueDKHOXUATID.Enabled = !daThanhToan;
            dtNGAY.Enabled = !daThanhToan && SystemConfig.ChoPhepThayDoiNgayTrenHoaDon == 30;
            grMua.ReadOnly = daThanhToan;
            grTraLai.ReadOnly = daThanhToan;
            lueDNHANVIENXUATID.Enabled = !daThanhToan;
            txtGIAOHANG.Enabled = !daThanhToan;

            dtNGAY.Enabled = SystemConfig.ChoPhepThayDoiNgayTrenHoaDon == 30;
            //numDATTRUOC.Enabled = !daThanhToan;
            bool giamGiaEn = !daThanhToan && SystemConfig.ChoPhepNhapGiamGia == 30;
            numTILEGIAMGIA.Enabled = giamGiaEn;
            numTIENGIAMGIA.Enabled = giamGiaEn;
            numTILETHUE.Enabled = !daThanhToan;
            numPHIVANCHUYEN.Enabled = !daThanhToan;
            lueDKHACHHANGID.Enabled = !daThanhToan;     
            lueDNHANVIENGIAOID.Enabled = !daThanhToan;
            numTRICHNHANVIEN.Enabled = !daThanhToan;

            btnInLaiBill.Visible = daThanhToan;
            btnThanhToan.Enabled = !daThanhToan;
            btnLuuTam.Enabled = !daThanhToan;
            btnMoLai.Enabled = !daThanhToan;
            txtNOTE.Enabled = !daThanhToan;

            UpdateXoaButton();

            SelectMaVach();
            reloading = false;
        }

        private void RefreshOrderNo()
        {
            txtNAME.Text = DbUtils.GenSoHoaDon(dtNGAY.DateTime, SoHoaDon, Tables.TDONHANG, TDONHANGInfo.NAME, TDONHANGInfo.NGAY, "LOAI = 0 AND DATHANHTOAN = 30");
        }

        private string GetDienGiai(int loaiGia)
        {
            if (loaiGia == 1)
                return SystemConfig.DienGiaiGia2;
            else if (loaiGia == 2)
                return SystemConfig.DienGiaiGia3;
            else if (loaiGia == 3)
                return SystemConfig.DienGiaiGia4;
            return "Giá bán";
        }


		public void mapper_OnCalculation(Object sender, EventArgs e)
		{
            if (reloading) return;
            if (!(numTILEGIAMGIA.Focused || numTIENGIAMGIA.Focused))
            {
                CapNhatGiamGiaTuDong();
            }

            UpdateTotalWithoutSave();

            if (!nhieuMayTram)
            {
                mapper.Update();
            }
		}

        private void UpdateTotalWithoutSave()
        {
            decimal thanhTien = grMua.CalcSum("THANHTIEN");
            decimal doiTra = grTraLai.CalcSum("THANHTIEN");
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

            numDOITRA.Value = -doiTra;
            numTIENTHUE.Value = numTILETHUE.Value * (numTIENHANG.Value - numTIENGIAMGIA.Value) / 100;
            decimal tongCong = numTIENHANG.Value - numTIENGIAMGIA.Value + numTIENTHUE.Value + numPHIVANCHUYEN.Value - numDOITRA.Value;
            if (lamTronTien > 0) tongCong = lamTronTien * Math.Round(tongCong / lamTronTien);
            numTONGCONG.Value = tongCong;
        }

        private decimal GetTiLeGiamTheoKhach()
        {
            decimal tiLeGiam = SystemConfig.MacDinhGiamGia;

            if (lueDKHACHHANGID.StringValue.Length > 0)
            {
                DKHACHHANGRow row = new DKHACHHANGRow(lueDKHACHHANGID.StringValue);
                if (row.DNHOMKHACHHANGID.Length > 0)
                {
                    DNHOMKHACHHANGRow nhomRow = new DNHOMKHACHHANGRow(row.DNHOMKHACHHANGID);
                    if (!nhomRow.IsNull)
                    {
                        tiLeGiam = nhomRow.TILEGIAMGIA;
                    }
                }
            }
            return tiLeGiam;
        }    

		public void lueDKHACHHANGID_OnEditValueChanged(Object sender, Object value)
		{
            if (reloading) return;
            
            mapper["GIAMTHEOTIEN"].Value = 0;

            decimal tiLeTheoKhach = GetTiLeGiamTheoKhach();
            decimal tiLe = GetTiLeGiamTheoKM();
            //nếu có km thì tính
            if (tiLe > tiLeTheoKhach)
            {
                tiLeTheoKhach = tiLe;
            }

            numTILEGIAMGIA.Value = tiLeTheoKhach;
            //giá theo khách hàng
            if (SystemConfig.SuDungGiaTheoKhach == 30)
            {
                int LoaiGia = Config.Db.GetFirstFieldInt("SELECT GIABAN FROM DKHACHHANG WHERE ID = '" + lueDKHACHHANGID.StringValue + "'");
                int val = mapper[TDONHANGInfo.LOAIGIA].ToInt();
                if (val != LoaiGia)
                {
                    SetLoaiGia(LoaiGia, true);
                }
            }
		}

        private void SetLoaiGia(int loaiGia, bool apDungTatCa)
        {
            mapper[TDONHANGInfo.LOAIGIA].Value = loaiGia;
            foreach (DataGridViewRow r in grMua.GridView.Rows)
            {
                if (!apDungTatCa && !r.Selected) continue;

                TDONHANGCHITIETRow row = new TDONHANGCHITIETRow((r.DataBoundItem as DataRowView).Row);
                DMATHANGRow mhRow = new DMATHANGRow(row.DMATHANGID);
                switch (loaiGia)
                {
                    case 1:
                        row.DONGIA = mhRow.GIABAN2;
                        break;
                    case 2:
                        row.DONGIA = mhRow.GIABAN3;
                        break;
                    case 3:
                        row.DONGIA = mhRow.GIABAN4;
                        break;
                    case 4: //gia ban gan nhat
                        string sql = @"SELECT DONGIA FROM TDONHANG INNER JOIN TDONHANGCHITIET ON TDONHANG.ID = TDONHANGCHITIET.TDONHANGID WHERE
DATHANHTOAN = 30 AND DKHACHHANGID = '" + lueDKHACHHANGID.StringValue + "' AND DMATHANGID = '" + row.DMATHANGID + "' AND DDONVITINHID = '" + row.DDONVITINHID + "' ORDER BY NGAY DESC, TDONHANG.TIMECREATED DESC";
                        row.DONGIA = Config.Db.GetFirstFieldDec(sql);
                        break;
                    default:
                        row.DONGIA = mhRow.GIABAN;
                        break;
                }
                grMua.CalculateRow(row.Row);
            }
            btnLoaiGia.Text = GetDienGiai(loaiGia);
        }

		public void btnLoaiGia_Click(Object sender, EventArgs e)
		{
            ChonLoaiGia form = (ChonLoaiGia)Config.CreateForm(Forms.ChonLoaiGia);
            if (form.form.ShowDialog() == DialogResult.OK)
            {
                int val = mapper[TDONHANGInfo.LOAIGIA].ToInt();
                if (form.LoaiGia != val)
                {
                    if (grMua.RowCount > 0)
                    {
                        if (Msg.ShowYesNo("Bạn có muốn chuyển mặt hàng từ '" + GetDienGiai(val) + "' thành '" + GetDienGiai(form.LoaiGia) + "' không?") == DialogResult.Yes)
                        {
                            SetLoaiGia(form.LoaiGia, form.chkApDungTatCa.Checked);

                            Track("Đổi loại giá bán từ '" + GetDienGiai(mapper[TDONHANGInfo.LOAIGIA].ToInt()) + "' sang '" + GetDienGiai(form.LoaiGia) + "'");
                        }
                    }
                    else
                    {
                        mapper[TDONHANGInfo.LOAIGIA].Value = form.LoaiGia;
                        btnLoaiGia.Text = GetDienGiai(form.LoaiGia);
                        Track("Đổi loại giá bán từ '" + GetDienGiai(mapper[TDONHANGInfo.LOAIGIA].ToInt()) + "' sang '" + GetDienGiai(form.LoaiGia) + "'");                        
                    }

                    mapper.RaiseOnCalculation();
                }
            }
		}


		public void tsbChonMatHang_Click(Object sender, EventArgs e)
		{
            if (btnThanhToan.Enabled)
            {
                ChonMatHangTra form = (ChonMatHangTra)Config.CreateForm(Forms.ChonMatHangTra);
                form.Load(grTraLai.DataSource);
                if (form.form.ShowDialog() == DialogResult.OK)
                {
                    DataTable dt = grTraLai.DataSource as DataTable;
                    if (form.DoiTheoDon)
                    {
                        foreach (DataRow r in form.Rows)
                        {
                            string CHITIETID = r["ID"].ToString();
                            DataRow[] rows = dt.Select("TDONHANGTRAID='" + CHITIETID + "'");
                            if (rows.Length == 0)
                            {
                                DataRow newRow = dt.NewRow();
                                TDONHANGCHITIETRow refRow = new TDONHANGCHITIETRow(CHITIETID);
                                TDONHANGRow refDhRow = new TDONHANGRow(refRow.TDONHANGID);

                                DMATHANGRow spRow = new DMATHANGRow(refRow.DMATHANGID);

                                newRow["DMATHANGID"] = spRow.ID;
                                newRow["DMATHANG_NAME"] = spRow.NAME;
                                newRow["DMATHANG_CODE"] = spRow.CODE;
                                newRow["DMATHANG_MASANCO"] = spRow.MASANCO;                                                                

                                newRow["DDONVITINHID"] = spRow.DDONVITINHID;
                                newRow["DDONVITINH_NAME"] = spRow.DDONVITINHID.Length == 0 ? "" : new DDONVITINHRow(spRow.DDONVITINHID).NAME;

                                newRow["DONGIA"] = refRow.DONGIA * (1 + refDhRow.TILETHUE / 100);
                                newRow["TILEGIAMGIA"] = 100 - (100 - refRow.TILEGIAMGIA) / 100 * (100 - refDhRow.TILEGIAMGIA);

                                newRow["DDONVITINHID"] = refRow.DDONVITINHID;
                                newRow["DDONVITINH_NAME"] = refRow.DDONVITINHID.Length == 0 ? "" : new DDONVITINHRow(refRow.DDONVITINHID).NAME;
                                decimal quyDoi = 1;
                                if (refRow.DDONVITINHID != spRow.DDONVITINHID)
                                {
                                    quyDoi = spRow.QUYDOI;
                                }
                                newRow["SLNHAPCHUAQUYDOI"] = ConvertTo.Decimal(r["SLTRA"]);
                                newRow["SLNHAP"] = quyDoi * ConvertTo.Decimal(r["SLTRA"]);
                                newRow["TDONHANGTRAID"] = refRow.ID;
                                if (!refRow.IsNullValue(TDONHANGCHITIETInfo.HANSUDUNG))
                                    newRow["HANSUDUNG"] = refRow.HANSUDUNG;
                                newRow["KICHTHUOC"] = refRow.KICHTHUOC;
                                dt.Rows.Add(newRow);
                                grTraLai.CalculateRow(newRow);      
                            }
                            else
                            {
                                rows[0]["SLNHAPCHUAQUYDOI"] = ConvertTo.Decimal(r["SLTRA"]);
                                grTraLai.CalculateRow(rows[0]);      
                            }                            
                        }
                    }
                    else
                    {
                        DataRow newRow = dt.NewRow();
                        string dMatHangID = form.DMATHANGID;
                        DMATHANGRow spRow = new DMATHANGRow(dMatHangID);

                        newRow["DMATHANGID"] = spRow.ID;                                        
                        newRow["DMATHANG_NAME"] = spRow.NAME;
                        newRow["DMATHANG_CODE"] = spRow.CODE;
                        newRow["DMATHANG_MASANCO"] = spRow.MASANCO;
                        newRow["DONGIA"] = spRow.GIABAN;
                        newRow["SLNHAPCHUAQUYDOI"] = 1;

                        newRow["DDONVITINHID"] = spRow.DDONVITINHID;
                        newRow["DDONVITINH_NAME"] = spRow.DDONVITINHID.Length == 0 ? "" : new DDONVITINHRow(spRow.DDONVITINHID).NAME;
                        newRow["SLNHAP"] = 1;

                        dt.Rows.Add(newRow);
                        grTraLai.CalculateRow(newRow);                    
                    }

                    mapper.RaiseOnCalculation();              
                }
            }
		}


		public void tabMuaTra_SelectedPageChanged(Object sender, EventArgs e)
		{
            UpdateXoaButton();
		}

        private void UpdateXoaButton()
        {
            bool enable = false;
            if (btnThanhToan.Enabled)
            {
                if (tabMuaTra.SelectedIndex == 0) enable = grMua.GridView.SelectedRows.Count > 0;
                else enable = grTraLai.GridView.SelectedRows.Count > 0;
            }
            btnXoa.Enabled = enable;
            btnTangSl1.Enabled = tabMuaTra.SelectedPage == pageMua && enable;
            btnGiamSl1.Enabled = tabMuaTra.SelectedPage == pageMua && enable;
        }


		public void grTraLai_grMain_SelectionChanged(Object sender, EventArgs e)
		{
            UpdateXoaButton();
		}


		public void numSL_KeyDown(Object sender, KeyEventArgs e)
		{
            if (e.KeyCode == Keys.Enter)
            {
                e.Handled = true;
                btnThem.PerformClick();
            }
		}

        Timer tmrLoadImage;
		public void UserControl1_KeyDownEx(Object sender, KeyEventArgs e)
		{
            if (e.KeyCode == Keys.F10) btnThongKe.PerformClick();
            else if (e.KeyCode == Keys.F12) btnThanhToan.PerformClick();
            else if (e.KeyCode == Keys.F3) txtMaVach.SelectAllEx();
            else if (e.KeyCode == Keys.F6) lueDKHACHHANGID.Select();
            else if (e.KeyCode == Keys.F7) btnLoaiGia.PerformClick();
            else if (e.KeyCode == Keys.F9) btnLuuTam.PerformClick();
            else if (e.KeyCode == Keys.F11) btnMoLai.PerformClick();
		}


		public void grTraLai_OnRowCalculate(Object sender, DataRow r, String fieldName)
		{
            decimal quyDoi = 1;
            if (suDung2DVT)
            {
                string DMATHANGID = r["DMATHANGID"].ToString();
                DMATHANGRow mhRow = new DMATHANGRow(DMATHANGID);                
                if (TonKhoHandler.Has2DonViTinh(mhRow) && r["DDONVITINHID"].ToString() != mhRow.DDONVITINHID)
                {
                    quyDoi = mhRow.QUYDOI;
                }
            }
            r["SLNHAP"] = quyDoi * ConvertTo.Decimal(r["SLNHAPCHUAQUYDOI"]);

            decimal thanhTien = ConvertTo.Decimal(r["SLNHAPCHUAQUYDOI"]) * ConvertTo.Decimal(r["DONGIA"]) * (1 - ConvertTo.Decimal(r["TILEGIAMGIA"]) / 100);
            r["THANHTIEN"] = -thanhTien;
            r["THANHTIENDUONG"] = thanhTien;
		}


		public void txtMaVach_KeyDown(Object sender, KeyEventArgs e)
		{
            if (e.KeyCode == Keys.Enter)
            {
                if (numSL.Visible && SystemConfig.BanHangDungDauDocMaVach == 0)
                {
                    numSL.Select();
                    numSL.Select(0, numSL.Text.Length);
                }
                else
                {
                    btnThem.PerformClick();
                }
            }
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
            }
		}

        private decimal GetTiLeGiamTheoKM()
        {
            decimal giamGia = -1;

            DataTable dtKM = GetKhuyenMaiDangSuDung(LOAIHINHKHUYENMAI.GIAMGIATONGBILL);
            foreach (DataRow r in dtKM.Rows)
            {
                DDOTKHUYENMAIRow kmRow = new DDOTKHUYENMAIRow(r);
                if (kmRow.TILEGIAMGIA > 0 && kmRow.TILEGIAMGIA >= giamGia)
                {
                    giamGia = kmRow.TILEGIAMGIA;
                }
            }

            dtKM = GetKhuyenMaiDangSuDung(LOAIHINHKHUYENMAI.GIAMTHEOGIATRIDONHANG);

            decimal tongSL = numTONGCONG.Value;
            foreach (DataRow r in dtKM.Rows)
            {
                decimal val = Config.Db.GetFirstFieldDec("SELECT TILEGIAMGIA FROM DDOTKHUYENMAICHITIET WHERE DDOTKHUYENMAIID = '" + r["ID"].ToString() + "' AND GIATRIDONHANG <= " + ((int)tongSL).ToString() + " ORDER BY GIATRIDONHANG DESC");
                if (val >= 0 && val >= giamGia)
                {
                    giamGia = val;
                }
            }

            dtKM = GetKhuyenMaiDangSuDung(LOAIHINHKHUYENMAI.GIAMTHEOSOLUONGMATHANG);
            tongSL = grMua.CalcSum("SLXUAT") - grTraLai.CalcSum("SLNHAP");
            foreach (DataRow r in dtKM.Rows)
            {
                decimal val = Config.Db.GetFirstFieldDec("SELECT TILEGIAMGIA FROM DDOTKHUYENMAICHITIET WHERE DDOTKHUYENMAIID = '" + r["ID"].ToString() + "' AND SOLUONGMATHANG <= " + ((int)tongSL).ToString() + " ORDER BY SOLUONGMATHANG DESC");
                if (val >= 0 && val >= giamGia)
                {
                    giamGia = val;
                }
            }

            return giamGia;
        }

		public Boolean grMatHang_OnAddedRowToGrid(DataRow selRow, DataRow row)
		{
            tabMuaTra.SelectedIndex = 0;
            return true;
		}

        private void CapNhatGiamGiaTuDong()
        {
            decimal tiLe = GetTiLeGiamTheoKM();
            //nếu có km thì tính
            if (tiLe >= 0)
            {
                decimal tileGiamTheoKhach = GetTiLeGiamTheoKhach();
                mapper["GIAMTHEOTIEN"].Value = 0;
                numTILEGIAMGIA.Value = Math.Max(tileGiamTheoKhach, tiLe);
            }
        }


		public void grMatHang_OnThemClicked(Object sender, CancelEventArgs e)
		{
            if (!btnThanhToan.Enabled || printing)
            {
                e.Cancel = true;
                return;
            }
            lastSlLe = numSL.Value;

            int theoKl = 0;
            if (numCan.Visible)
            {
                theoKl = ConvertTo.Int(grMatHang.SelectedRow["THEOCAN"]);
                lastSlLe = theoKl == 30 ? numCan.Value : numSL.Value;
            }

            lastSlChan = 0;
            lastCk = 0;

            int loaiGia = mapper[TDONHANGInfo.LOAIGIA].ToInt();
            bool chonLoaiGia = SystemConfig.HienThiDeLuaChonNeuCoNhieuGia == 30 && (SystemConfig.SuDungGia2 == 30 || SystemConfig.SuDungGia3 == 30 || SystemConfig.SuDungGia4 == 30);
            if (chonLoaiGia && SystemConfig.SuDungGiaTheoKhach == 30 && lueDKHACHHANGID.StringValue.Length > 0) chonLoaiGia = false;
            if (chonLoaiGia)
            {                
                ChonLoaiGia frmLoaiGia = (ChonLoaiGia)Config.CreateForm(Forms.ChonLoaiGia);
                frmLoaiGia.SetMatHang(new DMATHANGRow(grMatHang.SelectedRow));
                if (frmLoaiGia.form.ShowDialog() == DialogResult.OK)
                {
                    loaiGia = frmLoaiGia.LoaiGia;
                }
                else
                {
                    e.Cancel = true;
                    return;
                }
            }

            string field = loaiGia == 1 ? "GIABAN2" : (loaiGia == 2 ? "GIABAN3" : (loaiGia == 3 ? "GIABAN4" : "GIABAN"));

            lastDonGia = ConvertTo.Decimal(grMatHang.SelectedRow[field]);            
            lastDonGiaChan = ConvertTo.Decimal(grMatHang.SelectedRow["GIABANCHAN"]);            

            bool cancel = false;
            decimal quyDoi = 1;
            lastKichThuoc = "";
            lastHsd = null;
            GetSoLuong(ref cancel, ref lastSlLe, ref lastSlChan, ref lastCk, ref lastDonGia, ref lastDonGiaChan, ref quyDoi, theoKl, ref lastKichThuoc, ref lastHsd);
            e.Cancel = cancel;

            bool khongAmKho = false;
            if (lueDKHOXUATID.StringValue.Length > 0)
            {
                khongAmKho = new DKHOHANGRow(lueDKHOXUATID.StringValue).CHOPHEPAMKHO == 0;
            }

            if (!cancel && khongAmKho)
            {
                //kiem tra xem co cho am kho khong?
                decimal tonKho = TonKhoHandler.GetTonKho(grMatHang.SelectedID, lueDKHOXUATID.StringValue, mapper.ID);
                if (tonKho < lastSlLe + quyDoi * lastSlChan)
                {
                    string conTon = tonKho.ToString() + " " + grMatHang.SelectedRow["DDONVITINH_NAME"].ToString();
                    if (quyDoi != 1)
                    {
                        int tonChan = (int)(tonKho / quyDoi);
                        decimal tonLe = tonKho - tonChan * quyDoi;
                        conTon = tonLe.ToString() + " " + grMatHang.SelectedRow["DDONVITINH_NAME"].ToString() + " " + tonChan.ToString() + " " + grMatHang.SelectedRow["DDONVITINH2_NAME"].ToString();
                    }
                    Msg.ShowWarning("Không đủ hàng để xuất, trong kho chỉ còn: " + conTon);
                    e.Cancel = true;
                }                
            }

            if (!e.Cancel)
            {
                if (SystemConfig.SuDungGiaTheoKhach == 30 && loaiGia == 4 && lueDKHACHHANGID.StringValue.Length > 0)
                {
                    string sql = @"SELECT DONGIA FROM TDONHANG INNER JOIN TDONHANGCHITIET ON TDONHANG.ID = TDONHANGCHITIET.TDONHANGID WHERE
DATHANHTOAN = 30 AND DKHACHHANGID = '" + lueDKHACHHANGID.StringValue + "' AND DMATHANGID = '" + grMatHang.SelectedID + "' AND DDONVITINHID = '@DDONVITINHID' ORDER BY NGAY DESC, TDONHANG.TIMECREATED DESC";
                    decimal val = Config.Db.GetFirstFieldDec(sql.Replace("@DDONVITINHID", grMatHang.SelectedRow["DDONVITINHID"].ToString()));
                    if (val > 0) lastDonGia = val;
                    //don gia chan
                    string dvtChanID = grMatHang.SelectedRow["DDONVITINHCHANID"].ToString();
                    if (dvtChanID.Length > 0)
                    {
                        val = Config.Db.GetFirstFieldDec(sql.Replace("@DDONVITINHID", dvtChanID));
                        if (val > 0) lastDonGiaChan = val;
                    }
                }

                ThemSoLuongChan(lastSlChan, lastDonGiaChan, lastCk, quyDoi);
            }
		}

        private void ThemSoLuongChan(decimal lastSlChan, decimal lastDonGiaChan, decimal lastCk, decimal quyDoi)
        {
            //neu co so luong chan thi them luon
            if (lastSlChan > 0)
            {
                string DMATHANGID = grMatHang.SelectedID;
                string DDONVITINHCHANID = grMatHang.SelectedRow["DDONVITINHCHANID"].ToString();
                if (SystemConfig.NhapMotMatHangNhieuLanTrongPhieu != 30)
                {                   
                    //kiem tra xem da ton tai trong danh sach chua?
                    DataTable dt = grMua.DataSource as DataTable;
                    foreach (DataRow r in dt.Rows)
                    {
                        if (r.RowState == DataRowState.Deleted) continue;
                        TDONHANGCHITIETRow ctRow = new TDONHANGCHITIETRow(r);
                        if (ctRow.DMATHANGID == DMATHANGID && ctRow.DDONVITINHID == DDONVITINHCHANID)
                        {
                            //tang so luong len
                            ctRow.SLXUATCHUAQUYDOI += lastSlChan;
                            //quy ra so luong le
                            ctRow.SLXUAT = ctRow.SLXUATCHUAQUYDOI * quyDoi;
                            grMua.CalculateRow(r);
                            return;
                        }
                    }
                }

                DoAddItemToGrid(DMATHANGID, grMatHang.SelectedRow["CODE"].ToString(), grMatHang.SelectedRow["NAME"].ToString(), grMatHang.SelectedRow["MASANCO"].ToString(), grMatHang.SelectedRow["DDONVITINH2_NAME"].ToString(), DDONVITINHCHANID, lastSlChan, quyDoi, lastCk, lastDonGiaChan);
            }
        }

        private void DoAddItemToGrid(string DMATHANGID, string code, string name, string maSanCo, string dvt, string DDONVITINHID, decimal soLuong, decimal quyDoi, decimal ck, decimal donGia)
        {
            if (!btnThanhToan.Enabled) return;
            
            TDONHANGCHITIETRow newRow = new TDONHANGCHITIETRow(grMua.DataSource.NewRow());
            newRow.DMATHANGID = DMATHANGID;
            newRow.DDONVITINHID = DDONVITINHID;
            newRow.SLXUATCHUAQUYDOI = soLuong;
            newRow.SLXUAT = soLuong * quyDoi;
            newRow.DONGIA = donGia;
            //hien thi ten, don vi, ma san co...                
            newRow["DDONVITINH_NAME"] = dvt;
            newRow["DMATHANG_NAME"] = name;
            newRow["DMATHANG_CODE"] = code;
            newRow["DMATHANG_MASANCO"] = maSanCo;
            newRow.TILEGIAMGIA = ck;
            newRow.DKHOHANGID = lueDKHOXUATID.StringValue;

            grMua.DataSource.Rows.Add(newRow.Row);
            grMua.CalculateRow(newRow.Row);
        }

		public void grMatHang_OnCustomFilter(ref String filter)
		{
            filter = "";
            if (txtMaVach.Text.Length > 0)
            {
                string orgVal = DbUtils.LoaiBoDauTiengViet(txtMaVach.Text);

                string txt = SystemConfig.CoKyTuKiemTra == 30 ? orgVal.Substring(0, orgVal.Length - 1).Replace("'", "''") : orgVal.Replace("'", "''");
                if (SystemConfig.SuDungCanDienTu == 30 && txt.StartsWith(canBatDau) && orgVal.Length > 6)
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

            if (tvNhom.tvMain.Focused && tvNhom.SelectedID.Length > 0)
            {
                if (filter.Length > 0) filter += " AND ";
                filter += "DNHOMMATHANGID = '" + tvNhom.SelectedID + "'";
            }
		}

        #region IRefreshable Members

        public void DoRefresh()
        {
            //refresh mat hang neu co
            grMatHang.DoRefresh();
        }

        #endregion


		public void grMua_OnRowCalculate(Object sender, DataRow r, String fieldName)
		{
			//tính toán lại số lượng quy đổi
            decimal quyDoi = 1;
            if (suDung2DVT)
            {
                string DMATHANGID = r["DMATHANGID"].ToString();
                DMATHANGRow mhRow = new DMATHANGRow(DMATHANGID);                
                if (TonKhoHandler.Has2DonViTinh(mhRow) && r["DDONVITINHID"].ToString() != mhRow.DDONVITINHID)
                {
                    quyDoi = mhRow.QUYDOI;
                }                
            }
            r["SLXUAT"] = quyDoi * ConvertTo.Decimal(r["SLXUATCHUAQUYDOI"]);
            if (fieldName == "TILEGIAMGIA")
            {
                r["GIAMTHEOTIEN"] = 0;
            }
            else if (fieldName == "TIENGIAMGIA")
            {
                r["GIAMTHEOTIEN"] = 30;
            }

            if (ConvertTo.Int(r["GIAMTHEOTIEN"]) == 30)
            {
                decimal soLuong = ConvertTo.Decimal(r["SLXUATCHUAQUYDOI"]);
                decimal donGia = ConvertTo.Decimal(r["DONGIA"]);
                decimal tienGiam = ConvertTo.Decimal(r["TIENGIAMGIA"]);
                r["TILEGIAMGIA"] = donGia * soLuong == 0 ? 0 : Math.Round(tienGiam * 100 / (donGia * soLuong), 2);
                r["THANHTIEN"] = soLuong * donGia - tienGiam;
            }
            else
            {
                decimal thanhTien = ConvertTo.Decimal(r["SLXUATCHUAQUYDOI"]) * ConvertTo.Decimal(r["DONGIA"]);
                r["TIENGIAMGIA"] = thanhTien * ConvertTo.Decimal(r["TILEGIAMGIA"]) / 100;
                r["THANHTIEN"] = thanhTien * (1 - ConvertTo.Decimal(r["TILEGIAMGIA"]) / 100);
            }
		}


		public void btnMoDatHang_Click(Object sender, EventArgs e)
		{
            if (grMua.RowCount + grTraLai.RowCount == 0)
            {
                string filter = "NOT EXISTS (SELECT * FROM TDONHANG WHERE TDATHANGID = TDATHANG.ID)";
                STABLEDESCRow row = Config.GetTableDesc(Tables.TDATHANG);
                TimKiem form = new TimKiem(row, filter);
                if (form.ShowDialog() == DialogResult.OK)
                {
                    //them vao mat hang
                    TDATHANGRow dhRow = new TDATHANGRow(form.SelectedID);
                    mapper[TDONHANGInfo.TDATHANGID].Value = form.SelectedID;
                    if (dhRow.DKHACHHANGID.Length > 0)
                    {
                        lueDKHACHHANGID.EditValue = dhRow.DKHACHHANGID;
                    }                    
                    //lay danh sach mat hang va dua vao hoa don

                    string sql = "SELECT * FROM TDATHANGCHITIET WHERE TDATHANGID = '" + form.SelectedID + "'";
                    DataTable dt = Config.Db.GetTable(sql);                    
                    foreach (DataRow r in dt.Rows)
                    {
                        TDATHANGCHITIETRow ctRow = new TDATHANGCHITIETRow(r);
                        DMATHANGRow mhRow = new DMATHANGRow(ctRow.DMATHANGID);
                        decimal quyDoi = mhRow.DDONVITINHID == ctRow.DDONVITINHID ? 1 : mhRow.QUYDOI;
                        DoAddItemToGrid(ctRow.DMATHANGID, mhRow.CODE, mhRow.NAME, mhRow.MASANCO, ctRow.DDONVITINHID.Length == 0 ? "" : new DDONVITINHRow(ctRow.DDONVITINHID).NAME, ctRow.DDONVITINHID, ctRow.SOLUONG, quyDoi, ctRow.TILEGIAMGIA, ctRow.DONGIA);
                    }
                    numTILEGIAMGIA.LockEvent = true;
                    numTIENGIAMGIA.LockEvent = true;
                    numTILEGIAMGIA.Value = dhRow.TILEGIAMGIA;
                    numTIENGIAMGIA.Value = dhRow.TIENGIAMGIA;
                    mapper[TDONHANGInfo.GIAMTHEOTIEN].Value = dhRow.GIAMTHEOTIEN;
                    numTILEGIAMGIA.LockEvent = false;
                    numTIENGIAMGIA.LockEvent = false;
                    mapper.RaiseOnCalculation();
                }
            }
            else
            {
                Msg.ShowWarning("Mời bạn tạo mới hóa đơn trước");
            }
		}


		public void btnTangSl1_Click(Object sender, EventArgs e)
		{
            foreach (DataGridViewRow r in grMua.GridView.SelectedRows)
            {
                DataRow row = (r.DataBoundItem as DataRowView).Row;
                TDONHANGCHITIETRow ctRow = new TDONHANGCHITIETRow(row);
                ctRow.SLXUATCHUAQUYDOI = ctRow.SLXUATCHUAQUYDOI + 1;
                grMua.CalculateRow(ctRow.Row);
                Track("Tăng số lượng '" + ctRow["DMATHANG_NAME"].ToString() + "' lên 1 ");
            }

            mapper.RaiseOnCalculation();

            txtMaVach.Select();
            txtMaVach.SelectAll();
		}


		public void btnGiamSl1_Click(Object sender, EventArgs e)
		{
            foreach (DataGridViewRow r in grMua.GridView.SelectedRows)
            {
                DataRow row = (r.DataBoundItem as DataRowView).Row;
                TDONHANGCHITIETRow ctRow = new TDONHANGCHITIETRow(row);
                ctRow.SLXUATCHUAQUYDOI = Math.Max(1, ctRow.SLXUATCHUAQUYDOI - 1);
                grMua.CalculateRow(ctRow.Row);
                Track("Giảm số lượng '" + ctRow["DMATHANG_NAME"].ToString() + "' đi 1 ");
            }

            mapper.RaiseOnCalculation();

            txtMaVach.Select();
            txtMaVach.SelectAll();
		}

        #region ITestingSupport Members

        public void DoAutoTest()
        {
            Random rand = new Random();
            int numRow = rand.Next(3, 7);
            //them mat hang vao hoa don
            for (int i = 0; i < numRow; i++)
            {
                numSL.Value = rand.Next(1, 10);
                grMatHang.SelectRandomPos();
                btnThem.PerformClick();
            }
            //thanh toan
            btnThanhToan.PerformClick();

            //hien thi thong ke
            btnThongKe.PerformClick();

            //kiem thu he thong
            UiUtils.CloseActiveTab();
        }

        #endregion


		public void lueDKHOXUATID_CustomLoadData(object sender, CustomLoadDataArgs e)
		{
            if (!DbConfig.IsAdmin)
            {
                e.Where += " AND DCUAHANGID IN (SELECT DCUAHANGID FROM TNGUOIDUNGTHEOCUAHANG WHERE SUSERID = '" + DbConfig.UserID + "')";
            }
		}


		public void lueDKHOXUATID_OnEditValueChanged(object sender, object value)
		{
            if (lueDKHOXUATID.StringValue.Length > 0)
            {
                mapper[TDONHANGInfo.DCUAHANGID].Value = new DKHOHANGRow(lueDKHOXUATID.StringValue).DCUAHANGID;
            }
		}


		public void grMua_OnColumnInit(DataGridViewColumn col, SCOLUMNRow row)
		{
            if (SystemConfig.SuDungCanDienTu == 30)
            {
                if (row.NAME == "SLXUATCHUAQUYDOI")
                {
                    (col as NumericDataGridViewColumn).DecimalLength = 3;
                }
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

            //loại bỏ các sản phẩm là vật tư
            if (e.Where.Length > 0) e.Where += " AND ";
            e.Where += String.Format("COALESCE(LOAIDINHLUONG, 0) <> {0}", (int)LoaiDinhLuong.VatTuNguyenLieu);
		}
    }
}
