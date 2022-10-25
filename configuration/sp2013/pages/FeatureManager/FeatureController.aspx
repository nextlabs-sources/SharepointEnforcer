<%@ Assembly Name="NextLabs.Deployment, Version=1.0.0.0, Culture=neutral, PublicKeyToken=e03e4c7ee29d89ce" %>
<%@ Page Language="C#" AutoEventWireup="true" Inherits="NextLabs.Deployment.FeatureController" MasterPageFile="~/_layouts/application.master" %>
<%@ Assembly Name="Microsoft.SharePoint.ApplicationPages, Version=15.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c"%>
<%@ Import Namespace="Microsoft.SharePoint.ApplicationPages" %>
<%@ Register Tagprefix="SharePoint" Namespace="Microsoft.SharePoint.WebControls" Assembly="Microsoft.SharePoint, Version=15.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %> 
<%@ Register Tagprefix="Utilities" Namespace="Microsoft.SharePoint.Utilities" Assembly="Microsoft.SharePoint, Version=15.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %> 
<%@ Import Namespace="Microsoft.SharePoint" %>

<%@ Assembly Name="Microsoft.SharePoint.Publishing, Version=15.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %>
<%@ Assembly Name="Microsoft.Web.CommandUI, Version=15.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %> 
<%@ Register Tagprefix="wssawc" Namespace="Microsoft.SharePoint.WebControls" Assembly="Microsoft.SharePoint, Version=15.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %> 
<%@ Register Tagprefix="SharePoint" Namespace="Microsoft.SharePoint.WebControls" Assembly="Microsoft.SharePoint, Version=15.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %> 

<%@ Register TagPrefix="wssuc" TagName="InputFormSection" src="~/_controltemplates/15/InputFormSection.ascx" %>
<%@ Register TagPrefix="wssuc" TagName="InputFormControl" src="~/_controltemplates/15/InputFormControl.ascx" %>
<%@ Register TagPrefix="wssuc" TagName="ButtonSection" src="~/_controltemplates/15/ButtonSection.ascx" %>
<%@ Register TagPrefix="wssuc" TagName="TemplatePickerControl" src="~/_controltemplates/15/TemplatePickerControl.ascx" %>
<%@ Register Tagprefix="wssawc" Namespace="Microsoft.SharePoint.WebControls" Assembly="Microsoft.SharePoint, Version=15.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %> 


<asp:Content ContentPlaceHolderID="PlaceHolderPageTitle" runat="server">
    <SharePoint:EncodedLiteral runat="server" Text="NextLabs Entitlement Management - Enable or disable policy enforcement " EncodeMethod='HtmlEncode' />
</asp:Content>
<asp:Content ContentPlaceHolderID="PlaceHolderPageTitleInTitleArea" runat="server">
    <SharePoint:EncodedLiteral runat="server" Text="NextLabs Entitlement Management - Enable or disable policy enforcement " EncodeMethod='HtmlEncode' />
</asp:Content>
<asp:Content ContentPlaceHolderID="PlaceHolderPageDescription" runat="server">
    <SharePoint:EncodedLiteral runat="server" ID="PageDescription" Text="NextLabs Entitlement Management - Enable or disable policy enforcement "
        EncodeMethod='HtmlEncode' />
</asp:Content>
<asp:Content ContentPlaceHolderID="PlaceHolderAdditionalPageHead" runat="server">
    <script type="text/javascript" src="TreeUtils.js"></script>
    <script type="text/javascript" src="UITools.js"></script>
    <script type="text/javascript">
        var FeatureTreeClientID = "<%=FeatureTree.ClientID%>";
        function UpdateButtonClick() {
            if (document.getElementById("ProgressBar").height == "0") {
                document.getElementById("ProgressBar").height = "100";
            }
        }
    </script>
</asp:Content>
<asp:Content ContentPlaceHolderID="PlaceHolderMain" runat="server">
    <table border="0" cellspacing="0" cellpadding="0" class="ms-propertysheet">
<tr><td>
       <wssuc:InputFormSection ID="Description" Title="Description" Visible="true" Description="NextLabs Entitlement Management can be enabled/disabled on web application or site collection.Just select the web application and/or site collection that you want to operate and click Update to change.Click Disable to disable policy enforcement on the selected web application." runat="server">
	   </wssuc:InputFormSection>	

        <wssuc:InputFormSection ID="CentralAdminSection" Title="Web Application" TextAlign="Right" Visible="true" Description="Select Web Application <br/> <br/>If you check Activate Policy Enforcement by Default. All new created site collection will be protected by policy enforcement." runat="server">
            <template_inputformcontrols>       
   			    <asp:Label ID="WebAppDropDownText" Text="Web Application" runat="server" > </asp:Label>
                <asp:DropDownList ID="WebAppDropDown" runat="server" EnableViewState="true" AutoPostBack="true" OnSelectedIndexChanged="WebAppDropDown_SelectedIndexChanged"></asp:DropDownList>
                <br>
                <br>
                <wssawc:InputFormCheckBox ID="OptionCheckBox"  ForeColor=Blue  Width=260 Text="Activate Policy Enforcement by Default"  runat="server">
                </wssawc:InputFormCheckBox>
				<wssawc:InputFormCheckBox ID="DisableCheckBox"  ForeColor=Blue  Width=360 Text="Disable Policy Enforcement on the web application"  runat="server">
                </wssawc:InputFormCheckBox>
</td></tr>

<tr><td>

                <SharePoint:EncodedLiteral runat="server" id="ResetRealtimeModeText" EncodeMethod='HtmlEncode' Text="Default Mode for Document/List Processing"/>
                <wssawc:InputFormRadioButton ID="GlobalProcessUploadBatchMode" runat="server" Text="Batch" GroupName="GroupResetRealtimeMode" >
                </wssawc:InputFormRadioButton>
                <wssawc:InputFormRadioButton ID="GlobalProcessUploadRealTime" runat="server" Text="Real Time" GroupName="GroupResetRealtimeMode" >
                </wssawc:InputFormRadioButton>
                   <wssawc:InputFormRadioButton ID="GlobalProcessUploadNotset" runat="server" Text="None-Use Library Setting" GroupName="GroupResetRealtimeMode">
                </wssawc:InputFormRadioButton>
 </td></tr>     
                
			</template_inputformcontrols>
		</wssuc:InputFormSection>	
		
        <wssuc:InputFormSection ID="Feedback"  Visible="true"  runat="server">
            <template_inputformcontrols>
                <iframe id="ProgressBar" style="padding:0px; margin:0px" width="330" height='<%=visibility %>' frameborder="0" src="nlprogress.aspx?webAppName=<%=webAppName %>&&barValue=<%=barValue %>&&barStr=<%=barStr %>" scrolling="no" ></iframe>
	        </template_inputformcontrols>
       </wssuc:InputFormSection>



<wssuc:InputFormSection ID="JavaPCSetting" Title="NextLabs Platform Configuration" Visible="true" Description="Configure the host and port for the CloudAZ or Control Center Policy Controller REST API" runat="server">
  <template_inputformcontrols>
<wssuc:InputFormControl LabelText="" runat="server">
  <Template_Control>
    <table cellpadding="0" cellspacing="0">
      <tr>
      <td class="ms-descriptiontext ms-inputformdescription" style="padding-bottom:5px;padding-left:5px"></td>
      <td></td>
      </tr>
      <tr><td>
        <wssawc:InputFormCheckBox ID="InputFormCheckBoxUseJpc" ForeColor=Blue Width=260 Text="Use JPC" runat="server" AutoPostBack="true" OnCheckedChanged="UseJavaPC_CheckedChanged"></wssawc:InputFormCheckBox>
      </td></tr>
      <tr><td>
	<asp:Label ID="Label1" Text="<br/><br/>PC Host Address<br/>" runat="server" > </asp:Label>
        <wssawc:InputFormTextBox ID="InputFormTextBoxJavaPcHost"  ForeColor=Blue  Width=260 placeholder="Example: your-jpc.cloudaz.com:Port" runat="server"></wssawc:InputFormTextBox>
        <asp:Label ID="Label2" Text="<br/><br/>OAUTH Host Address<br/>" runat="server" > </asp:Label>
        <wssawc:InputFormTextBox ID="InputFormTextBoxOAUTHHost"  ForeColor=Blue  Width=260 placeholder="Example: your-cc.cloudaz.com" runat="server"></wssawc:InputFormTextBox>      
        <asp:Label ID="Label3" Text="<br/><br/>Client ID<br/>" runat="server" > </asp:Label>
        <wssawc:InputFormTextBox ID="InputFormTextBoxClientID"  ForeColor=Blue  Width=260 runat="server"></wssawc:InputFormTextBox>      
        <asp:Label ID="Label4" Text="<br/><br/>Client Secure Key<br/>" runat="server" > </asp:Label>
        <wssawc:InputFormTextBox ID="InputFormTextBoxClientSecureKey"  ForeColor=Blue  Width=260 TextMode="Password" runat="server"></wssawc:InputFormTextBox>      
      </td></tr>
    </table>
</Template_Control>
</wssuc:InputFormControl>
  </template_inputformcontrols> 
</wssuc:InputFormSection>		


		<wssuc:InputFormSection ID="RightManagementSetting" Title="Right Mangement Configuration" Visible="true" Description="Configure the right protected and secure view enable or disable" runat="server">
				<template_inputformcontrols>
                </wssawc:InputFormCheckBox>
                   <wssawc:InputFormCheckBox ID="DisableProcessRMS" runat="server" ForeColor=Blue Width=260 Text="Disable Right Management" AutoPostBack="true">
                </wssawc:InputFormCheckBox>
				</template_inputformcontrols>				
		</wssuc:InputFormSection>


        <wssuc:InputFormSection ID="OverviewSection" Title="Overview" Description="" runat="server">
            <template_description>
                Select the site collection(s) that you want to operate.
            </template_description>

            <template_inputformcontrols>

					<wssuc:InputFormControl LabelText="" runat="server">
						<Template_Control>
						    <table cellpadding="0" cellspacing="0">
						        <tr>
						            <td class="ms-descriptiontext ms-inputformdescription" style="padding-bottom:5px;padding-left:5px">
                                   
						            </td>
						            <td>
                                    
                                    </td>
						        </tr>
                                <tr>
                                    <td>
							            <asp:TreeView ShowCheckBoxes="All" EnableViewState="true" ShowLines="true" onClick="OnTreeClick(event, false)" onDblClick="OnTreeClick(event, true)" ID="FeatureTree" runat="server" NodeIndent="12" ExpandImageUrl="/_layouts/15/images/tvplus.gif" CollapseImageUrl="/_layouts/15/images/tvminus.gif" NoExpandImageUrl="/_layouts/15/images/tvblank.gif">
								            <NodeStyle CssClass="ms-authoringcontrols" />
							            </asp:TreeView>
							        </td>
							        <td id="loaderCell" style="vertical-align:middle;display:none;width:100%;text-align:center">
							            <img id="loader" src="/_layouts/15/images/FeatureManager/ajax-loader.gif" border="0" />
							        </td>
							    </tr>
							</table>
						</Template_Control>
					</wssuc:InputFormControl>
				
			</template_inputformcontrols>
        </wssuc:InputFormSection>
        <wssuc:ButtonSection runat="server" ShowStandardCancelButton="false">
            <template_buttons>
					<%--<asp:Button ID="UpdateButton" runat="server" Text="Update" class="ms-ButtonHeightWidth"  OnClick="UpdateButton_Click" OnClientClick="ShowProcess();" />--%>
                    <asp:Button ID="UpdateButton" runat="server" Text="Update" class="ms-ButtonHeightWidth" OnClientClick="UpdateButtonClick();" OnClick="UpdateButton_Click"  />
					<asp:Button ID="ReturnButton" runat="server" Text="Return" class="ms-ButtonHeightWidth"  OnClick="ReturnButton_Click" />		
			</template_buttons>
        </wssuc:ButtonSection>
    </table>
</asp:Content>
