using System;
using System.Collections.Generic;
using System.Text;
using NextLabs.Diagnostic;
using System.Diagnostics;
using NextLabs.CSCInvoke;
using System.Net;
namespace NextLabs.PLE.Log
{
    public class PLE_ReportAdminObligationLog
    {
        public void DoReportLog(IntPtr handler, string[] obligation, string sitevalue,string[] admin_log)
        {
            int count = 0;
            CETYPE.CEResult_t call_result;
            string logIdentifier = null;
            string assistantName = "SharePoint Enforcer";
            int ob_start = 0;
            List<string> attr = null;
            if (obligation == null)
                return;
            try
            {
                for (count = 0; count < obligation.Length; count += 2)
                {
                    if (obligation[count] != null && obligation[count].IndexOf("CE_ATTR_OBLIGATION_NAME") != -1)
                    {
                        if (obligation[count + 1] != null && obligation[count + 1].Equals("SP_PERMISSION_AUDIT", StringComparison.OrdinalIgnoreCase))
                        {
                            //Fix bug 8399, added by William 20090203
                            assistantName = obligation[count + 1];
                            ob_start = count + 4;
                            attr = new List<string>();
                            for (; ob_start < obligation.Length; ob_start += 2)
                            {
                                {
                                    if (obligation[ob_start] != null && obligation[ob_start].IndexOf("CE_ATTR_OBLIGATION_VALUE") != -1)
                                    {
                                        if (obligation[ob_start + 1] != null && obligation[ob_start + 1].Equals("LogId"))
                                        {
                                            ob_start += 2;
                                            if (ob_start < obligation.Length && obligation[ob_start] != null && obligation[ob_start].IndexOf("CE_ATTR_OBLIGATION_VALUE") != -1)
                                            {
                                                logIdentifier = obligation[ob_start + 1];
                                            }
                                        }
                                    }
                                    else if (obligation[ob_start] != null && obligation[ob_start].IndexOf("CE_ATTR_OBLIGATION_NUMVALUES") != -1)
                                    {
                                        String _Name = "";
                                        String _Value = "";
                                        string[] attr_value = new String[6];
                                        int i = 2;
                                        attr_value[0] = "Log Obligation Name";
                                        attr_value[1] = "Log Obligation Name : SP_PERMISSION_AUDIT";
                                        attr_value[2] = admin_log[0];
                                        attr_value[3] = admin_log[0] + ":" + admin_log[1];
                                        int _fixedlength = attr_value[0].Length + attr_value[1].Length + attr_value[2].Length + attr_value[3].Length;
                                        while (i < admin_log.Length)
                                        {
                                            _Name = "";
                                            _Value = "";
                                            if (admin_log[i].Length > (1024) || (admin_log[i].Length + admin_log[i + 1].Length + 5) > (1024 - _fixedlength))
                                            {
                                                break;
                                            }
                                            for (; i < admin_log.Length; i += 2)
                                            {
                                                if ((_Name.Length + _Value.Length + admin_log[i].Length + admin_log[i + 1].Length + 5) <= (1024 - _fixedlength))
                                                {
                                                    _Name += admin_log[i] + "\r\n";
                                                    _Value += admin_log[i] + " : " + admin_log[i + 1] + "\r\n";
                                                }
                                                else
                                                {
                                                    break;
                                                }
                                            }
                                            attr_value[4] = _Name;
                                            attr_value[5] = _Value;
                                            call_result = CESDKAPI.CELOGGING_LogObligationData(handler, logIdentifier, assistantName, ref attr_value);
                                        }
                                        ob_start += 2;
                                        count = ob_start-2;
                                        break;
                                    }
                                }

                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Warn, "Exception during PLEHttpEnforcerModule ReportAdminObligation:", null, ex);
            }
        }
    }
}
