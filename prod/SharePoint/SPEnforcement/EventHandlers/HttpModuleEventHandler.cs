using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Web;
using Microsoft.SharePoint.WebControls;
using Microsoft.SharePoint;
using NextLabs.Common;

namespace NextLabs.SPEnforcer
{
    class HttpModuleEventHandler : IHttpModuleEventHandler
    {
        void IHttpModuleEventHandler.OnUnlocked(object sender, EventArgs e, HttpContext httpContext)
        {
            //George: Do nothing in this
        }
    }
}
