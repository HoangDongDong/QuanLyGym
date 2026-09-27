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
    public partial class NhapHanSuDung
    {
        internal void SetMatHang(DMATHANGRow mhRow)
        {
            lblSanPham.Text = lblSanPham.Text + " " + mhRow.NAME.ToUpper();
        }


        public void btnChapNhan_Click(object sender, EventArgs e)
        {
            if (dtHanDung.IsEmpty)
            {
                Msg.ShowWarning("Mời bạn nhập hạn sử dụng");
            }
            else if (dtHanDung.DateTime < Config.Db.DbDate)
            {
                if (Msg.ShowYesNo("Hạn sử dụng trước ngày hiện tại, bạn có muốn thực hiện tiếp không?") == DialogResult.Yes)
                {
                    No1Form1.DialogResult = DialogResult.OK;
                }
            }
            else
            {
                No1Form1.DialogResult = DialogResult.OK;
            }
        }
    }
}
