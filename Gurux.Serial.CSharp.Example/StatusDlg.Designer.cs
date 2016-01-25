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
		//NOTE: The following procedure is required by the Windows Form Designer
		//It can be modified using the Windows Form Designer.
		//Do not modify it using the code editor.
		[System.Diagnostics.DebuggerStepThrough()]private void InitializeComponent()
		{
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
            this.SuspendLayout();
            // 
            // BreakLed
            // 
            this.BreakLed.BackColor = System.Drawing.Color.Transparent;
            this.BreakLed.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.BreakLed.ForeColor = System.Drawing.Color.Black;
            this.BreakLed.Location = new System.Drawing.Point(8, 102);
            this.BreakLed.Name = "BreakLed";
            this.BreakLed.Size = new System.Drawing.Size(17, 25);
            this.BreakLed.TabIndex = 8;
            // 
            // BreakLbl
            // 
            this.BreakLbl.BackColor = System.Drawing.SystemColors.Control;
            this.BreakLbl.Cursor = System.Windows.Forms.Cursors.Default;
            this.BreakLbl.ForeColor = System.Drawing.SystemColors.ControlText;
            this.BreakLbl.Location = new System.Drawing.Point(40, 110);
            this.BreakLbl.Name = "BreakLbl";
            this.BreakLbl.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.BreakLbl.Size = new System.Drawing.Size(65, 17);
            this.BreakLbl.TabIndex = 6;
            this.BreakLbl.Text = "Break";
            // 
            // RingLed
            // 
            this.RingLed.BackColor = System.Drawing.Color.Transparent;
            this.RingLed.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.RingLed.ForeColor = System.Drawing.Color.Black;
            this.RingLed.Location = new System.Drawing.Point(8, 78);
            this.RingLed.Name = "RingLed";
            this.RingLed.Size = new System.Drawing.Size(17, 25);
            this.RingLed.TabIndex = 9;
            // 
            // RingLbl
            // 
            this.RingLbl.BackColor = System.Drawing.SystemColors.Control;
            this.RingLbl.Cursor = System.Windows.Forms.Cursors.Default;
            this.RingLbl.ForeColor = System.Drawing.SystemColors.ControlText;
            this.RingLbl.Location = new System.Drawing.Point(40, 86);
            this.RingLbl.Name = "RingLbl";
            this.RingLbl.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.RingLbl.Size = new System.Drawing.Size(65, 17);
            this.RingLbl.TabIndex = 5;
            this.RingLbl.Text = "Ring";
            // 
            // DSRLed
            // 
            this.DSRLed.BackColor = System.Drawing.Color.Transparent;
            this.DSRLed.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.DSRLed.ForeColor = System.Drawing.Color.Black;
            this.DSRLed.Location = new System.Drawing.Point(8, 54);
            this.DSRLed.Name = "DSRLed";
            this.DSRLed.Size = new System.Drawing.Size(17, 25);
            this.DSRLed.TabIndex = 10;
            // 
            // DSRLbl
            // 
            this.DSRLbl.BackColor = System.Drawing.SystemColors.Control;
            this.DSRLbl.Cursor = System.Windows.Forms.Cursors.Default;
            this.DSRLbl.ForeColor = System.Drawing.SystemColors.ControlText;
            this.DSRLbl.Location = new System.Drawing.Point(40, 62);
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
            this.DCDLbl.Location = new System.Drawing.Point(40, 38);
            this.DCDLbl.Name = "DCDLbl";
            this.DCDLbl.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.DCDLbl.Size = new System.Drawing.Size(65, 17);
            this.DCDLbl.TabIndex = 3;
            this.DCDLbl.Text = "DCD";
            // 
            // DCDLed
            // 
            this.DCDLed.BackColor = System.Drawing.Color.Transparent;
            this.DCDLed.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.DCDLed.ForeColor = System.Drawing.Color.Black;
            this.DCDLed.Location = new System.Drawing.Point(8, 30);
            this.DCDLed.Name = "DCDLed";
            this.DCDLed.Size = new System.Drawing.Size(17, 25);
            this.DCDLed.TabIndex = 11;
            // 
            // CTSLbl
            // 
            this.CTSLbl.BackColor = System.Drawing.SystemColors.Control;
            this.CTSLbl.Cursor = System.Windows.Forms.Cursors.Default;
            this.CTSLbl.ForeColor = System.Drawing.SystemColors.ControlText;
            this.CTSLbl.Location = new System.Drawing.Point(40, 14);
            this.CTSLbl.Name = "CTSLbl";
            this.CTSLbl.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.CTSLbl.Size = new System.Drawing.Size(65, 17);
            this.CTSLbl.TabIndex = 2;
            this.CTSLbl.Text = "CTS";
            // 
            // CTSLed
            // 
            this.CTSLed.BackColor = System.Drawing.Color.Transparent;
            this.CTSLed.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.CTSLed.ForeColor = System.Drawing.Color.Black;
            this.CTSLed.Location = new System.Drawing.Point(8, 6);
            this.CTSLed.Name = "CTSLed";
            this.CTSLed.Size = new System.Drawing.Size(17, 25);
            this.CTSLed.TabIndex = 12;
            // 
            // StatusDlg
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.Control;
            this.ClientSize = new System.Drawing.Size(144, 139);
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
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.StatusDlg_FormClosing);
            this.Load += new System.EventHandler(this.StatusDlg_Load);
            this.ResumeLayout(false);

		}
		#endregion

        private System.ComponentModel.IContainer components;
	}
}
