using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using NextLabs.Common;
using Microsoft.Win32;
using System.ComponentModel;
using System.Web;
using System.IO;
using System.Text.RegularExpressions;
using System.Web.UI;
using Microsoft.SharePoint;
using NextLabs.Diagnostic;

namespace NextLabs.SPEnforcer
{
    class NxlFileNotFoundBlocker:Stream
    {
        public const string ITEM_FILE_RENAMED = "NXL-RMS-ITEM-RENAMED";
        public const string REGISTRY_KEY_SUPPRESS_DIALOG = "SuppressEditPropertiesForm";//value:false/true
        #region static members
        private static int _initializing = 0;//0=false;1=true
        public static void Initialize()
        {
            //use Interlocked instead of lock() to reduce performance overhead
            if (Interlocked.CompareExchange(ref _initializing, 1, 0) == 0)
            {
                if (IsEnabled())
                {
                    EventHelper.Instance.BeforeEventExecuting += NxlFileNotFoundBlocker.BeforeEventExecutingHandler;
                }
                else
                {
                    EventHelper.Instance.BeforeEventExecuting -= NxlFileNotFoundBlocker.BeforeEventExecutingHandler;
                }
                _initializing = 0;
            }
        }
        private static bool? _enabled;
        private static DateTime _resetTime;
        public static bool IsEnabled()
        {
            if (!_enabled.HasValue || DateTime.Now >= _resetTime)
            {
                try
                {
                    using (var ceKey = Registry.LocalMachine.OpenSubKey(@"Software\NextLabs\Compliant Enterprise\Sharepoint Enforcer\", false))
                    {
                        if (ceKey != null)
                        {
                            object regValue = ceKey.GetValue(REGISTRY_KEY_SUPPRESS_DIALOG, string.Empty);
                            bool enabled = false;
                            if (regValue != null && bool.TryParse(regValue.ToString(), out enabled))
                            {
                                _enabled = enabled;
                            }
                        }
                    }
                    _resetTime = DateTime.Now.AddMinutes(10);
                }
                catch (Exception ex)
                {
                    NLLogger.OutputLog(LogLevel.Debug, "Exception during IsEnabled when reading registry key:", null, ex);
                }
            }
            return _enabled.HasValue ? _enabled.Value : false;
        }
        public static void BeforeEventExecutingHandler(object sender, CancelEventArgs args)
        {
            if (args is EventHandlerEventArgs)
            {
                //this event is from ItemAdded event
                var eventArgs = args as EventHandlerEventArgs;

                switch (eventArgs.EventProperties.EventType)
                {
                    case SPEventReceiverType.ItemAdding:
                    case SPEventReceiverType.ItemUpdating:
                        {
                            try
                            {
                                // fix bug 51654, It only work on document library.
                                SPItemEventProperties properties = eventArgs.EventProperties as SPItemEventProperties;
                                SPList list = properties.List;
                                if (list.BaseType == SPBaseType.DocumentLibrary)
                                {
                                    var httpContext = eventArgs.Context;
                                    if (httpContext == null)
                                    {
                                        break;
                                    }
                                    if (httpContext.Request.HttpMethod != "POST")
                                    {
                                        return;
                                    }
                                    string requestUrl = string.Empty;
                                    SPEEvalAttr attrs = SPEEvalAttrs.Current();
                                    if (attrs != null)
                                    {
                                        requestUrl = attrs.RequestURL;
                                    }
                                    if (string.IsNullOrEmpty(requestUrl))
                                    {
                                        requestUrl = httpContext.Request.Url.AbsolutePath;
                                    }
                                    if (requestUrl.IndexOf("uploadex.aspx", StringComparison.OrdinalIgnoreCase) > 0)
                                    {
                                        //post back uploadex.aspx
                                        httpContext.Response.Filter = new NxlFileNotFoundBlocker(httpContext);
                                    }
                                }
                            }
                            catch
                            {

                            }
                        }
                        break;
                    default:
                        break;
                }
            }
        }
        #endregion
        private readonly HttpContext m_httpContext;
        private readonly Stream m_originalStream;
        private NxlFileNotFoundBlocker(HttpContext context)
        {
            m_httpContext = context;
            m_originalStream = context.Response.Filter;
        }
        #region overriden members
        public override bool CanRead
        {
            get { return m_originalStream.CanRead; }
        }
        public override bool CanSeek
        {
            get { return m_originalStream.CanSeek; }
        }
        public override bool CanWrite
        {
            get { return m_originalStream.CanWrite; }
        }
        public override void Flush()
        {
            m_originalStream.Flush();
        }
        public override long Length
        {
            get { return m_originalStream.Length; }
        }
        public override long Position
        {
            get
            {
                return m_originalStream.Position;
            }
            set
            {
                m_originalStream.Position = value;
            }
        }
        public override void Close()
        {
            m_originalStream.Close();
        }
        public override int Read(byte[] buffer, int offset, int count)
        {
            return m_originalStream.Read(buffer, offset, count);
        }
        public override long Seek(long offset, SeekOrigin origin)
        {
            return m_originalStream.Seek(offset, origin);
        }
        public override void SetLength(long value)
        {
            m_originalStream.SetLength(value);
        }
        private const string ERROR_MESSAGE="File Not Found";
        public override void Write(byte[] buffer, int offset, int count)
        {
            //identify the "Sorry, something went wrong - FILE NOT FOUND" error
            var strBuffer = m_httpContext.Request.ContentEncoding.GetString(buffer, offset, count);
            if (TryInsertScript(ref strBuffer))
            {
                //insert commitPopup script to close the dialog and refresh the parent dialog
                var bytes = m_httpContext.Request.ContentEncoding.GetBytes(strBuffer.ToCharArray(), 0, strBuffer.Length);
                m_originalStream.Write(bytes, 0, bytes.Length);
                ResetFilter();
                return;
            }
            m_originalStream.Write(buffer, offset, count);
        }
        #endregion
        #region private members
        private const string COMMIT_POPUP = "window.frameElement.commitPopup();";
        private const string CLOSE_SCRIPT = @"<script>window.frameElement.commitPopup();</script>";
        //For sp2010, the script is like window.location.replace("");
        //For SP2013, the script is like window.location.href="";
        private const string WINDOW_HREF_SCRIPT = "window.location.";
        private bool TryInsertScript(ref string inputString)
        {
            if (string.IsNullOrEmpty(inputString))
                return false;
            int index = -1;
            //check if this is an deny page of SPE
            index = inputString.IndexOf(Common.Globals.EnforcementMessage, StringComparison.OrdinalIgnoreCase);
            if (index >= 0)
            {
                //skip this page
                return true;
            }
            //check if response is redirecting to editform.aspx
            index = inputString.IndexOf(WINDOW_HREF_SCRIPT, StringComparison.OrdinalIgnoreCase);
            if (index >= 0)
            {
                var endIndex = inputString.IndexOf(";", index, StringComparison.OrdinalIgnoreCase);
                var newLocation = inputString.Substring(index, endIndex - index + 1);
                if (newLocation.IndexOf("EditForm.aspx", StringComparison.OrdinalIgnoreCase) > 0)
                {
                    //window is redirecting edit properties form
                    inputString = inputString.Replace(newLocation, COMMIT_POPUP);
                    return true;
                }
            }
            //try insert close script in the end of form section
            index = inputString.IndexOf(@"</form>", StringComparison.OrdinalIgnoreCase);
            if (index >= 0)
            {
                inputString = inputString.Insert(index, CLOSE_SCRIPT);
                return true;
            }
            return false;
        }
        private void ResetFilter()
        {
            if (m_httpContext.Items.Contains(ITEM_FILE_RENAMED))
                m_httpContext.Items.Remove(ITEM_FILE_RENAMED);
            if (m_httpContext.Response.Filter is NxlFileNotFoundBlocker)
            {
                m_httpContext.Response.Filter = m_originalStream;
            }
        }
        #endregion
    }
}
