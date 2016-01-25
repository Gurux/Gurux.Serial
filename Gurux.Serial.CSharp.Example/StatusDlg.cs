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
		private void StatusDlg_Load(System.Object eventSender, System.EventArgs eventArgs)
		{
            GXSerial1.OnPinChanged += new System.IO.Ports.SerialPinChangedEventHandler(GXSerial1_OnPinChanged);
            GXSerial1.OnMediaStateChange += new Gurux.Common.MediaStateChangeEventHandler(GXSerial1_OnMediaStateChange);
		}

        /// <summary>
        /// Clear states when media is closed.
        /// </summary>
        void GXSerial1_OnMediaStateChange(object sender, Gurux.Common.MediaStateEventArgs e)
        {
            if (e.State == Gurux.Common.MediaState.Closing)
            {
                BreakLed.BackColor = RingLed.BackColor = RingLed.BackColor = RingLed.BackColor = RingLed.BackColor = System.Drawing.Color.Transparent;
            }                    
        }

        /// <summary>
        /// Show serial port states.
        /// </summary>
        void GXSerial1_OnPinChanged(object sender, System.IO.Ports.SerialPinChangedEventArgs e)
        {
            switch (e.EventType)
            {
                case System.IO.Ports.SerialPinChange.Break:
                    BreakLed.BackColor = GXSerial1.BreakState ? System.Drawing.Color.Red : System.Drawing.Color.Transparent;                    
                    break;
                case System.IO.Ports.SerialPinChange.CDChanged:
                    RingLed.BackColor = GXSerial1.CDHolding ? System.Drawing.Color.Red : System.Drawing.Color.Transparent;                    
                    break;
                case System.IO.Ports.SerialPinChange.CtsChanged:
                    RingLed.BackColor = GXSerial1.CtsHolding ? System.Drawing.Color.Red : System.Drawing.Color.Transparent;                    
                    break;
                case System.IO.Ports.SerialPinChange.DsrChanged:
                    RingLed.BackColor = GXSerial1.DsrHolding ? System.Drawing.Color.Red : System.Drawing.Color.Transparent;                    
                    break;
                case System.IO.Ports.SerialPinChange.Ring:                    
                    RingLed.BackColor = System.Drawing.Color.Red;
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Stop listen events when window is closed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StatusDlg_FormClosing(object sender, FormClosingEventArgs e)
        {
            GXSerial1.OnPinChanged -= new System.IO.Ports.SerialPinChangedEventHandler(GXSerial1_OnPinChanged);
            GXSerial1.OnMediaStateChange -= new Gurux.Common.MediaStateChangeEventHandler(GXSerial1_OnMediaStateChange);
        }
	}
}
