using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.Specialized;
using Microsoft.SharePoint;
using Microsoft.SharePoint.Administration;
using Microsoft.Office.Server;
using Microsoft.Office.Server.Search.Administration;
using Microsoft.Office.Server.Search.Administration.Security;
using NextLabs.Common;

namespace Nextlabs.SPE.Console
{
    class SearchResultTrimmingCommand : ISpeAdminCommand
    {
        enum SrtCommand
        {
            SrtCommandUnknown = 0,
            SrtCommandInstall = 1,
            SrtCommandUninstall = 2
        }

        private SrtCommand Command;
        private String SsaName;
        private int Id;
        private String RulePath;

        public SearchResultTrimmingCommand()
        {
            Command = SrtCommand.SrtCommandUnknown;
            Id = 100;
        }

        public string GetHelpString(string feature)
        {
            string help = "";

            help = "\nCE_SPAdmin.exe -o searchresulttrimming {-install | -uninstall} {-ssa <Search Service Application Name} [-id <INT>] [-rulepath <Crawl rule URL>]";

            return help;
        }

        public void Run(string feature, StringDictionary keyValues)
        {
            SsaName = keyValues["-ssa"];
            RulePath = keyValues["-rulepath"];

            if (String.IsNullOrEmpty(SsaName))
                throw new InvalidOperationException("Search Service Application Name should be specified for searchresulttrimming operation.");

            if (keyValues.ContainsKey("-id"))
                Id = int.Parse(keyValues["-id"]);

            if (keyValues.ContainsKey("-install"))
            {
                Command = SrtCommand.SrtCommandInstall;
            }
            else if (keyValues.ContainsKey("-uninstall"))
            {
                Command = SrtCommand.SrtCommandUninstall;
            }

            SPTrimming searchResultTrimming = new SPTrimming();
            switch (Command)
            {
                case SrtCommand.SrtCommandInstall:
                    searchResultTrimming.InstallSearchResultTrimming(SsaName, Id, RulePath);
                    break;
                case SrtCommand.SrtCommandUninstall:
                    searchResultTrimming.UninstallSearchResultTrimming(SsaName, Id, RulePath);
                    break;
                default:
                    throw new InvalidOperationException("Unsupported arguments for searchresulttrimming operation.");
            }
        }
    }
}
