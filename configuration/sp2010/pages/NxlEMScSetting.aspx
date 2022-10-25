<%@ Assembly Name="Microsoft.SharePoint.ApplicationPages, Version=14.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c"%> 
<%@ Page language="C#" DynamicMasterPageFile="~masterurl/default.master"      Inherits="Nextlabs.SPSecurityTrimming.SPSecurityTrimmingPage,Nextlabs.SPSecurityTrimming,Version=3.0.0.0,Culture=neutral,PublicKeyToken=7030e9011c5eb860" %>
<%@ Register Tagprefix="OSRVWC" Namespace="Microsoft.Office.Server.WebControls" Assembly="Microsoft.Office.Server, Version=14.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %> <%@ Register Tagprefix="SPSWC" Namespace="Microsoft.SharePoint.Portal.WebControls" Assembly="Microsoft.SharePoint.Portal, Version=14.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %> <%@ Register Tagprefix="SharePoint" Namespace="Microsoft.SharePoint.WebControls" Assembly="Microsoft.SharePoint, Version=14.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %>
<%@ Register TagPrefix="wssuc" TagName="LinksTable" src="/_controltemplates/LinksTable.ascx" %> <%@ Register TagPrefix="wssuc" TagName="InputFormSection" src="/_controltemplates/InputFormSection.ascx" %> <%@ Register TagPrefix="wssuc" TagName="InputFormControl" src="/_controltemplates/InputFormControl.ascx" %> <%@ Register TagPrefix="wssuc" TagName="LinkSection" src="/_controltemplates/LinkSection.ascx" %> <%@ Register TagPrefix="wssuc" TagName="ButtonSection" src="/_controltemplates/ButtonSection.ascx" %> <%@ Register TagPrefix="wssuc" TagName="ActionBar" src="/_controltemplates/ActionBar.ascx" %> <%@ Register TagPrefix="wssuc" TagName="ToolBar" src="/_controltemplates/ToolBar.ascx" %> <%@ Register TagPrefix="wssuc" TagName="ToolBarButton" src="/_controltemplates/ToolBarButton.ascx" %> <%@ Register TagPrefix="wssuc" TagName="Welcome" src="/_controltemplates/Welcome.ascx" %>
<%@ Register Tagprefix="wssawc" Namespace="Microsoft.SharePoint.WebControls" Assembly="Microsoft.SharePoint, Version=14.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %> <%@ Register Tagprefix="SharePoint" Namespace="Microsoft.SharePoint.WebControls" Assembly="Microsoft.SharePoint, Version=14.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %>

<asp:Content ContentPlaceHolderId="PlaceHolderPageTitle" runat="server">
	<SharePoint:EncodedLiteral runat="server" text="NextLabs Entitlement Manager settings" EncodeMethod='HtmlEncode'/>
</asp:Content>

<asp:Content contentplaceholderid="PlaceHolderPageTitleInTitleArea" runat="server">
	<a href="settings.aspx"><SharePoint:EncodedLiteral ID="EncodedLiteral2" runat="server" text="NextLabs Entitlement Manager settings" EncodeMethod="HtmlEncode"/></a>&#32;<SharePoint:ClusteredDirectionalSeparatorArrow ID="ClusteredDirectionalSeparatorArrow1" runat="server" />
</asp:Content>

<asp:Content contentplaceholderid="PlaceHolderAdditionalPageHead" runat="server">
	<script Language="javascript">
//	    function CbEM_Clicked(CbCtrl) {
//	        var CbEnableSecurityTrimming = document.getElementById("<%=EnableSecurityTrimming.ClientID%>");
//	        if (CbEnableSecurityTrimming != null) {
//	            CbEnableSecurityTrimming.parentElement.disabled = false;
//	            if (CbEnableSecurityTrimming.parentElement.parentElement.nextSibling.firstChild.tagName == 'SPAN') {
//	                CbEnableSecurityTrimming.parentElement.parentElement.nextSibling.firstChild.disabled = false;
//	            }
//	        }
//	        CbEnableSecurityTrimming.disabled = !CbCtrl.checked;
//	        if (!CbCtrl.checked) {
//	            CbSecurityTrimming_Clicked(CbCtrl);
//	        }
//	        //bear fix bug 24282
//	        else {
//	            if (CbEnableSecurityTrimming.checked) {
//	                var CbListTrimming = document.getElementById("<%=EnableAllListTrimming.ClientID%>");
//	                var CbTabTrimming = document.getElementById("<%=EnableTabTrimming.ClientID%>");
//	                var CbWebpartTrimming = document.getElementById("<%=EnableWebpartTrimming.ClientID%>");
//	                var CbPageTrimming = document.getElementById("<%=EnablePageTrimming.ClientID%>");
//	                var CbFastSearchTrimming = document.getElementById("<%=EnableFastSearchTrimming.ClientID%>");
//	                var CbClearTrimmingCache = document.getElementById("<%=ClearTrimmingCache.ClientID%>");
//	                if (CbListTrimming != null) {
//	                    CbListTrimming.disabled = false;
//	                }
//	                if (CbTabTrimming != null) {
//	                    CbTabTrimming.disabled = false;
//	                }
//	                if (CbWebpartTrimming != null) {
//	                    CbWebpartTrimming.disabled = false;
//	                }
//	                if (CbPageTrimming != null) {
//	                    CbPageTrimming.disabled = false;
//	                }
//	                if (CbFastSearchTrimming != null) {
//	                    CbFastSearchTrimming.disabled = false;
//	                }
//	                if (CbClearTrimmingCache != null) {
//	                    CbClearTrimmingCache.disabled = false;
//	                }
//	            }
//	        }
//            
//	    }

		function CbSecurityTrimming_Clicked(CbCtrl)
		{
			var CbListTrimming = document.getElementById("<%=EnableAllListTrimming.ClientID%>");
			var CbTabTrimming = document.getElementById("<%=EnableTabTrimming.ClientID%>");
			var CbWebpartTrimming = document.getElementById("<%=EnableWebpartTrimming.ClientID%>");				
			var CbPageTrimming = document.getElementById("<%=EnablePageTrimming.ClientID%>");	
			var CbFastSearchTrimming = document.getElementById("<%=EnableFastSearchTrimming.ClientID%>");	
			var CbClearTrimmingCache = document.getElementById("<%=ClearTrimmingCache.ClientID%>");
			if(CbListTrimming !=null)
			{
				if(CbListTrimming.parentElement.disabled)
				{
					CbListTrimming.parentElement.disabled = false;
					if(CbListTrimming.parentElement.parentElement.nextSibling.firstChild.tagName == 'SPAN')
					{
						CbListTrimming.parentElement.parentElement.nextSibling.firstChild.disabled = false;
					}
				}
				
				CbListTrimming.disabled = !CbCtrl.checked;
			}
			if(CbTabTrimming !=null)
			{
				if(CbTabTrimming.parentElement.disabled)
				{
					CbTabTrimming.parentElement.disabled = false;
					if(CbTabTrimming.parentElement.parentElement.nextSibling.firstChild.tagName == 'SPAN')
					{
						CbTabTrimming.parentElement.parentElement.nextSibling.firstChild.disabled = false;
					}
				}				
				CbTabTrimming.disabled = !CbCtrl.checked;
			}
			if(CbWebpartTrimming !=null)
			{
				if(CbWebpartTrimming.parentElement.disabled)
				{
					CbWebpartTrimming.parentElement.disabled = false;
					if(CbWebpartTrimming.parentElement.parentElement.nextSibling.firstChild.tagName == 'SPAN')
					{
						CbWebpartTrimming.parentElement.parentElement.nextSibling.firstChild.disabled = false;
					}
				}				
				CbWebpartTrimming.disabled = !CbCtrl.checked;
			}	
            if(CbPageTrimming !=null)
			{
				if(CbPageTrimming.parentElement.disabled)
				{
					CbPageTrimming.parentElement.disabled = false;
					if(CbPageTrimming.parentElement.parentElement.nextSibling.firstChild.tagName == 'SPAN')
					{
						CbPageTrimming.parentElement.parentElement.nextSibling.firstChild.disabled = false;
					}
				}				
				CbPageTrimming.disabled = !CbCtrl.checked;
			}					
            if(CbFastSearchTrimming !=null)
			{
				if(CbFastSearchTrimming.parentElement.disabled)
				{
					CbFastSearchTrimming.parentElement.disabled = false;
					if(CbFastSearchTrimming.parentElement.parentElement.nextSibling.firstChild.tagName == 'SPAN')
					{
						CbFastSearchTrimming.parentElement.parentElement.nextSibling.firstChild.disabled = false;
					}
				}				
				CbFastSearchTrimming.disabled = !CbCtrl.checked;
			}	
	        if(CbClearTrimmingCache !=null)
			{
				if(CbClearTrimmingCache.parentElement.disabled)
				{
					CbClearTrimmingCache.parentElement.disabled = false;
					if(CbClearTrimmingCache.parentElement.parentElement.nextSibling.firstChild.tagName == 'SPAN')
					{
						CbClearTrimmingCache.parentElement.parentElement.nextSibling.firstChild.disabled = false;
					}
				}				
				CbClearTrimmingCache.disabled = !CbCtrl.checked;
			}				
		}
	</script>
</asp:Content>

<asp:content contentplaceholderid="PlaceHolderMain" runat="server">
	<table class=propertysheet border="0" width="100%" cellspacing="0" cellpadding="0" id="SPSecurityTrimmingSettingsPage">
<%--
        <wssuc:InputFormSection  runat="server" Title="Activate Policy Enforcement" id="WPTSection1">
			<Template_Description>
				<SharePoint:EncodedLiteral runat="server" id="EncodedLiteral16" text="Nextlabs Entitlement Manager for this site collection can be deactivated/activated." EncodeMethod='HtmlEncode'/>
			</Template_Description>
			
			<Template_InputFormControls>
			    <wssawc:InputFormCheckBox ID="EnableEM" LabelText="Activate Policy Enforcement for this site collection."  onclick="javascript:CbEM_Clicked(this)" runat="server"/>
			</Template_InputFormControls>
		</wssuc:InputFormSection>--%>
		
		 <wssuc:InputFormSection  runat="server" Title="SharePoint Page Level Access Control" id="WPTSection2">
			<Template_Description>
				<SharePoint:EncodedLiteral runat="server" id="EncodedLiteral17" text="SharePoint Page Level Access Control allow users to control, via policy, who can access various SharePoint settings pages." EncodeMethod='HtmlEncode'/>
			</Template_Description>
			
			<Template_InputFormControls>
			    <wssawc:InputFormCheckBox ID="EnablePLE" LabelText="Enable access control enforcement for Page resources."  runat="server"/>
			</Template_InputFormControls>
		</wssuc:InputFormSection>
		
		<wssuc:InputFormSection  runat="server" Title="SharePoint Security Trimming" id="WPTSection">
			<Template_Description>
				<SharePoint:EncodedLiteral runat="server" id="DescriptionLiteral" text="SharePoint Security Trimming automatically hides webparts, site menus or tree views that the user has no permission to access on a page." EncodeMethod='HtmlEncode'/>
				<br />
				<br />
				<SharePoint:EncodedLiteral runat="server" id="HelpLiteral" text="For more information about this feature please refer to the Nextlabs Entitlement Manager Product Documentation." EncodeMethod='HtmlEncode'/>
			</Template_Description>
			
			<Template_InputFormControls>
			    <wssawc:InputFormCheckBox ID="EnableSecurityTrimming" LabelText="Enable Security Trimming for current Site Collection." onclick="javascript:CbSecurityTrimming_Clicked(this)" runat="server"/>
			</Template_InputFormControls>
		</wssuc:InputFormSection>
		
		<wssuc:InputFormSection  runat="server" Title="SharePoint List Trimming">
			<Template_Description>
				<SharePoint:EncodedLiteral ID="EncodedLiteral1" runat="server" text="SharePoint List Trimming automatically hides all list items that the user has no permission to access in a list or document library." EncodeMethod='HtmlEncode'/>
			</Template_Description>
			<Template_InputFormControls>
			    <wssawc:InputFormCheckBox ID="EnableAllListTrimming" LabelText="Activate list trimming feature for all lists under current Site Collection." runat="server">
			        <SharePoint:EncodedLiteral runat="server" id="EnableAllListTrimmingLiteral" text="For improving performance, List Trimming should not be Activated for lists or libraries where it is not required." EncodeMethod='HtmlEncode'/>
				</wssawc:InputFormCheckBox>
			</Template_InputFormControls>
		</wssuc:InputFormSection>
		
		<wssuc:InputFormSection  runat="server" Title="Site Tab and Quick Link Trimming">
			<Template_Description>
				<SharePoint:EncodedLiteral ID="EncodedLiteral4" runat="server" text="Site Tab and Quick Link Trimming hides tabs and links for sites that the current user does not have access to." EncodeMethod='HtmlEncode'/>
			</Template_Description>
			<Template_InputFormControls>
			    <wssawc:InputFormCheckBox ID="EnableTabTrimming" LabelText="Enable Site Tab and Quick Link Trimming for the current site collection" runat="server">
			        <SharePoint:EncodedLiteral runat="server" id="EnableTabTrimmingLiteral" text= "" EncodeMethod='HtmlEncode'/>
				</wssawc:InputFormCheckBox>
			</Template_InputFormControls>
		</wssuc:InputFormSection>		
		
		<wssuc:InputFormSection  runat="server" Title="Web Part Trimming">
			<Template_Description>
				<SharePoint:EncodedLiteral ID="EncodedLiteral6" runat="server" text="SharePoint Web Part Trimming hides web parts that that the current user does not have access to." EncodeMethod='HtmlEncode'/>
			</Template_Description>
			<Template_InputFormControls>
			    <wssawc:InputFormCheckBox ID="EnableWebpartTrimming" LabelText="Enable Web Part Trimming for the current site collection" runat="server">
			        <SharePoint:EncodedLiteral runat="server" id="EnableWebpartTrimmingLiteral" text="" EncodeMethod='HtmlEncode'/>
				</wssawc:InputFormCheckBox>
			</Template_InputFormControls>
		</wssuc:InputFormSection>	
		
		<wssuc:InputFormSection  runat="server" Title="Page Trimming">
			<Template_Description>
				<SharePoint:EncodedLiteral ID="EncodedLiteral5" runat="server" text="Page Trimming hides links to pages that the current user does not have access to. " EncodeMethod='HtmlEncode'/>
			</Template_Description>
			<Template_InputFormControls>
			    <wssawc:InputFormCheckBox ID="EnablePageTrimming" LabelText="Enable Page Trimming for the current site collection" runat="server">
			        <SharePoint:EncodedLiteral runat="server" id="EnablePageTrimmingLiteral" text="" EncodeMethod='HtmlEncode'/>
				</wssawc:InputFormCheckBox>
			</Template_InputFormControls>
		</wssuc:InputFormSection>		

		<wssuc:InputFormSection  runat="server" Title="FastSearch Trimming">
			<Template_Description>
				<SharePoint:EncodedLiteral ID="EncodedLiteral7" runat="server" text="FastSearch Trimming hides links to pages that the current user does not have access to. " EncodeMethod='HtmlEncode'/>
			</Template_Description>
			<Template_InputFormControls>
			    <wssawc:InputFormCheckBox ID="EnableFastSearchTrimming" LabelText="Enable FastSearch Trimming for the current site collection" runat="server">
			        <SharePoint:EncodedLiteral runat="server" id="EnableFastSearchTrimmingLiteral" text="" EncodeMethod='HtmlEncode'/>
				</wssawc:InputFormCheckBox>
			</Template_InputFormControls>
		</wssuc:InputFormSection>

		<wssuc:InputFormSection  runat="server" Title="Clear Trimming Cache">
			<Template_Description>
				<SharePoint:EncodedLiteral ID="EncodedLiteral8" runat="server" text="Clear the Trimming Cache. " EncodeMethod='HtmlEncode'/>
			</Template_Description>
			<Template_InputFormControls>
			    <wssawc:InputFormCheckBox ID="ClearTrimmingCache" LabelText="Clear the Trimming Cache" runat="server">
			        <SharePoint:EncodedLiteral runat="server" id="ClearTrimmingCacheLiteral" text="" EncodeMethod='HtmlEncode'/>
				</wssawc:InputFormCheckBox>
			</Template_InputFormControls>
		</wssuc:InputFormSection>
		
		<!-- OK/Cancel Buttons -->  
		<wssuc:ButtonSection id="BtnSectionBottom" runat="server" ShowStandardCancelButton="false">
			<Template_Buttons>
			    <asp:Button runat="server" class="ms-ButtonHeightWidth" OnClick="BtnOK_Click" Text="OK" id="BtnOK" accesskey="<%$Resources:wss,okbutton_accesskey%>"/>
			    <asp:Button runat="server" class="ms-ButtonHeightWidth" OnClick="BtnCancel_Click" Text="Cancel" id="BtnCancel" accesskey="<%$Resources:wss,okbutton_accesskey%>"/>
			    
       		</Template_Buttons>
        </wssuc:ButtonSection>
        
	</TABLE>
</asp:Content>
