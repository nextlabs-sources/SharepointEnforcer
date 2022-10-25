<%@ Assembly Name="Microsoft.SharePoint.ApplicationPages, Version=14.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c"%>

<%@ Page language="C#" DynamicMasterPageFile="~masterurl/default.master" Inherits="NextLabs.SPEnforcer.UserDriveRightProtectPage,NextLabs.SPEnforcer,Version=3.0.0.0,Culture=neutral,PublicKeyToken=5ef8e9c15bdfa43e" %>

<%@ Import Namespace="Microsoft.SharePoint" %> 
<%@ Import Namespace="Microsoft.SharePoint.ApplicationPages" %> 
<%@ Import Namespace="Microsoft.SharePoint.Utilities" %>
<%@ Import Namespace="Microsoft.SharePoint.Administration" %>

<%@ Register Tagprefix="SharePoint" Namespace="Microsoft.SharePoint.WebControls" Assembly="Microsoft.SharePoint, Version=14.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %>
<%@ Register Tagprefix="SPSWC" Namespace="Microsoft.SharePoint.Portal.WebControls" Assembly="Microsoft.SharePoint.Portal, Version=14.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %>

<asp:Content contentplaceholderid="PlaceHolderPageTitle" runat="server">
<SharePoint:EncodedLiteral runat="server" Text="Nextlabs Rights Protection" EncodeMethod="HtmlEncode" />
</asp:Content>

<asp:Content contentplaceholderid="PlaceHolderPageTitleInTitleArea" runat="server">
<a id=onetidListHlink HREF=<%SPHttpUtility.AddQuote(SPHttpUtility.UrlPathEncode(SPContext.Current.List.DefaultViewUrl,true),Response.Output);%>><%SPHttpUtility.HtmlEncode(SPContext.Current.List.Title,Response.Output);%></a>&#32;
<SharePoint:ClusteredDirectionalSeparatorArrow ID="ClusteredDirectionalSeparatorArrow2" runat="server" />
<SharePoint:EncodedLiteral runat="server" Text="Nextlabs Rights Protection" EncodeMethod="HtmlEncode" />
</asp:Content>

<asp:Content contentplaceholderid="PlaceHolderMain" runat="server">
    <SharePoint:EncodedLiteral runat="server" id="ResultMessage" EncodeMethod='HtmlEncode'/>
</asp:Content>