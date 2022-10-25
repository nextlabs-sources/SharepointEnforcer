using System;
using System.Collections.Generic;
using System.Text;

namespace NextLabs.Common
{
    public class Obligation
    {
        private string m_Name;
        private string m_Policy;
        private Dictionary<string, string> m_Attributes;

        public string Name
        {
            get { return m_Name; }
            set { m_Name = value; }
        }

        public string Policy
        {
            get { return m_Policy; }
            set { m_Policy = value; }
        }

        public Dictionary<string, string> Attributes
        {
            get { return m_Attributes; }
        }

        public Obligation()
        {
            m_Attributes = new Dictionary<string, string>();
        }

        public Obligation(string name, string policy)
        {
            m_Name = name;
            m_Policy = policy;
            m_Attributes = new Dictionary<string, string>();
        }

        public void AddAttribute(string key, string value)
        {
            m_Attributes.Add(key, value);
        }

        public string GetAttribute(string key)
        {
            try
            {
				if (!string.IsNullOrEmpty(key) && m_Attributes.ContainsKey(key))
	            {
	                return m_Attributes[key];
	            }
            }
            catch
            {
            }
			return "";
        }
    }
}
