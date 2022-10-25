using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.SharePoint;
using System.Web;
using System.ComponentModel;
using NextLabs.Diagnostic;

namespace NextLabs.Common
{
    #region Definitions for Event handlder&args
    public delegate void BeforeEventExecutingHandler(object sender, CancelEventArgs args);
    public delegate void AfterEventExecutedHandler(object sender,EventArgs args);
    public enum HttpModuleEvents { BeginRequest, PreRequestHandlerExecute, EndRequest }
    /// <summary>
    /// EventArgs for HttpModule
    /// </summary>
    public class HttpModuleEventArgs:CancelEventArgs
    {

        public HttpApplication Application { get; private set; }
        public HttpModuleEvents EventType { get; private set; }
        public HttpModuleEventArgs(HttpApplication application,HttpModuleEvents eventType)
        {
            Application = application;
            EventType = eventType;
        }
    }
    /// <summary>
    /// EventArgs for EventHandler
    /// </summary>
    public class EventHandlerEventArgs : CancelEventArgs
    {
        public SPEventPropertiesBase EventProperties { get; private set; }
        public HttpContext Context { get; private set; }
        public EventHandlerEventArgs(SPEventPropertiesBase properties,HttpContext context)
        {
            EventProperties = properties;
            Context = context;
        }
    }

    // EventArgs for TrimmingControl
    public class ControlEventArgs : CancelEventArgs
    {
         public HttpContext Context { get; private set; }
         public ControlEventArgs(HttpContext context)
        {
            Context = context;
        }
    }

    #endregion
    public class EventHelper
    {
        #region static members
        private static EventHelper _eventHelper = null;
        private static object _syncRoot = new object();
        public static EventHelper Instance
        {
            get
            {
                if (_eventHelper == null)
                {
                    lock (_syncRoot)
                    {
                        if (_eventHelper == null)
                        {
                            _eventHelper = new EventHelper();
                        }
                    }
                }
                return _eventHelper;
            }
        }
        #endregion

        #region public members
        public void OnBeforeEventExecuting(object sender, CancelEventArgs eventArgs)
        {
            if (_beforeEventExecuting != null)
            {
                try
                {
                    //break when any event handler already cancelled this event
                    foreach (BeforeEventExecutingHandler eventHandler in _beforeEventExecuting.GetInvocationList())
                    {
                        if (eventArgs.Cancel)
                        {
                            break;
                        }
                        eventHandler(sender, eventArgs);
                    }
                }
                catch (Exception ex)
                {
                    //catch all exceptions to avoid stopping other event handlers
                    NLLogger.OutputLog(LogLevel.Debug, "Exception during OnBeforeEventExecuting, happened during event handling", null, ex);
                }
            }
        }
        public void OnAfterEventExecuted(object sender, EventArgs eventArgs)
        {
            if (_afterEventExecuted != null)
            {
                try
                {
                    _afterEventExecuted(sender, eventArgs);
                }
                catch (Exception ex)
                {
                    //catch all exceptions to avoid stopping other event handlers
                    NLLogger.OutputLog(LogLevel.Debug, "Exception during OnAfterEventExecuted,happened during event handling", null, ex);
                }
            }
        }
        public event BeforeEventExecutingHandler BeforeEventExecuting
        {
            add
            {
                //add check to avoid duplicated event handler
                if (_beforeEventExecuting == null || !_beforeEventExecuting.GetInvocationList().Contains(value))
                {
                    _beforeEventExecuting += value;
                }
            }
            remove { _beforeEventExecuting -= value; }
        }
        #endregion
        #region private members
        private event BeforeEventExecutingHandler _beforeEventExecuting;
        private event AfterEventExecutedHandler _afterEventExecuted;
        private EventHelper()
        {
            //register eventhandler here
            BeforeEventExecuting += SCLSwitchChecker.OnBeforeEventExecuting;
        }
        #endregion
    }
}
