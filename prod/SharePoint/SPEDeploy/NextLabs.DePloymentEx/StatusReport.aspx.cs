using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Web.UI.WebControls;
using Microsoft.SharePoint.WebControls;
using Microsoft.SharePoint.Utilities;
using Microsoft.SharePoint;
using Microsoft.SharePoint.Administration;
using Microsoft.Win32;
using Microsoft.Office.Server.Search.Administration;
using System.ServiceProcess;
using NextLabs.Common;
using NextLabs.Diagnostic;

namespace NextLabs.Deployment
{
    public class StatusReportPage : LayoutsPageBase
    {
        protected SPGridView SPGridView1;

        public class NLStatusReportJob : SPJobDefinition
        {
            public static string JobName { get { return "NLStatusReportJob"; } }
            public static string JobTitle { get { return "The reporting of nextlabs product status"; } }

            public string ProductVersion
            {
                get { return this.Properties["ProductVersion"] as string; }
                set { this.Properties["ProductVersion"] = value; }
            }

            public string ServerStatus
            {
                get { return this.Properties["ServerStatus"] as string; }
                set { this.Properties["ServerStatus"] = value; }
            }

            /// <summary>
            /// Creates the job instance
            /// </summary>
            public NLStatusReportJob()
                : base(JobName, SPFarm.Local.TimerService, null, SPJobLockType.None)
            {
            }

            /// <summary>
            /// A description of the job being ran
            /// </summary>
            public override string Description
            {
                get
                {
                    return "The reporting of nextlabs product status";
                }
            }

            /// <summary>
            /// The name to display for the job.
            /// </summary>
            public override string DisplayName
            {
                get
                {
                    return "Nextlabs product status";
                }
            }

            /// <summary>
            /// Called by SharePoint when this job is ran.
            /// </summary>
            /// <param name="targetInstance">The target instance IDfor the job</param>
            public override void Execute(Guid targetInstance)
            {
                CommonLib utility = new CommonLib();
                try
                {
                    SPFarm farm = SPFarm.Local;
                    string Servername = SPServer.Local.Name;
                    string name = "Nextlabs_SPE_";
                    string status = "";
                    name += Servername;
                    if (utility.IsSPEInstalled())
                    {
                        status = "installed";
                        if (utility.IsDeployOnThisServer())
                        {
                            status = "deployed";
                        }
                        status += "#";
                        status += utility.GetSPEVersion();
                    }

                    if (!farm.Properties.ContainsKey(name))
                    {
                        farm.Properties.Add(name, status);
                    }
                    else
                    {
                        farm.Properties[name] = status;
                    }
                    farm.Update();
                }
                catch (Exception ex)
                {
                    NLLogger.OutputLog(LogLevel.Error, $"Exception during {SPServer.Local.Name},NextLabs--NLStatusReportJob--Excute:", null, ex);
                }

            }

            /// <summary>
            /// This method sets up the timer to fire.  This is the method after the logging categories are
            /// setup that an application can call to register the event sources.
            /// </summary>
            public static void ScheduleJob()
            {
                CleanUpJobs(SPFarm.Local.TimerService.JobDefinitions);
                var job = new NLStatusReportJob();
                job.Schedule = new SPOneTimeSchedule(GetImmediateJobTime());
                job.Update();
            }

            protected static DateTime GetImmediateJobTime()
            {
                return DateTime.Now - TimeSpan.FromDays(1);
            }
            /// <summary>
            /// Cleans up and old versions found of the job.
            /// </summary>
            /// <param name="jobs">The job list.</param>
            private static void CleanUpJobs(SPJobDefinitionCollection jobs)
            {
                foreach (SPJobDefinition job in jobs)
                {
                    if (job.Name.Equals(NLStatusReportJob.JobName, StringComparison.OrdinalIgnoreCase))
                    {
                        job.Delete();
                    }
                }
            }
        }

        public StatusReportPage()
        {
            SPGridView1 = new SPGridView();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            if (!Page.IsPostBack)
            {
                if (!IsCentralAdmin())
                {
                    return;
                }

                SPContext.Current.Web.AllowUnsafeUpdates = true;
                NLStatusReportJob.ScheduleJob();

                SPBoundField boundField = new SPBoundField();
                boundField.HeaderText = "Server";
                boundField.DataField = "Server";
                SPGridView1.Columns.Add(boundField);

                boundField = new SPBoundField();
                boundField.HeaderText = "Status";
                boundField.DataField = "Status";
                SPGridView1.Columns.Add(boundField);

                boundField = new SPBoundField();
                boundField.HeaderText = "Version";
                boundField.DataField = "Version";
                SPGridView1.Columns.Add(boundField);


                SPGridView1.AllowSorting = true;
                SPGridView1.HeaderStyle.Font.Bold = true;

                DataTable table = new DataTable();
                table.Columns.Add("Server", typeof(string));
                table.Columns.Add("Status", typeof(string));
                table.Columns.Add("Version", typeof(string));

                DataRow newRow;
                try
                {
                    SPFarm farm = SPFarm.Local;
                    foreach (DictionaryEntry de in farm.Properties)
                    {
                        string name = de.Key.ToString();
                        if (name.Contains("Nextlabs_SPE_"))
                        {
                            string value = de.Value.ToString();
                            newRow = table.Rows.Add();
                            newRow["Server"] = name.Substring(13);
                            if (value.Contains("installed"))
                            {
                                newRow["Status"] = "installed";
                            }
                            else
                            {
                                newRow["Status"] = "deployed";
                            }

                            string[] strs = value.Split(new char[] { '#' });
                            newRow["Version"] = strs[1];
                        }
                    }
                }
                catch (Exception ex)
                {
                    NLLogger.OutputLog(LogLevel.Error, "Exception during NextLabs--StatusReport--OnLoad:", null, ex);
                }

                SPGridView1.AutoGenerateColumns = false;
                SPGridView1.DataSource = table.DefaultView;
                SPGridView1.DataBind();
            }

        }

        private bool IsCentralAdmin()
        {
            return SPContext.Current.Site.WebApplication.IsAdministrationWebApplication;
        }

        protected void cmdDeleteAllEntires_Click(object sender, EventArgs e)
        {
            if (!IsCentralAdmin())
            {
                return;
            }
            try
            {
                CommonLib utility = new CommonLib();
                SPFarm farm = SPFarm.Local;
                Hashtable dics = farm.Properties;
                foreach (DictionaryEntry de in dics)
                {
                    string name = de.Key.ToString();
                    if (name.Contains("Nextlabs_SPE_"))
                    {
                        farm.Properties.Remove(de.Key);
                    }
                }
                farm.Update();
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Debug, "Exception during cmdDeleteAllEntires_Click:", null, ex);
            }
            SPUtility.Redirect(this.Context.Request.Url.ToString(), SPRedirectFlags.Trusted, this.Context);
        }

    }
}
