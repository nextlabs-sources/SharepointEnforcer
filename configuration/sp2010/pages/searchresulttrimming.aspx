<%@ Assembly Name="Microsoft.SharePoint.ApplicationPages.Administration, Version=14.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c"%> 
<%@ Page Language="C#" Inherits="NextLabs.Deployment.SearchResultTrimmingPage, NextLabs.Deployment,Version=1.0.0.0,Culture=neutral,PublicKeyToken=e03e4c7ee29d89ce" MasterPageFile="~/_admin/admin.master"%> 
<%@ Import Namespace="Microsoft.SharePoint.ApplicationPages" %> 
<%@ Register Tagprefix="SharePoint" Namespace="Microsoft.SharePoint.WebControls" Assembly="Microsoft.SharePoint, Version=14.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %> <%@ Register Tagprefix="Utilities" Namespace="Microsoft.SharePoint.Utilities" Assembly="Microsoft.SharePoint, Version=14.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %> <%@ Import Namespace="Microsoft.SharePoint" %> <%@ Assembly Name="Microsoft.Web.CommandUI, Version=14.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %>
<%@ Register Tagprefix="SharePoint" Namespace="Microsoft.SharePoint.WebControls" Assembly="Microsoft.SharePoint, Version=14.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %> 
<%@ Register Tagprefix="Utilities" Namespace="Microsoft.SharePoint.Utilities" Assembly="Microsoft.SharePoint, Version=14.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %> 
<%@ Import Namespace="Microsoft.SharePoint" %> 
<%@ Assembly Name="Microsoft.Web.CommandUI, Version=14.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %> <%@ Register Tagprefix="wssawc" Namespace="Microsoft.SharePoint.WebControls" Assembly="Microsoft.SharePoint, Version=14.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %> <%@ Register Tagprefix="SharePoint" Namespace="Microsoft.SharePoint.WebControls" Assembly="Microsoft.SharePoint, Version=14.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %> 

<%@ Register TagPrefix="wssuc" TagName="InputFormSection" src="~/_controltemplates/InputFormSection.ascx" %>
<%@ Register TagPrefix="wssuc" TagName="InputFormControl" src="~/_controltemplates/InputFormControl.ascx" %>
<%@ Register TagPrefix="wssuc" TagName="ButtonSection" src="~/_controltemplates/ButtonSection.ascx" %>
<%@ Register TagPrefix="wssuc" TagName="TemplatePickerControl" src="~/_controltemplates/TemplatePickerControl.ascx" %>
<%@ Register Tagprefix="wssawc" Namespace="Microsoft.SharePoint.WebControls" Assembly="Microsoft.SharePoint, Version=14.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %> 

<asp:Content contentplaceholderid="PlaceHolderPageTitle" runat="server">
	<SharePoint:EncodedLiteral runat="server" text="NextLabs Entitlement Manager - Configure search trimming" EncodeMethod='HtmlEncode'/>
</asp:content>
<asp:Content contentplaceholderid="PlaceHolderPageTitleInTitleArea" runat="server">
	<SharePoint:EncodedLiteral runat="server" text="NextLabs Entitlement Manager - Configure search trimming" EncodeMethod='HtmlEncode'/>
</asp:Content>
<asp:content contentplaceholderid="PlaceHolderAdditionalPageHead" runat="server">

</asp:content>

<asp:content contentplaceholderid="PlaceHolderPageDescription" runat="server">
<SharePoint:EncodedLiteral runat="server" text="Manager NextLabs Entitlement Manager search trimmers for SharePoint Search." EncodeMethod='HtmlEncodeAllowSimpleTextFormatting'/>
</asp:content>

<asp:content contentplaceholderid="PlaceHolderMain" runat="server">
  <asp:literal id="LiteralHiddenQuotaTemplates" runat="server"/>
    <table border="0" cellspacing="0" cellpadding="0" class="ms-propertysheet" width="100%">
	
	<wssuc:InputFormSection runat="server"
		Title="Search Service Application"
		Description="Install trimmer for the selected Search Service Application."
		id="SSADescSection">
           <template_inputformcontrols>
                <wssuc:InputFormControl LabelText="Search Service Application Name" runat="server">
					<Template_Control>
                        <asp:DropDownList ID="SSA_Name" runat="server" EnableViewState="true" AutoPostBack="true" OnSelectedIndexChanged="SSA_Name_SelectedIndexChanged"></asp:DropDownList>
					</Template_Control>
				</wssuc:InputFormControl>       
			</template_inputformcontrols>
	</wssuc:InputFormSection>
	

	<wssuc:InputFormSection Title="Crawl Rule"
		Description="Provide the Crawl Rule URL for which you wish to install the trimmer.  The Crawl Rule URL must be the same as specified in Search Administration."
		runat="server"
		id="idSecondaryAdministratorSection">
           <template_inputformcontrols>
                <wssuc:InputFormControl LabelText="Crawl rule URL" runat="server">
					<Template_Control>
                        <asp:DropDownList ID="CR_URL" runat="server" EnableViewState="true" AutoPostBack="true" OnSelectedIndexChanged="CR_URL_SelectedIndexChanged"></asp:DropDownList>
					</Template_Control>
				</wssuc:InputFormControl>       
			</template_inputformcontrols>
	</wssuc:InputFormSection>
	
	<wssuc:InputFormSection Title="Trimmer ID"
		Description="Provide unique numeric ID for the trimmer (1-10000), default is 100."
		runat="server"
		id="idthirdAdministratorSection">
		<Template_InputFormControls>
            <Template_Description>
				<SharePoint:EncodedLiteral ID="inputid" runat="server" text="Please enter ID:" EncodeMethod='HtmlEncode'/>
			</Template_Description>
			<br/>
			<br/>
            <wssawc:InputFormTextBox title="ID" class="ms-input" ID="Identifier" text="100" Columns="35" Runat="server" MaxLength=512 Direction="LeftToRight" />			
        </Template_InputFormControls>
	</wssuc:InputFormSection>

	<wssuc:InputFormSection Title="Status"
		Description="The status of SearchResultTrimming on this crawl rule URL."
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
						<asp:Button runat="server" class="ms-ButtonHeightWidth" OnClick="BtnOK_Click" Text="Install" id="BtnOK" accesskey="<%$Resources:wss,okbutton_accesskey%>"/>
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
