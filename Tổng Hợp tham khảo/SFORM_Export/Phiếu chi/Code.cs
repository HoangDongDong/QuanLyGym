using System;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using No1Lib.Sys;
using No1Lib.Db;
using No1Lib.Utils;
using FirebirdSql.Data.FirebirdClient;

namespace No1Run
{
    public partial class TTHUCHI1Ae
    {
        public void lueDLYDOTHUCHIID_CustomLoadData(Object sender, CustomLoadDataArgs e)
        {
            e.Where += " AND LALYDOTHU = 0";		
        }
        public void cboLOAIDOITUONG_SelectedIndexChanged(Object sender, EventArgs e)
        {
            lueDKHACHHANGID.Enabled = cboLOAIDOITUONG.SelectedIndex == 2;
            lueDNHANVIENID.Enabled = cboLOAIDOITUONG.SelectedIndex == 1;
            txtTENDOITUONG.Enabled = cboLOAIDOITUONG.SelectedIndex == 0;
            lueDNHACUNGCAPID.Enabled = cboLOAIDOITUONG.SelectedIndex == 3;
            txtDIACHI.Enabled = cboLOAIDOITUONG.SelectedIndex == 0;

            if (cboLOAIDOITUONG.Focused)
            {
                if (lueDKHACHHANGID.Enabled) lueDKHACHHANGID.showDropDown("");
                else if (lueDNHANVIENID.Enabled) lueDNHANVIENID.showDropDown();
                else if (lueDNHACUNGCAPID.Enabled) lueDNHACUNGCAPID.showDropDown("");
            }
        }
		public void mapper_AfterFillData(Object sender, EventArgs e)
		{
            if (mapper.ID.Length == 0)
            {
                cboLOAIDOITUONG.SelectedIndex = 0;
                mapper[TTHUCHIInfo.LOAI].Value = 1;
                lueDCUAHANGID.EditValue = Shared.DCUAHANGID;
            }

            int val = mapper[TTHUCHIInfo.LATAMUNG].ToInt();
            if (val == 30)
            {
                cboLOAIDOITUONG.SelectedIndex = 1;
                cboLOAIDOITUONG.Enabled = false;
                chkCHUYENKHOAN.Visible = false;
            }
		}
		public void lueDNHANVIENID_OnEditValueChanged(Object sender, Object value)
		{
            if (lueDNHANVIENID.StringValue.Length > 0)               
            {
                DNHANVIENRow row = new DNHANVIENRow(lueDNHANVIENID.StringValue);
                txtTENDOITUONG.Text = row.NAME;
                txtDIACHI.Text = "";
            }
            else
            {
                txtTENDOITUONG.Text = "";
                txtDIACHI.Text = "";
            }
		}
		public void lueDKHACHHANGID_OnEditValueChanged(Object sender, Object value)
		{
            if (lueDKHACHHANGID.StringValue.Length > 0)               
            {
                DKHACHHANGRow row = new DKHACHHANGRow(lueDKHACHHANGID.StringValue);
                txtTENDOITUONG.Text = row.NAME;
                txtDIACHI.Text = row.DIACHI;
            }
            else
            {
                txtTENDOITUONG.Text = "";
                txtDIACHI.Text = "";
            }
		}

		public void lueDLYDOTHUCHIID_AfterCreateEditForm(Object EditForm, String ID)
		{
            ((IAddEditForm)EditForm).SetValue("LALYDOTHU", 0);		
		}

		public void lueDLYDOTHUCHIID_OnCustomManagementForm(ref bool Handled)
		{                                                      
            TreeDataMg form = new TreeDataMg();            
            form.tvMain.CustomLoadData += new CustomLoadDataHandler(tvMain_CustomLoadData);
            form.tvMain.AfterCreateEditForm += lueDLYDOTHUCHIID_AfterCreateEditForm;
            form.LoadData(null, "DLYDOTHUCHI");
            form.ShowDialog();
            Handled = true;
		}

        void tvMain_CustomLoadData(object sender, CustomLoadDataArgs e)
        {
            e.Where += " AND LALYDOTHU = 0";
        }


		public void lueDNHACUNGCAPID_OnEditValueChanged(Object sender, Object value)
		{
            if (lueDNHACUNGCAPID.StringValue.Length > 0)
            {
                DNHACUNGCAPRow row = new DNHACUNGCAPRow(lueDNHACUNGCAPID.StringValue);
                txtTENDOITUONG.Text = row.NAME;
                txtDIACHI.Text = row.DIACHI;
            }
            else
            {
                txtTENDOITUONG.Text = "";
                txtDIACHI.Text = "";
            }
		}


		public void mapper_OnSetValue(String fieldName, Object fieldID)
		{
            if (fieldName == TTHUCHIInfo.DNHACUNGCAPID.ToString())
            {
                cboLOAIDOITUONG.SelectedIndex = 3;
            }
            else if (fieldName == "TONGCONGNO")
            {
                numCHI.EditValue = fieldID;
            }
            else if (fieldName == "LATAMUNG")
            {
                cboLOAIDOITUONG.SelectedIndex = 1;
                cboLOAIDOITUONG.Enabled = false;
                chkCHUYENKHOAN.Visible = false;
            }
		}

        int thang = -1;
        int nam = -1;
        public void SetGioiHanNgay(int thang, int nam)
        {
            this.thang = thang;
            this.nam = nam;
        }


		public void mapper_BeforeSave(Object sender, SaveCancelEventArgs e)
		{
            if (thang != -1)
            {
                if (dtNGAY.DateTime.Month != thang || dtNGAY.DateTime.Year != nam)
                {
                    Msg.ShowWarning("Ngày phải nằm trong tháng " + thang.ToString() + " năm " + nam.ToString());
                    e.Cancel = true;
                }
            }

            if (chkCHUYENKHOAN.Checked && lueDTAIKHOANNGANHANGID.StringValue.Length == 0)
            {
                Msg.ShowWarning("Mời bạn chọn tài khoản ngân hàng");
                e.Cancel = true;
                lueDTAIKHOANNGANHANGID.showDropDown();
            }
		}

        internal void SetLuong(string DNHANVIENID, decimal SoTien, string TBANGLUONGID)
        {
            TBANGLUONGRow row = new TBANGLUONGRow(TBANGLUONGID);
            txtDIENGIAI.Text = "Chi tiền lương tháng " + row.THANG.ToString() + " năm " + row.NAM.ToString();

            numCHI.Value = SoTien;
            cboLOAIDOITUONG.SelectedIndex = 1;
            cboLOAIDOITUONG.Enabled = false;            
            txtTENDOITUONG.Text = new DNHANVIENRow(DNHANVIENID).NAME;
            lueDNHANVIENID.EditValue = DNHANVIENID;            
            mapper[TTHUCHIInfo.TBANGLUONGID].Value = TBANGLUONGID;

            object val = Config.Db.GetFirstField("SELECT FIRST 1 DLYDOTHUCHIID FROM TTHUCHI WHERE TBANGLUONGID IS NOT NULL ORDER BY TIMECREATED DESC");
            if (val != null && val.ToString().Length > 0) lueDLYDOTHUCHIID.EditValue = val.ToString();
        }


		public void lueDCUAHANGID_CustomLoadData(object sender, CustomLoadDataArgs e)
		{
			if (!DbConfig.IsAdmin)
            {
                e.Where += " AND ID IN (SELECT DCUAHANGID FROM TNGUOIDUNGTHEOCUAHANG WHERE SUSERID = '" + DbConfig.UserID + "')";
            }
		}


		public void chkCHUYENKHOAN_OnEditValueChanged(object sender, object value)
		{
            if (chkCHUYENKHOAN.Checked)
            {
                lueDTAIKHOANNGANHANGID.showDropDown();
            }            
		}


		public void chkCHUYENKHOAN_CheckedChanged(object sender, EventArgs e)
		{
            lueDTAIKHOANNGANHANGID.Enabled = chkCHUYENKHOAN.Checked;
		}
    }
}
