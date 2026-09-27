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
    public class TDONHANG3MgHandler : ICustomManagementSupport
    {
        public void SetCustomManagement(TreeGridMg treeGrid)
        {
            treeGrid.OnRefGridCreated += new OnRefGridCreatedHandler(treeGrid_OnRefGridCreated);
        }

        void treeGrid_OnRefGridCreated(ComponentFactory.Krypton.Navigator.KryptonPage page, No1DataGrid grid, DbMapping.STABLEDESCRow table, int loai)
        {
            if (table.NAME == Tables.TDONHANGCHITIET)
            {
                grid.CustomLoadData += new CustomLoadDataHandler(grid_CustomLoadData);
            }
        }

        void grid_CustomLoadData(object sender, CustomLoadDataArgs e)
        {
            e.Where += " AND SLNHAP > 0";
        }
    }
}
