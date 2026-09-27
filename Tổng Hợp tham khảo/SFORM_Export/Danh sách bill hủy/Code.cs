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
    public partial class DanhSachBillHuy
    {
		public void grMain_OnFocusedRowChanged(Boolean CanRemove, Boolean canAdd, Boolean CanEdit)
		{
            grDetail.LoadData();
		}


		public void grDetail_CustomLoadData(Object sender, CustomLoadDataArgs e)
		{
            e.Where = "TDONHANGHUYID = '" + grMain.SelectedID + "'";
		}


		public void form_Load(Object sender, EventArgs e)
		{
			
		}
    }
}
