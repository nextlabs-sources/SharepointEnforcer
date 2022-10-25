using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.SharePoint;
using System.Security.Principal;
namespace NextLabs.Common
{
    public enum NoiseLevel
    {
        Min = 0,
        System = 1,
        Application = 2,
        UserAction = 3,
        Max = 4,
    }

    public class EvaluatorContext
    {
        public const string CacheHintPropertyName = "ce::request_cache_hint";
        public const string ContentAnalysisHintName = "ce::nativeresourcename";

        private SPWeb m_Web;
        private CETYPE.CEAction m_ActionType;
        private string m_ActionStr;
        private NoiseLevel m_NoiseLevel;
        private string m_RemoteAddress;
        private IPrincipal m_PrincipalUser;
        private string m_UserName;
        private string m_UserSid;
        private string m_SrcName;
        private string m_TargetName;
        private List<KeyValuePair<string, string>> m_SrcAttributes;
        private List<KeyValuePair<string, string>> m_TargetAttributes;

        // Output
        private string m_PolicyName;
        private string m_PolicyMessage;
        private int m_CacheHint;
        private bool m_bAllow;
        private string m_FailedReason;

        public SPWeb Web
        {
            get { return m_Web; }
            set { m_Web = value; }
        }

        public CETYPE.CEAction ActionType
        {
            get { return m_ActionType; }
            set { m_ActionType = value; }
        }

        public string ActionStr
        {
            get { return m_ActionStr; }
            set { m_ActionStr = value; }
        }

        public NoiseLevel NoiseLevel
        {
            get { return m_NoiseLevel; }
            set { m_NoiseLevel = value; }
        }

        public string RemoteAddress
        {
            get { return m_RemoteAddress; }
            set { m_RemoteAddress = value; }
        }

        public IPrincipal PrincipalUser
        {
            get { return m_PrincipalUser; }
            set { m_PrincipalUser = value; }
        }

        public string UserName
        {
            get { return m_UserName; }
            set { m_UserName = value; }
        }

        public string UserSid
        {
            get { return m_UserSid; }
            set { m_UserSid = value; }
        }

        public string SrcName
        {
            get { return m_SrcName; }
            set { m_SrcName = value; }
        }

        public string TargetName
        {
            get { return m_TargetName; }
            set { m_TargetName = value; }
        }

        public List<KeyValuePair<string, string>> SrcAttributes
        {
            get { return m_SrcAttributes; }
            set { m_SrcAttributes = value; }
        }

        public List<KeyValuePair<string, string>> TargetAttributes
        {
            get { return m_TargetAttributes; }
            set { m_TargetAttributes = value; }
        }

        public string PolicyName
        {
            get { return m_PolicyName; }
            set { m_PolicyName = value; }
        }

        public string PolicyMessage
        {
            get { return m_PolicyMessage; }
            set { m_PolicyMessage = value; }
        }

        public int CacheHint
        {
            get { return m_CacheHint; }
            set { m_CacheHint = value; }
        }

        public bool Allow
        {
            get { return m_bAllow; }
            set { m_bAllow = value; }
        }

        public string FailedReason
        {
            get { return m_FailedReason; }
            set { m_FailedReason = value; }
        }

        public EvaluatorContext()
        {
            m_Web = null;
            m_PrincipalUser = null;
            m_ActionType = CETYPE.CEAction.Unknown;
            m_NoiseLevel = NoiseLevel.UserAction;
            m_SrcAttributes = new List<KeyValuePair<string, string>>();
            m_TargetAttributes = new List<KeyValuePair<string, string>>();

            m_CacheHint = 0;
            m_bAllow = true;
        }
    }
}
