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
    public partial class LichSuMuaBan
    {
        public void SetData(string DMATHANGID, bool LichSuMua)
        {
            lblTenHang.Text = new DMATHANGRow(DMATHANGID).NAME;
            string sql;
            if (LichSuMua)
            {
                sql = @"SELECT NGAY, EXTRACT(HOUR FROM TDONHANG.TIMECREATED) || ':' || EXTRACT(MINUTE FROM TDONHANG.TIMECREATED) AS GIO, 
DONGIA, 
(SELECT NAME FROM DNHACUNGCAP WHERE ID = DNHACUNGCAPID) AS KHACHHANG 
FROM TDONHANG INNER JOIN TDONHANGCHITIET ON TDONHANG.ID = TDONHANGCHITIET.TDONHANGID
AND DMATHANGID = '" + DMATHANGID + "' AND LOAI = 1";

                colKhachHang.HeaderText = "Nhà cung cấp";
                form.Text = "LỊCH SỬ MUA HÀNG";
            }
            else
            {
                sql = @"SELECT NGAY, EXTRACT(HOUR FROM GIOTHANHTOAN) || ':' || EXTRACT(MINUTE FROM GIOTHANHTOAN) AS GIO, DONGIA, (SELECT NAME FROM DKHACHHANG WHERE ID = DKHACHHANGID) AS KHACHHANG 
FROM TDONHANG INNER JOIN TDONHANGCHITIET ON TDONHANG.ID = TDONHANGCHITIET.TDONHANGID
AND DMATHANGID = '" + DMATHANGID + "' AND LOAI = 0 AND DATHANHTOAN = 30 ORDER BY NGAY, GIOTHANHTOAN";
            }

            grMain.LoadDataSearchable(Config.Db.GetTable(sql));
        }
    }
}
