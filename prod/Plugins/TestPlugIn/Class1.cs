using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NextLabs.PreAuthAttributes;
using Microsoft.SharePoint;
using System.Diagnostics;
namespace TestPlugIn
{
    public class Class1 : IGetPreAuthAttributes
    {
        public int GetCustomAttributes(object spUser, object spSource, object SPFile, string strAction, Dictionary<string, string> userPair, Dictionary<string, string> srcPair, Dictionary<string, string> dstPair, int nobjecttype, int nTimeout = 3000)
        {
            int iResult = 0;
            if (spSource is SPWeb)
            {
                Trace.WriteLine("bear------this is SPWeb");
                userPair.Add("User_SPWeb_Dll_B_Key_1", "User_SPWeb_DllB_Value_1");
                srcPair.Add("Src_SPWeb_Dll_B_Key_1", "Src_SPWeb_DLL_B_Value_1");
                dstPair.Add("Dst_SPWeb_Dll_B_Key_1", "Dst_SPWeb_Dll_B_Value_1");
            }
            else if (spSource is SPList)
            {
                Trace.WriteLine("bear------this is splist");
                userPair.Add("User_List_Dll_B_Key_1", "User_List_DllB_Value_1");
                srcPair.Add("Src_List_Dll_B_Key_1", "Src_List_DLL_B_Value_1");
                dstPair.Add("Dst_List_Dll_B_Key_1", "Dst_List_Dll_B_Value_1");
            }
            else if (spSource is SPListItem)
            {
                Trace.WriteLine("bear------this is SPListItem");
                userPair.Add("User_SPListItem_Dll_B_Key_1", "User_SPListItem_DllB_Value_1");
                srcPair.Add("Src_SPListItem_Dll_B_Key_1", "Src_SPListItem_DLL_B_Value_1");
                dstPair.Add("Dst_SPListItem_Dll_B_Key_1", "Dst_SPListItem_Dll_B_Value_1");
            }
            else
            {
                Trace.WriteLine("bear------this is Other");
                userPair.Add("User_List_Dll_B_Key_1", "User_List_DllB_Value_1");
                srcPair.Add("Src_List_Dll_B_Key_1", "Src_List_DLL_B_Value_1");
                dstPair.Add("Dst_List_Dll_B_Key_1", "Dst_List_Dll_B_Value_1");
            }
            return iResult;
        }
    }
}
