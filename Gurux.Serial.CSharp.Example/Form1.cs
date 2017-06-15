//
// --------------------------------------------------------------------------
//  Gurux Ltd
// 
//
//
// Filename:        $HeadURL: svn://utopia/projects/Old/GuruxSerial/GXSerial%20csharp%20Sample/Form1.cs $
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
using System.Text;
using Gurux.Common;
using System.Diagnostics;

namespace GXSerialSample
{
    internal partial class Form1 : System.Windows.Forms.Form
    {
        StatusDlg m_StatusDlg;

        #region Close
        /// <summary>
        /// Closes serial connection.
		/// </summary>
		/// <param name="eventSender"></param>
		/// <param name="eventArgs"></param>
        private void CloseBtn_Click(System.Object eventSender, System.EventArgs eventArgs)
        {
            try
            {
                gxSerial1.Close();
            }
            catch (Exception Ex)
            {
                MessageBox.Show(Ex.Message);
            }
        }
        #endregion //Close

        /// <summary>
        /// Disables buttons.
		/// </summary>
		/// <param name="eventSender"></param>
		/// <param name="eventArgs"></param>
		private void Form1_Load(System.Object eventSender, System.EventArgs eventArgs)
        {
            try
            {
                gxSerial1 = new Gurux.Serial.GXSerial();
                gxSerial1.Settings = GXSerialSample.Properties.Settings.Default.MediaSetting;
                gxSerial1.Trace = TraceLevel.Verbose;
                gxSerial1.OnTrace += new TraceEventHandler(gxSerial1_OnTrace);
                gxSerial1.OnError += new ErrorEventHandler(gxSerial1_OnError);
                gxSerial1.OnReceived += new ReceivedEventHandler(gxSerial1_OnReceived);
                gxSerial1.OnMediaStateChange += new MediaStateChangeEventHandler(gxSerial1_OnMediaStateChange);
                PropertiesBtn.Enabled = true;
                if (gxSerial1.IsOpen)
                {
                    gxSerial1_OnMediaStateChange(this, new MediaStateEventArgs(MediaState.Open));
                }
                else
                {
                    gxSerial1_OnMediaStateChange(this, new MediaStateEventArgs(MediaState.Closed));
                }
            }
            catch (Exception Ex)
            {
                MessageBox.Show(Ex.Message);
            }
        }

        void gxSerial1_OnTrace(object sender, TraceEventArgs e)
        {
            if ((e.Type & TraceTypes.Info) != 0)
            {
                System.Diagnostics.Debug.WriteLine(e.ToString());
            }
            else if ((e.Type & TraceTypes.Error) != 0)
            {
                System.Diagnostics.Debug.WriteLine(e.ToString());
            }
            else if ((e.Type & TraceTypes.Warning) != 0)
            {
                System.Diagnostics.Debug.WriteLine(e.ToString());
            }
            else if ((e.Type & TraceTypes.Sent) != 0)
            {
                System.Diagnostics.Debug.WriteLine("<- " + e.ToString());
            }
            else if ((e.Type & TraceTypes.Received) != 0)
            {
                System.Diagnostics.Debug.WriteLine("-> " + e.ToString());
            }
        }

        #region OnError
        /// <summary>
        /// Show occured error.
        /// </summary>
        private void gxSerial1_OnError(object sender, Exception ex)
        {
            try
            {
                gxSerial1.Close();
                MessageBox.Show(ex.Message);
            }
            catch (Exception Ex)
            {
                MessageBox.Show(Ex.Message);
            }
        }
        #endregion //OnError

        #region OnMediaStateChange
        /// <summary>
        /// Update UI when media state changes.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void gxSerial1_OnMediaStateChange(object sender, MediaStateEventArgs e)
        {
            try
            {
                bool bOpen = e.State == Gurux.Common.MediaState.Open;
                OpenBtn.Enabled = !bOpen;
                SendText.Enabled = bOpen;
                SendBtn.Enabled = bOpen;
                CloseBtn.Enabled = bOpen;
                ReceivedText.Enabled = bOpen;
                PacketCounterTimer.Enabled = bOpen;
            }
            catch (Exception Ex)
            {
                MessageBox.Show(Ex.Message);
            }

        }
        #endregion //OnMediaStateChange

        #region OnReceived

        private void OnReceived(object sender, ReceiveEventArgs e)
        {
            try
            {
                // Byte array received from GXSerial, and must be changed to chars.
                if (HexCB.Checked)
                {
                    //Show only last data in echo.
                    if (EchoCB.Checked)
                    {
                        ReceivedText.Text = BitConverter.ToString((byte[])e.Data);
                    }
                    else
                    {
                        ReceivedText.Text += BitConverter.ToString((byte[])e.Data);
                    }
                }
                else // Gets received data as string.
                {
                    //Show only last data in echo.
                    if (EchoCB.Checked)
                    {
                        ReceivedText.Text = System.Text.Encoding.ASCII.GetString((byte[])e.Data);
                    }
                    else
                    {
                        ReceivedText.Text += System.Text.Encoding.ASCII.GetString((byte[])e.Data);
                    }
                }
                if (EchoCB.Checked)
                {
                    gxSerial1.Send(e.Data);
                }
            }
            catch (Exception Ex)
            {
                MessageBox.Show(Ex.Message);
            }
        }

        private void gxSerial1_OnReceived(object sender, ReceiveEventArgs e)
        {
            try
            {
                if (this.InvokeRequired)
                {
                    this.BeginInvoke(new ReceivedEventHandler(OnReceived), sender, e);
                }
                else
                {
                    OnReceived(sender, e);
                }
            }
            catch (Exception Ex)
            {
                MessageBox.Show(Ex.Message);
            }
        }
        #endregion //OnReceived


        #region Open
        /// <summary>
        /// Opens serial port connection.
        /// </summary>
        private void OpenBtn_Click(System.Object eventSender, System.EventArgs eventArgs)
        {
            try
            {
                gxSerial1.Open();
                gxSerial1.DtrEnable = gxSerial1.RtsEnable = true;
            }
            catch (Exception Ex)
            {
                MessageBox.Show(Ex.Message);
            }
        }
        #endregion //Open

        /// <summary>
        /// Updates packet counters.
        /// </summary>
        /// <param name="eventSender"></param>
        /// <param name="eventArgs"></param>
		private void PacketCounterTimer_Tick(System.Object eventSender, System.EventArgs eventArgs)
        {
            try
            {
                ReceivedTB.Text = gxSerial1.BytesReceived.ToString();
                SentTB.Text = gxSerial1.BytesSent.ToString();
                gxSerial1.ResetByteCounters();
            }
            catch (Exception Ex)
            {
                MessageBox.Show(Ex.Message);
            }
        }

        #region Properties
        /// <summary>
        /// Shows GXSerial media properties.
        /// </summary>
		private void PropertiesBtn_Click(System.Object eventSender, System.EventArgs eventArgs)
        {
            try
            {
                if (gxSerial1.Properties(this))
                {
                    //Save settings.
                    GXSerialSample.Properties.Settings.Default.MediaSetting = gxSerial1.Settings;
                    GXSerialSample.Properties.Settings.Default.Save();
                }
            }
            catch (Exception Ex)
            {
                MessageBox.Show(Ex.Message);
            }
        }
        #endregion //Properties

        #region Send
        /// <summary>
        /// Sends data to the serial port.
        /// </summary>
        /// <param name="eventSender"></param>
        /// <param name="eventArgs"></param>
		private void SendBtn_Click(System.Object eventSender, System.EventArgs eventArgs)
        {
            try
            {
                ReceivedText.Text = string.Empty;
                if (SyncBtn.Checked) // Sends data synchronously.
                {
                    if (HexCB.Checked)
                    {
                        // Sends data as byte array.                        
                        lock (gxSerial1.Synchronous)
                        {
                            Gurux.Common.ReceiveParameters<byte[]> p = new Gurux.Common.ReceiveParameters<byte[]>()
                            {
                                WaitTime = Convert.ToInt32(WaitTimeTB.Text),
                                Eop = EOPText.Text,
                                Count = Convert.ToInt32(MinSizeTB.Text)
                            };
                            gxSerial1.Send(GXCommon.HexToBytes(SendText.Text, true));
                            if (gxSerial1.Receive(p))
                            {
                                ReceivedText.Text = GXCommon.ToHex(p.Reply, true);
                            }
                        }
                    }
                    else
                    {
                        // Sends data as ASCII string.
                        lock (gxSerial1.Synchronous)
                        {
                            Gurux.Common.ReceiveParameters<string> p = new Gurux.Common.ReceiveParameters<string>()
                            {
                                WaitTime = Convert.ToInt32(WaitTimeTB.Text),
                                Eop = EOPText.Text,
                                Count = Convert.ToInt32(MinSizeTB.Text)
                            };
                            gxSerial1.Send(SendText.Text);
                            if (gxSerial1.Receive(p))
                            {
                                ReceivedText.Text = Convert.ToString(p.Reply);
                            }
                        }
                    }
                }
                else // Sends data asynchronously.
                {
                    if (HexCB.Checked)
                    {
                        // Sends data as byte array.
                        gxSerial1.Send(GXCommon.HexToBytes(SendText.Text, true));
                    }
                    else
                    {
                        string data = SendText.Text;
                        // Sends data as ASCII string.
                        if (EOPText.Text == "\\r")
                        {
                            data += '\r';
                        }
                        if (EOPText.Text == "\\n")
                        {
                            data += '\n';
                        }
                        if (EOPText.Text == "\\r\\n")
                        {
                            data += '\r' + '\n';
                        }
                        gxSerial1.Send(data);
                    }
                }
            }
            catch (Exception Ex)
            {
                MessageBox.Show(Ex.Message);
            }
        }
        #endregion //Send

        private void StatusBtn_Click(System.Object eventSender, System.EventArgs eventArgs)
        {
            try
            {
                if (m_StatusDlg == null || !m_StatusDlg.Created)
                {
                    m_StatusDlg = new StatusDlg(gxSerial1);
                }
                if (!m_StatusDlg.Visible)// Shows dialog.
                {
                    m_StatusDlg.Show(this);
                }
                else // Activates dialog.
                {
                    m_StatusDlg.Focus();
                }
            }
            catch (Exception Ex)
            {
                MessageBox.Show(Ex.Message);
            }
        }

        private void Timer1_Timer()
        {
            try
            {
                SendBtn_Click(SendBtn, new System.EventArgs());
            }
            catch (Exception Ex)
            {
                MessageBox.Show(Ex.Message);
            }
        }

        /// <summary>
        /// End of Packet used only, when data is sent synchronously.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SyncBtn_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                // If True, no more data is expected to be received synchronously. 
                // If False, asynchronous data is not received.
                MinSizeTB.Enabled = WaitTimeTB.Enabled = SyncBtn.Checked;
            }
            catch (Exception Ex)
            {
                MessageBox.Show(Ex.Message);
            }
        }

        #region GetAvailablePorts
        /// <summary>
        /// Show available ports.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AvailablePortsBtn_Click(object sender, EventArgs e)
        {
            string[] ports = Gurux.Serial.GXSerial.GetPortNames();
            MessageBox.Show(string.Join(System.Environment.NewLine, ports));
        }
        #endregion //GetAvailablePorts
    }
}
