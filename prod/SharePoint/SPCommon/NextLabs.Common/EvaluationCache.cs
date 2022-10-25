using System;
using System.Collections.Generic;
using System.Text;

namespace NextLabs.Common
{
    public struct UserInfo
    {
        public String[] usernames;    
        public UInt64 time;
    }    
    public class EvaluationUserCache
    {
        public static EvaluationUserCache Instance = new EvaluationUserCache();
        private Dictionary<int, UserInfo> m_DicWithSessionIdKey;
        private Object m_Lock;
        public EvaluationUserCache()
        {
            m_DicWithSessionIdKey = new Dictionary<int, UserInfo>();
            m_Lock = new Object();
        }

        public void Add(int sessid, String[] usernames)
        {
            lock (m_Lock)
            {
                if (m_DicWithSessionIdKey.ContainsKey(sessid))
                {
                    UserInfo _UserInfo = m_DicWithSessionIdKey[sessid];
                    _UserInfo.usernames = usernames;
                    _UserInfo.time = ((UInt64)
                             ((DateTime.Now.ToUniversalTime() -
                               new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).
                              TotalMilliseconds));
                    m_DicWithSessionIdKey[sessid] = _UserInfo;
                }
                else
                {
                    UserInfo _UserInfo;
                    _UserInfo.usernames = usernames;
                    _UserInfo.time = ((UInt64)
                             ((DateTime.Now.ToUniversalTime() -
                               new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).
                              TotalMilliseconds));
                    m_DicWithSessionIdKey.Add(sessid, _UserInfo);
                }
            }
        }

        public bool IfCacheTimeOut(int sessid)
        {
            if (m_DicWithSessionIdKey.ContainsKey(sessid))
            {
                UInt64 current_time = ((UInt64)
                     ((DateTime.Now.ToUniversalTime() -
                       new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).
                      TotalMilliseconds));
                UserInfo _UserInfo = m_DicWithSessionIdKey[sessid];
                if ((current_time - _UserInfo.time) < 7200000)
                {
                    return false;
                }
                return true;
            }
            else
                return true;
        }

        public bool CompareValue(int sessid,String username)
        {
            if (m_DicWithSessionIdKey.ContainsKey(sessid))
            {
                UserInfo _UserInfo = m_DicWithSessionIdKey[sessid];
                for (int i = 0; i < _UserInfo.usernames.Length; i++)
                {
                    if (username.EndsWith(_UserInfo.usernames[i], StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
                return false;
            }
            else
                return false;
        }

        public void Clear()
        {
            lock (m_Lock)
            {
                m_DicWithSessionIdKey.Clear();
            }
        }
    }

    
    public class EvaluationCache
    {
        public static EvaluationCache Instance = new EvaluationCache();
        public static TimeSpan TimeOutInterval = new TimeSpan(0, 1, 0);

        private Dictionary<String, EvaluationCacheLevel2> m_DicWithSessionIdKey;
        private Object m_Lock;

        public EvaluationCache()
        {
            m_DicWithSessionIdKey = new Dictionary<string, EvaluationCacheLevel2>();
            m_Lock = new Object();
        }

        public void Add(string sessid, string userid, string guid, bool allow, string query, TimeSpan ttl, DateTime lastModifiedTime, DateTime lastEvalTime)
        {
            lock (m_Lock)
            {
                if (m_DicWithSessionIdKey.ContainsKey(sessid))
                {
                    EvaluationCacheLevel2 cache = m_DicWithSessionIdKey[sessid];
                    cache.Add(userid, guid, allow, query, ttl, lastModifiedTime, lastEvalTime);
                }
                else
                {
                    EvaluationCacheLevel2 cache = new EvaluationCacheLevel2();
                    cache.Add(userid, guid, allow, query, ttl, lastModifiedTime, lastEvalTime);
                    m_DicWithSessionIdKey.Add(sessid, cache);
                }
            }
        }

        public bool GetValue(string sessid, string userid, string guid, ref bool allow, ref string query, DateTime lastModifiedTime)
        {
            bool bExisted = false;

            lock (m_Lock)
            {
                if (m_DicWithSessionIdKey.ContainsKey(sessid))
                {
                    EvaluationCacheLevel2 cache = m_DicWithSessionIdKey[sessid];
                    bExisted = cache.GetValue(userid, guid, ref allow, ref query, lastModifiedTime);
                }
            }

            return bExisted;
        }

        public void Remove(string sessid)
        {
            lock (m_Lock)
            {
                if (m_DicWithSessionIdKey.ContainsKey(sessid))
                {
                    m_DicWithSessionIdKey.Remove(sessid);
                }
            }
        }

        public void Remove(string sessid, string userid)
        {
            lock (m_Lock)
            {
                if (m_DicWithSessionIdKey.ContainsKey(sessid))
                {
                    EvaluationCacheLevel2 cache = m_DicWithSessionIdKey[sessid];
                    cache.Remove(userid);
                }
            }
        }

        public void Remove(string sessid, string userid, string guid)
        {
            lock (m_Lock)
            {
                if (m_DicWithSessionIdKey.ContainsKey(sessid))
                {
                    EvaluationCacheLevel2 cache = m_DicWithSessionIdKey[sessid];
                    cache.Remove(userid, guid);
                }
            }
        }

        public void ClearTimeOut()
        {
            lock (m_Lock)
            {
                foreach (EvaluationCacheLevel2 cache in m_DicWithSessionIdKey.Values)
                {
                    cache.ClearTimeOut();
                }
            }
        }

        public void Clear()
        {
            lock (m_Lock)
            {
                m_DicWithSessionIdKey.Clear();
            }
        }
    }

    class EvaluationCacheLevel2
    {
        private Dictionary<string, EvaluationCacheLevel3> m_DicWithSidKey;

        public EvaluationCacheLevel2()
        {
            m_DicWithSidKey = new Dictionary<string, EvaluationCacheLevel3>();
        }

        public void Add(string userid, string guid, bool allow, string query, TimeSpan ttl, DateTime lastModifiedTime, DateTime lastEvalTime)
        {
            EvaluationCacheLevel3 cache;
            if (m_DicWithSidKey.ContainsKey(userid))
            {
                cache = m_DicWithSidKey[userid];
                EvaluationResult result = new EvaluationResult(allow, query, ttl, lastModifiedTime, lastEvalTime);
                cache.Add(guid, result);
            }
            else
            {
                cache = new EvaluationCacheLevel3();
                EvaluationResult result = new EvaluationResult(allow, query, ttl, lastModifiedTime, lastEvalTime);
                cache.Add(guid, result);
                m_DicWithSidKey.Add(userid, cache);
            }
        }

        public bool GetValue(string userid, string guid, ref bool allow, ref string query, DateTime lastModifiedTime)
        {
            bool bExisted = false;
            if (m_DicWithSidKey.ContainsKey(userid))
            {
                EvaluationCacheLevel3 cache = m_DicWithSidKey[userid];
                EvaluationResult result = cache.GetValue(guid, lastModifiedTime);
                if (result != null)
                {
                    bExisted = true;
                    allow = result.Allow;
                    query = result.Query;
                }
            }

            return bExisted;
        }

        public void Remove(string userid)
        {
            if (m_DicWithSidKey.ContainsKey(userid))
            {
                EvaluationCacheLevel3 cache = m_DicWithSidKey[userid];
                cache.Clear();

                m_DicWithSidKey.Remove(userid);
            }
        }

        public void Remove(string userid, string guid)
        {
            if (m_DicWithSidKey.ContainsKey(userid))
            {
                EvaluationCacheLevel3 cache = m_DicWithSidKey[userid];
                cache.Remove(guid);
            }
        }

        public void ClearTimeOut()
        {
            foreach (EvaluationCacheLevel3 cache in m_DicWithSidKey.Values)
            {
                cache.ClearTimeOut();
            }
        }

        public void Clear()
        {
            m_DicWithSidKey.Clear();
        }
    }

    class EvaluationCacheLevel3
    {
        private Dictionary<string, EvaluationResult> m_DicWithGuidKey;

        public EvaluationCacheLevel3()
        {
            m_DicWithGuidKey = new Dictionary<string, EvaluationResult>();
        }

        public void Add(string key, EvaluationResult result)
        {
            if (m_DicWithGuidKey.ContainsKey(key))
                m_DicWithGuidKey.Remove(key);

            m_DicWithGuidKey.Add(key, result);
        }

        public EvaluationResult GetValue(string key, DateTime lastModifiedTime)
        {
            EvaluationResult result = null;

            if (m_DicWithGuidKey.ContainsKey(key))
            {
                result = m_DicWithGuidKey[key];

                DateTime curTime = DateTime.Now;
                if (curTime - result.RefereshTime > result.TTL
                    || lastModifiedTime.CompareTo(result.LastModifiedTime) != 0)
                {
                    if (result.TTL.Minutes <= 0 && DateTime.Compare(result.LastEvalTime , Globals.g_BundleTime) > 0)
                    {
                        if (lastModifiedTime.CompareTo(result.LastModifiedTime) > 0)
                        {
                            m_DicWithGuidKey.Remove(key);
                            result = null;
                        }
                        else
                        {
                            result.RefereshTime = DateTime.Now;
                        }
                    }
                    else
                    {
                        m_DicWithGuidKey.Remove(key);
                        result = null;
                    }
                }
                else
                {
                    result.RefereshTime = DateTime.Now;
                }
            }

            return result;
        }

        public void Remove(string key)
        {
            if (m_DicWithGuidKey.ContainsKey(key))
            {
                m_DicWithGuidKey.Remove(key);
            }
        }

        public void ClearTimeOut()
        {
            DateTime curTime = DateTime.Now;

            foreach (string key in m_DicWithGuidKey.Keys)
            {
                EvaluationResult result = m_DicWithGuidKey[key];
                if (curTime - result.RefereshTime > result.TTL)
                {
                    m_DicWithGuidKey.Remove(key);
                }
            }
        }

        public void Clear()
        {
            m_DicWithGuidKey.Clear();
        }
    }

    class EvaluationResult
    {
        private bool m_bAllow;
        private String m_QueryString;
        private DateTime m_RefereshTime;
        private DateTime m_LastModifiedTime;
        private DateTime m_lastEvalTime;
        private TimeSpan m_TTL;

        public bool Allow
        {
            get { return m_bAllow; }
            set { m_bAllow = value; }
        }

        public String Query
        {
            get { return m_QueryString; }
            set { m_QueryString = value; }
        }

        public DateTime RefereshTime
        {
            get { return m_RefereshTime; }
            set { m_RefereshTime = value; }
        }

        public DateTime LastModifiedTime
        {
            get { return m_LastModifiedTime; }
            set { m_LastModifiedTime = value; }
        }

        public DateTime LastEvalTime
        {
            get { return m_lastEvalTime; }
            set { m_lastEvalTime = value; }
        }

        public TimeSpan TTL
        {
            get { return m_TTL; }
            set { m_TTL = value; }
        }

        public EvaluationResult(bool allow, String queryString, TimeSpan ttl, DateTime lastModifiedTime, DateTime lastEvalTime)
        {
            m_bAllow = allow;
            m_QueryString = queryString;
            m_RefereshTime = DateTime.Now;
            m_LastModifiedTime = lastModifiedTime;
            m_lastEvalTime = lastEvalTime;
            m_TTL = ttl;
        }
    }
}
