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
    public partial class TTHUCHI0Ae
    {
        public void lueDLYDOTHUCHIID_CustomLoadData(Object sender, CustomLoadDataArgs e)
        {
            e.Where += " AND LALYDOTHU = 30";		
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
                mapper[TTHUCHIInfo.LOAI].Value = 0;
                lueDCUAHANGID.EditValue = Shared.DCUAHANGID;
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
		public void mapper_OnSetValue(String fieldName, Object fieldID)
		{
            if (fieldName == "TONGCONGNO")
            {
                numTHU.EditValue = fieldID;
            }
            else if (fieldName == "DKHACHHANGID")
            {
                cboLOAIDOITUONG.EditValue = 2;
            }
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


		public void lueDCUAHANGID_CustomLoadData(object sender, CustomLoadDataArgs e)
		{
            if (!DbConfig.IsAdmin)
            {
                e.Where += " AND ID IN (SELECT DCUAHANGID FROM TNGUOIDUNGTHEOCUAHANG WHERE SUSERID = '" + DbConfig.UserID + "')";
            }
		}


		public void chkCHUYENKHOAN_OnEditValueChanged(object sender, object value)
		{
            if (chkCHUYENKHOAN.Checked) lueDTAIKHOANNGANHANGID.showDropDown();
		}


		public void chkCHUYENKHOAN_CheckedChanged(object sender, EventArgs e)
		{
            lueDTAIKHOANNGANHANGID.Enabled = chkCHUYENKHOAN.Checked;
		}


		public void mapper_BeforeSave(object sender, SaveCancelEventArgs e)
		{
            if (chkCHUYENKHOAN.Checked && lueDTAIKHOANNGANHANGID.StringValue.Length == 0)
            {
                Msg.ShowWarning("Mời bạn chọn tài khoản ngân hàng");
                e.Cancel = true;
                lueDTAIKHOANNGANHANGID.showDropDown();
            }
		}

        internal void SetNapThe()
        {
            if (mapper.ID.Length == 0)
            {
                txtTENDOITUONG.Text = "Khách nạp thẻ";
                lblLOAIDOITUONG.Visible = false;
                cboLOAIDOITUONG.Visible = false;
                chkKHONGTHAYDOICONGNO.Checked = true;
            }
        }
    }
}
