<%@ Page language="C#"   Inherits="NextLabs.SPEnforcer.CASchedulePage,NextLabs.SPEnforcer,Version=3.0.0.0,Culture=neutral,PublicKeyToken=5ef8e9c15bdfa43e" %>
<%@ Register Tagprefix="SharePoint" Namespace="Microsoft.SharePoint.WebControls" Assembly="Microsoft.SharePoint, Version=16.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %> <%@ Register Tagprefix="WebPartPages" Namespace="Microsoft.SharePoint.WebPartPages" Assembly="Microsoft.SharePoint, Version=15.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %> <%@ Register Tagprefix="OSRVWC" Namespace="Microsoft.Office.Server.WebControls" Assembly="Microsoft.Office.Server, Version=15.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %> <%@ Register Tagprefix="SPSWC" Namespace="Microsoft.SharePoint.Portal.WebControls" Assembly="Microsoft.SharePoint.Portal, Version=15.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %> <%@ Register Tagprefix="SEARCHWC" Namespace="Microsoft.Office.Server.Search.WebControls" Assembly="Microsoft.Office.Server.Search, Version=15.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %> <%@ Register Tagprefix="PublishingWebControls" Namespace="Microsoft.SharePoint.Publishing.WebControls" Assembly="Microsoft.SharePoint.Publishing, Version=15.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %>

<script runat="server">

    protected void Page_Load(object sender, EventArgs e)
    {

    }
</script>

<HTML dir="<%$Resources:wss, multipages_direction_dir_value%>" Runat="server">
    <HEAD>
        <base target="_self">
        <SPSWC:PageHeadTags Runat="server"
            TitleLocId="EditSchedule_PageTitle_Text"
            OldExpires="0"
            PageContext="SitePage"
            PageType="Form"/>
        <link rel="stylesheet" type="text/css" href="/_layouts/<%=System.Threading.Thread.CurrentThread.CurrentUICulture.LCID%>/styles/portal.css" />
    </HEAD>   

    <script language="javascript">
    document.onkeypress = function()
    {
        if(event.keyCode == 27) 
        {
            window.close();
        }
    }
    </script>

    <body>
        <form id="EditSchedule" method="post" action="editcaschedule.aspx" runat="server">
        <SharePoint:FormDigest runat=server/>

        <table cellpadding="10" cellspacing="0" border="0" width="100%" height="100%" class="ms-propertysheet"><tr valign="top"><td>
        <SPSWC:PageLevelError runat="server" id="pageLevelError"/>
        <SPSWC:InputForm runat="server" id="NewInputForm" ShowRequiredText="true">
            <SPSWC:TextBoxLoc runat="server" id="DisplaySchedule" style="display:none"/>
            <SPSWC:TextBoxLoc runat="server" id="XmlSchedule" style="display:none"/>

            <SPSWC:InputFormSection runat="server">
                <SPSWC:InputFormSectionHelpArea runat="server" titleLocId="EditSchedule_Type_Label">
                    <SPSWC:InputFormSectionHelpText runat="server" textLocId="EditSchedule_TypeHelp_Label"/>
                </SPSWC:InputFormSectionHelpArea> 

                <SPSWC:InputFormSectionFieldArea runat="server">
                    <SPSWC:InputFormRadioButton runat="server" 
                        id="scheduleMinutely" 
                        text="Minutely"
                        groupName="scheduleDetails"
                        onClick="ResetSections(); Schedule_ShowSection('minutelySection')"/>
                    <SPSWC:InputFormRadioButton runat="server" 
                        id="scheduleHourly" 
                        text="Hourly"
                        groupName="scheduleDetails"
                        onClick="ResetSections(); Schedule_ShowSection('hourlySection')"/>
                    <SPSWC:InputFormRadioButton runat="server" 
                        id="scheduleDaily" 
                        textLocId="EditSchedule_Daily_Label" 
                        groupName="scheduleDetails"
                        onClick="ResetSections(); Schedule_ShowSection('dailySection')" 
                        checked/>
                    <SPSWC:InputFormRadioButton runat="server"    
                        id="scheduleWeekly" 
                        textLocId="EditSchedule_Weekly_Label" 
                        groupName="scheduleDetails"
                        onClick="ResetSections(); Schedule_ShowSection('weeklySection')"/>
                </SPSWC:InputFormSectionFieldArea>
            </SPSWC:InputFormSection>

            <SPSWC:InputFormSection runat="server">
                <SPSWC:InputFormSectionHelpArea runat="server" titleLocId="EditSchedule_Settings_Label">
                    <SPSWC:InputFormSectionHelpText runat="server" textLocId="EditSchedule_SettingsHelp_Label"/>
                </SPSWC:InputFormSectionHelpArea> 
                <SPSWC:InputFormSectionFieldArea runat="server">
                    <SPSWC:InputFormTable runat="server">
                        <SPSWC:InputFormTableRow runat="server">

                            <SPSWC:InputFormTableData runat="server" width="100%">
                                <SPSWC:InputFormDynamicSection runat="server" id="minutelySection" style="display:none">
                                    <SPSWC:InputFormTable runat="server">

                                        <SPSWC:InputFormTableRow runat="server">
                                            <SPSWC:InputFormTableData runat="server" width="200" colspan="2">
                                                <SPSWC:InputFormLabel runat="server" 
                                                    labelTextLocId="EditSchedule_RunEvery_Label"
                                                    isRequired="true"/>
                                            </SPSWC:InputFormTableData>
                                            <SPSWC:InputFormTableData runat="server">
                                                <tr><td class="ms-authoringcontrols">                                        
                                                <SPSWC:TextBoxLoc runat="server"
                                                    id="minutelyIntervalText"
                                                    width="50"
                                                    class="ms-long"
                                                    maxLength="2"
                                                    text="1"
                                                    onKeyPress="return CheckKeyIsNumber()"/>
                                                <SPSWC:LabelLoc ID="LabelLoc2" runat="server" text="minutes"/>
                                                </td></tr>
                                            </SPSWC:InputFormTableData>
                                        </SPSWC:InputFormTableRow>

                                        <SPSWC:InputFormTableRow runat="server">
                                            <SPSWC:InputFormTableData runat="server" colspan="3">
                                                <SPSWC:InputFormDynamicSection runat="server" id="minutelyIntervalValidator">
                                                    <SPSWC:InputFormCustomValidator runat="server" 
                                                        display="dynamic"
                                                        errorMessageLocId="EditSchedule_Interval_Error"
                                                        onServerValidate="MinutelyIntervalValidation"/>
                                                </SPSWC:InputFormDynamicSection>
                                            </SPSWC:InputFormTableData>
                                        </SPSWC:InputFormTableRow>
                                    </SPSWC:InputFormTable>
                                </SPSWC:InputFormDynamicSection>
                                
                                <SPSWC:InputFormDynamicSection runat="server" id="hourlySection" style="display:none">
                                    <SPSWC:InputFormTable runat="server">

                                        <SPSWC:InputFormTableRow runat="server">
                                            <SPSWC:InputFormTableData runat="server" width="200" colspan="2">
                                                <SPSWC:InputFormLabel runat="server" 
                                                    labelTextLocId="EditSchedule_RunEvery_Label"
                                                    isRequired="true"/>
                                            </SPSWC:InputFormTableData>
                                            <SPSWC:InputFormTableData runat="server">
                                                <tr><td class="ms-authoringcontrols">                                        
                                                <SPSWC:TextBoxLoc runat="server"
                                                    id="hourlyIntervalText"
                                                    width="50"
                                                    class="ms-long"
                                                    maxLength="2"
                                                    text="1"
                                                    onKeyPress="return CheckKeyIsNumber()"/>
                                                <SPSWC:LabelLoc ID="LabelLoc1" runat="server" text="hours"/>
                                                </td></tr>
                                            </SPSWC:InputFormTableData>
                                        </SPSWC:InputFormTableRow>

                                        <SPSWC:InputFormTableRow runat="server">
                                            <SPSWC:InputFormTableData runat="server" colspan="3">
                                                <SPSWC:InputFormDynamicSection runat="server" id="hourlyIntervalValidator">
                                                    <SPSWC:InputFormCustomValidator runat="server" 
                                                        display="dynamic"
                                                        errorMessageLocId="EditSchedule_Interval_Error"
                                                        onServerValidate="HourlyIntervalValidation"/>
                                                </SPSWC:InputFormDynamicSection>
                                            </SPSWC:InputFormTableData>
                                        </SPSWC:InputFormTableRow>
                                    </SPSWC:InputFormTable>
                                </SPSWC:InputFormDynamicSection>


                                <SPSWC:InputFormDynamicSection runat="server" id="dailySection" style="display:none">
                                    <SPSWC:InputFormTable runat="server">

                                        <SPSWC:InputFormTableRow runat="server">
                                            <SPSWC:InputFormTableData runat="server" width="200" colspan="2">
                                                <SPSWC:InputFormLabel runat="server" 
                                                    labelTextLocId="EditSchedule_RunEvery_Label"
                                                    isRequired="true"/>
                                            </SPSWC:InputFormTableData>
                                            <SPSWC:InputFormTableData runat="server">
                                                <tr><td class="ms-authoringcontrols">                                        
                                                <SPSWC:TextBoxLoc runat="server"
                                                    id="dailyIntervalText"
                                                    width="50"
                                                    class="ms-long"
                                                    maxLength="3"
                                                    text="1"
                                                    onKeyPress="return CheckKeyIsNumber()"/>
                                                <SPSWC:LabelLoc runat="server" textLocId="EditSchedule_Days_Label"/>
                                                </td></tr>
                                            </SPSWC:InputFormTableData>
                                        </SPSWC:InputFormTableRow>

                                        <SPSWC:InputFormTableRow runat="server">
                                            <SPSWC:InputFormTableData runat="server" colspan="3">
                                                <SPSWC:InputFormDynamicSection runat="server" id="dailyIntervalValidator">
                                                    <SPSWC:InputFormCustomValidator runat="server" 
                                                        display="dynamic"
                                                        errorMessageLocId="EditSchedule_Interval_Error"
                                                        onServerValidate="DailyIntervalValidation"/>
                                                </SPSWC:InputFormDynamicSection>
                                            </SPSWC:InputFormTableData>
                                        </SPSWC:InputFormTableRow>

                                        <SPSWC:InputFormTableRow runat="server">
                                            <SPSWC:InputFormTableData runat="server" colspan="2">
                                                <SPSWC:InputFormLabel runat="server" labelTextLocId="EditSchedule_StartTime_Label"/>
                                            </SPSWC:InputFormTableData>
                                            <SPSWC:InputFormTableData runat="server">
                                                <SPSWC:InputFormDropDownList runat="server" 
                                                    width="100" 
                                                    id="dailyStartTimeList"
                                                    indentedControl="false"/>
                                            </SPSWC:InputFormTableData>
                                        </SPSWC:InputFormTableRow>
                                    </SPSWC:InputFormTable>
                                </SPSWC:InputFormDynamicSection>

                                <SPSWC:InputFormDynamicSection runat="server" id="weeklySection" style="display:none">
                                    <SPSWC:InputFormTable runat="server">

                                        <SPSWC:InputFormTableRow runat="server">
                                            <SPSWC:InputFormTableData runat="server" width="200" colspan="2">
                                                <SPSWC:InputFormLabel runat="server" 
                                                    labelTextLocId="EditSchedule_RunEvery_Label"
                                                    isRequired="true"/>
                                            </SPSWC:InputFormTableData>
                                            <SPSWC:InputFormTableData runat="server">
                                                <tr><td class="ms-authoringcontrols">                                        
                                                <SPSWC:TextBoxLoc runat="server"
                                                    id="weeklyIntervalText"
                                                    width="50"
                                                    class="ms-long"
                                                    maxLength="3"
                                                    text="1"
                                                    onKeyPress="return CheckKeyIsNumber()"/>
                                                <SPSWC:LabelLoc runat="server" textLocId="EditSchedule_Weeks_Label"/>
                                                </td></tr>
                                            </SPSWC:InputFormTableData>
                                        </SPSWC:InputFormTableRow>

                                        <SPSWC:InputFormTableRow runat="server">
                                            <SPSWC:InputFormTableData runat="server" colspan="3">
                                                <SPSWC:InputFormDynamicSection runat="server" id="weeklyIntervalValidator"> 
                                                    <SPSWC:InputFormCustomValidator runat="server" 
                                                        display="dynamic"
                                                        errorMessageLocId="EditSchedule_Interval_Error"
                                                        onServerValidate="WeeklyIntervalValidation"/>
                                                </SPSWC:InputFormDynamicSection>
                                            </SPSWC:InputFormTableData>
                                        </SPSWC:InputFormTableRow>

                                        <SPSWC:InputFormTableRow runat="server">
                                            <SPSWC:InputFormTableData runat="server" colspan="2">
                                                <SPSWC:InputFormLabel runat="server" 
                                                    labelTextLocId="EditSchedule_On_Label"
                                                    isRequired="true"/>
                                            </SPSWC:InputFormTableData>
                                            <SPSWC:InputFormTableData runat="server">
                                                <tr><td><SPSWC:CheckBoxLoc runat="server"
                                                            id="weeklyDayMonday" 
                                                            textLocId="WeekDay_Monday_Label"/></td></tr>
                                                <tr><td><SPSWC:CheckBoxLoc runat="server" 
                                                            id="weeklyDayTuesday"
                                                            textLocId="WeekDay_Tuesday_Label"/></td></tr>
                                                <tr><td><SPSWC:CheckBoxLoc runat="server" 
                                                            id="weeklyDayWednesday"
                                                            textLocId="WeekDay_Wednesday_Label"/></td></tr>
                                                <tr><td><SPSWC:CheckBoxLoc runat="server" 
                                                            id="weeklyDayThursday"
                                                            textLocId="WeekDay_Thursday_Label"/></td></tr>
                                                <tr><td><SPSWC:CheckBoxLoc runat="server" 
                                                            id="weeklyDayFriday"
                                                            textLocId="WeekDay_Friday_Label"/></td></tr>
                                                <tr><td><SPSWC:CheckBoxLoc runat="server" 
                                                            id="weeklyDaySaturday"
                                                            textLocId="WeekDay_Saturday_Label"/></td></tr>
                                                <tr><td><SPSWC:CheckBoxLoc runat="server" 
                                                            id="weeklyDaySunday"
                                                            textLocId="WeekDay_Sunday_Label"/></td></tr>
                                            </SPSWC:InputFormTableData>
                                        </SPSWC:InputFormTableRow>
                                        
                                        <SPSWC:InputFormTableRow runat="server">
                                            <SPSWC:InputFormTableData runat="server" width="20"></SPSWC:InputFormTableData>
                                            <SPSWC:InputFormTableData runat="server" colspan="2">
                                                <SPSWC:InputFormDynamicSection runat="server" id="weeklyDayValidator">
                                                    <SPSWC:InputFormCustomValidator runat="server" 
                                                        display="dynamic"
                                                        errorMessageLocId="EditSchedule_DaySelection_Error"
                                                        onServerValidate="DaysOfWeekValidation"/>
                                                </SPSWC:InputFormDynamicSection>
                                            </SPSWC:InputFormTableData>
                                        </SPSWC:InputFormTableRow>

                                        <SPSWC:InputFormTableRow runat="server">
                                            <SPSWC:InputFormTableData runat="server" colspan="2">
                                                <SPSWC:InputFormLabel runat="server" labelTextLocId="EditSchedule_StartTime_Label"/>
                                            </SPSWC:InputFormTableData>
                                            <SPSWC:InputFormTableData runat="server">
                                                <SPSWC:InputFormDropDownList runat="server" 
                                                    width="100" 
                                                    id="weeklyStartTimeList"
                                                    indentedControl="false"/>
                                            </SPSWC:InputFormTableData>
                                        </SPSWC:InputFormTableRow>
                                    </SPSWC:InputFormTable>
                                </SPSWC:InputFormDynamicSection>
                            </SPSWC:InputFormTableData> 
                        </SPSWC:InputFormTableRow>
                    </SPSWC:InputFormTable>
                </SPSWC:InputFormSectionFieldArea>
            </SPSWC:InputFormSection>

            <SPSWC:InputFormButtonSection runat="server">
                <SPSWC:InputFormButtonAtBottom runat="server" 
                    id="cmdOK" 
                    OnClick="OnClickOK" 
                    TextLocId="Page_OkButton_Text"
                    accessKey="<%$Resources:Microsoft.Office.Server.Search, SearchAdmin_Ok_AccessKey%>"/>
                <SPSWC:InputFormButtonAtBottom runat="server" 
                    id="cmdCancel"
                    OnClick="OnClickCancel" 
                    TextLocId="Page_CancelButton_Text"
                    accessKey="<%$Resources:Microsoft.Office.Server.Search, SearchAdmin_Cancel_AccessKey%>"/>
            </SPSWC:InputFormButtonSection>
        </SPSWC:InputForm>
        </td></tr>
        </table>            
    </form>
    </body>
</HTML>

