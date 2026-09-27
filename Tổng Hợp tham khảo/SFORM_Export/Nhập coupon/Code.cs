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
    public partial class NhapCoupon
    {
        public string VoucherID = "";
        public decimal GiaTriVoucher = 0;

		public void btnOK_Click(object sender, EventArgs e)
		{
            if (txtMa.Text.Trim().Length == 0)
            {
                Msg.ShowWarning("Mời bạn nhập voucher trước");
                return;
            }

            //kiểm tra xem voucher có tồn tại không
            string sql = "SELECT * FROM DVOUCHER WHERE NAME = @NAME";
            FbCommand cmd = Config.Db.GetCommand(sql);
            cmd.Parameters.Add("@NAME", FbDbType.VarChar).Value = txtMa.Text.Trim();
            DataRow r = Config.Db.GetFirstRow(cmd);
                
            if (r == null)
            {
                Msg.ShowWarning("Mã voucher '" + txtMa.Text + "' không tồn tại trong hệ thống");
                txtMa.Text = "";
                txtMa.SelectAll();
                return;
            }

            DVOUCHERRow row = new DVOUCHERRow(r);
            //quá hạn sử dụng?
            if (!row.IsNullValue(DVOUCHERInfo.HANSUDUNG))
            {
                if (row.HANSUDUNG < Config.Db.DbDate)
                {
                    Msg.ShowWarning("Mã thẻ này đã quá hạn sử dụng");
                    txtMa.Text = "";
                    txtMa.SelectAll();
                    return;
                }
            }

            //kiểm tra xem voucher có sử dụng chưa?
            sql = String.Format("SELECT COUNT(*) FROM TDONHANG WHERE DVOUCHERID = '{0}'", row.ID);
            if (Config.Db.GetFirstFieldInt(sql) > 0)
            {
                Msg.ShowWarning("Mã thẻ này đã được sử dụng");
                txtMa.Text = "";
                txtMa.SelectAll();
                return;
            }

            VoucherID = row.ID;
            GiaTriVoucher = row.GIATRI;
            No1Form1.DialogResult = DialogResult.OK;
		}
    }
}
