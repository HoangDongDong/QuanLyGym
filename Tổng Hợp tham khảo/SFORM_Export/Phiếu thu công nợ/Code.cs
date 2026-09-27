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
    public partial class TTHUCHI2Ae
    {


		public void lueDNHANVIENID_OnEditValueChanged(object sender, object value)
		{
			mapper[TTHUCHIInfo.TENDOITUONG].Value = lueDNHANVIENID.DisplayText;
		}


		public void lueDLYDOTHUCHIID_CustomLoadData(object sender, CustomLoadDataArgs e)
		{
			e.Where += " AND LALYDOTHU = 30";	
		}


		public void mapper_OnSetValue(string fieldName, object fieldID)
		{
            if (fieldName == "DETAILDATA")
            {
                decimal soTien = 0;
                DataTable dt = fieldID as DataTable;
                
                DataTable dtNew = grMain.DataSource;
                foreach(DataRow r in dt.Rows)
                {
                    DataRow newRow = dtNew.NewRow();
                    newRow["SOTIEN"] = r["LANNAY"];                    
                    newRow["TDONHANG_NAME"] = r["NAME"];
                    newRow["TDONHANG_NGAY"] = r["NGAY"];
                    newRow["TDONHANG_TONGCONG"] = r["TONGCONG"];
                    newRow["TDONHANGID"] = r["ID"];
                    dtNew.Rows.Add(newRow);
                    soTien += ConvertTo.Decimal(r["LANNAY"]);
                }
                
                numTHU.Value = soTien;
                txtDIENGIAI.Text = "Thu công nợ";
                mapper[TTHUCHIInfo.LOAIDOITUONG].Value = 1;
                mapper[TTHUCHIInfo.LOAI].Value = 2;
                mapper[TTHUCHIInfo.LAPHIEUTHUCONGNO].Value = 30;
                mapper[TTHUCHIInfo.DCUAHANGID].Value = Shared.DCUAHANGID;
            }
		}
    }
}
