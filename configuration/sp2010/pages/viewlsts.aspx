<%@ Assembly Name="Microsoft.SharePoint.ApplicationPages, Version=14.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c"%> <%@ Page Language="C#" DynamicMasterPageFile="~masterurl/default.master" Inherits="Microsoft.SharePoint.ApplicationPages.ViewListsPage"   EnableViewState="false" EnableViewStateMac="false"    %> <%@ Import Namespace="Microsoft.SharePoint.ApplicationPages" %> <%@ Register Tagprefix="SharePoint" Namespace="Microsoft.SharePoint.WebControls" Assembly="Microsoft.SharePoint, Version=14.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %> <%@ Register Tagprefix="Utilities" Namespace="Microsoft.SharePoint.Utilities" Assembly="Microsoft.SharePoint, Version=14.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %> <%@ Import Namespace="Microsoft.SharePoint" %> <%@ Assembly Name="Microsoft.Web.CommandUI, Version=14.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %>
<%@ Register Tagprefix="SPSecurityTrimming" Namespace="Nextlabs.SPSecurityTrimming" Assembly="Nextlabs.SPSecurityTrimming, Version=3.0.0.0, Culture=neutral, PublicKeyToken=7030e9011c5eb860" %>
<%@ Register TagPrefix="wssuc" TagName="TopNavBar" src="~/_controltemplates/TopNavBar.ascx" %>
<%@ Register TagPrefix="wssuc" TagName="ToolBar" src="~/_controltemplates/ToolBar.ascx" %>
<%@ Register TagPrefix="wssuc" TagName="ToolBarButton" src="~/_controltemplates/ToolBarButton.ascx" %>
<%@ Register TagPrefix="wssuc" TagName="ViewHeader" src="~/_controltemplates/ViewHeader.ascx" %>
<%@ Register Tagprefix="wssawc" Namespace="Microsoft.SharePoint.WebControls" Assembly="Microsoft.SharePoint, Version=14.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %> <%@ Register Tagprefix="SharePoint" Namespace="Microsoft.SharePoint.WebControls" Assembly="Microsoft.SharePoint, Version=14.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %>
<asp:Content ContentPlaceHolderId="PlaceHolderPageTitle" runat="server">
<SharePoint:EncodedLiteral runat="server" text="<%$Resources:wss,viewlsts_pagetitle_doclist%>" EncodeMethod='HtmlEncode'/>
</asp:Content>
<asp:Content ContentPlaceHolderId="PlaceHolderPageTitleInTitleArea" runat="server">
<SharePoint:EncodedLiteral runat="server" text="<%$Resources:wss,viewlsts_pagetitle_doclist%>" EncodeMethod='HtmlEncode'/>
</asp:Content>
<asp:Content ContentPlaceHolderId="PlaceHolderPageImage" runat="server"><SharePoint:AlphaImage src="/_layouts/images/allcontents.png" Height="54" Width="145" Alt="" runat="server"/></asp:Content>
<asp:Content ContentPlaceHolderId="PlaceHolderAdditionalPageHead" runat="server">
	<script>
// <![CDATA[
		var navBarHelpOverrideKey = "WSSEndUser_ListOLists";
// ]]>
	</script>
</asp:Content>
<asp:Content ContentPlaceHolderId="PlaceHolderPageDescription" runat="server">
	<asp:Label id="LabelPageDescription" runat="server"/>
</asp:Content>
<asp:Content ContentPlaceHolderId ="PlaceHolderTitleLeftBorder" runat="server">
 <div class="ms-titleareaframe"><img src="/_layouts/images/blank.gif" width='1' height='100%' alt="" /></div>
</asp:Content>
<asp:Content ContentPlaceHolderId ="PlaceHolderTitleRightMargin" runat="server">
 <div style="height:100%;" class="ms-titleareaframe"><img src="/_layouts/images/blank.gif" width='1' height='1' alt="" /></div>
</asp:Content>
<asp:Content ContentPlaceHolderId ="PlaceHolderBodyLeftBorder" runat="server">
 <div style="height:100%;" class="ms-pagemargin"><img src="/_layouts/images/blank.gif" width='10' height='1' alt="" /></div>
</asp:Content>
<asp:Content ContentPlaceHolderId ="PlaceHolderBodyRightMargin" runat="server">
 <div style="height:100%;" class="ms-pagemargin"><img src="/_layouts/images/blank.gif" width='10' height='1' alt="" /></div>
</asp:Content>
<asp:Content contentplaceholderid="PlaceHolderTopNavBar" runat="server">
	<wssuc:TopNavBar id="IdTopNavBar" runat="server" Version="4" ShouldUseExtra="true"/>
</asp:Content>
<asp:Content contentplaceholderid="PlaceHolderHorizontalNav" runat="server"/>
<asp:Content contentplaceholderid="PlaceHolderTitleBreadcrumb" runat="server">
	<SharePoint:UIVersionedContent UIVersion="3" runat="server"><ContentTemplate>
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
	<SharePoint:UIVersionedContent UIVersion="4" runat="server"><ContentTemplate>
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
<asp:Content ContentPlaceHolderId="PlaceHolderMain" runat="server">
	<wssuc:ToolBar id="ToolBar" CssClass="ms-menutoolbar" ButtonSeparator="<img src='/_layouts/images/blank.gif' />" runat="server" FocusOnToolBar = true>
		<Template_Buttons>
			<SharePoint:SPLinkButton runat="server" id="diidIONewList" AccessKey="<%$Resources:wss,tb_new_ak%>"  ImageUrl="/_layouts/images/createcontent.gif" ShowImageAndText="true" HoverCellActiveCssClass="ms-buttonactivehover" HoverCellInActiveCssClass="ms-buttoninactivehover" />
			<SharePoint:ClusteredSPLinkButton
				runat="server"
				id="diidIOSiteWorkflows"
				ShowImageAndText="true"
				CssClass="ms-wkflwbtn"
				AccessKey="<%$Resources:wss,viewlsts_SiteWorkflow_ak%>"
				ImageUrl="/_layouts/images/fgimg.png"
				ImageWidth=16
				ImageHeight=16
				OffsetX=0
				OffsetY=642
				Text="<%$Resources:wss,siteactions_siteworkflow%>"
				NavigateUrl="~site/_layouts/workflow.aspx"
				PermissionsString="EditListItems, AddAndCustomizePages"
				PermissionMode="Any"
				HoverCellActiveCssClass="ms-buttonactivehover"
				HoverCellInActiveCssClass="ms-buttoninactivehover" />
		</Template_Buttons>
		<Template_RightButtons>
			<SharePoint:TemplateBasedControl TemplateName="AllContentViewSelector" runat="server"/>
		</Template_RightButtons>
	</wssuc:ToolBar>
	<table cellpadding="1" style="border-collapse: collapse;" cellspacing="0" border="0" width="100%" class="ms-viewlsts">
		<SharePoint:UIVersionedContent UIVersion="3" runat="server">
			<ContentTemplate>
				<tr>
					<th scope="col" class="ms-vh2-nofilter" nowrap>&#160;</th>
					<th scope="col" class="ms-vh2-nofilter" nowrap><SharePoint:EncodedLiteral runat="server" text="<%$Resources:wss,viewlsts_title%>" EncodeMethod='HtmlEncode'/></th>
					<th scope="col" class="ms-vh2-nofilter" nowrap width="40%"><SharePoint:EncodedLiteral runat="server" text="<%$Resources:wss,viewlsts_desc%>" EncodeMethod='HtmlEncode'/></th>
					<th scope="col" class="ms-vh2-nofilter" style="text-align: right;" nowrap width="3%"><SharePoint:EncodedLiteral runat="server" text="<%$Resources:wss,viewlsts_items%>" EncodeMethod='HtmlEncode'/></th>
					<th scope="col" class="ms-vh2-nofilter" nowrap width="25%"><SharePoint:EncodedLiteral runat="server" text="<%$Resources:wss,viewlsts_lastmodified%>" EncodeMethod='HtmlEncode'/></th>
				</tr>
			</ContentTemplate>
		</SharePoint:UIVersionedContent>
		<SharePoint:UIVersionedContent UIVersion="4" runat="server">
			<ContentTemplate>
				<tr class="ms-vh2-nobg">
					<th scope="col" class="ms-vh2-nofilter" style="white-space:nowrap;">&#160;</th>
					<th scope="col" class="ms-vh2-nofilter" style="white-space:nowrap;" title="<SharePoint:EncodedLiteral runat='server' text='<%$Resources:wss,viewlsts_title%>' EncodeMethod='HtmlEncode'/>">&#160;</th>
					<th scope="col" class="ms-vh2-nofilter" style="white-space:nowrap; width:40%;" title="<SharePoint:EncodedLiteral runat='server' text='<%$Resources:wss,viewlsts_desc%>' EncodeMethod='HtmlEncode'/>">&#160;</th>
					<th scope="col" class="ms-vh2-nofilter" style="text-align: right; white-space:nowrap; width:3%;"><SharePoint:EncodedLiteral runat="server" text="<%$Resources:wss,viewlsts_items%>" EncodeMethod='HtmlEncode'/></th>
					<th scope="col" class="ms-vh2-nofilter" style="white-space:nowrap; width:25%;"><SharePoint:EncodedLiteral runat="server" text="<%$Resources:wss,viewlsts_lastmodified%>" EncodeMethod='HtmlEncode'/></th>
				</tr>
			</ContentTemplate>
		</SharePoint:UIVersionedContent>
<%
DateTime dtCurrent = DateTime.UtcNow;
string alternatingClass = (null != SPContext.Current.Web && SPContext.Current.Web.UIVersion < 4) ? "ms-alternating" : "ms-alternatingstrong";
if (!bShowSites)
{
	System.Collections.IEnumerator myRgs = rgRgs.GetEnumerator();
	int j = 0;
	while (myRgs.MoveNext())
	{
			SPBaseType currBaseType = (SPBaseType)myRgs.Current;
			myRgs.MoveNext();
			SPListTemplateType currListTemplate = (SPListTemplateType)myRgs.Current;
			myRgs.MoveNext();
					String strBaseType=(String)myRgs.Current;
			myRgs.MoveNext();
					String strNoBaseType=(String)myRgs.Current;
			myRgs.MoveNext();
			myRgs.MoveNext();
			ArrayList currList = (ArrayList)myRgs.Current;
			System.Collections.IEnumerator currRg = currList.GetEnumerator();
			if (!bBaseTypeInited ||
			   (spBaseType == currBaseType && spListTemplate == currListTemplate))
			{
				if (j == 0)
				{
	%>
					<tr>
						<td class="ms-gb" colspan="5">
						   <h3 class="ms-standardheader">
							  &#160; <%SPHttpUtility.HtmlEncode(strBaseType,Response.Output);%>
						   </h3>
						</td>
					</tr>
	<%
						j = 1;
				}
				else
				{
	%>
					<tr>
						<td class="ms-gb"  colspan="5" style="white-space:nowrap;">
						   <h3 class="ms-standardheader">
							  &#160; <%SPHttpUtility.HtmlEncode(strBaseType,Response.Output);%>
						   </h3>
						</td>
					</tr>
	<%
				}
	%>
	<%
				if (currList.Count == 0)
				{
	%>
					<tr><td class="ms-vb2 ms-viewlsts-noitems" colspan="6">
						<%SPHttpUtility.NoEncode(strNoBaseType,Response.Output);%>
					</td></tr>
	<%
				}
	}
	%>
	<%
			string rowClass = alternatingClass;
			while (currRg.MoveNext())
			{
				int iList = (int)currRg.Current;
				SPList spList = (iList>=spLists.Count )? spListsIssue[iList - spLists.Count ]  : spLists[iList];
				if (spList.Hidden)
				{
					continue;
				}

                // Added by SharePoint Security Trimming feature of ComplianceEnterprise product
                bool allow = ViewListPageTrimmer.TrimList(Context, spList);
                if (!allow)
                {
                    continue;
                }
	%>
					<tr class="<%=rowClass%>">
						<td class="ms-vb-icon">
						<%
							string listViewUrl;
							try
							{
								if (spList.BaseTemplate == SPListTemplateType.WebPageLibrary)
								{
									listViewUrl = GetRootFolderOfList(spList);
								}
								else
								{
									listViewUrl = spList.DefaultViewUrl;
								}
							}
							catch
							{
								listViewUrl = "";
							}
							if (listViewUrl == "")
								listViewUrl = "ListEdit.aspx?List=" + spList.ID.ToString("B").ToUpper();
						%>
							  <a id=<%SPHttpUtility.AddQuote(SPHttpUtility.HtmlEncode("viewlist" + spList.BaseTemplate.ToString()),Response.Output);%> href=<%SPHttpUtility.AddQuote(SPHttpUtility.UrlPathEncode(listViewUrl,true),Response.Output);%> >
							  <img border="0" alt=<%SPHttpUtility.AddQuote(SPHttpUtility.HtmlEncode(spList.Title),Response.Output);%> src=<%SPHttpUtility.AddQuote(SPHttpUtility.UrlPathEncode(spList.ImageUrl,true),Response.Output);%> width="16" height="16" /></a>
						</td>
						<td class="ms-vb2" >
							  <a id=<%SPHttpUtility.AddQuote(SPHttpUtility.HtmlEncode("viewlist" + spList.BaseTemplate.ToString()),Response.Output);%> href=<%SPHttpUtility.AddQuote(SPHttpUtility.UrlPathEncode(listViewUrl,true),Response.Output);%>><%SPHttpUtility.HtmlEncode(spList.Title,Response.Output);%></a>&#160;
						</td>
						<%
							string listDescription;
							listDescription = ListPageBase.RenderListDescription(Web, spList);
						%>
						<td class="ms-vb2" width="40%" >
							  <%SPHttpUtility.NoEncode(listDescription,Response.Output);%>&#160;
						</td>
						<td class="ms-vb2" width="3%" align="<SharePoint:EncodedLiteral runat='server' text='<%$Resources:wss,viewlsts_align%>' EncodeMethod='HtmlEncode'/>">
						<%
							if (spList.DataSource != null)
							{
								bShowExternalDataListCountInfo = true;
								%>
								<SharePoint:EncodedLiteral runat="server" text="<%$Resources:wss,viewlsts_ExternalDataList_CountExternal%>" EncodeMethod='HtmlEncodeAllowSimpleTextFormatting'/>
								<%
							}
							else
							{
								SPHttpUtility.NoEncode(spList.ItemCount,Response.Output);
							}
						%>
						</td>
						<td class="ms-vb2" width="25%" >
							<nobr>
						   <%
						   SPHttpUtility.HtmlEncode(SPUtility.TimeDeltaAsString(spList.LastItemModifiedDate, dtCurrent),Response.Output);
						   %>
							</nobr>
						</td>
					</tr>
	<%
				rowClass = (rowClass == "")? alternatingClass : "";
					}
	}
}
if (!bBaseTypeInited || bShowSites)
{
	%>
	<tr>
		<td class="ms-gb" colspan="5" style="white-space:nowrap;">
						   <h3 class="ms-standardheader">
							  &#160; <SharePoint:EncodedLiteral runat="server" text="<%$Resources:wss,viewlsts_sitesandworkspaces_title%>" EncodeMethod='HtmlEncode'/>
						   </h3>
		</td>
	</tr>
	<%
	SPWebCollection webs = Web.GetSubwebsForCurrentUser();
	if (webs.Count==0)
	{
		%>
			<tr><td class="ms-vb2 ms-viewlsts-noitems" colspan="6">
		<% if(Web.DoesUserHavePermissions(SPBasePermissions.ManageSubwebs)) { %>
				<SharePoint:EncodedLiteral runat="server" text="<%$Resources:wss,viewlsts_L_szNoSites_Text%>" EncodeMethod='HtmlEncodeAllowSimpleTextFormatting'/>
			</td></tr>
		<% } else { %>
				<SharePoint:EncodedLiteral runat="server" text="<%$Resources:wss,viewlsts_L_szNoSites1_Text%>" EncodeMethod='HtmlEncodeAllowSimpleTextFormatting'/>
			</td></tr>
		<% }
	}
	string rowClass = alternatingClass;
	foreach (SPWeb webToDisplay in webs)
	{
        // Added by SharePoint Security Trimming feature of ComplianceEnterprise product
        bool allow = ViewListPageTrimmer.TrimWeb(Context, webToDisplay);
        if (!allow)
        {
            continue;
        }
        
		string imageUrl;
		string toolTip;
		Pair webImageData = SPUtility.MapWebToIcon(webToDisplay);
		imageUrl = (string)(webImageData.First);
		toolTip = SPHttpUtility.HtmlEncode((string)(webImageData.Second));
		string destUrl = SPHttpUtility.UrlPathEncode(webToDisplay.Url + "/",true);
	%>
			 <tr class="<%=rowClass%>">
				<td class="ms-vb-icon" >
					  <a id="webIcon" href="<%=destUrl%>"  >
					  <img border="0" alt="<%=toolTip%>" src="<%=("/_layouts/images/"+imageUrl)%>" width="16" height="16" /></a>
				</td>
				<td class="ms-vb2" >
					  <a id="webUrl" href="<%=destUrl%>"><%SPHttpUtility.HtmlEncode(webToDisplay.Title,Response.Output);%></a>&#160;
				</td>
				<td class="ms-vb2" width="40%" >
					  <%SPHttpUtility.HtmlEncode(webToDisplay.Description,Response.Output);%>
				</td>
				<td class="ms-vb2" width="3%"></td>
				<td class="ms-vb2" width="25%" >
					<nobr>
				   <%=SPUtility.TimeDeltaAsString(webToDisplay.LastItemModifiedDate, dtCurrent)%>
					</nobr>
				</td>
			</tr>
	<%
	rowClass = (rowClass == "")? alternatingClass : "";
	}
}
if (bShowRecycleBin)
{
%>
		<tr>
			<td class="ms-gb"  colspan="5" style="white-space:nowrap;">
						   <h3 class="ms-standardheader">
							  &#160; <SharePoint:EncodedLiteral runat="server" text="<%$Resources:wss,viewlsts_recyclebin%>" EncodeMethod='HtmlEncode'/>
						   </h3>
			</td>
		</tr>
		<tr>
			<td class="ms-vb-icon" >
				  <a id="viewlistRecycleBin" href="RecycleBin.aspx" >
				  <img border="0" alt="<SharePoint:EncodedLiteral runat='server' text='<%$Resources:wss,viewlsts_recyclebin%>' EncodeMethod='HtmlEncode'/>" src="/_layouts/images/recycbin.gif" width="16" height="16" /></a>
			</td>
			<td class="ms-vb2" >
				  <a id="viewlistRecycleBin" href="RecycleBin.aspx"><SharePoint:EncodedLiteral runat="server" text="<%$Resources:wss,viewlsts_recyclebin%>" EncodeMethod='HtmlEncode'/></a>&#160;
			</td>
			<td class="ms-vb2" width="40%" >
				  <SharePoint:EncodedLiteral runat="server" text="<%$Resources:wss,viewlsts_recyclebin_description%>" EncodeMethod='HtmlEncode'/>
			</td>
			<td class="ms-vb2" width="3%" align="right">
				  <%
				  SPHttpUtility.NoEncode(Convert.ToString(RecycleBinItemCount),Response.Output);
				  %>
			</td>
			<td class="ms-vb2" width="25%" >
				<nobr>
			   &#160;
				</nobr>
			</td>
		</tr>
<%
}
%>
	</table>
	<div class="ms-vb2">
<%
if (bShowExternalDataListCountInfo)
{
%>
		<SharePoint:EncodedLiteral runat="server" text="<%$Resources:wss,viewlsts_ExternalDataList_CountNotAvailable%>" EncodeMethod='HtmlEncodeAllowSimpleTextFormatting'/>
<%
}
%>
	</div>
</asp:Content>
