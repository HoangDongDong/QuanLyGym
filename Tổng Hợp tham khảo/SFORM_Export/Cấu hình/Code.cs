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
    public partial class CauHinh: ISystemConfigPage
    {
        public override void OnLoad(ComponentFactory.Krypton.Navigator.KryptonPage page)
        {
            txtGhiChu50.Text = SystemConfig.GhiChu80;
            txtGhiChuA5.Text = SystemConfig.GhiChuA5;

            txtGhiChu50.OnEditValueChanged += new No1ControlChangedHandler(txtGhiChu50_OnEditValueChanged);
            txtGhiChuA5.OnEditValueChanged += new No1ControlChangedHandler(txtGhiChu50_OnEditValueChanged);
        }

        void txtGhiChu50_OnEditValueChanged(object sender, object value)
        {
            SetOkButtonEnabled();
        }

        public override void DoSave(CancelEventArgs ce)
        {            
            SystemConfig.GhiChu80 = txtGhiChu50.Text;
            SystemConfig.GhiChuA5 = txtGhiChuA5.Text;
        }
    }
}
