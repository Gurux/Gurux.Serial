// --------------------------------------------------------------------------
// Gurux Ltd
//
// Copyright (c) Gurux Ltd.
//
// This file is part of Gurux Device Framework.
//
// Commit: $Format:%H$
// Date:   $Format:%cI$
//
// This software is available under either of the following licenses:
//
// 1. GNU General Public License, version 2 only (GPL-2.0-only).
//    See the LICENSE file included with this distribution.
//
// 2. A commercial license granted by Gurux Ltd under a separate written
//    agreement. Use under this option requires a valid commercial license
//    and is governed by the terms of that agreement.
//
// If you have not obtained a commercial license from Gurux Ltd,
// use of this software is governed by GPL-2.0-only.
//
// Commercial licensing: https://www.gurux.fi/
//
// SPDX-License-Identifier: GPL-2.0-only OR LicenseRef-Gurux-Commercial
// --------------------------------------------------------------------------
#if ANDROID
using System.IO.Ports;
using Android.Bluetooth;
using Android.Hardware.Usb;
using Java.Util;

namespace Gurux.Serial;
/// <summary>Creates owned stream factories for Android Bluetooth Classic RFCOMM and USB CDC ACM devices.</summary>
/// <remarks>The application must obtain the required USB or Bluetooth runtime permissions before opening the media.</remarks>
public static partial class AndroidSerialTransports
{
    /// <summary>Creates a factory that connects to a paired Bluetooth Classic device using RFCOMM.</summary>
    /// <remarks>The returned factory connects when invoked by <see cref="GXSerial.OpenAsync"/> and requires BLUETOOTH_CONNECT permission on Android 12 or later.</remarks>
    /// <param name="device">The Android device whose serial connection will be opened.</param>
    /// <param name="service">The RFCOMM service UUID, or null to use the standard serial port service.</param>
    /// <param name="insecure">Whether to explicitly request an unauthenticated RFCOMM socket; defaults to an authenticated socket.</param>
    /// <returns>A factory creating an owned Bluetooth RFCOMM stream.</returns>
    public static Func<SerialPortSettings, CancellationToken, ValueTask<Stream>> BluetoothRfcomm(BluetoothDevice device, Guid? service = null, bool insecure = false) => async (_, token) =>
    {
        var uuid = UUID.FromString((service ?? Guid.Parse("00001101-0000-1000-8000-00805f9b34fb")).ToString());
        var socket = (insecure ? device.CreateInsecureRfcommSocketToServiceRecord(uuid) : device.CreateRfcommSocketToServiceRecord(uuid)) ?? throw new IOException(global::Gurux.Serial.Properties.Resources.Get("CannotCreateRFCOMMSocket"));
        try
        {
            using var registration = token.Register(socket.Close);
            await Task.Run(socket.Connect, token).ConfigureAwait(false);
            token.ThrowIfCancellationRequested();
            return new DuplexStream(socket.InputStream!, socket.OutputStream!, () =>
            {
                socket.Close();
                socket.Dispose();
            });
        }
        catch
        {
            socket.Close();
            socket.Dispose();
            throw;
        }
    };
    /// <summary>Creates a factory that opens a single USB CDC ACM port and applies its serial framing and control signals.</summary>
    /// <remarks>Call UsbManager.RequestPermission first and wait for permission. Composite devices with multiple CDC ports,
    /// FTDI, CP210x, CH34x and other vendor protocols require a dedicated adapter. The returned factory rejects hardware and software handshaking.</remarks>
    /// <param name="manager">The Android USB manager providing device access and permission state.</param>
    /// <param name="device">The Android device whose serial connection will be opened.</param>
    /// <returns>A factory creating an owned stream over the device CDC ACM endpoints.</returns>
    public static Func<SerialPortSettings, CancellationToken, ValueTask<Stream>> UsbCdcAcm(UsbManager manager, UsbDevice device) => (settings, token) =>
    {
        token.ThrowIfCancellationRequested();
        if (!manager.HasPermission(device))
        {
            throw new UnauthorizedAccessException(global::Gurux.Serial.Properties.Resources.Get("USBDevicePermissionHasNotBeenGranted"));
        }

        var interfaces = Enumerable.Range(0, device.InterfaceCount).Select(i => device.GetInterface(i)!).ToArray();
        var controls = interfaces.Where(i => i.InterfaceClass == UsbClass.Comm && (int)i.InterfaceSubclass == 2).ToArray();
        var dataPorts = interfaces.Where(i => i.InterfaceClass == UsbClass.CdcData).ToArray();
        if (controls.Length != 1 || dataPorts.Length != 1)
        {
            throw new NotSupportedException(global::Gurux.Serial.Properties.Resources.Get("ThisAdapterSupportsASingleUSBCDCACMPortThisDeviceNeedsAChipsetSpecificDriver"));
        }

        if (settings.Handshake != Handshake.None)
        {
            throw new NotSupportedException(global::Gurux.Serial.Properties.Resources.Get("CDCACMHardwareSoftwareHandshakingIsNotImplemented"));
        }

        var control = controls[0];
        var data = dataPorts[0];
        var endpoints = Enumerable.Range(0, data.EndpointCount).Select(i => data.GetEndpoint(i)!).Where(e => e.Type == UsbAddressing.XferBulk).ToArray();
        var input = endpoints.FirstOrDefault(e => e.Direction == UsbAddressing.In);
        var output = endpoints.FirstOrDefault(e => e.Direction == UsbAddressing.Out);
        if (input == null || output == null)
        {
            throw new NotSupportedException(global::Gurux.Serial.Properties.Resources.Get("USBCDCDataInterfaceRequiresBulkINAndOUTEndpoints"));
        }

        var connection = manager.OpenDevice(device) ?? throw new IOException(global::Gurux.Serial.Properties.Resources.Get("CannotOpenUSBDevice"));
        try
        {
            if (!connection.ClaimInterface(control, true) || !connection.ClaimInterface(data, true))
            {
                throw new IOException(global::Gurux.Serial.Properties.Resources.Get("CannotClaimUSBCDCInterfaces"));
            }

            byte[] coding = new byte[7];
            System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(coding, settings.BaudRate);
            coding[4] = settings.StopBits switch
            {
                StopBits.One => 0,
                StopBits.OnePointFive => 1,
                _ => 2
            };
            coding[5] = (byte)settings.Parity;
            coding[6] = (byte)settings.DataBits;
            if (connection.ControlTransfer((UsbAddressing)0x21, 0x20, 0, control.Id, coding, 7, 1000) != 7)
            {
                throw new IOException(global::Gurux.Serial.Properties.Resources.Get("USBCDCSETLINECODINGFailed"));
            }

            if (connection.ControlTransfer((UsbAddressing)0x21, 0x22, (settings.DtrEnable ? 1 : 0) | (settings.RtsEnable ? 2 : 0), control.Id, null, 0, 1000) < 0)
            {
                throw new IOException(global::Gurux.Serial.Properties.Resources.Get("USBCDCSETCONTROLLINESTATEFailed"));
            }

            return ValueTask.FromResult<Stream>(new CdcStream(manager, device, connection, control, data, input, output));
        }
        catch
        {
            connection.Close();
            connection.Dispose();
            throw;
        }
    };
    /// <summary>Adapts Android USB CDC bulk endpoints to a cancellable duplex stream.</summary>
    /// <param name="manager">The Android USB manager providing device access and permission state.</param>
    /// <param name="device">The Android device whose serial connection will be opened.</param>
    /// <param name="connection">The opened USB device connection owned by this stream.</param>
    /// <param name="control">The CDC communication interface claimed by this connection.</param>
    /// <param name="data">The CDC data interface claimed by this connection.</param>
    /// <param name="input">The readable stream or bulk IN endpoint owned by this adapter.</param>
    /// <param name="output">The writable stream or bulk OUT endpoint owned by this adapter.</param>
    sealed class CdcStream(UsbManager manager, UsbDevice device, UsbDeviceConnection connection, UsbInterface control, UsbInterface data, UsbEndpoint input, UsbEndpoint output) : Stream
    {
        /// <summary>Records whether this stream has released its owned connection.</summary>
        volatile bool disposed;
        /// <inheritdoc/>
        public override bool CanRead => !disposed;
        /// <inheritdoc/>
        public override bool CanWrite => !disposed;
        /// <inheritdoc/>
        public override bool CanSeek => false;
        /// <inheritdoc/>
        public override long Length => throw new NotSupportedException();
        /// <inheritdoc/>
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        /// <inheritdoc/>
        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (buffer.IsEmpty)
            {
                return 0;
            }

            byte[] bytes = new byte[Math.Min(buffer.Length, 16384)];
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                ObjectDisposedException.ThrowIf(disposed, this);
                int count = await Task.Run(() => connection.BulkTransfer(input, bytes, bytes.Length, 200), cancellationToken).ConfigureAwait(false);
                if (count > 0)
                {
                    bytes.AsMemory(0, count).CopyTo(buffer);
                    return count;
                }

                if (!(manager.DeviceList?.ContainsKey(device.DeviceName) ?? false))
                {
                    throw new IOException(global::Gurux.Serial.Properties.Resources.Get("USBSerialDeviceWasDisconnected"));
                }
            }
        }

        /// <inheritdoc/>
        public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            while (!buffer.IsEmpty)
            {
                cancellationToken.ThrowIfCancellationRequested();
                ObjectDisposedException.ThrowIf(disposed, this);
                byte[] bytes = buffer[..Math.Min(buffer.Length, 16384)].ToArray();
                int count = await Task.Run(() => connection.BulkTransfer(output, bytes, bytes.Length, 1000), cancellationToken).ConfigureAwait(false);
                if (count <= 0)
                {
                    throw new IOException(global::Gurux.Serial.Properties.Resources.Get("USBCDCWriteFailedOrTimedOut"));
                }

                buffer = buffer[count..];
            }
        }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            if (!disposed)
            {
                disposed = true;
                connection.ReleaseInterface(control);
                connection.ReleaseInterface(data);
                connection.Close();
                connection.Dispose();
            }

            base.Dispose(disposing);
        }

        /// <inheritdoc/>
        public override int Read(byte[] b, int o, int c) => ReadAsync(b.AsMemory(o, c)).AsTask().GetAwaiter().GetResult();
        /// <inheritdoc/>
        public override void Write(byte[] b, int o, int c) => WriteAsync(b.AsMemory(o, c)).AsTask().GetAwaiter().GetResult();
        /// <inheritdoc/>
        public override void Flush()
        {
        }

        /// <inheritdoc/>
        public override long Seek(long o, SeekOrigin s) => throw new NotSupportedException();
        /// <inheritdoc/>
        public override void SetLength(long l) => throw new NotSupportedException();
    }
}
#endif

