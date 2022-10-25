using System;
using System.IO;
using System.Collections;
using System.Collections.Specialized;
using System.Web;
using System.Web.UI.WebControls.WebParts;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Diagnostics;
using Microsoft.SharePoint;
using Microsoft.SharePoint.WebControls;
using Microsoft.SharePoint.WebPartPages;
using System.Security.Principal;
using System.Runtime.InteropServices;
using System.Xml;
using NextLabs.Common;
using System.Text.RegularExpressions;
using NextLabs.Diagnostic;

namespace NextLabs.HttpEnforcer
{
    public class RequestFilter : Stream
    {
        #region properties
        Stream requestStream;
        Stream m_stream;
        StringBuilder responseHtml = new StringBuilder();
        #endregion
        #region constructor
        public RequestFilter(Stream inputStream)
        {
            requestStream = inputStream;
        }
        #endregion
        #region implemented abstract members

        public override bool CanRead
        {
            get { return true; }
        }
        public override bool CanSeek
        {
            get { return false; }
        }
        public override bool CanWrite
        {
            get { return false; }
        }
        public override void Close()
        {
            requestStream.Close();
        }
        public override void Flush()
        {
            requestStream.Flush();
        }
        public override long Length
        {
            get { return requestStream.Length; }
        }
        public override long Position
        {
            get { return requestStream.Position; }
            set { throw new NotSupportedException(); }
        }
        public override long Seek(long offset, System.IO.SeekOrigin direction)
        {
            throw new NotSupportedException();
        }
        public override void SetLength(long length)
        {
            throw new NotSupportedException();
        }
        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }
        #endregion
        #region Read method
        //public override void Write(byte[] buffer, int offset, int count)
        public override int Read(byte[] buffer, int offset, int count)
        {
            HttpRequest Request = HttpContext.Current.Request;
            try
            {
                if (m_stream == null)
                {
                    var sr = new StreamReader(requestStream, Request.ContentEncoding);
                    string content = sr.ReadToEnd();

                    //Perform the content replacement routine
                    if (-1 != content.IndexOf("QueryText", StringComparison.OrdinalIgnoreCase) && -1 != content.IndexOf("SetQueryPropertyValue", StringComparison.OrdinalIgnoreCase)
                    && content.StartsWith("<Request", StringComparison.OrdinalIgnoreCase) && content.EndsWith("</Request>", StringComparison.OrdinalIgnoreCase))
                    {
                        SPWeb web = SPControl.GetContextWeb(HttpContext.Current);

                        #region add prefilter
                        bool noMatch = false;
                        try
                        {
                            SPUser user = web.CurrentUser;
                            if (user != null)
                            {
                                string loginName = user.LoginName;
                                IPrincipal PrincipalUser = HttpContext.Current.User;
                                noMatch = PolicyEngineModule.PrefilterEngineMatch(web, PrincipalUser, loginName, "");
                                if (noMatch)
                                {
                                    NLLogger.OutputLog(LogLevel.Debug, "policy no match");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            NLLogger.OutputLog(LogLevel.Error, "Exception during prefilter read:", null, ex);
                        }
                        #endregion
                        //added,only in match condition will do search prefilter
                        if (!noMatch && SPEHttpModule.CheckPreFilterSearchTrimming(web))
                        {
                            content = DoSearchPrefilter(Request, web, content);
                        }
                    }

                    // Write the content to stream.
                    Byte[] bytes = Request.ContentEncoding.GetBytes(content);
                    m_stream = new MemoryStream();
                    m_stream.Write(bytes, 0, bytes.Length);
                    m_stream.Seek(0, SeekOrigin.Begin);
                }
            }
            catch(Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Debug, "Exception during RequestFilter Read:", null, ex);
                return requestStream.Read(buffer, offset, count);
            }

            return m_stream.Read(buffer, offset, count);
        }
        #endregion

        // Add by George, for search pre-filter in soap case.
        static public string DoSearchPrefilter(HttpRequest Request, SPWeb web, string inputXml)
        {
            string finalXml = inputXml;
            try
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.InnerXml = inputXml;
                XmlNode node = xmlDoc.DocumentElement;
                XmlNode actions = node["Actions"];
                bool bSearch = false;
                string queryStr = "";
                XmlNode queryText = null;
                if (actions != null)
                {
                    foreach (XmlNode childNode in actions.ChildNodes)
                    {
                        if (childNode.Name.Equals("Method"))
                        {
                            foreach (XmlAttribute attr in childNode.Attributes)
                            {
                                if (attr.Name.Equals("Name") && attr.Value.Equals("SetQueryPropertyValue"))
                                {
                                    bSearch = true;
                                    break;
                                }
                            }
                        }
                        else if (childNode.Name.Equals("SetProperty"))
                        {
                            bool bQuery = false;
                            foreach (XmlAttribute attr in childNode.Attributes)
                            {
                                if (attr.Name.Equals("Name") && attr.Value.Equals("QueryText"))
                                {
                                    bQuery = true;
                                    break;
                                }
                            }
                            if (bQuery && childNode["Parameter"] != null)
                            {
                                queryText = childNode["Parameter"];
                                queryStr = queryText.InnerText;
                            }
                        }
                    }
                }

                if (bSearch && !string.IsNullOrEmpty(queryStr) && web != null)
                {
                    string filterStr = "";
                    Globals.DoPreFilterForKql(Request, web, CETYPE.CEAction.View, ref filterStr);
                    // George: Check if the search keyword is changed by SPE.
                    if (!queryStr.EndsWith(filterStr))
                    {
                        queryStr = "(" + queryStr + ") AND " + filterStr;
                    }
                    queryText.InnerText = queryStr;
                    finalXml = xmlDoc.InnerXml;
                }
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Debug, "Exception during DoSearchPrefilter:", null, ex);
            }
            return finalXml;
        }
    }
}