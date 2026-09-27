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
    public partial class TTHUCHI5Ae
    {

		public void lueDLOAITHEID_OnEditValueChanged(object sender, object value)
		{
            if (lueDLOAITHEID.StringValue.Length > 0)
            {
                numGIATRIGOI.Value = new DLOAITHERow(lueDLOAITHEID.StringValue).GIABAN;
            }
            else
            {
                numGIATRIGOI.Value = 0;
            }
            numGIAMGIA_OnEditValueChanged(null, null);
		}


		public void mapper_AfterFillData(object sender, EventArgs e)
		{
            if (mapper.ID.Length == 0)
            {
                //lấy lý do gần nhất
                string DLYDOID = Config.Db.GetFirstFieldString("SELECT FIRST 1 DLYDOTHUCHIID FROM TTHUCHI WHERE LOAI = 5 ORDER BY TIMECREATED DESC");
                if (DLYDOID.Length > 0)
                {
                    lueDLYDOTHUCHIID.EditValue = DLYDOID;
                }

                mapper[TTHUCHIInfo.LOAI].Value = 5;
                mapper[TTHUCHIInfo.KHONGTHAYDOICONGNO].Value = 30;
                
                mapper[TTHUCHIInfo.DCUAHANGID].Value = Shared.DCUAHANGID;
            }

            int GiamTheoTien = mapper[TTHUCHIInfo.GIAMTHEOTIEN].ToInt();
            if (GiamTheoTien == 0)
                numGIAMGIA_OnEditValueChanged(null, null);
            else
                numTIENGIAM_OnEditValueChanged(null, null);

            LoadCustomer();
		}

        public void mapper_BeforeSave(object sender, SaveCancelEventArgs e)
        {
            if (numTHU.Value > numTONGCONG.Value)
            {
                Msg.ShowWarning("Số tiền đặt trước lớn hơn giá trị gói");
                return;
            }
            
            if (chkCHUYENKHOAN.Checked && lueDTAIKHOANNGANHANGID.StringValue.Length == 0)
            {
                Msg.ShowWarning("Mời bạn chọn tài khoản ngân hàng");
                e.Cancel = true;
                lueDTAIKHOANNGANHANGID.showDropDown();
            }
        }


		public void lueDLYDOTHUCHIID_CustomLoadData(object sender, CustomLoadDataArgs e)
		{
            e.Where += " AND LALYDOTHU = 30";	
		}


		public void chkCHUYENKHOAN_CheckedChanged(object sender, EventArgs e)
		{
			lueDTAIKHOANNGANHANGID.Enabled = chkCHUYENKHOAN.Checked;
		}


		public void chkCHUYENKHOAN_OnEditValueChanged(object sender, object value)
		{
			if (chkCHUYENKHOAN.Checked) lueDTAIKHOANNGANHANGID.showDropDown();
		}


		public void numGIAMGIA_OnEditValueChanged(object sender, object value)
		{
            mapper[TTHUCHIInfo.GIAMTHEOTIEN].Value = 0;
            UpdateTong();
		}


		public void numTIENGIAM_OnEditValueChanged(object sender, object value)
		{
            mapper[TTHUCHIInfo.GIAMTHEOTIEN].Value = 30;
            UpdateTong();
		}

        private void UpdateTong()
        {
            int value = mapper[TTHUCHIInfo.GIAMTHEOTIEN].ToInt();
            if (value == 30)
            {
                numTONGCONG.Value = numGIATRIGOI.Value - numTIENGIAM.Value;
                numGIAMGIA.LockEvent = true;
                numGIAMGIA.Value = Math.Round((numTIENGIAM.Value * 100) / numGIATRIGOI.Value, 2);
                numGIAMGIA.LockEvent = false;
            }
            else
            {
                numTONGCONG.Value = numGIATRIGOI.Value * (1 - numGIAMGIA.Value / 100);
                numTIENGIAM.LockEvent = true;
                numTIENGIAM.Value = numGIATRIGOI.Value - numTONGCONG.Value;
                numTIENGIAM.LockEvent = false;
            }
        }


		public void btnDatThem_Click(object sender, EventArgs e)
		{
			
		}


		public void btnKhachCu_Click(object sender, EventArgs e)
		{
            Search form = new Search(Tables.DKHACHHANG, Tables.DNHOMKHACHHANG, "DNHOMKHACHHANGID");
            if (form.ShowDialog() == DialogResult.OK)
            {
                DKHACHHANGRow khRow = new DKHACHHANGRow(form.SelectedID);
                txtTENDOITUONG.Text = khRow.NAME;
                txtDIACHI.Text = khRow.DIACHI;
                txtDIENTHOAI.Text = khRow.DIENTHOAI;
                mapper[TTHUCHIInfo.DKHACHHANGID].Value = form.SelectedID;
                LoadCustomer();
            }
		}

        private void LoadCustomer()
        {
            string ID = mapper[TTHUCHIInfo.DKHACHHANGID].ToStringValue();
            bool hasCustomer = ID.Length > 0;
            btnXoa.Visible = hasCustomer;
            txtNAME.Enabled = !hasCustomer;
            txtDIACHI.Enabled = !hasCustomer;
            txtDIENTHOAI.Enabled = !hasCustomer;
        }


		public void btnXoa_Click(object sender, EventArgs e)
		{
            txtTENDOITUONG.Text = "";
            txtDIACHI.Text = "";
            txtDIENTHOAI.Text = "";
            mapper[TTHUCHIInfo.DKHACHHANGID].Value = "";
            LoadCustomer();
		}
    }
}
