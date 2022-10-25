using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Xml;
using System.Xml.Serialization;
using System.Windows.Forms;
using NextLabs.CSCInvoke;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using System.Threading;
namespace WorkFlowAdapter
{
    public class ProgressThreadMethod
    {
        private ProgressBar m_Progress;
        public ProgressThreadMethod()
        {

        }
        public void ThreadRun()
        {
            m_Progress = new ProgressBar();
            Application.EnableVisualStyles();
            m_Progress.progressBar1.Show();
            m_Progress.progressBar1.Style = ProgressBarStyle.Marquee;
            m_Progress.progressBar1.Visible = true; ;
            m_Progress.ShowDialog();
        }
    }    
    
    public class ThreadMethod
    {

        private BrowserForm m_Browser;
        private string LastUrl;
        private string logID;
        public ThreadMethod(BrowserForm _Browser, string _LastUrl, string _logID)
        {
            m_Browser = _Browser;
            LastUrl = _LastUrl;
            logID = _logID;
        }

        public void ThreadRun()
        {
            DialogResult MsgBoxResult = System.Windows.Forms.MessageBox.Show(m_Browser,"Would you like cancel this workflow process",
                "NextLabs Workflow Obligation",
                System.Windows.Forms.MessageBoxButtons.YesNo,
                System.Windows.Forms.MessageBoxIcon.Warning);
            if (MsgBoxResult == DialogResult.Yes)
            {
                //exit
                WfSPHandler.DoLogObligation("Cancel", logID);
                m_Browser.Stop();
                m_Browser.bCloseInside = true;
                m_Browser.Close();
            }
            else
            {
                m_Browser.Stop();
                m_Browser.Navigate(LastUrl);
            }
        }
    }
    
    class WfHanlderUtility
    {
        protected string listPath = null;
        protected string filePath = null;
	protected string filePathCopied = null;
        protected string fileName = null;
        protected string itemPath = null;
        protected int ItemId = 0;
        //Workflow parameters
        protected string spAssociation = null;
        protected string workflowPath = null;
        protected string logID = null;
        //User information
        protected string userName = null;
        protected string userPassword = null;
        protected string userDomain = null;
        protected string tryTimes = null;

        protected int intervaltime = 0;
        //Unique name
        protected bool bIfUniqueName = false;
        protected bool bInitFromBrowser = false;

        protected bool bIfFailed = false;
        protected List<string> argscl = null;
        protected List<string> columnNames = null;
        protected List<string> columnValues = null;

        protected void InitParameters()
        {
            listPath = null;
            filePath = null;
            fileName = null;
            itemPath = null;
            spAssociation = null;
            workflowPath = null;
            logID = null;
            userName = null;
            userPassword = null;
            userDomain = null;
            bIfUniqueName = false;
            bInitFromBrowser = false;
            bIfFailed = false;
        }

        protected void AnalyzeInputParameters(string[] args)
        {
            argscl = new List<string>();
            for (int k = 0; k < args.Length; k++)
            {
                if (args[k].Equals("-username", StringComparison.OrdinalIgnoreCase)
                    || args[k].Equals("-password", StringComparison.OrdinalIgnoreCase)
                    || args[k].Equals("-domain", StringComparison.OrdinalIgnoreCase))
                {
                    k++;
                    if (k < args.Length && args[k] == "")
                    {
                        args[k] = "dummy";
                    }
                }
            }
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].StartsWith("-"))
                {
                    string keyword = args[i];
                    string value = "";
                    if (args[i].Equals("-unique-name",StringComparison.OrdinalIgnoreCase))
                    {
                        bIfUniqueName = true;
                    }
                    else if (args[i].Equals("-sp-initfrombrowser", StringComparison.OrdinalIgnoreCase))
                    {
                        bInitFromBrowser = true;
                    }
                    argscl.Add(keyword);
                    i++;
                    for (int j = i; ; j++)
                    {
                        if (j >= args.Length || args[j].StartsWith("-"))
                        {
                            if (i == j)
                            {
                                argscl.RemoveAt(argscl.Count - 1);
                            }
                            i = j;
                            i--;
                            break;
                        }
                        if (i == j)
                            value = args[j];
                        else
                            value += " " + args[j];
                    }
                    if(value != null && value != "")
                        argscl.Add(value);
                }
            }
            string[] values = argscl.ToArray();
            CatlalogParameters(values);
        }

        protected void CatlalogParameters(string[] values)
        {
            for (int i = 0; i < values.Length; i+= 2)
            {
                if (values[i] != null)
                    values[i] = values[i].ToLower();
                switch (values[i])
                {
                    case "-sp-location":
                        listPath = values[i + 1];
                        break;
                    case "-target":
                        filePath = values[i + 1];
                        if (filePath.Length > 2 &&
                            filePath[0] == '/' && filePath[1] == '/' && filePath[2] != '/')
                        {
                            filePath=filePath.Replace('/', '\\');
                        }
                        break;
                    case "-sp-association":
                        spAssociation = values[i + 1];
                        break;
                    case "-username":
                        userName = values[i + 1];
                        break;
                    case "-password":
                        userPassword = values[i + 1];
                        break;
                    case "-domain":
                        userDomain = values[i + 1];
                        break;
                    case "-logid":
                            logID = values[i + 1];
                        break;
                    case "-try-times":
                        tryTimes = values[i + 1];
                        break;
                    case "-interval-time":
                        intervaltime = (Convert.ToInt32(values[i + 1])) * 60;
                        break;
                    case "-sp-wfinput":
                        string wfinput = values[i + 1];
                        string[] wfinputs = wfinput.Split(new string[] { ";" }, StringSplitOptions.None);
                        for (int j = 0; j < wfinputs.Length; j++)
                        {
                            int pos = wfinputs[j].IndexOf("=");
                            if (pos != -1)
                            {
                                if (columnNames == null)
                                    columnNames = new List<string>();
                                if (columnValues == null)
                                    columnValues = new List<string>();
                                string name = wfinputs[j].Substring(0, pos);
                                string value = wfinputs[j].Substring(pos + 1);
                                columnNames.Add(name);
                                columnValues.Add(value);
                            }
                        }
                        break;
                    default:
                        break;
                }
                string columnkey = "-sp-wfinput-";
                if (values[i].IndexOf(columnkey) != -1)
                {
                    if(columnNames == null)
                        columnNames = new List<string>();
                    if (columnValues == null)
                        columnValues = new List<string>();
                    string columnName = values[i].Substring(columnkey.Length);
                    columnNames.Add(columnName);
                    columnValues.Add(values[i + 1]);
                }
            }
            if (listPath == null || filePath == null || spAssociation == null)
            {
                Trace.WriteLine("listPath or filePath or spAssociation is null");
                bIfFailed = true;
            }
        }

        protected void StoreCommandLines(string[] args)
        {
            if (tryTimes != null && Convert.ToInt32(tryTimes) > 0)
            {
                if (intervaltime <= 0)
                    intervaltime = 5 * 60;
                UInt64 current_time = ((UInt64)
                             ((DateTime.Now.ToUniversalTime() -
                               new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds)) + (UInt64)(intervaltime);
                string _currenttime = current_time.ToString() + " ";
                string _fileName = Process.GetCurrentProcess().MainModule.FileName + " ";
                string _filePath = _fileName.Substring(0, _fileName.LastIndexOf("\\")) + "\\";
                int itryTimes = Convert.ToInt32(tryTimes) - 1;
                Random _Random = new Random();
                string filePath = _filePath + "retry\\" + _Random.Next().ToString() + ".retry";
                FileStream fs = new FileStream(filePath, FileMode.Create);
                string[] argsvalues = argscl.ToArray();
                if (fs != null)
                {
                    byte[] _bcurrenttime = System.Text.Encoding.Default.GetBytes(_currenttime);
                    fs.Write(_bcurrenttime, 0, _bcurrenttime.Length);
                    byte[] _bfileName = System.Text.Encoding.Default.GetBytes(_fileName);
                    fs.Write(_bfileName, 0, _bfileName.Length);
                    for (int i = 0; i < argsvalues.Length; i++)
                    {
                        if (argsvalues[i] != null && argsvalues[i].Equals("-try-times", StringComparison.OrdinalIgnoreCase))
                        {
                            string _retrykeyword = " " + argsvalues[i];
                            byte[] input = System.Text.Encoding.Default.GetBytes(_retrykeyword);
                            fs.Write(input, 0, _retrykeyword.Length);
                            i++;
                            string _trytimes = " " + Convert.ToString(itryTimes);
                            input = System.Text.Encoding.Default.GetBytes(_trytimes);
                            fs.Write(input, 0, _trytimes.Length);
                        }
                        else
                        {
                            string _input = " " + argsvalues[i];
                            byte[] input = System.Text.Encoding.Default.GetBytes(_input);
                            fs.Write(input, 0, _input.Length);
                        }
                    }
                    if (bIfUniqueName)
                    {
                        string _input = " " + "-unique-name";
                        byte[] input = System.Text.Encoding.Default.GetBytes(_input);
                        fs.Write(input, 0, _input.Length);
                    }
                    if (bInitFromBrowser)
                    {
                        string _input = " " + "-sp-initfrombrowser";
                        byte[] input = System.Text.Encoding.Default.GetBytes(_input);
                        fs.Write(input, 0, _input.Length);
                    }

                    fs.Close();
                }
            }
        }

    }

    interface WfHanlder
    {
        bool Process(string[] argv,int argc);
        bool SetErrorCode(int errCode);
        int GetErrorCdoe();
    }

    class WfHandlerFactory
    {
        [DllImport("kernel32.dll", EntryPoint = "LoadLibrary")]
        public static extern int LoadLibrary([MarshalAs(UnmanagedType.LPStr)] string lpLibFileName);
        public static bool LoadSDKLibrary()
        {
            try
            {
                int re = 0;
                string[] librarynames = { "CEBRAIN.dll", "CECEM.dll", "CEMARSHAL.dll", "CETRANSPORT.dll", "CEPEPMAN.dll", "CECONN.dll", "CEEVAL.dll" ,"CELOGGING.dll"};
                string[] librarynames32 = { "CEBRAIN32.dll", "CECEM32.dll", "CEMARSHAL5032.dll", "CETRANSPORT32.dll", "CEPEPMAN32.dll", "CECONN32.dll", "CEEVAL32.dll", "CELOGGING32.dll" };
                RegistryKey PPC_key = Registry.LocalMachine.OpenSubKey("Software\\NextLabs\\Compliant Enterprise\\Policy Controller\\", false);
                object RegPPCInstallDir = null;
                string PPCBinDir = null;
                if (PPC_key != null)
                    RegPPCInstallDir = PPC_key.GetValue("InstallDir");
                if (RegPPCInstallDir != null)
                {
                    String RegPPCInstallDir_str = Convert.ToString(RegPPCInstallDir);
                    if (RegPPCInstallDir_str.EndsWith("\\"))
                        PPCBinDir = RegPPCInstallDir_str + "Common\\";
                    else
                        PPCBinDir = RegPPCInstallDir_str + "\\Common\\";
                }
                if (IntPtr.Size == 4)
                {
                    PPCBinDir += "bin32\\";
                    for (int i = 0; i < librarynames.Length; i++)
                    {
                        re = LoadLibrary(PPCBinDir + librarynames32[i]);
                    }    
                }
                else
                {
                    PPCBinDir += "bin64\\";
                    for (int i = 0; i < librarynames.Length; i++)
                    {
                        re = LoadLibrary(PPCBinDir + librarynames[i]);
                    }    
                }
                return true;
            }
            catch (Exception e)
            {
                Trace.WriteLine("call LoadSDKLibrary failed:" + e.Message);
            }
            return false;
        }

        
        static public WfHanlder CreateWfHanlder(string strEngineName)
        {
            if (strEngineName != null)
                strEngineName = strEngineName.ToLower();
            switch (strEngineName)
            {
                case "sharepoint":
                    LoadSDKLibrary();
                    return new WfSPHandler();
                default:
                    return null;

            }
        }

        static public WfHanlder CreateWfHanlderAlternative(string[] argv, int argc)
        {
            for (int i = 0; i < argv.Length; i++)
            {
                if (argv[i].Equals("-engine", StringComparison.OrdinalIgnoreCase))
                {
                    if (i + 1 < argv.Length)
                    {
                        return CreateWfHanlder(argv[i+1]);
                    }
                }
            }
            return null;
        }
    }

    class WfSPHandler : WfHanlderUtility, WfHanlder
    {
        private WSSCEService.DocUpload _DocUpload = null;
        private Thread _ProgressThread = null;
        ProgressThreadMethod _ProgressThreadMethod = null;
        public BrowserForm m_Browser;



        public static void DoLogObligation(string status, string logID)
        {
            try
            {
                if (logID != null && logID != "")
                {
                    CETYPE.CEResult_t call_result;
                    CETYPE.CEApplication app =
                        new CETYPE.CEApplication("SharePoint", null, null);
                    CETYPE.CEUser user = new CETYPE.CEUser("dummyName", "dummyId");
                    IntPtr localConnectHandle = IntPtr.Zero;

                    call_result = CESDKAPI.CECONN_Initialize(app, user, null,
                                                        out localConnectHandle,
                                                        5 * 1000);
                    if (call_result != CETYPE.CEResult_t.CE_RESULT_SUCCESS)
                    {
                        return;
                    }
                    string[] attr_value = { "Status", status };
                    call_result = CESDKAPI.CELOGGING_LogObligationData(localConnectHandle, logID, "WorkFlow Adapter", ref attr_value);
                    CESDKAPI.CECONN_Close(localConnectHandle,
                                          5 * 1000);
                }
            }
            catch (Exception e)
            {
                Trace.WriteLine("DoLogObligation Exception happened:"+e.Message);
            }
        }



        public bool Process(string[] argv, int argc)
        {
            try
            {
                Mutex _lock = null;
                bool _haslock = true;
                InitParameters();
                AnalyzeInputParameters(argv);
                if (!bIfFailed)
                {
                    if (bInitFromBrowser)
                    {
                        _lock = new Mutex(true, "WorkFlowLock", out _haslock);
                        if (_haslock)
                        {
                            _ProgressThreadMethod = new ProgressThreadMethod();
                            _ProgressThread = new Thread(new ThreadStart(_ProgressThreadMethod.ThreadRun));
                            _ProgressThread.Start();
                        }                       
                    }
                    GenerateWSSCEService();
                    UploadDocument();
                    SetColumns();
                }
                if (!bIfFailed)
                {
                    if (!bInitFromBrowser)
                    {
                        //new code
                        StartWorkFlowFromWSSCEService();
                    }
                    else
                    {                       
                        try
                        {
                            _lock.WaitOne();
                        }
                        catch
                        {
                        }
                        //old code
                        try
                        {
                            StartWorkFlowFromWSSWorFlow();
                        }
                        catch(Exception e)
                        {
                            Trace.WriteLine("StartWorkFlowFromWSSWorFlow Exception:" + e.Message);
                            bIfFailed = true;
                        }
                        try
                        {
                            _lock.ReleaseMutex();
                        }
                        catch
                        {
                        }
                    }                    
                }                    
            }
            catch (Exception e)
            {
                Trace.WriteLine("Exception:" + e.Message);
                bIfFailed = true;
            }
	        try
            {
                //sometimes the progress bar will not terminate normally
                if (_ProgressThread != null)
                {
                    _ProgressThread.Abort();
                    _ProgressThread = null;
                }
                if (filePathCopied != null)
                    System.IO.File.Delete(filePathCopied);
            }
            catch (System.Exception e)
            {
            }
            try
            {
                if (bIfFailed)
                {
                    StoreCommandLines(argv);
                }
            }
            catch (Exception e)
            {
                Trace.WriteLine("Exception:" + e.Message);
            }
            return bIfFailed;
        }
        public bool SetErrorCode(int errCode)
        {
            return true;
        }
        public int GetErrorCdoe()
        {
            return 0;
        }

        void GenerateWSSCEService()
        {
            _DocUpload = new WSSCEService.DocUpload();
            _DocUpload.PreAuthenticate = true;
            if (userName != null && userPassword != null && userDomain != null
                && userName != "dummy" && userPassword != "dummy" && userDomain != "dummy")
            {
                _DocUpload.Credentials = new NetworkCredential(userName, userPassword, userDomain);
            }
            else
            {
                _DocUpload.Credentials = CredentialCache.DefaultCredentials;
            }
            string _withouthead = listPath.Remove(0, 7);
            string _sitepath = "http://" + _withouthead.Substring(0, _withouthead.IndexOf("/")) + "/_layouts/ceSPService.asmx";
            _DocUpload.Url = _sitepath;
        }
	    static string GetTempPathFileName(string fileName)
        {
            string tempPath = Path.GetTempPath();
            fileName = tempPath + Path.GetFileName(fileName);
            string fileNameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
            string fileExt = Path.GetExtension(fileName);

            int i = 0;
            while(File.Exists(fileName))
            {
                fileName = tempPath + fileNameWithoutExt + string.Format("({0})", ++i) + fileExt;
            }
            return fileName;
        }
        void UploadDocument()
        {
            FileStream fs = null;// new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            try
            {
                fs = new FileStream(filePath, FileMode.Open, FileAccess.Read,FileShare.Read);
            }
            catch (System.IO.IOException e)
            {
                Trace.WriteLine("IOException when open file:" + filePath + "(" + e.Message + ")");
                if (e.Message.Contains("because it is being used by another process") == true)
                {
                    bIfFailed = true;
                    filePathCopied=filePath;
                    filePathCopied = GetTempPathFileName(filePathCopied);//System.Environment.GetFolderPath(Environment.SpecialFolder.InternetCache);
                    System.IO.File.Copy(filePath, filePathCopied, true);
                    bIfFailed = false;
                    fs = new FileStream(filePathCopied, FileMode.Open, FileAccess.Read);
                }
            }
            //catch (System.Exception e)
            //{
            //    Trace.WriteLine("Exception when open file:" + filePath + "(" + e.Message + ")");
            //}

            if (fs != null && fs.Length > 0)
            {                
                Trace.WriteLine("The file length:" + fs.Length);
                byte[] fileContent = new byte[fs.Length];
                int iReadLen=fs.Read(fileContent, 0, (int)fs.Length);
                Trace.WriteLine("The file length read by:" + iReadLen);
                if (filePath.LastIndexOf("\\") != -1)
                    fileName = filePath.Substring(filePath.LastIndexOf("\\") + 1);
                else
                    fileName = filePath.Substring(filePath.LastIndexOf("/") + 1);                
                string _itemPath = null;
                string _webUrl = null;
                string _re = _DocUpload.UploadDocument(fileName, fileContent, listPath, ref ItemId, ref _itemPath, ref _webUrl, bIfUniqueName);
                workflowPath = _webUrl + "/_vti_bin/workflow.asmx";
                itemPath = _itemPath;
                fs.Close();                
            }
            else
            {
                bIfFailed = true;
            }
        }

        void SetColumns()
        {
            Guid guid = new Guid();    
            if (columnNames != null && columnNames.Count > 0 && columnValues != null && columnValues.Count > 0)
            {
                for (int i = 0; i < columnNames.Count; i++)
                {
                    string re = _DocUpload.SetColumns(ItemId, guid, listPath, columnNames[i], columnValues[i]);
                }
            }
        }

        void StartWorkFlowFromWSSCEService()
        {
            bool bHasWorkFlowRunning = false;
            int iWorkFlowStatus = 0;
            string re = _DocUpload.StartWorkFlow(ItemId, "", listPath, spAssociation, logID, true, ref bHasWorkFlowRunning, ref iWorkFlowStatus);
            if (re != null && re.Equals("Workflow Start faile", StringComparison.OrdinalIgnoreCase))
            {
                DoLogObligation("Error", logID);
            }
            if (bHasWorkFlowRunning)
            {
                Trace.WriteLine("_DocUpload.StartWorkFlow this kind of workflow alread exist");
            }
            else
            {
                Trace.WriteLine("_DocUpload.StartWorkFlow workflow status:" + iWorkFlowStatus);
                if ((iWorkFlowStatus & 0x40) != 0)
                {
                    DoLogObligation("Start with Error", logID);
                    bIfFailed = true;
                }
                else
                {
                    DoLogObligation("Start", logID);
                }
            }
        }

        XmlNodeList GetWorkFlowKeyNodes(XmlNode xmlNodeData, string nodekey)
        {
            XmlDocument xmlDoc = new XmlDocument();
            string xmlString = "<?xml version=\"1.0\" ?>";
            xmlString += xmlNodeData.OuterXml;
            xmlDoc.LoadXml(xmlString);
            const string SharePointNamespacePrefix = "spwf";
            const string SharePointNamespaceURI = "http://schemas.microsoft.com/sharepoint/soap/workflow/";
            XmlNamespaceManager namespaceMgr = new XmlNamespaceManager(xmlDoc.NameTable);
            namespaceMgr.AddNamespace(SharePointNamespacePrefix, SharePointNamespaceURI);
            XmlNodeList xmlTemplates = xmlDoc.SelectNodes(nodekey, namespaceMgr);
            return xmlTemplates;
        }
        
        public void BrowserWinThread(string strUrl,string logid,string _itemPath)
        {
            logID = logid;
            itemPath = _itemPath;
            Submitted = false;
            StartButtonClicked = false;
            Console.WriteLine("The initialize URL is " + strUrl);
            m_Browser = new BrowserForm();
            m_Browser.Navigate(strUrl);
            m_Browser.Visible = true;
            int IEVersion=m_Browser.webBrowser.Version.Major;
            if (IEVersion == 6)
            {
                m_Browser.webBrowser.Navigated += new System.Windows.Forms.WebBrowserNavigatedEventHandler(webBrowser_Navigated_IE6);
                m_Browser.webBrowser.Navigating += new System.Windows.Forms.WebBrowserNavigatingEventHandler(webBrowser_Navigating_IE6);
            }
            else 
            {
                m_Browser.webBrowser.Navigated += new System.Windows.Forms.WebBrowserNavigatedEventHandler(webBrowser_Navigated_IE7);
                m_Browser.webBrowser.Navigating += new System.Windows.Forms.WebBrowserNavigatingEventHandler(webBrowser_Navigating_IE7);
            }
            m_Browser.FormClosing += new FormClosingEventHandler(webBrowser_Leave);
            m_Browser.Visible = false;
            m_Browser.webBrowser.ScriptErrorsSuppressed = true;
            m_Browser.ShowDialog();
            return;
        }

        void webBrowser_Leave(object sender, FormClosingEventArgs e)
        {
            if (Submitted != true && m_Browser.bCloseInside != true)
            {
                {
                    if (StartButtonClicked == false)
                    {
                        DialogResult MsgBoxResult = System.Windows.Forms.MessageBox.Show("Would you like cancel this workflow process",
                            "NextLabs Workflow Obligation",
                            System.Windows.Forms.MessageBoxButtons.YesNo,
                            System.Windows.Forms.MessageBoxIcon.Warning);
                        if (MsgBoxResult == DialogResult.Yes)
                        {
                            //exit
                            DoLogObligation("Cancel", logID);
                        }
                        else
                        {
                            e.Cancel = true;
                        }
                    }
                }
            }
        }

        void webBrowser_Navigating_IE6(object sender, System.Windows.Forms.WebBrowserNavigatingEventArgs e)
        {
            if (m_Browser.webBrowser.ReadyState < System.Windows.Forms.WebBrowserReadyState.Complete)
                return;
            if (Submitted != true)
            {
                if (LastUrl != null)
                {
                    if (StartButtonClicked == false)
                    {
                        if (e.Url != null && itemPath != null && e.Url.ToString().Equals(itemPath, StringComparison.OrdinalIgnoreCase))
                        {
                            DialogResult MsgBoxResult = System.Windows.Forms.MessageBox.Show("Would you like cancel this workflow process",
                                "NextLabs Workflow Obligation",
                                System.Windows.Forms.MessageBoxButtons.YesNo,
                                System.Windows.Forms.MessageBoxIcon.Warning);
                            if (MsgBoxResult == DialogResult.Yes)
                            {
                                //exit
                                DoLogObligation("Cancel", logID);
                                m_Browser.Stop();
                                m_Browser.bCloseInside = true;
                                m_Browser.Close();
                            }
                            else
                            {
                                m_Browser.Stop();
                                m_Browser.Navigate(LastUrl);
                            }
                            LastUrl = null;
                        }
                    }
                }
            }
        }
        
        void webBrowser_Navigated_IE6(object sender, System.Windows.Forms.WebBrowserNavigatedEventArgs e)
        {
            if (m_Browser.webBrowser.ReadyState < System.Windows.Forms.WebBrowserReadyState.Complete)
            {
                if (m_Browser.webBrowser.ReadyState == System.Windows.Forms.WebBrowserReadyState.Loading &&
                    m_Browser.webBrowser.Url.Scheme == "file" &&
                    m_Browser.webBrowser.Url.Segments[m_Browser.webBrowser.Url.Segments.Length - 1] == "wfMsg.html")
                {
                    System.Windows.Forms.HtmlElement btnCloseForm = m_Browser.webBrowser.Document.GetElementById("btnCloseForm");
                    if (btnCloseForm != null)
                    {
                        btnCloseForm.Click += new HtmlElementEventHandler(btnCloseForm_Click);
                    }
                }
                return;
            }
            if (LastUrl == null)
            {
                System.Windows.Forms.HtmlElement btnElementStart = m_Browser.webBrowser.Document.GetElementById("V1_I1_B12");
                if (btnElementStart != null)
                {
                    LastUrl = m_Browser.webBrowser.Url.ToString();
                    btnElementStart.Click += new System.Windows.Forms.HtmlElementEventHandler(btnElementStart_Click);
                }
                else
                {
                    Console.WriteLine("fail to find Start button");
                }
            }
            else
            {
                if (m_Browser.webBrowser.Url.ToString() != LastUrl)
                {
                    if (Submitted == true)
                    {
                        //m_Browser.webBrowser.Navigated -= new System.Windows.Forms.WebBrowserNavigatedEventHandler(webBrowser_Navigated);
                        string _fileName = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName + " ";
                        string _filePath = _fileName.Substring(0, _fileName.LastIndexOf("\\")) + "\\";
                        _filePath += "wfMsg.html";
                        m_Browser.Navigate(_filePath);
                        return;
                    }
                    else
                    {
                        if (LastUrl != null)
                        {
                            if (StartButtonClicked == false)
                            {
                                System.Windows.Forms.Control.CheckForIllegalCrossThreadCalls = false;
                                ThreadMethod method = new ThreadMethod(m_Browser, LastUrl, logID);
                                Thread t = new Thread(new ThreadStart(method.ThreadRun));
                                m_Browser.Navigate(LastUrl);
                                t.Start();
                                LastUrl = null;
                            }
                        }
                    }
                }
                else
                {
                    System.Windows.Forms.HtmlElement btnElementStart = m_Browser.webBrowser.Document.GetElementById("V1_I1_B12");
                    if (btnElementStart != null)
                    {
                        LastUrl = m_Browser.webBrowser.Url.ToString();
                    }
                    else
                    {
                        Console.WriteLine("You clicked Start button");
                        Submitted = true;
                    }
                }
            }
        }
        
        void webBrowser_Navigating_IE7(object sender, System.Windows.Forms.WebBrowserNavigatingEventArgs e)
        {
            System.Windows.Forms.WebBrowserReadyState state = m_Browser.webBrowser.ReadyState;
            HtmlElement actElmt = m_Browser.webBrowser.Document.ActiveElement;
            Uri uri = m_Browser.webBrowser.Url;
            if (actElmt != null && actElmt.TagName.Equals("input", StringComparison.OrdinalIgnoreCase) && (actElmt.Id.StartsWith("ctl00_PlaceHolderMain_XmlFormControl_V1_I1", StringComparison.OrdinalIgnoreCase) ||
                                   actElmt.GetAttribute("value").Equals("Start", StringComparison.OrdinalIgnoreCase)
                                   || actElmt.Id.StartsWith("__DialogFocusRetainer", StringComparison.OrdinalIgnoreCase)))
            {
                StartButtonClicked = true;
            }
        }
       
        void webBrowser_Navigated_IE7(object sender, System.Windows.Forms.WebBrowserNavigatedEventArgs e)
        {
            if (WorkflowInitPageUrl == null)
            {
                WorkflowInitPageUrl = m_Browser.webBrowser.Url.ToString();
                LastUrl = m_Browser.webBrowser.Url.ToString();
                string _fileName = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName + " ";
                MsgHtmlPath = _fileName.Substring(0, _fileName.LastIndexOf("\\")) + "\\";
                MsgHtmlPath += "wfMsg.html";
                return;
            }
            System.Windows.Forms.WebBrowserReadyState state = m_Browser.webBrowser.ReadyState;
            HtmlElement actElmt = m_Browser.webBrowser.Document.ActiveElement;
            Uri uri = m_Browser.webBrowser.Url;
            if (StartButtonClicked)
            {
                if (uri.ToString() != WorkflowInitPageUrl)
                {
                    if (uri.LocalPath != null && !uri.LocalPath.Equals(MsgHtmlPath, StringComparison.OrdinalIgnoreCase))
                    {
                        m_Browser.Navigate(MsgHtmlPath);
                    }
                    else if (uri.LocalPath != null && uri.LocalPath.Equals(MsgHtmlPath, StringComparison.OrdinalIgnoreCase))
                    {
                        System.Windows.Forms.HtmlElement btnCloseForm = m_Browser.webBrowser.Document.GetElementById("btnCloseForm");
                        if (btnCloseForm != null)
                        {
                            btnCloseForm.Click += new HtmlElementEventHandler(btnCloseForm_Click);
                        }
                    }
                }
            }
            else
            {
                if (uri.ToString() != WorkflowInitPageUrl)
                {
                    DialogResult MsgBoxResult = System.Windows.Forms.MessageBox.Show("Would you like cancel this workflow process",
                                    "NextLabs Workflow Obligation",
                                    System.Windows.Forms.MessageBoxButtons.YesNo,
                                    System.Windows.Forms.MessageBoxIcon.Warning);
                    if (MsgBoxResult == DialogResult.Yes)
                    {
                        //exit                        
                        m_Browser.Stop();
                        m_Browser.bCloseInside = true;
                        m_Browser.Close();
                        DoLogObligation("Cancel", logID);
                    }
                    else
                    {
                        m_Browser.Stop();
                        m_Browser.Navigate(LastUrl);
                        StartButtonClicked = false;
                        WorkflowInitPageUrl = null;
                    }
                }
            }
            
            return;

        }

        void btnCloseForm_Click(object sender, HtmlElementEventArgs e)
        {
            m_Browser.bCloseInside = true;
            m_Browser.Close();
        }
        public bool StartButtonClicked
        {
            get { return _StartButtonClicked; }
            set { _StartButtonClicked = value; }
        }
        private bool _StartButtonClicked;
        public bool Submitted
        {
            get { return _Submitted; }
            set { _Submitted = value; }
        }
        private bool _Submitted;
        public string LastUrl
        {
            get { return _LastUrl; }
            set { _LastUrl = value; }
        }
        private string _LastUrl;

        public string WorkflowInitPageUrl
        {
            get { return _WorkflowInitPageUrl; }
            set { _WorkflowInitPageUrl = value; }
        }
        private string _WorkflowInitPageUrl;

        public string MsgHtmlPath
        {
            get { return _MsgHtmlPath; }
            set { _MsgHtmlPath = value; }
        }
        private string _MsgHtmlPath;

        void btnElementStart_Click(object sender, System.Windows.Forms.HtmlElementEventArgs e)
        {
            StartButtonClicked = true;
        }
        bool StartWorkFlowFromWSSWorFlow()
        {
            int iStartIndex = listPath.LastIndexOf("/");
            string sitePath = listPath.Remove(iStartIndex);
            string spguid = null;
            bool bStartWorkFlow = false;
            bool bStartBrowser = false;

            WSSWorkFlow.Workflow _Workflow = new WSSWorkFlow.Workflow();
            _Workflow.Url = workflowPath;
            _Workflow.PreAuthenticate = true;
            if (userName != null && userPassword != null && userDomain != null)
            {
                _Workflow.Credentials = new NetworkCredential(userName, userPassword, userDomain);
            }
            else
            {
                _Workflow.Credentials = CredentialCache.DefaultCredentials;
            }

            //XmlNode assocnode = _Workflow.GetTemplatesForItem(itemPath);
            //XmlNamespaceManager nsmgr = new XmlNamespaceManager(assocnode.OwnerDocument.NameTable);
            //nsmgr.AddNamespace("wf", "http://schemas.microsoft.com/sharepoint/soap/workflow/");
            //XmlNode idNode = assocnode.SelectSingleNode("//wf:WorkflowTemplateIdSet", nsmgr);
            //Guid templateID = new Guid(idNode.Attributes.GetNamedItem("TemplateId").Value);


            XmlNode xmlNodeData = _Workflow.GetWorkflowDataForItem(itemPath);
            XmlNodeList xmlTemplates = GetWorkFlowKeyNodes(xmlNodeData, "/spwf:WorkflowData/spwf:TemplateData/spwf:WorkflowTemplates/spwf:WorkflowTemplate");
            XmlNodeList OldActiveWorkFlows = GetWorkFlowKeyNodes(xmlNodeData, "/spwf:WorkflowData/spwf:ActiveWorkflowsData/spwf:Workflows");


            foreach (XmlNode template in xmlTemplates)
            {
                string associationName = template.Attributes["Name"].Value;

                foreach (XmlNode child in template.ChildNodes)
                {
                    if (child.Name != null && child.Name.Equals("workflowTemplateIdSet", StringComparison.OrdinalIgnoreCase))
                    {
                        XmlAttribute obspguid = child.Attributes["TemplateId"];
                        if (obspguid != null)
                            spguid = obspguid.Value;
                        break;
                    }
                }
                Guid templateID = new Guid(spguid);
                if (spguid != null && associationName != null && OldActiveWorkFlows != null && associationName.Equals(spAssociation, StringComparison.OrdinalIgnoreCase))
                {
                    //we have found the workflow template
                    //First, we should see if the old activeworkflow has this kind of workflow
                    bool bHasWorkflowRunning = false;
                    foreach (XmlNode ActiveWorkFlow in OldActiveWorkFlows)
                    {
                        foreach (XmlNode _ChildNodes in ActiveWorkFlow.ChildNodes)
                        {
                            XmlAttribute ob_guid = _ChildNodes.Attributes["TemplateId"];
                            if (ob_guid != null)
                            {
                                string _guid = ob_guid.Value;
                                if (_guid != null && _guid.Equals(spguid))
                                {
                                    bHasWorkflowRunning = true;
                                    break;
                                }
                            }
                        }
                    }
                    if (bHasWorkflowRunning)
                        break;
                    //Second, we should start this workflow
                    foreach (XmlNode node in template.ChildNodes)
                    {
                        if (node.Name != null && node.Name.Equals("AssociationData", StringComparison.OrdinalIgnoreCase))
                        {
                            string associationData = node.ChildNodes[0].ChildNodes[0].Value;
                            if (bInitFromBrowser)
                            {
                                XmlAttribute obinitUrl = template.Attributes["InstantiationUrl"];
                                if (obinitUrl != null)
                                {
                                    if (_ProgressThread != null)
                                    {
                                        _ProgressThread.Abort();
                                        _ProgressThread = null;
                                    }                                    
                                    string initUrl = obinitUrl.Value;
                                    //System.Diagnostics.Process.Start("IExplore.exe", initUrl);
                                    System.Threading.ThreadStart threadProc = delegate { new WfSPHandler().BrowserWinThread(initUrl,logID,itemPath); };
                                    System.Threading.Thread browserThread = new System.Threading.Thread(threadProc);
                                    browserThread.SetApartmentState(System.Threading.ApartmentState.STA);
                                    browserThread.Start();
                                    browserThread.Join();
                                }
                                bStartBrowser = true;
                                break;
                            }
                            if (logID != null)
                            {
                                try
                                {
                                    //add id to the workflow associationData
                                    int startpos = associationData.IndexOf("LogId>");
                                    startpos += 6;
                                    int endpos = associationData.LastIndexOf("LogId>");
                                    string startstr = associationData.Substring(0, startpos);
                                    string endstr1 = associationData.Substring(0, endpos);
                                    string endstr2 = associationData.Substring(endpos);
                                    int addpos = endstr1.LastIndexOf("</");
                                    endstr1 = endstr1.Substring(addpos);
                                    associationData = startstr + logID + endstr1 + endstr2;
                                }
                                catch(Exception e)
                                {
                                    Trace.WriteLine("try to add log id to workflow associationData exception:"+e.Message);
                                }
                            }
                            XmlDocument xmlPara = new XmlDocument();
                            xmlPara.LoadXml(associationData);
                            XmlNode xmlNode = _Workflow.StartWorkflow(itemPath, templateID, xmlPara);
                            bStartWorkFlow = true;
                        }
                    }
                }
                if (bStartWorkFlow)
                    break;
                if (bStartBrowser)
                    break;
            }

            if (bStartWorkFlow || bStartBrowser)
            {
                try
                {
                    bool bFind = false;
                    XmlNode ActiveWorkFlowsNodeData = _Workflow.GetWorkflowDataForItem(itemPath);
                    XmlNodeList ActiveWorkFlows = GetWorkFlowKeyNodes(ActiveWorkFlowsNodeData, "/spwf:WorkflowData/spwf:ActiveWorkflowsData/spwf:Workflows");
                    foreach (XmlNode ActiveWorkFlow in ActiveWorkFlows)
                    {
                        foreach (XmlNode _ChildNodes in ActiveWorkFlow.ChildNodes)
                        {
                            string _guid = _ChildNodes.Attributes["TemplateId"].Value;
                            string _status = _ChildNodes.Attributes["Status1"].Value;
                            if (_guid != null && _guid.Equals(spguid))
                            {
                                bFind = true;
                                if (_status == null)
                                {
                                    bIfFailed = true;
                                    break;
                                }
                                else
                                {
                                    int istatus = Convert.ToInt32(_status);
                                    int statusbinding = istatus & 0x40;
                                    if (statusbinding != 0)
                                    {
                                        //0x40 is the status wfs_faulting
                                        bIfFailed = true;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                    if (bFind)
                    {
                        if (bIfFailed)
                        {
                            DoLogObligation("Start with error", logID);
                        }
                        else
                        {
                            DoLogObligation("Start", logID);
                        }
                    }
                }
                catch (Exception e)
                {

                }
            }
            return true;

        }
    }


    class Program
    {
        static void Main(string[] args)
        {
            string[] test = { "-sp-location", "http://lab01-w08-sps13/lists/tasks",
                                "-target", "C:\\jjin\\data\\test11.doc",
                            "-sp-association","Application",
                            "-logid","22",
                            "-interval-time","5",
                            "-retry-times","1",
                            "-engine","sharepoint",
                        "-sp-initfrombrowser",
                        "-domain",
                        "lab01",
                        "-username",
                        "john.tyler",
                        "-password",
                        "john.tyler",
                //"-try-times","2",
                //"-sp-wfinput-abc","abc"
                                            };
            WfHanlder _WfHanlder = WfHandlerFactory.CreateWfHanlderAlternative(args, 1);
            if (_WfHanlder != null)
                _WfHanlder.Process(args, 1);

        }
    }
}
