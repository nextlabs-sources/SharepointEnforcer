﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

// 
// This source code was auto-generated by Microsoft.VSDesigner, Version 4.0.30319.42000.
// 
#pragma warning disable 1591

namespace WorkFlowAdapter.WSSCEService {
    using System;
    using System.Web.Services;
    using System.Diagnostics;
    using System.Web.Services.Protocols;
    using System.Xml.Serialization;
    using System.ComponentModel;
    
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Web.Services", "4.8.3752.0")]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Web.Services.WebServiceBindingAttribute(Name="DocUploadSoap", Namespace="http://www.nextlabs.com/")]
    public partial class DocUpload : System.Web.Services.Protocols.SoapHttpClientProtocol {
        
        private System.Threading.SendOrPostCallback IsDocLibOperationCompleted;
        
        private System.Threading.SendOrPostCallback UploadDocumentOperationCompleted;
        
        private System.Threading.SendOrPostCallback SetColumnsOperationCompleted;
        
        private System.Threading.SendOrPostCallback IfInAssociationTemplatesOperationCompleted;
        
        private System.Threading.SendOrPostCallback StartWorkFlowOperationCompleted;
        
        private bool useDefaultCredentialsSetExplicitly;
        
        /// <remarks/>
        public DocUpload() {
            this.Url = global::WorkFlowAdapter.Properties.Settings.Default.WorkFlowAdapter_WSSCEService_DocUpload;
            if ((this.IsLocalFileSystemWebService(this.Url) == true)) {
                this.UseDefaultCredentials = true;
                this.useDefaultCredentialsSetExplicitly = false;
            }
            else {
                this.useDefaultCredentialsSetExplicitly = true;
            }
        }
        
        public new string Url {
            get {
                return base.Url;
            }
            set {
                if ((((this.IsLocalFileSystemWebService(base.Url) == true) 
                            && (this.useDefaultCredentialsSetExplicitly == false)) 
                            && (this.IsLocalFileSystemWebService(value) == false))) {
                    base.UseDefaultCredentials = false;
                }
                base.Url = value;
            }
        }
        
        public new bool UseDefaultCredentials {
            get {
                return base.UseDefaultCredentials;
            }
            set {
                base.UseDefaultCredentials = value;
                this.useDefaultCredentialsSetExplicitly = true;
            }
        }
        
        /// <remarks/>
        public event IsDocLibCompletedEventHandler IsDocLibCompleted;
        
        /// <remarks/>
        public event UploadDocumentCompletedEventHandler UploadDocumentCompleted;
        
        /// <remarks/>
        public event SetColumnsCompletedEventHandler SetColumnsCompleted;
        
        /// <remarks/>
        public event IfInAssociationTemplatesCompletedEventHandler IfInAssociationTemplatesCompleted;
        
        /// <remarks/>
        public event StartWorkFlowCompletedEventHandler StartWorkFlowCompleted;
        
        /// <remarks/>
        [System.Web.Services.Protocols.SoapDocumentMethodAttribute("http://www.nextlabs.com/IsDocLib", RequestNamespace="http://www.nextlabs.com/", ResponseNamespace="http://www.nextlabs.com/", Use=System.Web.Services.Description.SoapBindingUse.Literal, ParameterStyle=System.Web.Services.Protocols.SoapParameterStyle.Wrapped)]
        public bool IsDocLib(string pathFolder) {
            object[] results = this.Invoke("IsDocLib", new object[] {
                        pathFolder});
            return ((bool)(results[0]));
        }
        
        /// <remarks/>
        public void IsDocLibAsync(string pathFolder) {
            this.IsDocLibAsync(pathFolder, null);
        }
        
        /// <remarks/>
        public void IsDocLibAsync(string pathFolder, object userState) {
            if ((this.IsDocLibOperationCompleted == null)) {
                this.IsDocLibOperationCompleted = new System.Threading.SendOrPostCallback(this.OnIsDocLibOperationCompleted);
            }
            this.InvokeAsync("IsDocLib", new object[] {
                        pathFolder}, this.IsDocLibOperationCompleted, userState);
        }
        
        private void OnIsDocLibOperationCompleted(object arg) {
            if ((this.IsDocLibCompleted != null)) {
                System.Web.Services.Protocols.InvokeCompletedEventArgs invokeArgs = ((System.Web.Services.Protocols.InvokeCompletedEventArgs)(arg));
                this.IsDocLibCompleted(this, new IsDocLibCompletedEventArgs(invokeArgs.Results, invokeArgs.Error, invokeArgs.Cancelled, invokeArgs.UserState));
            }
        }
        
        /// <remarks/>
        [System.Web.Services.Protocols.SoapDocumentMethodAttribute("http://www.nextlabs.com/UploadDocument", RequestNamespace="http://www.nextlabs.com/", ResponseNamespace="http://www.nextlabs.com/", Use=System.Web.Services.Description.SoapBindingUse.Literal, ParameterStyle=System.Web.Services.Protocols.SoapParameterStyle.Wrapped)]
        public string UploadDocument(string fileName, [System.Xml.Serialization.XmlElementAttribute(DataType="base64Binary")] byte[] fileContents, string pathFolder, ref int ItemId, ref string itemPath, ref string webUrl, bool bIfUnique) {
            object[] results = this.Invoke("UploadDocument", new object[] {
                        fileName,
                        fileContents,
                        pathFolder,
                        ItemId,
                        itemPath,
                        webUrl,
                        bIfUnique});
            ItemId = ((int)(results[1]));
            itemPath = ((string)(results[2]));
            webUrl = ((string)(results[3]));
            return ((string)(results[0]));
        }
        
        /// <remarks/>
        public void UploadDocumentAsync(string fileName, byte[] fileContents, string pathFolder, int ItemId, string itemPath, string webUrl, bool bIfUnique) {
            this.UploadDocumentAsync(fileName, fileContents, pathFolder, ItemId, itemPath, webUrl, bIfUnique, null);
        }
        
        /// <remarks/>
        public void UploadDocumentAsync(string fileName, byte[] fileContents, string pathFolder, int ItemId, string itemPath, string webUrl, bool bIfUnique, object userState) {
            if ((this.UploadDocumentOperationCompleted == null)) {
                this.UploadDocumentOperationCompleted = new System.Threading.SendOrPostCallback(this.OnUploadDocumentOperationCompleted);
            }
            this.InvokeAsync("UploadDocument", new object[] {
                        fileName,
                        fileContents,
                        pathFolder,
                        ItemId,
                        itemPath,
                        webUrl,
                        bIfUnique}, this.UploadDocumentOperationCompleted, userState);
        }
        
        private void OnUploadDocumentOperationCompleted(object arg) {
            if ((this.UploadDocumentCompleted != null)) {
                System.Web.Services.Protocols.InvokeCompletedEventArgs invokeArgs = ((System.Web.Services.Protocols.InvokeCompletedEventArgs)(arg));
                this.UploadDocumentCompleted(this, new UploadDocumentCompletedEventArgs(invokeArgs.Results, invokeArgs.Error, invokeArgs.Cancelled, invokeArgs.UserState));
            }
        }
        
        /// <remarks/>
        [System.Web.Services.Protocols.SoapDocumentMethodAttribute("http://www.nextlabs.com/SetColumns", RequestNamespace="http://www.nextlabs.com/", ResponseNamespace="http://www.nextlabs.com/", Use=System.Web.Services.Description.SoapBindingUse.Literal, ParameterStyle=System.Web.Services.Protocols.SoapParameterStyle.Wrapped)]
        public string SetColumns(int id, System.Guid guid, string pathFolder, string fieldName, string fieldValue) {
            object[] results = this.Invoke("SetColumns", new object[] {
                        id,
                        guid,
                        pathFolder,
                        fieldName,
                        fieldValue});
            return ((string)(results[0]));
        }
        
        /// <remarks/>
        public void SetColumnsAsync(int id, System.Guid guid, string pathFolder, string fieldName, string fieldValue) {
            this.SetColumnsAsync(id, guid, pathFolder, fieldName, fieldValue, null);
        }
        
        /// <remarks/>
        public void SetColumnsAsync(int id, System.Guid guid, string pathFolder, string fieldName, string fieldValue, object userState) {
            if ((this.SetColumnsOperationCompleted == null)) {
                this.SetColumnsOperationCompleted = new System.Threading.SendOrPostCallback(this.OnSetColumnsOperationCompleted);
            }
            this.InvokeAsync("SetColumns", new object[] {
                        id,
                        guid,
                        pathFolder,
                        fieldName,
                        fieldValue}, this.SetColumnsOperationCompleted, userState);
        }
        
        private void OnSetColumnsOperationCompleted(object arg) {
            if ((this.SetColumnsCompleted != null)) {
                System.Web.Services.Protocols.InvokeCompletedEventArgs invokeArgs = ((System.Web.Services.Protocols.InvokeCompletedEventArgs)(arg));
                this.SetColumnsCompleted(this, new SetColumnsCompletedEventArgs(invokeArgs.Results, invokeArgs.Error, invokeArgs.Cancelled, invokeArgs.UserState));
            }
        }
        
        /// <remarks/>
        [System.Web.Services.Protocols.SoapDocumentMethodAttribute("http://www.nextlabs.com/IfInAssociationTemplates", RequestNamespace="http://www.nextlabs.com/", ResponseNamespace="http://www.nextlabs.com/", Use=System.Web.Services.Description.SoapBindingUse.Literal, ParameterStyle=System.Web.Services.Protocols.SoapParameterStyle.Wrapped)]
        public bool IfInAssociationTemplates(string pathFolder, string associationName, string[] associationTemplates) {
            object[] results = this.Invoke("IfInAssociationTemplates", new object[] {
                        pathFolder,
                        associationName,
                        associationTemplates});
            return ((bool)(results[0]));
        }
        
        /// <remarks/>
        public void IfInAssociationTemplatesAsync(string pathFolder, string associationName, string[] associationTemplates) {
            this.IfInAssociationTemplatesAsync(pathFolder, associationName, associationTemplates, null);
        }
        
        /// <remarks/>
        public void IfInAssociationTemplatesAsync(string pathFolder, string associationName, string[] associationTemplates, object userState) {
            if ((this.IfInAssociationTemplatesOperationCompleted == null)) {
                this.IfInAssociationTemplatesOperationCompleted = new System.Threading.SendOrPostCallback(this.OnIfInAssociationTemplatesOperationCompleted);
            }
            this.InvokeAsync("IfInAssociationTemplates", new object[] {
                        pathFolder,
                        associationName,
                        associationTemplates}, this.IfInAssociationTemplatesOperationCompleted, userState);
        }
        
        private void OnIfInAssociationTemplatesOperationCompleted(object arg) {
            if ((this.IfInAssociationTemplatesCompleted != null)) {
                System.Web.Services.Protocols.InvokeCompletedEventArgs invokeArgs = ((System.Web.Services.Protocols.InvokeCompletedEventArgs)(arg));
                this.IfInAssociationTemplatesCompleted(this, new IfInAssociationTemplatesCompletedEventArgs(invokeArgs.Results, invokeArgs.Error, invokeArgs.Cancelled, invokeArgs.UserState));
            }
        }
        
        /// <remarks/>
        [System.Web.Services.Protocols.SoapDocumentMethodAttribute("http://www.nextlabs.com/StartWorkFlow", RequestNamespace="http://www.nextlabs.com/", ResponseNamespace="http://www.nextlabs.com/", Use=System.Web.Services.Description.SoapBindingUse.Literal, ParameterStyle=System.Web.Services.Protocols.SoapParameterStyle.Wrapped)]
        public string StartWorkFlow(int itemID, string itemNamestring, string pathFolder, string associationName, string associationDes, bool bIsAutoStart, ref bool bHasWorkFlowRunning, ref int iWorkFlowStatus) {
            object[] results = this.Invoke("StartWorkFlow", new object[] {
                        itemID,
                        itemNamestring,
                        pathFolder,
                        associationName,
                        associationDes,
                        bIsAutoStart,
                        bHasWorkFlowRunning,
                        iWorkFlowStatus});
            bHasWorkFlowRunning = ((bool)(results[1]));
            iWorkFlowStatus = ((int)(results[2]));
            return ((string)(results[0]));
        }
        
        /// <remarks/>
        public void StartWorkFlowAsync(int itemID, string itemNamestring, string pathFolder, string associationName, string associationDes, bool bIsAutoStart, bool bHasWorkFlowRunning, int iWorkFlowStatus) {
            this.StartWorkFlowAsync(itemID, itemNamestring, pathFolder, associationName, associationDes, bIsAutoStart, bHasWorkFlowRunning, iWorkFlowStatus, null);
        }
        
        /// <remarks/>
        public void StartWorkFlowAsync(int itemID, string itemNamestring, string pathFolder, string associationName, string associationDes, bool bIsAutoStart, bool bHasWorkFlowRunning, int iWorkFlowStatus, object userState) {
            if ((this.StartWorkFlowOperationCompleted == null)) {
                this.StartWorkFlowOperationCompleted = new System.Threading.SendOrPostCallback(this.OnStartWorkFlowOperationCompleted);
            }
            this.InvokeAsync("StartWorkFlow", new object[] {
                        itemID,
                        itemNamestring,
                        pathFolder,
                        associationName,
                        associationDes,
                        bIsAutoStart,
                        bHasWorkFlowRunning,
                        iWorkFlowStatus}, this.StartWorkFlowOperationCompleted, userState);
        }
        
        private void OnStartWorkFlowOperationCompleted(object arg) {
            if ((this.StartWorkFlowCompleted != null)) {
                System.Web.Services.Protocols.InvokeCompletedEventArgs invokeArgs = ((System.Web.Services.Protocols.InvokeCompletedEventArgs)(arg));
                this.StartWorkFlowCompleted(this, new StartWorkFlowCompletedEventArgs(invokeArgs.Results, invokeArgs.Error, invokeArgs.Cancelled, invokeArgs.UserState));
            }
        }
        
        /// <remarks/>
        public new void CancelAsync(object userState) {
            base.CancelAsync(userState);
        }
        
        private bool IsLocalFileSystemWebService(string url) {
            if (((url == null) 
                        || (url == string.Empty))) {
                return false;
            }
            System.Uri wsUri = new System.Uri(url);
            if (((wsUri.Port >= 1024) 
                        && (string.Compare(wsUri.Host, "localHost", System.StringComparison.OrdinalIgnoreCase) == 0))) {
                return true;
            }
            return false;
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Web.Services", "4.8.3752.0")]
    public delegate void IsDocLibCompletedEventHandler(object sender, IsDocLibCompletedEventArgs e);
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Web.Services", "4.8.3752.0")]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class IsDocLibCompletedEventArgs : System.ComponentModel.AsyncCompletedEventArgs {
        
        private object[] results;
        
        internal IsDocLibCompletedEventArgs(object[] results, System.Exception exception, bool cancelled, object userState) : 
                base(exception, cancelled, userState) {
            this.results = results;
        }
        
        /// <remarks/>
        public bool Result {
            get {
                this.RaiseExceptionIfNecessary();
                return ((bool)(this.results[0]));
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Web.Services", "4.8.3752.0")]
    public delegate void UploadDocumentCompletedEventHandler(object sender, UploadDocumentCompletedEventArgs e);
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Web.Services", "4.8.3752.0")]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class UploadDocumentCompletedEventArgs : System.ComponentModel.AsyncCompletedEventArgs {
        
        private object[] results;
        
        internal UploadDocumentCompletedEventArgs(object[] results, System.Exception exception, bool cancelled, object userState) : 
                base(exception, cancelled, userState) {
            this.results = results;
        }
        
        /// <remarks/>
        public string Result {
            get {
                this.RaiseExceptionIfNecessary();
                return ((string)(this.results[0]));
            }
        }
        
        /// <remarks/>
        public int ItemId {
            get {
                this.RaiseExceptionIfNecessary();
                return ((int)(this.results[1]));
            }
        }
        
        /// <remarks/>
        public string itemPath {
            get {
                this.RaiseExceptionIfNecessary();
                return ((string)(this.results[2]));
            }
        }
        
        /// <remarks/>
        public string webUrl {
            get {
                this.RaiseExceptionIfNecessary();
                return ((string)(this.results[3]));
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Web.Services", "4.8.3752.0")]
    public delegate void SetColumnsCompletedEventHandler(object sender, SetColumnsCompletedEventArgs e);
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Web.Services", "4.8.3752.0")]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class SetColumnsCompletedEventArgs : System.ComponentModel.AsyncCompletedEventArgs {
        
        private object[] results;
        
        internal SetColumnsCompletedEventArgs(object[] results, System.Exception exception, bool cancelled, object userState) : 
                base(exception, cancelled, userState) {
            this.results = results;
        }
        
        /// <remarks/>
        public string Result {
            get {
                this.RaiseExceptionIfNecessary();
                return ((string)(this.results[0]));
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Web.Services", "4.8.3752.0")]
    public delegate void IfInAssociationTemplatesCompletedEventHandler(object sender, IfInAssociationTemplatesCompletedEventArgs e);
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Web.Services", "4.8.3752.0")]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class IfInAssociationTemplatesCompletedEventArgs : System.ComponentModel.AsyncCompletedEventArgs {
        
        private object[] results;
        
        internal IfInAssociationTemplatesCompletedEventArgs(object[] results, System.Exception exception, bool cancelled, object userState) : 
                base(exception, cancelled, userState) {
            this.results = results;
        }
        
        /// <remarks/>
        public bool Result {
            get {
                this.RaiseExceptionIfNecessary();
                return ((bool)(this.results[0]));
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Web.Services", "4.8.3752.0")]
    public delegate void StartWorkFlowCompletedEventHandler(object sender, StartWorkFlowCompletedEventArgs e);
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Web.Services", "4.8.3752.0")]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class StartWorkFlowCompletedEventArgs : System.ComponentModel.AsyncCompletedEventArgs {
        
        private object[] results;
        
        internal StartWorkFlowCompletedEventArgs(object[] results, System.Exception exception, bool cancelled, object userState) : 
                base(exception, cancelled, userState) {
            this.results = results;
        }
        
        /// <remarks/>
        public string Result {
            get {
                this.RaiseExceptionIfNecessary();
                return ((string)(this.results[0]));
            }
        }
        
        /// <remarks/>
        public bool bHasWorkFlowRunning {
            get {
                this.RaiseExceptionIfNecessary();
                return ((bool)(this.results[1]));
            }
        }
        
        /// <remarks/>
        public int iWorkFlowStatus {
            get {
                this.RaiseExceptionIfNecessary();
                return ((int)(this.results[2]));
            }
        }
    }
}

#pragma warning restore 1591