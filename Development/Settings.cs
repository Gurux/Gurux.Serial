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
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Gurux.Common;

namespace Gurux.Serial
{
	/// <summary>
	/// Settings dialog.
	/// </summary>
    partial class Settings : Form, Gurux.Common.IGXPropertyPage
    {
        GXSerial Target;
		/// <summary>
		/// Constructor.
		/// </summary>
        public Settings(GXSerial target)
        {
            InitializeComponent();
            Target = target;
        }

        private void PortNameCB_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (BaudRatePanel.Enabled)
            {
                this.BaudRateCB.Items.Clear();
                BaudRateCB.DropDownStyle = ComboBoxStyle.DropDownList;
                //User can't change values when connection is open.
                if (Target.IsOpen)
                {
                    BaudRateCB.Items.Add(Target.BaudRate);
                    this.BaudRateCB.SelectedItem = 0;
                }
                else
                {
                    foreach (int it in GXSerial.GetAvailableBaudRates(PortNameCB.Text))
                    {
                        if (it == 0)
                        {
                            BaudRateCB.DropDownStyle = ComboBoxStyle.DropDown;
                        }
                        else
                        {
                            this.BaudRateCB.Items.Add(it);
                        }
                    }
                    this.BaudRateCB.SelectedItem = Target.BaudRate;
                }
            }
        }

        #region IGXPropertyPage Members

        void IGXPropertyPage.Apply()
        {
            Target.PortName = this.PortNameCB.Text;
            Target.BaudRate = Convert.ToInt32(this.BaudRateCB.Text);
            Target.DataBits = Convert.ToInt32(this.DataBitsCB.Text);
            Target.Parity = (System.IO.Ports.Parity)this.ParityCB.SelectedItem;
            Target.StopBits = (System.IO.Ports.StopBits)this.StopBitsCB.SelectedItem;
        }

        void IGXPropertyPage.Initialize()
        {
            //Update texts.
            this.Text = Gurux.Serial.Resources.SettingsTxt;
            this.PortNameLbl.Text = Gurux.Serial.Resources.PortNameTxt;
            this.BaudRateLbl.Text = Gurux.Serial.Resources.BaudRateTxt;
            this.DataBitsLbl.Text = Gurux.Serial.Resources.DataBitsTxt;
            this.ParityLbl.Text = Gurux.Serial.Resources.ParityTxt;
            this.StopBitsLbl.Text = Gurux.Serial.Resources.StopBitsTxt;
            //Hide controls which user do not want to show.
            PortNamePanel.Enabled = (Target.ConfigurableSettings & AvailableMediaSettings.PortName) != 0;
            if (PortNamePanel.Enabled)
            {
				if (Target.AvailablePorts != null)
				{
					PortNameCB.Items.AddRange(Target.AvailablePorts);
				}
				else
				{
					PortNameCB.Items.AddRange(GXSerial.GetPortNames());
				}
				if (this.PortNameCB.Items.Contains(Target.PortName))
				{
					this.PortNameCB.SelectedItem = Target.PortName;
				}
				else
				{
                    if (PortNameCB.Items.Count != 0)
                    {
                        this.PortNameCB.SelectedIndex = 0;
                    }
                    else
                    {
                        StopBitsPanel.Enabled = ParityPanel.Enabled = DataBitsPanel.Enabled = BaudRatePanel.Enabled = this.PortNameCB.Enabled = false;
                        return;
                    }
				}
            }
            BaudRatePanel.Enabled = (Target.ConfigurableSettings & AvailableMediaSettings.BaudRate) != 0;
            if (BaudRatePanel.Enabled)
            {
                PortNameCB_SelectedIndexChanged(null, null);
                this.BaudRateCB.SelectedItem = Target.BaudRate;
            }
            DataBitsPanel.Enabled = (Target.ConfigurableSettings & AvailableMediaSettings.DataBits) != 0;
            if (DataBitsPanel.Enabled)
            {
                this.DataBitsCB.Items.Add(7);
                this.DataBitsCB.Items.Add(8);
                this.DataBitsCB.SelectedItem = Target.DataBits;
            }

            ParityPanel.Enabled = (Target.ConfigurableSettings & AvailableMediaSettings.Parity) != 0;
            if (ParityPanel.Enabled)
            {
                this.ParityCB.Items.Add(System.IO.Ports.Parity.None);
                this.ParityCB.Items.Add(System.IO.Ports.Parity.Odd);
                this.ParityCB.Items.Add(System.IO.Ports.Parity.Even);
                this.ParityCB.Items.Add(System.IO.Ports.Parity.Mark);
                this.ParityCB.Items.Add(System.IO.Ports.Parity.Space);
                this.ParityCB.SelectedItem = Target.Parity;
            }

            StopBitsPanel.Enabled = (Target.ConfigurableSettings & AvailableMediaSettings.StopBits) != 0;
            if (StopBitsPanel.Enabled)
            {
                this.StopBitsCB.Items.Add(System.IO.Ports.StopBits.None);
                this.StopBitsCB.Items.Add(System.IO.Ports.StopBits.One);
                this.StopBitsCB.Items.Add(System.IO.Ports.StopBits.OnePointFive);
                this.StopBitsCB.Items.Add(System.IO.Ports.StopBits.Two);
                this.StopBitsCB.SelectedItem = Target.StopBits;
            }
        }       

        #endregion
        
    }
}
