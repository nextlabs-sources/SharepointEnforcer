using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.IO;
using System.Web;
using NextLabs.Common;
using Microsoft.SharePoint;
namespace NextLabs.HttpEnforcer
{
    public class SPE_CLIENT_SVC_Module : SPEModuleBase
    {

        private int ListContainAnyWord(String[] Listwords, String Keyword)
        {
            for (int i = 0; i < Listwords.Length; i++)
            {
                if (Listwords[i].IndexOf(Keyword) != -1)
                {
                    return i;
                }
            }
            return -1;
        }

        public override bool DoSPEProcess()
        {
            SPEEvalAttr _SPEEvalAttr = SPEEvalAttrs.Current();          
            String _listName = null;
            String _listDesc = null;
            String _listType = null;
            bool _ItemQuery = false;
            String _RelativeUrl = null;
            //String _objectPath = null;
            List<string> _objectPathList = new List<string>();
            XmlDocument _inputdoc = new XmlDocument();
            Stream _inputStream = m_Request.InputStream;
            byte[] ContentBuffer = new byte[_inputStream.Length + 1];
            //int _readlen = 0;
            long _oldPos = _inputStream.Seek(0, SeekOrigin.Current);
            //_readlen = _inputStream.Read(ContentBuffer, 0, (int)_inputStream.Length);
            _inputStream.Seek(_oldPos, SeekOrigin.Begin);
           // String _strContent = HttpUtility.UrlDecode(ContentBuffer, m_Request.ContentEncoding);
            String _strContent = Globals.UrlDecode(Encoding.UTF8.GetString(ContentBuffer));
            byte[] InputBuffer = Encoding.Default.GetBytes(_strContent); ;
            using (Stream InputStream = new MemoryStream(InputBuffer))
            {
                _inputdoc.Load(InputStream);
                byte[] _InputBuffer = Encoding.Default.GetBytes(_inputdoc.InnerXml);

                using (Stream _InputStream = new MemoryStream(_InputBuffer))
                {
                    XmlTextReader reader = new XmlTextReader(_InputStream);
                    while (reader.Read())
                    {
                        if (reader.NodeType == XmlNodeType.Element)
                        {
                            if (reader.Name.Equals("Query", StringComparison.OrdinalIgnoreCase))
                            {
                                while (reader.MoveToNextAttribute())
                                {
                                    if (reader.Name.Equals("Id", StringComparison.OrdinalIgnoreCase))
                                    {
                                        _SPEEvalAttr.Action = "LIST QUERY";
                                        break;
                                    }
                                }
                            }
                            if (reader.Name.Equals("Identity", StringComparison.OrdinalIgnoreCase))
                            {
                                while (reader.MoveToNextAttribute())
                                {
                                    if (reader.Name.Equals("Name", StringComparison.OrdinalIgnoreCase))
                                    {
                                        _objectPathList.Add(reader.Value);
                                        break;
                                    }
                                }
                            }
                            else if (reader.Name.Equals("Method", StringComparison.OrdinalIgnoreCase))
                            {
                                while (reader.MoveToNextAttribute())
                                {
                                    if (reader.Name.Equals("Name", StringComparison.OrdinalIgnoreCase))
                                    {
                                        if (reader.Value.Equals("Add", StringComparison.OrdinalIgnoreCase))
                                        {
                                            _SPEEvalAttr.Action = "LIST CREATE";
                                            break;
                                        }
                                        else if (reader.Value.Equals("Recycle", StringComparison.OrdinalIgnoreCase))
                                        {
                                            _SPEEvalAttr.Action = "LIST Recycle";
                                            break;
                                        }
                                        else if (reader.Value.Equals("Update", StringComparison.OrdinalIgnoreCase))
                                        {
                                            _SPEEvalAttr.Action = "LIST Update";
                                            break;
                                        }
                                        else if (reader.Value.Equals("DeleteObject", StringComparison.OrdinalIgnoreCase))
                                        {
                                            _SPEEvalAttr.Action = "LIST Update";
                                            break;
                                        }
                                        else if (reader.Value.Equals("GetFileByServerRelativeUrl", StringComparison.OrdinalIgnoreCase))
                                        {
                                            _SPEEvalAttr.Action = "LIST QUERY";
                                            _ItemQuery = true;
                                            break;
                                        }

                                    }
                                }
                            }
                            else if (reader.Name.Equals("Parameter", StringComparison.OrdinalIgnoreCase) && _ItemQuery)
                            {
                                if (reader.Read() && reader.NodeType == XmlNodeType.Text)
                                {
                                    _RelativeUrl = reader.Value;
                                }
                            }
                            else if (reader.Name.Equals("Property", StringComparison.OrdinalIgnoreCase))
                            {
                                reader.MoveToNextAttribute();//Fix bug 9838
                                {
                                    if (reader.Name.Equals("Name", StringComparison.OrdinalIgnoreCase))
                                    {
                                        if (reader.Value.Equals("Title", StringComparison.OrdinalIgnoreCase))
                                        {
                                            if (reader.Read() && reader.NodeType == XmlNodeType.Text)
                                            {
                                                _listName = reader.Value;
                                            }
                                        }
                                        else if (reader.Value.Equals("Description", StringComparison.OrdinalIgnoreCase))
                                        {
                                            if (reader.Read() && reader.NodeType == XmlNodeType.Text)
                                            {
                                                _listDesc = reader.Value;
                                            }
                                        }
                                        else if (reader.Value.Equals("TemplateType", StringComparison.OrdinalIgnoreCase))
                                        {
                                            if (reader.Read() && reader.NodeType == XmlNodeType.Text)
                                            {
                                                _listType = reader.Value;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                    if (_SPEEvalAttr.Action.Equals("LIST CREATE", StringComparison.OrdinalIgnoreCase))
                    {
                        _SPEEvalAttr.PolicyAction = CETYPE.CEAction.Write;
                        if (_listType != null && Globals.IsLibraryTemplateID(_listType))
                        {
                            _SPEEvalAttr.ObjEvalUrl = _SPEEvalAttr.WebUrl + "/" + _listName;
                            _SPEEvalAttr.ObjSubtype = CETYPE.CEAttrVal.CE_ATTR_SP_SUBTYPE_VAL_LIBRARY;
                        }
                        else
                        {
                            _SPEEvalAttr.ObjEvalUrl = _SPEEvalAttr.WebUrl + "/" + "Lists" + "/" + _listName;
                            _SPEEvalAttr.ObjSubtype = CETYPE.CEAttrVal.CE_ATTR_SP_SUBTYPE_VAL_LIST;
                        }
                        _SPEEvalAttr.ObjName = _listName;
                        _SPEEvalAttr.ObjTitle = _listName;
                        _SPEEvalAttr.ObjDesc = _listDesc;
                        _SPEEvalAttr.ObjType = CETYPE.CEAttrVal.CE_ATTR_SP_TYPE_VAL_PORTLET;
                    }
                    else if (_SPEEvalAttr.Action.Equals("LIST QUERY", StringComparison.OrdinalIgnoreCase))
                    {
                        _SPEEvalAttr.PolicyAction = CETYPE.CEAction.Read;
                    }
                    else if (_SPEEvalAttr.Action.Equals("LIST Recycle", StringComparison.OrdinalIgnoreCase))
                    {
                        _SPEEvalAttr.PolicyAction = CETYPE.CEAction.Delete;
                    }
                    else if (_SPEEvalAttr.Action.Equals("LIST Update", StringComparison.OrdinalIgnoreCase))
                    {
                        _SPEEvalAttr.PolicyAction = CETYPE.CEAction.Write;
                    }
                    String[] _objectPaths = _objectPathList.ToArray();
                    if (_SPEEvalAttr.Action.Equals("LIST QUERY", StringComparison.OrdinalIgnoreCase)
                        || _SPEEvalAttr.Action.Equals("LIST Recycle", StringComparison.OrdinalIgnoreCase)
                        || _SPEEvalAttr.Action.Equals("LIST Update", StringComparison.OrdinalIgnoreCase))
                    {
                        int fileindex = ListContainAnyWord(_objectPaths, "file:");
                        int listindex = ListContainAnyWord(_objectPaths, "list:");
                        if (fileindex != -1 || _ItemQuery)
                        {
                            if (_ItemQuery)
                            {
                                _SPEEvalAttr.ObjEvalUrl = "http://" + m_Request.Url.Host + _RelativeUrl;
                            }
                            else
                            {
                                int index = _objectPaths[fileindex].LastIndexOf("file:");
                                String _objectID = _objectPaths[fileindex].Substring(index + 5);
                                _SPEEvalAttr.ObjEvalUrl = "http://" + m_Request.Url.Host + _objectID;
                            }
                            try
                            {
                                _SPEEvalAttr.ItemObj = (SPListItem)_SPEEvalAttr.WebObj.GetObject(_SPEEvalAttr.ObjEvalUrl);
                            }
                            catch
                            {
                            }
                            if (_SPEEvalAttr.ItemObj != null)
                            {
                                SPEEvalAttrHepler.SetObjEvalAttr(_SPEEvalAttr.ItemObj, _SPEEvalAttr);
                            }
                        }
                        else if (listindex != -1)
                        {
                            int index = _objectPaths[listindex].LastIndexOf("list:");
                            String _objectID = _objectPaths[listindex].Substring(index + 5, 36);
                            _SPEEvalAttr.ListObj = (SPList)Utilities.GetCachedSPContent(_SPEEvalAttr.WebObj, _objectID, Utilities.SPUrlListID);
                            if (_SPEEvalAttr.ListObj != null)
                            {
                                SPEEvalAttrHepler.SetObjEvalAttr(_SPEEvalAttr.ListObj, _SPEEvalAttr);
                            }
                        }
                        else
                        {
                            if (_SPEEvalAttr.WebObj != null)
                                SPEEvalAttrHepler.SetObjEvalAttr(_SPEEvalAttr.WebObj, _SPEEvalAttr);
                        }
                    }
                }
            }
            return false;
        }
    }

    public class SPE_CELLSTORAGE_SVC_Module : SPEModuleBase
    {

        public override bool DoSPEProcess()
        {
            SPEEvalAttr _SPEEvalAttr = SPEEvalAttrs.Current();  
            XmlDocument _inputdoc = new XmlDocument();
            Stream _inputStream = m_Request.InputStream;
            byte[] ContentBuffer = new byte[_inputStream.Length + 1];
            int _readlen = 0;
            long _oldPos = _inputStream.Seek(0, SeekOrigin.Current);
            _readlen = _inputStream.Read(ContentBuffer, 0, (int)_inputStream.Length);
            _inputStream.Seek(_oldPos, SeekOrigin.Begin);
            String _strContent = Globals.UrlDecode(Encoding.UTF8.GetString(ContentBuffer));
            _strContent = _strContent.Substring(_strContent.IndexOf("<s:Envelope xmlns"));
            string end = "</s:Envelope>\r\n";
            _strContent = _strContent.Substring(0, _strContent.IndexOf(end) + end.Length);
            byte[] InputBuffer = Encoding.Default.GetBytes(_strContent); ;
            using (Stream InputStream = new MemoryStream(InputBuffer))
            {
                _inputdoc.Load(InputStream);
                byte[] _InputBuffer = Encoding.Default.GetBytes(_inputdoc.InnerXml);
                using (Stream _InputStream = new MemoryStream(_InputBuffer))
                {
                    XmlTextReader reader = new XmlTextReader(_InputStream);
                    while (reader.Read())
                    {
                        if (reader.NodeType == XmlNodeType.Element)
                        {
                            if (reader.Name.Equals("Request", StringComparison.OrdinalIgnoreCase))
                            {
                                while (reader.MoveToNextAttribute())
                                {
                                    if (reader.Name.Equals("Url", StringComparison.OrdinalIgnoreCase))
                                    {
                                        _SPEEvalAttr.ObjEvalUrl = reader.Value;
                                        break;
                                    }
                                }
                            }
                            if (reader.Name.Equals("SubRequestData", StringComparison.OrdinalIgnoreCase))
                            {
                                while (reader.MoveToNextAttribute())
                                {
                                    //word 2010 ppt 2010
                                    if (reader.Name.Equals("CoauthRequestType", StringComparison.OrdinalIgnoreCase))
                                    {
                                        if (reader.Value.Equals("JoinCoauthoring", StringComparison.OrdinalIgnoreCase))
                                        {
                                            _SPEEvalAttr.Action = "DOC READ";
                                            _SPEEvalAttr.PolicyAction = CETYPE.CEAction.Read;
                                            break;
                                        }
                                        else if (reader.Value.Equals("RefreshCoauthoring", StringComparison.OrdinalIgnoreCase))
                                        {
                                            _SPEEvalAttr.Action = "DOC SYNC";
                                            _SPEEvalAttr.PolicyAction = CETYPE.CEAction.Write;
                                            break;
                                        }
                                        break;
                                    }
                                    //excel 2010
                                    else if (reader.Name.Equals("ExclusiveLockRequestType", StringComparison.OrdinalIgnoreCase))
                                    {
                                        if (reader.Value.Equals("GetLock", StringComparison.OrdinalIgnoreCase))
                                        {
                                            _SPEEvalAttr.Action = "DOC READ";
                                            _SPEEvalAttr.PolicyAction = CETYPE.CEAction.Read;
                                            break;
                                        }
                                        else if (reader.Value.Equals("RefreshLock", StringComparison.OrdinalIgnoreCase))
                                        {
                                            _SPEEvalAttr.Action = "DOC SYNC";
                                            _SPEEvalAttr.PolicyAction = CETYPE.CEAction.Write;
                                            break;
                                        }
                                        break;
                                    }
                                    //for readonly mode
                                    else if (reader.Name.Equals("GetFileProps", StringComparison.OrdinalIgnoreCase))
                                    {
                                        if (reader.Value.Equals("true", StringComparison.OrdinalIgnoreCase))
                                        {
                                            _SPEEvalAttr.Action = "DOC READ";
                                            _SPEEvalAttr.PolicyAction = CETYPE.CEAction.Read;
                                            break;
                                        }
                                        break;
                                    }
                                }
                            }
                        }
                    }
                    if (_SPEEvalAttr.Action.Equals("DOC READ", StringComparison.OrdinalIgnoreCase)
                        || _SPEEvalAttr.Action.Equals("DOC SYNC", StringComparison.OrdinalIgnoreCase))
                    {
                        try
                        {
                            _SPEEvalAttr.ItemObj = (SPListItem)_SPEEvalAttr.WebObj.GetObject(_SPEEvalAttr.ObjEvalUrl);
                        }
                        catch (Exception exp)
                        {
                            if (exp.Message.Contains("Unable to cast object of type 'Microsoft.SharePoint.SPFile' to type"))
                                _SPEEvalAttr.ObjSubtype = CETYPE.CEAttrVal.CE_ATTR_SP_SUBTYPE_VAL_LIST_ITEM;
                        }
                        if (_SPEEvalAttr.ItemObj != null)
                        {
                            SPEEvalAttrHepler.SetObjEvalAttr(_SPEEvalAttr.ItemObj, _SPEEvalAttr);
                        }
                    }
                }
            }
            return false;
        }
        
    }
}


