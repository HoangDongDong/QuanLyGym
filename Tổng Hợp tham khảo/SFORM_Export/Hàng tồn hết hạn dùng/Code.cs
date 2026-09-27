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
    public partial class HangTonHetHanDung
    {
        internal void LoadData(bool sapHetHan)
        {
            string sql = @"SELECT DMATHANG.NAME, DMATHANG.CODE, HANSUDUNG,
                (SELECT NAME FROM DDONVITINH WHERE ID = DMATHANG.DDONVITINHID) AS DVT,
                DNHOMMATHANGID,
                DATEDIFF (DAY FROM HANSUDUNG TO CURRENT_DATE) AS NGAYCON,
                SUM(COALESCE(SLNHAP, 0) - COALESCE(SLXUAT, 0)) AS TON
                FROM TDONHANGCHITIET INNER JOIN DMATHANG ON TDONHANGCHITIET.DMATHANGID = DMATHANG.ID
                AND COHANSUDUNG = 30
                INNER JOIN TDONHANG ON TDONHANGCHITIET.TDONHANGID = TDONHANG.ID
                AND ((LOAI = 0 AND DATHANHTOAN = 30) OR LOAI <> 0)
                @WHERE
                GROUP BY DMATHANG.NAME, DMATHANG.CODE, HANSUDUNG, DMATHANG.DDONVITINHID, DNHOMMATHANGID
                HAVING SUM(COALESCE(SLNHAP, 0) - COALESCE(SLXUAT, 0)) > 0";
            if (sapHetHan)
            {
                int soNgay = SystemConfig.ThongBaoTruocNgay;                
                sql = sql.Replace("@WHERE", "AND HANSUDUNG >= CURRENT_DATE AND HANSUDUNG <= DATEADD(" + soNgay.ToString() + " DAY TO CURRENT_DATE)");
                sql = sql.Replace("DATEDIFF (DAY FROM HANSUDUNG TO CURRENT_DATE) AS NGAYCON", "DATEDIFF (DAY FROM CURRENT_DATE TO HANSUDUNG) AS NGAYCON");
            }
            else
            {
                sql = sql.Replace("@WHERE", "AND HANSUDUNG < CURRENT_DATE");
                colSoNgay.HeaderText = "Ngày quá hạn";
            }

            grMain.LoadDataSearchable(Config.Db.GetTable(sql));
        }


        public void tvMain_OnFocusedNodeChanged(TreeNode node, DataRow r, string ID, TreeItemType type)
        {
            grMain.Filter = txtLoc.Text;
        }


        public void grMain_OnCustomFilter(ref String filter)
        {
            if (tvMain.SelectedID.Length > 0)
            {
                if (filter.Length > 0) filter += " AND ";
                filter += "DNHOMMATHANGID = '" + tvMain.SelectedID + "'";
            }
        }
    }
}
