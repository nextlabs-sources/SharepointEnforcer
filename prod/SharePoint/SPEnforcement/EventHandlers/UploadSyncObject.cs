using System;
using System.Collections.Generic;
using System.Text;

namespace NextLabs.SPEnforcer
{
    public class UploadSyncObject
    {
        private static UploadSyncObject Instance = null;
        private Dictionary<String, Int32> m_SyncObjs;
        private Object m_LockObj;

        public UploadSyncObject()
        {
            m_SyncObjs = new Dictionary<string, int>();
            m_LockObj = new Object();
        }

        public static UploadSyncObject CreateInstance()
        {
            if (Instance == null)
            {
                Instance = new UploadSyncObject();
            }

            return Instance;
        }

        public void AddNewItem(String user, String ip, String listGuid)
        {
            string key = user + ip + listGuid;
            key = key.ToLower();
            lock (m_LockObj)
            {
                if (!m_SyncObjs.ContainsKey(key))
                {
                    m_SyncObjs[key] = 0;
                }
            }
        }

        public void UpdateExistedItem(String user, String ip, String listGuid, bool bInc)
        {
            string key = user + ip + listGuid;
            key = key.ToLower();
            lock (m_LockObj)
            {
                if (m_SyncObjs.ContainsKey(key))
                {
                    Int32 num = m_SyncObjs[key];
                    if (bInc)
                        m_SyncObjs[key] = ++num;
                    else
                        m_SyncObjs[key] = --num;
                }
            }
        }

        public Int32 QueryItem(String user, String ip, String listGuid)
        {
            Int32 num = 0;
            string key = user + ip + listGuid;
            key = key.ToLower();

            lock (m_LockObj)
            {
                if (m_SyncObjs.ContainsKey(key))
                {
                    num = m_SyncObjs[key];
                }
            }

            return num;
        }

        public void DeleteItem(String user, String ip, String listGuid)
        {
            string key = user + ip + listGuid;
            key = key.ToLower();

            lock (m_LockObj)
            {
                if (m_SyncObjs.ContainsKey(key))
                {
                    m_SyncObjs.Remove(key);
                }
            }
        }
    }
}
