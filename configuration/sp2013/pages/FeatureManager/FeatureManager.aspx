<%@ Assembly Name="NextLabs.Deployment, Version=1.0.0.0, Culture=neutral, PublicKeyToken=e03e4c7ee29d89ce" %>
<%@ Page Language="C#" AutoEventWireup="true" Inherits="NextLabs.Deployment.FeatureManager" MasterPageFile="~/_layouts/application.master" %>
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
    <SharePoint:EncodedLiteral runat="server" Text="NextLabs Entitlement Management - Prepare site for export or backup" EncodeMethod='HtmlEncode' />
</asp:Content>
<asp:Content ContentPlaceHolderID="PlaceHolderPageTitleInTitleArea" runat="server">
    <SharePoint:EncodedLiteral runat="server" Text="NextLabs Entitlement Management - Prepare site for export or backup" EncodeMethod='HtmlEncode' />
</asp:Content>
<asp:Content ContentPlaceHolderID="PlaceHolderPageDescription" runat="server">
    <SharePoint:EncodedLiteral runat="server" ID="PageDescription" Text="Disable NextLabs Entitlement Management features before exporting or backing up SharePoint sites."
        EncodeMethod='HtmlEncode' />
</asp:Content>
<asp:Content ContentPlaceHolderID="PlaceHolderAdditionalPageHead" runat="server">
    <script type="text/javascript" src="TreeUtils.js"></script>
    <script type="text/javascript" src="UITools.js"></script>
</asp:Content>
<asp:Content ContentPlaceHolderID="PlaceHolderMain" runat="server">
    <table border="0" cellspacing="0" cellpadding="0" class="ms-propertysheet">

       <wssuc:InputFormSection ID="Description" Title="Description" Visible="true" Description="NextLabs Entitlement Management includes SharePoint Features.  If you are performing an export or back up a site for transfer or restore to a system that does not have the NextLabs Entitlement Manager installed, these features need to be disabled before export." runat="server">
		</wssuc:InputFormSection>	

		<wssuc:InputFormSection ID="ScopeSection" Title="Export/Backup Scope" Description="Select the scope that you want to export or backup" runat="server">
            <template_inputformcontrols>   
			    <SharePoint:InputFormRadioButton ID="WebScopeRadioButton" LabelText="Site" runat="server" GroupName="FeatureScope"  AutoPostBack="true" EnableViewState="true" OnCheckedChanged="WebScopeRadioButton_CheckedChanged" />
                 <SharePoint:InputFormRadioButton ID="SiteScopeRadioButton" LabelText="Site Collection" runat="server" GroupName="FeatureScope" AutoPostBack="true" EnableViewState="true" OnCheckedChanged="SiteScopeRadioButton_CheckedChanged" />                    
                <SharePoint:InputFormRadioButton ID="WebAppRadioButton" LabelText="Web Application" runat="server" GroupName="FeatureScope"  AutoPostBack="true" EnableViewState="true" OnCheckedChanged="WebAppRadioButton_CheckedChanged" />
			</template_inputformcontrols>
		</wssuc:InputFormSection>

        <wssuc:InputFormSection ID="CentralAdminSection" Title="Web Application" Visible="false" Description="Select Web Application" runat="server">
            <template_inputformcontrols>
                <wssuc:InputFormControl LabelText="Web Application" runat="server">
					<Template_Control>
                        <asp:DropDownList ID="WebAppDropDown" runat="server" EnableViewState="true" AutoPostBack="true" OnSelectedIndexChanged="WebAppDropDown_SelectedIndexChanged"></asp:DropDownList>
					</Template_Control>
				</wssuc:InputFormControl>       
			</template_inputformcontrols>
		</wssuc:InputFormSection>	
		
        <wssuc:InputFormSection ID="FeatureSection" Visible="false" Title="Features"  Description="Select which Feature to manage from the list. (Hidden Features appear in gray)" runat="server">
            <template_inputformcontrols>
					<wssuc:InputFormControl LabelText="Features" runat="server">
						<Template_Control>
						    <table cellpadding="0" cellspacing="0">
								<tr style="padding-bottom:5px">
									<td class="ms-authoringcontrols">
										<asp:Label ID="hiddenLabel" runat="server" Text="Include Hidden Features" />
									</td>
									<td class="ms-authoringcontrols">
									    <asp:CheckBox ID="ShowHiddenFeaturesCB" runat="server" OnCheckedChanged="ShowHiddenFeaturesCB_CheckedChanged" AutoPostBack="true" EnableViewState="true" />
									</td>
								</tr>
							</table>
							<asp:DropDownList ID="WebFeatureDropDown" runat="server" EnableViewState="true" AutoPostBack="true" OnSelectedIndexChanged="WebFeatureDropDown_SelectedIndexChanged"></asp:DropDownList>
						</Template_Control>
					</wssuc:InputFormControl>
					<wssuc:InputFormControl LabelText="Feature Info" runat="server">					
						<Template_Control>
							<table width="100%" cellpadding="0" cellspacing="0">
								<tr style="padding-top:5px">
									<td style="vertical-align:top;padding:8px 10px 0px 0px;">
										<asp:Image ID="FeatureImage" runat="server" />
									</td>
									<td style="vertical-align:top" width="100%">
										<table width="100%" cellpadding="0" cellspacing="0">
											<tr>
												<td class="ms-authoringcontrols" style="padding-bottom:5px;font-weight:bold">
													<asp:Label ID="FeatureTitleLabel" Text="Feature Title" runat="server" />
												</td>
											</tr>
											<tr>
												<td class="ms-authoringcontrols" style="padding-bottom:5px">
													<asp:Label ID="FeatureDescriptionLabel" Text="Feature Description" runat="server" />
												</td>
											</tr>
											<tr>
												<td class="ms-authoringcontrols">
													<asp:Label ID="FeatureStatusLabel" Text="" runat="server" />
												</td>
											</tr>
										</table>
									</td>
								</tr>
							</table>										
						</Template_Control>
					</wssuc:InputFormControl>
				</template_inputformcontrols>
        </wssuc:InputFormSection>

        <wssuc:InputFormSection ID="OverviewSection" Title="Overview" Description="" runat="server">
            <template_description>
                Select the item(s) that you want to export/backup and then disable them before export/backup. Or enable again after export/backup. 
                <br><br>
                <table>
                    <tr>
                        <td class="ms-descriptiontext ms-inputformdescription" style="vertical-align:top;padding-left:3px;width:15px"><img src="/_layouts/15/Images/FeatureManager/Active.gif" border="0" /></td>
                        <td class="ms-descriptiontext ms-inputformdescription">The item(s) was selected before.</td>
                    </tr>
                </table>
                <br /><br />
                <asp:Label ID="Warning" Text="Please enable NextLabs SharePoint Entitlement again after export/backup." Visible="false" ForeColor="red" runat="server" />         
            </template_description>
            <template_inputformcontrols>
					<wssuc:InputFormControl LabelText="Overview" runat="server">
						<Template_Control>
						    <table cellpadding="0" cellspacing="0">
						        <tr>
						            <td class="ms-descriptiontext ms-inputformdescription" style="padding-bottom:5px;padding-left:5px">
						                <asp:HyperLink ID="ExpandCollapseTreeHyperlink" runat="server" text="Expand/Collapse All" />
						            </td>
						            <td></td>
						        </tr>
                                <tr>
                                    <td>
							            <asp:TreeView ShowCheckBoxes="All" EnableViewState="true" ShowLines="true" onClick="OnTreeClick(event, false)" onDblClick="OnTreeClick(event, true)" ID="FeatureTree" runat="server" NodeIndent="12" ExpandImageUrl="/_layouts/15/images/tvplus.gif" CollapseImageUrl="/_layouts/15/images/tvminus.gif" NoExpandImageUrl="/_layouts/15/images/tvblank.gif">
								            <NodeStyle CssClass="ms-authoringcontrols" />
							            </asp:TreeView>
							        </td>
							        <td id="loaderCell" style="vertical-align:middle;display:none;width:100%;text-align:center">
							            <img id="loader" src="~/_layouts/15/images/FeatureManager/ajax-loader.gif" border="0" />
							        </td>
							    </tr>
							</table>
						</Template_Control>
					</wssuc:InputFormControl>
					 <wssuc:InputFormControl id="NavigationControl" LabelText="Navigation" runat="server" Visible="false">
						<Template_Control>
						    <table cellpadding="0" cellspacing="0">
			                    <tr>
                                    <td class="ms-descriptiontext ms-inputformdescription" style="width:16px">
                                        <asp:ImageButton id="RootSiteImageButton" runat="server" ImageUrl="~/_layouts/15/Images/UPFOLDER.GIF" OnClick="RootSiteImageButton_Click" ToolTip="Root Site." />
                                    </td>
                                    <td class="ms-descriptiontext ms-inputformdescription">
                                        <asp:LinkButton ID="RootSiteLinkButton" runat="server" Text="Root Site" OnClick="RootSiteLinkButton_Click"/>
                                    </td>
                                </tr>
                                <tr>
                                    <td class="ms-descriptiontext ms-inputformdescription" style="width:16px">
                                        <asp:ImageButton id="ParentSiteImageButton" runat="server" ImageUrl="~/_layouts/15/Images/UPFOLDER.GIF" OnClick="ParentSiteImageButton_Click" ToolTip="Parent Site." />
                                    </td>
                                    <td class="ms-descriptiontext ms-inputformdescription">
                                        <asp:LinkButton ID="ParentSiteLinkButton" runat="server" Text="Parent Site" OnClick="ParentSiteLinkButton_Click"/>
                                    </td>
                                </tr>
                            </table>
						</Template_Control>
					</wssuc:InputFormControl>
			</template_inputformcontrols>
        </wssuc:InputFormSection>
        <wssuc:InputFormSection ID="UpdateSection" Title="Update Status" Description="List of all changes made on the current Update." runat="server">
            <template_inputformcontrols>
					<wssuc:InputFormControl LabelText="Changes" runat="server">
						<Template_Control>
							<asp:Table ID="StatusTable" runat="server"></asp:Table>
						</Template_Control>
					</wssuc:InputFormControl>
				</template_inputformcontrols>
        </wssuc:InputFormSection>
        <wssuc:ButtonSection runat="server" ShowStandardCancelButton="false">
            <template_buttons>
					<asp:Button ID="EnableButton" runat="server" Text="Enable" class="ms-ButtonHeightWidth" OnClick="EnableButton_Click" OnClientClick="ShowProcess();" />
					<asp:Button ID="DisableButton" runat="server" Text="Disable" class="ms-ButtonHeightWidth" OnClick="DisableButton_Click" OnClientClick="ShowProcess();" />
                    <asp:Button ID="ClearButton" runat="server" Visible="false" Text="Clear" class="ms-ButtonHeightWidth" OnClick="ClearButton_Click" OnClientClick="ShowProcess();" />
				</template_buttons>
        </wssuc:ButtonSection>
    </table>
</asp:Content>
