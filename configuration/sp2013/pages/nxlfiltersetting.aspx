<%@ Assembly Name="Microsoft.SharePoint.ApplicationPages.Administration, Version=15.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c"%> 
<%@ Page Language="C#" Inherits="NextLabs.Deployment.NxlFilterPage, NextLabs.Deployment,Version=1.0.0.0,Culture=neutral,PublicKeyToken=e03e4c7ee29d89ce" MasterPageFile="~/_admin/admin.master"%> 
<%@ Import Namespace="Microsoft.SharePoint.ApplicationPages" %> 
<%@ Register Tagprefix="SharePoint" Namespace="Microsoft.SharePoint.WebControls" Assembly="Microsoft.SharePoint, Version=15.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %> <%@ Register Tagprefix="Utilities" Namespace="Microsoft.SharePoint.Utilities" Assembly="Microsoft.SharePoint, Version=15.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %> <%@ Import Namespace="Microsoft.SharePoint" %> <%@ Assembly Name="Microsoft.Web.CommandUI, Version=15.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %>
<%@ Register Tagprefix="SharePoint" Namespace="Microsoft.SharePoint.WebControls" Assembly="Microsoft.SharePoint, Version=15.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %> 
<%@ Register Tagprefix="Utilities" Namespace="Microsoft.SharePoint.Utilities" Assembly="Microsoft.SharePoint, Version=15.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %> 
<%@ Import Namespace="Microsoft.SharePoint" %> 
<%@ Assembly Name="Microsoft.Web.CommandUI, Version=15.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %> <%@ Register Tagprefix="wssawc" Namespace="Microsoft.SharePoint.WebControls" Assembly="Microsoft.SharePoint, Version=15.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %> <%@ Register Tagprefix="SharePoint" Namespace="Microsoft.SharePoint.WebControls" Assembly="Microsoft.SharePoint, Version=15.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %> 

<%@ Register TagPrefix="wssuc" TagName="InputFormSection" src="~/_controltemplates/InputFormSection.ascx" %>
<%@ Register TagPrefix="wssuc" TagName="InputFormControl" src="~/_controltemplates/InputFormControl.ascx" %>
<%@ Register TagPrefix="wssuc" TagName="ButtonSection" src="~/_controltemplates/ButtonSection.ascx" %>
<%@ Register TagPrefix="wssuc" TagName="TemplatePickerControl" src="~/_controltemplates/TemplatePickerControl.ascx" %>
<%@ Register Tagprefix="wssawc" Namespace="Microsoft.SharePoint.WebControls" Assembly="Microsoft.SharePoint, Version=15.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %> 

<asp:Content contentplaceholderid="PlaceHolderPageTitle" runat="server">
	<SharePoint:EncodedLiteral runat="server" text="NextLabs Entitlement Management - Configure NextLabs Rights Management" EncodeMethod='HtmlEncode'/>
</asp:content>
<asp:Content contentplaceholderid="PlaceHolderPageTitleInTitleArea" runat="server">
	<SharePoint:EncodedLiteral runat="server" text="NextLabs Entitlement Management - Configure NextLabs Rights Management" EncodeMethod='HtmlEncode'/>
</asp:Content>
<asp:content contentplaceholderid="PlaceHolderAdditionalPageHead" runat="server">

</asp:content>

<asp:content contentplaceholderid="PlaceHolderPageDescription" runat="server">
<SharePoint:EncodedLiteral runat="server" text="Configure support for NextLabs Rights Management" EncodeMethod='HtmlEncodeAllowSimpleTextFormatting'/>
</asp:content>

<asp:content contentplaceholderid="PlaceHolderMain" runat="server">
  <asp:literal id="LiteralHiddenQuotaTemplates" runat="server"/>
    <table border="0" cellspacing="0" cellpadding="0" class="ms-propertysheet" width="100%">
	
	<wssuc:InputFormSection runat="server"
		Title="Description"
		Description="If you will be managing NextLabs rights protected files in Microsoft SharePoint installing the NXL iFilter will enable search to work for protected files. "
		id="idTitleDescSection">
		<Template_InputFormControls>
            <Template_Description>
				<SharePoint:EncodedLiteral ID="Description" runat="server" text="This page is used to install or uninstall nxlfilter." EncodeMethod='HtmlEncode'/>
			</Template_Description>
			<br/>
			<br/>			
        </Template_InputFormControls>
	</wssuc:InputFormSection>
	


	<wssuc:InputFormSection Title="Status:"
		Description="After installing or uninstalling the NXL iFilter it is recommended that you run a full crawl to update search indices."
		runat="server"
		id="statusSection">
		<Template_InputFormControls>
            <Template_Description>
            <br/>
				<SharePoint:EncodedLiteral ID="ExcuteStatus" runat="server" text="Not Install." EncodeMethod='HtmlEncode'/>
			</Template_Description>
		</Template_InputFormControls>
	</wssuc:InputFormSection>
	
	<SharePoint:DelegateControl runat="server" Id="DelctlCreateSiteCollectionPanel" ControlId="CreateSiteCollectionPanel1" Scope="Farm" />
	<wssuc:ButtonSection runat="server" ShowStandardCancelButton="false">
		<Template_Buttons>
            	<asp:PlaceHolder ID="PlaceHolder1" runat="server">				
						<asp:Button runat="server" class="ms-ButtonHeightWidth" OnClick="Btninstall_Click" Text="Install"  id="Btninstall"/>
				</asp:PlaceHolder>	
				<asp:PlaceHolder ID="PlaceHolder2" runat="server">					
						<asp:Button runat="server" class="ms-ButtonHeightWidth" OnClick="BtnUninstall_Click" Text="Uninstall" id="BtnUninstall" CausesValidation="false"/>
				</asp:PlaceHolder>
				<asp:PlaceHolder ID="PlaceHolder3" runat="server">					
						<asp:Button runat="server" class="ms-ButtonHeightWidth" Visible="false" OnClick="BtnCancel_Click" Text="<%$Resources:wss,multipages_cancelbutton_text%>" id="BtnCancel" CausesValidation="false"/>
				</asp:PlaceHolder>	
		</Template_Buttons>
	</wssuc:ButtonSection>
   </table>
  </asp:content>
