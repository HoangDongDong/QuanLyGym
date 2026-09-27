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
using zkemkeeper;

namespace No1Run
{
    public partial class DongBoKhachHang
    {
        public void SetData(DataTable dt)
        {
            grid.DataSource = dt;
        }

        bool DaLayVanTay = false;
        public void btnAdd_Click(object sender, EventArgs e)
        {
            if (!DaLayVanTay && SystemConfig.ThietBiSuDung == (int)ThietBiSuDung.CuaTu)
            {
                DangThucHien frmTaiVanTay = (DangThucHien)Config.CreateForm(Forms.DangThucHien);
                frmTaiVanTay.PostData(grid.DataSource as DataTable);
                frmTaiVanTay.DoWork += new DoWorkEventHandler(frmTaiVanTay_DoWork);
                frmTaiVanTay.RunWorkerCompleted += new RunWorkerCompletedEventHandler(frmTaiVanTay_RunWorkerCompleted);
                frmTaiVanTay.No1Form1.ShowDialog();
            }

            if (grid.SelectedRow != null)
            {
                DataRow SelectedRow = grid.SelectedRow;
                if (grid.SelectedID.Length == 0)
                {
                    IAddEditForm obj = (IAddEditForm)Config.CreateAeForm(Tables.DKHACHHANG, 0, string.Empty);
                    obj.ReLoad("");
                    obj.SetValue(DKHACHHANGInfo.MAKHACH.ToString(), SelectedRow["MAKHACH"].ToString());
                    obj.SetValue(DKHACHHANGInfo.MAVANTAY.ToString(), SelectedRow["MAVANTAY"].ToString());

                    (obj as Form).ShowDialog();
                    if (obj is ISaveStateSupport)
                    {
                        if (((ISaveStateSupport)obj).IsDataSaved())
                        {
                            DKHACHHANGRow row = new DKHACHHANGRow(obj.GetID());

                            SelectedRow["ID"] = row.ID;
                            SelectedRow["NAME"] = row.NAME;
                            SelectedRow["DIACHI"] = row.DIACHI;
                            SelectedRow["DIENTHOAI"] = row.DIENTHOAI;

                            if (row.MAVANTAY.Length > 0)
                            {
                                //Đẩy lên các thiết bị còn lại
                                string msg = "";
                                foreach (ThietBiInfo thietBi in Shared.QuanLyThietBi.lstThietBi)
                                {
                                    if (thietBi.IP != SystemConfig.IpMayVanTayCoDuLieuDangKy)
                                    {
                                        if (thietBi.IMay.IsConnected)
                                        {
                                            if (!thietBi.IMay.CapNhatVanTay(row.MAKHACH, row.MAVANTAY))
                                            {
                                                if (msg.Length > 0) msg += Environment.NewLine;
                                                msg += "Không đẩy được lên thiết bị " + thietBi.IP;
                                            }
                                        }
                                        else
                                        {
                                            if (msg.Length > 0) msg += Environment.NewLine;
                                            msg += "Thiết bị " + thietBi.IP + " chưa kết nối";
                                        }
                                    }
                                }
                                if (msg.Length > 0)
                                    Msg.ShowWarning(msg);
                            }
                            (obj as Form).Close();
                        }
                    }
                }
                else
                    Msg.ShowWarning("Khách hàng này đã được tạo trên hệ thống!");
            }
        }

        void frmTaiVanTay_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            DaLayVanTay = true;
            if (e.Result != null)
                Msg.ShowWarning(e.Result.ToString());
        }

        void frmTaiVanTay_DoWork(object sender, DoWorkEventArgs e)
        {
            DangThucHien f = sender as DangThucHien;
            DataTable dt = e.Argument as DataTable;

            int iMachineNumber = 0;
            string sdwEnrollNumber = "";
            string sName = "";
            string sPassword = "";
            int iPrivilege = 0;
            bool bEnabled = false;

            int idwFingerIndex;
            string sTmpData = "";
            int iTmpLength = 0;
            int iFlag = 0;

            if (SystemConfig.ThietBiSuDung == (int)ThietBiSuDung.CuaTu)
            {
                f.UpdateStatus("Kết nối máy vân tay " + SystemConfig.IpMayVanTayCoDuLieuDangKy + ":" + SystemConfig.CongMayVanTayCoDuLieuDangKy, 0);

                CZKEMClass axCZKEM = new CZKEMClass();
                bool isOk = axCZKEM.Connect_Net(SystemConfig.IpMayVanTayCoDuLieuDangKy, SystemConfig.CongMayVanTayCoDuLieuDangKy);
                if (isOk)
                {
                    f.UpdateStatus("Tải vân tay...", 0);

                    axCZKEM.EnableDevice(iMachineNumber, false);
                    axCZKEM.ReadAllUserID(iMachineNumber);
                    axCZKEM.ReadAllTemplate(iMachineNumber);//read all the users' fingerprint templates to the memory

                    while (axCZKEM.SSR_GetAllUserInfo(iMachineNumber, out sdwEnrollNumber, out sName, out sPassword, out iPrivilege, out bEnabled))//get all the users' information from the memory
                    {
                        DataRow[] rs = dt.Select("MAKHACH='" + sdwEnrollNumber + "'");
                        if (rs.Length > 0)
                        {
                            DKHACHHANGRow khRow = new DKHACHHANGRow(rs[0]);
                            f.UpdateStatus("Tải vân tay " + khRow.MAKHACH + "-" + khRow.NAME, 0);
                            for (idwFingerIndex = 0; idwFingerIndex < 10; idwFingerIndex++)
                            {
                                if (axCZKEM.GetUserTmpExStr(iMachineNumber, sdwEnrollNumber, idwFingerIndex, out iFlag, out sTmpData, out iTmpLength))//get the corresponding templates string and length from the memory
                                {
                                    if (!string.IsNullOrEmpty(sTmpData))
                                    {
                                        khRow.MAVANTAY = sTmpData;
                                        break;
                                    }
                                }
                            }
                        }
                    }

                    axCZKEM.EnableDevice(iMachineNumber, true);
                    axCZKEM.Disconnect();
                }
                else
                    e.Result = "Không tải được vân tay từ thiết bị chưa dữ liệu!";
            }
        }

        public void btnGiaHan_Click(object sender, EventArgs e)
        {
            if (grid.SelectedRow != null && grid.SelectedID.Length > 0)
            {
                IAddEditForm obj = (IAddEditForm)Config.CreateAeForm(Tables.TGIAHANTHE, 0, string.Empty);
                obj.ReLoad("");
                obj.SetValue(TGIAHANTHEInfo.DKHACHHANGID.ToString(), grid.SelectedRow["ID"].ToString());
                (obj as Form).ShowDialog();
                if (obj is ISaveStateSupport)
                {
                    if (((ISaveStateSupport)obj).IsDataSaved())
                    {
                        (obj as Form).Close();
                    }
                }
            }
            else
                Msg.ShowWarning("Bạn phải thực hiện tạo khách hàng trước khi gia hạn thẻ!");
        }


		public void grid_SelectionChanged(object sender, EventArgs e)
		{
            btnThem.Visible = grid.SelectedID.Length == 0;
            btnGiaHan.Visible = grid.SelectedID.Length > 0;
		}
    }
}