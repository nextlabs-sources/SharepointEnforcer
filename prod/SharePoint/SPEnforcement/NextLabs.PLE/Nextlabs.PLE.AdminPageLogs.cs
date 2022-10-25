using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using NextLabs.PLE.Log;
using Microsoft.SharePoint;
using Microsoft.SharePoint.WebControls;
using NextLabs.Diagnostic;
using System.Diagnostics;
using NextLabs.Common;

// using obsolete object "Microsoft.SharePoint.SPWeb.Roles" and "Microsoft.SharePoint.SPRole"
#pragma warning disable 618

namespace Nextlabs.PLE.PageModule
{
    class AdminPageLogs
    {
        private enum SiteStyle
        {
            Style2007,
            Style2010,
            Style2013
        };

        SiteStyle _SiteStyle = SiteStyle.Style2010;

        public static bool Is_AdminPages(HttpRequest Request)
        {
            if (Request.HttpMethod == "POST")
            {
                if (Request.FilePath.EndsWith("Aclinv.aspx", StringComparison.OrdinalIgnoreCase))
                {
                    String _ButtonAction = Request.Form[SPEEvalInit._sAclinvPage_PostWords[1]];
                    if (_ButtonAction != null && (_ButtonAction.EndsWith(SPEEvalInit._sAclinvPage_PostWords[2], StringComparison.OrdinalIgnoreCase) || _ButtonAction.EndsWith("btnOK", StringComparison.OrdinalIgnoreCase)))
                    {
                        //this is not a real post page action
                        return true;
                    }
                }
                else if (Request.FilePath.EndsWith("Newgrp.aspx", StringComparison.OrdinalIgnoreCase))
                {
                    String _ButtonAction = Request.Form[SPEEvalInit._sNewgrpPage_PostWords[1]];
                    if (_ButtonAction != null && _ButtonAction.EndsWith(SPEEvalInit._sNewgrpPage_PostWords[2], StringComparison.OrdinalIgnoreCase))
                    {
                        //this is not a real post page action
                        return true;
                    }
                }
                else if (Request.FilePath.EndsWith("People.aspx", StringComparison.OrdinalIgnoreCase))
                {
                    String _ButtonAction = Request.Form[SPEEvalInit._sPeoplePage_PostWords[1]];
                    if (_ButtonAction != null && (_ButtonAction.EndsWith(SPEEvalInit._sPeoplePage_PostWords[2], StringComparison.OrdinalIgnoreCase) || _ButtonAction.EndsWith(SPEEvalInit._sPeoplePage_PostWords[6], StringComparison.OrdinalIgnoreCase)))
                    {
                        //this is not a real post page action
                        return true;
                    }
                }
                else if (Request.FilePath.EndsWith("Editgrp.aspx", StringComparison.OrdinalIgnoreCase))
                {
                    String _ButtonAction = Request.Form[SPEEvalInit._sEditgrpPage_PostWords[1]];
                    if (_ButtonAction != null && (_ButtonAction.EndsWith(SPEEvalInit._sEditgrpPage_PostWords[2], StringComparison.OrdinalIgnoreCase) || _ButtonAction.EndsWith(SPEEvalInit._sEditgrpPage_PostWords[3], StringComparison.OrdinalIgnoreCase)))
                    {
                        //this is not a real post page action
                        return true;
                    }
                }
                else if (Request.FilePath.EndsWith("Permsetup.aspx", StringComparison.OrdinalIgnoreCase))
                {
                    String _ButtonAction = Request.Form[SPEEvalInit._sPermsetupPage_PostWords[1]];
                    if (_ButtonAction != null && _ButtonAction.EndsWith(SPEEvalInit._sPermsetupPage_PostWords[2], StringComparison.OrdinalIgnoreCase))
                    {
                        //this is not a real post page action
                        return true;
                    }
                }
                else if (Request.FilePath.EndsWith("Mngsiteadmin.aspx", StringComparison.OrdinalIgnoreCase))
                {
                    String _ButtonAction = Request.Form[SPEEvalInit._sMngsiteadminPage_PostWords[1]];
                    if (_ButtonAction != null && _ButtonAction.EndsWith(SPEEvalInit._sMngsiteadminPage_PostWords[2], StringComparison.OrdinalIgnoreCase))
                    {
                        //this is not a real post page action
                        return true;
                    }
                }
                else if (Request.FilePath.EndsWith("Editprms.aspx", StringComparison.OrdinalIgnoreCase))
                {
                    String _ButtonAction = Request.Form[SPEEvalInit._sEditprmsPage_PostWords[1]];
                    if (_ButtonAction != null && _ButtonAction.EndsWith(SPEEvalInit._sEditprmsPage_PostWords[2], StringComparison.OrdinalIgnoreCase))
                    {
                        //this is not a real post page action
                        return true;
                    }
                }
                else if (Request.FilePath.EndsWith("User.aspx", StringComparison.OrdinalIgnoreCase))
                {
                    String _ButtonAction = Request.Form[SPEEvalInit._sUserPage_PostWords[1]];
                    if (_ButtonAction != null && _ButtonAction.Equals(SPEEvalInit._sUserPage_PostWords[2], StringComparison.OrdinalIgnoreCase))
                    {
                        //this is not a real post page action
                        return true;
                    }
                }
                else if (Request.FilePath.EndsWith("Addrole.aspx", StringComparison.OrdinalIgnoreCase))
                {
                    String _ButtonAction = Request.Form[SPEEvalInit._sAddrolePage_PostWords[1]];
                    if (_ButtonAction != null && _ButtonAction.EndsWith(SPEEvalInit._sAddrolePage_PostWords[2], StringComparison.OrdinalIgnoreCase))
                    {
                        //this is not a real post page action
                        return true;
                    }
                }
                else if (Request.FilePath.EndsWith("editrole.aspx", StringComparison.OrdinalIgnoreCase))
                {
                    String _ButtonAction = Request.Form[SPEEvalInit._sEditRolePage_PostWords[1]];
                    if (_ButtonAction != null && _ButtonAction.EndsWith(SPEEvalInit._sEditRolePage_PostWords[2], StringComparison.OrdinalIgnoreCase))
                    {
                        //this is not a real post page action
                        return true;
                    }
                }
                else if (Request.FilePath.EndsWith("Role.aspx", StringComparison.OrdinalIgnoreCase))
                {
                    String _ButtonAction = Request.Form[SPEEvalInit._sRolePage_PostWords[1]];
                    if (_ButtonAction != null && _ButtonAction.Equals(SPEEvalInit._sRolePage_PostWords[2], StringComparison.OrdinalIgnoreCase))
                    {
                        //this is not a real post page action
                        return true;
                    }
                }
            }
            return false;
        }

        int[] GetHiddenRoleList(HttpRequest Request, SPWeb _webObj)
        {
            if (_SiteStyle == SiteStyle.Style2007 || _SiteStyle == SiteStyle.Style2010)
            {
                String _RawViewState = Request.Form["__VIEWSTATE"];
                if (!string.IsNullOrEmpty(_RawViewState))
                {
                    List<int> _LstHiddenRoles = new List<int>();
                    byte[] _bRawViewState = Convert.FromBase64String(_RawViewState);
                    String _ViewState = System.Text.Encoding.Default.GetString(_bRawViewState);
                    for (int i = 0; i < _webObj.Roles.Count; i++)
                    {
                        String _Des = _webObj.Roles[i].Name + " - " + _webObj.Roles[i].Description;
                        if (_ViewState.IndexOf(_Des, StringComparison.OrdinalIgnoreCase) <= -1)
                        {
                            _LstHiddenRoles.Add(i);
                        }
                    }
                    return _LstHiddenRoles.ToArray();
                }
            }
            return null;
        }

        int GetTransferedID(int RoleID, int[] _LstHiddenRoles, int totalcount)
        {
            int _reRoleID = RoleID;
            if (_LstHiddenRoles == null)
                return _reRoleID;
            if (_LstHiddenRoles.Length <= 0)
                return _reRoleID;
            for (int i = 0; i < _LstHiddenRoles.Length; i++)
            {
                if (_reRoleID >= _LstHiddenRoles[i])
                    _reRoleID++;
            }
            if (_reRoleID >= totalcount)
                _reRoleID = totalcount - 1;
            return _reRoleID;
        }

        private String GetPermissionWordForSP2013(HttpRequest Request, SPWeb _webObj, String _spRoleWord)
        {
            String _spPermission = "";
            SPRoleDefinition spRoleDef = null;
            if (_webObj == null)
                return null;

            for (int i = Request.Form.AllKeys.Length - 1; i >= 0; i--)
            {
                if (Request.Form.AllKeys[i] != null && Request.Form.AllKeys[i].IndexOf(_spRoleWord) > -1)
                {
                    string roleIDfromRequest = Request.Form[i];
                    int roleID = -1;
                    try
                    {
                        roleID = Convert.ToInt32(roleIDfromRequest);
                    }
                    catch { }

                    if (roleID == -1)
                        continue;
                    for (int j = _webObj.RoleDefinitions.Count - 1; j >= 0; j--)
                    {
                        if (_webObj.RoleDefinitions[j].Id == roleID)
                            spRoleDef = _webObj.RoleDefinitions[j];
                    }

                    if (spRoleDef != null)
                        _spPermission += spRoleDef.Name + ";";
                }
            }
            return _spPermission;
        }
        String GetPermissionWord(HttpRequest Request, SPWeb _webObj, String _spRoleWord)
        {
            if (_SiteStyle == SiteStyle.Style2013)
            {
                return GetPermissionWordForSP2013(Request, _webObj, _spRoleWord);
            }

            String _spPermission = "";
            SPRole _role = null;
            int _RoleID = -1;
            int[] _hiddenlist = GetHiddenRoleList(Request, _webObj);
            for (int i = Request.Form.AllKeys.Length - 1; i >= 0; i--)
            {
                if (Request.Form.AllKeys[i] != null && Request.Form.AllKeys[i].IndexOf(_spRoleWord) > -1)
                {
                    if (Request.Form[i].Equals("on"))
                    {
                        String _spRoleID = null;
                        //Get the last number word
                        if (_SiteStyle == SiteStyle.Style2007)
                        {
                            _spRoleID = Request.Form.AllKeys[i].Substring(_spRoleWord.Length, Request.Form.AllKeys[i].Length - _spRoleWord.Length);
                            try
                            {
                                _RoleID = Convert.ToInt32(_spRoleID);
                            }
                            catch
                            {
                                int index = Request.Form.AllKeys[i].LastIndexOf("$");
                                _spRoleID = Request.Form.AllKeys[i].Substring(index + 1);
                                _RoleID = Convert.ToInt32(_spRoleID);
                            }
                        }
                        else
                        {
                            int index = Request.Form.AllKeys[i].LastIndexOf("$");
                            _spRoleID = Request.Form.AllKeys[i].Substring(index + 1);
                            _RoleID = Convert.ToInt32(_spRoleID);
                        }
                        //And convert it to number                                               
                        try
                        {
                            //fix bug 10571 Convert it again as there will be some hidden roles
                            _RoleID = GetTransferedID(_RoleID, _hiddenlist, _webObj.Roles.Count);

                            //this role id can be used to get the role
                            _role = _webObj.Roles[_RoleID];
                        }
                        catch
                        {
                        }
                        if (_role != null)
                            _spPermission += _role.Name + ";";// +" - " + _role.Description + ";";
                    }
                    else
                    {
                        try
                        {
                            _RoleID = Convert.ToInt32(Request.Form[i]);
                            _RoleID = GetTransferedID(_RoleID, _hiddenlist, _webObj.Roles.Count);
                            _role = _webObj.Roles[_RoleID];
                            if (_role != null)
                                _spPermission += _role.Name + ";";
                        }
                        catch
                        {
                        }
                    }
                }
            }
            return _spPermission;
        }

        public String[] ProcessAdminLogs(HttpRequest Request, String Object, String TargetLevel, String _SiteVersion)
        {
            if (_SiteVersion != null && _SiteVersion.StartsWith("12"))
            {
                _SiteStyle = SiteStyle.Style2007;
            }
            else if (_SiteVersion != null && _SiteVersion.StartsWith("15"))
            {
                _SiteStyle = SiteStyle.Style2013;
            }

            if (Request.FilePath.EndsWith("Aclinv.aspx", StringComparison.OrdinalIgnoreCase))
            {
                return ProcessAdmin_AclinvPage(Request, Object, TargetLevel);
            }
            else if (Request.FilePath.EndsWith("Newgrp.aspx", StringComparison.OrdinalIgnoreCase))
            {
                return ProcessAdmin_NewgrpPage(Request, Object, TargetLevel);
            }
            else if (Request.FilePath.EndsWith("People.aspx", StringComparison.OrdinalIgnoreCase))
            {
                return ProcessAdmin_PeoplePage(Request, Object, TargetLevel);
            }
            else if (Request.FilePath.EndsWith("editgrp.aspx", StringComparison.OrdinalIgnoreCase))
            {
                return ProcessAdmin_EditgrpPage(Request, Object, TargetLevel);
            }
            else if (Request.FilePath.EndsWith("Permsetup.aspx", StringComparison.OrdinalIgnoreCase))
            {
                return ProcessAdmin_PermsetupPage(Request, Object, TargetLevel);
            }
            else if (Request.FilePath.EndsWith("Mngsiteadmin.aspx", StringComparison.OrdinalIgnoreCase))
            {
                return ProcessAdmin_MngsiteadminPage(Request, Object, TargetLevel);
            }
            else if (Request.FilePath.EndsWith("Editprms.aspx", StringComparison.OrdinalIgnoreCase))
            {
                return ProcessAdmin_EditprmsPage(Request, Object, TargetLevel);
            }
            else if (Request.FilePath.EndsWith("user.aspx", StringComparison.OrdinalIgnoreCase))
            {
                return ProcessAdmin_UserPage(Request, Object, TargetLevel);
            }
            else if (Request.FilePath.EndsWith("Addrole.aspx", StringComparison.OrdinalIgnoreCase))
            {
                return ProcessAdmin_AddrolePage(Request, Object, TargetLevel);
            }
            else if (Request.FilePath.EndsWith("EditRole.aspx", StringComparison.OrdinalIgnoreCase))
            {
                return ProcessAdmin_EditRolePage(Request, Object, TargetLevel);
            }
            else if (Request.FilePath.EndsWith("Role.aspx", StringComparison.OrdinalIgnoreCase))
            {
                return ProcessAdmin_RolePage(Request, Object, TargetLevel);
            }
            return null;
        }

        String AnalayseCheckedData(String _CheckedString)
        {
            String _keyString = "class=ms-entity-resolved id=span";
            String _endString = "title=";
            String _keyString1 = "SPAN id=span";
            String _endString1 = "class=ms-entity-resolved";
            String _keyString2 = "SPAN id=\"span";
            String _endString2 = "\" class=ms-entity-resolved";
            String _keyString3 = "class=ms-entity-resolved id=\"span";
            String _endString3 = "\" title=";

            int _keyindex = _CheckedString.IndexOf(_keyString);
            int _endindex = _CheckedString.IndexOf(_endString);
            int _keyindex1 = _CheckedString.IndexOf(_keyString1);
            int _endindex1 = _CheckedString.IndexOf(_endString1);
            int _keyindex2 = _CheckedString.IndexOf(_keyString2);
            int _endindex2 = _CheckedString.IndexOf(_endString2);
            int _keyindex3 = _CheckedString.IndexOf(_keyString3);
            int _endindex3 = _CheckedString.IndexOf(_endString3);
            if (_keyindex != -1 && _endindex != -1 && _endindex > _keyindex + _keyString.Length)
            {
                return _CheckedString.Substring(_keyindex + _keyString.Length, _endindex - _keyindex - _keyString.Length);
            }
            //fix bug 11241, this string sequence only happen in windows 7
            else if (_keyindex1 != -1 && _endindex1 != -1 && _endindex1 > _keyindex1 + _keyString1.Length)
            {
                return _CheckedString.Substring(_keyindex1 + _keyString1.Length, _endindex1 - _keyindex1 - _keyString1.Length);
            }
            else if (_keyindex2 != -1 && _endindex2 != -1 && _endindex2 > _keyindex2 + _keyString2.Length)
            {
                return _CheckedString.Substring(_keyindex2 + _keyString2.Length, _endindex2 - _keyindex2 - _keyString2.Length);
            }
            else if (_keyindex3 != -1 && _endindex3 != -1 && _endindex3 > _keyindex3 + _keyString3.Length)
            {
                return _CheckedString.Substring(_keyindex3 + _keyString3.Length, _endindex3 - _keyindex3 - _keyString3.Length);
            }
            //fix bug 10585,add a group's keyword may be different from add use
            _keyString = "title=";
            _endString = "contentEditable";
            _keyindex = _CheckedString.IndexOf(_keyString);
            _endindex = _CheckedString.IndexOf(_endString);
            if (_keyindex != -1 && _endindex != -1 && _endindex > _keyindex + _keyString.Length)
            {
                return _CheckedString.Substring(_keyindex + _keyString.Length + 1, _endindex - _keyindex - _keyString.Length - 3);
            }
            return "";
        }

        String AnalayseUnCheckedData(String _UnCheckedString)
        {
            String _reString = _UnCheckedString;
            if (_reString.StartsWith("</SPAN>", StringComparison.OrdinalIgnoreCase))
                _reString = _reString.Substring(7);
            if (_reString.EndsWith("<SPAN", StringComparison.OrdinalIgnoreCase))
                _reString = _reString.Substring(0, _reString.Length - 5);
            if (_reString.StartsWith(";", StringComparison.OrdinalIgnoreCase))
                _reString = _reString.Substring(1);
            return _reString;

        }
        string ExtractUserFromJSON(string strJSONString)
        {
            if (string.IsNullOrEmpty(strJSONString))
                return string.Empty;

            StringBuilder sb = new StringBuilder();
            string strBegin = "\"Key\":\"";
            int iPosBegin = 0, iPosEnd = 0;
            while (true)
            {
                iPosBegin = strJSONString.IndexOf(strBegin, iPosEnd);
                if (iPosBegin != -1)
                {
                    iPosEnd = strJSONString.IndexOf("\"", iPosBegin + 1 + strBegin.Length);
                    if (iPosEnd != -1)
                    {
                        string userKey=strJSONString.Substring(iPosBegin + strBegin.Length, iPosEnd - iPosBegin - strBegin.Length);
                        if (sb.Length > 0)
                        {
                            sb.AppendFormat(";{0}", userKey);
                        }
                        else
                        {
                            sb.Append(userKey);
                        }
                        iPosEnd += 1;
                    }
                    else
                        break;
                }
                else
                    break;
            }
            return sb.ToString();
        }
        String[] SplitHiddenData(String _spHiddenData)
        {
            List<String> _LstHiddenData = new List<String>();
            if (!string.IsNullOrEmpty(_spHiddenData))
            {
                if (_spHiddenData.StartsWith("&nbsp;", StringComparison.OrdinalIgnoreCase) && _SiteStyle == SiteStyle.Style2010)
                {
                    _spHiddenData = _spHiddenData.Substring(6);
                }
                for (int _Start = 0; _Start < _spHiddenData.Length; )
                {
                    int _SpanStart = _spHiddenData.IndexOf("<SPAN", _Start, StringComparison.OrdinalIgnoreCase);
                    if (_SpanStart <= -1)
                    {
                        if (_Start < _spHiddenData.Length)
                        {
                            String _spSub = _spHiddenData.Substring(_Start, _spHiddenData.Length - _Start);
                            _LstHiddenData.Add(AnalayseUnCheckedData(_spSub));
                        }
                        _Start = _spHiddenData.Length;
                    }
                    else
                    {
                        if (_SpanStart > _Start)
                        {
                            String _spSub = _spHiddenData.Substring(_Start, _SpanStart - _Start);
                            _LstHiddenData.Add(AnalayseUnCheckedData(_spSub));
                            _Start = _SpanStart;
                        }
                        else
                        {
                            int _SpanEnd = _spHiddenData.IndexOf("</SPAN>", _Start, StringComparison.OrdinalIgnoreCase);
                            if (_SpanEnd != -1)
                            {
                                String _spSub = _spHiddenData.Substring(_Start, _SpanEnd - _Start);
                                _LstHiddenData.Add(AnalayseCheckedData(_spSub));
                                _Start = _SpanEnd + 7;
                            }
                            else
                            {
                                String _spSub = _spHiddenData.Substring(_Start, _spHiddenData.Length - _Start);
                                _LstHiddenData.Add(AnalayseUnCheckedData(_spSub));
                                _Start = _spHiddenData.Length;
                            }
                        }
                    }

                }
            }
            return _LstHiddenData.ToArray();

        }


        String[] ProcessAdmin_AclinvPage(HttpRequest Request, String _spObject, String _spTargetLevel)
        {

            String[] pAclinvPage_PostWords = { };
            pAclinvPage_PostWords = SPEEvalInit._sAclinvPage_PostWords;
#if SP2013
            if (_SiteStyle == SiteStyle.Style2010)
            {
                pAclinvPage_PostWords = SPEEvalInit._sAclinvPage_PostWords_2010;
            }
#endif
            SPWeb _webObj = SPControl.GetContextWeb(HttpContext.Current);
            String _spAction = "";
            String _spUser = "";
            String _spGroup = "";
            String _spPermission = "";
            String _spChangedBy = "";
            String _spHiddenData = "";
            //Action's group
            SPGroup _ActionGroup = null;
            //Added to group's role
            SPRole _role = null;
            String _StrAddGroupID = null;
            //Get action type
            String _ButtonAction = Request.Form[pAclinvPage_PostWords[1]];
            if (_ButtonAction == null || !_ButtonAction.EndsWith(pAclinvPage_PostWords[2], StringComparison.OrdinalIgnoreCase))
            {
                //this is not a real post page action
                return null;
            }
            //Action
            _spAction = "AddUserToGroup";
            //Get changed by user
            _spChangedBy = Request.ServerVariables[pAclinvPage_PostWords[3]];
            if (string.IsNullOrEmpty(_spChangedBy))
            {
                _spChangedBy = _webObj.CurrentUser.LoginName;
            }
            if (_SiteStyle == SiteStyle.Style2007)
            {
                _StrAddGroupID = Request.QueryString[pAclinvPage_PostWords[4]];
                if (string.IsNullOrEmpty(_StrAddGroupID))
                    _StrAddGroupID = Request.Form[pAclinvPage_PostWords[4]];
            }
            else
                _StrAddGroupID = Request.QueryString[pAclinvPage_PostWords[4]];

            if (string.IsNullOrEmpty(_StrAddGroupID))
                _StrAddGroupID = Request.QueryString[pAclinvPage_PostWords[4]];

            //Action's group
            int _GroupID = -1;
            int _RoleID = -1;
            if (!string.IsNullOrEmpty(_StrAddGroupID))
            {
                _GroupID = Convert.ToInt32(_StrAddGroupID);
            }
            else
            {
                string groupid = null;
                try
                {
                    groupid = Request.Form[pAclinvPage_PostWords[10]];
                }
                catch
                {
                }
                if (!string.IsNullOrEmpty(groupid))
                {
                    string[] strs = groupid.Split(new Char[] { ':' });
                    if (strs[0].Equals("group"))
                    {
                        _GroupID = Convert.ToInt32(strs[1]);
                    }
                    if (strs[0].Equals("role"))
                    {
                        _RoleID = Convert.ToInt32(strs[1]);
                    }
                }
                else
                {
                    try
                    {
                        groupid = Request.Form[pAclinvPage_PostWords[9]];
                    }
                    catch
                    {
                    }
                    if (!string.IsNullOrEmpty(groupid))
                    {
                        _GroupID = Convert.ToInt32(groupid);
                    }
                    else
                    {
                        if (_SiteStyle == SiteStyle.Style2010)
                        {
                            try
                            {
                                for (int i = Request.Form.AllKeys.Length - 1; i >= 0; i--)
                                {
                                    if (Request.Form.AllKeys[i] != null && Request.Form.AllKeys[i].Contains(pAclinvPage_PostWords[11]))
                                    {
                                        if (!Request.Form[Request.Form.AllKeys[i]].Contains("on"))
                                        {
                                            groupid = Request.Form[Request.Form.AllKeys[i]];
                                        }
                                        break;
                                    }
                                }
                            }
                            catch
                            {
                            }
                            if(!string.IsNullOrEmpty(groupid))
                            {
                                _RoleID = Convert.ToInt32(groupid);
                            }
                        }
                    }
                }
            }

            if (_SiteStyle == SiteStyle.Style2010 || _SiteStyle == SiteStyle.Style2007)
            {
                _spHiddenData = Request.Form[pAclinvPage_PostWords[7]];
                String[] _spUserArray = SplitHiddenData(_spHiddenData);
                for (int i = 0; i < _spUserArray.Length; i++)
                {
                    if (!string.IsNullOrWhiteSpace(_spUserArray[i]))
                    {
                        if (_spUserArray[i].StartsWith("&nbsp;", StringComparison.OrdinalIgnoreCase))
                        {
                            _spUserArray[i] = _spUserArray[i].Substring(6);
                            if (_spUserArray[i].Length <= 0)
                                continue;
                        }
                        int index = _spUserArray[i].IndexOf("|");
                        if (index != -1)
                        {
                            _spUserArray[i] = _spUserArray[i].Substring(index + 1);
                        }
                        if (_spUserArray[i].EndsWith(";"))
                        {
                            _spUser += _spUserArray[i];
                        }
                        else
                        {
                            _spUser += _spUserArray[i] + ";";
                        }
                    }
                }
            }
            else
            {
                _spUser = Request.Form[pAclinvPage_PostWords[7]];
                //try get user info from json
                string usersString = ExtractUserFromJSON(_spUser);
                if (!string.IsNullOrEmpty(usersString))
                {
                    _spUser = usersString;
                }
            }

            //Get the group
            foreach (SPGroup group in _webObj.SiteGroups)
            {
                if (group.ID == _GroupID)
                {
                    _ActionGroup = group;
                    break;
                }
            }
            if (_ActionGroup != null)
                _spGroup = _ActionGroup.Name;



            //Get the role permission
            //User choose a group from a down-select box
            if (_GroupID != -1)
            {
                //Add to what group
                if (_RoleID != -1)
                {
                    _role = _webObj.Roles.GetByID(_RoleID);
                    if (_role != null)
                    {
                        _spPermission += _role.Name + _role.Description + ";";
                    }

                }
                else
                {
                    SPSecurity.RunWithElevatedPrivileges(delegate()
                    {
                        using (SPSite _site = new SPSite(_webObj.Url))
                        {
                            using (SPWeb _Web = _site.OpenWeb())
                            {

                                foreach (SPRole role in _Web.Roles)
                                {
                                    foreach (SPGroup group in role.Groups)
                                    {
                                        if (group.ID == _GroupID)
                                        {
                                            _role = role;
                                            if (_role != null)
                                            {
                                                _spPermission += _role.Name + _role.Description + ";";
                                            }
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    });
                }
                if (_role == null || _spPermission == null || _spPermission.Length == 0)
                {
                    _spPermission = "No Access";
                }
            }
            //User choose permission from check box
            else
            {
                if (_RoleID != -1)
                {
                    _role = _webObj.Roles.GetByID(_RoleID);
                    if (_role != null)
                    {
                        _spPermission += _role.Name + _role.Description + ";";
                    }

                }
                if (string.IsNullOrEmpty(_spPermission))
                {
#if SP2010
                    _spPermission = GetPermissionWord(Request, _webObj, SPEEvalInit._sAclinvPage_PostWords[11]);
#else
                    _spPermission = GetPermissionWord(Request, _webObj, SPEEvalInit._sAclinvPage_PostWords[8]);
#endif
                }
            }
            List<String> _list = new List<String>();
            _list.Add("Action");
            _list.Add(_spAction);
            _list.Add("User");
            _list.Add(_spUser);
            _list.Add("Group");
            _list.Add(_spGroup);


            _list.Add("Permission");
            _list.Add(_spPermission);

            _list.Add("TargetLevel");
            _list.Add(_spTargetLevel);
            _list.Add("Object");
            _list.Add(_spObject);
            _list.Add("ChangedBy");
            _list.Add(_spChangedBy);
            return _list.ToArray();
        }

        String[] ProcessAdmin_NewgrpPage(HttpRequest Request, String _spObject, String _spTargetLevel)
        {
            String[] pNewgrpPage_PostWords = { };
            pNewgrpPage_PostWords = SPEEvalInit._sNewgrpPage_PostWords;
#if SP2013
            if (_SiteStyle == SiteStyle.Style2010)
            {
                pNewgrpPage_PostWords = SPEEvalInit._sNewgrpPage_PostWords_2010;
            }
#endif
            SPWeb _webObj = SPControl.GetContextWeb(HttpContext.Current);
            String _spAction = "";
            String _spGroupOwner = "";
            String _spGroup = "";
            String _spPermission = "";
            String _spChangedBy = "";
            String _spHiddenData = "";
            String _spUserData = "";
            //Get action type
            String _ButtonAction = Request.Form[pNewgrpPage_PostWords[1]];
            if (_ButtonAction == null || !_ButtonAction.EndsWith(pNewgrpPage_PostWords[2], StringComparison.OrdinalIgnoreCase))
            {
                //this is not a real post page action
                return null;
            }
            //Action
            _spAction = "NewGroup";
            //Get changed by user
            _spChangedBy = Request.ServerVariables[pNewgrpPage_PostWords[3]];
            if (string.IsNullOrEmpty(_spChangedBy))
            {
                _spChangedBy = _webObj.CurrentUser.LoginName;
            }
            //Get new group name
            _spGroup = Request.Form[pNewgrpPage_PostWords[4]];
            //Get the group owner user
            _spUserData = Request.Form[pNewgrpPage_PostWords[5]];
            //Get Hidden Data
            _spHiddenData = Request.Form[pNewgrpPage_PostWords[6]];
            if (_SiteStyle == SiteStyle.Style2007)
            {
                if (_spHiddenData != null && _spHiddenData.IndexOf("http://www.w3.org/2001/XMLSchema") > -1
                    && _spHiddenData.IndexOf(_spUserData) > -1)
                {
                    _spGroupOwner = _spUserData;
                }
                else
                {
                    _spGroupOwner = _spHiddenData;
                }
            }
            else if (_SiteStyle == SiteStyle.Style2010)
            {
                String[] _spUserArray = SplitHiddenData(_spHiddenData);
                if (_spUserArray != null)
                {
                    int index = _spUserArray[0].IndexOf("|");
                    if (index != -1)
                    {
                        _spUserArray[0] = _spUserArray[0].Substring(index + 1);
                    }
                    _spGroupOwner = _spUserArray[0];
                }
            }
            else
            {
                _spGroupOwner = _spUserData;
                //try get user info from json string
                var users = ExtractUserFromJSON(_spGroupOwner);
                if (!string.IsNullOrEmpty(users))
                {
                    _spGroupOwner = users;
                }
            }


            //Get permission words
            if (_SiteStyle == SiteStyle.Style2007)
                _spPermission = GetPermissionWord(Request, _webObj, pNewgrpPage_PostWords[7]);
            else
                _spPermission = GetPermissionWord(Request, _webObj, pNewgrpPage_PostWords[8]);

            List<String> _list = new List<String>();
            _list.Add("Action");
            _list.Add(_spAction);
            _list.Add("Group");
            _list.Add(_spGroup);
            _list.Add("Permission");
            _list.Add(_spPermission);
            _list.Add("GroupOwner");
            _list.Add(_spGroupOwner);
            _list.Add("TargetLevel");
            _list.Add(_spTargetLevel);
            _list.Add("Object");
            _list.Add(_spObject);
            _list.Add("ChangedBy");
            _list.Add(_spChangedBy);
            return _list.ToArray();
        }

        String[] ProcessAdmin_PeoplePage(HttpRequest Request, String _spObject, String _spTargetLevel)
        {
            String[] pPeoplePage_PostWords = { };
            pPeoplePage_PostWords = SPEEvalInit._sPeoplePage_PostWords;
#if SP2013
            if (_SiteStyle == SiteStyle.Style2010)
            {
                pPeoplePage_PostWords = SPEEvalInit._sPeoplePage_PostWords_2010;
            }
#endif
            SPWeb _webObj = SPControl.GetContextWeb(HttpContext.Current);
            String _spAction = "";
            String _spUser = "";
            String _spGroup = "";
            String _spChangedBy = "";

            //Get action type
            String _ButtonAction = Request.Form[pPeoplePage_PostWords[1]];
            if (_ButtonAction == null || !(_ButtonAction.EndsWith(pPeoplePage_PostWords[2], StringComparison.OrdinalIgnoreCase) || _ButtonAction.EndsWith(pPeoplePage_PostWords[6], StringComparison.OrdinalIgnoreCase)))
            {
                //this is not a real post page action
                return null;
            }
            //Action
            if (_ButtonAction.EndsWith(pPeoplePage_PostWords[2], StringComparison.OrdinalIgnoreCase))
                _spAction = "RemoveFromGroup";
            else if (_ButtonAction.EndsWith(pPeoplePage_PostWords[6], StringComparison.OrdinalIgnoreCase))
                _spAction = "RemoveUsersFromSiteCollection";
            //Get changed by user
            _spChangedBy = Request.ServerVariables[pPeoplePage_PostWords[3]];
            if (string.IsNullOrEmpty(_spChangedBy))
            {
                _spChangedBy = _webObj.CurrentUser.LoginName;
            }
            //Get Group
            String _StrGroupID = Request.QueryString[pPeoplePage_PostWords[4]];
            int _GroupID = Convert.ToInt32(_StrGroupID);
            foreach (SPGroup group in _webObj.SiteGroups)
            {
                if (group.ID == _GroupID)
                {
                    _spGroup = group.Name;
                    break;
                }
            }
            //Get Deleting user
            String _spUserID = Request.Form[pPeoplePage_PostWords[5]];
            String[] _User_list = _spUserID.Split(new String[] { "," }, StringSplitOptions.None);
            for (int i = 0; i < _User_list.Length; i++)
            {
                for (int j = 0; j < _webObj.SiteUsers.Count; j++)
                {
                    if (_webObj.SiteUsers[j].ID.ToString() == _User_list[i])
                    {
                        _spUser += _webObj.SiteUsers[j].Name + ";";
                        break;
                    }
                }
            }
            List<String> _list = new List<String>();
            _list.Add("Action");
            _list.Add(_spAction);
            _list.Add("Group");
            _list.Add(_spGroup);
            _list.Add("User");
            _list.Add(_spUser);
            _list.Add("TargetLevel");
            _list.Add(_spTargetLevel);
            _list.Add("Object");
            _list.Add(_spObject);
            _list.Add("ChangedBy");
            _list.Add(_spChangedBy);
            return _list.ToArray();
        }

        String[] ProcessAdmin_EditgrpPage(HttpRequest Request, String _spObject, String _spTargetLevel)
        {
            String[] pEditgrpPage_PostWords = { };
            pEditgrpPage_PostWords = SPEEvalInit._sEditgrpPage_PostWords;
#if SP2013
            if (_SiteStyle == SiteStyle.Style2010)
            {
                pEditgrpPage_PostWords = SPEEvalInit._sEditgrpPage_PostWords_2010;
            }
#endif
            SPWeb _webObj = SPControl.GetContextWeb(HttpContext.Current);
            String _spAction = "";
            String _spOldGroupOwner = "";
            String _spOldGroupName = "";
            String _spOldPermission = "";
            String _spNewGroupOwner = "";
            String _spNewGroupName = "";
            String _spNewPermission = "";
            String _spChangedBy = "";
            String _spHiddenData = "";
            String _spUserData = "";
            //Get action type
            String _ButtonAction = Request.Form[pEditgrpPage_PostWords[1]];
            if (_ButtonAction == null || !(_ButtonAction.EndsWith(pEditgrpPage_PostWords[2], StringComparison.OrdinalIgnoreCase) || _ButtonAction.EndsWith(pEditgrpPage_PostWords[3], StringComparison.OrdinalIgnoreCase)))
            {
                //this is not a real post page action
                return null;
            }
            //Get changed by user
            _spChangedBy = Request.ServerVariables[pEditgrpPage_PostWords[4]];
            if (string.IsNullOrEmpty(_spChangedBy))
            {
                _spChangedBy = _webObj.CurrentUser.LoginName;
            }
            //Get Group
            _spOldGroupName = Request.QueryString[pEditgrpPage_PostWords[5]];
            //Get Old Owner and Old group name
            foreach (SPGroup group in _webObj.SiteGroups)
            {
                if (group.Name == _spOldGroupName)
                {
                    _spOldGroupOwner = group.Owner.ToString();
                    break;
                }
            }
            int index1 = _spOldGroupOwner.IndexOf("|");
            if (index1 != -1 && _SiteStyle == SiteStyle.Style2010)
            {
                _spOldGroupOwner = _spOldGroupOwner.Substring(index1 + 1);
            }
            //Get Old Permission
            SPSecurity.RunWithElevatedPrivileges(delegate()
            {
                using (SPSite _site = new SPSite(_webObj.Url))
                {
                    using (SPWeb _Web = _site.OpenWeb())
                    {
                        foreach (SPRole role in _Web.Roles)
                        {
                            foreach (SPGroup group in role.Groups)
                            {
                                if (group.Name == _spOldGroupName)
                                {
                                    if (role != null)
                                    {
                                        _spOldPermission += role.Name; //+ role.Description + ";";
                                    }
                                    break;
                                }
                            }
                        }
                    }
                }
            });
            if (_ButtonAction.EndsWith(pEditgrpPage_PostWords[2], StringComparison.OrdinalIgnoreCase))
            {
                //Action            
                _spAction = "EditGroup";
                //Get New Owner and New Group Name
                _spUserData = Request.Form[pEditgrpPage_PostWords[6]];
                //Get Hidden Data
                _spHiddenData = Request.Form[pEditgrpPage_PostWords[7]];
                if (_SiteStyle == SiteStyle.Style2007)
                {
                    if (_spHiddenData != null && _spHiddenData.IndexOf("http://www.w3.org/2001/XMLSchema") > -1
                        && _spHiddenData.IndexOf(_spUserData) > -1)
                    {
                        _spNewGroupOwner = _spUserData;
                    }
                    else
                    {
                        _spNewGroupOwner = _spHiddenData;
                    }
                }
                else if (_SiteStyle == SiteStyle.Style2010)
                {
                    String[] _spUserArray = SplitHiddenData(_spHiddenData);
                    int index = _spUserArray[0].IndexOf("|");
                    if (index != -1)
                    {
                        _spUserArray[0] = _spUserArray[0].Substring(index + 1);
                    }
                    _spNewGroupOwner = _spUserArray[0];
                }
                else
                {
                    _spNewGroupOwner = _spHiddenData;
                    //try get user info from json string
                    var users = ExtractUserFromJSON(_spNewGroupOwner);
                    if (!string.IsNullOrEmpty(users))
                    {
                        _spNewGroupOwner = users;
                    }
                }

                _spNewGroupName = Request.Form[pEditgrpPage_PostWords[8]];
                //Get New Permission
                if (_SiteStyle == SiteStyle.Style2007)
                {
                    _spNewPermission = GetPermissionWord(Request, _webObj, pEditgrpPage_PostWords[9]);
                    if (_spOldPermission != null && string.IsNullOrEmpty(_spOldPermission))
                    {
                        _spOldPermission = "No Access";
                    }
                    if (_spNewPermission != null && string.IsNullOrEmpty(_spNewPermission))
                    {
                        if (_webObj.Url.Equals(_webObj.Site.Url) && _spOldPermission.IndexOf("Limited Access", StringComparison.OrdinalIgnoreCase) <= -1)
                            _spNewPermission = "No Access";
                        else
                            _spNewPermission = _spOldPermission;
                    }
                }
                else
                    _spNewPermission = _spOldPermission;
                List<String> _list = new List<String>();
                _list.Add("Action");
                _list.Add(_spAction);
                _list.Add("NewGroupName");
                _list.Add(_spNewGroupName);
                _list.Add("OldGroupName");
                _list.Add(_spOldGroupName);
                _list.Add("NewGroupOwner");
                _list.Add(_spNewGroupOwner);
                _list.Add("OldGroupOwner");
                _list.Add(_spOldGroupOwner);
                _list.Add("NewPermission");
                _list.Add(_spNewPermission);
                _list.Add("OldPermission");
                _list.Add(_spOldPermission);
                _list.Add("TargetLevel");
                _list.Add(_spTargetLevel);
                _list.Add("Object");
                _list.Add(_spObject);
                _list.Add("ChangedBy");
                _list.Add(_spChangedBy);
                return _list.ToArray();
            }
            else if (_ButtonAction.EndsWith(pEditgrpPage_PostWords[3], StringComparison.OrdinalIgnoreCase))
            {
                //Action            
                _spAction = "DeleteGroup";
                List<String> _list = new List<String>();
                _list.Add("Action");
                _list.Add(_spAction);
                _list.Add("Group");
                _list.Add(_spOldGroupName);
                _list.Add("TargetLevel");
                _list.Add(_spTargetLevel);
                _list.Add("Object");
                _list.Add(_spObject);
                _list.Add("ChangedBy");
                _list.Add(_spChangedBy);
                return _list.ToArray();
            }
            return null;
        }

        String PermsetupPageGetGroupName(HttpRequest Request, SPWeb _webObj,
                                         String _keyGroupChoice,
                                         String _keyGroupCreateNew,
                                         String _keyGroupNewName,
                                         String _keyGroupUseExisting,
                                         String _keyGroupExistingName)
        {
            String _GroupName = "";
            String _GroupChoice = Request.Form[_keyGroupChoice];
            if (_GroupChoice != null && _GroupChoice.Equals(_keyGroupCreateNew))
            {
                _GroupName = Request.Form[_keyGroupNewName];
            }
            else if (_GroupChoice != null && _GroupChoice.Equals(_keyGroupUseExisting))
            {
                String _spGroupID = Request.Form[_keyGroupExistingName];
                int _GroupID = Convert.ToInt32(_spGroupID);
                foreach (SPGroup group in _webObj.SiteGroups)
                {
                    if (group.ID == _GroupID)
                    {
                        _GroupName = group.Name;
                        break;
                    }
                }
            }
            return _GroupName;
        }

        String[] ProcessAdmin_PermsetupPage(HttpRequest Request, String _spObject, String _spTargetLevel)
        {
            String[] pPermsetupPage_PostWords = { };
            pPermsetupPage_PostWords = SPEEvalInit._sPermsetupPage_PostWords;
#if SP2013
            if (_SiteStyle == SiteStyle.Style2010)
            {
                pPermsetupPage_PostWords = SPEEvalInit._sPermsetupPage_PostWords_2010;
            }
#endif
            SPWeb _webObj = SPControl.GetContextWeb(HttpContext.Current);
            String _spAction = "";
            String _spVistorsGroup = "";
            String _spMembersGroup = "";
            String _spOwnersGroup = "";
            String _spChangedBy = "";

            //Get action type
            String _ButtonAction = Request.Form[pPermsetupPage_PostWords[1]];
            if (_ButtonAction == null || !_ButtonAction.EndsWith(pPermsetupPage_PostWords[2], StringComparison.OrdinalIgnoreCase))
            {
                //this is not a real post page action
                return null;
            }
            //Get Action type
            _spAction = "SetupGroupsForSite";
            //Get changed by user
            _spChangedBy = Request.ServerVariables[pPermsetupPage_PostWords[3]];
            if (string.IsNullOrEmpty(_spChangedBy))
            {
                _spChangedBy = _webObj.CurrentUser.LoginName;
            }
            //Get Vistors Group
            _spVistorsGroup = PermsetupPageGetGroupName(Request, _webObj,
                                      pPermsetupPage_PostWords[4],
                                      pPermsetupPage_PostWords[5],
                                      pPermsetupPage_PostWords[6],
                                      pPermsetupPage_PostWords[7],
                                      pPermsetupPage_PostWords[8]);
            //Get Members Group
            _spMembersGroup = PermsetupPageGetGroupName(Request, _webObj,
                                      pPermsetupPage_PostWords[9],
                                      pPermsetupPage_PostWords[10],
                                      pPermsetupPage_PostWords[11],
                                      pPermsetupPage_PostWords[12],
                                      pPermsetupPage_PostWords[13]);
            //Get Owners Group
            _spOwnersGroup = PermsetupPageGetGroupName(Request, _webObj,
                                      pPermsetupPage_PostWords[14],
                                      pPermsetupPage_PostWords[15],
                                      pPermsetupPage_PostWords[16],
                                      pPermsetupPage_PostWords[17],
                                      pPermsetupPage_PostWords[18]);
            List<String> _list = new List<String>();
            _list.Add("Action");
            _list.Add(_spAction);
            _list.Add("VistorsGroup");
            _list.Add(_spVistorsGroup);
            _list.Add("MembersGroup");
            _list.Add(_spMembersGroup);
            _list.Add("OwnersGroup");
            _list.Add(_spOwnersGroup);
            _list.Add("TargetLevel");
            _list.Add(_spTargetLevel);
            _list.Add("Object");
            _list.Add(_spObject);
            _list.Add("ChangedBy");
            _list.Add(_spChangedBy);
            return _list.ToArray();


        }

        String[] ProcessAdmin_MngsiteadminPage(HttpRequest Request, String _spObject, String _spTargetLevel)
        {
            String[] pMngsiteadminPage_PostWords = { };
            pMngsiteadminPage_PostWords = SPEEvalInit._sMngsiteadminPage_PostWords;
#if SP2013
            if (_SiteStyle == SiteStyle.Style2010)
            {
                pMngsiteadminPage_PostWords = SPEEvalInit._sMngsiteadminPage_PostWords_2010;
            }
#endif
            SPWeb _webObj = SPControl.GetContextWeb(HttpContext.Current);
            String _spAction = "";
            String _spUser = "";
            String _spChangedBy = "";
            String _spHiddenData = "";
            //Get action type
            String _ButtonAction = Request.Form[pMngsiteadminPage_PostWords[1]];
            if (_ButtonAction == null || !_ButtonAction.EndsWith(pMngsiteadminPage_PostWords[2], StringComparison.OrdinalIgnoreCase))
            {
                //this is not a real post page action
                return null;
            }
            //Get action type
            _spAction = "AssignSiteCollectAdmin";
            //Get changed by user
            _spChangedBy = Request.ServerVariables[pMngsiteadminPage_PostWords[3]];
            if (string.IsNullOrEmpty(_spChangedBy))
            {
                _spChangedBy = _webObj.CurrentUser.LoginName;
            }
            //Get new admin user
            _spUser = Request.Form[pMngsiteadminPage_PostWords[4]];
            if (_spUser != null)
                _spUser = ExtractUserFromJSON(_spUser);
            _spHiddenData = Request.Form[pMngsiteadminPage_PostWords[5]];
            if (_spHiddenData != null)
            {
                String[] _spUserArray = SplitHiddenData(_spHiddenData);
                for (int i = 0; i < _spUserArray.Length; i++)
                {
                    if (!string.IsNullOrWhiteSpace(_spUserArray[i]))
                    {
                        if (_spUserArray[i].EndsWith(";"))
                        {
                            _spUser += _spUserArray[i];
                        }
                        else
                        {
                            _spUser += _spUserArray[i] + ";";
                        }
                    }
                }
            }
            List<String> _list = new List<String>();
            _list.Add("Action");
            _list.Add(_spAction);
            _list.Add("User");
            _list.Add(_spUser);
            _list.Add("TargetLevel");
            _list.Add(_spTargetLevel);
            _list.Add("Object");
            _list.Add(_spObject);
            _list.Add("ChangedBy");
            _list.Add(_spChangedBy);
            return _list.ToArray();
        }

        String[] ProcessAdmin_EditprmsPage(HttpRequest Request, String _spObject, String _spTargetLevel)
        {
            String[] pEditprmsPage_PostWords = { };
            pEditprmsPage_PostWords = SPEEvalInit._sEditprmsPage_PostWords;
#if SP2013
            if (_SiteStyle == SiteStyle.Style2010)
            {
                pEditprmsPage_PostWords = SPEEvalInit._sEditprmsPage_PostWords_2010;
            }
#endif
            SPWeb _webObj = SPControl.GetContextWeb(HttpContext.Current);
            String _spAction = "";
            String _spUsers = "";
            String _spGroups = "";
            String _spChangedBy = "";
            String _spPermission = "";
            String _ButtonAction = Request.Form[pEditprmsPage_PostWords[1]];
            if (_ButtonAction == null || !_ButtonAction.EndsWith(pEditprmsPage_PostWords[2], StringComparison.OrdinalIgnoreCase))
            {
                //this is not a real post page action
                return null;
            }
            //Get action type
            _spAction = "AssignPermissionLevel";
            //Get changed by user
            _spChangedBy = Request.ServerVariables[pEditprmsPage_PostWords[3]];
            if (string.IsNullOrEmpty(_spChangedBy))
            {
                _spChangedBy = _webObj.CurrentUser.LoginName;
            }
            //Get Edit Items
            String _spItemsID = Request.QueryString[pEditprmsPage_PostWords[4]];
            String[] _Items_list = _spItemsID.Split(new String[] { "," }, StringSplitOptions.None);
            //Here we suppose group ID and user ID will not be confused, this is not a offical conclusion, just from experience
            for (int i = 0; i < _Items_list.Length; i++)
            {
                foreach (SPGroup group in _webObj.SiteGroups)
                {
                    if (group.ID.ToString().Equals(_Items_list[i]))
                    {
                        _spGroups += group.Name + ";";
                        break;
                    }
                }
            }
            for (int i = 0; i < _Items_list.Length; i++)
            {
                foreach (SPUser user in _webObj.SiteUsers)
                {
                    if (user.ID.ToString().Equals(_Items_list[i]))
                    {
                        _spUsers += user.Name + ";";
                        break;
                    }
                }
            }
            String _Target = (!string.IsNullOrEmpty(_spUsers)) ? _spUsers : _spGroups;
            _spPermission = GetPermissionWord(Request, _webObj, pEditprmsPage_PostWords[5]);
            List<String> _list = new List<String>();
            _list.Add("Action");
            _list.Add(_spAction);
            _list.Add("User");
            _list.Add(_spUsers);
            _list.Add("Group");
            _list.Add(_spGroups);
            _list.Add("Permission");
            _list.Add(_spPermission);
            _list.Add("TargetLevel");
            _list.Add(_spTargetLevel);
            _list.Add("Object");
            _list.Add(_spObject);
            _list.Add("ChangedBy");
            _list.Add(_spChangedBy);
            return _list.ToArray();
        }

        String[] ProcessAdmin_UserPage(HttpRequest Request, String _spObject, String _spTargetLevel)
        {
            String[] pUserPage_PostWords = { };
            pUserPage_PostWords = SPEEvalInit._sUserPage_PostWords;
#if SP2013
            if (_SiteStyle == SiteStyle.Style2010)
            {
                pUserPage_PostWords = SPEEvalInit._sUserPage_PostWords_2010;
            }
#endif
            SPWeb _webObj = SPControl.GetContextWeb(HttpContext.Current);
            String _spAction = "";
            String _spUsers = "";
            String _spGroups = "";
            String _spChangedBy = "";

            //Get action type
            String _ButtonAction = Request.Form[pUserPage_PostWords[1]];
            if (_ButtonAction == null || !_ButtonAction.Equals(pUserPage_PostWords[2], StringComparison.OrdinalIgnoreCase))
            {
                //this is not a real post page action
                return null;
            }
            //Get action type
            _spAction = "RemoveUserGroup";
            //Get changed by user
            _spChangedBy = Request.ServerVariables[pUserPage_PostWords[3]];
            if (string.IsNullOrEmpty(_spChangedBy))
            {
                _spChangedBy = _webObj.CurrentUser.LoginName;
            }
            //Get Removed Items
            String _spItemsID = Request.Form[pUserPage_PostWords[4]];
            String[] _Items_list = _spItemsID.Split(new String[] { "," }, StringSplitOptions.None);
            int _GroupsFind = 0;
            int _UsersFind = 0;
            String _spGroupIDsFind = "";
            //Here we suppose group ID and user ID will not be confused, this is not a offical conclusion, just from experience
            for (int i = 0; i < _Items_list.Length; i++)
            {
                foreach (SPGroup group in _webObj.SiteGroups)
                {
                    if (group.ID.ToString().Equals(_Items_list[i]))
                    {
                        _spGroups += group.Name + ";";
                        _GroupsFind++;
                        _spGroupIDsFind += group.ID + ";";
                        break;
                    }
                }
            }
            for (int i = 0; i < _Items_list.Length; i++)
            {
                foreach (SPUser user in _webObj.SiteUsers)
                {
                    if (user.ID.ToString().Equals(_Items_list[i]))
                    {
                        _spUsers += user.Name + ";";
                        _UsersFind++;
                        break;
                    }
                }
            }
            //Fix bug 804, some groups are not in _webObj.AssociatedGroups but in _webObj.Groups
            if ((_GroupsFind + _UsersFind) < _Items_list.Length)
            {
                for (int i = 0; i < _Items_list.Length; i++)
                {
                    foreach (SPGroup group in _webObj.Groups)
                    {
                        if (group.ID.ToString().Equals(_Items_list[i]))
                        {
                            if (_spGroupIDsFind.IndexOf(group.ID.ToString()) > -1)
                            {
                                break;
                            }
                            else
                            {
                                _spGroups += group.Name + ";";
                                break;
                            }
                        }
                    }
                }
            }


            List<String> _list = new List<String>();
            _list.Add("Action");
            _list.Add(_spAction);
            _list.Add("User");
            _list.Add(_spUsers);
            _list.Add("Group");
            _list.Add(_spGroups);
            _list.Add("TargetLevel");
            _list.Add(_spTargetLevel);
            _list.Add("Object");
            _list.Add(_spObject);
            _list.Add("ChangedBy");
            _list.Add(_spChangedBy);
            return _list.ToArray();
        }

        String[] ProcessAdmin_AddrolePage(HttpRequest Request, String _spObject, String _spTargetLevel)
        {
            String[] pAddrolePage_PostWords = { };
            pAddrolePage_PostWords = SPEEvalInit._sAddrolePage_PostWords;
#if SP2013
            if (_SiteStyle == SiteStyle.Style2010)
            {
                pAddrolePage_PostWords = SPEEvalInit._sAddrolePage_PostWords_2010;
            }
#endif
            SPWeb _webObj = SPControl.GetContextWeb(HttpContext.Current);
            String _spAction = "";
            String _sp_List_Permissions = "";
            String _sp_Site_Permissions = "";
            String _sp_Personal_Permissions = "";
            String _spLevelName = "";
            String _spChangedBy = "";

            //Get action type
            String _ButtonAction = Request.Form[pAddrolePage_PostWords[1]];
            if (_ButtonAction == null || !_ButtonAction.EndsWith(pAddrolePage_PostWords[2], StringComparison.OrdinalIgnoreCase))
            {
                //this is not a real post page action
                return null;
            }
            //Get action type
            _spAction = "AddPermissionLevel";
            //Get changed by user
            _spChangedBy = Request.ServerVariables[pAddrolePage_PostWords[3]];
            if (string.IsNullOrEmpty(_spChangedBy))
            {
                _spChangedBy = _webObj.CurrentUser.LoginName;
            }
            //Get new level name
            _spLevelName = Request.Form[pAddrolePage_PostWords[4]];
            //Get Right Items
            String _spItemsID = Request.Form[pAddrolePage_PostWords[5]];
            String[] _Items_list = _spItemsID.Split(new String[] { "," }, StringSplitOptions.None);
            String _List_Permissions_List = "ManageLists + BreakCheckout + InsertListItems + EditListItems + DeleteListItems + ViewListItems + ApproveItems + OpenItems + ViewVersions + DeleteVersions + CreateAlerts + ViewFormPages";
            String _Site_Permissions_List = "ManagePermissions + ViewUsageData + ManageSubwebs + ManageWeb + WriteWebPages + ThemeWeb + LinkStyleSheet + CreatePersonalGroups + BrowseDirectories + CreateSscSite + ViewPages + EnumeratePermissions + BrowseUserInfo + ManageAlerts + UseRemoteAPIs + UseClientIntegration + OpenWeb + EditMyUserInfo";
            String _Personal_Permissions_List = "ManagePersonalViews + AddDelPrivateWebParts + UpdatePersonalWebParts";
            for (int i = 0; i < _Items_list.Length; i++)
            {
                if (_List_Permissions_List.IndexOf(_Items_list[i]) > -1)
                {
                    _sp_List_Permissions += _Items_list[i] + ";";
                }
                else if (_Site_Permissions_List.IndexOf(_Items_list[i]) > -1)
                {
                    _sp_Site_Permissions += _Items_list[i] + ";";
                }
                else if (_Personal_Permissions_List.IndexOf(_Items_list[i]) > -1)
                {
                    _sp_Personal_Permissions += _Items_list[i] + ";";
                }
            }
            List<String> _list = new List<String>();
            _list.Add("Action");
            _list.Add(_spAction);
            _list.Add("LevelName");
            _list.Add(_spLevelName);
            _list.Add("ListPermission");
            _list.Add(_sp_List_Permissions);
            _list.Add("SitePermission");
            _list.Add(_sp_Site_Permissions);
            _list.Add("PersonalPermission");
            _list.Add(_sp_Personal_Permissions);
            _list.Add("TargetLevel");
            _list.Add(_spTargetLevel);
            _list.Add("Object");
            _list.Add(_spObject);
            _list.Add("ChangedBy");
            _list.Add(_spChangedBy);
            return _list.ToArray();
        }

        String[] ProcessAdmin_RolePage(HttpRequest Request, String _spObject, String _spTargetLevel)
        {
            String[] pRolePage_PostWords = { };
            pRolePage_PostWords = SPEEvalInit._sRolePage_PostWords;
#if SP2013
            if (_SiteStyle == SiteStyle.Style2010)
            {
                pRolePage_PostWords = SPEEvalInit._sRolePage_PostWords_2010;
            }
#endif
            SPWeb _webObj = SPControl.GetContextWeb(HttpContext.Current);
            String _spAction = "";
            String _spLevelName = "";
            String _spChangedBy = "";

            //Get action type
            String _ButtonAction = Request.Form[pRolePage_PostWords[1]];
            if (_ButtonAction == null || !_ButtonAction.Equals(pRolePage_PostWords[2], StringComparison.OrdinalIgnoreCase))
            {
                //this is not a real post page action
                return null;
            }
            //Get action type
            _spAction = "DeletePermissionLevel";
            //Get changed by user
            _spChangedBy = Request.ServerVariables[pRolePage_PostWords[3]];
            if (string.IsNullOrEmpty(_spChangedBy))
            {
                _spChangedBy = _webObj.CurrentUser.LoginName;
            }
            //Get new level name
            String _spLevel_ID = Request.Form[pRolePage_PostWords[4]];
            //Fix bug 815, there could be multiply roles that are being deleted
            String[] _spLevel_ID_Array = _spLevel_ID.Split(new String[] { "," }, StringSplitOptions.None);
            for (int i = 0; i < _spLevel_ID_Array.Length; i++)
            {
                foreach (SPRole role in _webObj.Roles)
                {
                    if (role.ID.ToString().Equals(_spLevel_ID_Array[i]))
                    {
                        _spLevelName += role.Name + ";";
                        break;
                    }
                }
            }
            List<String> _list = new List<String>();
            _list.Add("Action");
            _list.Add(_spAction);
            _list.Add("LevelName");
            _list.Add(_spLevelName);
            _list.Add("TargetLevel");
            _list.Add(_spTargetLevel);
            _list.Add("Object");
            _list.Add(_spObject);
            _list.Add("ChangedBy");
            _list.Add(_spChangedBy);
            return _list.ToArray();
        }

        String[] ProcessAdmin_EditRolePage(HttpRequest Request, String _spObject, String _spTargetLevel)
        {
            String[] pEditRolePage_PostWords = { };
            pEditRolePage_PostWords = SPEEvalInit._sEditRolePage_PostWords;
#if SP2013
            if (_SiteStyle == SiteStyle.Style2010)
            {
                pEditRolePage_PostWords = SPEEvalInit._sEditRolePage_PostWords_2010;
            }
#endif
            SPWeb _webObj = SPControl.GetContextWeb(HttpContext.Current);
            String _spAction = "";
            String _sp_OldList_Permissions = "";
            String _sp_OldSite_Permissions = "";
            String _sp_OldPersonal_Permissions = "";
            String _sp_NewList_Permissions = "";
            String _sp_NewSite_Permissions = "";
            String _sp_NewPersonal_Permissions = "";
            String _spOldLevelName = "";
            String _spNewLevelName = "";
            String _spChangedBy = "";

            //Get action type
            String _ButtonAction = Request.Form[pEditRolePage_PostWords[1]];
            if (_ButtonAction == null || !_ButtonAction.EndsWith(pEditRolePage_PostWords[2], StringComparison.OrdinalIgnoreCase))
            {
                //this is not a real post page action
                return null;
            }
            //Get action type
            _spAction = "EditPermissionLevel";
            //Get changed by user
            _spChangedBy = Request.ServerVariables[pEditRolePage_PostWords[3]];
            if (string.IsNullOrEmpty(_spChangedBy))
            {
                _spChangedBy = _webObj.CurrentUser.LoginName;
            }
            //Get old level name
            _spOldLevelName = Request.Form[pEditRolePage_PostWords[4]];
            //Get new level name
            _spNewLevelName = Request.Form[pEditRolePage_PostWords[5]];
            //Get Right Items
            String _spItemsID = Request.Form[pEditRolePage_PostWords[6]];

            SPBasePermissions[] _List_Permissions = { SPBasePermissions.ManageLists, SPBasePermissions.CancelCheckout, SPBasePermissions.AddListItems,
                                                    SPBasePermissions.EditListItems,SPBasePermissions.DeleteListItems,SPBasePermissions.ViewListItems,SPBasePermissions.ApproveItems,
                                                    SPBasePermissions.OpenItems,SPBasePermissions.ViewVersions,SPBasePermissions.DeleteVersions,SPBasePermissions.CreateAlerts,SPBasePermissions.ViewFormPages};
            SPBasePermissions[] _Site_Permissions = { SPBasePermissions.ManagePermissions, SPBasePermissions.ViewUsageData, SPBasePermissions .ManageSubwebs,
                                                    SPBasePermissions.ManageWeb,SPBasePermissions.AddAndCustomizePages,SPBasePermissions.ApplyThemeAndBorder,
                                                    SPBasePermissions.ApplyStyleSheets,SPBasePermissions.CreateGroups,SPBasePermissions.BrowseDirectories,SPBasePermissions.CreateSSCSite,
                                                    SPBasePermissions.ViewPages,SPBasePermissions.EnumeratePermissions,SPBasePermissions.BrowseUserInfo,SPBasePermissions.ManageAlerts,
                                                    SPBasePermissions.UseRemoteAPIs,SPBasePermissions.UseClientIntegration,SPBasePermissions.Open,SPBasePermissions.EditMyUserInfo};
            SPBasePermissions[] _Personal_Permissions = { SPBasePermissions.ManagePersonalViews,
                                                        SPBasePermissions.AddDelPrivateWebParts,
                                                        SPBasePermissions.UpdatePersonalWebParts};
            SPRoleDefinition _spRoleDe = _webObj.RoleDefinitions[_spOldLevelName];
            String[] _spList_Permissions_List = { "ManageLists", "BreakCheckout", "InsertListItems", "EditListItems", "DeleteListItems", "ViewListItems", "ApproveItems", "OpenItems", "ViewVersions", "DeleteVersions", "CreateAlerts", "ViewFormPages" };
            String[] _spSite_Permissions_List = { "ManagePermissions", "ViewUsageData", "ManageSubwebs", "ManageWeb", "WriteWebPages", "ThemeWeb", "LinkStyleSheet", "CreatePersonalGroups", "BrowseDirectories", "CreateSscSite", "ViewPages", "EnumeratePermissions", "BrowseUserInfo", "ManageAlerts", "UseRemoteAPIs", "UseClientIntegration", "OpenWeb", "EditMyUserInfo" };
            String[] _spPersonal_Permissions_List = { "ManagePersonalViews", "AddDelPrivateWebParts", "UpdatePersonalWebParts" };
            for (int i = 0; i < _List_Permissions.Length; i++)
            {
                if ((_spRoleDe.BasePermissions & _List_Permissions[i]) > 0)
                {
                    _sp_OldList_Permissions += _spList_Permissions_List[i] + ";";
                }
            }
            for (int i = 0; i < _Site_Permissions.Length; i++)
            {
                if ((_spRoleDe.BasePermissions & _Site_Permissions[i]) > 0)
                {
                    _sp_OldSite_Permissions += _spSite_Permissions_List[i] + ";";
                }
            }
            for (int i = 0; i < _Personal_Permissions.Length; i++)
            {
                if ((_spRoleDe.BasePermissions & _Personal_Permissions[i]) > 0)
                {
                    _sp_OldPersonal_Permissions += _spPersonal_Permissions_List[i] + ";";
                }
            }

            String[] _Items_list = _spItemsID.Split(new String[] { "," }, StringSplitOptions.None);
            String _List_Permissions_List = "ManageLists + BreakCheckout + InsertListItems + EditListItems + DeleteListItems + ViewListItems + ApproveItems + OpenItems + ViewVersions + DeleteVersions + CreateAlerts + ViewFormPages";
            String _Site_Permissions_List = "ManagePermissions + ViewUsageData + ManageSubwebs + ManageWeb + WriteWebPages + ThemeWeb + LinkStyleSheet + CreatePersonalGroups + BrowseDirectories + CreateSscSite + ViewPages + EnumeratePermissions + BrowseUserInfo + ManageAlerts + UseRemoteAPIs + UseClientIntegration + OpenWeb + EditMyUserInfo";
            String _Personal_Permissions_List = "ManagePersonalViews + AddDelPrivateWebParts + UpdatePersonalWebParts";
            for (int i = 0; i < _Items_list.Length; i++)
            {
                if (_List_Permissions_List.IndexOf(_Items_list[i]) > -1)
                {
                    _sp_NewList_Permissions += _Items_list[i] + ";";
                }
                else if (_Site_Permissions_List.IndexOf(_Items_list[i]) > -1)
                {
                    _sp_NewSite_Permissions += _Items_list[i] + ";";
                }
                else if (_Personal_Permissions_List.IndexOf(_Items_list[i]) > -1)
                {
                    _sp_NewPersonal_Permissions += _Items_list[i] + ";";
                }
            }
            List<String> _list = new List<String>();
            _list.Add("Action");
            _list.Add(_spAction);
            _list.Add("OldLevelName");
            _list.Add(_spOldLevelName);
            _list.Add("NewLevelName");
            _list.Add(_spNewLevelName);
            _list.Add("OldListPermission");
            _list.Add(_sp_OldList_Permissions);
            _list.Add("OldSitePermission");
            _list.Add(_sp_OldSite_Permissions);
            _list.Add("OldPersonalPermission");
            _list.Add(_sp_OldPersonal_Permissions);
            _list.Add("NewListPermission");
            _list.Add(_sp_NewList_Permissions);
            _list.Add("NewSitePermission");
            _list.Add(_sp_NewSite_Permissions);
            _list.Add("NewPersonalPermission");
            _list.Add(_sp_NewPersonal_Permissions);
            _list.Add("TargetLevel");
            _list.Add(_spTargetLevel);
            _list.Add("Object");
            _list.Add(_spObject);
            _list.Add("ChangedBy");
            _list.Add(_spChangedBy);
            return _list.ToArray();
        }

    }
}
#pragma warning restore 618
