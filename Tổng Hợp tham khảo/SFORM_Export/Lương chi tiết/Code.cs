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
    public partial class LuongChiTiet
    {
        string DNHANVIENID;
        LuongChiTietMode Mode;
        int thang;
        int nam;

        public void SetData(string DNHANVIENID, LuongChiTietMode Mode, string DBANGLUONGID)
        {
            TBANGLUONGRow row = new TBANGLUONGRow(DBANGLUONGID);
            thang = row.THANG;
            nam = row.NAM;
            this.DNHANVIENID = DNHANVIENID;            

            lblThang.Text = row.NAME;
            this.Mode = Mode;
            DNHANVIENRow nvRow = new DNHANVIENRow(DNHANVIENID);
            lblNhanVien.Text = "Nhân viên: " + nvRow.NAME;
            switch (Mode)
            {
                case LuongChiTietMode.Phat:
                    pageTamUng.Visible = false;
                    pageThuongPhat.Text = "Danh sách phạt";
                    this.form.Text = "CHI TIẾT PHẠT";
                    grThuongPhat.LoadData("");
                    break;
                case LuongChiTietMode.TamUng:
                    this.form.Text = "CHI TIẾT TẠM ỨNG";
                    pageThuongPhat.Visible = false;
                    grTamUng.CustomAeForm +=new CustomAeFormHandler(grTamUng_CustomAeForm);
                    grTamUng.LoadData("");
                    break;
                case LuongChiTietMode.Thuong:
                    this.form.Text = "CHI TIẾT THƯỞNG";
                    pageTamUng.Visible = false;
                    pageThuongPhat.Text = "Danh sách thưởng";
                    grThuongPhat.LoadData("");
                    break;
            }
        }

		public void grTamUng_CustomLoadData(Object sender, CustomLoadDataArgs e)
		{
            e.Where += " AND DNHANVIENID = '" + DNHANVIENID + "' AND EXTRACT(MONTH FROM NGAY) = " + thang.ToString() + " AND EXTRACT(YEAR FROM NGAY) = " + nam + " AND LATAMUNG = 30";
		}


		public void grTamUng_OnEditDataChanged(Object sender, EventArgs e)
		{
            Changed = true;
		}


		public void grTamUng_CustomAeForm(ref Type Type, String ID, ref Int32 loai, ref String where)
		{
			//tao form moi
            loai = 1;
		}


		public void grTamUng_AfterCreatedEditForm(IAddEditForm form, String ID)
		{
            form.SetValue("DNHANVIENID", DNHANVIENID);
            form.SetValue("LATAMUNG", 30);
            object codeRunner = ((DynamicAeForm)form).CodeRunner;
            if (codeRunner is TTHUCHI1Ae)
            {
                (codeRunner as TTHUCHI1Ae).SetGioiHanNgay(thang, nam);
            }            
		}


		public void grThuongPhat_AfterCreatedEditForm(IAddEditForm form, String ID)
		{
            form.SetValue("DNHANVIENID", DNHANVIENID);
            ((TTHUONGPHATAe)((DynamicAeForm) form).CodeRunner).SetGioiHanNgay(thang, nam);
		}


		public void grThuongPhat_CustomLoadData(Object sender, CustomLoadDataArgs e)
		{
            e.Where += " AND DNHANVIENID = '" + DNHANVIENID + "' AND EXTRACT(MONTH FROM NGAY) = " + thang.ToString() + " AND EXTRACT(YEAR FROM NGAY) = " + nam + " AND " + (Mode == LuongChiTietMode.Thuong ? "THUONG > 0" : "PHAT > 0");
		}

        internal bool Changed = false;
		public void grThuongPhat_OnEditDataChanged(Object sender, EventArgs e)
		{
            Changed = true;
		}
    }

    public enum LuongChiTietMode
    {
        TamUng = 0,
        Thuong = 1,
        Phat = 2
    }
}
