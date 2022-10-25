using System;
using System.Text;
using System.Xml;
using System.Collections;

using Microsoft.SharePoint;

namespace NextLabs.SPSecurityEnforcer.wehInstaller
{
	class ConfigManager
	{

        private XmlDocument _config;
        private IEnumerator _componentEnum;
        private XmlNode _currentNode;

		internal ConfigManager(string strConfigPath)
		{
			_config = new XmlDocument();
			_config.Load(strConfigPath);
            XmlElement root = _config.DocumentElement;
            XmlNodeList elemList = root.GetElementsByTagName("Component");
            _componentEnum = root.GetEnumerator();  
		}

        internal bool GetNextComponent()
        {
            try
            {
                if (_componentEnum.MoveNext())
                {
                    _currentNode = (XmlNode)_componentEnum.Current;
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

		internal string GetName()
		{
            return GetValue("Name");
		}

		internal string GetAssembly()
		{
            return GetValue("Assembly");
		}

		internal string GetClass()
		{
			return GetValue("Class");
		}

		internal SPEventReceiverType GetEventReceiverType()
		{			
			return (SPEventReceiverType) Convert.ToInt32(GetValue("Type"));
		}

		internal int GetSequence()
		{
			return Convert.ToInt32(GetValue("Sequence"));
		}

		private string GetValue(string strXPath)
		{
			return _currentNode.SelectSingleNode(strXPath).InnerText;
		}

		public XmlDocument Config
		{
			get 
			{ 
				return _config; 
			}			
		}
	}
}