using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace JsonRAP
{
    static class BaseCommon
    {
        public static string converToJSON(XmlDocument doc)
        {
            String json = null;
            try
            {
                json = JsonConvert.SerializeXmlNode(doc);
                Trace.WriteLine("JSONRAP: Json string |" + json);
            }
            catch (Exception e)
            {
                Trace.TraceError("JSONRAP: Json Conversion Error " + e.Message);
            }
            return json;
        }

        public static Encoding GetEncoding(string filePath)
        {
            // Read the BOM
            var bom = new byte[4];
            using (var file = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                file.Read(bom, 0, 4);
            }

            // Analyze the BOM
            if (bom[0] == 0x2b && bom[1] == 0x2f && bom[2] == 0x76) return Encoding.UTF7;
            if (bom[0] == 0xef && bom[1] == 0xbb && bom[2] == 0xbf) return Encoding.UTF8;
            if (bom[0] == 0xff && bom[1] == 0xfe) return Encoding.Unicode; //UTF-16LE
            if (bom[0] == 0xfe && bom[1] == 0xff) return Encoding.BigEndianUnicode; //UTF-16BE
            if (bom[0] == 0 && bom[1] == 0 && bom[2] == 0xfe && bom[3] == 0xff) return Encoding.UTF32;
            return Encoding.ASCII;
        }

        public static bool FlagUpdateIfNotEqual(ref bool bOldFlagRef, bool bNewFlag)
        {
            bool bUpdateRet = false;
            if (bOldFlagRef != bNewFlag)
            {
                bOldFlagRef = bNewFlag;
                bUpdateRet = true;
            }
            return bUpdateRet;
        }
    }
}
