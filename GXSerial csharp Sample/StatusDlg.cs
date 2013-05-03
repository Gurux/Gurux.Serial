//
// --------------------------------------------------------------------------
//  Gurux Ltd
// 
//
//
// Filename:        $HeadURL: svn://utopia/projects/Old/GuruxSerial/GXSerial%20csharp%20Sample/StatusDlg.cs $
//
// Version:         $Revision: 3104 $,
//                  $Date: 2010-12-03 13:43:16 +0200 (pe, 03 joulu 2010) $
//                  $Author: kurumi $
//
// Copyright (c) Gurux Ltd
//
//---------------------------------------------------------------------------
//
//  DESCRIPTION
//
// This file is a part of Gurux Device Framework.
//
// Gurux Device Framework is Open Source software; you can redistribute it
// and/or modify it under the terms of the GNU General Public License 
// as published by the Free Software Foundation; version 2 of the License.
// Gurux Device Framework is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of 
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. 
// See the GNU General Public License for more details.
// 
// More information of Gurux Serial : http://www.gurux.org/GXSerial
//
// This code is licensed under the GNU General Public License v2. 
// Full text may be retrieved at http://www.gnu.org/licenses/gpl-2.0.txt
//---------------------------------------------------------------------------

using System;
using System.Windows.Forms;

namespace GXSerialSample
{
	internal partial class StatusDlg : System.Windows.Forms.Form
	{
        public Gurux.Serial.GXSerial GXSerial1;	

        /// <summary>
        /// Constructor.
        /// </summary>
        public StatusDlg(Gurux.Serial.GXSerial Serial)
        {
            //This call is required by the Windows Form Designer.
            InitializeComponent();
            GXSerial1 = Serial;
        }
        
        /// <summary>
        /// Start listen serial port events when dialog is shown.
        /// </summary>
        /// <param name="eventSender"></param>
        /// <param name="eventArgs"></param>
		private void StatusDlg_Load(System.Object eventSender, System.EventArgs eventArgs)
		{
            ErrorLed.BackColor = BreakLed.BackColor = RingLed.BackColor = DSRLed.BackColor = DCDLed.BackColor = CTSLed.BackColor = TXDLed.BackColor = RXDLed.BackColor = System.Drawing.Color.Gray;
            //Mikko GXSerial1.OnDiagnosticEventsOccurred += new Gurux.Serial.DiagnosticEventsOccurredEventHandler(GXSerial1_OnDiagnosticEventsOccurred);
		}
        /*
        /// <summary>
        /// Update UI when diagnostic event occured.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="Events"></param>
        private void GXSerial1_OnDiagnosticEventsOccurred(object sender)
		{
            EventReset.Enabled = false;
			//if break is set
            if ((Events & Gurux.Serial.Diagnostic.Break) != 0)
			{
                BreakLed.BackColor = System.Drawing.Color.Green;
			}
			else
			{
                BreakLed.BackColor = System.Drawing.Color.Gray;
			}
			
			//Carrier detected
            if ((Events & Gurux.Serial.Diagnostic.CdChanged) != 0)
			{
                DCDLed.BackColor = System.Drawing.Color.Green;
			}
			else
			{
                DCDLed.BackColor = System.Drawing.Color.Gray;
			}
			
			//If Clear to send is set
            if ((Events & Gurux.Serial.Diagnostic.CtsChanged) != 0)
			{
                CTSLed.BackColor = System.Drawing.Color.Green;
			}
			else
			{
                CTSLed.BackColor = System.Drawing.Color.Gray;
			}
			
			//If Data Set Ready is set
            if ((Events & Gurux.Serial.Diagnostic.DsrChanged) != 0)
			{
                DSRLed.BackColor = System.Drawing.Color.Green;
			}
			else
			{
                DSRLed.BackColor = System.Drawing.Color.Gray;
			}
			
			//If error frame has occurred
            if ((Events & Gurux.Serial.Diagnostic.ErrorFrame) != 0)
			{
                ErrorLed.BackColor = System.Drawing.Color.Red;
			}			
			//If buffer overrun has occured
            else if ((Events & Gurux.Serial.Diagnostic.ErrorOverrun) != 0)
			{
                ErrorLed.BackColor = System.Drawing.Color.Red;
			}
            //If parity error has occurred
			else if ((Events & Gurux.Serial.Diagnostic.ErrorParity) != 0)
			{
                ErrorLed.BackColor = System.Drawing.Color.Red;
			}
			else
			{
                ErrorLed.BackColor = System.Drawing.Color.Gray;
			}
			
			//If charachter is received
            if ((Events & Gurux.Serial.Diagnostic.Chars) != 0)
			{
                RXDLed.BackColor = System.Drawing.Color.Green;
			}
			else
			{
                RXDLed.BackColor = System.Drawing.Color.Gray;
			}
			
			//If ring is detected
            if ((Events & Gurux.Serial.Diagnostic.Ring) != 0)
			{
                RingLed.BackColor = System.Drawing.Color.Green;
			}
			else
			{
                RingLed.BackColor = System.Drawing.Color.Gray;
			}
			
			//Send char
            if ((Events & Gurux.Serial.Diagnostic.SendChars) != 0)
			{
                TXDLed.BackColor = System.Drawing.Color.Green;
			}
            EventReset.Enabled = true;
		} 
         * */

        /// <summary>
        /// Event's are reset every second.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EventReset_Tick(object sender, EventArgs e)
        {
            BreakLed.BackColor = RingLed.BackColor = DCDLed.BackColor = TXDLed.BackColor = RXDLed.BackColor = System.Drawing.Color.Gray;
        }
	}
}
