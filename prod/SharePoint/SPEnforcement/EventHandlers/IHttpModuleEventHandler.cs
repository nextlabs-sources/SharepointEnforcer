using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace NextLabs.SPEnforcer
{
    public interface IHttpModuleEventHandler
    {
        void OnUnlocked(object sender, EventArgs e, HttpContext httpContext);
    }
}
