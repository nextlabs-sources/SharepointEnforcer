using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.SharePoint;
using System.Collections.Specialized;

namespace Nextlabs.SPE.Console
{
    /// <summary>
    /// Add By ROY
    /// </summary>
    class PLECommand : ISpeAdminCommand
    {

        private string cmdArg;
        private string target;

        public string GetHelpString(string feature)
        {
            string help = String.Empty;
            help = @"ce_spadmin -o ple -enable/disable -url url-refer-to-a-site-collection";
            return help;
        }

        public void Run(string feature, StringDictionary keyValues)
        {
            bool bBadOperation = false;
         
            target = keyValues["-url"];
            try
            {
                if (keyValues["-enable"] == null && keyValues["-disable"] == null)
                {
                    bBadOperation = true;
                }
                else if(string.IsNullOrEmpty(keyValues["-enable"]))
                {
                    cmdArg = "enable";
                }
                else if (string.IsNullOrEmpty(keyValues["-disable"]))
                {
                    cmdArg = "disable";
                }
                if (target == null)
                {
                    bBadOperation = true;
                }
                if (!bBadOperation)
                {
                    Process();
                }
            }
            catch (Exception exp)
            {
                System.Diagnostics.Trace.WriteLine("Exception: " + exp.Message);
            }

            if (bBadOperation)
            {
                throw new InvalidOperationException("Unsupported arguments for webparttrimming operation.");
            }

        }

        protected void Process()
        {
            try
            {

                using (SPSite _site1 = new SPSite(target))
                {
                    using (SPWeb _web1 = _site1.RootWeb)
                    {
                        _web1.Properties["spepleswitch"] = cmdArg;
                        _web1.Properties.Update();
                    }
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Trace.WriteLine("New SPSite failed:" + e.Message);
            }
        }

    }
}
