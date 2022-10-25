<%@ Assembly Name="Microsoft.SharePoint, Version=14.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %>

<%@ Page Language="C#" DynamicMasterPageFile="~masterurl/default.master" Inherits="NextLabs.SPEnforcer.CALogViewerPage,NextLabs.SPEnforcer,Version=3.0.0.0,Culture=neutral,PublicKeyToken=5ef8e9c15bdfa43e" %>

<%@ Import Namespace="Microsoft.SharePoint" %>
<%@ Import Namespace="System.Data" %>
<%@ Import Namespace="Microsoft.SharePoint.ApplicationPages" %> 
<%@ Import Namespace="Microsoft.SharePoint.Utilities" %>

<%@ Register TagPrefix="SharePoint" Namespace="Microsoft.SharePoint.WebControls"
  Assembly="Microsoft.SharePoint, Version=14.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %>

<asp:Content ID="Content6" ContentPlaceHolderID="PlaceHolderMain" runat="server">
  <asp:Button ID="cmdRefreshPage" runat="server" Text="Refresh Page" OnClick="cmdRefreshPage_Click" />
  <asp:Button ID="cmdDeleteAllEntires" runat="server" Text="Clear All" OnClick="cmdDeleteAllEntires_Click" />
  <asp:Button ID="cmdCancel" runat="server" Text="OK" OnClick="cmdOK_Click" />
  <br /><br />
  <font color="red">
    <asp:Label ID="lblInfo" runat="server" Text="&nbsp;&nbsp;Clearing the Information Control log will clear log information for all document libraries and lists within the site collection." />
  </font>
  <hr />
  <SharePoint:SPGridView ID="SPGridView1" runat="server" AutoGenerateColumns="False"
    Width="100%">
    <AlternatingRowStyle CssClass="ms-alternating" />
  </SharePoint:SPGridView>
</asp:Content>
<asp:Content ID="Content1" ContentPlaceHolderID="PlaceHolderPageTitle" runat="server">
  Information Control Log Viewer
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="PlaceHolderPageTitleInTitleArea" runat="server">
  <a id=onetidListHlink HREF=<%SPHttpUtility.AddQuote(SPHttpUtility.UrlPathEncode(SPContext.Current.List.DefaultViewUrl,true),Response.Output);%>><%SPHttpUtility.HtmlEncode(SPContext.Current.List.Title,Response.Output);%></a>&#32;<SharePoint:ClusteredDirectionalSeparatorArrow ID="ClusteredDirectionalSeparatorArrow1" runat="server" /> <a HREF=<%SPHttpUtility.AddQuote(SPHttpUtility.UrlPathEncode("listedit.aspx?List=" + SPContext.Current.List.ID.ToString(),true),Response.Output);%>> <SharePoint:FormattedStringWithListType ID="FormattedStringWithListType1" runat="server" String="<%$Resources:wss,listsettings_titleintitlearea%>" LowerCase="false" /></a>&#32;<SharePoint:ClusteredDirectionalSeparatorArrow ID="ClusteredDirectionalSeparatorArrow2" runat="server" />
  <SharePoint:EncodedLiteral runat="server" Text="Information Control Log Viewer" EncodeMethod="HtmlEncode" />
</asp:Content>
