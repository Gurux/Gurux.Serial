//
// --------------------------------------------------------------------------
//  Gurux Ltd
//
//
//
// Filename:        $HeadURL$
//
// Version:         $Revision$,
//                  $Date$
//                  $Author$
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
// This code is licensed under the GNU General Public License v2.
// Full text may be retrieved at http://www.gnu.org/licenses/gpl-2.0.txt
//---------------------------------------------------------------------------
using System;
using System.Diagnostics;
using System.Drawing;
using System.Security.Policy;
using System.Windows.Forms;
using Gurux.Common;

namespace Gurux.Shared
{
    partial class PropertiesForm : Form
    {
        IGXPropertyPage Properties;
        string HelpLink;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="properties">Properties form.</param>
        /// <param name="title">Window title.</param>
        /// <param name="open">Is media opened.</param>
        /// <param name="okBtnText">Localized OK button text.</param>
        /// <param name="cancelBtnText">Localized cancel button text.</param>
        /// <param name="helpLink">Help link address.</param>
        public PropertiesForm(Form properties, 
            string title, 
            bool open, 
            string okBtnText, 
            string cancelBtnText, 
            string helpLink)
        {
            HelpLink = helpLink;
            Properties = (IGXPropertyPage)properties;
            InitializeComponent();
            Properties.Initialize();
            if (string.IsNullOrEmpty(helpLink))
            {
                HelpButton = false;
            }
            this.Icon = System.Drawing.Icon.ExtractAssociatedIcon(GetType().Assembly.Location);
            this.OKBtn.Enabled = !open;
            this.Text = title;
            this.OKBtn.Text = okBtnText;
            this.CancelBtn.Text = cancelBtnText;
            //Settings can be change if connection is open.
            int h = 0;
            while (properties.Controls.Count != 0)
            {
                Control ctr = properties.Controls[0];
                if (ctr is Panel)
                {
                    if (ctr.Enabled)
                    {
                        h += ctr.Height;
                    }
                    else
                    {
                        properties.Controls.RemoveAt(0);
                        continue;
                    }
                }
                ctr.Enabled = !open;
                FormPanel.Controls.Add(ctr);
            }
            this.LocationChanged += new EventHandler(PropertiesForm_LocationChanged);
            properties.TopLevel = false;
            properties.TopMost = false;
            properties.FormBorderStyle = FormBorderStyle.None;
            this.Controls.Add(properties);
            if (h == 0)
            {
                h = properties.Height;
            }
            h += panel1.Height;
            //Set client size.
            this.ClientSize = new Size(properties.Width, h);
        }

        void PropertiesForm_LocationChanged(object sender, EventArgs e)
        {
            (Properties as Form).Location = this.Location;
        }

        private void OKBtn_Click(object sender, EventArgs e)
        {
            try
            {
                Properties.Apply();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// Show help not available message.
        /// </summary>
        /// <param name="hevent">A HelpEventArgs that contains the event data.</param>
        protected override void OnHelpRequested(HelpEventArgs hevent)
        {
            try
            {
                // Show online help.
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = HelpLink,
                    UseShellExecute = true // Required in .NET Core and later
                };
                Process.Start(psi);
                // Set flag to show that the Help event as been handled
                hevent.Handled = true;
            }
            catch (Exception Ex)
            {
                MessageBox.Show(this, Ex.Message, Text, MessageBoxButtons.OK);
            }
        }
    }
}