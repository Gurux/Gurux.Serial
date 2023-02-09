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
using System.ComponentModel;
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
        private GXSerial _target;

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
            _target = target;
        }

        private void PortNameCB_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (BaudRatePanel.Enabled)
            {
                BaudRateCB.Items.Clear();
                BaudRateCB.DropDownStyle = ComboBoxStyle.DropDownList;
                //User can't change values when connection is open.
                if (_target.IsOpen)
                {
                    BaudRateCB.Items.Add(_target.BaudRate);
                    BaudRateCB.SelectedItem = 0;
                }
                else
                {
                    foreach (int it in _target.GetAvailableBaudRates(PortNameCB.Text))
                    {
                        if (it == 0)
                        {
                            BaudRateCB.DropDownStyle = ComboBoxStyle.DropDown;
                        }
                        else
                        {
                            BaudRateCB.Items.Add(it);
                        }
                    }
                    BaudRateCB.SelectedItem = _target.BaudRate;
                }
            }
            Dirty = true;
            if (propertyChanged != null)
            {
                propertyChanged(this, new PropertyChangedEventArgs("PortName"));
            }
            if (PortNameCB.SelectedIndex != -1)
            {
                _target.PortName = PortNameCB.Text;
            }
        }

        #region IGXPropertyPage Members

        void IGXPropertyPage.Apply()
        {
            _target.PortName = PortNameCB.Text;
            if (BaudRateCB.Enabled)
            {
                _target.BaudRate = Convert.ToInt32(BaudRateCB.Text);
            }
            if (DataBitsCB.Enabled)
            {
                _target.DataBits = Convert.ToInt32(DataBitsCB.Text);
            }
            if (ParityCB.Enabled)
            {
                _target.Parity = (System.IO.Ports.Parity)ParityCB.SelectedItem;
            }
            if (StopBitsCB.Enabled)
            {
                _target.StopBits = (System.IO.Ports.StopBits)StopBitsCB.SelectedItem;
            }
            if (FlowControlCb.Enabled)
            {
                _target.Handshake = (System.IO.Ports.Handshake)FlowControlCb.SelectedItem;
            }
            Dirty = false;
        }

        void IGXPropertyPage.Initialize()
        {
            //Update texts.
            Text = Resources.SettingsTxt;
            PortNameLbl.Text = Resources.PortNameTxt;
            BaudRateLbl.Text = Resources.BaudRateTxt;
            DataBitsLbl.Text = Resources.DataBitsTxt;
            ParityLbl.Text = Resources.ParityTxt;
            StopBitsLbl.Text = Resources.StopBitsTxt;
            FlowControlLbl.Text = Resources.FlowControlTxt;
            //Hide controls which user do not want to show.
            PortNamePanel.Enabled = (_target.ConfigurableSettings & AvailableMediaSettings.PortName) != 0;
            if (PortNamePanel.Enabled)
            {
                if (_target.AvailablePorts != null)
                {
                    PortNameCB.Items.AddRange(_target.AvailablePorts);
                }
                else
                {
                    PortNameCB.Items.AddRange(GXSerial.GetPortNames());
                }
                if (PortNameCB.Items.Contains(_target.PortName))
                {
                    PortNameCB.SelectedItem = _target.PortName;
                }
                else
                {
                    if (PortNameCB.Items.Count != 0)
                    {
                        PortNameCB.SelectedIndex = 0;
                    }
                    else
                    {
                        FlowControlPanel.Enabled = StopBitsPanel.Enabled = ParityPanel.Enabled = DataBitsPanel.Enabled = BaudRatePanel.Enabled = PortNameCB.Enabled = false;
                        return;
                    }
                }
            }
            BaudRatePanel.Enabled = (_target.ConfigurableSettings & AvailableMediaSettings.BaudRate) != 0;
            if (BaudRatePanel.Enabled)
            {
                PortNameCB_SelectedIndexChanged(null, null);
                BaudRateCB.SelectedItem = _target.BaudRate;
            }
            DataBitsPanel.Enabled = (_target.ConfigurableSettings & AvailableMediaSettings.DataBits) != 0;
            if (DataBitsPanel.Enabled)
            {
                DataBitsCB.Items.Add(7);
                DataBitsCB.Items.Add(8);
                DataBitsCB.SelectedItem = _target.DataBits;
            }

            ParityPanel.Enabled = (_target.ConfigurableSettings & AvailableMediaSettings.Parity) != 0;
            if (ParityPanel.Enabled)
            {
                ParityCB.Items.Add(System.IO.Ports.Parity.None);
                ParityCB.Items.Add(System.IO.Ports.Parity.Odd);
                ParityCB.Items.Add(System.IO.Ports.Parity.Even);
                ParityCB.Items.Add(System.IO.Ports.Parity.Mark);
                ParityCB.Items.Add(System.IO.Ports.Parity.Space);
                ParityCB.SelectedItem = _target.Parity;
            }

            StopBitsPanel.Enabled = (_target.ConfigurableSettings & AvailableMediaSettings.StopBits) != 0;
            if (StopBitsPanel.Enabled)
            {
                StopBitsCB.Items.Add(System.IO.Ports.StopBits.None);
                StopBitsCB.Items.Add(System.IO.Ports.StopBits.One);
                StopBitsCB.Items.Add(System.IO.Ports.StopBits.OnePointFive);
                StopBitsCB.Items.Add(System.IO.Ports.StopBits.Two);
                StopBitsCB.SelectedItem = _target.StopBits;
            }

            FlowControlPanel.Enabled = (_target.ConfigurableSettings & AvailableMediaSettings.Handshake) != 0;
            if (FlowControlPanel.Enabled)
            {
                FlowControlCb.Items.Add(System.IO.Ports.Handshake.None);
                FlowControlCb.Items.Add(System.IO.Ports.Handshake.XOnXOff);
                FlowControlCb.Items.Add(System.IO.Ports.Handshake.RequestToSend);
                FlowControlCb.Items.Add(System.IO.Ports.Handshake.RequestToSendXOnXOff);
                FlowControlCb.SelectedItem = _target.Handshake;
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
            foreach (Control it in Controls)
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
            foreach (Control it in Controls)
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
            if (BaudRateCB.SelectedIndex != -1)
            {
                _target.BaudRate = Convert.ToInt32(BaudRateCB.Text);
            }
        }

        private void DataBitsCB_SelectedIndexChanged(object sender, EventArgs e)
        {
            Dirty = true;
            if (propertyChanged != null)
            {
                propertyChanged(this, new PropertyChangedEventArgs("DataBits"));
            }
            if (DataBitsCB.SelectedIndex != -1)
            {
                _target.DataBits = Convert.ToInt32(DataBitsCB.Text);
            }
        }

        private void ParityCB_SelectedIndexChanged(object sender, EventArgs e)
        {
            Dirty = true;
            if (propertyChanged != null)
            {
                propertyChanged(this, new PropertyChangedEventArgs("Parity"));
            }
            if (ParityCB.SelectedIndex != -1)
            {
                _target.Parity = (System.IO.Ports.Parity)ParityCB.SelectedItem;
            }
        }

        private void StopBitsCB_SelectedIndexChanged(object sender, EventArgs e)
        {
            Dirty = true;
            if (propertyChanged != null)
            {
                propertyChanged(this, new PropertyChangedEventArgs("StopBits"));
            }
            if (StopBitsCB.SelectedIndex != -1)
            {
                _target.StopBits = (System.IO.Ports.StopBits)StopBitsCB.SelectedItem;
            }
        }

        private void FlowControlCb_SelectedIndexChanged(object sender, EventArgs e)
        {
            Dirty = true;
            if (propertyChanged != null)
            {
                propertyChanged(this, new PropertyChangedEventArgs("FlowControl"));
            }
            if (FlowControlCb.SelectedIndex != -1)
            {
                _target.Handshake = (System.IO.Ports.Handshake)FlowControlCb.SelectedItem;
            }
        }
    }
}