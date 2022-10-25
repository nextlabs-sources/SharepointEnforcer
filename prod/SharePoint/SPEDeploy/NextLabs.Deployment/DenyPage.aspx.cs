using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.UI.WebControls;
using Microsoft.SharePoint.WebControls;
using Microsoft.SharePoint.Utilities;
using Microsoft.SharePoint;
using Microsoft.SharePoint.Administration;
using Microsoft.Win32;
using Microsoft.Office.Server.Search.Administration;
using System.ServiceProcess;
using NextLabs.Common;

namespace NextLabs.Deployment
{
    public partial class DenyPage : LayoutsPageBase
    {
        public string loginName = String.Empty;
        public string policyMessage = String.Empty;
        public string resouceID = String.Empty;
        protected void Page_Load(object sender, EventArgs e)
        {
          
            loginName = System.Web.HttpUtility.UrlDecode(Request.QueryString["loginName"]);
            policyMessage = System.Web.HttpUtility.UrlDecode(Request.QueryString["policyMessage"]);
            resouceID = System.Web.HttpUtility.UrlDecode(Request.QueryString["resouceID"]);

            if(!String.IsNullOrEmpty(loginName))
            {
                int index = loginName.IndexOf("\\");
                if(index != -1)
                {
                    loginName = loginName.Substring(index+1);
                }
            }

            if (String.IsNullOrEmpty(policyMessage))
            {
                policyMessage = "You are denied to the access the resource.";
            }
        }
    }
}