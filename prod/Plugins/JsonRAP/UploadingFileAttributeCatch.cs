using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JsonRAP
{
    class UploadingFileAttributeCache
    {
        #region Singleton
        static private object s_obLockForInstance = new object();
        static private UploadingFileAttributeCache s_obUploadingFileAttributeCacheIns = null;
        static public UploadingFileAttributeCache GetInstance()
        {
            if (null == s_obUploadingFileAttributeCacheIns)
            {
                lock (s_obLockForInstance)
                    if (null == s_obUploadingFileAttributeCacheIns)
                    {
                        s_obUploadingFileAttributeCacheIns = new UploadingFileAttributeCache();
                    }
            }
            return s_obUploadingFileAttributeCacheIns;
        }
        #endregion

        #region Cache operation
        #endregion

        #region Members
        Dictionary<string, string> m_dicFileUrlAndHeaderInfo = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        #endregion
    }


}
