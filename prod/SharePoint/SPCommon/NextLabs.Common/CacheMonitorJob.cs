using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using Microsoft.SharePoint;
using Microsoft.SharePoint.Administration;
using NextLabs.Common;

namespace NextLabs.Common
{
    public class CacheMonitorJob : SPJobDefinition
    {
        static public String JobName = "Cache Monitor Job";

        public CacheMonitorJob() : base() 
        {
        }

        public CacheMonitorJob(SPWebApplication webApp)
            : base(JobName, webApp, null, SPJobLockType.None)
        {
            this.Title = JobName;
        }

        public override void Execute(Guid targetInstanceId)
        {
            EvaluationCache.Instance.ClearTimeOut();
        }
    }

    public class CacheMonitorSchedule
    {
        static public CacheMonitorSchedule Instance = new CacheMonitorSchedule();
        private SPWebApplication WebApplication;
        private CacheMonitorJob Job;

        public CacheMonitorSchedule() { }

        public bool StartTimer()
        {
            if (WebApplication == null
                && GetAdministrationWebApplication() == false)
                return false;

            SPJobDefinitionCollection jobs = WebApplication.JobDefinitions;
            foreach (SPJobDefinition job in jobs)
            {
                if (job.Name.Equals(CacheMonitorJob.JobName))
                {
                    job.Delete();
                }
            }
            

            if (Job != null)
                return true;

            Job = new CacheMonitorJob(WebApplication);

            SPMinuteSchedule schedule = new SPMinuteSchedule();
            schedule.BeginSecond = 0;
            schedule.EndSecond = 59;
            schedule.Interval = (int)EvaluationCache.TimeOutInterval.TotalMinutes;
            Job.Schedule = schedule;

            Job.Update();

            return true;
        }

        public bool StopTimer()
        {
            if (Job != null)
            {
                Job.Delete();
                Job = null;
            }
            else
            {
                if (WebApplication == null
                        && GetAdministrationWebApplication() == false)
                    return false;

                SPJobDefinitionCollection jobs = WebApplication.JobDefinitions;
                foreach (SPJobDefinition job in jobs)
                {
                    if (job.Name.Equals(CacheMonitorJob.JobName))
                    {
                        job.Delete();
                    }
                }
            }

            return true;
        }

        public bool UpdateTimer()
        {
            if (Job == null)
                return StartTimer();

            SPMinuteSchedule schedule = new SPMinuteSchedule();
            schedule.BeginSecond = 0;
            schedule.EndSecond = 59;
            schedule.Interval = (int)EvaluationCache.TimeOutInterval.TotalMinutes;
            Job.Schedule = schedule;

            Job.Update();

            return true;
        }

        private bool GetAdministrationWebApplication()
        {
            bool bRet = false;
            try
            {
                foreach (SPService service in SPFarm.Local.Services)
                {
                    if (service is SPWebService)
                    {
                        SPWebService webService = (SPWebService)service;
                        foreach (SPWebApplication webapp in webService.WebApplications)
                        {
                            if (webapp.IsAdministrationWebApplication)
                            {
                                WebApplication = webapp;
                                bRet = true;
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
            }

            return bRet;
        }
    }
}
