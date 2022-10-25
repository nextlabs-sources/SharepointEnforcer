<!-- _lcid="1033" _version="14.0.4758" _dal="1" -->
<!-- _LocalBinding -->
<%@ Assembly Name="Microsoft.Office.Policy.Pages, Version=16.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c"%>
<%@ Register Tagprefix="SharePoint" Namespace="Microsoft.SharePoint.WebControls" Assembly="Microsoft.SharePoint, Version=16.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %> <%@ Register Tagprefix="Utilities" Namespace="Microsoft.SharePoint.Utilities" Assembly="Microsoft.SharePoint, Version=16.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %> <%@ Import Namespace="Microsoft.SharePoint" %> <%@ Assembly Name="Microsoft.Web.CommandUI, Version=16.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %>
<%@ Control Language="C#" Inherits="Microsoft.Office.RecordsManagement.PolicyFeatures.ApplicationPages.LabelSettings" %>
<p>
<table cellpadding="0" class="ms-authoringcontrols">
	<tr>
		<td></td>
		<td><asp:CheckBox runat="Server" id="CheckBoxEvents" ToolTip="<%$Resources:dlcpolicy, LabelSettings_PromptUsers%>" />
				<label for="CheckBoxEvents">&#160;<SharePoint:EncodedLiteral runat="server" text="<%$Resources:dlcpolicy, LabelSettings_PromptUsers%>" EncodeMethod='HtmlEncode'/></label>
		</td>
	</tr>
	<tr>
		<td></td>
		<td><asp:CheckBox runat="Server" id="CheckBoxForceLock" ToolTip="<%$Resources:dlcpolicy, LabelSettings_LockLabel%>" />
				<label for="CheckBoxForceLock">&#160;<SharePoint:EncodedLiteral runat="server" text="<%$Resources:dlcpolicy, LabelSettings_LockLabel%>" EncodeMethod='HtmlEncode'/></label>
		</td>
	</tr>
	<tr>
		<td></td>
		<td>&#160;</td>
	</tr>
	<tr>
		<td>&#160;</td>
		<td><SharePoint:EncodedLiteral runat="server" text="<%$Resources:dlcpolicy, LabelSettings_Label%>" EncodeMethod='HtmlEncode'/>
		</td>
	</tr>
	<tr>
		<td>&#160;</td>
		<td>
		 <asp:TextBox id="TextBoxLabelFormat" runat="server" MaxLength="1024" Columns="40" class="ms-input" ToolTip="<%$Resources:dlcpolicy, LabelSettings_Label%>" />
		 <asp:RequiredFieldValidator id="RequiredValidatorLabelFormat"
					ControlToValidate="TextBoxLabelFormat"
					ErrorMessage="<%$Resources:dlcpolicy, LabelSettings_ErrorInvalid%>"
					Text="<%$Resources:dlcpolicy, Error_Indicator%>"
					EnableClientScript="false"
					runat="server"/>
		 <asp:CustomValidator id="CustomValidatorLabelFormat"
					ControlToValidate="TextBoxLabelFormat"
					EnableClientScript="false"
					OnServerValidate="ValidateLabelFormat"
					Text="<%$Resources:dlcpolicy, Error_Indicator%>"
					runat="server"/>
		</td>
	</tr>
	<tr>
		<td></td>
		<td>&#160;</td>
	</tr>
	<tr>
		<td>&#160;</td>
		<td><SharePoint:EncodedLiteral runat="server" text="<%$Resources:dlcpolicy, LabelSettings_Examples%>" EncodeMethod='HtmlEncode'/>
		</td>
	</tr>
	<tr>
		<td>&#160;</td>
		<td>
			<ul>
				<li><SharePoint:EncodedLiteral runat="server" text="<%$Resources:dlcpolicy, LabelSettings_ExampleProject%>" EncodeMethod='HtmlEncode'/></li>
				<li><SharePoint:EncodedLiteral runat="server" text="<%$Resources:dlcpolicy, LabelSettings_ExampleConfidential%>" EncodeMethod='HtmlEncode'/></li>
			</ul>
		</td>
	</tr>
</table>
<table cellpadding="0" width="100%">
	<tr>
		<td class="ms-sectionline"><img src="/_layouts/images/blank.gif" width='1' height='1' alt="" /></td>
	</tr>
</table>
</p>
<span class="ms-separator">
<SharePoint:EncodedLiteral runat="server" text="<%$Resources:dlcpolicy, LabelSettings_TextFormatting%>" EncodeMethod='HtmlEncode'/>
</span>
<table cellpadding="0" class="ms-authoringcontrols">
	<tr>
		<td ><label for="DropDownFont"><SharePoint:EncodedLiteral runat="server" text="<%$Resources:dlcpolicy, LabelSettings_Font%>" EncodeMethod='HtmlEncode'/></label></td>
		<td><img src="/_layouts/images/blank.gif" width='25' height='1' alt="" /></td>
		<td><asp:DropDownList id="DropDownFont" runat="server" ToolTip="<%$Resources:dlcpolicy, LabelSettings_ListFontName%>" /></td>
	</tr>
	<tr>
		<td><label for="DropDownSize"><SharePoint:EncodedLiteral runat="server" text="<%$Resources:dlcpolicy, LabelSettings_Size%>" EncodeMethod='HtmlEncode'/></label></td>
		<td><img src="/_layouts/images/blank.gif" width='25' height='1' alt="" /></td>
		<td><asp:DropDownList id="DropDownFontSize" runat="server" ToolTip="<%$Resources:dlcpolicy, LabelSettings_FontSize%>" >
				<asp:ListItem Value="8">8</asp:ListItem>
				<asp:ListItem Value="9">9</asp:ListItem>
				<asp:ListItem Value="10">10</asp:ListItem>
				<asp:ListItem Value="11">11</asp:ListItem>
				<asp:ListItem Value="12">12</asp:ListItem>
				<asp:ListItem Value="14">14</asp:ListItem>
				<asp:ListItem Value="16">16</asp:ListItem>
				<asp:ListItem Value="18">18</asp:ListItem>
				<asp:ListItem Value="20">20</asp:ListItem>
				<asp:ListItem Value="22">22</asp:ListItem>
				<asp:ListItem Value="24">24</asp:ListItem>
				<asp:ListItem Value="26">26</asp:ListItem>
				<asp:ListItem Value="28">28</asp:ListItem>
				<asp:ListItem Value="36">36</asp:ListItem>
				<asp:ListItem Value="48">48</asp:ListItem>
				<asp:ListItem Value="72">72</asp:ListItem>
			</asp:DropDownList></td>
	</tr>
	<tr>
		<td><label for="DropDownStyle"><SharePoint:EncodedLiteral runat="server" text="<%$Resources:dlcpolicy, LabelSettings_Style%>" EncodeMethod='HtmlEncode'/></label></td>
		<td><img src="/_layouts/images/blank.gif" width='25' height='1' alt="" /></td>
		<td><asp:DropDownList id="DropDownStyle" runat="server" ToolTip="<%$Resources:dlcpolicy, LabelSettings_FontStyle%>" >
				<asp:ListItem Value="regular" Text="<%$Resources:dlcpolicy, LabelSettings_Regular%>"></asp:ListItem>
				<asp:ListItem Value="bold" Text="<%$Resources:dlcpolicy, LabelSettings_Bold%>"></asp:ListItem>
				<asp:ListItem Value="italic" Text="<%$Resources:dlcpolicy, LabelSettings_Italic%>"></asp:ListItem>
				<asp:ListItem Value="bold italic" Text="<%$Resources:dlcpolicy, LabelSettings_BoldItalic%>"></asp:ListItem>
			</asp:DropDownList>
		</td>
	</tr>
	<tr>
		<td><label for="DropDownJustification"><SharePoint:EncodedLiteral runat="server" text="<%$Resources:dlcpolicy, LabelSettings_Justification%>" EncodeMethod='HtmlEncode'/></label></td>
		<td><img src="/_layouts/images/blank.gif" width='25' height='1' alt="" /></td>
		<td><asp:DropDownList id="DropDownJustification" runat="server" ToolTip="<%$Resources:dlcpolicy, LabelSettings_FontJustification%>" >
				<asp:ListItem Value="center" Text="<%$Resources:dlcpolicy, LabelSettings_Center%>"></asp:ListItem>
				<asp:ListItem Value="left" Text="<%$Resources:dlcpolicy, LabelSettings_Left%>"></asp:ListItem>
				<asp:ListItem Value="right" Text="<%$Resources:dlcpolicy, LabelSettings_Right%>"></asp:ListItem>
			</asp:DropDownList>
		</td>
	</tr>
</table>
<span class="ms-separator">
<SharePoint:EncodedLiteral runat="server" text="<%$Resources:dlcpolicy, LabelSettings_LabelSize%>" EncodeMethod='HtmlEncode'/>
</span>
<table class="ms-authoringcontrols">
	<tr>
		<td><label for="TextBoxHeight"><SharePoint:EncodedLiteral runat="server" text="<%$Resources:dlcpolicy, LabelSettings_Height%>" EncodeMethod='HtmlEncode'/></label></td>
		<td><img src="/_layouts/images/blank.gif" width='25' height='1' alt="" /></td>
		<td><asp:TextBox id="TextBoxHeight" runat="server" MaxLength="5" Columns="5" ToolTip="<%$Resources:dlcpolicy, LabelSettings_Height%>" ></asp:TextBox>
			<asp:RangeValidator id="RangeValidatorHeight"
				ControlToValidate="TextBoxHeight"
				Type="Double"
				Display="Static"
				EnableClientScript="false"
				Text="<%$Resources:dlcpolicy, Error_Indicator%>"
				runat="server"/>
		</td>
		<td><asp:Label id="LabelHeightUnit" runat="server" /></td>
	</tr>
	<tr>
		<td><label for="TextBoxWidth"><SharePoint:EncodedLiteral runat="server" text="<%$Resources:dlcpolicy, LabelSettings_Width%>" EncodeMethod='HtmlEncode'/></label></td>
		<td><img src="/_layouts/images/blank.gif" width='25' height='1' alt="" /></td>
		<td><asp:TextBox id="TextBoxWidth" runat="server" MaxLength="5" Columns="5" ToolTip="<%$Resources:dlcpolicy, LabelSettings_Width%>" ></asp:TextBox>
			<asp:RangeValidator id="RangeValidatorWidth"
				ControlToValidate="TextBoxWidth"
				Type="Double"
				Display="Static"
				EnableClientScript="false"
				Text="<%$Resources:dlcpolicy, Error_Indicator%>"
				runat="server"/>
		</td>
		<td><asp:Label id="LabelWidthUnit" runat="server" /></td>
	</tr>
</table>
<table cellpadding="0" Width="100%">
	<tr>
		<td class="ms-sectionline"><img src="/_layouts/images/blank.gif" width='1' height='1' alt="" /></td>
	</tr>
</table>
<p>
<span class="ms-separator">
<SharePoint:EncodedLiteral runat="server" text="<%$Resources:dlcpolicy, LabelSettings_Preview%>" EncodeMethod='HtmlEncode'/>
</span>
<table cellpadding="5" class="ms-authoringcontrols">
	<tr>
		<td colspan="2"><asp:Image id="ImageSample" runat="server" AlternateText="<%$Resources:dlcpolicy, LabelSettings_SampleAltText%>" /></td>
	</tr>
	<tr>
		<td>
		   <asp:Button id="ButtonSampleRefresh" Text="<%$Resources:dlcpolicy, LabelSettings_Refresh%>" OnCommand="BtnSampleRefresh_Click" runat="server" />
		</td>
	</tr>
</table>
</p>
