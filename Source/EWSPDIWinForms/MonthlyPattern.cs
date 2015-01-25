//===============================================================================================================
// System  : EWSoftware.PDI Windows Forms Controls
// File    : MonthlyPattern.cs
// Author  : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 10/21/2014
// Note    : Copyright 2004-2014, Eric Woodruff, All rights reserved
// Compiler: Microsoft Visual C#
//
// This file contains one of several user controls that are combined to allow the editing of various recurrence
// parameters.  This one is used to specify the settings for a monthly recurrence pattern.
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code and can be found at the project website: https://github.com/EWSoftware/PDI.
// This notice, the author's name, and all copyright notices must remain intact in all applications,
// documentation, and source files.
//
//    Date     Who  Comments
// ==============================================================================================================
// 01/20/2004  EFW  Created the code
//===============================================================================================================

using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace EWSoftware.PDI.Windows.Forms
{
	/// <summary>
    /// This user control is used to specify the settings for a monthly recurrence pattern
	/// </summary>
    [ToolboxItem(false)]
	internal sealed partial class MonthlyPattern : System.Windows.Forms.UserControl
	{
        #region Constructor
        //=====================================================================

        /// <summary>
        /// Constructor
        /// </summary>
		public MonthlyPattern()
		{
			InitializeComponent();

            cboOccurrence.DisplayMember = "Display";
            cboOccurrence.ValueMember = "Value";
            cboOccurrence.DataSource = RecurOptsDataSource.DayOccurrences;

            cboDOW.DisplayMember = "Display";
            cboDOW.ValueMember = "Value";
            cboDOW.DataSource = RecurOptsDataSource.DaysOfWeek;
		}
        #endregion

        #region Helper methods
        //=====================================================================

        /// <summary>
        /// This is called to get the values from the controls and set them in the specified recurrence object
        /// </summary>
        /// <param name="recurrence">The recurrence object to which the settings are applied</param>
        /// <remarks>It is assumed that the recurrence object has been reset to a default state</remarks>
        public void GetValues(Recurrence recurrence)
        {
            int instance;
            DaysOfWeek rd;

            if(recurrence.Frequency != RecurFrequency.Monthly)
                return;

            if(rbDayXEveryYMonths.Checked)
            {
                recurrence.Interval = (int)udcMonths.Value;
                recurrence.ByMonthDay.Add((int)udcDay.Value);
            }
            else
            {
                recurrence.Interval = (int)udcDOWMonths.Value;

                // If it's a single day, use ByDay.  If it's a combination, use ByDay with BySetPos
                rd = (DaysOfWeek)cboDOW.SelectedValue;
                instance = ((DayOccurrence)cboOccurrence.SelectedValue == DayOccurrence.Last) ? -1 :
                    (int)cboOccurrence.SelectedValue;

                switch(rd)
                {
                    case DaysOfWeek.EveryDay:
                        recurrence.BySetPos.Add(instance);
                        recurrence.ByDay.AddRange(new DayOfWeek[] { DayOfWeek.Sunday, DayOfWeek.Monday,
                            DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday,
                            DayOfWeek.Saturday });
                        break;

                    case DaysOfWeek.Weekdays:
                        recurrence.BySetPos.Add(instance);
                        recurrence.ByDay.AddRange(new DayOfWeek[] { DayOfWeek.Monday, DayOfWeek.Tuesday,
                            DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday });
                        break;

                    case DaysOfWeek.Weekends:
                        recurrence.BySetPos.Add(instance);
                        recurrence.ByDay.AddRange(new DayOfWeek[] { DayOfWeek.Sunday, DayOfWeek.Saturday });
                        break;

                    default:
                        recurrence.ByDay.Add(new DayInstance(instance, DateUtils.ToDayOfWeek(rd)));
                        break;
                }
            }
        }

        /// <summary>
        /// This is called to set the values for the controls based on the current recurrence settings
        /// </summary>
        /// <param name="rrecurrence">The recurrence object from which to get the settings</param>
        public void SetValues(Recurrence rrecurrence)
        {
            DaysOfWeek rd = DaysOfWeek.None;

            rbDayXEveryYMonths.Checked = true;

            // Use default values if not a monthly frequency
            if(rrecurrence.Frequency != RecurFrequency.Monthly)
            {
                udcDay.Value = udcMonths.Value = udcDOWMonths.Value = 1;
                cboOccurrence.SelectedIndex = cboDOW.SelectedIndex = 0;
            }
            else
            {
                if(rrecurrence.ByDay.Count == 0)
                {
                    if(rrecurrence.ByMonthDay.Count != 0)
                        udcDay.Value = rrecurrence.ByMonthDay[0];
                    else
                        udcDay.Value = rrecurrence.StartDateTime.Day;

                    udcMonths.Value = (rrecurrence.Interval < 1000) ? rrecurrence.Interval : 999;

                    cboOccurrence.SelectedIndex = cboDOW.SelectedIndex = 0;
                    udcDOWMonths.Value = 1;
                }
                else
                {
                    rbDayOfWeek.Checked = true;

                    udcDay.Value = udcMonths.Value = 1;
                    udcDOWMonths.Value = (rrecurrence.Interval < 1000) ? rrecurrence.Interval : 999;

                    // If it's a single day, use ByDay.  If it's a combination, use ByDay with BySetPos.
                    if(rrecurrence.ByDay.Count == 1)
                    {
                        cboOccurrence.SelectedValue = (rrecurrence.ByDay[0].Instance < 1 ||
                          rrecurrence.ByDay[0].Instance > 4) ? DayOccurrence.Last :
                            (DayOccurrence)rrecurrence.ByDay[0].Instance;

                        cboDOW.SelectedValue = DateUtils.ToDaysOfWeek(rrecurrence.ByDay[0].DayOfWeek);
                    }
                    else
                    {
                        if(rrecurrence.BySetPos.Count == 0)
                            cboOccurrence.SelectedIndex = 0;
                        else
                            cboOccurrence.SelectedValue = (rrecurrence.BySetPos[0] < 1 ||
                              rrecurrence.BySetPos[0] > 4) ? DayOccurrence.Last :
                                (DayOccurrence)rrecurrence.BySetPos[0];

                        // Figure out days used
                        foreach(DayInstance di in rrecurrence.ByDay)
                            switch(di.DayOfWeek)
                            {
                                case DayOfWeek.Sunday:
                                    rd |= DaysOfWeek.Sunday;
                                    break;

                                case DayOfWeek.Monday:
                                    rd |= DaysOfWeek.Monday;
                                    break;

                                case DayOfWeek.Tuesday:
                                    rd |= DaysOfWeek.Tuesday;
                                    break;

                                case DayOfWeek.Wednesday:
                                    rd |= DaysOfWeek.Wednesday;
                                    break;

                                case DayOfWeek.Thursday:
                                    rd |= DaysOfWeek.Thursday;
                                    break;

                                case DayOfWeek.Friday:
                                    rd |= DaysOfWeek.Friday;
                                    break;

                                case DayOfWeek.Saturday:
                                    rd |= DaysOfWeek.Saturday;
                                    break;
                            }

                        // If not EveryDay, Weekdays, or Weekends, force it to a single day of the week
                        if(rd == DaysOfWeek.None || (rd != DaysOfWeek.EveryDay && rd != DaysOfWeek.Weekdays &&
                          rd != DaysOfWeek.Weekends))
                            rd = DateUtils.ToDaysOfWeek(DateUtils.ToDayOfWeek(rd));

                        cboDOW.SelectedValue = rd;
                    }
                }
            }
        }
        #endregion

        #region Event handlers
        //=====================================================================

        /// <summary>
        /// Enable or disable the controls based on the selection
        /// </summary>
        private void Monthly_CheckedChanged(object sender, System.EventArgs e)
        {
            RadioButton r = (sender as RadioButton);

            udcDay.Enabled = udcMonths.Enabled = (r == rbDayXEveryYMonths);
            cboOccurrence.Enabled = cboDOW.Enabled = udcDOWMonths.Enabled =  (r == rbDayOfWeek);
        }
        #endregion
    }
}
