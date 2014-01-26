﻿// <copyright>
// Copyright 2013 by the Spark Development Network
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//
using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.UI;
using DDay.iCal;
using Rock;
using Rock.Constants;
using Rock.Data;
using Rock.Model;
using Rock.Web;
using Rock.Web.UI;
using Rock.Web.UI.Controls;

namespace RockWeb.Blocks.Core
{
    [DisplayName( "Schedule Detail" )]
    [Category( "Core" )]
    [Description( "Displays the details of the given schedule." )]
    public partial class ScheduleDetail : RockBlock, IDetailBlock
    {
        #region Control Methods

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            if ( !Page.IsPostBack )
            {
                string itemId = PageParameter( "scheduleId" );
                string parentCategoryId = PageParameter( "parentCategoryId" );
                if ( !string.IsNullOrWhiteSpace( itemId ) )
                {
                    if ( string.IsNullOrWhiteSpace( parentCategoryId ) )
                    {
                        ShowDetail( "ScheduleId", int.Parse( itemId ) );
                    }
                    else
                    {
                        ShowDetail( "ScheduleId", int.Parse( itemId ), int.Parse( parentCategoryId ) );
                    }
                }
                else
                {
                    pnlDetails.Visible = false;
                }
            }
        }

        #endregion

        #region Edit Events

        /// <summary>
        /// Handles the Click event of the btnEdit control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        protected void btnEdit_Click( object sender, EventArgs e )
        {
            var service = new ScheduleService();
            var item = service.Get( int.Parse( hfScheduleId.Value ) );
            ShowEditDetails( item );
        }

        /// <summary>
        /// Handles the Click event of the btnSave control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        protected void btnSave_Click( object sender, EventArgs e )
        {
            Schedule schedule;
            ScheduleService scheduleService = new ScheduleService();

            int scheduleId = int.Parse( hfScheduleId.Value );

            if ( scheduleId == 0 )
            {
                schedule = new Schedule();
                scheduleService.Add( schedule, CurrentPersonId );
            }
            else
            {
                schedule = scheduleService.Get( scheduleId );
            }

            schedule.Name = tbScheduleName.Text;
            schedule.Description = tbScheduleDescription.Text;
            schedule.iCalendarContent = sbSchedule.iCalendarContent;

            schedule.CategoryId = cpCategory.SelectedValueAsInt();

            int offsetMins = int.MinValue;
            if ( int.TryParse( nbStartOffset.Text, out offsetMins ) )
            {
                schedule.CheckInStartOffsetMinutes = offsetMins;
            }
            else
            {
                schedule.CheckInStartOffsetMinutes = null;
            }

            offsetMins = int.MinValue;
            if ( int.TryParse( nbEndOffset.Text, out offsetMins ) )
            {
                schedule.CheckInEndOffsetMinutes = offsetMins;
            }
            else
            {
                schedule.CheckInEndOffsetMinutes = null;
            }

            if ( !schedule.IsValid )
            {
                // Controls will render the error messages
                return;
            }

            RockTransactionScope.WrapTransaction( () =>
            {
                scheduleService.Save( schedule, CurrentPersonId );
            } );

            var qryParams = new Dictionary<string, string>();
            qryParams["ScheduleId"] = schedule.Id.ToString();
            NavigateToPage( RockPage.Guid, qryParams );
        }

        /// <summary>
        /// Handles the Click event of the btnCancel control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        protected void btnCancel_Click( object sender, EventArgs e )
        {
            if ( hfScheduleId.Value.Equals( "0" ) )
            {
                // Cancelling on Add.  Return to tree view with parent category selected
                var qryParams = new Dictionary<string, string>();

                string parentCategoryId = PageParameter( "parentCategoryId" );
                if ( !string.IsNullOrWhiteSpace( parentCategoryId ) )
                {
                    qryParams["CategoryId"] = parentCategoryId;
                }
                NavigateToPage( RockPage.Guid, qryParams );
            }
            else
            {
                // Cancelling on Edit.  Return to Details
                var service = new ScheduleService();
                var item = service.Get( int.Parse( hfScheduleId.Value ) );
                ShowReadonlyDetails( item );
            }
        }

        /// <summary>
        /// Handles the Click event of the btnDelete control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        protected void btnDelete_Click( object sender, EventArgs e )
        {
            int? categoryId = null;

            var service = new ScheduleService();
            var item = service.Get( int.Parse( hfScheduleId.Value ) );

            if ( item != null )
            {
                string errorMessage;
                if ( !service.CanDelete( item, out errorMessage ) )
                {
                    ShowReadonlyDetails( item );
                    mdDeleteWarning.Show( errorMessage, ModalAlertType.Information );
                }
                else
                {
                    categoryId = item.CategoryId;

                    service.Delete( item, CurrentPersonId );
                    service.Save( item, CurrentPersonId );

                    // reload page, selecting the deleted data view's parent
                    var qryParams = new Dictionary<string, string>();
                    if ( categoryId != null )
                    {
                        qryParams["CategoryId"] = categoryId.ToString();
                    }

                    NavigateToPage( RockPage.Guid, qryParams );
                }
            }
        }

        #endregion

        /// <summary>
        /// Handles the SaveSchedule event of the sbSchedule control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void sbSchedule_SaveSchedule( object sender, EventArgs e )
        {
            UpdateHelpText();
        }

        #region Internal Methods

        /// <summary>
        /// Updates the help text.
        /// </summary>
        private void UpdateHelpText()
        {
            var fakeSchedule = new Rock.Model.Schedule();
            fakeSchedule.iCalendarContent = sbSchedule.iCalendarContent;
            sbSchedule.ToolTip = fakeSchedule.ToFriendlyScheduleText();

            hbSchedulePreview.Text = @"<strong>iCalendar Content</strong>
<div style='white-space: pre' Font-Names='Consolas' Font-Size='9'><br />" + sbSchedule.iCalendarContent + "</div>";

            iCalendar calendar = iCalendar.LoadFromStream( new StringReader( sbSchedule.iCalendarContent ) ).First() as iCalendar;
            DDay.iCal.Event calendarEvent = calendar.Events[0] as Event;

            if ( calendarEvent.DTStart != null )
            {
                List<Occurrence> nextOccurrences = calendar.GetOccurrences( RockDateTime.Now.Date, RockDateTime.Now.Date.AddYears( 1 ) ).Take( 26 ).ToList();

                string listHtml = "<hr /><strong>Occurrences Preview</strong><ul>";
                foreach ( var occurrence in nextOccurrences )
                {
                    if ( occurrence.Period.StartTime.Value.Date.Equals( occurrence.Period.EndTime.Value.Date ) )
                    {
                        listHtml += string.Format( "<li>{0} - {1} to {2} ( {3} hours) </li>", occurrence.Period.StartTime.Value.Date.ToShortDateString(), occurrence.Period.StartTime.Value.TimeOfDay.ToTimeString(), occurrence.Period.EndTime.Value.TimeOfDay.ToTimeString(), occurrence.Period.Duration.TotalHours.ToString( "#0.00" ) );
                    }
                    else
                    {
                        listHtml += string.Format( "<li>{0} to {1} ( {2} hours) </li>", occurrence.Period.StartTime.Value.ToString( "g" ), occurrence.Period.EndTime.Value.ToString( "g" ), occurrence.Period.Duration.TotalHours.ToString( "#0.00" ) );
                    }
                }

                listHtml += string.Format( "<li>{0}</li>", "..." );
                listHtml += "</ul>";

                hbSchedulePreview.Text += listHtml;
            }
        }

        /// <summary>
        /// Shows the detail.
        /// </summary>
        /// <param name="itemKey">The item key.</param>
        /// <param name="itemKeyValue">The item key value.</param>
        public void ShowDetail( string itemKey, int itemKeyValue )
        {
            ShowDetail( itemKey, itemKeyValue, null );
        }

        /// <summary>
        /// Shows the detail.
        /// </summary>
        /// <param name="itemKey">The item key.</param>
        /// <param name="itemKeyValue">The item key value.</param>
        /// <param name="parentCategoryId">The parent category id.</param>
        public void ShowDetail( string itemKey, int itemKeyValue, int? parentCategoryId )
        {
            pnlDetails.Visible = false;
            if ( itemKey != "ScheduleId" )
            {
                return;
            }

            var scheduleService = new ScheduleService();
            Schedule schedule = null;

            if ( !itemKeyValue.Equals( 0 ) )
            {
                schedule = new ScheduleService().Get( itemKeyValue );
            }
            else
            {
                schedule = new Schedule { Id = 0, CategoryId = parentCategoryId };
            }

            if ( schedule == null )
            {
                return;
            }

            pnlDetails.Visible = true;
            hfScheduleId.Value = schedule.Id.ToString();

            // render UI based on Authorized and IsSystem
            bool readOnly = false;

            nbEditModeMessage.Text = string.Empty;
            if ( !IsUserAuthorized( "Edit" ) )
            {
                readOnly = true;
                nbEditModeMessage.Text = EditModeMessage.ReadOnlyEditActionNotAllowed( Schedule.FriendlyTypeName );
            }

            if ( readOnly )
            {
                btnEdit.Visible = false;
                btnDelete.Visible = false;
                ShowReadonlyDetails( schedule );
            }
            else
            {
                btnEdit.Visible = true;
                string errorMessage = string.Empty;
                btnDelete.Visible = scheduleService.CanDelete( schedule, out errorMessage );
                if ( schedule.Id > 0 )
                {
                    ShowReadonlyDetails( schedule );
                }
                else
                {
                    ShowEditDetails( schedule );
                }
            }

        }

        /// <summary>
        /// Shows the edit details.
        /// </summary>
        /// <param name="schedule">The schedule.</param>
        public void ShowEditDetails( Schedule schedule )
        {
            if ( schedule.Id > 0 )
            {
                lActionTitle.Text = ActionTitle.Edit( Schedule.FriendlyTypeName ).FormatAsHtmlTitle();
            }
            else
            {
                lActionTitle.Text = ActionTitle.Add( Schedule.FriendlyTypeName ).FormatAsHtmlTitle();
            }

            SetEditMode( true );

            tbScheduleName.Text = schedule.Name;
            tbScheduleDescription.Text = schedule.Description;

            sbSchedule.iCalendarContent = schedule.iCalendarContent;
            UpdateHelpText();

            cpCategory.SetValue( schedule.CategoryId );

            nbStartOffset.Text = schedule.CheckInStartOffsetMinutes.HasValue ? schedule.CheckInStartOffsetMinutes.ToString() : string.Empty;
            nbEndOffset.Text = schedule.CheckInEndOffsetMinutes.HasValue ? schedule.CheckInEndOffsetMinutes.Value.ToString() : string.Empty;
        }

        /// <summary>
        /// Shows the readonly details.
        /// </summary>
        /// <param name="schedule">The schedule.</param>
        private void ShowReadonlyDetails( Schedule schedule )
        {
            SetEditMode( false );
            hfScheduleId.SetValue( schedule.Id );
            lReadOnlyTitle.Text = schedule.Name.FormatAsHtmlTitle();

            var calendarEvent = schedule.GetCalenderEvent();
            string occurrenceText = string.Empty;
            if ( calendarEvent != null )
            {
                if ( calendarEvent.DTStart != null )
                {
                    var occurrences = calendarEvent.GetOccurrences( RockDateTime.Now.Date, RockDateTime.Now.Date.AddYears( 1 ) );
                    if ( occurrences.Any() )
                    {
                        var occurrence = occurrences[0];

                        if ( occurrence.Period.StartTime.Value.Date.Equals( occurrence.Period.EndTime.Value.Date ) )
                        {
                            occurrenceText += string.Format( "{0} - {1} to {2} ( {3} hours)", occurrence.Period.StartTime.Value.Date.ToShortDateString(), occurrence.Period.StartTime.Value.TimeOfDay.ToTimeString(), occurrence.Period.EndTime.Value.TimeOfDay.ToTimeString(), occurrence.Period.Duration.TotalHours.ToString( "#0.00" ) );
                        }
                        else
                        {
                            occurrenceText += string.Format( "<li>{0} to {1} ( {2} hours) </li>", occurrence.Period.StartTime.Value.ToString( "g" ), occurrence.Period.EndTime.Value.ToString( "g" ), occurrence.Period.Duration.TotalHours.ToString( "#0.00" ) );
                        }
                    }
                }
            }

            DescriptionList descriptionList = new DescriptionList()
                .Add( "Description", schedule.Description ?? string.Empty )
                .Add( "Next Occurrence", occurrenceText )
                .Add( "Category", schedule.Category != null ? schedule.Category.Name : string.Empty );

            if ( schedule.CheckInStartOffsetMinutes.HasValue )
            {
                descriptionList.Add( "Check-in Starts", schedule.CheckInStartOffsetMinutes.Value.ToString() + " minutes before start of schedule" );
            }

            if ( schedule.CheckInEndOffsetMinutes.HasValue )
            {
                descriptionList.Add( "Check-in Ends", schedule.CheckInEndOffsetMinutes.Value.ToString() + " minutes after start of schedule" );
            }

            lblMainDetails.Text = descriptionList.Html;
        }

        /// <summary>
        /// Sets the edit mode.
        /// </summary>
        /// <param name="editable">if set to <c>true</c> [editable].</param>
        private void SetEditMode( bool editable )
        {
            pnlEditDetails.Visible = editable;
            pnlViewDetails.Visible = !editable;
        }

        #endregion
    }
}