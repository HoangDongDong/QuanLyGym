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
    public partial class NhapTheTraTruoc
    {
        public string TheTraTruocID = "";
		public void btnOK_Click(object sender, EventArgs e)
		{
            if (txtMa.Text.Trim().Length == 0)
            {
                Msg.ShowWarning("Mời bạn nhập voucher trước");
                return;
            }

            //kiểm tra xem voucher có tồn tại không
            string sql = "SELECT * FROM DTHETRATRUOC WHERE NAME = @NAME";
            FbCommand cmd = Config.Db.GetCommand(sql);
            cmd.Parameters.Add("@NAME", FbDbType.VarChar).Value = txtMa.Text.Trim();
            DataRow r = Config.Db.GetFirstRow(cmd);

            if (r == null)
            {
                Msg.ShowWarning("Mã thẻ trả trước '" + txtMa.Text + "' không tồn tại trong hệ thống");
                txtMa.Text = "";
                txtMa.SelectAll();
                return;
            }

            DTHETRATRUOCRow row = new DTHETRATRUOCRow(r);
            //quá hạn sử dụng?
            if (!row.IsNullValue(DTHETRATRUOCInfo.NGAYHETHAN))
            {
                if (row.NGAYHETHAN < Config.Db.DbDate)
                {
                    Msg.ShowWarning("Mã thẻ này đã quá hạn sử dụng");
                    txtMa.Text = "";
                    txtMa.SelectAll();
                    return;
                }
            }

            if (row.KHOA == 30)
            {
                Msg.ShowWarning("Thẻ này đã bị khóa, không thể sử dụng");
                txtMa.Text = "";
                txtMa.SelectAll();
                return;
            }

            //kiểm tra số tiền còn lại trong thẻ xem còn hay đã hết
            SoTienConLai = GetSoTienConLai(row.ID);
            if (SoTienConLai <= 0)
            {
                Msg.ShowWarning("Thẻ này không còn tiền để sử dụng");
                txtMa.Text = "";
                txtMa.SelectAll();
                return;
            }

            TheTraTruocID = row.ID;
            No1Form1.DialogResult = DialogResult.OK;
		}

        public decimal SoTienConLai;

        public static decimal GetSoTienConLai(string DTHETRATRUOCID)
        {
            decimal nap;
            decimal suDung;
            return GetSoTienConLai(DTHETRATRUOCID, out nap, out suDung);
        }

        public static decimal GetSoTienConLai(string DTHETRATRUOCID, out decimal nap, out decimal suDung)
        {
            if (DTHETRATRUOCID.Length == 0)
            {
                nap = 0;
                suDung = 0;
                return 0;
            }
            //lấy số tiền nạp
            string sql = String.Format("SELECT SUM(THU) FROM TTHUCHI WHERE DTHETRATRUOCID = '{0}'", DTHETRATRUOCID);
            nap = Config.Db.GetFirstFieldDec(sql);
            //lấy số tiền thanh toán
            sql = String.Format("SELECT SUM(COALESCE(THETRATRUOC, 0)) FROM TDONHANG WHERE DTHETRATRUOCID = '{0}'", DTHETRATRUOCID);
            suDung = Config.Db.GetFirstFieldDec(sql);
            return nap - suDung;
        }
    }
}
