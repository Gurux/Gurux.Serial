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
using System.Linq;
using System.Text;
using System.ComponentModel;
using Gurux.Common;
using System.IO;
using Gurux.Shared;
using System.Windows.Forms;
using System.Xml;
using System.Diagnostics;
using System.IO.Ports;
using System.Reflection;
using System.Threading;

namespace Gurux.Serial
{
    class ReceiveThread
    {
        public ManualResetEvent Closing;
        GXSerial m_Parent;
        public ReceiveThread(GXSerial parent)
        {
            Closing = new ManualResetEvent(false);            
            m_Parent = parent;
        }

        /// <summary>
        /// Receive data from the server using the established socket connection
        /// </summary>
        /// <returns>The data received from the server</returns>
        public void Receive()
        {
            try
            {                				
                while (!Closing.WaitOne(1))
                {		
                    if (m_Parent.BytesToRead > 0)
                    {									
                        m_Parent.GXSerial_DataReceived(m_Parent, null);                        
                    }
                    else
                    {
                        Closing.WaitOne(100);
                    }
                }
            }
            catch (Exception ex)
            {
                m_Parent.NotifyError(ex);
                if (!Closing.WaitOne(1))
                {
                    m_Parent.Close();
                }
            }            
        }
    }

	/// <summary>
	/// A media component that enables communication of serial port.
	/// </summary>
    public class GXSerial : Gurux.Common.IGXMedia, INotifyPropertyChanged, IDisposable
    {
        TraceLevel m_Trace;
        static Dictionary<string, List<int>> BaudRates = new Dictionary<string, List<int>>();
        object m_Eop;
        GXSynchronousMediaBase m_syncBase;
        UInt64 m_BytesSent, m_BytesReceived;
        readonly object m_Synchronous = new object();        
		readonly object m_baseLock = new object();        
        internal System.IO.Ports.SerialPort m_base = new System.IO.Ports.SerialPort();
        ReceiveThread m_Receiver;
        Thread m_ReceiverThread;

		/// <summary>
		/// Get baud rates supported by given serial port.
		/// </summary>
        static public int[] GetAvailableBaudRates(string portName)
        {
            if (BaudRates.ContainsKey(portName.ToLower()))
            {
                return BaudRates[portName.ToLower()].ToArray();
            }
            List<int> items = new List<int>();
            if (string.IsNullOrEmpty(portName))
            {
                string[] ports = GXSerial.GetPortNames();
                if (ports.Length != 0)
                {
                    portName = GXSerial.GetPortNames()[0];
                }
                else
                {
                    portName = null;
                }
            }
            if (portName != null)
            {
                BaudRates[portName.ToLower()] = items;
                try
                {
                    Int32 value = 0;
                    using (SerialPort port = new SerialPort(portName))
                    {
                        port.Open();
                        FieldInfo fi = port.BaseStream.GetType().GetField("commProp", BindingFlags.Instance | BindingFlags.NonPublic);
                        if (fi != null)
                        {
                            object p = fi.GetValue(port.BaseStream);
                            value = (Int32)p.GetType().GetField("dwSettableBaud", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).GetValue(p);
                        }
                    }
                    if (value != 0)
                    {
                        if ((value & 0x1) != 0)
                        {
                            items.Add(75);
                        }
                        if ((value & 0x2) != 0)
                        {
                            items.Add(110);
                        }
                        if ((value & 0x8) != 0)
                        {
                            items.Add(150);
                        }
                        if ((value & 0x10) != 0)
                        {
                            items.Add(300);
                        }
                        if ((value & 0x20) != 0)
                        {
                            items.Add(600);
                        }
                        if ((value & 0x40) != 0)
                        {
                            items.Add(1200);
                        }
                        if ((value & 0x80) != 0)
                        {
                            items.Add(1800);
                        }
                        if ((value & 0x100) != 0)
                        {
                            items.Add(2400);
                        }
                        if ((value & 0x200) != 0)
                        {
                            items.Add(4800);
                        }
                        if ((value & 0x400) != 0)
                        {
                            items.Add(7200);
                        }
                        if ((value & 0x800) != 0)
                        {
                            items.Add(9600);
                        }
                        if ((value & 0x1000) != 0)
                        {
                            items.Add(14400);
                        }
                        if ((value & 0x2000) != 0)
                        {
                            items.Add(19200);
                        }
                        if ((value & 0x4000) != 0)
                        {
                            items.Add(38400);
                        }
                        if ((value & 0x8000) != 0)
                        {
                            items.Add(56000);
                        }
                        if ((value & 0x40000) != 0)
                        {
                            items.Add(57600);
                        }
                        if ((value & 0x20000) != 0)
                        {
                            items.Add(115200);
                        }
                        if ((value & 0x10000) != 0)
                        {
                            items.Add(128000);
                        }
                        if ((value & 0x10000000) != 0) //Programmable baud rate.
                        {
                            items.Add(0);
                        }
                    }
                }
                catch
                {
                    items.Clear();
                }
            }
			//Add default baud rates.
			if (items.Count == 0)
			{
				items.Add(300);
                items.Add(600);
                items.Add(1800);
                items.Add(2400);
                items.Add(4800);
                items.Add(9600);
                items.Add(19200);
                items.Add(38400);
                items.Add(0); //Programmable baud rate.	
			}
            return items.ToArray();
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public GXSerial()
        {
            ConfigurableSettings = AvailableMediaSettings.All;
            m_syncBase = new GXSynchronousMediaBase(1024);            
            //Events are not currently implemented in Mono's serial port.
            if (Environment.OSVersion.Platform != PlatformID.Unix)
            {
                m_base.DataReceived += new System.IO.Ports.SerialDataReceivedEventHandler(GXSerial_DataReceived);
            }         
        }

        internal void NotifyError(Exception ex)
        {
            if (m_OnError != null)
            {
                m_OnError(this, ex);
            }
            if (m_Trace >= TraceLevel.Error && m_OnTrace != null)
            {
                m_OnTrace(this, new TraceEventArgs(TraceTypes.Error, ex));
            }
        }

        void NotifyMediaStateChange(MediaState state)
        {
            if (m_Trace >= TraceLevel.Info && m_OnTrace != null)
            {
                m_OnTrace(this, new TraceEventArgs(TraceTypes.Info, state));
            }
            if (m_OnMediaStateChange != null)
            {
                m_OnMediaStateChange(this, new MediaStateEventArgs(state));
            }
        }

		/// <summary>
		/// What level of tracing is used.
		/// </summary>
		public TraceLevel Trace
		{
            get
            {
                return m_Trace;
            }
            set
            {
                m_Trace = m_syncBase.Trace = value;
            }
		}

        /// <summary>
        /// Gets the underlying System.IO.Stream object for a System.IO.Ports.SerialPort object.
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public Stream BaseStream
        {
            get
            {
                lock (m_baseLock)
                {
                    return m_base.BaseStream;
                }
            }
        }

        internal void GXSerial_DataReceived(object sender, System.IO.Ports.SerialDataReceivedEventArgs e)
        {
            try
            {
                int count = 0, index = 0;
                byte[] buff = null;
                if (this.IsSynchronous)
                {
                    TraceEventArgs arg = null;                    
                    lock (m_syncBase.m_ReceivedSync)
                    {
                        int totalCount = 0;
                        index = m_syncBase.m_ReceivedSize;
                        while (this.IsOpen && (count = this.BytesToRead) != 0)
                        {
                            totalCount += count;                            
                            buff = new byte[count];                            
                            m_base.Read(buff, 0, count);
                            m_syncBase.AppendData(buff, 0, count);                                                        
                            m_BytesReceived += (uint)count;
                        }
                        if (totalCount != 0 && Eop != null) //Search Eop if given.
                        {
                            if (Eop is Array)
                            {                                
                                foreach (object eop in (Array)Eop)
                                {
                                    totalCount = GXCommon.IndexOf(m_syncBase.m_Received, GXCommon.GetAsByteArray(eop), index, m_syncBase.m_ReceivedSize);
                                    if (totalCount != -1)
                                    {
                                        break;
                                    }
                                }
                            }
                            else
                            {
                                totalCount = GXCommon.IndexOf(m_syncBase.m_Received, GXCommon.GetAsByteArray(Eop), index, m_syncBase.m_ReceivedSize);
                            }
                        }
                        if (totalCount != -1)
                        {
                            if (totalCount != 0 && m_Trace == TraceLevel.Verbose && m_OnTrace != null)
                            {
                                arg = new TraceEventArgs(TraceTypes.Received, m_syncBase.m_Received, 0, totalCount + 1);
                            }
                            m_syncBase.m_ReceivedEvent.Set();                            
                        }
                    }
                    if (arg != null)
                    {
                        m_OnTrace(this, arg);
                    }
                }
                else if (this.m_OnReceived != null)
                {
                    int totalCount = 0;
                    while (this.IsOpen && (count = this.BytesToRead) != 0)
                    {
                        index = m_syncBase.m_ReceivedSize;
                        buff = new byte[count];
                        totalCount += count;
                        m_base.Read(buff, 0, count);
                        m_BytesReceived += (uint)count;                        
                        if (Eop != null) //Search Eop if given.
                        {                            
                            m_syncBase.AppendData(buff, 0, count);
                            if (Eop is Array)
                            {
                                foreach (object eop in (Array)Eop)
                                {
                                    totalCount = GXCommon.IndexOf(m_syncBase.m_Received, GXCommon.GetAsByteArray(Eop), index, m_syncBase.m_ReceivedSize);
                                    if (totalCount != -1)
                                    {
                                        break;
                                    }
                                }
                            }
                            else
                            {
                                totalCount = GXCommon.IndexOf(m_syncBase.m_Received, GXCommon.GetAsByteArray(Eop), index, m_syncBase.m_ReceivedSize);
                            }
                            if (totalCount != -1)
                            {
                                buff = new byte[m_syncBase.m_ReceivedSize];
                                Array.Copy(m_syncBase.m_Received, buff, m_syncBase.m_ReceivedSize);
                                m_syncBase.m_ReceivedSize = 0;
                                m_OnReceived(this, new ReceiveEventArgs(buff, this.PortName));
                                if (buff != null && m_Trace == TraceLevel.Verbose && m_OnTrace != null)
                                {
                                    m_OnTrace(this, new TraceEventArgs(TraceTypes.Received, buff, index, totalCount - index));
                                }
                            }
                        }
                        else
                        {
                            m_OnReceived(this, new ReceiveEventArgs(buff, this.PortName));
                            if (buff != null && m_Trace == TraceLevel.Verbose && m_OnTrace != null)
                            {
                                m_OnTrace(this, new TraceEventArgs(TraceTypes.Received, buff));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (this.IsSynchronous)
                {
                    m_syncBase.Exception = ex;
                    m_syncBase.m_ReceivedEvent.Set();
                }
                else
                {
                    NotifyError(ex);
                }
            }
        }

		/// <summary>
		/// Used baud rate for communication.
		/// </summary>
		/// <remarks>Can be changed without disconnecting.</remarks>
        [Browsable(true)]
        [DefaultValue(9600)]
        [MonitoringDescription("BaudRate")]
        public int BaudRate 
        {
            get
            {
                lock (m_baseLock)
                {
                    return m_base.BaudRate;
                }
            }
            set
            {
                lock (m_baseLock)
                {
                    bool change = m_base.BaudRate != value;
                    m_base.BaudRate = value;
                    if (change)
                    {
                        NotifyPropertyChanged("BaudRate");
                    }
                }
            }
        }

		/// <summary>
		/// True if the port is in a break state; otherwise, false.
		/// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public bool BreakState
        {
            get
            {
				lock(m_baseLock)
				{
	                return m_base.BreakState;
				}
            }
            set
            {                
				bool change;
				lock(m_baseLock)
				{
	                change = m_base.BreakState != value;
	                m_base.BreakState = value;
				}
	            if (change)
                {
                    NotifyPropertyChanged("BreakState");
                }
            }
        }

		/// <summary>
		/// Gets the number of bytes in the receive buffer.
		/// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public int BytesToRead
        {
            get
            {
				lock(m_baseLock)
				{
                	return m_base.BytesToRead;
				}
            }
        }
       
		/// <summary>
		/// Gets the number of bytes in the send buffer.
		/// </summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int BytesToWrite
        {
            get
            {
				lock(m_baseLock)
				{
	                return m_base.BytesToWrite;
				}
            }
        }

		/// <summary>
		/// Gets the state of the Carrier Detect line for the port.
		/// </summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool CDHolding 
        {
            get
            {
				lock(m_baseLock)
				{
                	return m_base.CDHolding;
				}
            }
        }
       
		/// <summary>
		/// Gets the state of the Clear-to-Send line.
		/// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public bool CtsHolding
        {
            get
            {
				lock(m_baseLock)
				{
                	return m_base.CtsHolding;
				}
            }
        }

		/// <summary>
		/// Gets or sets the standard length of data bits per byte.
		/// </summary>
        [MonitoringDescription("DataBits")]
        [DefaultValue(8)]
        [Browsable(true)]
        public int DataBits
        {
            get
            {
				lock(m_baseLock)
				{
                	return m_base.DataBits;
				}
            }
            set
            {
                bool change;
				lock(m_baseLock)
				{
					change = m_base.DataBits != value;
                	m_base.DataBits = value;
				}
                if (change)
                {
                    NotifyPropertyChanged("DataBits");
                }
            }
        }
       
		/// <summary>
		/// Gets or sets a value indicating whether null bytes are ignored when transmitted between the port and the receive buffer.
		/// </summary>
        [Browsable(true)]
        [DefaultValue(false)]
        [MonitoringDescription("DiscardNull")]
        public bool DiscardNull
        {
            get
            {
				lock(m_baseLock)
				{
                	return m_base.DiscardNull;
				}
            }
            set
            {                
                bool change;
				lock(m_baseLock)
				{
					change = m_base.DiscardNull != value;
                	m_base.DiscardNull = value;
				}
                if (change)
                {
                    NotifyPropertyChanged("DiscardNull");
                }
            }
        }

		/// <summary>
		/// Gets the state of the Data Set Ready (DSR) signal.
		/// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public bool DsrHolding
        {
            get
            {
				lock(m_baseLock)
				{
                	return m_base.DsrHolding;
				}
            }
        }
       
		/// <summary>
		/// Gets or sets a value that enables the Data Terminal Ready (DTR) signal during serial communication.
		/// </summary>
        [DefaultValue(false)]
        [MonitoringDescription("DtrEnable")]
        [Browsable(true)]
        public bool DtrEnable
        {
            get
            {
				lock(m_baseLock)
				{
                	return m_base.DtrEnable;
				}
            }
            set
            {                
                bool change;
				lock(m_baseLock)
				{
					change = m_base.DtrEnable != value;
                	m_base.DtrEnable = value;
				}
                if (change)
                {
                    NotifyPropertyChanged("DtrEnable");
                }
            }
        }
       
		/// <summary>
		/// Gets or sets the byte encoding for pre- and post-transmission conversion of text.
		/// </summary>
        [MonitoringDescription("Encoding")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public Encoding Encoding
        {
            get
            {
				lock(m_baseLock)
				{
                	return m_base.Encoding;
				}
            }
            set
            {                
                bool change;
				lock(m_baseLock)
				{
					change = m_base.Encoding != value;
                	m_base.Encoding = value;
				}
                if (change)
                {
                    NotifyPropertyChanged("Encoding");
                }
            }
        }
        
		/// <summary>
		/// Gets or sets the handshaking protocol for serial port transmission of data.
		/// </summary>
        [Browsable(true)]
        [MonitoringDescription("Handshake")]
        public Handshake Handshake
        {
            get
            {
				lock(m_baseLock)
				{
                	return m_base.Handshake;
				}
            }
            set
            {                
                bool change;
				lock(m_baseLock)
				{
					change = m_base.Handshake != value;
                	m_base.Handshake = value;
				}
                if (change)
                {
                    NotifyPropertyChanged("Handshake");
                }
            }
        }
        
		/// <summary>
		/// Gets a value indicating the open or closed status of the System.IO.Ports.SerialPort object.
		/// </summary>
        [Browsable(false)]
        public bool IsOpen
        {
            get
            {
				lock(m_baseLock)
				{
                	return m_base.IsOpen;
				}
            }            
        }

		/// <summary>
		/// Gets or sets the parity-checking protocol.
		/// </summary>
        [Browsable(true)]
        [MonitoringDescription("Parity")]
        public Parity Parity
        {
            get
            {
				lock(m_baseLock)
				{
                	return m_base.Parity;
				}
            }
            set
            {                
                bool change;
				lock(m_baseLock)
				{
					change = m_base.Parity != value;
                	m_base.Parity = value;
				}
                if (change)
                {
                    NotifyPropertyChanged("Parity");
                }
            }
        }

		/// <summary>
		/// Gets or sets the byte that replaces invalid bytes in a data stream when a parity error occurs.
		/// </summary>
        [Browsable(true)]
        [MonitoringDescription("ParityReplace")]
        [DefaultValue(63)]
        public byte ParityReplace
        {
            get
            {
				lock(m_baseLock)
				{
                	return m_base.ParityReplace;
				}
            }
            set
            {                
                bool change;
				lock(m_baseLock)
				{
					change = m_base.ParityReplace != value;
                	m_base.ParityReplace = value;
				}
                if (change)
                {
                    NotifyPropertyChanged("ParityReplace");
                }
            }
        }
       
		/// <summary>
		/// Gets or sets the port for communications, including but not limited to all available COM ports.
		/// </summary>
        [MonitoringDescription("PortName")]
        [Browsable(true)]
        [DefaultValue("COM1")]
        public string PortName
        {
            get
            {
				lock(m_baseLock)
				{
                	return m_base.PortName;
				}
            }
            set
            {               
                bool change;
				lock(m_baseLock)
				{
					change = m_base.PortName != value;
                	m_base.PortName = value;
				}
                if (change)
                {
                    NotifyPropertyChanged("PortName");
                }
            }
        }
        
		/// <summary>
		/// Gets or sets the size of the System.IO.Ports.SerialPort input buffer.
		/// </summary>
        [DefaultValue(4096)]
        [MonitoringDescription("ReadBufferSize")]
        [Browsable(true)]
        public int ReadBufferSize
        {
            get
            {
				lock(m_baseLock)
				{
                	return m_base.ReadBufferSize;
				}
            }
            set
            {                
                bool change;
				lock(m_baseLock)
				{
					change = m_base.ReadBufferSize != value;
                	m_base.ReadBufferSize = value;
				}
                if (change)
                {
                    NotifyPropertyChanged("ReadBufferSize");
                }
            }
        }
       
		/// <summary>
		/// Gets or sets the number of milliseconds before a time-out occurs when a read operation does not finish.
		/// </summary>
        [MonitoringDescription("ReadTimeout")]
        [Browsable(true)]
        [DefaultValue(-1)]
        public int ReadTimeout
        {
            get
            {
				lock(m_baseLock)
				{
                	return m_base.ReadTimeout;
				}
            }
            set
            {                
                bool change;
				lock(m_baseLock)
				{
					change = m_base.ReadTimeout != value;
                	m_base.ReadTimeout = value;
				}
                if (change)
                {
                    NotifyPropertyChanged("ReadTimeout");
                }
            }
        }

		/// <summary>
		/// Gets or sets the number of bytes in the internal input buffer before a System.IO.Ports.SerialPort.DataReceived event occurs.
		/// </summary>
        [MonitoringDescription("ReceivedBytesThreshold")]
        [DefaultValue(1)]
        [Browsable(true)]
        public int ReceivedBytesThreshold
        {
            get
            {
				lock(m_baseLock)
				{
                	return m_base.ReceivedBytesThreshold;
				}
            }
            set
            {                
                bool change;
				lock(m_baseLock)
				{
					change = m_base.ReceivedBytesThreshold != value;
                	m_base.ReceivedBytesThreshold = value;
				}
                if (change)
                {
                    NotifyPropertyChanged("ReceivedBytesThreshold");
                }
            }
        }

		/// <summary>
		/// Gets or sets a value indicating whether the Request to Send (RTS) signal is enabled during serial communication.
		/// </summary>
        [MonitoringDescription("RtsEnable")]
        [DefaultValue(false)]
        [Browsable(true)]
        public bool RtsEnable
        {
            get
            {
				lock(m_baseLock)
				{
                	return m_base.RtsEnable;
				}
            }
            set
            {                
                bool change;
				lock(m_baseLock)
				{
					change = m_base.RtsEnable != value;
                	m_base.RtsEnable = value;
				}
                if (change)
                {
                    NotifyPropertyChanged("RtsEnable");
                }
            }
        }
       
		/// <summary>
		/// Gets or sets the standard number of stopbits per byte.
		/// </summary>
        [MonitoringDescription("StopBits")]
        [Browsable(true)]
        public StopBits StopBits
        {
            get
            {
				lock(m_baseLock)
				{
                	return m_base.StopBits;
				}
            }
            set
            {                
                bool change;
				lock(m_baseLock)
				{
					change = m_base.StopBits != value;
                	m_base.StopBits = value;
				}
                if (change)
                {
                    NotifyPropertyChanged("StopBits");
                }
            }
        }
        
		/// <summary>
		/// Gets or sets the size of the serial port output buffer.
		/// </summary>
        [Browsable(true)]
        [DefaultValue(2048)]
        [MonitoringDescription("WriteBufferSize")]
        public int WriteBufferSize
        {
            get
            {
				lock(m_baseLock)
				{
                	return m_base.WriteBufferSize;
				}
            }
            set
            {
                bool change;
				lock(m_baseLock)
				{
					change = m_base.WriteBufferSize != value;
                	m_base.WriteBufferSize = value;
				}
                if (change)
                {
                    NotifyPropertyChanged("WriteBufferSize");
                }
            }
        }
        
		/// <summary>
		/// Gets or sets the number of milliseconds before a time-out occurs when a write operation does not finish.
		/// </summary>
        [MonitoringDescription("WriteTimeout")]
        [Browsable(true)]
        [DefaultValue(-1)]
        public int WriteTimeout         
        {
            get
            {
				lock(m_baseLock)
				{
                	return m_base.WriteTimeout;
				}
            }
            set
            {                
                bool change;
				lock(m_baseLock)
				{
					change = m_base.WriteTimeout != value;
                	m_base.WriteTimeout = value;
				}
                if (change)
                {
                    NotifyPropertyChanged("WriteTimeout");
                }
            }
        }

		/// <summary>
		/// Closes the port connection, sets the System.IO.Ports.SerialPort.IsOpen property to false, and disposes of the internal System.IO.Stream object.
		/// </summary>
        public void Close()
        {
			bool bOpen;
			lock(m_baseLock)
			{
				bOpen = m_base.IsOpen;
			}
            if (bOpen)
            {
                try
                {
                    NotifyMediaStateChange(MediaState.Closing);
                }
                catch (Exception ex)
                {
                    NotifyError(ex);
                    throw;
                }
                finally
                {
                    try
                    {
                        if (m_Receiver != null)
                        {
                            m_Receiver.Closing.Set();
                        }
                        lock (m_baseLock)
                        {
                            m_base.Close();
                        }
                        if (m_ReceiverThread != null && m_ReceiverThread.IsAlive)
                        {
                            m_ReceiverThread.Join();
                        }                        
                    }
                    catch
                    {
                        //Ignore all errors on close.
                    }
                    NotifyMediaStateChange(MediaState.Closed);
                }
            }
        }

		/// <summary>
		/// Discards data from the serial driver's receive buffer.
		/// </summary>
        public void DiscardInBuffer()
        {
			lock(m_baseLock)
			{
            	m_base.DiscardInBuffer();
			}
        }
        
		/// <summary>
		/// Discards data from the serial driver's transmit buffer.
		/// </summary>
		public void DiscardOutBuffer()
        {
			lock(m_baseLock)
			{
            	m_base.DiscardOutBuffer();
			}
        }

		/// <summary>
		/// Gets an array of serial port names for the current computer.
		/// </summary>
		/// <returns></returns>
        public static string[] GetPortNames()
        {
           	return System.IO.Ports.SerialPort.GetPortNames();
        }

		/// <summary>
		/// User defined available ports.
		/// </summary>
		/// <remarks>If this is not set ports are retrieved from current system.</remarks>
		public string[] AvailablePorts
		{
			get;
			set;
		}

		/// <summary>
		/// Opens a new serial port connection.
		/// </summary>
        public void Open()
        {
            Close();
            try
            {
                lock (m_syncBase.m_ReceivedSync)
                {
                    m_syncBase.m_LastPosition = 0;
                }
                NotifyMediaStateChange(MediaState.Opening);
                if (m_Trace >= TraceLevel.Info && m_OnTrace != null)
                {
                    string eop = "None";
                    if (Eop is byte[])
                    {
                        eop = BitConverter.ToString(Eop as byte[], 0);
                    }
                    else if (Eop != null)
                    {
                        eop = Eop.ToString();
                    }
                    m_OnTrace(this, new TraceEventArgs(TraceTypes.Info, "Settings: Port: " + this.PortName + 
                                " Baud Rate: " + BaudRate + " Data Bits: " + DataBits.ToString() + " Parity: " 
                                + Parity.ToString() + " Stop Bits: " + StopBits.ToString() + " Eop:" + eop));
                }
				lock(m_baseLock)
				{
                	m_base.Open();
				}
                //Events are not currently implemented in Mono's serial port.
                if (Environment.OSVersion.Platform == PlatformID.Unix)
                {
                    m_Receiver = new ReceiveThread(this);
                    m_ReceiverThread = new Thread(new ThreadStart(m_Receiver.Receive));
                    m_ReceiverThread.IsBackground = true;
                    m_ReceiverThread.Start();
                }
                NotifyMediaStateChange(MediaState.Open);
            }
            catch
            {
                Close();
                throw;
            }
        }       

        #region Events
        /// <summary>
        /// GXNet component sends received data through this method.
        /// </summary>
        [Description("GXNet component sends received data through this method.")]
        public event ReceivedEventHandler OnReceived
        {
            add
            {
                m_OnReceived += value;
            }
            remove
            {
                m_OnReceived -= value;
            }
        }

        /// <summary>
        /// Errors that occur after the connection is established, are sent through this method. 
        /// </summary>       
        [Description("Errors that occur after the connection is established, are sent through this method.")]
        public event Gurux.Common.ErrorEventHandler OnError
        {
            add
            {

                m_OnError += value;
            }
            remove
            {
                m_OnError -= value;
            }
        }

        /// <summary>
        /// Media component sends notification, when its state changes.
        /// </summary>       
        [Description("Media component sends notification, when its state changes.")]
        public event MediaStateChangeEventHandler OnMediaStateChange
        {
            add
            {
                m_OnMediaStateChange += value;
            }
            remove
            {
                m_OnMediaStateChange -= value;
            }
        }

        /// <summary>
        /// Called when the client is establishing a connection with a Net Server.
        /// </summary>
        [Description("Called when the client is establishing a connection with a Net Server.")]
        public event ClientConnectedEventHandler OnClientConnected
        {
            add
            {
                m_OnClientConnected += value;
            }
            remove
            {
                m_OnClientConnected -= value;
            }
        }

        /// <summary>
        /// Called when the client has been disconnected from the network server.
        /// </summary>
        [Description("Called when the client has been disconnected from the network server.")]
        public event ClientDisconnectedEventHandler OnClientDisconnected
        {
            add
            {
                m_OnClientDisconnected += value;
            }
            remove
            {
                m_OnClientDisconnected -= value;
            }
        }

        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged
        {
            add
            {
                m_OnPropertyChanged += value;
            }
            remove
            {
                m_OnPropertyChanged -= value;
            }
        }
        
        /// <inheritdoc cref="TraceEventHandler"/>
        [Description("Called when the Media is sending or receiving data.")]
        public event TraceEventHandler OnTrace
        {
            add
            {
                m_OnTrace += value;
            }
            remove
            {
                m_OnTrace -= value;
            }
        }        

        private void NotifyPropertyChanged(string info)
        {
            if (m_OnPropertyChanged != null)
            {
                m_OnPropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }


        //Events
        TraceEventHandler m_OnTrace;
        PropertyChangedEventHandler m_OnPropertyChanged;
        MediaStateChangeEventHandler m_OnMediaStateChange;
        ClientConnectedEventHandler m_OnClientConnected;
        ClientDisconnectedEventHandler m_OnClientDisconnected;
        internal Gurux.Common.ErrorEventHandler m_OnError;
        ReceivedEventHandler m_OnReceived;

        #endregion //Events

        /// <inheritdoc cref="IGXMedia.ConfigurableSettings"/>
        public AvailableMediaSettings ConfigurableSettings
        {
            get
            {
                return (Gurux.Serial.AvailableMediaSettings)((IGXMedia)this).ConfigurableSettings;
            }
            set
            {
                ((IGXMedia)this).ConfigurableSettings = (int)value;
            }
        }


        /// <inheritdoc cref="IGXMedia.Synchronous"/>
        public object Synchronous
        {
            get
            {
                return m_Synchronous;
            }
        }

        /// <inheritdoc cref="IGXMedia.IsSynchronous"/>
        public bool IsSynchronous
        {
            get
            {
                bool reserved = System.Threading.Monitor.TryEnter(m_Synchronous, 0);
                if (reserved)
                {
                    System.Threading.Monitor.Exit(m_Synchronous);
                }
                return !reserved;
            }
        }

        /// <inheritdoc cref="IGXMedia.ResetSynchronousBuffer"/>
        public void ResetSynchronousBuffer()
        {
            lock (m_syncBase.m_ReceivedSync)
            {
                m_syncBase.m_ReceivedSize = 0;
            }
        }

        #region IGXMedia Members

        /// <summary>
        /// Sent byte count.
        /// </summary>
        /// <seealso cref="BytesReceived">BytesReceived</seealso>
        /// <seealso cref="ResetByteCounters">ResetByteCounters</seealso>
        [Browsable(false)]
        public UInt64 BytesSent
        {
            get
            {
                return m_BytesSent;
            }
        }

        /// <summary>
        /// Received byte count.
        /// </summary>
        /// <seealso cref="BytesSent">BytesSent</seealso>
        /// <seealso cref="ResetByteCounters">ResetByteCounters</seealso>
        [Browsable(false)]
        public UInt64 BytesReceived
        {
            get
            {
                return m_BytesReceived;
            }
        }

        /// <summary>
        /// Resets BytesReceived and BytesSent counters.
        /// </summary>
        /// <seealso cref="BytesSent">BytesSent</seealso>
        /// <seealso cref="BytesReceived">BytesReceived</seealso>
        public void ResetByteCounters()
        {
            m_BytesSent = m_BytesReceived = 0;
        }   

        void Gurux.Common.IGXMedia.Copy(object target)
        {
            GXSerial Target = (GXSerial)target;
            BaudRate = Target.BaudRate;
            StopBits = Target.StopBits;
            Parity = Target.Parity;
            DataBits = Target.DataBits;
        }

        /// <inheritdoc cref="IGXMedia.Eop"/>
        public object Eop
        {
            get
            {
                return m_Eop;
            }
            set
            {                 
                bool change = m_Eop != value;
                m_Eop = value;
                if (change)
                {
                    NotifyPropertyChanged("Eop");
                }
            }
        }

        /// <summary>
        /// Media settings as a XML string.
        /// </summary>
        public string Settings
        {
            get
            {
                string tmp = "";
                if (!string.IsNullOrEmpty(PortName))
                {
                    tmp += "<Port>" + PortName + "</Port>";
                }
                if (BaudRate != 9600)
                {
                    tmp += "<Bps>" + BaudRate + "</Bps>";
                }
                if (this.StopBits != System.IO.Ports.StopBits.None)
                {
                    tmp += "<StopBits>" + (int)StopBits + "</StopBits>";
                }
                if (this.Parity != System.IO.Ports.Parity.None)
                {
                    tmp += "<Parity>" + (int)Parity + "</Parity>";
                }
                if (this.DataBits != 8)
                {
                    tmp += "<ByteSize>" + DataBits + "</ByteSize>";
                }
                return tmp;
            }
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    XmlReaderSettings settings = new XmlReaderSettings();
                    settings.ConformanceLevel = ConformanceLevel.Fragment;
                    string str;
                    int result;
                    using (XmlReader xmlReader = XmlReader.Create(new System.IO.StringReader(value), settings))
                    {
                        while (xmlReader.Read())
                        {
                            if (xmlReader.IsStartElement())
                            {
                                switch (xmlReader.Name)
                                {
                                    case "Port":
                                        PortName = xmlReader.ReadString();
                                        break;
                                    case "Bps":
                                        BaudRate = Convert.ToInt32(xmlReader.ReadString());
                                        break;
                                    case "StopBits":
                                        str = xmlReader.ReadString();
                                        if (int.TryParse(str, out result))
                                        {
                                            StopBits = (StopBits)result;
                                        }
                                        else
                                        {
                                            StopBits = (StopBits)Enum.Parse(typeof(StopBits), str);
                                        }

                                        break;
                                    case "Parity":
                                        str = xmlReader.ReadString();
                                        if (int.TryParse(str, out result))
                                        {
                                            Parity = (Parity)result;
                                        }
                                        else
                                        {
                                            Parity = (Parity)Enum.Parse(typeof(System.IO.Ports.Parity), str);
                                        }
                                        break;
                                    case "ByteSize":
                                        DataBits = Convert.ToInt32(xmlReader.ReadString());
                                        break;
                                }
                            }
                        }
                    }
                }
            }
        }       

        string Gurux.Common.IGXMedia.MediaType
        {
            get
            {
                return "Serial";
            }
        }

        bool Gurux.Common.IGXMedia.Enabled
        {
            get
            {
                return GXSerial.GetPortNames().Length != 0;
            }
        }

        string Gurux.Common.IGXMedia.Name
        {
            get 
            {
                return this.PortName;
            }
        }

        /// <summary>
        /// Shows the serial port Properties dialog.
        /// </summary>
        /// <param name="parent">Owner window of the Properties dialog.</param>
        /// <returns>True, if the user has accepted the changes.</returns>
        /// <seealso cref="PortName">PortName</seealso>
        /// <seealso cref="BaudRate">BaudRate</seealso>
        /// <seealso cref="DataBits">DataBits</seealso>
        /// <seealso href="PropertiesDialog.html">Properties Dialog</seealso>
        public bool Properties(Form parent)
        {            
            return new Gurux.Shared.PropertiesForm(this.PropertiesForm, Gurux.Serial.Resources.SettingsTxt, IsOpen).ShowDialog(parent) == DialogResult.OK;
        }

		/// <summary>
		/// Sends data asynchronously. <br/>
		/// No reply from the receiver, whether or not the operation was successful, is expected.
		/// </summary>
        public void Send(object data)
        {
            ((Gurux.Common.IGXMedia)this).Send(data, null);
        }

		/// <summary>
		/// Returns a new instance of the Settings form.
		/// </summary>
        public System.Windows.Forms.Form PropertiesForm
        {
            get
            {
                return new Settings(this);
            }
        }

        /// <inheritdoc cref="IGXMedia.Receive"/>
        /// <example>
        /// <code>
        /// 'Send long and wait until OK reply is received or 5 seconds.
        /// 'Data is returned as string.
        /// lock (GXSerial11.Synchronous)
        /// {        
        ///     dim params as new Receiveparameters
        ///     params.Eop = "OK"
        ///     params.WaitTime = 10000
        ///     params.Type = typeof(string)
        ///     GXSerial11.Send((byte) 0x13)
        ///     GXSerial11.Receive(params)
        ///     ' While all data is not received read more data.
        ///     ' This is done because reply data might include "OK" but all data is not read yet.
        ///     'While PacketIsNotCompleted
        ///         GXSerial11.Receive(params)
        ///     'Wend
        /// }
        /// 
        /// 'Send data and wait until 4 bytes is received.
        /// 'Received data is received as long (Int32)
        /// lock (GXSerial11.Synchronous)
        /// {           
        ///     dim params as new Receiveparameters
        ///     params.Count = 4
        ///     params.WaitTime = 10000
        ///     params.Type = typeof(string)
        ///     GXSerial11.Send((byte) 0x13)
        ///     GXSerial11.Receive(params)
        /// }
        /// </code>
        /// </example>
        public bool Receive<T>(Gurux.Common.ReceiveParameters<T> args)
        {
            return m_syncBase.Receive(args);
        }

        void Gurux.Common.IGXMedia.Send(object data, string receiver)
        {			
            byte[] value = GXCommon.GetAsByteArray(data);			
			lock(m_baseLock)
			{
                if (m_Trace == TraceLevel.Verbose && m_OnTrace != null)
                {
                    m_OnTrace(this, new TraceEventArgs(TraceTypes.Sent, value));
                }
            	m_BytesSent += (uint) value.Length;
                //Reset last position if Eop is used.
                lock (m_syncBase.m_ReceivedSync)
                {
                    m_syncBase.m_LastPosition = 0;
                }
            	m_base.Write(value, 0, value.Length);
			}
        }

        void Gurux.Common.IGXMedia.Validate()
        {
            
        }

        int Gurux.Common.IGXMedia.ConfigurableSettings
        {
            get;
            set;
        }

        #endregion

        #region IDisposable Members

		/// <summary>
		/// Closes the connection.
		/// </summary>
        public void Dispose()
        {
            if (this.IsOpen)
            {
                Close();
            }            
        }

        #endregion
    }
}
