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
    public partial class TamUngLuong
    {
		public void grMain_CustomLoadData(Object sender, CustomLoadDataArgs e)
		{
            e.Where += " AND LATAMUNG = 30";
		}


		public void grMain_AfterCreatedEditForm(IAddEditForm form, String ID)
		{
            form.SetValue("LATAMUNG", 30);
		}


		public void grMain_CustomAeForm(ref Type Type, String ID, ref Int32 loai, ref String where)
		{
            loai = 1;
		}
    }
}
