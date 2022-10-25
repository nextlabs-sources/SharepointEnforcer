<%@ Assembly Name="Microsoft.SharePoint.ApplicationPages, Version=15.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c"%>
<%@ Register Tagprefix="SPSecurityTrimming" Namespace="Nextlabs.SPSecurityTrimming" Assembly="Nextlabs.SPSecurityTrimming, Version=3.0.0.0, Culture=neutral, PublicKeyToken=7030e9011c5eb860" %> 
<%@ Page Language="C#" DynamicMasterPageFile="~masterurl/default.master" Inherits="Microsoft.SharePoint.ApplicationPages.AllAppsPage"   EnableViewState="false"    %> 
<%@ Import Namespace="Microsoft.SharePoint.ApplicationPages" %> 
<%@ Register Tagprefix="SharePoint" Namespace="Microsoft.SharePoint.WebControls" Assembly="Microsoft.SharePoint, Version=15.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %> 
<%@ Register Tagprefix="Utilities" Namespace="Microsoft.SharePoint.Utilities" Assembly="Microsoft.SharePoint, Version=15.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %> 
<%@ Import Namespace="Microsoft.SharePoint" %> 
<%@ Assembly Name="Microsoft.Web.CommandUI, Version=15.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %>
<%@ Register TagPrefix="wssuc" TagName="TopNavBar" src="~/_controltemplates/15/TopNavBar.ascx" %>
<%@ Register TagPrefix="wssuc" TagName="ToolBar" src="~/_controltemplates/15/ToolBar.ascx" %>
<%@ Register TagPrefix="wssuc" TagName="ToolBarButton" src="~/_controltemplates/15/ToolBarButton.ascx" %>
<%@ Register TagPrefix="wssuc" TagName="ViewHeader" src="~/_controltemplates/15/ViewHeader.ascx" %>
<%@ Register Tagprefix="wssawc" Namespace="Microsoft.SharePoint.WebControls" Assembly="Microsoft.SharePoint, Version=15.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %> 
<%@ Register Tagprefix="SharePoint" Namespace="Microsoft.SharePoint.WebControls" Assembly="Microsoft.SharePoint, Version=15.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %>
<%@ Import Namespace="Microsoft.SharePoint.Administration" %>
<asp:Content ContentPlaceHolderId="PlaceHolderPageTitle" runat="server">
<SharePoint:EncodedLiteral runat="server" text="<%$Resources:wss,viewlsts_pagetitle_doclist_15%>" EncodeMethod='HtmlEncode'/>
</asp:Content>
<asp:Content ContentPlaceHolderId="PlaceHolderPageImage" runat="server"><SharePoint:AlphaImage src="/_layouts/15/images/allcontents.png?rev=23" Height="54" Width="145" Alt="" runat="server"/></asp:Content>
<asp:Content ContentPlaceHolderId="PlaceHolderAdditionalPageHead" runat="server">
	<SharePoint:ScriptLink language="javascript" runat="server" name="DragDrop.js" Localizable="false" OnDemand="true" />
	<SharePoint:ScriptBlock runat="server">
		var navBarHelpOverrideKey = "WSSEndUser_ListOLists";
	</SharePoint:ScriptBlock>
</asp:Content>
<asp:Content ContentPlaceHolderId="PlaceHolderPageDescription" runat="server">
	<asp:Label id="LabelPageDescription" runat="server"/>
</asp:Content>
<asp:Content ContentPlaceHolderId ="PlaceHolderTitleLeftBorder" runat="server">
 <div class="ms-titleareaframe"><img src="/_layouts/15/images/blank.gif?rev=23" width='1' height='100%' alt="" /></div>
</asp:Content>
<asp:Content ContentPlaceHolderId ="PlaceHolderTitleRightMargin" runat="server">
 <div style="height:100%;" class="ms-titleareaframe"><img src="/_layouts/15/images/blank.gif?rev=23" width='1' height='1' alt="" /></div>
</asp:Content>
<asp:Content ContentPlaceHolderId ="PlaceHolderBodyLeftBorder" runat="server">
 <div style="height:100%;" class="ms-pagemargin"><img src="/_layouts/15/images/blank.gif?rev=23" width='10' height='1' alt="" /></div>
</asp:Content>
<asp:Content ContentPlaceHolderId ="PlaceHolderBodyRightMargin" runat="server">
 <div style="height:100%;" class="ms-pagemargin"><img src="/_layouts/15/images/blank.gif?rev=23" width='10' height='1' alt="" /></div>
</asp:Content>
<asp:Content contentplaceholderid="PlaceHolderHorizontalNav" runat="server"/>
<asp:Content contentplaceholderid="PlaceHolderTitleBreadcrumb" runat="server">
	<SharePoint:UIVersionedContent UIVersion="<=4" runat="server"><ContentTemplate>
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
	<SharePoint:UIVersionedContent UIVersion=">=15" runat="server"><ContentTemplate>
		<SharePoint:ListSiteMapPath
			runat="server"
			SiteMapProviders="SPSiteMapProvider,SPXmlContentMapProvider"
			RenderCurrentNodeAsLink="false"
			PathSeparator=""
			CssClass="ms-breadcrumb"
			NodeStyle-CssClass="ms-breadcrumbNode"
			CurrentNodeStyle-CssClass="ms-breadcrumbCurrentNode"
			RootNodeStyle-CssClass="ms-breadcrumbRootNode"
			HideInteriorRootNodes="true"
			SkipLinkText="" />
	</ContentTemplate></SharePoint:UIVersionedContent>
</asp:Content>
<asp:Content ContentPlaceHolderId="PlaceHolderMain" runat="server">
<SharePoint:ScriptBlock runat="server">
var g_appTypeNone = 0;
var g_appTypeList = 1;
var g_appTypeApp = 2;
var g_appTypeFeature = 3;
var g_isCorpSite = false;
var tenantAppData = GetTenantAppData();
if (tenantAppData != null)
{
	g_isCorpSite = tenantAppData["isCorpSite"];
}
var g_recycleBinEnabled = <% if (RecycleBinEnabled) { SPHttpUtility.WriteNoEncode("true",this.Page); } else { SPHttpUtility.WriteNoEncode("false",this.Page); } %>;
var g_appSourceMarketPlace = 1;
var g_appSourceCorpCatalog = 2;
</SharePoint:ScriptBlock>
  <table id="appsTable" cellspacing="0" cellpadding="0" style="border-collapse:collapse;" border="0" class="ms-viewlsts">
	<tr class="ms-vl-sectionHeaderRow">
	  <td colspan="3">
		<span class="ms-vl-sectionHeader" >
		   <h2 class="ms-webpart-titleText">
			<span><SharePoint:EncodedLiteral runat="server" text="<%$Resources:wss,allapps_appsTitle%>" EncodeMethod='HtmlEncode'/>
			</span>
		  </h2>
		</span>
	  </td>
	  <td class="ms-alignRight">
		  <SharePoint:ClusteredSPLinkButton
			runat="server"
			id="diidIOSiteWorkflows"
			ShowImageAndText="true"
			CssClass="ms-calloutLink ms-vl-alignactionsmiddle"
			AccessKey="<%$Resources:wss,viewlsts_SiteWorkflow_ak%>"
			ImageUrl="/_layouts/15/images/fgimg.png?rev=23"
			ImageWidth=16
			ImageHeight=16
			OffsetX=0
			OffsetY=629
			Text="<%$Resources:wss,siteactions_siteworkflow%>"
			ThemeKey="fgimg"
			NavigateUrl="~site/_layouts/workflow.aspx"
			PermissionsString="EditListItems, AddAndCustomizePages"
			PermissionMode="Any"
		  />
		  <SharePoint:ClusteredSPLinkButton
			runat="server"
			id="diidSiteSettings"
			ShowImageAndText="true"
			CssClass="ms-calloutLink ms-vl-settingsmarginleft ms-vl-alignactionsmiddle"
			AccessKey="<%$Resources:wss,allapps_SiteSettings_ak%>"
			ImageUrl="/_layouts/15/images/spcommon.png?rev=23"
			ImageWidth=15
			ImageHeight=14
			OffsetX=179
			OffsetY=114
			Text="<%$Resources:wss,allapps_settings%>"
			ThemeKey="spcommon"
			NavigateUrl="~site/_layouts/settings.aspx"
			PermissionsString="EnumeratePermissions,ManageWeb,ManageSubwebs,AddAndCustomizePages,ApplyThemeAndBorder,ManageAlerts,ManageLists,ViewUsageData"
			PermissionMode="Any"
		  />
		  <% if (RecycleBinEnabled) { %>
		  <SharePoint:ClusteredSPLinkButton
			runat="server"
			id="diidRecycleBin"
			ShowImageAndText="true"
			CssClass="ms-calloutLink ms-vl-settingsmarginleft ms-vl-alignactionsmiddle"
			ImageUrl="/_layouts/15/images/spcommon.png?rev=23"
			ImageWidth=16
			ImageHeight=16
			OffsetX=196
			OffsetY=155
			ThemeKey="spcommon"
			NavigateUrl="~site/_layouts/RecycleBin.aspx"
			PermissionsString="DeleteListItems"
			PermissionMode="Any"
		  />
		  <% } %>
	  </td>
	</tr>
	<tr>
	  <td colspan="4">
		<div id="applist" class="ms-vl-applist">
		  <% if (UserHasAppInstallRights){ %>
		  <div id="apptile-appadd" class="ms-vl-apptile ms-vl-apptilehover ms-vl-pointer" onClick="javascript:navigateToAddAnApp();">
			<div class="ms-vl-appimage">
			  <a tabindex="-1" style="height:97px;width:97px;position:relative;display:inline-block;overflow:hidden;" id="appadd" href="<%SPHttpUtility.WriteHtmlEncode(this.AddAnAppUrl, this.Page);%>" title="<SharePoint:EncodedLiteral runat='server' text='<%$Resources:wss,allapps_addAnApp%>' EncodeMethod='HtmlEncode'/>" name="<SharePoint:EncodedLiteral runat='server' text='<%$Resources:wss,allapps_addAnApp%>' EncodeMethod='HtmlEncode'/>">
				<SharePoint:ThemedForegroundImage
				  ThemeKey="spcommon"
				  ImageUrl="/_layouts/15/images/spcommon.png?rev=23"
				  CssClass="ms-vl-appadd-img"
				  alt="<SharePoint:EncodedLiteral runat='server' text='<%$Resources:wss,allapps_addAnApp%>' EncodeMethod='HtmlEncode'/>"
				  runat="server"
				/>
			  </a>
			</div>
			<div class="ms-vl-appinfo">
			  <div style="display:table-cell;vertical-align:middle;height:96px;">
				<a class="ms-verticalAlignMiddle ms-textLarge ms-vl-apptitle" id="appadd-link" href="<%SPHttpUtility.WriteHtmlEncode(this.AddAnAppUrl, this.Page);%>" title="<SharePoint:EncodedLiteral runat='server' text='<%$Resources:wss,allapps_addAnApp%>' EncodeMethod='HtmlEncode'/>" name="<SharePoint:EncodedLiteral runat='server' text='<%$Resources:wss,allapps_addAnApp%>' EncodeMethod='HtmlEncode'/>">
				  <SharePoint:EncodedLiteral runat="server" text="<%$Resources:wss,allapps_addAnApp%>" EncodeMethod='HtmlEncodeAllowSimpleTextFormatting'/>
				</a>
			  </div>
			</div>
		  </div>
		  <% } %>
<script runat="server">
StringBuilder appJSON;
int statusCount;
string statusColor;
string draggableContentIds;
StringBuilder appSortList;
</script>
<%
DateTime dtCurrent = DateTime.UtcNow;
draggableContentIds = "var g_draggableContentIds = [";
appJSON = new StringBuilder("var appData = {");
appSortList = new StringBuilder("var appSortList = [");
statusColor = "yellow";
statusCount = 0;
	int j = 0;
	bool first = true;
	foreach (KeyValuePair<string, object> entry in rgApps)
	{
		if (!first)
		{
			draggableContentIds += ",";
			appJSON.Append(",");
			appSortList.Append(",");
		}
		else
		{
			first = false;
		}
		Object obj = entry.Value;
		string title = "";
		string launchUrl = "";
		string appSettingsUrl = null;
		string productId = null;
		string assetId = null;
		string version = "";
		string id = null;
		string description = "null";
		string imageUrl = "null";
		string thumbnailUrl = "null";
		AppType appType = AppType.None;
		bool isUserWebAdmin = Web.DoesUserHavePermissions(SPBasePermissions.ManageWeb);
		string oauthId = "";
		string appSource = "0";
		string contentMarket = "";
		string problemUrl = "";
		bool hasErrorStyle = false;
		bool retryForRemove = false;
		int sourceState = -255;
		SPAppUIStateWrapper uiState = null;
		bool appDisabled = false;
		bool appNeedsAttention = false;
			if (obj is SPList)
			{
				SPList list = (SPList)obj;
				title = list.Title;
				id = list.ID.ToString("D");
				appType = AppType.List;
                
				try
				{
                    // Added by SharePoint Security Trimming feature of ComplianceEnterprise product
                    bool allow = ViewListPageTrimmer.TrimList(Context, list);
                    if (!allow)
                    {
                        continue;
                    }
                    
					if (list.BaseTemplate == SPListTemplateType.WebPageLibrary)
					{
						launchUrl = GetRootFolderOfList(list);
					}
					else
					{
						launchUrl = list.DefaultViewUrl;
					}
				}
				catch
				{
					launchUrl = "";
				}
				if (launchUrl == "")
					launchUrl = "ListEdit.aspx?List=" + SPHttpUtility.HtmlUrlAttributeEncode(list.ID.ToString("B").ToUpper());
				productId = GetFeatureIdForList(list);
				assetId = GetAssetIdForList(list);
				description = list.Description;
				imageUrl = list.ImageUrl;
				thumbnailUrl = GetListThumbnailUrl(list);
				version = Convert.ToString(list.Version);
			}
			else if (obj is SPAppInstance)
			{
				SPAppInstance app = (SPAppInstance)obj;
				CheckAppInstanceForCallback(app);
				uiState = GetUIStateWrapperFromInstance(app, Web);
				title = app.Title;
				appType = AppType.App;
				id = app.Id.ToString("D");
				appDisabled = uiState.Disabled;
				hasErrorStyle = uiState.HasErrorStyle;
				retryForRemove = uiState.RetryForRemove;
				appNeedsAttention = AppNeedsAttention(app);
				version = this.GetAppVersion(app);
				problemUrl = GetAppProblemLink(app);
				oauthId = this.GetOAuthIdFromAppInstance(app);
				appSource = Convert.ToString((int)app.App.Source);
				launchUrl = GetAppLaunchUrl(app);
				appSettingsUrl = GetAppSettingsUrl(app);
				productId=app.App.ProductId.ToString("D");
				assetId = this.GetAssetIdFromAppInstance(app);
				contentMarket = this.GetContentMarketFromAppInstance(app);
				thumbnailUrl = this.GetIconUrl(app);
				if (appNeedsAttention)
				{
					statusCount++;
					if ((sourceState == (int)SPAppSourceState.Withdrawn) ||
						(sourceState == (int)SPAppSourceState.Revoked) ||
						(sourceState == (int)SPAppSourceState.Removed) ||
						appDisabled)
					{
						statusColor = "red";
					}
				}
			}
			else
			{
				SPFeature f = (SPFeature)obj;
				id = f.Definition.Id.ToString("D");
				version = Convert.ToString(f.Definition.Version);
				SPAppDisplayData appDisplayData = this.GetAppDisplayData(f);
				launchUrl = this.GetServerRelativeUrl(appDisplayData.LaunchUrl.ToString());
				assetId = GetAssetIdForFeature(f.Definition);
				title = this.GetTitleWithDefaultForFeature(f);
				appType = AppType.Feature;
				description = f.Definition.GetDescription(SPContext.Current.Web.Locale);
				thumbnailUrl = this.GetFeatureThumbnail(f);
			}
			appJSON.Append("'");
			appJSON.Append(SPHttpUtility.EcmaScriptStringLiteralEncode(id.ToLower()));
			appJSON.Append("':{title:'");
			appJSON.Append(SPHttpUtility.EcmaScriptStringLiteralEncode(title));
			appJSON.Append("', id:'");
			appJSON.Append(SPHttpUtility.EcmaScriptStringLiteralEncode(id.ToLower()));
			appJSON.Append("', uistate:");
			appJSON.Append(uiState == null ? "-1" : SPHttpUtility.EcmaScriptStringLiteralEncode(((int)uiState.State).ToString()));
			appJSON.Append(", launchUrl:'");
			appJSON.Append(SPHttpUtility.EcmaScriptStringLiteralEncode(launchUrl));
			appJSON.Append("', appSettingsUrl:");
			if (appSettingsUrl == null)
			{
				appJSON.Append("null");
			}
			else
			{
				appJSON.Append("'");
				appJSON.Append(SPHttpUtility.EcmaScriptStringLiteralEncode(appSettingsUrl));
				appJSON.Append("'");
			}
			appJSON.Append(",productId:");
			if (productId == null)
			{
				appJSON.Append("null");
			}
			else
			{
				appJSON.Append("'");
				appJSON.Append(SPHttpUtility.EcmaScriptStringLiteralEncode(productId.ToLower()));
				appJSON.Append("'");
			}
			appJSON.Append(",assetId:");
			if (assetId == null)
			{
				appJSON.Append("null");
			}
			else
			{
				appJSON.Append("'");
				appJSON.Append(SPHttpUtility.EcmaScriptStringLiteralEncode(assetId));
				appJSON.Append("'");
			}
			appJSON.Append(", type:");
			appJSON.Append(Convert.ToString((int)appType));
			appJSON.Append(",description:'");
			appJSON.Append(SPHttpUtility.EcmaScriptStringLiteralEncode(description));
			appJSON.Append("',imageUrl:'");
			appJSON.Append(SPHttpUtility.EcmaScriptStringLiteralEncode(imageUrl));
			appJSON.Append("',thumbnailUrl:'");
			appJSON.Append(SPHttpUtility.EcmaScriptStringLiteralEncode(thumbnailUrl));
			appJSON.Append("'");
			appJSON.Append(",calloutEnabled:");
			appJSON.Append(uiState == null ? "true" : uiState.CalloutEnabled ? "true" : "false");
			appJSON.Append(",updatable:");
			appJSON.Append(uiState == null ? "false" : uiState.Updatable ? "true" : "false");
			appJSON.Append(",disabled:");
			appJSON.Append(uiState == null ? "false" : uiState.Disabled ? "true" : "false");
			appJSON.Append(",hasErrorStyle:");
			appJSON.Append(hasErrorStyle ? "true" : "false");
			appJSON.Append(",retryForRemove:");
			appJSON.Append(retryForRemove ? "true" : "false");
			appJSON.Append(",isTenantApp:false");
			appJSON.Append(",isUserWebAdmin:");
			appJSON.Append(isUserWebAdmin ? "true" : "false");
			appJSON.Append(",oauthId:'");
			appJSON.Append(SPHttpUtility.EcmaScriptStringLiteralEncode(oauthId));
			appJSON.Append("',problemUrl:'");
			appJSON.Append(SPHttpUtility.EcmaScriptStringLiteralEncode(problemUrl));
			appJSON.Append("',contentMarket:'");
			appJSON.Append(contentMarket);
			appJSON.Append("',version:'");
			appJSON.Append(SPHttpUtility.EcmaScriptStringLiteralEncode(SPHttpUtility.HtmlEncode(version)));
			appJSON.Append("',appSource:");
			appJSON.Append(appSource);
			appJSON.Append("}");
			appSortList.Append("{id:'");
			appSortList.Append(SPHttpUtility.EcmaScriptStringLiteralEncode(id.ToLower()));
			appSortList.Append("',title:'");
			appSortList.Append(SPHttpUtility.EcmaScriptStringLiteralEncode(title));
			appSortList.Append("'}");
			string linkId = "viewlist" + id;
			%>
<% if (IsMostRecentItem(obj)) { %>
<a tabindex="-1" href="javascript:;" name="newest"></a>
<%}%>
<div
  id="apptile-<%SPHttpUtility.WriteHtmlEncode(id.ToLower(), this.Page);%>"
  class="ms-vl-apptile ms-vl-apptilehover <%if(appDisabled){%> ms-vl-disabledapp<%}%>"
  onMouseOver="javascript:if (typeof(showCalloutIcon) == 'function'){showCalloutIcon('<%SPHttpUtility.WriteHtmlEncode(id.ToLower(), this.Page);%>');}"
  onMouseOut="if (typeof(hideCalloutIcon) == 'function'){javascript:hideCalloutIcon('<%SPHttpUtility.WriteHtmlEncode(id.ToLower(), this.Page);%>');}"
>
<div class="ms-vl-appimage">
<%
if (appType == AppType.App)
{
%>
		<a
		  class="ms-storefront-selectanchor ms-storefront-appiconspan"
		  tabindex="-1"
		  id="<%=linkId%>-image"
		  <%if(!appDisabled){%>onclick="<%=GetAppLaunchOnClick((SPAppInstance)obj)%>"
		  href="<%=GetAppLaunchHref((SPAppInstance)obj)%>"<%}%>>
<%
}
else if (appType == AppType.List || appType == AppType.Feature)
{
 %>
 <% if (imageUrl == "null"){%>
		<a
		  class="ms-storefront-selectanchor ms-storefront-appiconspan"
		  tabindex="-1"
		  id="<%=linkId%>-image"
		  href="<%SPHttpUtility.WriteHtmlEncode(launchUrl,this.Page);%>"
		>
<% } else { %>
		<a
		  class="ms-storefront-selectanchor ms-storefront-appiconspan"
		  tabindex="-1"
		  id="<%=linkId%>-image"
		  href="<%SPHttpUtility.WriteHtmlEncode(launchUrl,this.Page);%>"
		>
<% } %>
<%
}
%>
<% if (thumbnailUrl == "null"){%>
		<img alt=<%SPHttpUtility.WriteAddQuote(SPHttpUtility.HtmlEncode(title),this.Page);%> src="/_layouts/15/images/spstorefrontappdefault.96x96x32.png?rev=23"></img>
<% } else { %>
		<img
		  class="ms-storefront-appiconimg"
		  style="border:0;"
		  alt=<%SPHttpUtility.WriteAddQuote(SPHttpUtility.HtmlEncode(title),this.Page);%>
		  src=<%SPHttpUtility.WriteAddQuote(SPHttpUtility.UrlPathEncode(thumbnailUrl,true),this.Page);%>
		>
		</img>
<% } %>
		</a>
</div>
<div
  id="appinfo-<%SPHttpUtility.WriteHtmlEncode(id.ToLower(), this.Page);%>"
  class="ms-vl-appinfo ms-vl-pointer"
  onClick="javascript:launchCallout(arguments[0],'<%SPHttpUtility.WriteHtmlEncode(id.ToLower(), this.Page);%>', this)"
>
<div>
<div class="ms-vl-apptitleouter">
<%
draggableContentIds += "\"" + linkId + "\"";
if (appType == AppType.App)
{
	string appHref = launchUrl;
	if (appHref.IndexOf('?') == -1)
	{
		appHref += "?";
	}
	else
	{
		appHref += "&";
	}
	appHref += "SPSiteUrl=" +
		SPHttpUtility.UrlKeyValueEncode(Web.Url);
%>
		<a
		  class="<%if(!appDisabled){%>ms-draggable ms-listLink<%}%> <%if(appDisabled){%>ms-vl-disabledapp<%}%> ms-vl-apptitle"
		  id="<%=linkId%>" <%if(!appDisabled){%>onclick="<%=GetAppLaunchOnClick((SPAppInstance)obj)%>"
		  href="<%=GetAppLaunchHref((SPAppInstance)obj)%>"<%}%>
		  title=<%SPHttpUtility.WriteAddQuote(SPHttpUtility.HtmlEncode(title),this.Page);%>
		>
<%
}
else if (appType == AppType.List || appType == AppType.Feature)
{
 %>
		<a
		  class="ms-draggable ms-vl-apptitle ms-listLink"
		  id="<%=linkId%>"
		  href="<%SPHttpUtility.WriteHtmlEncode(launchUrl,this.Page);%>"
		  title=<%SPHttpUtility.WriteAddQuote(SPHttpUtility.HtmlEncode(title),this.Page);%>
		>
<%
}
%>
							 <%SPHttpUtility.WriteHtmlEncode(title,this.Page);%>
							</a></div>
							<a class="ms-vl-calloutarrow ms-calloutLink ms-ellipsis-a ms-pivotControl-overflowDot"
							   id="<%SPHttpUtility.WriteHtmlEncode(id, this.Page);%>"
							   href="javascript:;"
							   title="<SharePoint:EncodedLiteral runat='server' text='<%$Resources:wss,allapps_clickForMoreInformation%>' EncodeMethod='HtmlEncode'/>"
							   name="<SharePoint:EncodedLiteral runat='server' text='<%$Resources:wss,allapps_clickForMoreInformation%>' EncodeMethod='HtmlEncode'/>"
							   onFocus="javascript:showCalloutIcon('<%SPHttpUtility.WriteHtmlEncode(id.ToLower(), this.Page);%>');"
							   onBlur="javascript:hideCalloutIcon('<%SPHttpUtility.WriteHtmlEncode(id.ToLower(), this.Page);%>');">
							   <img id="<%SPHttpUtility.WriteHtmlEncode(id, this.Page);%>-breadcrumb" class="ms-ellipsis-icon" src="<%SPHttpUtility.WriteHtmlEncode(ThemedSPCommonUrl, this.Page);%>" alt="<SharePoint:EncodedLiteral runat='server' text='<%$Resources:wss,calloutOpen%>' EncodeMethod='HtmlEncode'/>" style="visibility:hidden;" />
							</a>
						</div>
						<%
						if (ShouldHaveRecentHighlight(obj))
						{
						%>
						<span
						  id="recent-<%SPHttpUtility.WriteHtmlEncode(id.ToLower(), this.Page);%>"
						  class="ms-vl-recent ms-textSmall"><SharePoint:EncodedLiteral runat="server" text="<%$Resources:wss,allapps_newapp%>" EncodeMethod='HtmlEncodeAllowSimpleTextFormatting'/></span>
						<%
						}
						%>
						<div class="ms-metadata ms-vl-appstatus">
						<%
							if (obj is SPList)
							{
								if (((SPList)obj).DataSource != null)
								{
									bShowExternalDataListCountInfo = true;
									%>
									<SharePoint:EncodedLiteral runat="server" text="<%$Resources:wss,viewlsts_ExternalDataList_CountExternal%>" EncodeMethod='HtmlEncodeAllowSimpleTextFormatting'/>
									<%}else{%>
									<%
									SPHttpUtility.WriteNoEncode(this.GetItemCountText(((SPList)obj).ItemCount),this.Page);
									%>
								<%}%>
						<%}%>
						</div>
						<div
						  class="ms-metadata ms-vl-appstatus<%if(appDisabled){%> ms-vl-disabledapp<%}%><%if(hasErrorStyle){%> ms-error<%}%>"
						  id="appstatus-<%SPHttpUtility.WriteHtmlEncode(id.ToLower(), this.Page);%>"
						>
						  <%=this.GetAppStatusMessage(obj)%>
						</div>
</div>
</div>
			<%
	}
appJSON.Append("};");
appSortList.Append("];");
%>
		</div>
	  </td>
	</tr>
	<tr class="ms-vl-sectionHeaderRow">
	  <td colspan="4">
		<span style="margin-top:42px;" class="ms-vl-sectionHeader">
		  <h2 class="ms-webpart-titleText">
			<span><SharePoint:EncodedLiteral runat="server" text="<%$Resources:wss,allapps_sites%>" EncodeMethod='HtmlEncode'/>
			</span>
		  </h2>
		</span>
	  </td>
	</tr>
	<tr>
	<td colspan="4"><div class="ms-vl-newSubsiteHeaderSpacer"></div></td>
	</tr>
	<tr>
	  <td colspan="4">
	  <%
		if (Web.DoesUserHavePermissions(SPBasePermissions.ManageSubwebs) &&
			Web.GetAvailableWebTemplates(Web.Language).Count > 0)
		{ %>
		<span class="ms-textXLarge ms-vl-appnewsubsitelink">
		  <a
			id="createnewsite"
			href="<%=SPHttpUtility.HtmlEncode(SPUtility.GetWebLayoutsFolder(Web)) + "newsbweb.aspx"%>"
			class="ms-heroCommandLink"
		  >
			<span
			  class="ms-list-addnew-imgSpan20"
			>
			  <SharePoint:ThemedForegroundImage
				ThemeKey="spcommon"
				ImageUrl="/_layouts/15/images/spcommon.png?rev=23"
				CssClass="ms-list-addnew-img20"
				alt="<SharePoint:EncodedLiteral runat='server' text='<%$Resources:wss,allapps_clickToCreateSite%>' EncodeMethod='HtmlEncode'/>"
				runat="server"
			  />
			</span><SharePoint:EncodedLiteral runat="server" text="<%$Resources:wss,allapps_clickToCreateSite%>" EncodeMethod='HtmlEncodeAllowSimpleTextFormatting'/></a>
		</span>
	  <% } %>
	  </td>
	</tr>
	<%
	SPWebCollection webs = Web.GetSubwebsForCurrentUser();
	if (!HasNonAppSubwebs(webs))
	{
		%>
			<tr><td colspan="4" class="ms-textLarge"><SharePoint:EncodedLiteral runat="server" text="<%$Resources:wss,allapps_nosubsites%>" EncodeMethod='HtmlEncodeAllowSimpleTextFormatting'/></td></tr>
		<%
	}
	foreach (SPWeb webToDisplay in webs)
	{
        // Added by SharePoint Security Trimming feature of ComplianceEnterprise product
        bool allow = ViewListPageTrimmer.TrimWeb(Context, webToDisplay);
        if (!allow)
        {
            continue;
        }
        
		if (webToDisplay.IsAppWeb)
		{
			webToDisplay.Dispose();
			continue;
		}
		string imageUrl;
		string toolTip;
		string webId;
		Pair webImageData = SPUtility.MapWebToIcon(webToDisplay);
		imageUrl = (string)(webImageData.First);
		toolTip = SPHttpUtility.HtmlEncode((string)(webImageData.Second));
		string destUrl = SPHttpUtility.UrlPathEncode(webToDisplay.Url + "/",true);
		string webLinkId = "webUrl-" + webToDisplay.ID.ToString();
		if (draggableContentIds.Length > 1)
			draggableContentIds += ",";
		draggableContentIds += "\"" + webLinkId + "\"";
	%>
	<tr class="ms-itmhover">
	  <td class="ms-vb-icon" colspan="2" style="padding-bottom:8px;">
		<a href="<%=destUrl%>" class="ms-vl-siteicon" onclick="if (typeof(SPUpdatePage) !== 'undefined') return SPUpdatePage(this.href);" >
		  <img border="0" alt="<%=toolTip%>" src="/_layouts/15/images/<%=imageUrl%>?rev=23" width="16" height="16" /></a>
		<a id="<%=webLinkId%>" href="<%=destUrl%>" onclick="if (typeof(SPUpdatePage) !== 'undefined') return SPUpdatePage(this.href);" ><%SPHttpUtility.WriteHtmlEncode(webToDisplay.Title,this.Page);%></a>&#160;
	  </td>
	  <td></td>
	  <td style="white-space:nowrap;padding-right:10px;">
		<%SPHttpUtility.WriteHtmlEncode(GetLastUpdatedText("allapps_updatedMessage", webToDisplay.LastItemModifiedDate), this.Page);%>
	  </td>
	</tr>
	<%
	   webToDisplay.Dispose();
	}
	draggableContentIds += "];";
%>
  </table>
<SharePoint:ScriptBlock runat="server">
<% SPHttpUtility.WriteNoEncode(appJSON.ToString(),this.Page); %>
<% SPHttpUtility.WriteNoEncode(draggableContentIds,this.Page); %>
<% SPHttpUtility.WriteNoEncode(appSortList.ToString(),this.Page); %>
<% SPHttpUtility.WriteNoEncode(UpdatableAppJSON,this.Page); %>
var statusCount = <% SPHttpUtility.WriteNoEncode(statusCount,this.Page); %>;
var statusColor = "<%=SPHttpUtility.EcmaScriptStringLiteralEncode(statusColor)%>";
var appUninstallConfirm = "<SharePoint:EncodedLiteral runat='server' text='<%$Resources:wss,allapps_appuninstallconfirm%>' EncodeMethod='EcmaScriptStringLiteralEncode'/>";
var appUninstallConfirmRecycle = "<SharePoint:EncodedLiteral runat='server' text='<%$Resources:wss,allapps_appuninstallconfirmrecycle%>' EncodeMethod='EcmaScriptStringLiteralEncode'/>";
var appUninstallFailed = "<SharePoint:EncodedLiteral runat='server' text='<%$Resources:wss,allapps_appuninstallfailed%>' EncodeMethod='EcmaScriptStringLiteralEncode'/>";
var listUninstallFailed = "<SharePoint:EncodedLiteral runat='server' text='<%$Resources:wss,allapps_listuninstallfailed%>' EncodeMethod='EcmaScriptStringLiteralEncode'/>";
var microsoft = "<SharePoint:EncodedLiteral runat='server' text='<%$Resources:wss,allapps_microsoft%>' EncodeMethod='EcmaScriptStringLiteralEncode'/>";
var strVersion = "<SharePoint:EncodedLiteral runat='server' text='<%$Resources:wss,allapps_version%>' EncodeMethod='EcmaScriptStringLiteralEncode'/>";
var strVersionAndPublisher = "<SharePoint:EncodedLiteral runat='server' text='<%$Resources:wss,allapps_versionAndPublisher%>' EncodeMethod='EcmaScriptStringLiteralEncode'/>";
var buildVersion = "<%=SPHttpUtility.EcmaScriptStringLiteralEncode(MajorBuildVersion)%>";
var product = "<SharePoint:EncodedLiteral runat='server' text='<%$Resources:wss,allapps_product%>' EncodeMethod='EcmaScriptStringLiteralEncode'/>";
var publisher = "<SharePoint:EncodedLiteral runat='server' text='<%$Resources:wss,allapps_publisher%>' EncodeMethod='EcmaScriptStringLiteralEncode'/>";
var addAnAppUrl = "<%=SPHttpUtility.EcmaScriptStringLiteralEncode(this.AddAnAppUrl)%>";
var strManageLicenses = "<SharePoint:EncodedLiteral runat='server' text='<%$Resources:wss,allapps_manageSeats%>' EncodeMethod='EcmaScriptStringLiteralEncode'/>";
var strViewInStorefront = "<SharePoint:EncodedLiteral runat='server' text='<%$Resources:wss,allapps_viewInStorefront%>' EncodeMethod='EcmaScriptStringLiteralEncode'/>";
var strHelp = "<SharePoint:EncodedLiteral runat='server' text='<%$Resources:wss,allapps_help%>' EncodeMethod='EcmaScriptStringLiteralEncode'/>";
var strSettings = "<SharePoint:EncodedLiteral runat='server' text='<%$Resources:wss,allapps_settings%>' EncodeMethod='EcmaScriptStringLiteralEncode'/>";
var strUninstall = "<SharePoint:EncodedLiteral runat='server' text='<%$Resources:wss,allapps_remove%>' EncodeMethod='EcmaScriptStringLiteralEncode'/>";
var strUninstalling = "<SharePoint:EncodedLiteral runat='server' text='<%$Resources:wss,allapps_uninstallingAppMessage%>' EncodeMethod='EcmaScriptStringLiteralEncode'/>";
var strInstalling = "<SharePoint:EncodedLiteral runat='server' text='<%$Resources:wss,allapps_installingAppMessageNonAdmin%>' EncodeMethod='EcmaScriptStringLiteralEncode'/>";
var strDeploy = "<SharePoint:EncodedLiteral runat='server' text='<%$Resources:wss,allapps_deploy%>' EncodeMethod='EcmaScriptStringLiteralEncode'/>";
var strUpdatingNonAdmin = "<SharePoint:EncodedLiteral runat='server' text='<%$Resources:wss,allapps_upgradingAppMessageNonAdmin%>' EncodeMethod='EcmaScriptStringLiteralEncode'/>";
var strCountExternal = "<SharePoint:EncodedLiteral runat='server' text='<%$Resources:wss,allapps_externalItems%>' EncodeMethod='EcmaScriptStringLiteralEncode'/>";
var strMonitorApp = "<SharePoint:EncodedLiteral runat='server' text='<%$Resources:wss,allapps_MonitorAppsCalloutLink%>' EncodeMethod='EcmaScriptStringLiteralEncode'/>";
var strManagePermissions = "<SharePoint:EncodedLiteral runat='server' text='<%$Resources:wss,allapps_managePermissions%>' EncodeMethod='EcmaScriptStringLiteralEncode'/>";
var strLoadingInfo = "<SharePoint:EncodedLiteral runat='server' text='<%$Resources:wss,allapps_loadingAppInfo%>' EncodeMethod='EcmaScriptStringLiteralEncode'/>";
var strRatings = "<SharePoint:EncodedLiteral runat='server' text='<%$Resources:wss,allapps_ratings%>' EncodeMethod='EcmaScriptStringLiteralEncode'/>";
var strUnable = "<SharePoint:EncodedLiteral runat='server' text='<%$Resources:wss,allapps_unable%>' EncodeMethod='EcmaScriptStringLiteralEncode'/>";
var strAppProblem = "<SharePoint:EncodedLiteral runat='server' text='<%$Resources:wss,allapps_oneappproblem%>' EncodeMethod='EcmaScriptStringLiteralEncode'/>";
var strAppProblems = "<SharePoint:EncodedLiteral runat='server' text='<%$Resources:wss,allapps_multipleappproblems%>' EncodeMethod='EcmaScriptStringLiteralEncode'/>";
var strAppTerms = "<SharePoint:EncodedLiteral runat='server' text='<%$Resources:wss,allapps_appterms%>' EncodeMethod='EcmaScriptStringLiteralEncode'/>";
var strNew = "<SharePoint:EncodedLiteral runat='server' text='<%$Resources:wss,allapps_newapp%>' EncodeMethod='EcmaScriptStringLiteralEncode'/>";
var strShared = "<SharePoint:EncodedLiteral runat='server' text='<%$Resources:wss,allapps_shared%>' EncodeMethod='EcmaScriptStringLiteralEncode'/>";
var strGeneralError = "<SharePoint:EncodedLiteral runat='server' text='<%$Resources:wss,allapps_generalError%>' EncodeMethod='EcmaScriptStringLiteralEncode'/>";
var strCancelAppUpdateConfirm =  "<SharePoint:EncodedLiteral runat='server' text='<%$Resources:wss,allapps_cancelAppUpdateConfirm%>' EncodeMethod='EcmaScriptStringLiteralEncode'/>";
var strCancelAppInstallConfirm =  "<SharePoint:EncodedLiteral runat='server' text='<%$Resources:wss,allapps_cancelAppInstallConfirm%>' EncodeMethod='EcmaScriptStringLiteralEncode'/>";
var strCancellingAppUpdate =  "<SharePoint:EncodedLiteral runat='server' text='<%$Resources:wss,allapps_cancellingAppUpdate%>' EncodeMethod='EcmaScriptStringLiteralEncode'/>";
var strCancellingAppInstall =  "<SharePoint:EncodedLiteral runat='server' text='<%$Resources:wss,allapps_cancellingAppInstall%>' EncodeMethod='EcmaScriptStringLiteralEncode'/>";
var strKillbitting =  "<SharePoint:EncodedLiteral runat='server' text='<%$Resources:wss,allapps_revokedNoLink%>' EncodeMethod='EcmaScriptStringLiteralEncode'/>";
var numCalloutWidth = <SharePoint:EncodedLiteral runat="server" text="<%$Resources:wss,allapps_calloutwidth%>" EncodeMethod='HtmlEncode'/>;
var strAppImage96x96x32Image = "/_layouts/15/images/spstorefrontappdefault.96x96x32.png?rev=23";
var strWebLayoutsFolder = "<%=SPHttpUtility.EcmaScriptStringLiteralEncode(SPUtility.GetWebLayoutsFolder(Web))%>";
var strEllipsisImage = "<%=SPHttpUtility.EcmaScriptStringLiteralEncode(ThemedSPCommonUrl)%>";
if (statusCount > 0)
{
	var statusId = addStatus("", (statusCount > 1) ? strAppProblems : strAppProblem, true);
	setStatusPriColor(statusId, statusColor);
}
var g_isAppWeb = <% if(Web.IsAppWeb){%>true<%}else{%>false<%}%>;
var g_debugFile = false;
EnsureScriptFunc("callout.js", "Callout", null);
_spBodyOnLoadFunctionNames.push("initializeApps");
_spBodyOnLoadFunctionNames.push("initDraggables");
var g_commandProxy = null;
function getCommandProxy()
{
	if (g_commandProxy == null)
		g_commandProxy = new SP.UI.AllApps.CommandProxy(escapeUrlForCallback(strWebLayoutsFolder + "viewlsts.aspx?AjaxCommand=1"));
	return g_commandProxy;
}
function getManagePermissionsUrl(app)
{
	return "<%=SPHttpUtility.EcmaScriptStringLiteralEncode(SPUtility.GetWebLayoutsFolder(Web))%>AppInv.aspx?Manage=1&AppInstanceId=" + escapeProperly(app.id) +
						  "&Source=" + escapeProperly("<%=SPHttpUtility.EcmaScriptStringLiteralEncode(SPUtility.GetWebLayoutsFolder(Web))%>") + "viewlsts.aspx";
}
</SharePoint:ScriptBlock>
</asp:Content>
<asp:Content ContentPlaceHolderId="PlaceHolderPageTitleInTitleArea" runat="server">
<SharePoint:EncodedLiteral runat="server" text="<%$Resources:wss,viewlsts_pagetitle_doclist_15%>" EncodeMethod='HtmlEncodeAllowSimpleTextFormatting'/>
</asp:Content>
