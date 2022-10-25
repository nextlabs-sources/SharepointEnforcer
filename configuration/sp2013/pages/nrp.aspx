<%@ Page language="C#"  AutoEventWireup="true"  Inherits="NextLabs.SPEnforcer.UserDriveRightProtectPage,NextLabs.SPEnforcer,Version=3.0.0.0,Culture=neutral,PublicKeyToken=5ef8e9c15bdfa43e" %>
<%@ Register Tagprefix="SharePoint" Namespace="Microsoft.SharePoint.WebControls" Assembly="Microsoft.SharePoint, Version=15.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %> <%@ Register Tagprefix="WebPartPages" Namespace="Microsoft.SharePoint.WebPartPages" Assembly="Microsoft.SharePoint, Version=15.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %> <%@ Register Tagprefix="OSRVWC" Namespace="Microsoft.Office.Server.WebControls" Assembly="Microsoft.Office.Server, Version=15.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %> <%@ Register Tagprefix="SPSWC" Namespace="Microsoft.SharePoint.Portal.WebControls" Assembly="Microsoft.SharePoint.Portal, Version=15.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %> <%@ Register Tagprefix="SEARCHWC" Namespace="Microsoft.Office.Server.Search.WebControls" Assembly="Microsoft.Office.Server.Search, Version=15.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %> <%@ Register Tagprefix="PublishingWebControls" Namespace="Microsoft.SharePoint.Publishing.WebControls" Assembly="Microsoft.SharePoint.Publishing, Version=15.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %>



<HTML dir="<%$Resources:wss, multipages_direction_dir_value%>" Runat="server">
    <HEAD>
        <style>
            *{margin:0; padding:0;}
            html{width:650px; }
            body{
                font:1em "Segoe UI Semilight","Segoe UI","Segoe",Tahoma,Helvetica,Arial,sans-serif;
                color: #777;
            }
            .workingOnImgHidden{
                visibility:hidden;
                display:none;
                width:24px; margin:88px auto; 
            }
       
            .workingOnImgDisplay{ display:block; width:24px; margin:88px auto; }
            #UpdatePanel1{ padding-left:10px;overflow:hidden;overflow-wrap:break-word;}
            .resultmessagedisplay{  display:block;  overflow-x: hidden;overflow-y: auto; 
                                      font: 13px "Segoe UI Semilight","Segoe UI","Segoe",Tahoma,Helvetica,Arial,sans-serif; 
                                      color: #666;  width:100%;  word-break: break-all;}
            .resultmessagehide{font: 1em "Segoe UI Semilight","Segoe UI","Segoe",Tahoma,Helvetica,Arial,sans-serif; color: #777;  width:100%;height:100%; display:none;}

        </style>
        <script type="text/javascript">
            window.onload = function () {        
                document.getElementById("RMSButton").click();
            }
        </script>
    </HEAD>   

    <script language="javascript">
 
    </script>

    <body>
        <form id="form1" runat="server">
            <asp:ScriptManager ID="ScriptManager1" runat="server">
            </asp:ScriptManager>
            <asp:UpdatePanel ID="UpdatePanel1" runat="server" ChildrenAsTriggers="True" >
                
                <ContentTemplate>
                    <asp:Button ID="RMSButton"  style="visibility:hidden;display:none;" Text="" runat="server" OnClick="btnRMS_Click" />
                    <asp:Label id="ResultMessage" runat="server"  CssClass="resultmessagehide" />
                    <SPSWC:ImageLoc runat="server" ImageUrl="/_layouts/15/images/hig_progcircle_loading24.gif" ID="WorkingOnImg" CssClass="workingOnImgDisplay" />
                </ContentTemplate>

            </asp:UpdatePanel>

        </form>
    </body>
</HTML>

