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
using Gurux.Common;
using Gurux.Shared;
using System.IO.Ports;
using System.Xml;
using System.Diagnostics;
using System.Threading;
using Android.Hardware.Usb;
using Android.Content;
using Android.App;
using System.Linq;
using Gurux.Serial.Enums;
using System.Globalization;
using System.Text;
using System.IO;
using System.Threading.Tasks;

[assembly: UsesFeature("android.hardware.usb.host")]

namespace Gurux.Serial
{
    /// <summary>
    /// A media component that enables communication of serial port for Android devices.
    /// See help in http://www.gurux.org/Gurux.Serial
    /// </summary>
    public class GXSerial : IGXMedia2, INotifyPropertyChanged, IDisposable
    {

        Task _receiver = null;
        /// <summary>
        /// If receiver buffer is empty how long is waited for new data.
        /// </summary>
        private const int WaitTime = 200;

        /// <summary>
        /// Is serial port closing.
        /// </summary>
        public ManualResetEvent _closing = new ManualResetEvent(false);

        private List<GXPort> _ports = new List<GXPort>();
        GXPort _Port;
        private readonly Context _contect;
        /// <summary>
        /// Receive notifications if serial port is removed or added.
        /// </summary>
        private GXUsbReciever _Receiver;
        /// <summary>
        /// Used chipset.
        /// </summary>
        internal GXChipset _Chipset;
        private PortAddEventHandler _OnPortAdd;
        private PortRemoveEventHandler _OnPortRemove;

        // Values are saved if port is not open and user try to set them.


        /// <summary>
        /// Serial port baud rate.
        /// </summary>
        private int _BaudRate = 9600;
        /// <summary>
        ///Used data bits. 
        /// </summary>
        private int _DataBits = 8;
        /// <summary>
        ///Stop bits. 
        /// </summary>
        private StopBits _StopBits = StopBits.One;
        /// <summary>
        /// Used parity. 
        /// </summary>
        private Parity _Parity = Parity.None;
        /// <summary>
        /// Write timeout.
        /// </summary>
        private int _WriteTimeout = 5000;
        /// <summary>
        /// Read timeout.
        /// </summary>
        private int _ReadTimeout = 5000;
        /// <summary>
        /// Serial port connection.
        /// </summary>
        UsbDeviceConnection _Connection;
        /// <summary>
        /// connection interface.
        /// </summary>
        UsbInterface _UsbIf;

        UsbEndpoint _Out;

        private object m_sync = new object();
        int LastEopPos = 0;
        TraceLevel m_Trace;
        object m_Eop;
        private readonly GXSynchronousMediaBase _syncBase;
        UInt64 _bytesSent, m_BytesReceived;
        readonly object m_Synchronous = new object();

        /// <summary>
        /// Get baud rates supported by given serial port.
        /// </summary>
        public static int[] GetAvailableBaudRates(string portName)
        {
            List<int> items = new List<int>();
            //Add default baud rates.
            items.Add(300);
            items.Add(600);
            items.Add(1800);
            items.Add(2400);
            items.Add(4800);
            items.Add(9600);
            items.Add(19200);
            items.Add(38400);
            items.Add(56000);
            items.Add(57600);
            items.Add(115200);
            items.Add(128000);
            return items.ToArray();
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public GXSerial(Context contect)
        {
            _contect = contect;
            _Receiver = new GXUsbReciever(this);
            IntentFilter filter = new IntentFilter("Gurux.Serial");
            filter.AddAction(UsbManager.ActionUsbAccessoryDetached);
            filter.AddAction(UsbManager.ActionUsbDeviceAttached);
            filter.AddAction(UsbManager.ActionUsbDeviceDetached);
            contect.RegisterReceiver(_Receiver, new IntentFilter(filter));
            ConfigurableSettings = AvailableMediaSettings.All;
            _syncBase = new GXSynchronousMediaBase(1024);
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="activity">Activity.</param>
        public GXSerial(Activity activity) : this((Context)activity)
        {
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="context">Context.</param>
        /// <param name="port">Serial port.</param>
        /// <param name="baudRate">Baud rate.</param>
        /// <param name="dataBits">Data bits.</param>
        /// <param name="parity">Parity.</param>
        /// <param name="stopBits">Stop bits.</param>
        public GXSerial(Context context, string port, int baudRate,
                        int dataBits, Parity parity,
                        StopBits stopBits) : this(context)
        {
            foreach (GXPort it in GetPorts())
            {
                if (string.Compare(port, it.Port, true) == 0)
                {
                    Port = it;
                    break;
                }
            }
            BaudRate = baudRate;
            DataBits = dataBits;
            Parity = parity;
            StopBits = stopBits;
        }


        /**
         * Constructor.
         *
         * @param port     Serial port.
         * @param baudRate Baud rate.
         * @param dataBits Data bits.
         * @param parity   Parity.
         * @param stopBits Stop bits.
         */
        public GXSerial(Activity activity, string port, int baudRate,
                        int dataBits, Parity parity,
                        StopBits stopBits) : this(activity)
        {
            foreach (GXPort it in GetPorts())
            {
                if (string.Compare(port, it.Port, true) == 0)
                {
                    Port = it;
                    break;
                }
            }
            BaudRate = baudRate;
            DataBits = dataBits;
            Parity = parity;
            this.StopBits = stopBits;
        }

        /// <summary>
        /// New serial port has added for the device.
        /// </summary>
        public event PortAddEventHandler OnPortAdd
        {
            add
            {
                _OnPortAdd += value;
            }
            remove
            {
                _OnPortAdd -= value;
            }
        }

        /// <summary>
        /// Serial port has removed for the device.
        /// </summary>
        public event PortRemoveEventHandler OnPortRemove
        {
            add
            {
                _OnPortRemove += value;
            }
            remove
            {
                _OnPortRemove -= value;
            }
        }

        /// <summary>
        /// Find used chipset.
        /// </summary>
        /// <param name="manufacturer">Manufacturer name.</param>
        /// <param name="vendor">Vendor ID.</param>
        /// <param name="productId">Product ID.</param>
        /// <returns>Chipset settings.</returns>
        private static GXChipset GetChipSet(string manufacturer, int vendor, int productId)
        {
            if (GXCP21xx.IsUsing(manufacturer, vendor, productId))
            {
                return new GXCP21xx();
            }
            else if (GXProfilic.IsUsing(manufacturer, vendor, productId))
            {
                return new GXProfilic();
            }
            else if (GXFtdi.IsUsing(manufacturer, vendor, productId))
            {
                return new GXFtdi();
            }
            else if (GXFtdi.IsUsing(manufacturer, vendor, productId))
            {
                return new GXFtdi();
            }
            else if (GXCh34x.IsUsing(manufacturer, vendor, productId))
            {
                return new GXCh34x();
            }
            return null;
        }

        /// <summary>
        /// Get chipset settings.
        /// </summary>
        /// <param name="chipset"></param>
        /// <returns></returns>
        private static GXChipset GetChipSet(Chipset chipset)
        {
            GXChipset value;
            switch (chipset)
            {
                case Chipset.Profilic:
                    value = new GXProfilic();
                    break;
                case Chipset.Cp21xx:
                    value = new GXCP21xx();
                    break;
                case Chipset.Ftdi:
                    value = new GXFtdi();
                    break;
                case Chipset.Ch34x:
                    value = new GXCh34x();
                    break;
                default:
                    throw new System.ArgumentOutOfRangeException("Invalid chipset.");
            }
            return value;
        }

        /// <summary>
        /// Remove USB serial port.
        /// </summary>
        /// <param name="device">Removed USB device.</param>
        internal void RemovePort(UsbDevice device)
        {
            lock (this)
            {
                if (_Port != null &&
                    _Port.Port == device.DeviceName &&
                    _Port.VendorId == device.VendorId &&
                    _Port.ProductId == device.ProductId)
                {
                    //Close open port.
                    Close();
                }
                int pos = 0;
                foreach (GXPort port in _ports)
                {
                    if (port.Port == device.DeviceName)
                    {
                        lock (_ports)
                        {
                            _ports.Remove(port);
                        }
                        _OnPortRemove?.Invoke(port, pos);
                        break;
                    }
                    ++pos;
                }
            }
        }

        internal void AddPort(UsbManager m, UsbDevice device, bool notify)
        {
            //Check if port is already added.
            foreach (GXPort it in _ports)
            {
                if (it.Port == device.DeviceName &&
                    it.VendorId == device.VendorId &&
                    it.ProductId == device.ProductId)
                {
                    return;
                }
            }
            UsbManager manager = m;
            if (manager == null)
            {
                manager = (UsbManager)_contect.GetSystemService(Context.UsbService);
            }
            byte[] buffer = new byte[255];
            string name = "Gurux.Serial";
            PendingIntent permissionIntent = PendingIntent.GetBroadcast(_contect, 0, new Intent(name), PendingIntentFlags.Immutable | PendingIntentFlags.UpdateCurrent);
            UsbEndpoint inUsbEndpoint = null, outUsbEndpoint = null;
            if (!manager.HasPermission(device))
            {
                //If there aren't permissions for the device.
                manager.RequestPermission(device, permissionIntent);
                if (!manager.HasPermission(device))
                {
                    //If there aren't permissions for the device.
                    return;
                }
            }
            for (int i = 0; i != device.InterfaceCount; ++i)
            {
                UsbInterface usbIf = device.GetInterface(i);
                for (int pos = 0; pos != usbIf.EndpointCount; ++pos)
                {
                    UsbAddressing direction = usbIf.GetEndpoint(pos).Direction;
                    if (usbIf.GetEndpoint(pos).Type == UsbAddressing.XferBulk)
                    {
                        if (direction == UsbAddressing.In)
                        {
                            inUsbEndpoint = usbIf.GetEndpoint(pos);
                        }
                        else if (direction == UsbAddressing.Out)
                        {
                            outUsbEndpoint = usbIf.GetEndpoint(pos);
                        }
                        if (outUsbEndpoint != null && inUsbEndpoint != null)
                        {
                            break;
                        }
                    }
                }
                if (outUsbEndpoint != null && inUsbEndpoint != null)
                {
                    GXPort port = new GXPort();
                    port.Port = device.DeviceName;
                    port.VendorId = device.VendorId;
                    port.ProductId = device.ProductId;
                    KeyValuePair<string, string>? info = Find(_contect, device.VendorId, device.ProductId);
                    if (info != null)
                    {
                        port.Vendor = info.Value.Key;
                        port.Product = info.Value.Value;
                    }
                    UsbDeviceConnection connection = manager.OpenDevice(device);
                    try
                    {
                        port.Serial = connection.Serial;
                        byte[] rawDescriptors = connection.GetRawDescriptors();
                        port.RawDescriptors = rawDescriptors;
                        string man = GetManufacturer(connection, rawDescriptors, buffer);
                        string prod = GetProduct(connection, rawDescriptors, buffer);
                        port.Manufacturer = (man + ": " + prod);
                        GXChipset chipset = GetChipSet(man, device.VendorId, device.ProductId);
                        if (chipset != null)
                        {
                            port.Chipset = chipset.Chipset;
                        }
                    }
                    finally
                    {
                        connection.Close();
                    }
                    lock (_ports)
                    {
                        _ports.Add(port);
                    }
                    if (notify)
                    {
                        _OnPortAdd?.Invoke(port);
                    }
                }
            }
        }

        internal void NotifyError(System.Exception ex)
        {
            if (m_OnError != null)
            {
                m_OnError(this, ex);
            }
            if (m_Trace >= TraceLevel.Error && m_OnTrace != null)
            {
                m_OnTrace(this, new TraceEventArgs(TraceTypes.Error, ex, null));
            }
        }

        void NotifyMediaStateChange(MediaState state)
        {
            if (m_Trace >= TraceLevel.Info && m_OnTrace != null)
            {
                m_OnTrace(this, new TraceEventArgs(TraceTypes.Info, state, null));
            }
            if (m_OnMediaStateChange != null)
            {
                m_OnMediaStateChange(this, new MediaStateEventArgs(state));
            }
        }

        private static int MANUFACTURER_INDEX = 14;
        private static int PRODUCT_INDEX = 15;
        private static int STD_USB_REQUEST_GET_DESCRIPTOR = 0x06;
        private static int LIBUSB_DT_STRING = 0x03;

        private static string GetManufacturer(
            UsbDeviceConnection connection,
            byte[] rawDescriptors,
            byte[] buff)
        {
            int lengthManufacturer = connection.ControlTransfer(
                UsbAddressing.In,
                    STD_USB_REQUEST_GET_DESCRIPTOR,
                    (LIBUSB_DT_STRING << 8) | rawDescriptors[MANUFACTURER_INDEX],
                    0,
                    buff,
                    0xFF,
                    0);
            if (lengthManufacturer > 0)
            {
                return Encoding.Unicode.GetString(buff, 2, lengthManufacturer - 2);
            }
            return null;
        }

        private static string GetProduct(
            UsbDeviceConnection connection,
            byte[] rawDescriptors,
            byte[] buff)
        {
            int lengthProduct = connection.ControlTransfer(
                    UsbAddressing.In,
                    STD_USB_REQUEST_GET_DESCRIPTOR,
                    (LIBUSB_DT_STRING << 8) | rawDescriptors[PRODUCT_INDEX],
                    0,
                    buff,
                    0xFF,
                    0);
            if (lengthProduct > 0)
            {
                return Encoding.Unicode.GetString(buff, 2, lengthProduct - 2);
            }
            return null;
        }

        /**
         * Find vendor and product name.
         *
         * @param context
         * @param vendor  Vendor ID.
         * @param product Product ID.
         * @return Vendor and product entry or null.
         * @
         */
        private static KeyValuePair<string, string>? Find(
            Context context,
            int vendor,
            int product)
        {
            string vendorName = null, productName = null;
            string line;
            using (StreamReader r = new StreamReader(context.Assets.Open("usbs.txt")))
            {
                while ((line = r.ReadLine()) != null)
                {
                    if (line.StartsWith("C 00"))
                    {
                        // If all manufacturers are read.
                        break;
                    }
                    if (!string.IsNullOrEmpty(line) && !line.StartsWith("#"))
                    {
                        if (vendorName == null)
                        {
                            // Find vendor.
                            if (!line.StartsWith("\t"))
                            {
                                int v = int.Parse(line.Substring(0, 4), NumberStyles.HexNumber);
                                if (v == vendor)
                                {
                                    vendorName = line.Substring(5).Trim();
                                }
                            }
                        }
                        else
                        {
                            // Find product.
                            if (!line.StartsWith("\t"))
                            {
                                break;
                            }
                            int v = int.Parse(line.Substring(1, 5), NumberStyles.HexNumber);
                            if (v == product)
                            {
                                productName = line.Substring(6).Trim();
                                break;
                            }
                        }
                    }
                }
            }
            if (vendorName == null)
            {
                return null;
            }
            return new KeyValuePair<string, string>(vendorName, productName);
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
                m_Trace = _syncBase.Trace = value;
            }
        }

        private void HandleReceivedData(int index, byte[] buffer, int totalCount)
        {
            lock (_syncBase.receivedSync)
            {
                if (totalCount != 0 && Eop != null) //Search Eop if given.
                {
                    byte[] eop = null;
                    if (Eop is Array)
                    {
                        foreach (object it in (Array)Eop)
                        {
                            eop = Gurux.Common.GXCommon.GetAsByteArray(it);
                            totalCount = Gurux.Common.GXCommon.IndexOf(_syncBase.m_Received, eop, index, _syncBase.receivedSize);
                            if (totalCount != -1)
                            {
                                break;
                            }
                        }
                    }
                    else
                    {
                        eop = Common.GXCommon.GetAsByteArray(Eop);
                        totalCount = Common.GXCommon.IndexOf(_syncBase.m_Received, eop, index, _syncBase.receivedSize);
                    }
                    if (totalCount != -1)
                    {
                        totalCount += eop.Length;
                    }
                }
            }
            if (totalCount != -1 && m_Trace == TraceLevel.Verbose && m_OnTrace != null)
            {
                int pos;
                //If sync data is not read.
                if (index + totalCount >= LastEopPos)
                {
                    pos = LastEopPos;
                }
                else //If sync data is read.
                {
                    pos = index;
                }
                int count;
                if (totalCount > LastEopPos)
                {
                    count = totalCount - LastEopPos;
                }
                else
                {
                    count = totalCount;
                }
                TraceEventArgs arg = new TraceEventArgs(TraceTypes.Received,
                                    buffer, 0, count, null);
                LastEopPos = index + totalCount;
                if (LastEopPos != 0)
                {
                    --LastEopPos;
                }
                m_OnTrace(this, arg);
            }
            if (this.IsSynchronous)
            {
                if (totalCount != -1)
                {
                    _syncBase.AppendData(buffer, index, totalCount);
                    _syncBase.receivedEvent.Set();
                }
            }
            else if (this.m_OnReceived != null)
            {
                if (totalCount != -1)
                {
                    byte[] buff = new byte[totalCount];
                    Array.Copy(buffer, buff, totalCount);
                    m_OnReceived(this, new ReceiveEventArgs(buff, Port.Port));
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
                return _BaudRate;
            }
            set
            {
                bool change = BaudRate != value;
                if (change)
                {
                    _BaudRate = value;
                    if (change)
                    {
                        NotifyPropertyChanged("BaudRate");
                    }
                }
            }
        }

        /// <summary>
        /// Used serial port chipset.
        /// </summary>
        public Chipset Chipset
        {
            get
            {
                if (_Chipset == null)
                {
                    return Chipset.None;
                }
                return _Chipset.Chipset;
            }
            set
            {
                bool change = _Chipset == null || _Chipset.Chipset != value;
                if (change)
                {
                    _Chipset = GetChipSet(value);
                    NotifyPropertyChanged("Chipset");
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
                return _DataBits;
            }
            set
            {
                bool change = DataBits != value;
                _DataBits = value;
                if (change)
                {
                    NotifyPropertyChanged("DataBits");
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
                if (IsOpen)
                {
                    return _Chipset.GetDtrEnable(_Connection);
                }
                return false;
            }
            set
            {
                if (IsOpen)
                {
                    bool change = DtrEnable != value;
                    _Chipset.SetDtrEnable(_Connection, value);
                    if (change)
                    {
                        NotifyPropertyChanged("DtrEnable");
                    }
                }
            }
        }

        /// <summary>
        /// Gets a value indicating the open or closed status of the SerialPort object.
        /// </summary>
        [Browsable(false)]
        public bool IsOpen
        {
            get
            {
                return _Connection != null;
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
                return _Parity;
            }
            set
            {
                bool change = Parity != value;
                _Parity = value;
                if (change)
                {
                    NotifyPropertyChanged("Parity");
                }
            }
        }

        /// <summary>
        /// Gets or sets the port for communications, including but not limited to all available COM ports.
        /// </summary>
        [Browsable(true)]
        public GXPort Port
        {
            get
            {
                return _Port;
            }
            set
            {
                bool change;
                change = value != _Port;
                _Port = value;
                if (change)
                {
                    NotifyPropertyChanged("Port");
                    if (value != null && value.VendorId != 0 && value.ProductId != 0)
                    {
                        _Chipset = GetChipSet(null, value.VendorId, value.ProductId);
                    }
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
                return _ReadTimeout;
            }
            set
            {
                bool change = ReadTimeout != value;
                _ReadTimeout = value;
                if (change)
                {
                    NotifyPropertyChanged("ReadTimeout");
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
                if (IsOpen)
                {
                    return _Chipset.GetRtsEnable(_Connection);
                }
                return false;
            }
            set
            {
                bool change = RtsEnable != value;
                if (IsOpen)
                {
                    _Chipset.SetRtsEnable(_Connection, value);
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
        [DefaultValue(StopBits.One)]
        [Browsable(true)]
        public StopBits StopBits
        {
            get
            {
                return _StopBits;
            }
            set
            {
                bool change = _StopBits != value;
                _StopBits = value;
                if (change)
                {
                    NotifyPropertyChanged("StopBits");
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
                return _WriteTimeout;
            }
            set
            {
                bool change = _WriteTimeout != value;
                _WriteTimeout = value;
                if (change)
                {
                    NotifyPropertyChanged("WriteTimeout");
                }
            }
        }

        /// <summary>
        /// Closes the port connection, sets the SerialPort.IsOpen property to false, and disposes of the internal System.IO.Stream object.
        /// </summary>
        public void Close()
        {
            if (_receiver != null && !_receiver.IsCompleted)
            {
                _closing.Set();
                _receiver.Wait();
                _receiver = null;
            }

            if (_UsbIf != null)
            {
                _Connection.ReleaseInterface(_UsbIf);
                _UsbIf = null;
            }

            if (_Connection != null)
            {
                try
                {
                    NotifyMediaStateChange(MediaState.Closing);
                }
                catch (System.Exception ex)
                {
                    NotifyError(ex);
                    throw ex;
                }
                finally
                {
                    _Out = null;
                    _Connection.Close();
                    _Connection = null;
                    NotifyMediaStateChange(MediaState.Closed);
                    _bytesSent = 0;
                }
            }
        }

        /// <summary>
        /// Gets an array of serial port names for the current computer.
        /// </summary>
        /// <returns></returns>
        public GXPort[] GetPorts()
        {
            UsbManager manager = (UsbManager)_contect.GetSystemService(Context.UsbService);
            var list = manager.DeviceList;
            if (list != null && list.Any())
            {
                foreach (var it in list)
                {
                    AddPort(manager, it.Value, false);
                }
            }
            return _ports.ToArray();
        }

        /// <summary>
        /// Opens a new serial port connection.
        /// </summary>
        public void Open()
        {
            Close();
            try
            {
                _closing.Reset();
                if (_Port == null)
                {
                    throw new System.Exception("Serial port is not selected.");
                }
                if (!_contect.PackageManager.HasSystemFeature("android.hardware.usb.host"))
                {
                    throw new System.Exception("Usb feature is not supported.");
                }
                NotifyMediaStateChange(MediaState.Opening);
                if (Trace >= TraceLevel.Info)
                {
                    string eopString = "None";
                    if (m_Eop is byte[] b)
                    {
                        eopString = Common.GXCommon.ToHex(b);
                    }
                    else if (m_Eop != null)
                    {
                        eopString = m_Eop.ToString();
                    }
                    m_OnTrace(this, new TraceEventArgs(TraceTypes.Info,
                            "Settings: Port: " + this.Port +
                            " Baud Rate: " + BaudRate
                                    + " Data Bits: " + DataBits +
                                    " Parity: " + Parity +
                                    " Stop Bits: " + StopBits +
                                    " Eop:" + eopString, null));
                }
                UsbEndpoint inUsbEndpoint = null;
                UsbManager manager = (UsbManager)_contect.GetSystemService(Context.UsbService);
                var devices = manager.DeviceList;
                int vendor = 0, productId = 0;
                foreach (var it in devices)
                {
                    if (it.Key == _Port.Port)
                    {
                        _Connection = manager.OpenDevice(it.Value);
                        UsbInterface usbIf = it.Value.GetInterface(0);
                        for (int pos = 0; pos != usbIf.EndpointCount; ++pos)
                        {
                            UsbAddressing direction = usbIf.GetEndpoint(pos).Direction;
                            if (usbIf.GetEndpoint(pos).Type == UsbAddressing.XferBulk)
                            {
                                if (direction == UsbAddressing.In)
                                {
                                    inUsbEndpoint = usbIf.GetEndpoint(pos);
                                }
                                else if (direction == UsbAddressing.Out)
                                {
                                    _Out = usbIf.GetEndpoint(pos);
                                }
                                if (_Out != null && inUsbEndpoint != null)
                                {
                                    vendor = it.Value.VendorId;
                                    productId = it.Value.ProductId;
                                    //Claims exclusive access to a Usb interface.
                                    //This must done to send or receive data.
                                    _Connection.ClaimInterface(usbIf, true);
                                    break;
                                }
                            }
                        }
                        break;
                    }
                }
                if (_Out == null || inUsbEndpoint == null)
                {
                    throw new System.Exception("Invalid serial port endpoint.");
                }
                if (_Chipset == null)
                {
                    throw new System.Exception("Invalid vendor id: " + vendor + " product Id: " + productId);
                }
                byte[] rawDescriptors = _Connection.GetRawDescriptors();
                if (!_Chipset.Open(this, _Connection, rawDescriptors))
                {
                    throw new System.Exception("Failed to open serial port.");
                }
                _receiver = Task.Run(() =>
                {
                    byte[] buff = new byte[inUsbEndpoint.MaxPacketSize];
                    while (!_closing.WaitOne(0))
                    {
                        try
                        {
                            int len = _Connection.BulkTransfer(inUsbEndpoint, buff, buff.Length, WaitTime);
                            //Len is -1 if timeout for some chipsets.
                            //http://b.android.com/28023
                            // If mConnection is closed.
                            if (len == 0 && _closing.WaitOne(0))
                            {
                                break;
                            }
                            if (len > 0 && _Chipset != null)
                            {
                                len = _Chipset.RemoveStatus(buff, len, buff.Length);
                            }
                            if (len > 0)
                            {
                                //Read all data before notified.
                                if (ReceiveDelay > 0)
                                {
                                    DateTime start = DateTime.Now;
                                    int elapsedTime = 0;
                                    //Bytes left.
                                    MemoryStream tmp = new MemoryStream();
                                    tmp.Write(buff, 0, len);
                                    while ((len = _Connection.BulkTransfer(inUsbEndpoint, buff, 0, buff.Length, (int)(ReceiveDelay - elapsedTime))) > 0)
                                    {
                                        if (len > 0 && _Chipset != null)
                                        {
                                            len = _Chipset.RemoveStatus(buff, len, buff.Length);
                                        }
                                        if (len > 0)
                                        {
                                            tmp.Write(buff, buff.Length, len);
                                        }
                                        elapsedTime = (int)(DateTime.Now - start).TotalMilliseconds;
                                        if (ReceiveDelay - elapsedTime < 1)
                                        {
                                            break;
                                        }
                                    }
                                    HandleReceivedData(0, tmp.ToArray(), (int) tmp.Length);
                                }
                                else
                                {
                                    HandleReceivedData(0, buff, len);
                                }
                            }
                        }
                        catch (System.Exception ex)
                        {
                            if (!_closing.WaitOne(0))
                            {
                                NotifyError(ex);
                            }
                        }
                    }
                });
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
        internal Common.ErrorEventHandler m_OnError;
        ReceivedEventHandler m_OnReceived;

        /// <inheritdoc />
        public event ClientConnectedEventHandler OnClientConnected;
        /// <inheritdoc />
        public event ClientDisconnectedEventHandler OnClientDisconnected;
        #endregion //Events

        /// <inheritdoc />
        public AvailableMediaSettings ConfigurableSettings
        {
            get
            {
                return (AvailableMediaSettings)((IGXMedia)this).ConfigurableSettings;
            }
            set
            {
                ((IGXMedia)this).ConfigurableSettings = (int)value;
            }
        }

        /// <inheritdoc cref="IGXMedia.Tag"/>
        public object Tag
        {
            get;
            set;
        }

        /// <inheritdoc cref="IGXMedia.MediaContainer"/>
        IGXMediaContainer IGXMedia.MediaContainer
        {
            get;
            set;
        }

        /// <inheritdoc cref="IGXMedia.SyncRoot"/>
        [Browsable(false), ReadOnly(true)]
        public object SyncRoot
        {
            get
            {
                //In some special cases when binary serialization is used this might be null
                //after deserialize. Just set it.
                if (m_sync == null)
                {
                    m_sync = new object();
                }
                return m_sync;
            }
        }

        /// <inheritdoc />
        public object Synchronous
        {
            get
            {
                return m_Synchronous;
            }
        }

        /// <inheritdoc />
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
            lock (_syncBase.receivedSync)
            {
                _syncBase.receivedSize = 0;
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
                return _bytesSent;
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
            _bytesSent = m_BytesReceived = 0;
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
                if (Port != null && !string.IsNullOrEmpty(Port.Port))
                {
                    tmp += "<Port>" + Port.Port + "</Port>";
                }
                if (BaudRate != 9600)
                {
                    tmp += "<Bps>" + BaudRate + "</Bps>";
                }
                if (StopBits != StopBits.One)
                {
                    tmp += "<StopBits>" + (int)StopBits + "</StopBits>";
                }
                if (Parity != Parity.None)
                {
                    tmp += "<Parity>" + (int)Parity + "</Parity>";
                }
                if (DataBits != 8)
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
                    //Set default settings.
                    BaudRate = 9600;
                    DataBits = 8;
                    Parity = Parity.None;
                    bool setStopBits = false;
                    using (XmlReader xmlReader = XmlReader.Create(new System.IO.StringReader(value), settings))
                    {
                        while (xmlReader.Read())
                        {
                            if (xmlReader.IsStartElement())
                            {
                                switch (xmlReader.Name)
                                {
                                    case "Port":
                                        {
                                            bool found = false;
                                            string name = xmlReader.ReadString();
                                            foreach (GXPort it in _ports)
                                            {
                                                if (name == it.Port)
                                                {
                                                    Port = it;
                                                    found = true;
                                                    break;
                                                }
                                            }
                                            if (!found)
                                            {
                                                Port = null;
                                            }
                                        }
                                        break;
                                    case "Bps":
                                        BaudRate = Convert.ToInt32(xmlReader.ReadString());
                                        break;
                                    case "StopBits":
                                        setStopBits = true;
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
                                            Parity = (Parity)Enum.Parse(typeof(Parity), str);
                                        }
                                        break;
                                    case "ByteSize":
                                        DataBits = Convert.ToInt32(xmlReader.ReadString());
                                        break;
                                }
                            }
                        }
                    }
                    if (!setStopBits)
                    {
                        StopBits = StopBits.One;
                    }
                }
            }
        }

        /// <summary>
        /// Current Serial port settings as a string.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            if (_Port != null)
            {
                sb.Append(_Port.Port);
                sb.Append(' ');
            }
            sb.Append(BaudRate);
            sb.Append(' ');
            sb.Append(DataBits);
            sb.Append(Parity);
            sb.Append((int)StopBits);
            return sb.ToString();
        }

        string IGXMedia.MediaType
        {
            get
            {
                return "Serial";
            }
        }

        bool IGXMedia.Enabled
        {
            get
            {
                return GetPorts().Any();
            }
        }

        string IGXMedia.Name
        {
            get
            {
                return _Port?.Port;
            }
        }

        /// <summary>
        /// Shows the serial port Properties dialog.
        /// </summary>
        /// <param name="activity">Owner window of the Properties dialog.</param>
        /// <returns>True, if the user has accepted the changes.</returns>
        /// <seealso cref="Port">Port</seealso>
        /// <seealso cref="BaudRate">BaudRate</seealso>
        /// <seealso cref="DataBits">DataBits</seealso>
        public bool Properties(Activity activity)
        {
            GXPropertiesBase.SetSerial(this);
            Intent intent = new Intent(activity, typeof(GXProperties));
            activity.StartActivity(intent);
            return true;
        }

        /// <summary>
        /// Returns a new instance of the Settings form.
        /// </summary>
        public AndroidX.Fragment.App.Fragment PropertiesForm
        {
            get
            {
                GXPropertiesBase.SetSerial(this);
                return new GXPropertiesFragment();
            }
        }

        /// <summary>
        /// Sends data asynchronously. <br/>
        /// No reply from the receiver, whether or not the operation was successful, is expected.
        /// </summary>
        public void Send(object data)
        {
            ((Gurux.Common.IGXMedia)this).Send(data, null);
        }

        /// <inheritdoc cref="IGXMedia.Receive"/>
        public bool Receive<T>(Gurux.Common.ReceiveParameters<T> args)
        {
            if (!IsOpen)
            {
                throw new InvalidOperationException("Media is closed.");
            }
            return _syncBase.Receive(args);
        }

        /// <inheritdoc />
        void IGXMedia.Send(object data, string receiver)
        {
            byte[] buff = Gurux.Common.GXCommon.GetAsByteArray(data);
            if (buff == null)
            {
                throw new System.Exception("Data send failed. Invalid data.");
            }
            if (_Out == null)
            {
                throw new System.Exception("Serial port is not open.");
            }
            if (Trace == TraceLevel.Verbose)
            {
                m_OnTrace(this, new TraceEventArgs(TraceTypes.Sent, data, null));
            }
            int ret, pos = 0, dataSize = _Out.MaxPacketSize;
            while (pos != buff.Length)
            {
                if (buff.Length - pos < dataSize)
                {
                    dataSize = buff.Length - pos;
                }
                ret = _Connection.BulkTransfer(_Out, buff, pos, dataSize, _WriteTimeout);
                if (ret != dataSize)
                {
                    throw new System.Exception("Data send failed.");
                }
                pos += ret;
            }
            _bytesSent += (UInt64)buff.Length;
        }

        /// <inheritdoc />
        public void Validate()
        {

        }

        /// <inheritdoc />
        int Gurux.Common.IGXMedia.ConfigurableSettings
        {
            get;
            set;
        }

        /// <inheritdoc />
        uint IGXMedia2.AsyncWaitTime
        {
            get;
            set;
        }

        /// <inheritdoc />
        EventWaitHandle IGXMedia2.AsyncWaitHandle
        {
            get
            {
                return null;
            }
        }

        /// <inheritdoc />
        public uint ReceiveDelay
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
            if (IsOpen)
            {
                Close();
            }
        }

        #endregion
    }
}
