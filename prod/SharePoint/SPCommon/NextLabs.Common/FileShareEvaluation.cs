using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Web;
using NextLabs.CSCInvoke;

namespace NextLabs.Common
{
    public class FileShareEvaluation : IEvaluation
    {
        protected CETYPE.CEAction m_Action;
        protected String m_HostAddress;
        protected String m_ModuleName;
        protected String m_FilePath;
        protected String m_UserName;
        protected String m_UserSid;

        protected String m_PolicyName;
        protected String m_PolicyMessage;

        public FileShareEvaluation(CETYPE.CEAction action, String path, 
            String host, String user, String sid, String module)
        {
            m_Action = action;
            m_FilePath = path;
            m_HostAddress = host;
            m_ModuleName = module;
            m_UserName = user;
            m_UserSid = sid;

            m_PolicyName = "";
            m_PolicyMessage = "";
        }

        public string ReConstructUrl()
        {
            m_FilePath = Globals.UrlDecode(m_FilePath.ToLower());
            if (m_FilePath.StartsWith("file://"))
            {
                m_FilePath = m_FilePath.Replace("file://", "\\\\");
                m_FilePath = m_FilePath.Replace("/", "\\");
            }

            string[] pathItems = m_FilePath.Split(new string[] {"\\"}, StringSplitOptions.RemoveEmptyEntries);
            string serverName = pathItems[0];
            IPHostEntry hostEntry = Dns.GetHostEntry(serverName);
            string longServerName = hostEntry.HostName;
            m_FilePath = m_FilePath.Replace(serverName, longServerName);

            return m_FilePath;
        }

        public bool Run()
        {
            bool bResult = true;

            ReConstructUrl();

            EvaluatorContext evaContext = new EvaluatorContext();
            evaContext.ActionType = m_Action;
            evaContext.NoiseLevel = NoiseLevel.UserAction;
            evaContext.Web = null;
            evaContext.RemoteAddress = m_HostAddress;
            evaContext.UserName = m_UserName;
            evaContext.UserSid = m_UserSid;
            evaContext.SrcName = m_FilePath;
            evaContext.TargetName = "";

            KeyValuePair<string, string> keyVaule = new KeyValuePair<string, string>(
                            CETYPE.CEAttrKey.CE_ATTR_MODIFIED_DATE, 
                            EvaluatorProperties.ConvertDataTime2Str(DateTime.Now));
            evaContext.SrcAttributes.Add(keyVaule);

            Evaluator evaluator = new Evaluator();
            bResult = evaluator.CheckFile(ref evaContext);
            return bResult;
        }
    }
}
