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
    public partial class DKHACHHANGAe
    {
		public void grLichSu_CustomLoadData(object sender, CustomLoadDataArgs e)
		{
			//chỉ lọc thông tin của khách hàng hiện tại
            if (e.Where.Length > 0) e.Where += " AND ";
            e.Where += "DKHACHHANGID = '" + mapper.ID + "'";
		}

		public void grTheTrang_OnRowCalculate(object sender, DataRow r, string fieldName)
		{
			//khi thêm mới thì chọn ngày hiện tại
            if (r["NGAY"] == DBNull.Value)
            {
                r["NGAY"] = Config.Db.DbDate;
            }
		}

		public void mapper_OnLoad(object sender, EventArgs e)
		{
            if (SystemConfig.ThietBiSuDung == (int)ThietBiSuDung.MayDocVanTay || SystemConfig.SuDungDauUsbDeLayVanTay == 30)
            {
                btnLayVanTay.Visible = true;
            }
			//chọn tab đầu tiên
            tabMain.SelectedIndex = 0;
            //Shared.QuanLyThietBi.OnError += QuanLyThietBi_OnError;
            //Shared.QuanLyThietBi.OnSuccess += QuanLyThietBi_OnSuccess;
            tabMain.FindForm().FormClosed += new FormClosedEventHandler(DKHACHHANGAe_FormClosed);
		}

        void QuanLyThietBi_OnError(IMayVanTay IMay, string MayID, string TenMay, string MaThe)
        {
            txtMAKHACH.Text = MaThe.ToString();
        }

        void QuanLyThietBi_OnSuccess(IMayVanTay IMay, string MayID, string TenMay, string MaThe)
        {
            //kiem tra xem may co phai o le tan khong?
            txtMAKHACH.Text = MaThe;
        }

        void DKHACHHANGAe_FormClosed(object sender, FormClosedEventArgs e)
        {
            //Shared.QuanLyThietBi.OnSuccess -= QuanLyThietBi_OnSuccess;
            //Shared.QuanLyThietBi.OnError -= QuanLyThietBi_OnError;
        }

        private int backupMaThe = 0;
		public void mapper_AfterFillData(object sender, EventArgs e)
		{
            backupMaThe = ConvertTo.Int(txtMAKHACH.Text);
            grLichSu.LoadData();
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

            if (mapper.ID.Length == 0)
            {
                lueDTRANGTHAIID.EditValue = TrangThaiIds.ChuaKichHoat;
            }
            else
            {
                LoadThongTinThe();
            }

            pageHienTai.Visible = mapper.ID.Length > 0 && SystemConfig.ThietBiSuDung == (int)ThietBiSuDung.CuaTu;

            //trường hợp chỉ cập nhật ảnh khi chỉnh sửa            
            if (mapper.ID.Length > 0 && DbUtils.CanView(Functions.ChiCapNhatAnhKhachHang) && !DbConfig.IsAdmin)
            {
                pageHienTai.Enabled = false;
                pageTheTrang.Enabled = false;
                pageLichSuGiaoDich.Enabled = false;
                foreach (Control c in pageThongTinChinh.Controls)
                {
                    c.Enabled = c == btnWebcam || c == ANH;
                }
            }
		}

        private void LoadThongTinThe()
        {
            DKHACHHANGRow khRow = new DKHACHHANGRow(mapper.ID);
            txtLoaiThe.Text = khRow.DLOAITHEID.Length == 0 ? "" : new DLOAITHERow(khRow.DLOAITHEID).NAME;
            if (!khRow.IsNullValue(DKHACHHANGInfo.TUNGAY))
            {
                txtTuNgay.Text = khRow.TUNGAY.ToString("dd/MM/yyyy");
            }
            else
            {
                txtTuNgay.Text = "";
            }

            if (!khRow.IsNullValue(DKHACHHANGInfo.DENNGAY))
            {
                txtDenNgay.Text = khRow.DENNGAY.ToString("dd/MM/yyyy");
                DateTime toDay = Config.Db.DbDate;
                if (khRow.DENNGAY < toDay)
                {
                    txtSoNgayCon.Text = "Quá hạn";
                }
                else
                {
                    TimeSpan ts = khRow.DENNGAY - toDay;
                    txtSoNgayCon.Text = ((int)ts.TotalDays).ToString() + " ngày";
                }
            }
            else
            {
                txtDenNgay.Text = "";
                txtSoNgayCon.Text = "";
            }

            txtSoLan.Text = khRow.SOLAN.ToString();
            txtDaTap.Text = khRow.DATAP.ToString();
            txtConLai.Text = khRow.CONLAI.ToString();
        }

		public void btnLayTrangThai_Click(object sender, EventArgs e)
		{
            int MaThe = GetMaThe();
            if (MaThe <= 0) return;

            Dictionary<string, string> dic = new Dictionary<string, string>();
            foreach (ThietBiInfo info in Shared.QuanLyThietBi.lstThietBi)
            {
                string State = info.IMay.GetCardInfo(MaThe);
                dic.Add(info.MayID, State);
            }

            foreach (DataRow r in (grid.DataSource as DataTable).Rows)
            {
                if (dic.ContainsKey(r["ID"].ToString()))
                    r["TRANGTHAITHE"] = dic[r["ID"].ToString()];
            }
		}

        public void btnTaoTaiKhoan_Click(object sender, EventArgs e)
        {
            int MaThe = GetMaThe();
            if (MaThe <= 0) return;

            string msgOk = "";
            string msgError = "";

            int nhom = SystemConfig.NhomKhongBiKhoa;
            DKHACHHANGRow khRow = new DKHACHHANGRow(mapper.ID);
            if (khRow.DCATAPID.Length > 0)
            {
                nhom = new DCATAPRow(khRow.DCATAPID).NHOMTRENMAY;
            }

            Shared.QuanLyThietBi.TaoThe(MaThe, khRow.MAVANTAY, nhom, ref msgOk, ref msgError, khRow.NAME);

            if (msgOk.Length > 0)
                Msg.ShowInfo(msgOk);
            if (msgError.Length > 0)
                Msg.ShowError(msgError);
        }

        private int GetMaThe()
        {
            int MaThe = ConvertTo.Int(txtMAKHACH.Text);
            if (MaThe == 0)
            {
                Msg.ShowWarning("Khách hàng chưa được cấp thẻ hoặc mã thẻ không đúng");                
            }
            return MaThe;
        }

		public void btnKhoaThe_Click(object sender, EventArgs e)
		{
            int MaThe = GetMaThe();
            if (MaThe == 0) return;

            string msgOk = "";
            string msgError = "";
            Shared.QuanLyThietBi.KhoaThe(MaThe, ref msgOk, ref msgError);

            btnLayTrangThai.PerformClick();

            if (msgOk.Length > 0)
                Msg.ShowInfo(msgOk);
            if (msgError.Length > 0)
                Msg.ShowError(msgError);
		}


		public void btnMoThe_Click(object sender, EventArgs e)
		{
            int MaThe = GetMaThe();
            if (MaThe == 0) return;

            string msgOk = "";
            string msgError = "";
            Shared.QuanLyThietBi.MoThe(MaThe, ref msgOk, ref msgError);
            btnLayTrangThai.PerformClick();

            if (msgOk.Length > 0)
                Msg.ShowInfo(msgOk);
            if (msgError.Length > 0)
                Msg.ShowError(msgError);
		}


		public void UserControl1_Load(object sender, EventArgs e)
		{
            DataTable dt = new DataTable();
            dt.Columns.Add("ID", typeof(string));
            dt.Columns.Add("TENMAY", typeof(string));
            dt.Columns.Add("IP", typeof(string));
            dt.Columns.Add("PORT", typeof(string));
            dt.Columns.Add("TRANGTHAIKETNOI", typeof(string));
            dt.Columns.Add("TRANGTHAITHE", typeof(string));
            dt.Columns.Add("STATUS", typeof(int));
            dt.Columns.Add("NHOMTHE", typeof(string));

            foreach (ThietBiInfo info in Shared.QuanLyThietBi.lstThietBi)
            {
                DataRow r = dt.NewRow();
                r["ID"] = info.MayID;
                r["TENMAY"] = info.TenMay;
                r["IP"] = info.IP;
                r["PORT"] = info.Port.ToString();
                if (info.IMay.IsConnected)
                {
                    r["STATUS"] = 1;
                    r["TRANGTHAIKETNOI"] = "Đã kết nối";
                }
                else
                {
                    r["STATUS"] = 0;
                    r["TRANGTHAIKETNOI"] = "Chưa kết nối";
                }                
                dt.Rows.Add(r);
            }

            grid.DataSource = dt;
		}


		public void grid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
		{
            if(e.RowIndex <0 || e.ColumnIndex < 0) return;
            DataRow Row = ((grid.Rows[e.RowIndex] as DataGridViewRow).DataBoundItem as DataRowView).Row;
            if (Row != null && Row["STATUS"].ToString() == "0")
            {
                e.CellStyle.BackColor = Color.Red;               
            }
		}


        public void mapper_AfterSave(object sender, EventArgs e)
        {
            string msgOk = "";
            string msgError = "";
            int maThe = ConvertTo.Int(txtMAKHACH.Text);
            if (lueDTRANGTHAIID.StringValue != TrangThaiIds.ChuaKichHoat && backupMaThe != maThe && SystemConfig.TuDongTaoXoaThe == 30)
            {
                if (backupMaThe > 0)
                {
                    Shared.QuanLyThietBi.XoaThe(backupMaThe, ref msgOk);
                }

                if (maThe > 0)
                {
                    TGIAHANTHE0Ae.TaoTheKhachHang(new DKHACHHANGRow(mapper.ID));
                }
            }

            if (bAdd && SystemConfig.ThietBiSuDung ==  (int)ThietBiSuDung.MayDocVanTay)
            {
                //refresh
                if (KiemSoatVaoRa.kiemSoatVaoRa != null)
                {
                    try
                    {
                        KiemSoatVaoRa.kiemSoatVaoRa.LoadTemplate();
                    }
                    catch
                    {
                    }
                }
            }

            if (bAdd && SystemConfig.GiaHanTheKhiThemKhachHang == 30)
            {
                IManagement man = mapper.Controller.GetManagement();
                if (man is No1DataGrid)
                {
                    DynamicAeForm form = (DynamicAeForm)Config.CreateAeForm(Tables.TGIAHANTHE, 0, "");
                    form.ReLoad("");
                    form.SetValue("DKHACHHANGID", mapper.ID);
                    form.ShowDialog();
                    if (form.IsDataSaved())
                        LoadThongTinThe();
                }
            }

            if (!bAdd && thayDoiVanTay && SystemConfig.ThietBiSuDung == (int)ThietBiSuDung.CuaTu)
            {
                long maKhach = ConvertTo.Long(txtMAKHACH.Text);

                if (maKhach > 0)
                {
                    string maVanTay = mapper[DKHACHHANGInfo.MAVANTAY].ToStringValue();
                    //cập nhật mã vân tay lên máy
                    Shared.QuanLyThietBi.CapNhatVanTay(maKhach.ToString(), maVanTay);
                }                
            }
        }

        bool bAdd = false;
		public void mapper_BeforeSave(object sender, SaveCancelEventArgs e)
		{
            bAdd = mapper.ID.Length == 0;
		}

        bool thayDoiVanTay = false;
		public void btnLayVanTay_Click(object sender, EventArgs e)
		{
            DangKyVanTay form = (DangKyVanTay)Config.CreateForm(Forms.DangKyVanTay);
            if (form.No1Form1.ShowDialog() == DialogResult.OK)
            {
                mapper[DKHACHHANGInfo.MAVANTAY].Value = form.TemplateStr;
                thayDoiVanTay = true;
            }                       
		}


		public void btnWebcam_Click(object sender, EventArgs e)
		{
            ChupWebcam form = (ChupWebcam)Config.CreateForm(Forms.ChupWebcam);
            form.Load();
            if (form.No1Form1.ShowDialog() == DialogResult.OK)
            {
                ANH.Image = form.Image;
                mapper.RaiseChangedEvent(ANH);
            }
		}
    }
}
