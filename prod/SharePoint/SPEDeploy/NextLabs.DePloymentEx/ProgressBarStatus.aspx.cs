using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using Microsoft.SharePoint.WebControls;
using NextLabs.Diagnostic;

namespace NextLabs.Deployment
{
    public partial class FeatureStatus : LayoutsPageBase
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (Request.QueryString["webAppName"] != null)
            {
                Response.Write(Progress.GetLastLog(Request.QueryString["webAppName"].ToString()));
            }
            if (Request.QueryString["AllLogWebAppName"] != null)
            {
                string webAppName = Request.QueryString["AllLogWebAppName"].ToString();
                List<string> allLog = Progress.GetAllLog(webAppName);
                Page.Response.AppendHeader("Content-Disposition", "attachment;filename=" + webAppName + ".log");
                Response.ContentType = "text/plain";
                string strLog = string.Join("\r\n", allLog.ToArray());
                Response.Write(strLog);
            }
        }
    }
}