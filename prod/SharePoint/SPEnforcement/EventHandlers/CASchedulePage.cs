using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Web.UI.WebControls;
using Microsoft.SharePoint;
using Microsoft.SharePoint.Utilities;
using Microsoft.SharePoint.WebControls;
using Microsoft.SharePoint.Portal.WebControls;
using System.Text.RegularExpressions;

namespace NextLabs.SPEnforcer
{
    public class CASchedulePage : LayoutsPageBase
    {
        protected Microsoft.SharePoint.Portal.WebControls.InputFormRadioButton scheduleDaily;
        protected Microsoft.SharePoint.Portal.WebControls.InputFormRadioButton scheduleWeekly;
        protected Microsoft.SharePoint.Portal.WebControls.InputFormRadioButton scheduleHourly;
        protected Microsoft.SharePoint.Portal.WebControls.InputFormRadioButton scheduleMinutely;

        protected Microsoft.SharePoint.Portal.WebControls.InputFormDynamicSection dailySection;
        protected Microsoft.SharePoint.Portal.WebControls.InputFormDynamicSection weeklySection;
        protected Microsoft.SharePoint.Portal.WebControls.InputFormDynamicSection hourlySection;
        protected Microsoft.SharePoint.Portal.WebControls.InputFormDynamicSection minutelySection;

        protected Microsoft.SharePoint.Portal.WebControls.TextBoxLoc hourlyIntervalText;
        protected Microsoft.SharePoint.Portal.WebControls.TextBoxLoc minutelyIntervalText;

        protected Microsoft.SharePoint.Portal.WebControls.TextBoxLoc dailyIntervalText;
        protected Microsoft.SharePoint.Portal.WebControls.InputFormDropDownList dailyStartTimeList;

        protected Microsoft.SharePoint.Portal.WebControls.TextBoxLoc weeklyIntervalText;
        protected Microsoft.SharePoint.Portal.WebControls.CheckBoxLoc weeklyDayMonday;
        protected Microsoft.SharePoint.Portal.WebControls.CheckBoxLoc weeklyDayTuesday;
        protected Microsoft.SharePoint.Portal.WebControls.CheckBoxLoc weeklyDayWednesday;
        protected Microsoft.SharePoint.Portal.WebControls.CheckBoxLoc weeklyDayThursday;
        protected Microsoft.SharePoint.Portal.WebControls.CheckBoxLoc weeklyDayFriday;
        protected Microsoft.SharePoint.Portal.WebControls.CheckBoxLoc weeklyDaySaturday;
        protected Microsoft.SharePoint.Portal.WebControls.CheckBoxLoc weeklyDaySunday;
        protected Microsoft.SharePoint.Portal.WebControls.InputFormDropDownList weeklyStartTimeList;

        protected Microsoft.SharePoint.Portal.WebControls.TextBoxLoc DisplaySchedule;
        protected Microsoft.SharePoint.Portal.WebControls.TextBoxLoc XmlSchedule;

        private const string urlFilter = @"^(ht|f)tp(s?)\:\/\/[0-9a-zA-Z]([-.\w]*[0-9a-zA-Z])*(:(0-9)*)*(\/?)([a-zA-Z0-9\-\.\?\,\'\/\\\+&amp;%\$#_]*)?$";
        private const string scriptFilter = @"\w+:\/{2}[\d\w-]+(\.[\d\w-]+)*(?:(?:\/[^\s/]*))*";
        public CASchedulePage()
        {
            dailyStartTimeList = new InputFormDropDownList();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            if (!this.ClientScript.IsClientScriptBlockRegistered(this.GetType(), "CustomJS"))
            {
                string scrp = "function ResetSections(){  var minutelysection = document.getElementById('minutelySection'); var dailysection = document.getElementById('dailySection'); var weeklysection = document.getElementById('weeklySection'); var hourlysection = document.getElementById('hourlySection'); dailysection.style.display='none'; weeklysection.style.display='none'; hourlysection.style.display='none'; minutelysection.style.display='none'; }";
                scrp += "function Schedule_ShowSection(sectionid){ var section = document.getElementById(sectionid); section.style.display='inline'; }";
                scrp += "function CheckKeyIsNumber(){ var key = window.event.keyCode; return (key >= 48 && key <= 57); }";
                this.ClientScript.RegisterStartupScript(this.GetType(), "CustomJS", scrp, true);
            }

            if (!Page.IsPostBack)
            {
                AddItemsToStartTimeList(dailyStartTimeList);
                AddItemsToStartTimeList(weeklyStartTimeList);

                var url = Request.QueryString["params"];

                if (validUrl(url))
                {
                   FilterPageContent(url);
                }
      
            }
            ResetSections();
            Schedule_ShowSection();
        }

        protected void FilterPageContent(string xmlSchedule)
        {
            if (!String.IsNullOrEmpty(xmlSchedule))
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.InnerXml = xmlSchedule;
                XmlNode node = xmlDoc.DocumentElement;

                string type = node["Type"].InnerText;
                if (type.Equals("Hourly"))
                {
                    scheduleHourly.Checked = true;
                    scheduleDaily.Checked = false;
                    scheduleWeekly.Checked = false;
                    string hours = node["Hours"].InnerText;
                    hourlyIntervalText.Text = hours;
                }
                else if (type.Equals("Minutely"))
                {
                    scheduleMinutely.Checked = true;
                    scheduleHourly.Checked = false;
                    scheduleDaily.Checked = false;
                    scheduleWeekly.Checked = false;
                    string minutes = node["Minutes"].InnerText;
                    minutelyIntervalText.Text = minutes;
                }
                else if (type.Equals("Daily"))
                {
                    scheduleHourly.Checked = false;
                    scheduleDaily.Checked = true;
                    scheduleWeekly.Checked = false;
                    dailyIntervalText.Text = node["Days"].InnerText;
                    string from = node["From"].InnerText;
                    ListItem item = dailyStartTimeList.Items.FindByText(from);
                    dailyStartTimeList.SelectedIndex = dailyStartTimeList.Items.IndexOf(item);
                }
                else
                {
                    scheduleHourly.Checked = false;
                    scheduleDaily.Checked = false;
                    scheduleWeekly.Checked = true;
                    weeklyIntervalText.Text = node["Weeks"].InnerText;
                    string from = node["From"].InnerText;
                    ListItem item = weeklyStartTimeList.Items.FindByText(from);
                    weeklyStartTimeList.SelectedIndex = weeklyStartTimeList.Items.IndexOf(item);
                    string weekdays = node["WeekDays"].InnerText;
                    if (weekdays.Contains("Mon"))
                        weeklyDayMonday.Checked = true;
                    if (weekdays.Contains("Tue"))
                        weeklyDayTuesday.Checked = true;
                    if (weekdays.Contains("Wed"))
                        weeklyDayWednesday.Checked = true;
                    if (weekdays.Contains("Thu"))
                        weeklyDayThursday.Checked = true;
                    if (weekdays.Contains("Fri"))
                        weeklyDayFriday.Checked = true;
                    if (weekdays.Contains("Sat"))
                        weeklyDaySaturday.Checked = true;
                    if (weekdays.Contains("Sun"))
                        weeklyDaySunday.Checked = true;
                }
            }
        }

        protected void ResetSections()
        {
            dailySection.Style = "display:none";
            weeklySection.Style = "display:none";
            hourlySection.Style = "display:none";
            minutelySection.Style = "display:none";
        }

        protected void Schedule_ShowSection()
        {
            if (scheduleDaily.Checked)
            {
                dailySection.Style = "display:inline";
            }
            if (scheduleWeekly.Checked)
            {
                weeklySection.Style = "display:inline";
            }
            if (scheduleHourly.Checked)
            {
                hourlySection.Style = "display:inline";
            }
            if (scheduleMinutely.Checked)
            {
                minutelySection.Style = "display:inline";
            }
        }

        private void AddItemsToStartTimeList(Microsoft.SharePoint.Portal.WebControls.InputFormDropDownList startTimeList)
        {
            ListItem item = new ListItem("12:00 AM", "0");
            startTimeList.Items.Add(item);
            item = new ListItem("1:00 AM", "1");
            item.Selected = true;
            startTimeList.Items.Add(item);
            item = new ListItem("2:00 AM", "2");
            startTimeList.Items.Add(item);
            item = new ListItem("3:00 AM", "3");
            startTimeList.Items.Add(item);
            item = new ListItem("4:00 AM", "4");
            startTimeList.Items.Add(item);
            item = new ListItem("5:00 AM", "5");
            startTimeList.Items.Add(item);
            item = new ListItem("6:00 AM", "6");
            startTimeList.Items.Add(item);
            item = new ListItem("7:00 AM", "7");
            startTimeList.Items.Add(item);
            item = new ListItem("8:00 AM", "8");
            startTimeList.Items.Add(item);
            item = new ListItem("9:00 AM", "9");
            startTimeList.Items.Add(item);
            item = new ListItem("10:00 AM", "10");
            startTimeList.Items.Add(item);
            item = new ListItem("11:00 AM", "11");
            startTimeList.Items.Add(item);
            item = new ListItem("12:00 PM", "12");
            startTimeList.Items.Add(item);
            item = new ListItem("1:00 PM", "13");
            startTimeList.Items.Add(item);
            item = new ListItem("2:00 PM", "14");
            startTimeList.Items.Add(item);
            item = new ListItem("3:00 PM", "15");
            startTimeList.Items.Add(item);
            item = new ListItem("4:00 PM", "16");
            startTimeList.Items.Add(item);
            item = new ListItem("5:00 PM", "17");
            startTimeList.Items.Add(item);
            item = new ListItem("6:00 PM", "18");
            startTimeList.Items.Add(item);
            item = new ListItem("7:00 PM", "19");
            startTimeList.Items.Add(item);
            item = new ListItem("8:00 PM", "20");
            startTimeList.Items.Add(item);
            item = new ListItem("9:00 PM", "21");
            startTimeList.Items.Add(item);
            item = new ListItem("10:00 PM", "22");
            startTimeList.Items.Add(item);
            item = new ListItem("11:00 PM", "23");
            startTimeList.Items.Add(item);
        }

        protected void DailyIntervalValidation(object source, ServerValidateEventArgs args)
        {
            Int32 value = Int32.Parse(dailyIntervalText.Text);

            args.IsValid = true;
            if (scheduleDaily.Checked && !(value > 0 && value < 1000))
            {
                args.IsValid = false;
            }
        }

        protected void WeeklyIntervalValidation(object source, ServerValidateEventArgs args)
        {
            Int32 value = Int32.Parse(weeklyIntervalText.Text);

            args.IsValid = true;
            if (scheduleWeekly.Checked && !(value > 0 && value < 1000))
            {
                args.IsValid = false;
            }
        }

        protected void DaysOfWeekValidation(object source, ServerValidateEventArgs args)
        {
            args.IsValid = true;

            if (scheduleWeekly.Checked &&
                !(weeklyDayMonday.Checked ||
                weeklyDayTuesday.Checked ||
                weeklyDayWednesday.Checked ||
                weeklyDayThursday.Checked ||
                weeklyDayFriday.Checked ||
                weeklyDaySaturday.Checked ||
                weeklyDaySunday.Checked))
            {
                args.IsValid = false;
            }
        }

        protected void HourlyIntervalValidation(object source, ServerValidateEventArgs args)
        {
            Int32 value = Int32.Parse(hourlyIntervalText.Text);

            args.IsValid = true;
            if (scheduleHourly.Checked && !(value > 0 && value < 100))
            {
                args.IsValid = false;
            }
        }

        protected void MinutelyIntervalValidation(object source, ServerValidateEventArgs args)
        {
            Int32 value = Int32.Parse(minutelyIntervalText.Text);

            args.IsValid = true;
            if (scheduleMinutely.Checked && !(value > 0 && value < 100))
            {
                args.IsValid = false;
            }
        }

        private string ConstructDisplaySchedule()
        {
            string schedule = "";
            DateTime curr = DateTime.Now;

            if (scheduleHourly.Checked)
            {
                if (validInput(hourlyIntervalText.Text))
                {
                    schedule = "Every " + hourlyIntervalText.Text + " hour(s) from ";
                    schedule += (curr.ToShortTimeString() + ", ");
                }
               
            }
            else if (scheduleMinutely.Checked)
            {
                if (validInput(minutelyIntervalText.Text))
                {
                    schedule = "Every " + minutelyIntervalText.Text + " minute(s) from ";

                    schedule += (curr.ToShortTimeString() + ", ");
                }
            }
            else if (scheduleDaily.Checked && validInput(dailyIntervalText.Text))
            {
                schedule = "At " + dailyStartTimeList.SelectedItem.Text;
                schedule += (" every " + dailyIntervalText.Text + " day(s), ");
            }
            else
            {
                if (validInput(weeklyIntervalText.Text))
                {
                    schedule = "At " + weeklyStartTimeList.SelectedItem.Text + " every ";

                    if (weeklyDayMonday.Checked)
                        schedule += "Mon,";
                    if (weeklyDayTuesday.Checked)
                        schedule += "Tue,";
                    if (weeklyDayWednesday.Checked)
                        schedule += "Wed,";
                    if (weeklyDayThursday.Checked)
                        schedule += "Thu,";
                    if (weeklyDayFriday.Checked)
                        schedule += "Fri,";
                    if (weeklyDaySaturday.Checked)
                        schedule += "Sat,";
                    if (weeklyDaySunday.Checked)
                        schedule += "Sun,";
                    schedule.Remove(schedule.Length - 1);
                    schedule += (" of every " + weeklyIntervalText.Text + " week(s), ");
                }
            }

            schedule += ("starting " + curr.ToShortDateString());

            return schedule;
        }

        private string ConstructXmlSchedule()
        {
            string schedule = "<Schedule>";
            DateTime curr = DateTime.Now;

            if (scheduleHourly.Checked && validInput(hourlyIntervalText.Text))
            {
                schedule += "<Type>Hourly</Type>";
                schedule += ("<Hours>" + hourlyIntervalText.Text + "</Hours>");
                schedule += ("<From>" + curr.ToShortTimeString() + "</From>");
            }

            else if (scheduleMinutely.Checked && validInput(minutelyIntervalText.Text))
            {
                schedule += "<Type>Minutely</Type>";
                schedule += ("<Minutes>" + minutelyIntervalText.Text + "</Minutes>");
                schedule += ("<From>" + curr.ToShortTimeString() + "</From>");
            }
            else if (scheduleDaily.Checked && validInput(dailyIntervalText.Text))
            {
                schedule += "<Type>Daily</Type>";
                schedule += ("<Days>" + dailyIntervalText.Text + "</Days>");
                schedule += ("<From>" + dailyStartTimeList.SelectedItem.Text + "</From>");
            }
            else
            {
                if (validInput(weeklyIntervalText.Text))
                {
                    schedule += "<Type>Weekly</Type>";
                    schedule += ("<Weeks>" + weeklyIntervalText.Text + "</Weeks>");
                    schedule += "<WeekDays>";
                    if (weeklyDayMonday.Checked)
                        schedule += "Mon,";
                    if (weeklyDayTuesday.Checked)
                        schedule += "Tue,";
                    if (weeklyDayWednesday.Checked)
                        schedule += "Wed,";
                    if (weeklyDayThursday.Checked)
                        schedule += "Thu,";
                    if (weeklyDayFriday.Checked)
                        schedule += "Fri,";
                    if (weeklyDaySaturday.Checked)
                        schedule += "Sat,";
                    if (weeklyDaySunday.Checked)
                        schedule += "Sun,";
                    schedule.Remove(schedule.Length - 1);
                    schedule += "</WeekDays>";
                    schedule += ("<From>" + weeklyStartTimeList.SelectedItem.Text + "</From>");
                }
            }

            schedule += ("<StartDate>" + curr.ToShortDateString() + "</StartDate>");
            schedule += "</Schedule>";

            return schedule;
        }

        protected void OnClickOK(object sender, EventArgs e)
        {
            if (this.Page.IsValid)
            {
                DisplaySchedule.Text = ConstructDisplaySchedule();
                XmlSchedule.Text = ConstructXmlSchedule();
                string script = "<script type=\"text/javascript\">var disschedule=\"";
                script += DisplaySchedule.Text;
                script += "\"; var xmlschedule=\"";
                script += XmlSchedule.Text;
                script += "\"; var ret=new Array(disschedule, xmlschedule);";
               
                script += "window.frameElement.commonModalDialogClose(1, ret);</script>";

                this.Page.Response.Write(script);
                this.Page.Response.End();
            }
        }

        protected void OnClickCancel(object sender, EventArgs e)
        {
            string script = "<script type=\"text/javascript\">window.frameElement.commonModalDialogClose(0, null);</script>";

            this.Page.Response.Write(script);
            this.Page.Response.End();
        }

        
        private bool validUrl(string input)
        {
           if (string.IsNullOrEmpty(input))
           {
               return true;
           }
           else
           {
               return validInput(input);
           }
        }
    

        private bool validInput(string input)
        {
            return (!Regex.IsMatch(input, urlFilter) && !Regex.IsMatch(input, scriptFilter));
        }
    }
}
