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
using ComponentFactory.Krypton.Toolkit;

namespace No1Run
{
    public class TonQuyHandler : ITonQuySupport
    {
        KryptonSplitContainer split;
        No1LookupEdit lue;
        TonQuy tonQuy;
        TreeView tv;
        TreeNode allNode;
        TreeNode nganHangNode;
        TreeNode tienMatNode;
        TreeNode quetTheNode;
        public void SetTonQuy(TonQuy tonQuy)
        {
            this.tonQuy = tonQuy;
            tonQuy.CustomLoadData += new TonQuy.CustomLoadDataHandler(tonQuy_CustomLoadData);

            //tạo thêm cửa sổ chọn cửa hàng
            lue = new No1LookupEdit();
            No1Label lbl = new No1Label();
            lbl.Text = "Cửa hàng:";

            tonQuy.pnlHeader.Controls.Add(lbl);
            lbl.Location = new Point(tonQuy.dtNgay.Right + 20, (tonQuy.pnlHeader.Height - lbl.Height) / 2);
            lbl.AutoSize = true;
            tonQuy.pnlHeader.Controls.Add(lue);
            lue.Location = new Point(lbl.Right + 20, (tonQuy.pnlHeader.Height - lue.Height) / 2);

            lue.CustomLoadData += new CustomLoadDataHandler(lue_CustomLoadData);
            lue.LoadData(Tables.DCUAHANG.ToString());
            lue.OnEditValueChanged += new No1ControlChangedHandler(lue_OnEditValueChanged);

            //tạo thêm cây bên tay trái
            tv = new TreeView();
            tv.ImageList = Config.ImgList;
            allNode = tv.Nodes.Add("Tất cả");
            allNode.Name = "TATCA";
            allNode.ImageIndex = Config.AllImageIndex;
            allNode.SelectedImageIndex = Config.AllImageIndex;

            tienMatNode = new TreeNode("Tiền mặt");
            int folderIndex = tv.ImageList.Images.Count - 2;
            tienMatNode.ImageIndex = folderIndex;
            tienMatNode.SelectedImageIndex = folderIndex;
            tienMatNode.Name = "TIENMAT";
            allNode.Nodes.Add(tienMatNode);

            quetTheNode = new TreeNode("Quẹt thẻ");
            quetTheNode.SelectedImageIndex = folderIndex;
            quetTheNode.ImageIndex = folderIndex;
            allNode.Nodes.Add(quetTheNode);

            nganHangNode = new TreeNode("Ngân hàng");
            nganHangNode.Name = "NGANHANG";
            nganHangNode.SelectedImageIndex = folderIndex;
            nganHangNode.ImageIndex = folderIndex;
            allNode.Nodes.Add(nganHangNode);
            //lấy ra danh sách ngân hàng
            DataTable dt = Config.Db.GetTable("SELECT ID, NAME, SIMAGEID FROM DTAIKHOANNGANHANG WHERE STATUS = 30 ORDER BY NAME");
            foreach (DataRow r in dt.Rows)
            {
                TreeNode node = new TreeNode(r["NAME"].ToString());
                node.Name = r["ID"].ToString();
                int imgIndex = Config.ImageIDToIndex(r["SIMAGEID"].ToString());
                node.SelectedImageIndex = imgIndex;
                node.ImageIndex = imgIndex;
                nganHangNode.Nodes.Add(node);
            }
            tv.ExpandAll();
            tv.ItemHeight = 22;
            tv.SelectedNode = allNode;
            tv.AfterSelect += new TreeViewEventHandler(tv_AfterSelect);

            //tạo ra splitcontainer
            split = new KryptonSplitContainer();
            split.Dock = DockStyle.Fill;
            tv.Dock = DockStyle.Fill;
            split.Panel1.Controls.Add(tv);
            split.Panel2.Controls.Add(tonQuy.grMain);
            split.Panel2.Controls.Add(tonQuy.kryptonPanel3);
            tonQuy.Controls.Add(split);
            split.BringToFront();
            split.FixedPanel = FixedPanel.Panel1;
            tonQuy.Load += new EventHandler(tonQuy_Load);
        }

        void lue_CustomLoadData(object sender, CustomLoadDataArgs e)
        {
            if (!DbConfig.IsAdmin)
            {
                e.Where = "ID IN (SELECT DCUAHANGID FROM TNGUOIDUNGTHEOCUAHANG WHERE SUSERID='" + DbConfig.UserID + "')";
            }
        }

        void tv_AfterSelect(object sender, TreeViewEventArgs e)
        {
            tonQuy.LoadData();
        }

        void lue_OnEditValueChanged(object sender, object value)
        {
            tonQuy.LoadData();
        }

        void tonQuy_CustomLoadData(FbCommand cmd)
        {
            string commandText = cmd.CommandText;
            string whereThuChi = "";
            string whereDonHang = "";
            string selectField = "";
            if (lue.StringValue.Length > 0)
            {
                whereThuChi = "DCUAHANGID = '" + lue.StringValue + "'";
                whereDonHang = "(DCUAHANGID = '" + lue.StringValue + "' OR (SELECT DCUAHANGID FROM DKHOHANG WHERE ID = COALESCE(DKHONHAPID, DKHOXUATID)) = '" + lue.StringValue + "')";
            }

            if (tv.SelectedNode != allNode)
            {
                if (tv.SelectedNode == tienMatNode)
                {
                    if (whereThuChi.Length > 0) whereThuChi += " AND ";
                    whereThuChi += "CHUYENKHOAN = 0";

                    if (whereDonHang.Length > 0) whereDonHang += " AND ";
                    whereDonHang += "COALESCE(TIENMAT, 0) <> 0";

                    selectField = "COALESCE(TIENMAT, 0)";
                }
                else if (tv.SelectedNode == quetTheNode)
                {
                    if (whereThuChi.Length > 0) whereThuChi += " AND ";
                    whereThuChi += "0 = 1";

                    if (whereDonHang.Length > 0) whereDonHang += " AND ";
                    whereDonHang += "COALESCE(THE, 0) <> 0";

                    selectField = "COALESCE(THE, 0)";
                }
                else if (tv.SelectedNode == nganHangNode)
                {
                    if (whereThuChi.Length > 0) whereThuChi += " AND ";
                    whereThuChi += "CHUYENKHOAN = 30";

                    if (whereDonHang.Length > 0) whereDonHang += " AND ";
                    whereDonHang += "COALESCE(CHUYENKHOAN, 0) <> 0";

                    selectField = "COALESCE(CHUYENKHOAN, 0)";
                }
                else
                {
                    if (whereThuChi.Length > 0) whereThuChi += " AND ";
                    whereThuChi += "CHUYENKHOAN = 30 AND DTAIKHOANNGANHANGID = '" + tv.SelectedNode.Name + "'";

                    if (whereDonHang.Length > 0) whereDonHang += " AND ";
                    whereDonHang += "DTAIKHOANNGANHANGID = '" + tv.SelectedNode.Name + "'";

                    selectField = "COALESCE(CHUYENKHOAN, 0)";
                }
            }

            if (whereThuChi.Length > 0)
            {
                commandText = commandText.Replace(" UNION ALL SELECT TDONHANG.ID,", " WHERE " + whereThuChi + " UNION ALL SELECT TDONHANG.ID,");
            }

            if (whereDonHang.Length > 0)
            {
                commandText = commandText.Replace("(TDONHANG.TIENTHANHTOAN <>0)", "(TDONHANG.TIENTHANHTOAN <>0) AND " + whereDonHang);
            }

            if (selectField.Length > 0)
            {
                commandText = commandText.Replace("COALESCE(TIENMAT, 0) + COALESCE(CHUYENKHOAN, 0) + COALESCE(THE, 0)", selectField);
            }

            cmd.CommandText = commandText;
        }

        void tonQuy_Load(object sender, EventArgs e)
        {
            split.SplitterDistance = 200;
        }
    }
}
