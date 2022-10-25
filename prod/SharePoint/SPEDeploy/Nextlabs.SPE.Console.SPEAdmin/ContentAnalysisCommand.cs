using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.Specialized;
using Microsoft.SharePoint;
using Microsoft.SharePoint.Utilities;
using Microsoft.SharePoint.Administration;
namespace Nextlabs.SPE.Console
{
    public class ContentAnalysisCommand : ISpeAdminCommand
    {
        enum CACommand
        {
            CACommandUnknown = 0,
            CACommandClearSchedule
        }

        public ContentAnalysisCommand()
        {
        }

        public string GetHelpString(string feature)
        {
            string help = "";

            help = "\nCE_SPAdmin.exe -o contentanalysis {-clearschedule}\n";
            help += "\nOptions: \n";
            help += "\t -clearschedule: Clear all content analysis schedule related content in SharePoint.\n";
            
            return help;
        }

        public void Run(string feature, StringDictionary keyValues)
        {
            bool bBadOperation = false;

            try
            {
                if (keyValues.ContainsKey("-clearschedule"))
                {
                }
                else
                {
                    bBadOperation = true;
                }

                if (!bBadOperation)
                    Process();
            }
            catch (Exception exp)
            {
                System.Diagnostics.Trace.WriteLine("Exception: " + exp.Message);
            }

            if (bBadOperation)
            {
                throw new InvalidOperationException("Unsupported arguments for contentanalysis operation.");
            }
        }

        protected void Process()
        {
            foreach (SPService service in SPFarm.Local.Services)
            {
                try
                {
                    if (service is SPWebService)
                    {
                        SPWebService webService = (SPWebService)service;
                        foreach (SPWebApplication webapp in webService.WebApplications)
                        {
                            try
                            {
                                SPJobDefinitionCollection jobs = webapp.JobDefinitions;
                                foreach (SPJobDefinition job in jobs)
                                {
                                    try
                                    {
                                        if (job.Name.StartsWith("CAJob_"))
                                        {
                                            job.Delete();
                                        }
                                    }
                                    catch
                                    { 
                                    }
                                }

                                foreach (SPSite site in webapp.Sites)
                                {
                                    try
                                    {
                                        foreach (SPWeb web in site.AllWebs)
                                        {
                                            try
                                            {
                                                foreach (SPList list in web.Lists)
                                                {
                                                    if (list.RootFolder.Properties.Contains("ca_schIndex"))
                                                    {
                                                        list.RootFolder.Properties.Remove("ca_schIndex");
                                                        list.RootFolder.Properties.Remove("ca_schedules");
                                                        list.RootFolder.Update();
                                                    }
                                                }
                                            }
                                            catch
                                            {
                                            }
                                            finally
                                            {
                                                if (web != null)
                                                    web.Dispose();
                                            }
                                        }
                                    }
                                    catch
                                    {
                                    }
                                    finally
                                    {
                                        if (site != null)
                                            site.Dispose();
                                    }
                                }
                            }
                            catch
                            {
                            }
                        }
                    }
                }
                catch
                { 
                }
            }
        }
    }
}
