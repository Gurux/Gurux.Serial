//
// --------------------------------------------------------------------------
//  Gurux Ltd
// 
//
//
// Filename:        $HeadURL: svn://utopia/projects/Old/GuruxSerial/GXSerial%20csharp%20Sample/StatusDlg.Designer.cs $
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
	partial class StatusDlg
	{
		#region "Windows Form Designer generated code "
		[System.Diagnostics.DebuggerNonUserCode()]
        public StatusDlg()
		{
			//This call is required by the Windows Form Designer.
			InitializeComponent();
		}
		//Form overrides dispose to clean up the component list.
		[System.Diagnostics.DebuggerNonUserCode()]protected override void Dispose(bool Disposing)
		{
			if (Disposing)
			{
				if (components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose(Disposing);
		}
        //Required by the Windows Form Designer
		public System.Windows.Forms.Label ErrorLed;
		public System.Windows.Forms.Label ErrorLbl;
		public System.Windows.Forms.Label BreakLed;
		public System.Windows.Forms.Label BreakLbl;
		public System.Windows.Forms.Label RingLed;
		public System.Windows.Forms.Label RingLbl;
		public System.Windows.Forms.Label DSRLed;
		public System.Windows.Forms.Label DSRLbl;
		public System.Windows.Forms.Label DCDLbl;
		public System.Windows.Forms.Label DCDLed;
		public System.Windows.Forms.Label CTSLbl;
		public System.Windows.Forms.Label CTSLed;
		public System.Windows.Forms.Label TXDLbl;
		public System.Windows.Forms.Label TXDLed;
		public System.Windows.Forms.Label RXDLbl;
		public System.Windows.Forms.Label RXDLed;
		//NOTE: The following procedure is required by the Windows Form Designer
		//It can be modified using the Windows Form Designer.
		//Do not modify it using the code editor.
		[System.Diagnostics.DebuggerStepThrough()]private void InitializeComponent()
		{
            this.components = new System.ComponentModel.Container();
            this.ErrorLed = new System.Windows.Forms.Label();
            this.ErrorLbl = new System.Windows.Forms.Label();
            this.BreakLed = new System.Windows.Forms.Label();
            this.BreakLbl = new System.Windows.Forms.Label();
            this.RingLed = new System.Windows.Forms.Label();
            this.RingLbl = new System.Windows.Forms.Label();
            this.DSRLed = new System.Windows.Forms.Label();
            this.DSRLbl = new System.Windows.Forms.Label();
            this.DCDLbl = new System.Windows.Forms.Label();
            this.DCDLed = new System.Windows.Forms.Label();
            this.CTSLbl = new System.Windows.Forms.Label();
            this.CTSLed = new System.Windows.Forms.Label();
            this.TXDLbl = new System.Windows.Forms.Label();
            this.TXDLed = new System.Windows.Forms.Label();
            this.RXDLbl = new System.Windows.Forms.Label();
            this.RXDLed = new System.Windows.Forms.Label();
            this.EventReset = new System.Windows.Forms.Timer(this.components);
            this.SuspendLayout();
            // 
            // ErrorLed
            // 
            this.ErrorLed.BackColor = System.Drawing.Color.Red;
            this.ErrorLed.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.ErrorLed.ForeColor = System.Drawing.Color.Black;
            this.ErrorLed.Location = new System.Drawing.Point(8, 0);
            this.ErrorLed.Name = "ErrorLed";
            this.ErrorLed.Size = new System.Drawing.Size(17, 25);
            this.ErrorLed.TabIndex = 0;
            // 
            // ErrorLbl
            // 
            this.ErrorLbl.BackColor = System.Drawing.SystemColors.Control;
            this.ErrorLbl.Cursor = System.Windows.Forms.Cursors.Default;
            this.ErrorLbl.ForeColor = System.Drawing.SystemColors.ControlText;
            this.ErrorLbl.Location = new System.Drawing.Point(40, 8);
            this.ErrorLbl.Name = "ErrorLbl";
            this.ErrorLbl.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.ErrorLbl.Size = new System.Drawing.Size(65, 17);
            this.ErrorLbl.TabIndex = 7;
            this.ErrorLbl.Text = "Error";
            // 
            // BreakLed
            // 
            this.BreakLed.BackColor = System.Drawing.Color.Red;
            this.BreakLed.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.BreakLed.ForeColor = System.Drawing.Color.Black;
            this.BreakLed.Location = new System.Drawing.Point(8, 168);
            this.BreakLed.Name = "BreakLed";
            this.BreakLed.Size = new System.Drawing.Size(17, 25);
            this.BreakLed.TabIndex = 8;
            // 
            // BreakLbl
            // 
            this.BreakLbl.BackColor = System.Drawing.SystemColors.Control;
            this.BreakLbl.Cursor = System.Windows.Forms.Cursors.Default;
            this.BreakLbl.ForeColor = System.Drawing.SystemColors.ControlText;
            this.BreakLbl.Location = new System.Drawing.Point(40, 176);
            this.BreakLbl.Name = "BreakLbl";
            this.BreakLbl.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.BreakLbl.Size = new System.Drawing.Size(65, 17);
            this.BreakLbl.TabIndex = 6;
            this.BreakLbl.Text = "Break";
            // 
            // RingLed
            // 
            this.RingLed.BackColor = System.Drawing.Color.Red;
            this.RingLed.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.RingLed.ForeColor = System.Drawing.Color.Black;
            this.RingLed.Location = new System.Drawing.Point(8, 144);
            this.RingLed.Name = "RingLed";
            this.RingLed.Size = new System.Drawing.Size(17, 25);
            this.RingLed.TabIndex = 9;
            // 
            // RingLbl
            // 
            this.RingLbl.BackColor = System.Drawing.SystemColors.Control;
            this.RingLbl.Cursor = System.Windows.Forms.Cursors.Default;
            this.RingLbl.ForeColor = System.Drawing.SystemColors.ControlText;
            this.RingLbl.Location = new System.Drawing.Point(40, 152);
            this.RingLbl.Name = "RingLbl";
            this.RingLbl.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.RingLbl.Size = new System.Drawing.Size(65, 17);
            this.RingLbl.TabIndex = 5;
            this.RingLbl.Text = "Ring";
            // 
            // DSRLed
            // 
            this.DSRLed.BackColor = System.Drawing.Color.Red;
            this.DSRLed.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.DSRLed.ForeColor = System.Drawing.Color.Black;
            this.DSRLed.Location = new System.Drawing.Point(8, 120);
            this.DSRLed.Name = "DSRLed";
            this.DSRLed.Size = new System.Drawing.Size(17, 25);
            this.DSRLed.TabIndex = 10;
            // 
            // DSRLbl
            // 
            this.DSRLbl.BackColor = System.Drawing.SystemColors.Control;
            this.DSRLbl.Cursor = System.Windows.Forms.Cursors.Default;
            this.DSRLbl.ForeColor = System.Drawing.SystemColors.ControlText;
            this.DSRLbl.Location = new System.Drawing.Point(40, 128);
            this.DSRLbl.Name = "DSRLbl";
            this.DSRLbl.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.DSRLbl.Size = new System.Drawing.Size(65, 17);
            this.DSRLbl.TabIndex = 4;
            this.DSRLbl.Text = "DSR";
            // 
            // DCDLbl
            // 
            this.DCDLbl.BackColor = System.Drawing.SystemColors.Control;
            this.DCDLbl.Cursor = System.Windows.Forms.Cursors.Default;
            this.DCDLbl.ForeColor = System.Drawing.SystemColors.ControlText;
            this.DCDLbl.Location = new System.Drawing.Point(40, 104);
            this.DCDLbl.Name = "DCDLbl";
            this.DCDLbl.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.DCDLbl.Size = new System.Drawing.Size(65, 17);
            this.DCDLbl.TabIndex = 3;
            this.DCDLbl.Text = "DCD";
            // 
            // DCDLed
            // 
            this.DCDLed.BackColor = System.Drawing.Color.Red;
            this.DCDLed.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.DCDLed.ForeColor = System.Drawing.Color.Black;
            this.DCDLed.Location = new System.Drawing.Point(8, 96);
            this.DCDLed.Name = "DCDLed";
            this.DCDLed.Size = new System.Drawing.Size(17, 25);
            this.DCDLed.TabIndex = 11;
            // 
            // CTSLbl
            // 
            this.CTSLbl.BackColor = System.Drawing.SystemColors.Control;
            this.CTSLbl.Cursor = System.Windows.Forms.Cursors.Default;
            this.CTSLbl.ForeColor = System.Drawing.SystemColors.ControlText;
            this.CTSLbl.Location = new System.Drawing.Point(40, 80);
            this.CTSLbl.Name = "CTSLbl";
            this.CTSLbl.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.CTSLbl.Size = new System.Drawing.Size(65, 17);
            this.CTSLbl.TabIndex = 2;
            this.CTSLbl.Text = "CTS";
            // 
            // CTSLed
            // 
            this.CTSLed.BackColor = System.Drawing.Color.Red;
            this.CTSLed.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.CTSLed.ForeColor = System.Drawing.Color.Black;
            this.CTSLed.Location = new System.Drawing.Point(8, 72);
            this.CTSLed.Name = "CTSLed";
            this.CTSLed.Size = new System.Drawing.Size(17, 25);
            this.CTSLed.TabIndex = 12;
            // 
            // TXDLbl
            // 
            this.TXDLbl.BackColor = System.Drawing.SystemColors.Control;
            this.TXDLbl.Cursor = System.Windows.Forms.Cursors.Default;
            this.TXDLbl.ForeColor = System.Drawing.SystemColors.ControlText;
            this.TXDLbl.Location = new System.Drawing.Point(40, 56);
            this.TXDLbl.Name = "TXDLbl";
            this.TXDLbl.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.TXDLbl.Size = new System.Drawing.Size(65, 17);
            this.TXDLbl.TabIndex = 1;
            this.TXDLbl.Text = "TXD";
            // 
            // TXDLed
            // 
            this.TXDLed.BackColor = System.Drawing.Color.Red;
            this.TXDLed.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.TXDLed.ForeColor = System.Drawing.Color.Black;
            this.TXDLed.Location = new System.Drawing.Point(8, 48);
            this.TXDLed.Name = "TXDLed";
            this.TXDLed.Size = new System.Drawing.Size(17, 25);
            this.TXDLed.TabIndex = 13;
            // 
            // RXDLbl
            // 
            this.RXDLbl.BackColor = System.Drawing.SystemColors.Control;
            this.RXDLbl.Cursor = System.Windows.Forms.Cursors.Default;
            this.RXDLbl.ForeColor = System.Drawing.SystemColors.ControlText;
            this.RXDLbl.Location = new System.Drawing.Point(40, 32);
            this.RXDLbl.Name = "RXDLbl";
            this.RXDLbl.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.RXDLbl.Size = new System.Drawing.Size(65, 17);
            this.RXDLbl.TabIndex = 0;
            this.RXDLbl.Text = "RXD";
            // 
            // RXDLed
            // 
            this.RXDLed.BackColor = System.Drawing.Color.Red;
            this.RXDLed.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.RXDLed.ForeColor = System.Drawing.Color.Black;
            this.RXDLed.Location = new System.Drawing.Point(8, 24);
            this.RXDLed.Name = "RXDLed";
            this.RXDLed.Size = new System.Drawing.Size(17, 25);
            this.RXDLed.TabIndex = 14;
            // 
            // EventReset
            // 
            this.EventReset.Interval = 1000;
            this.EventReset.Tick += new System.EventHandler(this.EventReset_Tick);
            // 
            // StatusDlg
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.Control;
            this.ClientSize = new System.Drawing.Size(144, 216);
            this.Controls.Add(this.ErrorLed);
            this.Controls.Add(this.ErrorLbl);
            this.Controls.Add(this.BreakLed);
            this.Controls.Add(this.BreakLbl);
            this.Controls.Add(this.RingLed);
            this.Controls.Add(this.RingLbl);
            this.Controls.Add(this.DSRLed);
            this.Controls.Add(this.DSRLbl);
            this.Controls.Add(this.DCDLbl);
            this.Controls.Add(this.DCDLed);
            this.Controls.Add(this.CTSLbl);
            this.Controls.Add(this.CTSLed);
            this.Controls.Add(this.TXDLbl);
            this.Controls.Add(this.TXDLed);
            this.Controls.Add(this.RXDLbl);
            this.Controls.Add(this.RXDLed);
            this.Cursor = System.Windows.Forms.Cursors.Default;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Location = new System.Drawing.Point(184, 250);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "StatusDlg";
            this.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "Serial port Status";
            this.Load += new System.EventHandler(this.StatusDlg_Load);
            this.ResumeLayout(false);

		}
		#endregion

        private System.ComponentModel.IContainer components;
        private Timer EventReset;
	}
}
