using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Collections.Specialized;
using Microsoft.SharePoint.Administration;
using Microsoft.Office.Server;
using Microsoft.Office.Server.Search.Administration;
using Microsoft.Office.Server.Search.Administration.Security;
using NextLabs.Diagnostic;

namespace NextLabs.Common
{
    public class SPTrimming
    {

        public bool IsInstalledOnURL(String SsaName, ref int Id, String RulePath)
        {
            Id = -1;
            try
            {
                SPFarm localFarm = SPFarm.Local;
#if SP2016 || SP2019
                SearchService searchService = localFarm.Services.GetValue<SearchService>("OSearch16");
#elif SP2013
                SearchService searchService = localFarm.Services.GetValue<SearchService>("OSearch15");
#elif SP2010
                SearchService searchService = localFarm.Services.GetValue<SearchService>("OSearch14");
#endif

                SearchServiceApplication searchApp;

                if (!String.IsNullOrEmpty(SsaName))
                {
                    searchApp = searchService.SearchApplications.GetValue<SearchServiceApplication>(SsaName);
                }
                else
                {
                    return false;
                }

                Content content = new Content(searchApp);
                if (!String.IsNullOrEmpty(RulePath))
                {
                    CrawlRule crawRule;
                    if (content.CrawlRules.Exists(RulePath))
                    {
                        crawRule = content.CrawlRules.Test(RulePath);
                    }
                    else
                    {
                        return false;
                    }

                    if (crawRule != null)
                    {
                        int trimmerid = crawRule.PluggableSecurityTrimmerId;
                        if (0 <= trimmerid && trimmerid <= 214783647)
                        {
                            Id = trimmerid;
                            return true;
                        }

                    }
                }
            }
            catch
            {
                return false;
            }
            return false;
        }

        public int InstallSearchResultTrimming(String SsaName, int Id, String RulePath)
        {
            int ret = 0;
            try
            {
                SPFarm localFarm = SPFarm.Local;
#if SP2016 || SP2019
                SearchService searchService = localFarm.Services.GetValue<SearchService>("OSearch16");
#elif SP2013
                SearchService searchService = localFarm.Services.GetValue<SearchService>("OSearch15");
#elif SP2010
                SearchService searchService = localFarm.Services.GetValue<SearchService>("OSearch14");
#endif
                SearchServiceApplication searchApp;

                if (!String.IsNullOrEmpty(SsaName))
                {
                    searchApp = searchService.SearchApplications.GetValue<SearchServiceApplication>(SsaName);
                }
                else
                {
                    searchApp = searchService.SearchApplications.GetValue<SearchServiceApplication>();
                }

                PluggableSecurityTrimmerManager manager = PluggableSecurityTrimmerManager.Instance;
                manager.SetSearchApplicationToUse(searchApp);
                string fullyQualifiedTypeName = "Nextlabs.SPSecurityTrimming.SPSearchResultTrimmer, Nextlabs.SPSecurityTrimming, Version=3.0.0.0, Culture=neutral, PublicKeyToken=7030e9011c5eb860";
#if SP2013 || SP2016 || SP2019
                manager.RegisterPluggableSecurityTrimmer(Id, fullyQualifiedTypeName, new NameValueCollection(),false);
#else
                manager.RegisterPluggableSecurityTrimmer(Id, fullyQualifiedTypeName, new NameValueCollection());
#endif

                Content content = new Content(searchApp);

                if (!String.IsNullOrEmpty(RulePath))
                {
                    CrawlRule crawRule;

                    if (content.CrawlRules.Exists(RulePath))
                    {
                        crawRule = content.CrawlRules.Test(RulePath);
                    }
                    else
                    {
                        crawRule = content.CrawlRules.Create(CrawlRuleType.InclusionRule, RulePath);
                    }

                    if (crawRule != null)
                    {
                        crawRule.PluggableSecurityTrimmerId = Id;
                        crawRule.Update();
                    }
                }
                else
                {
                    CrawlRuleCollection rules = content.CrawlRules;
                    foreach (CrawlRule crawRule in rules)
                    {
                        crawRule.PluggableSecurityTrimmerId = Id;
                        crawRule.Update();
                    }
                }

                // Start to full crawl all content sources
                ContentSourceCollection contentSources = content.ContentSources;
                foreach (ContentSource source in contentSources)
                {
                    if (source.CrawlStatus != CrawlStatus.Idle)
                        source.StopCrawl();

                    source.StartFullCrawl();
                }
            }
            catch(Exception ex)
            {
                ret = -1;
                NLLogger.OutputLog(LogLevel.Debug, "Exception during InstallSearchResultTrimming:", null, ex);
            }
            return ret;
        }
        public int UninstallSearchResultTrimming(String SsaName, int Id, String RulePath)
        {
            int ret = 0;
            try
            {
                SPFarm localFarm = SPFarm.Local;
#if SP2016 || SP2019
                SearchService searchService = localFarm.Services.GetValue<SearchService>("OSearch16");
#elif SP2013
                SearchService searchService = localFarm.Services.GetValue<SearchService>("OSearch15");
#elif SP2010
                SearchService searchService = localFarm.Services.GetValue<SearchService>("OSearch14");
#endif
                SearchServiceApplication searchApp;

                if (!String.IsNullOrEmpty(SsaName))
                {
                    searchApp = searchService.SearchApplications.GetValue<SearchServiceApplication>(SsaName);
                }
                else
                {
                    searchApp = searchService.SearchApplications.GetValue<SearchServiceApplication>();
                }

                PluggableSecurityTrimmerManager manager = PluggableSecurityTrimmerManager.Instance;
                manager.SetSearchApplicationToUse(searchApp);
                manager.UnregisterPluggableSecurityTrimmer(Id);

                Content content = new Content(searchApp);

                CrawlRuleCollection rules = content.CrawlRules;
                foreach (CrawlRule crawRule in rules)
                {
                    if (crawRule.PluggableSecurityTrimmerId == Id)
                        crawRule.PluggableSecurityTrimmerId = -1;
                    crawRule.Update();
                }

                // Start to full crawl all content sources
                ContentSourceCollection contentSources = content.ContentSources;
                foreach (ContentSource source in contentSources)
                {
                    if (source.CrawlStatus != CrawlStatus.Idle)
                        source.StopCrawl();

                    source.StartFullCrawl();
                }
            }
            catch(Exception ex)
            {
                ret = -1;
            }
            return ret;
        }
    }
}
