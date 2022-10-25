using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using Microsoft.SharePoint;
using Microsoft.SharePoint.ApplicationPages;
using System.Diagnostics;

namespace NextLabs.Tool
{
    //class NxtIrmListSettings : IrmListSettings
    //{
    //    public NxtIrmListSettings(SPWeb web, SPList list)
    //    {
    //        base.spWeb = web;
    //        base.spList = list;
    //        base.ChkExpire = new CheckBox();
    //        base.ChkOffline = new CheckBox();
    //        base.ChkPrint = new CheckBox();
    //        base.ChkProtect = new CheckBox();
    //        base.ChkReject = new CheckBox();
    //        base.ChkVBA = new CheckBox();

    //        base.TxtDescription = new TextBox();
    //        base.TxtOffline = new TextBox();
    //        base.TxtTitle = new TextBox();
    //    }

    //    public void OverwritedBtnSubmit_Click()
    //    {
    //        base.ChkProtect.Checked = true;
    //        base.ChkVBA.Checked = true;
    //        base.TxtTitle.Text = "Nextlabs";
    //        base.TxtDescription.Text = "Nextlabs";

    //        Console.WriteLine("aaaaaaaaaa");

    //        base.Validate();

    //        Console.WriteLine("bbbbbbbbbb");

    //        object _obj = new object();
    //        EventArgs args = new EventArgs();
    //        base.BtnSubmit_Click(_obj, args);
    //        Console.WriteLine("cccccccccc");
    //    }
    //}
}
