<%@ Assembly Name="Microsoft.SharePoint, Version=16.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %>
<%@ Page Language="C#" Inherits="NextLabs.Deployment.StatusReportPage, NextLabs.Deployment,Version=1.0.0.0,Culture=neutral,PublicKeyToken=e03e4c7ee29d89ce" DynamicMasterPageFile="~masterurl/default.master"%>
<%@ Import Namespace="Microsoft.SharePoint" %>
<%@ Import Namespace="System.Data" %>
<%@ Import Namespace="Microsoft.SharePoint.ApplicationPages" %> 
<%@ Import Namespace="Microsoft.SharePoint.Utilities" %>

<%@ Register TagPrefix="SharePoint" Namespace="Microsoft.SharePoint.WebControls" Assembly="Microsoft.SharePoint, Version=16.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %>

<asp:Content ID="Content6" ContentPlaceHolderID="PlaceHolderMain" runat="server">
  <asp:Button ID="cmdDeleteAllEntires" runat="server" Visible="false" Text="Clear All" OnClick="cmdDeleteAllEntires_Click" />
  <br /><br />
  <font color="red">
    <asp:Label ID="lblInfo" runat="server" Visible="false" Text="Because SPE's status are queried by Timer Jobs it may has some delay. So After click Query button, please wait for a minute then click Refresh Page button." />
  </font>
  <hr />

  <SharePoint:SPGridView ID="SPGridView1" runat="server" AutoGenerateColumns="false" Width="100%">
    <AlternatingRowStyle CssClass="ms-alternating" />
  </SharePoint:SPGridView>

</asp:Content>
<asp:Content ID="Content1" ContentPlaceHolderID="PlaceHolderPageTitle" runat="server">
  NextLabs Entitlement Manager ?View Status
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="PlaceHolderPageTitleInTitleArea" runat="server">
  <SharePoint:EncodedLiteral ID="EncodedLiteral1" runat="server" Text="NextLabs Entitlement Manager - View Status" EncodeMethod="HtmlEncode" />
</asp:Content>
