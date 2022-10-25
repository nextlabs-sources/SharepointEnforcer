using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Threading;
using System.Diagnostics;
using Microsoft.SharePoint;
using Microsoft.SharePoint.Administration;
using NextLabs.Common;
using System.Web;
using NextLabs.Diagnostic;

namespace NextLabs.SPEnforcer
{
    public enum RunLevel
    {
        RLListLevel,
        RLSiteLevel
    }
    public class ContentAnalysisJob : SPJobDefinition
    {
        static public String JobNamePrefix = "CAJob_";

        public string WebUrl
        {
            get { return this.Properties["WebUrl"] as string; }
            set { this.Properties["WebUrl"] = value; }
        }

        public string UserName
        {
            get { return this.Properties["UserName"] as string; }
            set { this.Properties["UserName"] = value; }
        }

        public string UserSid
        {
            get { return this.Properties["UserSid"] as string; }
            set { this.Properties["UserSid"] = value; }
        }

        public string ListGuid
        {
            get { return this.Properties["ListGuid"] as string; }
            set { this.Properties["ListGuid"] = value; }
        }

        public string ClientIp
        {
            get { return this.Properties["ClientIp"] as string; }
            set { this.Properties["ClientIp"] = value; }
        }

        public string IsMinutesJob
        {
            get { return this.Properties["IsMinutesJob"] as string; }
            set { this.Properties["IsMinutesJob"] = value; }
        }

        public string strRunLevel
        {
            get { return this.Properties["RunLevel"] as string; }
            set { this.Properties["RunLevel"] = value; }
        }
        public string strID
        {
            get { return this.Properties["strID"] as string; }
            set { this.Properties["strID"] = value; }
        }

        public ContentAnalysisJob()
            : base()
        {
        }

        public ContentAnalysisJob(SPWebApplication app, string webUrl, string userName, string userSid, string listGuid, string ip, string isMinutesJob, RunLevel level=RunLevel.RLListLevel, string strid=null)
            : base(JobNamePrefix + listGuid + isMinutesJob, app, null, SPJobLockType.Job)
        {
            this.Title = JobNamePrefix + listGuid + isMinutesJob;

            WebUrl = webUrl;
            UserName = userName;
            UserSid = userSid;
            ListGuid = listGuid;
            ClientIp = ip;
            IsMinutesJob = isMinutesJob;

            strRunLevel = level.ToString();
            if (level == RunLevel.RLSiteLevel)
            {
                strID = strid;
            }
        }
        protected override bool HasAdditionalUpdateAccess()
        {
            bool result = true;
            //System.Security.Principal.IIdentity userIdentity = HttpContext.Current.User.Identity;
            //string username = Globals.GetCurrentUser(userIdentity);
            //bool isAdmin = Globals.IsFarmAdministrator(username);
            //get user name, judge this user is farm administartor or not, if this user isn't farm administrator, return false
            //if return false, SPJobDefinition.execute will not be called, and will cause deny access error
            NLLogger.OutputLog(LogLevel.Debug, String.Format("current user name is: {0}, if user isn't farm administrator cannot edit configuration database", this.UserName));
            return result;
        }
        private bool ProcessSiteTimerTask()
        {
            bool bRet = false;
            using (SPSite Site = Globals.GetValidSPSite(WebUrl, HttpContext.Current))
            {
                string strState = string.Empty;
                using (SPWeb Web = Site.OpenWeb())
                {
                    strState = Globals.GetSiteProperty(Web, Globals.strSiteProcessStatePropName);
                }

                if (string.IsNullOrEmpty(strState) || !strState.Equals(Globals.strSiteCNMStatePropValue_Processing))
                {//can start the cnm, run worker thread...
                    bRet = true;
                    //Thread currentThread = Thread.CurrentThread;

                    using (AutoResetEvent autoEvent = new AutoResetEvent(false))
                    {
                        CNMWorker worker = new CNMWorker(WebUrl, UserName, UserSid, ListGuid, ClientIp, strID);
                        Thread workerThread = new Thread(worker.WorkerRun);
                        workerThread.Start(autoEvent);
                        autoEvent.WaitOne();
                    }
                }
                RunLevel rl = RunLevel.RLSiteLevel;
                UpdateTimerThread updateWorker = new UpdateTimerThread(this.WebApplication, WebUrl, UserName, UserSid, ListGuid, ClientIp, rl, int.Parse(strID));
                Thread updateThread = new Thread(updateWorker.Run);
                updateThread.Start();

            }

            return bRet;
        }

        public override void Execute(Guid targetInstanceId)
        {
            NLLogger.OutputLog(LogLevel.Debug, String.Format("ContentAnalysis Job execute, job guid:[{0}]\n", targetInstanceId));
            if (strRunLevel.Equals(RunLevel.RLSiteLevel.ToString(),StringComparison.OrdinalIgnoreCase))
            {
                SPSecurity.RunWithElevatedPrivileges(delegate()
                {
                    ProcessSiteTimerTask();
                });
                return;
            }

            try
            {
                SPSecurity.RunWithElevatedPrivileges(delegate()
                {
                    using (SPSite Site = Globals.GetValidSPSite(WebUrl, HttpContext.Current))
                    {
                        using (SPWeb Web = Site.OpenWeb())
                        {
                            SPList List = Web.Lists[new Guid(ListGuid)];
                            NLLogger.OutputLog(LogLevel.Debug, "Timer: Running..." + this.Title + " with List " + List.Title);

                            String ca_state = List.RootFolder.Properties["ca_state"] as String;
                            if (ca_state == null || !ca_state.Equals("In Progress") || IsMinutesJob.Equals("--IsSPMinuteScheduleJob"))
                            {
                                // Start Content Analysis
                               // Thread currentThread = Thread.CurrentThread;

                                using (AutoResetEvent autoEvent = new AutoResetEvent(false))
                                {
                                    ListContentAnalysisWorker worker = new ListContentAnalysisWorker(WebUrl, UserName, UserSid, ListGuid, ClientIp);
                                    Thread workerThread = new Thread(worker.Run);
                                    workerThread.Start(autoEvent);
                                    autoEvent.WaitOne();
                                }
                            }
                            if (!IsMinutesJob.Equals("--IsSPMinuteScheduleJob"))
                            {
                                UpdateTimerThread updateWorker = new UpdateTimerThread(this.WebApplication, WebUrl, UserName, UserSid, ListGuid, ClientIp);
                                Thread updateThread = new Thread(updateWorker.Run);
                                updateThread.Start();
                            }
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Error, "Exception during Timer Execute: ", null, ex);
            }
        }
    }

    public class CASchedule
    {
        protected string m_WebUrl;
        protected string m_UserName;
        protected string m_UserSid;
        protected string m_ListGuid;
        protected string m_Ip;
        protected int m_nID;
        RunLevel m_RLLevel;
        //if bSiteLevel==true, listGuid is a site level actually.
        public CASchedule(string webUrl, string userName, string userSid, string listGuid, string ip, RunLevel Level = RunLevel.RLListLevel, int nID = 0)
        {
            m_WebUrl = webUrl;
            m_UserName = userName;
            m_UserSid = userSid;
            m_ListGuid = listGuid;
            m_Ip = ip;

            m_RLLevel = Level;
            if (m_RLLevel == RunLevel.RLSiteLevel)
            {
               m_nID = nID;
            }
        }


        public void UpdateTimerInSiteLevel()
        {

            SPSecurity.RunWithElevatedPrivileges(delegate()
            {
                using (SPSite Site = Globals.GetValidSPSite(m_WebUrl, HttpContext.Current))
                {
                    string ca_schedules = string.Empty;
                    String ca_schIndex = string.Empty;

                        using (SPWeb Web = Site.OpenWeb())
                        {
                            ca_schedules = Globals.GetSiteProperty(Web, Globals.strSiteSchedulesPropName);
                            ca_schIndex = Globals.GetSiteProperty(Web, Globals.strSiteSchIndexPropName);
                        }
                    String strJobName = ContentAnalysisJob.JobNamePrefix + m_ListGuid;
                    SPJobDefinitionCollection jobs = Site.WebApplication.JobDefinitions;
                    SPJobDefinition Job = null;
                    foreach (SPJobDefinition job in jobs)
                    {
                        if (job.Name.Contains(strJobName))
                        {
                            Job = job;
                            break;
                        }
                    }

                    if (String.IsNullOrEmpty(ca_schIndex) || String.IsNullOrEmpty(ca_schedules) || ca_schIndex.Equals("0"))
                    {
                        if (Job != null)
                        {
                            Job.Delete();
                            return;
                        }
                    }
                    else
                    {
                        string jobType = "";
                        SPOneTimeSchedule schedule = ParseSchedule(ca_schedules, ca_schIndex, ref jobType);
                        if (Job != null)
                        {
                            if (Job.Name.Contains("IsSPMinuteScheduleJob"))
                            {
                                //To update the minuteSchedule
                                SPMinuteSchedule mSchedule = ParseMinuteSchedule(ca_schedules, ca_schIndex);
                                //if new schedule is not minute schedule, delete old minute schedule, then create new other schedule
                                if (mSchedule == null)
                                {
                                    Job.Delete();
                                    Job = new ContentAnalysisJob(Site.WebApplication, m_WebUrl, m_UserName, m_UserSid, m_ListGuid, m_Ip, "", m_RLLevel, m_nID.ToString());
                                    Job.Schedule = schedule;
                                    try
                                    {
                                        Job.Update(true);
                                    }
                                    catch (Exception exp)
                                    {
                                        string errorInfo = String.Format("Nextlabs--web id ={0}[mTid={1}]: UpdateTimer Add a new timer job Fail:{2}", m_ListGuid, Thread.CurrentThread.ManagedThreadId, exp.Message + exp.StackTrace);
                                        NLLogger.OutputLog(LogLevel.Debug, errorInfo);
                                    }
                                }
                                else
                                {
                                    Job.Schedule = mSchedule;
                                    Job.Update(true);
                                }
                            }
                            else
                            {
                                //To update one-time job
                                Job.Schedule = schedule;
                                Job.Update(true);
                            }
                        }
                        else
                        {
                            if (jobType.Equals("Minutely"))
                            {
                                //To Create a minute-job instead
                                Job = new ContentAnalysisJob(Site.WebApplication, m_WebUrl, m_UserName, m_UserSid, m_ListGuid, m_Ip, "--IsSPMinuteScheduleJob", m_RLLevel, m_nID.ToString());
                                SPMinuteSchedule mSchedule = ParseMinuteSchedule(ca_schedules, ca_schIndex);
                                if (mSchedule == null)
                                {
                                    //Ignore other type of schedule job
                                    return;
                                }
                                Job.Schedule = mSchedule;
                                try
                                {
                                    Job.Update(true);
                                }
                                catch (Exception ex)
                                {
                                    NLLogger.OutputLog(LogLevel.Debug, "Exception during Creat a new minute timer job:", null, ex);
                                }
                            }
                            else //To Creat other type of schedule jobs
                            {
                                Job = new ContentAnalysisJob(Site.WebApplication, m_WebUrl, m_UserName, m_UserSid, m_ListGuid, m_Ip, "", m_RLLevel, m_nID.ToString());
                                Job.Schedule = schedule;
                                try
                                {
                                    Job.Update(true);
                                }
                                catch (Exception ex)
                                {
                                    string errorInfo = String.Format("Nextlabs--ListId={0}[mTid={1}]: UpdateTimer Add a new timer job Fail:{2}", m_ListGuid, Thread.CurrentThread.ManagedThreadId, ex.Message + ex.StackTrace);
                                    NLLogger.OutputLog(LogLevel.Debug, errorInfo);
                                }
                            }
                        }

                    }
                }
            });
        }

        public void UpdateTimer()
        {
            bool bUpdated = false;
            try
            {
                bUpdated = InnerUpdateTimer();
            }
            catch (System.Exception ex)
            {
                bUpdated = false;
                NLLogger.OutputLog(LogLevel.Error, "Exception during update share point schedule timer", null, ex);
            }
            NLLogger.OutputLog(LogLevel.Info, String.Format("Update share point schedule timer [{0}]", bUpdated ? "Success" : "Failed"));
            if (!bUpdated)
            {
                try
                {
                    SPSecurity.RunWithElevatedPrivileges(delegate ()
                    {
                        bUpdated = InnerUpdateTimer();
                    });
                }
                catch (System.Exception ex)
                {
                    bUpdated = false;
                    NLLogger.OutputLog(LogLevel.Error, "Exception during update share point schedule timer with RunWithElevatedPrivileges", null, ex);
                }
                NLLogger.OutputLog(LogLevel.Info, String.Format("Update share point schedule timer with RunWithElevatedPrivileges [{0}]", bUpdated ? "Success" : "Failed"));
            }
        }
        private bool InnerUpdateTimer()
        {
            bool bUpdateRet = false;
            using (SPSite Site = Globals.GetValidSPSite(m_WebUrl, HttpContext.Current))
            {
                using (SPWeb Web = Site.OpenWeb())
                {
                    SPList List = Web.Lists[new Guid(m_ListGuid)];
                    SPWebApplication App = Site.WebApplication;

                    String ca_schedules = List.RootFolder.Properties["ca_schedules"] as String;
                    String ca_schIndex = List.RootFolder.Properties["ca_schIndex"] as String;
                    String jobName = ContentAnalysisJob.JobNamePrefix + m_ListGuid;
                    NLLogger.OutputLog(LogLevel.Debug, "UpdateTimer(" + jobName + "): ca_schedules=" + ca_schedules + " with ca_schIndex =" + ca_schIndex);
                    SPJobDefinitionCollection jobs = App.JobDefinitions;
                    SPJobDefinition Job = null;
                    foreach (SPJobDefinition job in jobs)
                    {
                        if (job.Name.Contains(jobName))
                        {
                            Job = job;
                            break;
                        }
                    }

                    if (String.IsNullOrEmpty(ca_schIndex) || String.IsNullOrEmpty(ca_schedules) || ca_schIndex.Equals("0"))
                    {
                        if (Job != null)
                        {
                            Job.Delete();
                        }
                    }
                    else
                    {
                        bool bNeedUpdateJob = false;
                        string jobType = "";
                        SPOneTimeSchedule schedule = ParseSchedule(ca_schedules, ca_schIndex, ref jobType);

                        if (Job != null)
                        {
                            if (Job.Name.Contains("IsSPMinuteScheduleJob"))
                            {
                                //To update the minuteSchedule
                                SPMinuteSchedule mSchedule = ParseMinuteSchedule(ca_schedules, ca_schIndex);
                                if (mSchedule == null)
                                {
                                    Job.Delete();
                                    Job = new ContentAnalysisJob(App, m_WebUrl, m_UserName, m_UserSid, m_ListGuid, m_Ip, "");
                                    Job.Schedule = schedule;

                                    bNeedUpdateJob = true;
                                }
                                else
                                {
                                    Job.Schedule = mSchedule;
                                    bNeedUpdateJob = true;
                                }

                            }
                            else
                            {
                                //To update one-time job
                                Job.Schedule = schedule;
                                bNeedUpdateJob = true;
                            }
                        }
                        else
                        {
                            if (jobType.Equals("Minutely"))
                            {
                                //To Create a minute-job instead
                                Job = new ContentAnalysisJob(App, m_WebUrl, m_UserName, m_UserSid, m_ListGuid, m_Ip, "--IsSPMinuteScheduleJob");
                                SPMinuteSchedule mSchedule = ParseMinuteSchedule(ca_schedules, ca_schIndex);
                                if (mSchedule == null)
                                {
                                    //Ignore other type of schedule job
                                    NLLogger.OutputLog(LogLevel.Error, String.Format("Failed to parse minute schedule info:[{0}],[{1}] when Update time job:[{2}]\n", ca_schIndex, ca_schedules, jobType));

                                    bNeedUpdateJob = false;
                                    bUpdateRet = false;
                                }
                                else
                                {
                                    Job.Schedule = mSchedule;

                                    bNeedUpdateJob = true;
                                }
                            }
                            else //To Create other type of schedule jobs
                            {
                                Job = new ContentAnalysisJob(App, m_WebUrl, m_UserName, m_UserSid, m_ListGuid, m_Ip, "");
                                Job.Schedule = schedule;

                                bNeedUpdateJob = true;
                            }
                        }

                        if (bNeedUpdateJob)
                        {
                            if (null == Job)
                            {
                                NLLogger.OutputLog(LogLevel.Error, String.Format("Logic error, need update job but the job object is null, please check. Nextlabs--web id={0}[mTid={1}]: Job:[{2}", m_ListGuid, Thread.CurrentThread.ManagedThreadId, Job));
                            }
                            else
                            {
                                try
                                {
                                    Job.Update(true);
                                    bUpdateRet = true;
                                }
                                catch (Exception ex)
                                {
                                    bUpdateRet = false;
                                    NLLogger.OutputLog(LogLevel.Error, String.Format("Exception when add timer for Nextlabs--web id={0}[mTid={1}]: Job:[{2}, Schedule:[{3}] ", m_ListGuid, Thread.CurrentThread.ManagedThreadId, Job, Job.Schedule), null, ex);
                                }
                            }
                        }
                    }
                }
            }
            return bUpdateRet;
        }

        private SPMinuteSchedule ParseMinuteSchedule(string schedules, string strIndex)
        {
            SPMinuteSchedule mSchedule = null;
            Int32 index = Int32.Parse(strIndex);
            if (index <= 0) return mSchedule;

            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.InnerXml = schedules;
            XmlNode node = xmlDoc.DocumentElement;

            index--;
            XmlNodeList nodes = node.ChildNodes;
            Int32 iCount = 0;
            XmlNode selectedNode = null;
            foreach (XmlNode child in nodes)
            {
                if (iCount == index)
                {
                    selectedNode = child;
                    break;
                }
                iCount++;
            }

            if (selectedNode != null)
            {
                string type = selectedNode["Type"].InnerText;
                mSchedule = new SPMinuteSchedule();
                if (type.Equals("Minutely"))
                {
                    int minutes = Int32.Parse(selectedNode["Minutes"].InnerText);
                    //DateTime startTime = DateTime.Parse(selectedNode["From"].InnerText + " " + selectedNode["StartDate"].InnerText);

                    mSchedule = GetMinuteSchedule(minutes);
                }
                else
                {
                    return null;
                }
            }

            return mSchedule;
        }

        private SPMinuteSchedule GetMinuteSchedule(int minutes)
        {
            SPMinuteSchedule schedule = new SPMinuteSchedule();
            schedule.BeginSecond = 0;
            schedule.EndSecond = 59;
            schedule.Interval = minutes;
            return schedule;
        }

        private SPOneTimeSchedule ParseSchedule(string schedules, string strIndex, ref string jobType)
        {
            SPOneTimeSchedule hourlySchedule = null;
            Int32 index = Int32.Parse(strIndex);
            if (index <= 0) return hourlySchedule;

            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.InnerXml = schedules;
            XmlNode node = xmlDoc.DocumentElement;

            index--;
            XmlNodeList nodes = node.ChildNodes;
            Int32 iCount = 0;
            XmlNode selectedNode = null;
            foreach (XmlNode child in nodes)
            {
                if (iCount == index)
                {
                    selectedNode = child;
                    break;
                }
                iCount++;
            }

            if (selectedNode != null)
            {
                string type = selectedNode["Type"].InnerText;
                jobType = type;
                hourlySchedule = new SPOneTimeSchedule();

                if (type.Equals("Hourly"))
                {
                    int hours = Int32.Parse(selectedNode["Hours"].InnerText);
                    DateTime startTime = DateTime.Parse(selectedNode["From"].InnerText + " " + selectedNode["StartDate"].InnerText);
                    hourlySchedule = GetNextOccurrence(hours, startTime);
                }

                else if (type.Equals("Minutely"))
                {

                    int minutes = Int32.Parse(selectedNode["Minutes"].InnerText);

                    DateTime startTime = DateTime.Parse(selectedNode["From"].InnerText + " " + selectedNode["StartDate"].InnerText);

                    hourlySchedule = GetNextOccurrenceMinute(minutes, startTime);

                }
                else if (type.Equals("Daily"))
                {
                    int days = Int32.Parse(selectedNode["Days"].InnerText);
                    DateTime startTime = DateTime.Parse(selectedNode["From"].InnerText);
                    DateTime startDate = DateTime.Parse(selectedNode["StartDate"].InnerText);
                    hourlySchedule = GetNextOccurrence(days, startTime, startDate);
                }
                else
                {
                    int weeks = Int32.Parse(selectedNode["Weeks"].InnerText);
                    string weekdays = selectedNode["WeekDays"].InnerText;
                    DateTime startTime = DateTime.Parse(selectedNode["From"].InnerText);
                    DateTime startDate = DateTime.Parse(selectedNode["StartDate"].InnerText);
                    hourlySchedule = GetNextOccurrence(weeks, weekdays, startTime, startDate);
                }
            }

            return hourlySchedule;
        }

        private SPOneTimeSchedule GetNextOccurrence(int hours, DateTime startTime)
        {
            SPOneTimeSchedule schedule = new SPOneTimeSchedule();

            DateTime now = DateTime.Now;

            TimeSpan span = new TimeSpan(hours, 0, 0);
            DateTime next = startTime + span;

            while (next <= now)
            {
                next += span;
            }

            schedule.Time = next;

            return schedule;
        }
        private SPOneTimeSchedule GetNextOccurrenceMinute(int minutes, DateTime startTime)
        {
            SPOneTimeSchedule schedule = new SPOneTimeSchedule();

            DateTime now = DateTime.Now;

            TimeSpan span = new TimeSpan(0, minutes, 0);
            DateTime next = startTime + span;

            while (next <= now)
            {
                next += span;
            }

            schedule.Time = next;

            return schedule;
        }

        private SPOneTimeSchedule GetNextOccurrence(int days, DateTime startTime, DateTime startDate)
        {
            SPOneTimeSchedule schedule = new SPOneTimeSchedule();

            DateTime now = DateTime.Now;

            TimeSpan span = new TimeSpan(days, 0, 0, 0);
            DateTime nextDate = startDate + new TimeSpan(days-1, 0, 0, 0);
            nextDate += new TimeSpan(startTime.Hour, 0, 0);

            while (nextDate <= now)
            {
                nextDate += span;
            }

            schedule.Time = nextDate;

            return schedule;
        }

        private SPOneTimeSchedule GetNextOccurrence(int weeks, string weekdays, DateTime startTime, DateTime startDate)
        {
            SPOneTimeSchedule schedule = new SPOneTimeSchedule();

            DateTime now = DateTime.Now;

            TimeSpan span = new TimeSpan(weeks*7, 0, 0, 0);
            DateTime nextDate = startDate + new TimeSpan((weeks-1) * 7, 0, 0, 0);
            nextDate += new TimeSpan(startTime.Hour, 0, 0);

            while (true)
            {
                DateTime sunday = nextDate - new TimeSpan((nextDate.DayOfWeek - DayOfWeek.Sunday), 0, 0, 0);
                if (weekdays.Contains("Sun") && sunday > now)
                {
                    nextDate = sunday;
                    break;
                }
                else if (weekdays.Contains("Mon") && (sunday + new TimeSpan(1, 0, 0, 0)) > now)
                {
                    nextDate = (sunday + new TimeSpan(1, 0, 0, 0));
                    break;
                }
                else if (weekdays.Contains("Tue") && (sunday + new TimeSpan(2, 0, 0, 0)) > now)
                {
                    nextDate = (sunday + new TimeSpan(2, 0, 0, 0));
                    break;
                }
                else if (weekdays.Contains("Wed") && (sunday + new TimeSpan(3, 0, 0, 0)) > now)
                {
                    nextDate = (sunday + new TimeSpan(3, 0, 0, 0));
                    break;
                }
                else if (weekdays.Contains("Thu") && (sunday + new TimeSpan(4, 0, 0, 0)) > now)
                {
                    nextDate = (sunday + new TimeSpan(4, 0, 0, 0));
                    break;
                }
                else if (weekdays.Contains("Fri") && (sunday + new TimeSpan(5, 0, 0, 0)) > now)
                {
                    nextDate = (sunday + new TimeSpan(5, 0, 0, 0));
                    break;
                }
                else if (weekdays.Contains("Sat") && (sunday + new TimeSpan(6, 0, 0, 0)) > now)
                {
                    nextDate = (sunday + new TimeSpan(6, 0, 0, 0));
                    break;
                }

                nextDate += span;
            }

            schedule.Time = nextDate;

            return schedule;
        }
    }


    class UpdateTimerThread
    {
        protected SPWebApplication m_WebApp;
        protected string m_WebUrl;
        protected string m_UserName;
        protected string m_UserSid;
        protected string m_ListGuid;
        protected string m_Ip;
        RunLevel m_RLLevel;
        protected int m_nID;

        public UpdateTimerThread(SPWebApplication app, string webUrl, string userName, string userSid, string listGuid, string ip, RunLevel level=RunLevel.RLListLevel, int nID = 0)
        {
            m_WebApp = app;
            m_WebUrl = webUrl;
            m_UserName = userName;
            m_UserSid = userSid;
            m_ListGuid = listGuid;
            m_Ip = ip;
            m_RLLevel = level;
            m_nID = nID;
        }

        public void Run()
        {
            SPSecurity.RunWithElevatedPrivileges(delegate()
            {
                SPJobDefinitionCollection jobs = m_WebApp.JobDefinitions;
                SPJobDefinition Job = null;
                String jobName = ContentAnalysisJob.JobNamePrefix + m_ListGuid;

                //  Wait the timer to be deleted.
                while (true)
                {
                    Thread.Sleep(30000);
                    Job = null;
                    foreach (SPJobDefinition job in jobs)
                    {
                        if (job.Name.Equals(jobName))
                        {
                            Job = job;
                            break;
                        }
                    }

                    if (Job == null)
                        break;
                }

                // Start new timer
                try
                {
                    if (m_RLLevel == RunLevel.RLSiteLevel)
                    {
                        CASchedule schedule = new CASchedule(m_WebUrl, m_UserName, m_UserSid, m_ListGuid, m_Ip, m_RLLevel, m_nID);
                        schedule.UpdateTimerInSiteLevel();
                    }
                    else
                    {
                        CASchedule schedule = new CASchedule(m_WebUrl, m_UserName, m_UserSid, m_ListGuid, m_Ip);
                        schedule.UpdateTimer();
                    }
                }
                catch (Exception ex)
                {
                    NLLogger.OutputLog(LogLevel.Debug, "Exceptionduring UpdateTimerThread:", null, ex);
                }
            });
        }
    }
}
