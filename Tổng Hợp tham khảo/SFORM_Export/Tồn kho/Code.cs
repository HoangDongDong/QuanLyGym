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
using ComponentFactory.Krypton.Navigator;

namespace No1Run
{
    public class TonKhoHandler : ITonKhoSupport
    {
        internal static bool Reminder = false;
        TonKho tonKho;
        bool co2DVT;
        private bool reminder;
        public void SetTonKho(TonKho tonKho)
        {
            if (tonKho.MenuRow.ID == Menus.TonNhieuKho)
            {
                tonKho.TatCaCacKho = true;
            }
            co2DVT = SystemConfig.SuDung2DonViTinh == 30;
            reminder = Reminder;
            this.tonKho = tonKho;
            tonKho.OnInit += new EventHandler(tonKho_OnInit);
            tonKho.CustomLoadData += new OnCustomLoadDataHandler(tonKho_CustomLoadData);
            tonKho.CustomLoadDetail += new OnCustomLoadDataHandler(tonKho_CustomLoadDetail);
            tonKho.OnAfterGetData += new OnAfterGetDataHandler(tonKho_OnAfterGetData);
            tonKho.RefreshOnMainFormSelectedPageChanged += new EventHandler(tonKho_RefreshOnMainFormSelectedPageChanged);            
        }

        void tonKho_CustomLoadDetail(FbCommand cmd)
        {
            string commandText = cmd.CommandText;
            commandText += ", TDONHANG_TIMECREATED ASC";
            commandText = commandText.Replace("TDONHANG.DIENGIAI AS TDONHANG_DIENGIAI", "TDONHANG.TIMECREATED AS TDONHANG_TIMECREATED, TDONHANG.DIENGIAI AS TDONHANG_DIENGIAI");
            cmd.CommandText = commandText;
        }

        void tonKho_CustomLoadData(FbCommand cmd)
        {
            if (SystemConfig.SapXepThuTuTheo == 0)
            {
                cmd.CommandText = cmd.CommandText + " ORDER BY CODE";
            }
            else
            {
                cmd.CommandText = cmd.CommandText + " ORDER BY NAME";
            }            
        }

        void tonKho_OnAfterGetData(DataTable dt)
        {
            if (co2DVT)
            {
                //tinh toan lai
                foreach (DataRow r in dt.Rows)
                {
                    DMATHANGRow row = new DMATHANGRow(r);
                    if (Has2DonViTinh(row))
                    {
                        decimal tonKho = ConvertTo.Decimal(row["TONKHO_"]);
                        int tonChan = (int)(tonKho / row.QUYDOI);
                        decimal tonLe = tonKho - tonChan * row.QUYDOI;
                        row.NOTE = (tonChan == 0 ? "" : tonChan.ToString() + " " + r["DDONVITINH2_NAME"].ToString() + " ") +
                                    (tonLe == 0 ? "" : tonLe.ToString() + " " + r["DDONVITINH_NAME"].ToString());
                    }
                }
            }

            if (reminder)
            {
                //
                for (int i = dt.Rows.Count - 1; i >= 0; i--)
                {
                    DataRow row = dt.Rows[i];
                    decimal tonKho = ConvertTo.Decimal(row["TONKHO_"]);
                    decimal tonToiThieu = ConvertTo.Decimal(row["TONTOITHIEU"]);
                    if (tonKho > tonToiThieu || tonToiThieu == 0)
                    {
                        dt.Rows.RemoveAt(i);
                    }
                }
            }
        }

        void tonKho_RefreshOnMainFormSelectedPageChanged(object sender, EventArgs e)
        {
            tonKho.LoadData();
        }

        KryptonPage pageTonTheoSize;
        KryptonPage pageTonTheoHsd;
        MiscDataGridView grTonTheoSize;
        MiscDataGridView grTonTheoHsd;
        void tonKho_OnInit(object sender, EventArgs e)
        {
            tonKho.lueKhoHang.CustomLoadData += new CustomLoadDataHandler(lueKhoHang_CustomLoadData);
            tonKho.lueKhoHang.LockEvent = true;
            tonKho.lueKhoHang.EditValue = Shared.DKHOHANGID;
            tonKho.lueKhoHang.LockEvent = false;
            if (!DbUtils.CanView(Functions.XemGiaNhap))
            {
                HideColumn("DONGIA");
                HideColumn("THANHTIEN");
                HideColumn("GIAVON");
                HideColumn("colGiaTriVon");
                HideColumn("colGiaTriBan");
            }

            bool needAddEvent = false;

            if (SystemConfig.MatHangCoKichThuoc == 30 && !tonKho.TatCaCacKho)
            {
                //thêm tab                
                pageTonTheoSize = new KryptonPage("Tồn theo kích thước");
                tonKho.tabDetail.Pages.Add(pageTonTheoSize);
                grTonTheoSize = new MiscDataGridView();
                DataGridViewTextBoxColumn colKichThuoc = new DataGridViewTextBoxColumn();
                colKichThuoc.HeaderText = "Kích thước";
                colKichThuoc.DataPropertyName = "KICHTHUOC";
                colKichThuoc.Width = 150;
                grTonTheoSize.Columns.Add(colKichThuoc);
                NumericDataGridViewColumn colTon = new NumericDataGridViewColumn();
                colTon.HeaderText = "Tồn";
                colTon.DataPropertyName = "TON";
                colTon.Width = 100;                
                grTonTheoSize.Columns.Add(colTon);
                grTonTheoSize.GUID = "11111111";                
                grTonTheoSize.Dock = DockStyle.Fill;
                grTonTheoSize.ReadOnly = true;
                grTonTheoSize.AllowUserToDeleteRows = false;
                grTonTheoSize.AllowUserToAddRows = false;

                pageTonTheoSize.Controls.Add(grTonTheoSize);
                needAddEvent = true;
            }

            if (SystemConfig.MatHangCoHanSuDung == 30 && !tonKho.TatCaCacKho)
            {
                //thêm tab                
                pageTonTheoHsd = new KryptonPage("Tồn theo hạn dùng");
                tonKho.tabDetail.Pages.Add(pageTonTheoHsd);
                grTonTheoHsd = new MiscDataGridView();
                DataGridViewTextBoxColumn colHsd = new DataGridViewTextBoxColumn();
                colHsd.HeaderText = "Hạn dùng";
                colHsd.DataPropertyName = "HANSUDUNG";
                colHsd.Width = 150;
                grTonTheoHsd.Columns.Add(colHsd);
                NumericDataGridViewColumn colTon = new NumericDataGridViewColumn();
                colTon.HeaderText = "Tồn";
                colTon.DataPropertyName = "TON";
                colTon.Width = 100;                
                grTonTheoHsd.Columns.Add(colTon);
                grTonTheoHsd.GUID = "222222222";                
                grTonTheoHsd.Dock = DockStyle.Fill;
                grTonTheoHsd.ReadOnly = true;
                grTonTheoHsd.AllowUserToDeleteRows = false;
                grTonTheoHsd.AllowUserToAddRows = false;
                pageTonTheoHsd.Controls.Add(grTonTheoHsd);
                needAddEvent = true;
            }

            if (needAddEvent)
            {
                tonKho.grMain.SelectionChanged += new EventHandler(grMain_SelectionChanged);
                tonKho.tabDetail.SelectedPageChanged += new EventHandler(tabDetail_SelectedPageChanged);
            }
        }

        void tabDetail_SelectedPageChanged(object sender, EventArgs e)
        {
            LoadTonKhoChiTiet();
        }

        private void LoadTonKhoChiTiet()
        {
            if (tonKho.tabDetail.SelectedPage == pageTonTheoSize)
            {
                LoadTonTheoSize();
            }
            else if (tonKho.tabDetail.SelectedPage == pageTonTheoHsd)
            {
                LoadTonTheoHsd();
            }
        }

        private void LoadTonTheoHsd()
        {
            if (tonTheoHsdLoaded) return;

            string DMATHANGID = tonKho.grMain.SelectedID;
            if (DMATHANGID.Length == 0)
            {
                grTonTheoHsd.DataSource = null;
            }
            else
            {
                string sql = @"SELECT SUM(SLNHAP - SLXUAT) AS TON, HANSUDUNG, 0 AS SOLUONG
FROM TDONHANG INNER JOIN TDONHANGCHITIET ON TDONHANG.ID = TDONHANGCHITIET.TDONHANGID
WHERE DMATHANGID = @DMATHANGID
AND DKHOHANGID = @DKHOHANGID
AND ((LOAI = 0 AND DATHANHTOAN = 30) OR LOAI <> 0)
GROUP BY HANSUDUNG
HAVING SUM(SLNHAP - SLXUAT) <> 0
ORDER BY HANSUDUNG";
                FbCommand cmd = Config.Db.GetCommand(sql);
                cmd.Parameters.Add("@DMATHANGID", FbDbType.VarChar).Value = DMATHANGID;
                cmd.Parameters.Add("@DKHOHANGID", FbDbType.VarChar).Value = tonKho.lueKhoHang.StringValue;

                grTonTheoHsd.DataSource = Config.Db.GetTable(cmd);
            }
            tonTheoHsdLoaded = true;
        }

        private bool tonTheoSizeLoaded = false;
        private bool tonTheoHsdLoaded = false;
        private void LoadTonTheoSize()
        {
            if (tonTheoSizeLoaded) return;

            string DMATHANGID = tonKho.grMain.SelectedID;
            if (DMATHANGID.Length == 0)
            {
                grTonTheoSize.DataSource = null;
            }
            else
            {
                string sql = @"SELECT SUM(COALESCE(SLNHAP, 0) - COALESCE(SLXUAT, 0)) AS TON, KICHTHUOC FROM TDONHANG INNER JOIN TDONHANGCHITIET ON TDONHANG.ID = TDONHANGCHITIET.TDONHANGID
WHERE DMATHANGID = @DMATHANGID AND ((LOAI = 0 AND DATHANHTOAN = 30) OR LOAI <> 0) AND DKHOHANGID = @DKHOHANGID
GROUP BY KICHTHUOC
HAVING SUM(COALESCE(SLNHAP, 0) - COALESCE(SLXUAT, 0)) <> 0";
                FbCommand cmd = Config.Db.GetCommand(sql);
                cmd.Parameters.Add("@DMATHANGID", FbDbType.VarChar).Value = DMATHANGID;
                cmd.Parameters.Add("@DKHOHANGID", FbDbType.VarChar).Value = tonKho.lueKhoHang.StringValue;
                grTonTheoSize.DataSource = Config.Db.GetTable(cmd);                
            }
            tonTheoSizeLoaded = true;
        }

        void grMain_SelectionChanged(object sender, EventArgs e)
        {
            tonTheoSizeLoaded = false;
            tonTheoHsdLoaded = false;
            LoadTonKhoChiTiet();            
        }

        private void HideColumn(string col)
        {
            DataGridViewColumn c = tonKho.grDetail.Columns[col];
            if (c != null) c.Visible = false;
        }

        void lueKhoHang_CustomLoadData(object sender, CustomLoadDataArgs e)
        {
            if (!DbConfig.IsAdmin)
            {
                e.Where += " AND DCUAHANGID IN (SELECT DCUAHANGID FROM TNGUOIDUNGTHEOCUAHANG WHERE SUSERID = '" + DbConfig.UserID + "')";
            }
        }

        public static decimal GetTonKho(string DMATHANGID, string DKHOID, string TDONHANGID)
        {
            string sql = "SELECT COALESCE(SUM(COALESCE(SLNHAP, 0) - COALESCE(SLXUAT, 0)), 0) FROM TDONHANGCHITIET INNER JOIN TDONHANG ON TDONHANG.ID = TDONHANGCHITIET.TDONHANGID WHERE DMATHANGID = '" + DMATHANGID + "' AND DKHOHANGID = '" + DKHOID + "' AND TDONHANG.ID <> '" + TDONHANGID + "' AND ((LOAI <> 0) OR (LOAI = 0 AND DATHANHTOAN = 30))";
            return ConvertTo.Decimal(Config.Db.GetFirstField(sql));
        }

        public static decimal GetTonKhoEx(string DMATHANGID, string DKHOID, string TDONHANGID, string KichThuoc, object HanSuDung)
        {
            FbCommand cmd = Config.Db.GetCommand("");
            string sql = "SELECT COALESCE(SUM(COALESCE(SLNHAP, 0) - COALESCE(SLXUAT, 0)), 0) FROM TDONHANGCHITIET INNER JOIN TDONHANG ON TDONHANG.ID = TDONHANGCHITIET.TDONHANGID WHERE DMATHANGID = '" + DMATHANGID + "' AND DKHOHANGID = '" + DKHOID + "' AND TDONHANG.ID <> '" + TDONHANGID + "' AND ((LOAI <> 0) OR (LOAI = 0 AND DATHANHTOAN = 30))";
            if (KichThuoc.Length > 0)
                sql += " AND KICHTHUOC = '" + KichThuoc.Replace("'", "''") + "'";
            if (HanSuDung != null)
            {
                sql += " AND HANSUDUNG = @HSD";
                cmd.Parameters.Add("@HSD", FbDbType.Date).Value = HanSuDung;
            }

            cmd.CommandText = sql;
            return Config.Db.GetFirstFieldDec(cmd);
        }

        public static bool Has2DonViTinh(DMATHANGRow row)
        {
            if (row.DDONVITINHCHANID.Length > 0 && row.DDONVITINHID != row.DDONVITINHCHANID)
            {
                return true;
            }
            return false;
        }
    }
}
