using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.SharePoint.Administration;
using System.Diagnostics;
using System.Text;

namespace NextLabs.Diagnostic
{
    public class LoggingService : SPDiagnosticsServiceBase
    {
        public static string MaventionDiagnosticAreaName = "Compliant Enterprise";
        private static LoggingService _Current;
        public static LoggingService Current
        {
            get
            {
                if (_Current == null)
                {
                    _Current = new LoggingService();
                }
     
                return _Current;
            }
        }
     
        private LoggingService()
            : base("Mavention Logging Service", SPFarm.Local)
        {
     
        }
     
        protected override IEnumerable<SPDiagnosticsArea> ProvideAreas()
        {
            List<SPDiagnosticsArea> areas = new List<SPDiagnosticsArea>
            {
                new SPDiagnosticsArea(MaventionDiagnosticAreaName, new List<SPDiagnosticsCategory>
                {
                    new SPDiagnosticsCategory("Compliant Enterprise", TraceSeverity.Verbose, EventSeverity.Error)
                })
            };
     
            return areas;
        }
    }
}
