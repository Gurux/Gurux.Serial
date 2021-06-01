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
using Gurux.Serial.Properties;

namespace Gurux.Serial
{
    /// <summary>
    /// Settings dialog.
    /// </summary>
    partial class Settings : Form, IGXPropertyPage, INotifyPropertyChanged
    {
        GXSerial target;

        PropertyChangedEventHandler propertyChanged;

        /// <summary>
        /// Has user made any changes for the settings.
        /// </summary>
        public bool Dirty
        {
            get;
            set;
        }

        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged
        {
            add
            {
                propertyChanged += value;
            }
            remove
            {
                propertyChanged -= value;
            }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public Settings(GXSerial target)
        {
            InitializeComponent();
            this.target = target;
        }

        private void PortNameCB_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (BaudRatePanel.Enabled)
            {
                this.BaudRateCB.Items.Clear();
                BaudRateCB.DropDownStyle = ComboBoxStyle.DropDownList;
                //User can't change values when connection is open.
                if (target.IsOpen)
                {
                    BaudRateCB.Items.Add(target.BaudRate);
                    this.BaudRateCB.SelectedItem = 0;
                }
                else
                {
                    foreach (int it in target.GetAvailableBaudRates(PortNameCB.Text))
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
                    this.BaudRateCB.SelectedItem = target.BaudRate;
                }
            }
            Dirty = true;
            if (propertyChanged != null)
            {
                propertyChanged(this, new PropertyChangedEventArgs("PortName"));
            }
        }

        #region IGXPropertyPage Members

        void IGXPropertyPage.Apply()
        {
            target.PortName = this.PortNameCB.Text;
            if (this.BaudRateCB.Enabled)
            {
                target.BaudRate = Convert.ToInt32(this.BaudRateCB.Text);
            }
            if (this.DataBitsCB.Enabled)
            {
                target.DataBits = Convert.ToInt32(this.DataBitsCB.Text);
            }
            if (this.ParityCB.Enabled)
            {
                target.Parity = (System.IO.Ports.Parity)this.ParityCB.SelectedItem;
            }
            if (this.StopBitsCB.Enabled)
            {
                target.StopBits = (System.IO.Ports.StopBits)this.StopBitsCB.SelectedItem;
            }
            if (this.FlowControlCb.Enabled)
            {
                target.Handshake = (System.IO.Ports.Handshake)this.FlowControlCb.SelectedItem;
            }
            Dirty = false;
        }

        void IGXPropertyPage.Initialize()
        {
            //Update texts.
            this.Text = Resources.SettingsTxt;
            this.PortNameLbl.Text = Resources.PortNameTxt;
            this.BaudRateLbl.Text = Resources.BaudRateTxt;
            this.DataBitsLbl.Text = Resources.DataBitsTxt;
            this.ParityLbl.Text = Resources.ParityTxt;
            this.StopBitsLbl.Text = Resources.StopBitsTxt;
            this.FlowControlLbl.Text = Resources.FlowControlTxt;
            //Hide controls which user do not want to show.
            PortNamePanel.Enabled = (target.ConfigurableSettings & AvailableMediaSettings.PortName) != 0;
            if (PortNamePanel.Enabled)
            {
                if (target.AvailablePorts != null)
                {
                    PortNameCB.Items.AddRange(target.AvailablePorts);
                }
                else
                {
                    PortNameCB.Items.AddRange(GXSerial.GetPortNames());
                }
                if (this.PortNameCB.Items.Contains(target.PortName))
                {
                    this.PortNameCB.SelectedItem = target.PortName;
                }
                else
                {
                    if (PortNameCB.Items.Count != 0)
                    {
                        this.PortNameCB.SelectedIndex = 0;
                    }
                    else
                    {
                        FlowControlPanel.Enabled = StopBitsPanel.Enabled = ParityPanel.Enabled = DataBitsPanel.Enabled = BaudRatePanel.Enabled = this.PortNameCB.Enabled = false;
                        return;
                    }
                }
            }
            BaudRatePanel.Enabled = (target.ConfigurableSettings & AvailableMediaSettings.BaudRate) != 0;
            if (BaudRatePanel.Enabled)
            {
                PortNameCB_SelectedIndexChanged(null, null);
                this.BaudRateCB.SelectedItem = target.BaudRate;
            }
            DataBitsPanel.Enabled = (target.ConfigurableSettings & AvailableMediaSettings.DataBits) != 0;
            if (DataBitsPanel.Enabled)
            {
                this.DataBitsCB.Items.Add(7);
                this.DataBitsCB.Items.Add(8);
                this.DataBitsCB.SelectedItem = target.DataBits;
            }

            ParityPanel.Enabled = (target.ConfigurableSettings & AvailableMediaSettings.Parity) != 0;
            if (ParityPanel.Enabled)
            {
                this.ParityCB.Items.Add(System.IO.Ports.Parity.None);
                this.ParityCB.Items.Add(System.IO.Ports.Parity.Odd);
                this.ParityCB.Items.Add(System.IO.Ports.Parity.Even);
                this.ParityCB.Items.Add(System.IO.Ports.Parity.Mark);
                this.ParityCB.Items.Add(System.IO.Ports.Parity.Space);
                this.ParityCB.SelectedItem = target.Parity;
            }

            StopBitsPanel.Enabled = (target.ConfigurableSettings & AvailableMediaSettings.StopBits) != 0;
            if (StopBitsPanel.Enabled)
            {
                this.StopBitsCB.Items.Add(System.IO.Ports.StopBits.None);
                this.StopBitsCB.Items.Add(System.IO.Ports.StopBits.One);
                this.StopBitsCB.Items.Add(System.IO.Ports.StopBits.OnePointFive);
                this.StopBitsCB.Items.Add(System.IO.Ports.StopBits.Two);
                this.StopBitsCB.SelectedItem = target.StopBits;
            }

            FlowControlPanel.Enabled = (target.ConfigurableSettings & AvailableMediaSettings.Handshake) != 0;
            if (FlowControlPanel.Enabled)
            {
                this.FlowControlCb.Items.Add(System.IO.Ports.Handshake.None);
                this.FlowControlCb.Items.Add(System.IO.Ports.Handshake.XOnXOff);
                this.FlowControlCb.Items.Add(System.IO.Ports.Handshake.RequestToSend);
                this.FlowControlCb.Items.Add(System.IO.Ports.Handshake.RequestToSendXOnXOff);
                this.FlowControlCb.SelectedItem = target.Handshake;
            }
            UpdateEditBoxSizes();
            Dirty = false;
        }

        /// <summary>
        /// Because label lenght depends from the localization string, edit box sizes must be update.
        /// </summary>
        private void UpdateEditBoxSizes()
        {
            //Find max length of the localization string.
            int maxLength = 0;
            foreach (Control it in this.Controls)
            {
                if (it.Enabled)
                {
                    foreach (Control it2 in it.Controls)
                    {
                        if (it2 is Label && it2.Right > maxLength)
                        {
                            maxLength = it2.Right;
                        }
                    }
                }
            }
            //Increase edit control length.
            foreach (Control it in this.Controls)
            {
                if (it.Enabled)
                {
                    foreach (Control it2 in it.Controls)
                    {
                        if (it2 is ComboBox)
                        {
                            it2.Width += it2.Left - maxLength - 10;
                            it2.Left = maxLength + 10;
                        }
                    }
                }
            }
        }

        #endregion

        private void BaudRateCB_SelectedIndexChanged(object sender, EventArgs e)
        {
            Dirty = true;
            if (propertyChanged != null)
            {
                propertyChanged(this, new PropertyChangedEventArgs("BaudRate"));
            }
        }

        private void DataBitsCB_SelectedIndexChanged(object sender, EventArgs e)
        {
            Dirty = true;
            if (propertyChanged != null)
            {
                propertyChanged(this, new PropertyChangedEventArgs("DataBits"));
            }
        }

        private void ParityCB_SelectedIndexChanged(object sender, EventArgs e)
        {
            Dirty = true;
            if (propertyChanged != null)
            {
                propertyChanged(this, new PropertyChangedEventArgs("Parity"));
            }
        }

        private void StopBitsCB_SelectedIndexChanged(object sender, EventArgs e)
        {
            Dirty = true;
            if (propertyChanged != null)
            {
                propertyChanged(this, new PropertyChangedEventArgs("StopBits"));
            }
        }

        private void FlowControlCb_SelectedIndexChanged(object sender, EventArgs e)
        {
            Dirty = true;
            if (propertyChanged != null)
            {
                propertyChanged(this, new PropertyChangedEventArgs("FlowControl"));
            }
        }
    }
}