<%@ Assembly Name="Microsoft.SharePoint.ApplicationPages, Version=14.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c"%> 
<%@ Page language="C#" DynamicMasterPageFile="~masterurl/default.master"  Inherits="NextLabs.SPEnforcer.NxlClassificationComparisonPage,NextLabs.SPEnforcer,Version=3.0.0.0,Culture=neutral,PublicKeyToken=5ef8e9c15bdfa43e" %>

<%@ Import Namespace="Microsoft.SharePoint" %> 
<%@ Import Namespace="Microsoft.SharePoint.ApplicationPages" %> 
<%@ Import Namespace="Microsoft.SharePoint.Utilities" %>
<%@ Import Namespace="Microsoft.SharePoint.Administration" %>

<%@ Register Tagprefix="OSRVWC" Namespace="Microsoft.Office.Server.WebControls" Assembly="Microsoft.Office.Server, Version=14.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %> 
<%@ Register Tagprefix="SPSWC" Namespace="Microsoft.SharePoint.Portal.WebControls" Assembly="Microsoft.SharePoint.Portal, Version=14.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %> 
<%@ Register Tagprefix="SharePoint" Namespace="Microsoft.SharePoint.WebControls" Assembly="Microsoft.SharePoint, Version=14.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %>
<%@ Register TagPrefix="wssuc" TagName="LinksTable" src="/_controltemplates/LinksTable.ascx" %>
<%@ Register TagPrefix="wssuc" TagName="InputFormSection" src="/_controltemplates/InputFormSection.ascx" %> 
<%@ Register TagPrefix="wssuc" TagName="InputFormControl" src="/_controltemplates/InputFormControl.ascx" %> 
<%@ Register TagPrefix="wssuc" TagName="LinkSection" src="/_controltemplates/LinkSection.ascx" %> 
<%@ Register TagPrefix="wssuc" TagName="ButtonSection" src="/_controltemplates/ButtonSection.ascx" %>
<%@ Register TagPrefix="wssuc" TagName="ActionBar" src="/_controltemplates/ActionBar.ascx" %> 
<%@ Register TagPrefix="wssuc" TagName="ToolBar" src="/_controltemplates/ToolBar.ascx" %> 
<%@ Register TagPrefix="wssuc" TagName="ToolBarButton" src="/_controltemplates/ToolBarButton.ascx" %> 
<%@ Register TagPrefix="wssuc" TagName="Welcome" src="/_controltemplates/Welcome.ascx" %>
<%@ Register Tagprefix="wssawc" Namespace="Microsoft.SharePoint.WebControls" Assembly="Microsoft.SharePoint, Version=14.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %> <%@ Register Tagprefix="SharePoint" Namespace="Microsoft.SharePoint.WebControls" Assembly="Microsoft.SharePoint, Version=14.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %>

<asp:Content ContentPlaceHolderId="PlaceHolderPageTitle" runat="server">
	<SharePoint:EncodedLiteral runat="server" text="Classification Comparison Settings" EncodeMethod='HtmlEncode'/>
</asp:Content>

<asp:Content contentplaceholderid="PlaceHolderPageTitleInTitleArea" runat="server">
	<a href="settings.aspx"><SharePoint:EncodedLiteral ID="EncodedLiteral2" runat="server" text="Classification Comparison Settings" EncodeMethod="HtmlEncode"/></a>&#32;<SharePoint:ClusteredDirectionalSeparatorArrow ID="ClusteredDirectionalSeparatorArrow1" runat="server" />
</asp:Content>

<asp:Content contentplaceholderid="PlaceHolderAdditionalPageHead" runat="server">
    <SharePoint:ScriptLink ID="ScriptLink1" language="javascript" name="commonvalidation.js" runat="server" />
	<SharePoint:ScriptLink ID="ScriptLink2" language="javascript" name="datepicker.js" Localizable="false" runat="server" />
    <SharePoint:ScriptLink ID="ScriptLink3" runat="server" Name="sp.js" OnDemand="false" Localizable="false" LoadAfterUI="true"/>
	<script Language="javascript" >
	    function CheckKeyIsNumber() {
	        var key = window.event.keyCode;
	        return (key >= 48 && key <= 57);
	    }

	</script>
</asp:Content>

<asp:content contentplaceholderid="PlaceHolderMain" runat="server">

    <SPSWC:TextBoxLoc runat="server" id="AllXmlSchedules" style="display:none"/>
	<SPSWC:TextBoxLoc runat="server" id="SelectedIndex" style="display:none"/>
    
    <table class=propertysheet border="0" width="100%" cellspacing="0" cellpadding="0" id="SPSecurityTrimmingSettingsPage">
        <wssuc:InputFormSection runat="server"
		   Description="Run a report to compare the classification of all items in the current site to the current site's classification. the report will be emailed directly to the site owners and users specified in the NextLabs policy."
		   id="idTitleDescSection">		
	    </wssuc:InputFormSection>

        <wssuc:InputFormSection  runat="server" id="WPTSection1" >
			<Template_Description>
				<SharePoint:EncodedLiteral ID="RunImmediate" runat="server" text="Run the report immediately." EncodeMethod='HtmlEncode'/>
			</Template_Description>
			<Template_InputFormControls>
			    <wssawc:InputFormCheckBox ID="StartCheckBox" LabelText="Run immediately" runat="server" />
			</Template_InputFormControls>
		</wssuc:InputFormSection>

		<wssuc:InputFormSection  runat="server" id="WPTSection3">
			<Template_Description>
				<SharePoint:EncodedLiteral runat="server" id="DescriptionLiteral" text="Run the report on a schedule." EncodeMethod='HtmlEncode'/>
			</Template_Description>
			<Template_InputFormControls>
			
                     <SPSWC:InputFormDropDownList runat="server" 
                        id="scheduleFullDropDown"
                        indentedControl="false"
                        width="100%"
                        LabelText ="Specify a schedule"/>
                     <SPSWC:InputFormTable runat="server">
                     <SPSWC:InputFormTableRow runat="server">
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
                  </SPSWC:InputFormTableRow>
                </SPSWC:InputFormTable>
             </Template_InputFormControls>
		</wssuc:InputFormSection>
		
		<!-- OK/Cancel Buttons -->  
		<wssuc:ButtonSection id="BtnSectionBottom" runat="server" ShowStandardCancelButton="false">
			<Template_Buttons>
			    <asp:Button runat="server" class="ms-ButtonHeightWidth" OnClick="BtnOK_Click" Text="<%$Resources:SPSecurityTrimming, BtnOKLabel%>" id="BtnOK" accesskey="<%$Resources:wss,okbutton_accesskey%>"/>
			    <asp:Button runat="server" class="ms-ButtonHeightWidth" OnClick="BtnCancel_Click" Text="<%$Resources:SPSecurityTrimming, BtnCancelLabel%>" id="BtnCancel" accesskey="<%$Resources:wss,okbutton_accesskey%>"/>
			    
       		</Template_Buttons>
        </wssuc:ButtonSection>
        
	</table>
</asp:Content>
