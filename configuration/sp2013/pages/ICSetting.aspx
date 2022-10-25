<%@ Assembly Name="Microsoft.SharePoint.ApplicationPages, Version=15.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c"%>
<%@ Page language="C#" DynamicMasterPageFile="~masterurl/default.master" Inherits="NextLabs.SPEnforcer.ContentAnalysisPage,NextLabs.SPEnforcer,Version=3.0.0.0,Culture=neutral,PublicKeyToken=5ef8e9c15bdfa43e" %>

<%@ Import Namespace="Microsoft.SharePoint" %> 
<%@ Import Namespace="Microsoft.SharePoint.ApplicationPages" %> 
<%@ Import Namespace="Microsoft.SharePoint.Utilities" %>
<%@ Import Namespace="Microsoft.SharePoint.Administration" %>

<%@ Register TagPrefix="wssuc" TagName="LinksTable" src="~/_controltemplates/15/LinksTable.ascx" %> 
<%@ Register TagPrefix="wssuc" TagName="InputFormSection" src="~/_controltemplates/15/InputFormSection.ascx" %> 
<%@ Register TagPrefix="wssuc" TagName="InputFormControl" src="~/_controltemplates/15/InputFormControl.ascx" %> 
<%@ Register TagPrefix="wssuc" TagName="LinkSection" src="~/_controltemplates/15/LinkSection.ascx" %> 
<%@ Register TagPrefix="wssuc" TagName="ButtonSection" src="~/_controltemplates/15/ButtonSection.ascx" %> 
<%@ Register TagPrefix="wssuc" TagName="ActionBar" src="~/_controltemplates/15/ActionBar.ascx" %> 
<%@ Register TagPrefix="wssuc" TagName="ToolBar" src="~/_controltemplates/15/ToolBar.ascx" %> 
<%@ Register TagPrefix="wssuc" TagName="ToolBarButton" src="~/_controltemplates/15/ToolBarButton.ascx" %> 
<%@ Register TagPrefix="wssuc" TagName="Welcome" src="~/_controltemplates/15/Welcome.ascx" %>
<%@ Register Tagprefix="wssawc" Namespace="Microsoft.SharePoint.WebControls" Assembly="Microsoft.SharePoint, Version=15.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %> 
<%@ Register Tagprefix="SharePoint" Namespace="Microsoft.SharePoint.WebControls" Assembly="Microsoft.SharePoint, Version=15.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %>
<%@ Register Tagprefix="SPSWC" Namespace="Microsoft.SharePoint.Portal.WebControls" Assembly="Microsoft.SharePoint.Portal, Version=15.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %>

<asp:Content ID="Content1" contentplaceholderid="PlaceHolderAdditionalPageHead" runat="server">
	<SharePoint:ScriptLink ID="ScriptLink1" language="javascript" name="commonvalidation.js" runat="server" />
	<SharePoint:ScriptLink ID="ScriptLink2" language="javascript" name="datepicker.js" Localizable="false" runat="server" />
    <SharePoint:ScriptLink ID="ScriptLink3" runat="server" Name="sp.js" OnDemand="false" Localizable="false" LoadAfterUI="true"/>
<script language="javascript" type="text/javascript">
    function postBackByObject() {
        var o = window.event.srcElement;
        if (o.tagName == "INPUT" && o.type == "checkbox") {
            __doPostBack("", "");
        }
    }

    function CbListTrimmingg_Clicked(CbCtrl) {
        var CbListPrefilterTrimming = document.getElementById("<%=EnableListPrefilterTrimming.ClientID%>");
        if (CbListPrefilterTrimming != null) {
            if (CbListPrefilterTrimming.parentElement.disabled) {
                CbListPrefilterTrimming.parentElement.disabled = false;
                if (CbListPrefilterTrimming.parentElement.parentElement.nextSibling.firstChild.tagName == 'SPAN') {
                    CbListPrefilterTrimming.parentElement.parentElement.nextSibling.firstChild.disabled = false;
                }
            }
            CbListPrefilterTrimming.disabled = !CbCtrl.checked;
        }
    }

    
</script>
</asp:Content>

<asp:Content contentplaceholderid="PlaceHolderPageTitle" runat="server">
<SharePoint:EncodedLiteral runat="server" Text="Entitlement Manager Information Control Settings" EncodeMethod="HtmlEncode" />
</asp:Content>

<asp:Content contentplaceholderid="PlaceHolderPageTitleInTitleArea" runat="server">
<a id=onetidListHlink HREF=<%SPHttpUtility.AddQuote(SPHttpUtility.UrlPathEncode(SPContext.Current.List.DefaultViewUrl,true),Response.Output);%>><%SPHttpUtility.HtmlEncode(SPContext.Current.List.Title,Response.Output);%></a>&#32;<SharePoint:ClusteredDirectionalSeparatorArrow ID="ClusteredDirectionalSeparatorArrow1" runat="server" /> <a HREF=<%SPHttpUtility.AddQuote(SPHttpUtility.UrlPathEncode("listedit.aspx?List=" + SPContext.Current.List.ID.ToString(),true),Response.Output);%>> <SharePoint:FormattedStringWithListType ID="FormattedStringWithListType1" runat="server" String="<%$Resources:wss,listsettings_titleintitlearea%>" LowerCase="false" /></a>&#32;<SharePoint:ClusteredDirectionalSeparatorArrow ID="ClusteredDirectionalSeparatorArrow2" runat="server" />
<SharePoint:EncodedLiteral runat="server" Text="Entitlement Manager Information Control Settings" EncodeMethod="HtmlEncode" />
</asp:Content>

<asp:Content contentplaceholderid="PlaceHolderMain" runat="server">
	<SharePoint:FormDigest runat="server" id="FormDigest" />
	
	<SPSWC:TextBoxLoc runat="server" id="AllXmlSchedules" style="display:none"/>
	<SPSWC:TextBoxLoc runat="server" id="SelectedIndex" style="display:none"/>
	
    <!--List Trimming Setting-->
    <TABLE border="0" cellspacing="0" cellpadding="0" class="ms-propertysheet" width="100%">
		<wssuc:InputFormSection Title="List Trimming" runat="server">
			<Template_Description>
				<SharePoint:EncodedLiteral runat="server" id="EncodedLiteral1" text="<%$Resources:SPSecurityTrimming, ListTrimmingListLevelDescription%>" EncodeMethod='HtmlEncode'/>
			</Template_Description>
			
            <Template_InputFormControls>
                <wssawc:InputFormCheckBox ID="EnableListTrimming" LabelText="Enable List Trimming" onclick="javascript:CbListTrimmingg_Clicked(this)" runat="server"/>
            </Template_InputFormControls>
        </wssuc:InputFormSection>


        <wssuc:InputFormSection Title="Security Filters for Lists" runat="server">
			<Template_Description>
				<SharePoint:EncodedLiteral runat="server" id="EncodedLiteral11" text="<%$Resources:SPSecurityTrimming, PrefilterTrimmingListLevelDescription%>" EncodeMethod='HtmlEncode'/>
			</Template_Description>
            <Template_InputFormControls>
                <wssawc:InputFormCheckBox ID="EnableListPrefilterTrimming" LabelText="Enable Security Filters" runat="server"/>
            </Template_InputFormControls>
        </wssuc:InputFormSection>
	</TABLE>
	
	<!--Content Analysis Setting-->
    <TABLE border="0" cellspacing="0" cellpadding="0" class="ms-propertysheet" width="100%">
        <wssuc:InputFormSection id="CAInfo" Title="Information Control Processing" runat="server">
            <Template_Description>
  			    <SharePoint:EncodedLiteral runat="server" id="EncodedLiteral5" text="Automated Classification and Integrated Rights Management Information Controls Policies can applied to documents" EncodeMethod='HtmlEncode'/>
				<SharePoint:EncodedLiteral runat="server" id="EncodedLiteral6" text="or attachments in document library. To enable information control policies specify a processing schedule below." EncodeMethod='HtmlEncode'/>
            </Template_Description>
        </wssuc:InputFormSection>
    </TABLE>
    <asp:HyperLink runat="server" ID="LogLink" Target="_parent" NavigateUrl="">View Log</asp:HyperLink>
	<TABLE border="0" cellspacing="0" cellpadding="0" class="ms-propertysheet" width="100%"> 
	   <wssuc:InputFormSection id="CASchedule" Title=" " runat="server">
            <Template_Description>
				<SharePoint:EncodedLiteral runat="server" id="EncodedLiteral3" text="Select the schedules for current library or list." EncodeMethod='HtmlEncode'/>
				<%
				bool isFarmAdmin = false;
                bool isVisiable = false;
                SPSecurity.RunWithElevatedPrivileges(delegate()
                {
                    isFarmAdmin = SPFarm.Local.CurrentUserIsAdministrator();

                });
                isVisiable = (isFarmAdmin == false && !SPFarm.Local.CurrentUserIsAdministrator());
			    if(isVisiable)
			    {
				%> 
				<BR /><BR />
			    <FONT COLOR=red>
			    <SharePoint:EncodedLiteral runat="server" id="EncodedLiteralWarn" text="Only Farm Administrator can start a scheduled job." EncodeMethod='HtmlEncode'/>
			    </FONT>
                <%} %>	
			</Template_Description>
			
            <Template_InputFormControls>
                <SPSWC:InputFormDropDownList runat="server" 
                        id="scheduleFullDropDown"
                        indentedControl="false"
                        width="100%"
                        LabelText ="Schedule List"/>
                    <SPSWC:InputFormTable runat="server">
                    <SPSWC:InputFormTableRow runat="server">
                    <%
                    bool isFarmAdmin = false;
                    bool isVisiable = false;
                    SPSecurity.RunWithElevatedPrivileges(delegate()
                    {
                        isFarmAdmin = SPFarm.Local.CurrentUserIsAdministrator();

                    });
                    isVisiable = (isFarmAdmin == false && !SPFarm.Local.CurrentUserIsAdministrator());
                    if (!isVisiable) 
                    {
                    %>
                    <SPSWC:InputFormTableData runat="server" width="150">
                    <SPSWC:InputFormHyperLink runat="server" 
                        text="Create Schedule"
                        id="manageScheduleLink"
                        navigateUrl="#EditSchedule" />
                       
                    </SPSWC:InputFormTableData>
                    <SPSWC:InputFormTableData runat="server">
                    <SPSWC:InputFormHyperLink runat="server" 
                        text="Delete Schedule"
                        id="deleteScheduleLink"
                        navigateUrl="#DeleteSchedule" />
                    </SPSWC:InputFormTableData>
                    <%}%>
                    </SPSWC:InputFormTableRow>
                    </SPSWC:InputFormTable>
            </Template_InputFormControls>
        </wssuc:InputFormSection>
	
		<wssuc:InputFormSection id="CAState" Title="Manual Information Control Processing" runat="server">
			<Template_Description>
				<SharePoint:EncodedLiteral runat="server" id="DescriptionLiteral" EncodeMethod='HtmlEncode'/>
				<br />
				<SharePoint:EncodedLiteral runat="server" id="DescriptionLiteral2" EncodeMethod='HtmlEncode'/>
				<br />
				<br />
				<SharePoint:EncodedLiteral runat="server" id="HelpLiteral" text="To perform immediate processing of information control policies, check the Start Processing checkbox and click ok." EncodeMethod='HtmlEncode'/>
			</Template_Description>
			
            <Template_InputFormControls>
                <wssawc:InputFormCheckBox ID="CAStatusCheckBox" LabelText="Start Processing" runat="server">
                    <b><SharePoint:EncodedLiteral runat="server" id="CAStatus" text="Status: Not Running" EncodeMethod='HtmlEncode'/></b>
                </wssawc:InputFormCheckBox>
            </Template_InputFormControls>
            
        </wssuc:InputFormSection>

		<wssuc:InputFormSection id="ClearLastSCanTime" Title="Clear Last Scan Time" runat="server">
			<Template_Description>
				<SharePoint:EncodedLiteral runat="server" id="EncodedLiteral9" text="By default, the scheduled task will only process doucument(s) which has been changed after last" EncodeMethod='HtmlEncode'/>
				<SharePoint:EncodedLiteral runat="server" id="EncodedLiteral7" text="scan time. Check this checkbox will lead to all items to be scanned isntead only those item(s) be" EncodeMethod='HtmlEncode'/>
				<SharePoint:EncodedLiteral runat="server" id="EncodedLiteral8" text="changed from last scan." EncodeMethod='HtmlEncode'/>
			</Template_Description>
			
            <Template_InputFormControls>
                <wssawc:InputFormCheckBox ID="ClearLastScanTimeCheckBox" LabelText="Clear last scan time"   runat="server">
                </wssawc:InputFormCheckBox>
            </Template_InputFormControls>
            
        </wssuc:InputFormSection>
        
		<wssuc:InputFormSection id="ProcessUploadCheck" Title="Enable Batch Mode" runat="server">
			<Template_Description>
				<SharePoint:EncodedLiteral runat="server" id="EncodedLiteral10" text="Process upload obligations in Batch Mode" EncodeMethod='HtmlEncode'/>
			</Template_Description>
			
            <Template_InputFormControls>
                <wssawc:InputFormCheckBox ID="ProcessUploadCheckBox" LabelText="Enable Batch Mode"   runat="server">
                </wssawc:InputFormCheckBox>
            </Template_InputFormControls>                                   
        </wssuc:InputFormSection>          
        
		<wssuc:ButtonSection runat="server" ShowStandardCancelButton="false">
			<Template_Buttons>
				<asp:PlaceHolder runat="server">				
						<asp:Button runat="server" class="ms-ButtonHeightWidth" OnClick="BtnOK_Click" Text="<%$Resources:wss,multipages_okbutton_text%>" id="BtnOK" accesskey="<%$Resources:wss,okbutton_accesskey%>"/>
				</asp:PlaceHolder>	
				<asp:PlaceHolder runat="server">					
						<asp:Button runat="server" class="ms-ButtonHeightWidth" OnClick="BtnCancel_Click" Text="<%$Resources:wss,multipages_cancelbutton_text%>" id="BtnCancel" CausesValidation="false"/>
				</asp:PlaceHolder>				
			</Template_Buttons>
		</wssuc:ButtonSection>
	</TABLE>
</asp:Content>
