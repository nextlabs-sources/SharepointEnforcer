<%@ Assembly Name="Microsoft.SharePoint.ApplicationPages, Version=14.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c"%> 
<%@ Page language="C#" DynamicMasterPageFile="~masterurl/default.master" Inherits="Nextlabs.SPSecurityTrimming.SPListTrimmingPage,Nextlabs.SPSecurityTrimming,Version=3.0.0.0,Culture=neutral,PublicKeyToken=7030e9011c5eb860" %>

<%@ Import Namespace="Microsoft.SharePoint" %> 
<%@ Import Namespace="Microsoft.SharePoint.ApplicationPages" %> 
<%@ Import Namespace="Microsoft.SharePoint.Utilities" %>

<%@ Register TagPrefix="wssuc" TagName="LinksTable" src="/_controltemplates/LinksTable.ascx" %> <%@ Register TagPrefix="wssuc" TagName="InputFormSection" src="/_controltemplates/InputFormSection.ascx" %> <%@ Register TagPrefix="wssuc" TagName="InputFormControl" src="/_controltemplates/InputFormControl.ascx" %> <%@ Register TagPrefix="wssuc" TagName="LinkSection" src="/_controltemplates/LinkSection.ascx" %> <%@ Register TagPrefix="wssuc" TagName="ButtonSection" src="/_controltemplates/ButtonSection.ascx" %> <%@ Register TagPrefix="wssuc" TagName="ActionBar" src="/_controltemplates/ActionBar.ascx" %> <%@ Register TagPrefix="wssuc" TagName="ToolBar" src="/_controltemplates/ToolBar.ascx" %> <%@ Register TagPrefix="wssuc" TagName="ToolBarButton" src="/_controltemplates/ToolBarButton.ascx" %> <%@ Register TagPrefix="wssuc" TagName="Welcome" src="/_controltemplates/Welcome.ascx" %>
<%@ Register Tagprefix="wssawc" Namespace="Microsoft.SharePoint.WebControls" Assembly="Microsoft.SharePoint, Version=14.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %> <%@ Register Tagprefix="SharePoint" Namespace="Microsoft.SharePoint.WebControls" Assembly="Microsoft.SharePoint, Version=14.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %>

<asp:Content contentplaceholderid="PlaceHolderPageTitle" runat="server">
<SharePoint:EncodedLiteral runat="server" Text="<%$Resources:SPSecurityTrimming, ListTrimmingPageTitle%>" EncodeMethod="HtmlEncode" />
</asp:Content>

<asp:Content contentplaceholderid="PlaceHolderPageTitleInTitleArea" runat="server">
<a id=onetidListHlink HREF=<%SPHttpUtility.AddQuote(SPHttpUtility.UrlPathEncode(SPContext.Current.List.DefaultViewUrl,true),Response.Output);%>><%SPHttpUtility.HtmlEncode(SPContext.Current.List.Title,Response.Output);%></a>&#32;<SharePoint:ClusteredDirectionalSeparatorArrow ID="ClusteredDirectionalSeparatorArrow1" runat="server" /> <a HREF=<%SPHttpUtility.AddQuote(SPHttpUtility.UrlPathEncode("listedit.aspx?List=" + SPContext.Current.List.ID.ToString(),true),Response.Output);%>> <SharePoint:FormattedStringWithListType ID="FormattedStringWithListType1" runat="server" String="<%$Resources:wss,listsettings_titleintitlearea%>" LowerCase="false" /></a>&#32;<SharePoint:ClusteredDirectionalSeparatorArrow ID="ClusteredDirectionalSeparatorArrow2" runat="server" />
<SharePoint:EncodedLiteral runat="server" Text="<%$Resources:SPSecurityTrimming,ListTrimmingPageTitle%>" EncodeMethod="HtmlEncode" />
</asp:Content>

<asp:Content contentplaceholderid="PlaceHolderMain" runat="server">
	<SharePoint:FormDigest runat="server" id="FormDigest" />

	<TABLE border="0" cellspacing="0" cellpadding="0" class="ms-propertysheet" width="100%">
		<wssuc:InputFormSection Title="<%$Resources:SPSecurityTrimming,ListTrimmingSectionTitle%>" runat="server">
			<Template_Description>
				<SharePoint:EncodedLiteral runat="server" id="DescriptionLiteral" text="<%$Resources:SPSecurityTrimming, ListTrimmingDescription%>" EncodeMethod='HtmlEncode'/>
				<br />
				<br />
				<SharePoint:EncodedLiteral runat="server" id="HelpLiteral" text="<%$Resources:SPSecurityTrimming, SecuritySettingHelp%>" EncodeMethod='HtmlEncode'/>
			</Template_Description>
			
            <Template_InputFormControls>
                <wssawc:InputFormCheckBox ID="EnableListTrimming" LabelText="<%$Resources:SPSecurityTrimming,EnableListTrimmingText%>" runat="server"/>
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

<%@ Register TagPrefix="wssuc" TagName="TopNavBar" src="~/_controltemplates/TopNavBar.ascx" %>
<asp:Content contentplaceholderid="PlaceHolderTopNavBar" runat="server">
  <wssuc:TopNavBar id="IdTopNavBar" runat="server" Version="4" ShouldUseExtra="true"/>
</asp:Content>
<asp:Content contentplaceholderid="PlaceHolderHorizontalNav" runat="server"/>
<asp:Content contentplaceholderid="PlaceHolderSearchArea" runat="server"/>
<asp:Content contentplaceholderid="PlaceHolderTitleBreadcrumb" runat="server">
  <SharePoint:UIVersionedContent ID="UIVersionedContent1" UIVersion="3" runat="server"><ContentTemplate>
	<asp:SiteMapPath
		SiteMapProvider="SPXmlContentMapProvider"
		id="ContentMap"
		SkipLinkText=""
		NodeStyle-CssClass="ms-sitemapdirectional"
		RootNodeStyle-CssClass="s4-die"
		PathSeparator="&#160;&gt; "
		PathSeparatorStyle-CssClass = "s4-bcsep"
		runat="server" />
  </ContentTemplate></SharePoint:UIVersionedContent>
  <SharePoint:UIVersionedContent ID="UIVersionedContent2" UIVersion="4" runat="server"><ContentTemplate>
	<SharePoint:ListSiteMapPath
		runat="server"
		SiteMapProviders="SPSiteMapProvider,SPXmlContentMapProvider"
		RenderCurrentNodeAsLink="false"
		PathSeparator=""
		CssClass="s4-breadcrumb"
		NodeStyle-CssClass="s4-breadcrumbNode"
		CurrentNodeStyle-CssClass="s4-breadcrumbCurrentNode"
		RootNodeStyle-CssClass="s4-breadcrumbRootNode"
		HideInteriorRootNodes="true"
		SkipLinkText="" />
  </ContentTemplate></SharePoint:UIVersionedContent>
</asp:Content>
