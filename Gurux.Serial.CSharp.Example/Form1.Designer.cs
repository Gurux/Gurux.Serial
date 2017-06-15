//
// --------------------------------------------------------------------------
//  Gurux Ltd
// 
//
//
// Filename:        $HeadURL: svn://utopia/projects/Old/GuruxSerial/GXSerial%20csharp%20Sample/Form1.Designer.cs $
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


using System.Diagnostics;
using System;
using System.Windows.Forms;
using System.Collections;
using System.Drawing;
using System.Data;

namespace GXSerialSample
{
    partial class Form1
    {
        [STAThread]
        static void Main()
        {
            System.Windows.Forms.Application.Run(new Form1());
        }
        #region "Windows Form Designer generated code "
        [System.Diagnostics.DebuggerNonUserCode()]
        public Form1()
        {
            //This call is required by the Windows Form Designer.
            InitializeComponent();
        }
        //Form overrides dispose to clean up the component list.
        [System.Diagnostics.DebuggerNonUserCode()]
        protected override void Dispose(bool Disposing)
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
        public System.Windows.Forms.Timer PacketCounterTimer;
        public System.Windows.Forms.TextBox ReceivedTB;
        public System.Windows.Forms.TextBox SentTB;
        public System.Windows.Forms.Label Label6;
        public System.Windows.Forms.Label Label5;
        public System.Windows.Forms.GroupBox Frame2;
        public System.Windows.Forms.Button SendBtn;
        public System.Windows.Forms.CheckBox SyncBtn;
        public System.Windows.Forms.CheckBox HexCB;
        public System.Windows.Forms.Button StatusBtn;
        public System.Windows.Forms.TextBox SendText;
        public System.Windows.Forms.TextBox ReceivedText;
        public System.Windows.Forms.Label _Label1_1;
        public System.Windows.Forms.GroupBox Frame1;
        public System.Windows.Forms.Button OpenBtn;
        public System.Windows.Forms.Button CloseBtn;
        public System.Windows.Forms.Button PropertiesBtn;
        //NOTE: The following procedure is required by the Windows Form Designer
        //It can be modified using the Windows Form Designer.
        //Do not modify it using the code editor.
        [System.Diagnostics.DebuggerStepThrough()]
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.Frame2 = new System.Windows.Forms.GroupBox();
            this.ReceivedTB = new System.Windows.Forms.TextBox();
            this.SentTB = new System.Windows.Forms.TextBox();
            this.Label6 = new System.Windows.Forms.Label();
            this.Label5 = new System.Windows.Forms.Label();
            this.PacketCounterTimer = new System.Windows.Forms.Timer(this.components);
            this.EchoCB = new System.Windows.Forms.CheckBox();
            this.SendBtn = new System.Windows.Forms.Button();
            this.SyncBtn = new System.Windows.Forms.CheckBox();
            this.HexCB = new System.Windows.Forms.CheckBox();
            this.StatusBtn = new System.Windows.Forms.Button();
            this.Frame1 = new System.Windows.Forms.GroupBox();
            this.SendText = new System.Windows.Forms.TextBox();
            this._Label1_1 = new System.Windows.Forms.Label();
            this.ReceivedText = new System.Windows.Forms.TextBox();
            this.OpenBtn = new System.Windows.Forms.Button();
            this.CloseBtn = new System.Windows.Forms.Button();
            this.PropertiesBtn = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.MinSizeTB = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.EOPText = new System.Windows.Forms.TextBox();
            this.WaitTimeTB = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.Frame2.SuspendLayout();
            this.Frame1.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // Frame2
            // 
            this.Frame2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.Frame2.BackColor = System.Drawing.SystemColors.Control;
            this.Frame2.Controls.Add(this.ReceivedTB);
            this.Frame2.Controls.Add(this.SentTB);
            this.Frame2.Controls.Add(this.Label6);
            this.Frame2.Controls.Add(this.Label5);
            this.Frame2.ForeColor = System.Drawing.SystemColors.ControlText;
            this.Frame2.Location = new System.Drawing.Point(3, 305);
            this.Frame2.Name = "Frame2";
            this.Frame2.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.Frame2.Size = new System.Drawing.Size(393, 49);
            this.Frame2.TabIndex = 12;
            this.Frame2.TabStop = false;
            this.Frame2.Text = "Packet statistics";
            // 
            // ReceivedTB
            // 
            this.ReceivedTB.AcceptsReturn = true;
            this.ReceivedTB.BackColor = System.Drawing.SystemColors.Window;
            this.ReceivedTB.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.ReceivedTB.Enabled = false;
            this.ReceivedTB.ForeColor = System.Drawing.SystemColors.WindowText;
            this.ReceivedTB.Location = new System.Drawing.Point(224, 16);
            this.ReceivedTB.MaxLength = 0;
            this.ReceivedTB.Name = "ReceivedTB";
            this.ReceivedTB.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.ReceivedTB.Size = new System.Drawing.Size(105, 20);
            this.ReceivedTB.TabIndex = 14;
            this.ReceivedTB.Text = "0";
            // 
            // SentTB
            // 
            this.SentTB.AcceptsReturn = true;
            this.SentTB.BackColor = System.Drawing.SystemColors.Window;
            this.SentTB.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.SentTB.Enabled = false;
            this.SentTB.ForeColor = System.Drawing.SystemColors.WindowText;
            this.SentTB.Location = new System.Drawing.Point(56, 16);
            this.SentTB.MaxLength = 0;
            this.SentTB.Name = "SentTB";
            this.SentTB.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.SentTB.Size = new System.Drawing.Size(89, 20);
            this.SentTB.TabIndex = 13;
            this.SentTB.Text = "0";
            // 
            // Label6
            // 
            this.Label6.AutoSize = true;
            this.Label6.BackColor = System.Drawing.SystemColors.Control;
            this.Label6.Cursor = System.Windows.Forms.Cursors.Default;
            this.Label6.ForeColor = System.Drawing.SystemColors.ControlText;
            this.Label6.Location = new System.Drawing.Point(160, 16);
            this.Label6.Name = "Label6";
            this.Label6.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.Label6.Size = new System.Drawing.Size(56, 13);
            this.Label6.TabIndex = 16;
            this.Label6.Text = "Received:";
            // 
            // Label5
            // 
            this.Label5.AutoSize = true;
            this.Label5.BackColor = System.Drawing.SystemColors.Control;
            this.Label5.Cursor = System.Windows.Forms.Cursors.Default;
            this.Label5.ForeColor = System.Drawing.SystemColors.ControlText;
            this.Label5.Location = new System.Drawing.Point(8, 16);
            this.Label5.Name = "Label5";
            this.Label5.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.Label5.Size = new System.Drawing.Size(32, 13);
            this.Label5.TabIndex = 15;
            this.Label5.Text = "Sent:";
            // 
            // PacketCounterTimer
            // 
            this.PacketCounterTimer.Interval = 1000;
            this.PacketCounterTimer.Tick += new System.EventHandler(this.PacketCounterTimer_Tick);
            // 
            // EchoCB
            // 
            this.EchoCB.BackColor = System.Drawing.SystemColors.Control;
            this.EchoCB.Cursor = System.Windows.Forms.Cursors.Default;
            this.EchoCB.ForeColor = System.Drawing.SystemColors.ControlText;
            this.EchoCB.Location = new System.Drawing.Point(134, 103);
            this.EchoCB.Name = "EchoCB";
            this.EchoCB.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.EchoCB.Size = new System.Drawing.Size(57, 25);
            this.EchoCB.TabIndex = 22;
            this.EchoCB.Text = "Echo";
            this.EchoCB.UseVisualStyleBackColor = false;
            // 
            // SendBtn
            // 
            this.SendBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.SendBtn.BackColor = System.Drawing.SystemColors.Control;
            this.SendBtn.Cursor = System.Windows.Forms.Cursors.Default;
            this.SendBtn.ForeColor = System.Drawing.SystemColors.ControlText;
            this.SendBtn.Location = new System.Drawing.Point(265, 102);
            this.SendBtn.Name = "SendBtn";
            this.SendBtn.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.SendBtn.Size = new System.Drawing.Size(73, 25);
            this.SendBtn.TabIndex = 21;
            this.SendBtn.Text = "Send";
            this.SendBtn.UseVisualStyleBackColor = false;
            this.SendBtn.Click += new System.EventHandler(this.SendBtn_Click);
            // 
            // SyncBtn
            // 
            this.SyncBtn.BackColor = System.Drawing.SystemColors.Control;
            this.SyncBtn.Cursor = System.Windows.Forms.Cursors.Default;
            this.SyncBtn.ForeColor = System.Drawing.SystemColors.ControlText;
            this.SyncBtn.Location = new System.Drawing.Point(71, 103);
            this.SyncBtn.Name = "SyncBtn";
            this.SyncBtn.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.SyncBtn.Size = new System.Drawing.Size(57, 25);
            this.SyncBtn.TabIndex = 11;
            this.SyncBtn.Text = "Sync";
            this.SyncBtn.UseVisualStyleBackColor = false;
            this.SyncBtn.CheckedChanged += new System.EventHandler(this.SyncBtn_CheckedChanged);
            // 
            // HexCB
            // 
            this.HexCB.BackColor = System.Drawing.SystemColors.Control;
            this.HexCB.Cursor = System.Windows.Forms.Cursors.Default;
            this.HexCB.ForeColor = System.Drawing.SystemColors.ControlText;
            this.HexCB.Location = new System.Drawing.Point(8, 103);
            this.HexCB.Name = "HexCB";
            this.HexCB.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.HexCB.Size = new System.Drawing.Size(57, 25);
            this.HexCB.TabIndex = 10;
            this.HexCB.Text = "Hex";
            this.HexCB.UseVisualStyleBackColor = false;
            // 
            // StatusBtn
            // 
            this.StatusBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.StatusBtn.BackColor = System.Drawing.SystemColors.Control;
            this.StatusBtn.Cursor = System.Windows.Forms.Cursors.Default;
            this.StatusBtn.ForeColor = System.Drawing.SystemColors.ControlText;
            this.StatusBtn.Location = new System.Drawing.Point(344, 104);
            this.StatusBtn.Name = "StatusBtn";
            this.StatusBtn.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.StatusBtn.Size = new System.Drawing.Size(73, 25);
            this.StatusBtn.TabIndex = 8;
            this.StatusBtn.Text = "Status";
            this.StatusBtn.UseVisualStyleBackColor = false;
            this.StatusBtn.Click += new System.EventHandler(this.StatusBtn_Click);
            // 
            // Frame1
            // 
            this.Frame1.BackColor = System.Drawing.SystemColors.Control;
            this.Frame1.Controls.Add(this.EchoCB);
            this.Frame1.Controls.Add(this.SendText);
            this.Frame1.Controls.Add(this.SyncBtn);
            this.Frame1.Controls.Add(this.SendBtn);
            this.Frame1.Controls.Add(this.HexCB);
            this.Frame1.Controls.Add(this._Label1_1);
            this.Frame1.ForeColor = System.Drawing.SystemColors.ControlText;
            this.Frame1.Location = new System.Drawing.Point(0, 0);
            this.Frame1.Name = "Frame1";
            this.Frame1.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.Frame1.Size = new System.Drawing.Size(337, 137);
            this.Frame1.TabIndex = 6;
            this.Frame1.TabStop = false;
            // 
            // SendText
            // 
            this.SendText.AcceptsReturn = true;
            this.SendText.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.SendText.BackColor = System.Drawing.SystemColors.Window;
            this.SendText.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.SendText.ForeColor = System.Drawing.SystemColors.WindowText;
            this.SendText.Location = new System.Drawing.Point(8, 32);
            this.SendText.MaxLength = 0;
            this.SendText.Multiline = true;
            this.SendText.Name = "SendText";
            this.SendText.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.SendText.Size = new System.Drawing.Size(321, 33);
            this.SendText.TabIndex = 4;
            // 
            // _Label1_1
            // 
            this._Label1_1.BackColor = System.Drawing.SystemColors.Control;
            this._Label1_1.Cursor = System.Windows.Forms.Cursors.Default;
            this._Label1_1.ForeColor = System.Drawing.SystemColors.ControlText;
            this._Label1_1.Location = new System.Drawing.Point(8, 8);
            this._Label1_1.Name = "_Label1_1";
            this._Label1_1.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this._Label1_1.Size = new System.Drawing.Size(89, 17);
            this._Label1_1.TabIndex = 0;
            this._Label1_1.Text = "Send:";
            // 
            // ReceivedText
            // 
            this.ReceivedText.AcceptsReturn = true;
            this.ReceivedText.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.ReceivedText.BackColor = System.Drawing.SystemColors.Window;
            this.ReceivedText.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.ReceivedText.ForeColor = System.Drawing.SystemColors.WindowText;
            this.ReceivedText.Location = new System.Drawing.Point(3, 189);
            this.ReceivedText.MaxLength = 0;
            this.ReceivedText.Multiline = true;
            this.ReceivedText.Name = "ReceivedText";
            this.ReceivedText.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.ReceivedText.Size = new System.Drawing.Size(414, 97);
            this.ReceivedText.TabIndex = 5;
            this.ReceivedText.TabStop = false;
            // 
            // OpenBtn
            // 
            this.OpenBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.OpenBtn.BackColor = System.Drawing.SystemColors.Control;
            this.OpenBtn.Cursor = System.Windows.Forms.Cursors.Default;
            this.OpenBtn.ForeColor = System.Drawing.SystemColors.ControlText;
            this.OpenBtn.Location = new System.Drawing.Point(344, 8);
            this.OpenBtn.Name = "OpenBtn";
            this.OpenBtn.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.OpenBtn.Size = new System.Drawing.Size(73, 25);
            this.OpenBtn.TabIndex = 1;
            this.OpenBtn.Text = "Open";
            this.OpenBtn.UseVisualStyleBackColor = false;
            this.OpenBtn.Click += new System.EventHandler(this.OpenBtn_Click);
            // 
            // CloseBtn
            // 
            this.CloseBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.CloseBtn.BackColor = System.Drawing.SystemColors.Control;
            this.CloseBtn.Cursor = System.Windows.Forms.Cursors.Default;
            this.CloseBtn.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.CloseBtn.Enabled = false;
            this.CloseBtn.ForeColor = System.Drawing.SystemColors.ControlText;
            this.CloseBtn.Location = new System.Drawing.Point(344, 40);
            this.CloseBtn.Name = "CloseBtn";
            this.CloseBtn.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.CloseBtn.Size = new System.Drawing.Size(73, 25);
            this.CloseBtn.TabIndex = 2;
            this.CloseBtn.Text = "Close";
            this.CloseBtn.UseVisualStyleBackColor = false;
            this.CloseBtn.Click += new System.EventHandler(this.CloseBtn_Click);
            // 
            // PropertiesBtn
            // 
            this.PropertiesBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.PropertiesBtn.BackColor = System.Drawing.SystemColors.Control;
            this.PropertiesBtn.Cursor = System.Windows.Forms.Cursors.Default;
            this.PropertiesBtn.ForeColor = System.Drawing.SystemColors.ControlText;
            this.PropertiesBtn.Location = new System.Drawing.Point(344, 72);
            this.PropertiesBtn.Name = "PropertiesBtn";
            this.PropertiesBtn.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.PropertiesBtn.Size = new System.Drawing.Size(73, 25);
            this.PropertiesBtn.TabIndex = 3;
            this.PropertiesBtn.Text = "Properties";
            this.PropertiesBtn.UseVisualStyleBackColor = false;
            this.PropertiesBtn.Click += new System.EventHandler(this.PropertiesBtn_Click);
            // 
            // groupBox1
            // 
            this.groupBox1.BackColor = System.Drawing.SystemColors.Control;
            this.groupBox1.Controls.Add(this.MinSizeTB);
            this.groupBox1.Controls.Add(this.label3);
            this.groupBox1.Controls.Add(this.EOPText);
            this.groupBox1.Controls.Add(this.WaitTimeTB);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.ForeColor = System.Drawing.SystemColors.ControlText;
            this.groupBox1.Location = new System.Drawing.Point(2, 134);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.groupBox1.Size = new System.Drawing.Size(393, 49);
            this.groupBox1.TabIndex = 18;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Synchronous settings";
            // 
            // MinSizeTB
            // 
            this.MinSizeTB.AcceptsReturn = true;
            this.MinSizeTB.BackColor = System.Drawing.SystemColors.Window;
            this.MinSizeTB.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.MinSizeTB.Enabled = false;
            this.MinSizeTB.ForeColor = System.Drawing.SystemColors.WindowText;
            this.MinSizeTB.Location = new System.Drawing.Point(310, 15);
            this.MinSizeTB.MaxLength = 0;
            this.MinSizeTB.Name = "MinSizeTB";
            this.MinSizeTB.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.MinSizeTB.Size = new System.Drawing.Size(72, 20);
            this.MinSizeTB.TabIndex = 26;
            this.MinSizeTB.Text = "0";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.BackColor = System.Drawing.SystemColors.Control;
            this.label3.Cursor = System.Windows.Forms.Cursors.Default;
            this.label3.ForeColor = System.Drawing.SystemColors.ControlText;
            this.label3.Location = new System.Drawing.Point(272, 16);
            this.label3.Name = "label3";
            this.label3.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.label3.Size = new System.Drawing.Size(30, 13);
            this.label3.TabIndex = 25;
            this.label3.Text = "Size:";
            // 
            // EOPText
            // 
            this.EOPText.BackColor = System.Drawing.SystemColors.Window;
            this.EOPText.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.EOPText.ForeColor = System.Drawing.SystemColors.WindowText;
            this.EOPText.Location = new System.Drawing.Point(192, 15);
            this.EOPText.MaxLength = 0;
            this.EOPText.Name = "EOPText";
            this.EOPText.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.EOPText.Size = new System.Drawing.Size(72, 20);
            this.EOPText.TabIndex = 24;
            this.EOPText.Text = "\\r\\n";
            // 
            // WaitTimeTB
            // 
            this.WaitTimeTB.AcceptsReturn = true;
            this.WaitTimeTB.BackColor = System.Drawing.SystemColors.Window;
            this.WaitTimeTB.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.WaitTimeTB.Enabled = false;
            this.WaitTimeTB.ForeColor = System.Drawing.SystemColors.WindowText;
            this.WaitTimeTB.Location = new System.Drawing.Point(80, 16);
            this.WaitTimeTB.MaxLength = 0;
            this.WaitTimeTB.Name = "WaitTimeTB";
            this.WaitTimeTB.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.WaitTimeTB.Size = new System.Drawing.Size(61, 20);
            this.WaitTimeTB.TabIndex = 13;
            this.WaitTimeTB.Text = "5000";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.BackColor = System.Drawing.SystemColors.Control;
            this.label1.Cursor = System.Windows.Forms.Cursors.Default;
            this.label1.ForeColor = System.Drawing.SystemColors.ControlText;
            this.label1.Location = new System.Drawing.Point(154, 16);
            this.label1.Name = "label1";
            this.label1.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.label1.Size = new System.Drawing.Size(32, 13);
            this.label1.TabIndex = 16;
            this.label1.Text = "EOP:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.BackColor = System.Drawing.SystemColors.Control;
            this.label2.Cursor = System.Windows.Forms.Cursors.Default;
            this.label2.ForeColor = System.Drawing.SystemColors.ControlText;
            this.label2.Location = new System.Drawing.Point(8, 18);
            this.label2.Name = "label2";
            this.label2.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.label2.Size = new System.Drawing.Size(58, 13);
            this.label2.TabIndex = 15;
            this.label2.Text = "Wait Time:";
            // 
            // Form1
            // 
            this.AcceptButton = this.OpenBtn;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.Control;
            this.CancelButton = this.CloseBtn;
            this.ClientSize = new System.Drawing.Size(422, 355);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.Frame2);
            this.Controls.Add(this.ReceivedText);
            this.Controls.Add(this.StatusBtn);
            this.Controls.Add(this.Frame1);
            this.Controls.Add(this.OpenBtn);
            this.Controls.Add(this.CloseBtn);
            this.Controls.Add(this.PropertiesBtn);
            this.Cursor = System.Windows.Forms.Cursors.Default;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Location = new System.Drawing.Point(226, 67);
            this.Name = "Form1";
            this.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "GXSerial Sample";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.Frame2.ResumeLayout(false);
            this.Frame2.PerformLayout();
            this.Frame1.ResumeLayout(false);
            this.Frame1.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }
        #endregion

        private System.ComponentModel.IContainer components;
        public GroupBox groupBox1;
        public TextBox EOPText;
        public TextBox WaitTimeTB;
        public Label label1;
        public Label label2;
        public TextBox MinSizeTB;
        public Label label3;
        private Gurux.Serial.GXSerial gxSerial1;
        public CheckBox EchoCB;
    }
}
