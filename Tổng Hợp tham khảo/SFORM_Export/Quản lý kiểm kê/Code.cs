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
    public class TDONHANG4MgHandler : ICustomManagementSupport
    {
        public void SetCustomManagement(TreeGridMg treeGrid)
        {
            if (!DbConfig.IsAdmin)
                treeGrid.grMain.CustomLoadData += new CustomLoadDataHandler(grMain_CustomLoadData);
        }

        void grMain_CustomLoadData(object sender, CustomLoadDataArgs e)
        {
            e.Where += " AND EXISTS (SELECT * FROM DKHOHANG INNER JOIN TNGUOIDUNGTHEOCUAHANG ON DKHOHANG.DCUAHANGID = TNGUOIDUNGTHEOCUAHANG.DCUAHANGID AND SUSERID = '" + DbConfig.UserID + "' AND DKHONHAPID = DKHOHANG.ID)";
        }
    }
}
