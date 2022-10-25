using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using Microsoft.Win32;
using Microsoft.SharePoint;
using Microsoft.SharePoint.Administration;
//using NextLabs.Diagnostic;

namespace NextLabs.Common
{
    // The goal of this class is to replace current URL's host with the
    // default one in AlternateUrl collection.
    // By Gavin Ye, Feb. 22, 2009
    //
    // Public API: string UrlUpdate(SPWebApplication spWebApp, string strUrl)
    // Parameter
    //   spWebApp - SPWebApplication in current context
    //   strUrl   - The url need o be checked and updated
    // Return
    //   string - return a string standing for new url
    //            If there is a default URL in Alternate URL collection, we
    //            use its host to replace coming in URL's
    //            If there is no such a default URL, return input URL
    public class CAlternateUrlCheck
    {
        // For exmaple:
        //   strUrl = "http://sps-web04.lab01.cn.nextlabs.com/sites/engineer/list/a.doc"
        //   default URL in AlternateURL collection = "http://sps-farm04.lab01.cn.nextlabs.com"
        //   return string = "http://sps-farm04.lab01.cn.nextlabs.com/sites/engineer/list/a.doc"
        public string UrlUpdate(SPWebApplication spWebApp, string strUrl)
        {
            string strProto = "";
            string strHost = "";
            string strBody = "";
            string strDefaultUrl = "";
            SPAlternateUrlCollection spAUC = null;

            // Sanity check
            if (null == spWebApp || string.IsNullOrEmpty(strUrl))
            {
                return strUrl;
            }

            strProto = GetProtoFromUrl(strUrl);
            if (string.IsNullOrEmpty(strProto))
            {
                // The url is not start with http:// or https://
                return strUrl;
            }

            strHost = GetHostFromUrl(strUrl);
            if (string.IsNullOrEmpty(strHost))
            {
                // If we can't find Host in the url, it is not a valid Url.
                return strUrl;
            }
            strBody = GetBodyFromUrl(strUrl);
            // Try to get default Alternate Urls
            spAUC = spWebApp.AlternateUrls;
            if (null == spAUC)
            {
                // No AlternateUrl Collection exists, return directly
                return strUrl;
            }
            for (int i = 0; i < spAUC.Count; i++)
            {
                SPAlternateUrl spUrl = spAUC[i];
                if (null == spUrl)
                    continue;
                string strAUrl = spUrl.Uri.ToString();
                if (string.IsNullOrEmpty(strAUrl))  // Not a valid URL
                    continue;
                string strAHost = GetHostFromUrl(strAUrl);
                if (string.IsNullOrEmpty(strAHost))                    // Can't find host
                    continue;

                if (SPUrlZone.Default == spUrl.UrlZone)
                {
                    // Get default URL
                    strDefaultUrl = strAUrl;
                    // The default URL is invalid, return directly directly
                    if (string.IsNullOrEmpty(strDefaultUrl))
                    {
                        return strUrl;
                    }
                    // Get host from default URL
                    strDefaultUrl = GetHostFromUrl(strDefaultUrl.ToLower());
                    // The default URL has no host, so it is invalid, return directly
                    if (string.IsNullOrEmpty(strDefaultUrl))
                    {
                        return strUrl;
                    }

                    break;  // We only need Default URL
                }
            }

            // If we can't find default URL or the coming in URL in not in alternate URL list
            // Do nothing change, return it directly
            if (string.IsNullOrEmpty(strDefaultUrl))
            {
                return strUrl;
            }

            // OKay, let's replace it's URL with the default Alternate URL
            if (strHost.ToLower().Equals(strDefaultUrl)) // strDefaultUrl has been set to Lower already
            {
                // It is using default, just return it
                return strUrl;
            }
            else
            {
                // replace the coming in url's host with the default alternate url's host
                string strNewUrl = "";
                string port = "";
                int index = strHost.IndexOf(":");
                //Fix bug 785, shall add the port number if it exists
                if (index > -1)
                {
                    port = strHost.Substring(index);
                    index = strDefaultUrl.IndexOf(":");
                    if (index > 0)
                    {
                        strDefaultUrl = strDefaultUrl.Substring(0, index);
                    }
                }
                strNewUrl = strProto + strDefaultUrl + port + strBody;
                return strNewUrl;
            }
        }

        private string GetProtoFromUrl(string strUrl)
        {
            int nPos = 0;
            string strProto = "";

            // Sanity check
            if (null == strUrl)
                return "";

            // Remove header
            nPos = strUrl.IndexOf("://");
            if (-1 == nPos) // Return empty, we think there is no "http://"
            {
                return "";
            }

            strProto = strUrl.Substring(0, nPos + 3);
            return strProto;
        }

        // E.g. whole url is "http://sps-farm04.lab01.cn.nextlabs.com/sites/engineer/list/a.doc"
        // The host should be "sps-farm04.lab01.cn.nextlabs.com"
        private string GetHostFromUrl(string strUrl)
        {
            int nPos = 0;
            string strHost = "";

            // Sanity check
            if (null == strUrl)
                return "";

            // Remove header
            nPos = strUrl.IndexOf("://");
            if (-1 == nPos) // Return empty, we think there is no "http://"
            {
                strHost = strUrl;
            }
            else
            {
                strHost = strUrl.Substring(nPos + 3);
            }

            // Remove tail
            nPos = strHost.IndexOf("/");
            if (-1 == nPos)
            {
                return strHost; // It is Host already if we can't find "/" after remove "://"
            }

            // Get host
            strHost = strHost.Substring(0, nPos);
            return strHost;
        }

        // E.g. whole url is "http://sps-farm04.lab01.cn.nextlabs.com/sites/engineer/list/a.doc"
        // The body should be "/sites/engineer/list/a.doc"
        private string GetBodyFromUrl(string strUrl)
        {
            int nPos = 0;
            string strBody = "";

            // Sanity check
            if (null == strUrl)
                return "";

            // Remove header
            nPos = strUrl.IndexOf("://");
            if (-1 == nPos)
            {
                // no "http://"
                nPos = 0;
            }
            else
            {
                nPos = nPos + 3;
            }

            // Remove tail
            nPos = strUrl.IndexOf("/", nPos);
            if (-1 == nPos)
            {
                // It's only host, no url body
                return "";
            }

            // Get host
            strBody = strUrl.Substring(nPos);
            return strBody;
        }

        private bool IsFqdn(string strHost)
        {
            if (-1 == strHost.IndexOf('.'))
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        private string GetHostFromFqdn(string strFqdn)
        {
            string host = "";
            int nPos = -1;
            if (string.IsNullOrEmpty(strFqdn))
            {
                return host;
            }

            nPos = strFqdn.IndexOf(".");
            if (-1 == nPos)
            {
                return host;
            }
            host = strFqdn.Substring(0, nPos);
            return host;
        }

        private bool IsSameHost(string strHost1, string strHost2)
        {
            bool bIsHost1Fqdn = true;
            bool bIsHost2Fqdn = true;

            // Sanity check
            if (null == strHost1 || null == strHost2)
                return false;

            bIsHost1Fqdn = IsFqdn(strHost1);
            bIsHost2Fqdn = IsFqdn(strHost2);
            if (bIsHost1Fqdn == bIsHost2Fqdn)
            {
                // If they are both FQDN or NonFQDN, compare directly
                return strHost1.ToLower().Equals(strHost2.ToLower());
            }
            else if (bIsHost1Fqdn && !bIsHost2Fqdn)
            {
                // If 1 is FQDN but 2 is not
                // Covert 1 to NonFQDN, and compare
                return strHost2.ToLower().Equals(GetHostFromFqdn(strHost1).ToLower());
            }
            else
            {
                // If 2 is FQDN but 1 is not
                // Covert 2 to NonFQDN, and compare
                return strHost1.ToLower().Equals(GetHostFromFqdn(strHost2).ToLower());
            }
        }
    }
}
