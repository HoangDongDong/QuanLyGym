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
    public class TTHUCHI1MgHandler : ICustomManagementSupport
    {
        public void SetCustomManagement(TreeGridMg treeGrid)
        {
            treeGrid.grMain.CustomLoadData += new CustomLoadDataHandler(grMain_CustomLoadData);
        }

        void grMain_CustomLoadData(object sender, CustomLoadDataArgs e)
        {
            e.Where += " AND (LATAMUNG = 0 OR LATAMUNG IS NULL)";
            if (!DbConfig.IsAdmin)
            {
                e.Where += " AND EXISTS (SELECT * FROM TNGUOIDUNGTHEOCUAHANG WHERE SUSERID = '" + DbConfig.UserID + "' AND TNGUOIDUNGTHEOCUAHANG.DCUAHANGID = TTHUCHI.DCUAHANGID)";
            }
        }
    }
}
