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
    /// <summary>
    /// XUẤT KHÁC
    /// </summary>
    public partial class TDONHANG2Ae
    {
        private bool suDung2DVT = false;
        private bool coSize = false;
        private bool coHanSd = false;
        private string lastKichThuoc;
        private object lastHsd;
		public void grMatHang_OnAddingRowToGrid(DataRow selRow, DataRow newRow, ref bool cancel)
		{
            if (lastSlLe == 0)
            {
                if (!suDung2DVT)
                {
                    Msg.ShowWarning("Mời bạn nhập số lượng");
                    numSoLuong.SelectAllEx();
                }
                cancel = true;
            }
            else
            {
                newRow["DMATHANGID"] = selRow["ID"];
                newRow["DDONVITINHID"] = selRow["DDONVITINHID"];
                newRow["DDONVITINH_NAME"] = selRow["DDONVITINH_NAME"];
                newRow["DMATHANG_NAME"] = selRow["NAME"];
                newRow["DMATHANG_CODE"] = selRow["CODE"];
                newRow["DONGIA"] = selRow["GIABAN"];
                newRow["SLXUAT"] = lastSlLe;
                newRow["SLXUATCHUAQUYDOI"] = lastSlLe;
                newRow["KICHTHUOC"] = lastKichThuoc;
                if (lastHsd != null) newRow["HANSUDUNG"] = lastHsd;
            }
		}


		public void mapper_OnCalculation(Object sender, EventArgs e)
		{
            decimal thanhTien = detail.CalcSum("THANHTIEN");
            numTIENHANG.Value = thanhTien;
            bool giamTheoTien = mapper["GIAMTHEOTIEN"].ToInt() == 30;
            if (giamTheoTien)
            {                
                numTILEGIAMGIA.Value = numTIENHANG.Value == 0 ? 0 : 100 * numTIENGIAMGIA.Value / numTIENHANG.Value;
            }
            else
            {
                numTIENGIAMGIA.Value = 100 * Math.Round(thanhTien * numTILEGIAMGIA.Value / (100 * 100));
            }
                                    
            numTONGCONG.Value = numTIENHANG.Value - numTIENGIAMGIA.Value;
        }


		public void numTILEGIAMGIA_OnEditValueChanged(Object sender, Object value)
		{
            if (numTILEGIAMGIA.Focused)
            {
                mapper["GIAMTHEOTIEN"].Value = 0;
            }
		}


		public void numTIENGIAMGIA_OnEditValueChanged(Object sender, Object value)
		{
            if (numTIENGIAMGIA.Focused)
            {
                mapper["GIAMTHEOTIEN"].Value = 30;
            }
		}


		public void numSoLuong_KeyDown(Object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Enter) btnThem.PerformClick();
		}


		public void mapper_OnKeyDown(Object sender, KeyEventArgs e)
		{
            if (e.KeyCode == Keys.F3)               
            {
                txtTim.SelectAllEx();
            }
		}


		public Boolean grMatHang_OnAddSameItem(DataRow selRow, DataRow row)
		{
            if (SystemConfig.NhapMotMatHangNhieuLanTrongPhieu == 30)
            {
                return false;
            }

            if (lastSlLe != 0 && selRow["ID"].ToString() == row["DMATHANGID"].ToString() && selRow["DDONVITINHID"].ToString() == row["DDONVITINHID"].ToString())
            {
                if (coSize && row["KICHTHUOC"].ToString() != lastKichThuoc)
                {
                    return false;
                }

                if (coHanSd && ConvertTo.Date(row["HANSUDUNG"]) != ConvertTo.Date(lastHsd))
                {
                    return false;
                }

                row["SLXUAT"] = ConvertTo.Decimal(row["SLXUAT"]) + lastSlLe;
                row["SLXUATCHUAQUYDOI"] = ConvertTo.Decimal(row["SLXUATCHUAQUYDOI"]) + lastSlLe;
                return true;
            }
            return false;
		}


		public void mapper_AfterFillData(Object sender, EventArgs e)
		{
            if (mapper.ID.Length == 0)
            {
                mapper[TDONHANGInfo.TILETHUE].Value = 0;
                mapper[TDONHANGInfo.TIENTHUE].Value = 0;
                mapper[TDONHANGInfo.PHIVANCHUYEN].Value = 0;

                lueDKHOXUATID.EditValue = Shared.DKHOHANGID;
                txtDIENGIAI.Text = "Xuất khác";
            }
		}


		public void mapper_BeforeSave(Object sender, SaveCancelEventArgs e)
		{
            bool kiemTraTon = false;
            if (lueDKHOXUATID.StringValue.Length > 0)
            {
                kiemTraTon = new DKHOHANGRow(lueDKHOXUATID.StringValue).CHOPHEPAMKHO == 0;
            }

            if (coHanSd)
            {
                foreach (DataRow r in detail.Rows)
                {
                    if (r.RowState == DataRowState.Deleted) continue;

                    DMATHANGRow mhRow = new DMATHANGRow(r["DMATHANGID"].ToString());
                    if (mhRow.COHANSUDUNG == 30 && r["HANSUDUNG"] == DBNull.Value)
                    {
                        Msg.ShowWarning("Mời bạn nhập hạn sử dụng cho mặt hàng '" + mhRow.NAME + "'");
                        e.Cancel = true;
                        return;
                    }
                }
            }

            foreach (DataRow r in detail.Rows)
            {
                if (r.RowState == DataRowState.Deleted) continue;
                TDONHANGCHITIETRow row = new TDONHANGCHITIETRow(r);
                if (kiemTraTon && (r.RowState == DataRowState.Added || r.RowState == DataRowState.Modified))
                {
                    //kiem tra xem con du hang trong kho khong?
                    decimal tongXuat = ConvertTo.Decimal( detail.DataSource.Compute("SUM(SLXUAT)", "DMATHANGID = '" + row.DMATHANGID + "'"));
                    decimal val = TonKhoHandler.GetTonKho(row.DMATHANGID, lueDKHOXUATID.StringValue, mapper.ID);
                    if (val < tongXuat)
                    {
                        Msg.ShowWarning("Mặt hàng '" + row["DMATHANG_NAME"].ToString() + "' không đủ xuất, trong kho chỉ còn '" + val.ToString() + "'");
                        e.Cancel = true;
                        return;
                    }
                }

                if (row.DKHOHANGID != lueDKHOXUATID.StringValue)
                {
                    row.DKHOHANGID = lueDKHOXUATID.StringValue;
                }

                if (row.SLXUAT <= 0)
                {
                    Msg.ShowWarning("Số lượng hàng xuất phải lớn hơn 0");
                    e.Cancel = true;
                    return;
                }
            }
		}


        decimal lastSlLe;
        decimal lastDonGia;        
		public void grMatHang_OnThemClicked(Object sender, CancelEventArgs e)
		{
            lastSlLe = numSoLuong.Value;
            lastKichThuoc = "";
            lastSlLe = numSoLuong.Value;

            DMATHANGRow mhRow = new DMATHANGRow(grMatHang.SelectedRow);
            if ((suDung2DVT && TonKhoHandler.Has2DonViTinh(mhRow)) || coSize || (coHanSd && mhRow.COHANSUDUNG == 30))
            {
                lastDonGia = ConvertTo.Decimal(mhRow.GIABAN);                
                decimal quyDoi = 1;
                
                NhapSoLuong obj = (NhapSoLuong)Config.CreateForm(Forms.NhapSoLuong);
                obj.SetMatHang(grMatHang.SelectedID, lueDKHOXUATID.StringValue, lastDonGia, lastSlLe);
                obj.numCK.Enabled = false;
                if (obj.No1Form1.ShowDialog() == DialogResult.OK)
                {
                    lastSlLe = obj.spSoLuong.Value;
                    lastHsd = obj.HanSuDung;
                    lastKichThuoc = obj.KichThuoc;
                    decimal soLuongChan = obj.spSLChan.Value;
                    if (soLuongChan > 0)
                    {
                        quyDoi = mhRow.QUYDOI;
                        ThemSoLuongChan(soLuongChan, ConvertTo.Decimal(grMatHang.SelectedRow["GIABANCHAN"]), 0, quyDoi);
                    }
                }
                else
                {
                    e.Cancel = true;
                    return;
                }                
            }
		}

        private void ThemSoLuongChan(decimal lastSlChan, decimal lastDonGiaChan, decimal lastCk, decimal quyDoi)
        {
            //neu co so luong chan thi them luon
            if (lastSlChan > 0)
            {
                string DMATHANGID = grMatHang.SelectedID;
                string DDONVITINHCHANID = grMatHang.SelectedRow["DDONVITINHCHANID"].ToString();
                if (SystemConfig.NhapMotMatHangNhieuLanTrongPhieu != 30)
                {
                    //kiem tra xem da ton tai trong danh sach chua?
                    DataTable dt = detail.DataSource as DataTable;
                    foreach (DataRow r in dt.Rows)
                    {
                        if (r.RowState == DataRowState.Deleted) continue;
                        TDONHANGCHITIETRow ctRow = new TDONHANGCHITIETRow(r);
                        if (ctRow.DMATHANGID == DMATHANGID && ctRow.DDONVITINHID == DDONVITINHCHANID)
                        {
                            //tang so luong len
                            ctRow.SLXUATCHUAQUYDOI += lastSlChan;
                            //quy ra so luong le
                            ctRow.SLXUAT = ctRow.SLXUATCHUAQUYDOI * quyDoi;
                            detail.CalculateRow(r);
                            return;
                        }
                    }
                }

                //neu chua co thi them vao                
                TDONHANGCHITIETRow newRow = new TDONHANGCHITIETRow(detail.DataSource.NewRow());
                newRow.DMATHANGID = DMATHANGID;
                newRow.DDONVITINHID = DDONVITINHCHANID;
                newRow.SLXUATCHUAQUYDOI = lastSlChan;
                newRow.SLXUAT = newRow.SLXUATCHUAQUYDOI * quyDoi;
                newRow.DONGIA = lastDonGiaChan;
                //hien thi ten, don vi, ma san co...                
                newRow["DDONVITINH_NAME"] = grMatHang.SelectedRow["DDONVITINH2_NAME"];
                newRow["DMATHANG_NAME"] = grMatHang.SelectedRow["NAME"];
                newRow["DMATHANG_CODE"] = grMatHang.SelectedRow["CODE"];
                newRow["DMATHANG_MASANCO"] = grMatHang.SelectedRow["MASANCO"];
                newRow.TILEGIAMGIA = 0;
                newRow.DKHOHANGID = lueDKHOXUATID.StringValue;

                detail.DataSource.Rows.Add(newRow.Row);
                detail.CalculateRow(newRow.Row);
            }
        }

		public void mapper_OnLoad(Object sender, EventArgs e)
		{
            suDung2DVT = SystemConfig.SuDung2DonViTinh == 30;
            coSize = SystemConfig.MatHangCoKichThuoc == 30;
            coHanSd = SystemConfig.MatHangCoHanSuDung == 30;
            mapper.Controller.Maximize();
            mapper.Controller.AllowMaxWindows = true;
            mapper.Controller.SetSaveByEnterKey(false);
		}


		public void detail_OnRowCalculate(Object sender, DataRow r, String fieldName)
		{
            if (suDung2DVT && fieldName == "SLXUATCHUAQUYDOI")
            {
                string DMATHANGID = r["DMATHANGID"].ToString();
                DMATHANGRow mhRow = new DMATHANGRow(DMATHANGID);
                decimal quyDoi = 1;
                if (TonKhoHandler.Has2DonViTinh(mhRow) && r["DDONVITINHID"].ToString() != mhRow.DDONVITINHID)
                {
                    quyDoi = mhRow.QUYDOI;
                }
                r["SLXUAT"] = quyDoi * ConvertTo.Decimal(r["SLXUATCHUAQUYDOI"]);
            }
		}


		public void lueDKHOXUATID_CustomLoadData(object sender, CustomLoadDataArgs e)
		{
            if (!DbConfig.IsAdmin)
            {
                e.Where += " AND DCUAHANGID IN (SELECT DCUAHANGID FROM TNGUOIDUNGTHEOCUAHANG WHERE SUSERID = '" + DbConfig.UserID + "')";
            }
		}


		public void detail_OnImpExcelColumnShowing(object sender, List<ImportColumn> lst)
		{
            suDungMa = SystemConfig.SuDungMaHang == 30;
            if (SystemConfig.SuDung2DonViTinh == 30)
                lst.Insert(0, new ImportColumn("ĐVT", "DVT", typeof(string)));

            if (SystemConfig.SuDungMaHang == 30)
                lst.Insert(0, new ImportColumn("Mã hàng", "MAHANG", typeof(string)));
            else
                lst.Insert(0, new ImportColumn("Tên hàng", "MAHANG", typeof(string)));
		}


		public void detail_OnImpExcelRowValidate(object sender, DataRow row, Boolean hasData, CancelEventArgs e)
		{
            //bỏ qua các dòng không có mặt hàng và số lượng
            if (ConvertTo.Decimal(row["SLXUATCHUAQUYDOI"]) == 0) e.Cancel = true;
            else
            {
                string code = row["MAHANG"].ToString().Trim();
                if (code.Length == 0) e.Cancel = true;
                else
                {
                    //kiem tra xem co ton tai mat hang khong?
                    if (dicMatHang.ContainsKey(code.ToLower()))
                    {
                        DataRow r = dicMatHang[code.ToLower()];
                        row["DMATHANGID"] = r["ID"];
                        row["DMATHANG_NAME"] = r["NAME"];
                        row["DMATHANG_CODE"] = r["CODE"];
                        row["DMATHANG_MASANCO"] = r["MASANCO"];
                        row["DONGIA"] = r["GIABAN"];

                        row["QUYDOI"] = 1;                        
                        row["DDONVITINHID"] = r["DDONVITINHID"];
                        row["DDONVITINH_NAME"] = r["DDONVITINH_NAME"];
                        //kiem tra xem don vi tinh co dung khong?
                        string dvt = row["DVT"].ToString();
                        if (dvt.Length > 0)
                        {
                            if (dicDvt.ContainsKey(dvt.ToLower()))
                            {
                                DataRow rDvt = dicDvt[dvt.ToLower()];
                                string DVTID = rDvt["ID"].ToString();
                                if (DVTID == r["DDONVITINHCHANID"].ToString())
                                {
                                    row["QUYDOI"] = r["QUYDOI"];
                                    row["DDONVITINHID"] = DVTID;
                                    row["DDONVITINH_NAME"] = dvt;
                                }
                                else if (DVTID != r["DDONVITINHID"].ToString())
                                {
                                    error.AppendLine("Mặt hàng '" + r["NAME"].ToString() + "' không có đơn vị tính '" + dvt + "'");
                                    e.Cancel = true;
                                }
                            }
                            else
                            {
                                error.AppendLine("Đơn vị tính '" + dvt + "' ở mặt hàng '" + r["NAME"].ToString() + "' không tồn tại trong hệ thống");
                                e.Cancel = true;
                            }
                        }

                        if (coHanSd)
                        {
                            if (ConvertTo.Int(r["COHANSUDUNG"]) == 30)
                            {
                                if (row["HANSUDUNG"] == DBNull.Value)
                                {
                                    error.AppendLine("Mặt hàng '" + r["NAME"].ToString() + "' phải có hạn dùng khi chuyển kho");
                                    e.Cancel = true;
                                }
                            }
                            else
                            {
                                row["HANSUDUNG"] = DBNull.Value;
                            }
                        }
                    }
                    else
                    {
                        error.AppendLine("Mã hàng '" + code + "' không tồn tại trong hệ thống");
                        e.Cancel = true;
                    }
                }
            }
		}


		public void detail_OnImpExcelRowAdding(object sender, DataRow row)
		{
            DataTable dt = detail.DataSource;
            DataRow newRow = dt.NewRow();
            newRow["DDONVITINH_NAME"] = row["DDONVITINH_NAME"];
            newRow["DDONVITINHID"] = row["DDONVITINHID"];
            newRow["DMATHANGID"] = row["DMATHANGID"];
            newRow["DMATHANG_NAME"] = row["DMATHANG_NAME"];
            newRow["DMATHANG_CODE"] = row["DMATHANG_CODE"];
            newRow["TILEGIAMGIA"] = ConvertTo.Decimal(row["TILEGIAMGIA"]);
            newRow["DMATHANG_MASANCO"] = row["DMATHANG_MASANCO"];            
            newRow["SLXUATCHUAQUYDOI"] = row["SLXUATCHUAQUYDOI"];
            newRow["DONGIA"] = row["DONGIA"];            
            newRow["SLXUAT"] = ConvertTo.Decimal(row["SLXUATCHUAQUYDOI"]) * ConvertTo.Decimal(row["QUYDOI"]);
            if (row["HANSUDUNG"] != DBNull.Value) newRow["HANSUDUNG"] = ConvertTo.Date(row["HANSUDUNG"]);
            newRow["KICHTHUOC"] = row["KICHTHUOC"];
            dt.Rows.Add(newRow);
            detail.CalculateRow(newRow);
		}


		public void detail_OnImpExcelDataValidate(object sender, DataTable dt, CancelEventArgs e)
		{
            //kiểm tra xem
            if (error.Length > 0)
            {
                Msg.ShowWarning(error.ToString());
                e.Cancel = true;
            }
		}

        bool suDungMa = false;
        Dictionary<string, DataRow> dicMatHang;
        Dictionary<string, DataRow> dicDvt;
        StringBuilder error;
		public void detail_OnImpExcelDataPrepare(object sender, DataTable dt)
		{            
            //lấy danh sách mặt hàng
            error = new StringBuilder();
            dicMatHang = new Dictionary<string, DataRow>();
            dicDvt = new Dictionary<string, DataRow>();
            DataTable dtMatHang = Config.Db.GetTable("SELECT ID, CODE, COHANSUDUNG, GIABAN, NAME, MASANCO, QUYDOI, DDONVITINHID, DDONVITINHCHANID, (SELECT NAME FROM DDONVITINH WHERE ID = DDONVITINHID) AS DDONVITINH_NAME FROM DMATHANG WHERE STATUS = 30");
            foreach (DataRow r in dtMatHang.Rows)
            {
                string code = r[suDungMa ? "CODE" : "NAME"].ToString().ToLower();
                if (!dicMatHang.ContainsKey(code)) dicMatHang.Add(code, r);
            }
            DataTable dtDvt = DbUtils.Select("ID, NAME", Tables.DDONVITINH, "STATUS = 30");
            foreach (DataRow r in dtDvt.Rows)
            {
                string code = r["NAME"].ToString().ToLower();
                if (!dicDvt.ContainsKey(code)) dicDvt.Add(code, r);
            }

            //thêm các cột dự trữ
            dt.Columns.Add("DMATHANGID", typeof(string));
            dt.Columns.Add("DMATHANG_NAME", typeof(string));
            dt.Columns.Add("DMATHANG_CODE", typeof(string));
            dt.Columns.Add("DMATHANG_MASANCO", typeof(string));
            dt.Columns.Add("DDONVITINHID", typeof(string));
            dt.Columns.Add("DDONVITINH_NAME", typeof(string));
            dt.Columns.Add("QUYDOI", typeof(decimal));
            if (dt.Columns["DVT"] == null) dt.Columns.Add("DVT", typeof(string));
            if (dt.Columns["DONGIA"] == null) dt.Columns.Add("DONGIA", typeof(decimal));
            if (dt.Columns["HANSUDUNG"] == null) dt.Columns.Add("HANSUDUNG", typeof(DateTime));
            if (dt.Columns["KICHTHUOC"] == null) dt.Columns.Add("KICHTHUOC", typeof(string));
		}


		public void detail_OnImpExcelColumnSelectionValidate(object sender, List<string> lstSelected, CancelEventArgs e)
		{
            suDungMa = SystemConfig.SuDungMaHang == 30;
            //bắt buộc phải có cột "Mã hàng" hoặc "Tên hàng", "Số lượng" và "Đơn giá"
            if (suDungMa)
            {
                if (!lstSelected.Contains("Mã hàng"))
                {
                    Msg.ShowWarning("Mời bạn chọn cột 'Mã hàng'");
                    e.Cancel = true;
                }
            }
            else
            {
                if (!lstSelected.Contains("Tên hàng"))
                {
                    Msg.ShowWarning("Mời bạn chọn cột 'Tên hàng'");
                    e.Cancel = true;
                }
            }

            if (!lstSelected.Contains("Số lượng"))
            {
                Msg.ShowWarning("Mời bạn chọn cột 'Số lượng'");
                e.Cancel = true;
            }
		}


		public void detail_OnImpExcelFinished(object sender, EventArgs e)
		{
            mapper.RaiseOnCalculation();
		}


		public void mapper_AfterSave(object sender, EventArgs e)
		{
            if (SystemConfig.SuDung2DonViTinh == 0)
            {
                Config.Db.ExecSql("UPDATE TDONHANGCHITIET SET DKHOHANGID = '" + lueDKHOXUATID.StringValue + "', SLNHAP = COALESCE(SLNHAPCHUAQUYDOI, 0), SLXUAT = COALESCE(SLXUATCHUAQUYDOI, 0) WHERE TDONHANGID = '" + mapper.ID + "'");
            }
		}


		public void grMatHang_CustomLoadData(object sender, CustomLoadDataArgs e)
		{
            if (SystemConfig.SapXepThuTuTheo == 0)
            {
                e.OrderBy = "CODE";
            }
            else
            {
                e.OrderBy = "NAME";
            }
		}
    }
}
