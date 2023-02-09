namespace Gurux.Serial
{
    partial class Settings
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.PortNameCB = new System.Windows.Forms.ComboBox();
            this.PortNameLbl = new System.Windows.Forms.Label();
            this.PortNamePanel = new System.Windows.Forms.Panel();
            this.BaudRatePanel = new System.Windows.Forms.Panel();
            this.BaudRateCB = new System.Windows.Forms.ComboBox();
            this.BaudRateLbl = new System.Windows.Forms.Label();
            this.DataBitsPanel = new System.Windows.Forms.Panel();
            this.DataBitsCB = new System.Windows.Forms.ComboBox();
            this.DataBitsLbl = new System.Windows.Forms.Label();
            this.ParityPanel = new System.Windows.Forms.Panel();
            this.ParityLbl = new System.Windows.Forms.Label();
            this.ParityCB = new System.Windows.Forms.ComboBox();
            this.StopBitsPanel = new System.Windows.Forms.Panel();
            this.StopBitsLbl = new System.Windows.Forms.Label();
            this.StopBitsCB = new System.Windows.Forms.ComboBox();
            this.FlowControlPanel = new System.Windows.Forms.Panel();
            this.FlowControlLbl = new System.Windows.Forms.Label();
            this.FlowControlCb = new System.Windows.Forms.ComboBox();
            this.PortNamePanel.SuspendLayout();
            this.BaudRatePanel.SuspendLayout();
            this.DataBitsPanel.SuspendLayout();
            this.ParityPanel.SuspendLayout();
            this.StopBitsPanel.SuspendLayout();
            this.FlowControlPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // PortNameCB
            // 
            this.PortNameCB.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.PortNameCB.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.PortNameCB.FormattingEnabled = true;
            this.PortNameCB.Location = new System.Drawing.Point(145, 6);
            this.PortNameCB.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.PortNameCB.Name = "PortNameCB";
            this.PortNameCB.Size = new System.Drawing.Size(191, 23);
            this.PortNameCB.TabIndex = 15;
            this.PortNameCB.SelectedIndexChanged += new System.EventHandler(this.PortNameCB_SelectedIndexChanged);
            // 
            // PortNameLbl
            // 
            this.PortNameLbl.AutoSize = true;
            this.PortNameLbl.Location = new System.Drawing.Point(5, 9);
            this.PortNameLbl.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.PortNameLbl.Name = "PortNameLbl";
            this.PortNameLbl.Size = new System.Drawing.Size(29, 15);
            this.PortNameLbl.TabIndex = 12;
            this.PortNameLbl.Text = "Port";
            // 
            // PortNamePanel
            // 
            this.PortNamePanel.Controls.Add(this.PortNameCB);
            this.PortNamePanel.Controls.Add(this.PortNameLbl);
            this.PortNamePanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.PortNamePanel.Location = new System.Drawing.Point(0, 0);
            this.PortNamePanel.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.PortNamePanel.Name = "PortNamePanel";
            this.PortNamePanel.Size = new System.Drawing.Size(350, 35);
            this.PortNamePanel.TabIndex = 28;
            // 
            // BaudRatePanel
            // 
            this.BaudRatePanel.Controls.Add(this.BaudRateCB);
            this.BaudRatePanel.Controls.Add(this.BaudRateLbl);
            this.BaudRatePanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.BaudRatePanel.Location = new System.Drawing.Point(0, 35);
            this.BaudRatePanel.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.BaudRatePanel.Name = "BaudRatePanel";
            this.BaudRatePanel.Size = new System.Drawing.Size(350, 35);
            this.BaudRatePanel.TabIndex = 34;
            // 
            // BaudRateCB
            // 
            this.BaudRateCB.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.BaudRateCB.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.BaudRateCB.FormattingEnabled = true;
            this.BaudRateCB.Location = new System.Drawing.Point(145, 6);
            this.BaudRateCB.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.BaudRateCB.Name = "BaudRateCB";
            this.BaudRateCB.Size = new System.Drawing.Size(191, 23);
            this.BaudRateCB.TabIndex = 16;
            this.BaudRateCB.SelectedIndexChanged += new System.EventHandler(this.BaudRateCB_SelectedIndexChanged);
            // 
            // BaudRateLbl
            // 
            this.BaudRateLbl.AutoSize = true;
            this.BaudRateLbl.Location = new System.Drawing.Point(5, 8);
            this.BaudRateLbl.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.BaudRateLbl.Name = "BaudRateLbl";
            this.BaudRateLbl.Size = new System.Drawing.Size(67, 15);
            this.BaudRateLbl.TabIndex = 10;
            this.BaudRateLbl.Text = "Baud RateX";
            // 
            // DataBitsPanel
            // 
            this.DataBitsPanel.Controls.Add(this.DataBitsCB);
            this.DataBitsPanel.Controls.Add(this.DataBitsLbl);
            this.DataBitsPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.DataBitsPanel.Location = new System.Drawing.Point(0, 70);
            this.DataBitsPanel.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.DataBitsPanel.Name = "DataBitsPanel";
            this.DataBitsPanel.Size = new System.Drawing.Size(350, 35);
            this.DataBitsPanel.TabIndex = 35;
            // 
            // DataBitsCB
            // 
            this.DataBitsCB.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.DataBitsCB.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.DataBitsCB.FormattingEnabled = true;
            this.DataBitsCB.Location = new System.Drawing.Point(145, 5);
            this.DataBitsCB.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.DataBitsCB.Name = "DataBitsCB";
            this.DataBitsCB.Size = new System.Drawing.Size(191, 23);
            this.DataBitsCB.TabIndex = 15;
            this.DataBitsCB.SelectedIndexChanged += new System.EventHandler(this.DataBitsCB_SelectedIndexChanged);
            // 
            // DataBitsLbl
            // 
            this.DataBitsLbl.AutoSize = true;
            this.DataBitsLbl.Location = new System.Drawing.Point(5, 8);
            this.DataBitsLbl.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.DataBitsLbl.Name = "DataBitsLbl";
            this.DataBitsLbl.Size = new System.Drawing.Size(60, 15);
            this.DataBitsLbl.TabIndex = 12;
            this.DataBitsLbl.Text = "Data BitsX";
            // 
            // ParityPanel
            // 
            this.ParityPanel.Controls.Add(this.ParityLbl);
            this.ParityPanel.Controls.Add(this.ParityCB);
            this.ParityPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.ParityPanel.Location = new System.Drawing.Point(0, 105);
            this.ParityPanel.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.ParityPanel.Name = "ParityPanel";
            this.ParityPanel.Size = new System.Drawing.Size(350, 35);
            this.ParityPanel.TabIndex = 36;
            // 
            // ParityLbl
            // 
            this.ParityLbl.AutoSize = true;
            this.ParityLbl.Location = new System.Drawing.Point(5, 8);
            this.ParityLbl.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.ParityLbl.Name = "ParityLbl";
            this.ParityLbl.Size = new System.Drawing.Size(44, 15);
            this.ParityLbl.TabIndex = 15;
            this.ParityLbl.Text = "ParityX";
            // 
            // ParityCB
            // 
            this.ParityCB.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.ParityCB.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.ParityCB.FormattingEnabled = true;
            this.ParityCB.Location = new System.Drawing.Point(145, 5);
            this.ParityCB.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.ParityCB.Name = "ParityCB";
            this.ParityCB.Size = new System.Drawing.Size(191, 23);
            this.ParityCB.TabIndex = 14;
            this.ParityCB.SelectedIndexChanged += new System.EventHandler(this.ParityCB_SelectedIndexChanged);
            // 
            // StopBitsPanel
            // 
            this.StopBitsPanel.Controls.Add(this.StopBitsLbl);
            this.StopBitsPanel.Controls.Add(this.StopBitsCB);
            this.StopBitsPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.StopBitsPanel.Location = new System.Drawing.Point(0, 140);
            this.StopBitsPanel.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.StopBitsPanel.Name = "StopBitsPanel";
            this.StopBitsPanel.Size = new System.Drawing.Size(350, 35);
            this.StopBitsPanel.TabIndex = 37;
            // 
            // StopBitsLbl
            // 
            this.StopBitsLbl.AutoSize = true;
            this.StopBitsLbl.Location = new System.Drawing.Point(5, 8);
            this.StopBitsLbl.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.StopBitsLbl.Name = "StopBitsLbl";
            this.StopBitsLbl.Size = new System.Drawing.Size(60, 15);
            this.StopBitsLbl.TabIndex = 15;
            this.StopBitsLbl.Text = "Stop BitsX";
            // 
            // StopBitsCB
            // 
            this.StopBitsCB.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.StopBitsCB.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.StopBitsCB.FormattingEnabled = true;
            this.StopBitsCB.Location = new System.Drawing.Point(145, 5);
            this.StopBitsCB.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.StopBitsCB.Name = "StopBitsCB";
            this.StopBitsCB.Size = new System.Drawing.Size(191, 23);
            this.StopBitsCB.TabIndex = 14;
            this.StopBitsCB.SelectedIndexChanged += new System.EventHandler(this.StopBitsCB_SelectedIndexChanged);
            // 
            // FlowControlPanel
            // 
            this.FlowControlPanel.Controls.Add(this.FlowControlLbl);
            this.FlowControlPanel.Controls.Add(this.FlowControlCb);
            this.FlowControlPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.FlowControlPanel.Location = new System.Drawing.Point(0, 175);
            this.FlowControlPanel.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.FlowControlPanel.Name = "FlowControlPanel";
            this.FlowControlPanel.Size = new System.Drawing.Size(350, 35);
            this.FlowControlPanel.TabIndex = 38;
            // 
            // FlowControlLbl
            // 
            this.FlowControlLbl.AutoSize = true;
            this.FlowControlLbl.Location = new System.Drawing.Point(5, 8);
            this.FlowControlLbl.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.FlowControlLbl.Name = "FlowControlLbl";
            this.FlowControlLbl.Size = new System.Drawing.Size(82, 15);
            this.FlowControlLbl.TabIndex = 15;
            this.FlowControlLbl.Text = "Flow ControlX";
            // 
            // FlowControlCb
            // 
            this.FlowControlCb.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.FlowControlCb.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.FlowControlCb.FormattingEnabled = true;
            this.FlowControlCb.Location = new System.Drawing.Point(145, 5);
            this.FlowControlCb.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.FlowControlCb.Name = "FlowControlCb";
            this.FlowControlCb.Size = new System.Drawing.Size(191, 23);
            this.FlowControlCb.TabIndex = 14;
            this.FlowControlCb.SelectedIndexChanged += new System.EventHandler(this.FlowControlCb_SelectedIndexChanged);
            // 
            // Settings
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(350, 226);
            this.Controls.Add(this.FlowControlPanel);
            this.Controls.Add(this.StopBitsPanel);
            this.Controls.Add(this.ParityPanel);
            this.Controls.Add(this.DataBitsPanel);
            this.Controls.Add(this.BaudRatePanel);
            this.Controls.Add(this.PortNamePanel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "Settings";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Serial Port Settings";
            this.PortNamePanel.ResumeLayout(false);
            this.PortNamePanel.PerformLayout();
            this.BaudRatePanel.ResumeLayout(false);
            this.BaudRatePanel.PerformLayout();
            this.DataBitsPanel.ResumeLayout(false);
            this.DataBitsPanel.PerformLayout();
            this.ParityPanel.ResumeLayout(false);
            this.ParityPanel.PerformLayout();
            this.StopBitsPanel.ResumeLayout(false);
            this.StopBitsPanel.PerformLayout();
            this.FlowControlPanel.ResumeLayout(false);
            this.FlowControlPanel.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.ComboBox PortNameCB;
        private System.Windows.Forms.Label PortNameLbl;
        private System.Windows.Forms.Panel PortNamePanel;
        private System.Windows.Forms.Panel BaudRatePanel;
        private System.Windows.Forms.ComboBox BaudRateCB;
        private System.Windows.Forms.Label BaudRateLbl;
        private System.Windows.Forms.Panel DataBitsPanel;
        private System.Windows.Forms.ComboBox DataBitsCB;
        private System.Windows.Forms.Label DataBitsLbl;
        private System.Windows.Forms.Panel ParityPanel;
        private System.Windows.Forms.Label ParityLbl;
        private System.Windows.Forms.ComboBox ParityCB;
        private System.Windows.Forms.Panel StopBitsPanel;
        private System.Windows.Forms.Label StopBitsLbl;
        private System.Windows.Forms.ComboBox StopBitsCB;
        private System.Windows.Forms.Panel FlowControlPanel;
        private System.Windows.Forms.Label FlowControlLbl;
        private System.Windows.Forms.ComboBox FlowControlCb;
    }
}