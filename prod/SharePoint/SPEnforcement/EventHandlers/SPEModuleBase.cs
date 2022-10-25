using System;
using System.Collections.Generic;
using System.Text;
using System.Web;

namespace NextLabs.HttpEnforcer
{
    public class SPEModuleBase
    {
        public HttpRequest m_Request;        
        public void Init(HttpRequest _Request)
        {
            m_Request = _Request;
        }
        public virtual bool DoSPEProcess()
        {
            return false;
        }
    }
}