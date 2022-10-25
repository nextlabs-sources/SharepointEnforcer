using System;
using System.Web;

using log4net.Layout;
using log4net.Core;
using log4net.Appender;
using NextLabs.Diagnostic;
using Microsoft.SharePoint.Administration;
using NextLabs.Diagnostic.RegCategory;
using System.Threading;
namespace NextLabs.Diagnostic.log4net.Appender
{
    public class SPTraceAppender : AppenderSkeleton
    {
        #region Public Instances Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="SPTraceAppender" /> class.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Default constructor.
        /// </para>
        /// </remarks>
        public SPTraceAppender()
        {
        }

        #endregion // Public Instances Constructors

        #region Override implementation of AppenderSkeleton

        /// <summary>
        /// Write the logging event to the SharePoint trace
        /// </summary>
        /// <param name="loggingEvent">the event to log</param>
        /// <remarks>
        /// <para>
        /// Write the logging event to the SharePoint trace
        /// <c>SharePoint Trace log</c> 
        /// (<see cref="TraceContext"/>).
        /// </para>
        /// </remarks>
        private void AsyncAppend(object state)
        {
            LoggingEvent loggingEvent = state as LoggingEvent;
            if (loggingEvent == null)
                return;
            TraceSeverity severity = TraceSeverity.Verbose;
            switch (loggingEvent.Level.Name)
            {
                case "FATAL":
                    severity = TraceSeverity.Unexpected;
                    break;
                case "ERROR":
                    severity = TraceSeverity.Monitorable;
                    break;
                case "WARN":
                    severity = TraceSeverity.High;
                    break;
                case "INFO":
                    severity = TraceSeverity.Medium;
                    break;
                case "DEBUG":
                    severity = TraceSeverity.Verbose;
                    break;
                default:
                    break;
            }
            SPDiagnosticsCategory category = LoggingService.Current.Areas["Compliant Enterprise"].Categories["Compliant Enterprise"];
            LoggingService.Current.WriteTrace(0, category, severity, RenderLoggingEvent(loggingEvent));
        }

        override protected void Append(LoggingEvent loggingEvent)
        {
            switch (loggingEvent.Level.Name)
            {
                case "FATAL":
                    break;
                case "ERROR":
                    break;
                case "WARN":
                    break;
                case "INFO":
                    break;
                case "DEBUG":
                    break;
                default:
                    break;
            }
            try
            {
                System.Threading.ThreadPool.QueueUserWorkItem(new WaitCallback(AsyncAppend), loggingEvent);
            }
            catch
            {
            }
        }

        /// <summary>
        /// This appender requires a <see cref="Layout"/> to be set.
        /// </summary>
        /// <value><c>true</c></value>
        /// <remarks>
        /// <para>
        /// This appender requires a <see cref="Layout"/> to be set.
        /// </para>
        /// </remarks>
        override protected bool RequiresLayout
        {
            get { return true; }
        }

        #endregion // Override implementation of AppenderSkeleton
    }
}
