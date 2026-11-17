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
using Android.Hardware.Usb;

namespace Gurux.Serial;
/// <summary>Owns a claimed USB interface and exposes bounded cancellable bulk transfers.</summary>
internal sealed class AndroidUsbStream : Stream
{
    /// <summary>Serializes native transfers with disposal.</summary>
    private readonly object gate = new();
    /// <summary>Serializes read operations to protect pending payload bytes.</summary>
    private readonly SemaphoreSlim readGate = new(1, 1);
    /// <summary>Serializes multi-transfer writes.</summary>
    private readonly SemaphoreSlim writeGate = new(1, 1);
    /// <summary>Provides current device attachment state.</summary>
    private readonly UsbManager manager;
    /// <summary>Identifies the device owned by this stream.</summary>
    private readonly UsbDevice device;
    /// <summary>Owns the native USB connection.</summary>
    private readonly UsbDeviceConnection connection;
    /// <summary>Identifies the claimed interface released at close.</summary>
    private readonly UsbInterface port;
    /// <summary>Receives serial payload and optional FTDI headers.</summary>
    private readonly UsbEndpoint input;
    /// <summary>Transmits serial payload.</summary>
    private readonly UsbEndpoint output;
    /// <summary>Indicates whether packets contain FTDI modem status headers.</summary>
    private readonly bool ftdi;
    /// <summary>Stores a complete packet-aligned read and its pending payload.</summary>
    private readonly byte[] pending;
    /// <summary>Indexes the next pending payload byte.</summary>
    private int pendingOffset;
    /// <summary>Counts the remaining pending payload bytes.</summary>
    private int pendingCount;
    /// <summary>Records whether the owned native connection has been closed.</summary>
    private volatile bool disposed;

    /// <summary>Initializes a stream after claiming and configuring its interface.</summary>
    /// <param name="manager">The USB attachment service.</param>
    /// <param name="device">The attached USB device.</param>
    /// <param name="connection">The connection transferred to stream ownership.</param>
    /// <param name="port">The claimed serial interface.</param>
    /// <param name="input">The bulk input endpoint.</param>
    /// <param name="output">The bulk output endpoint.</param>
    /// <param name="ftdi">Whether FTDI headers must be removed.</param>
    private AndroidUsbStream(UsbManager manager, UsbDevice device, UsbDeviceConnection connection, UsbInterface port, UsbEndpoint input, UsbEndpoint output, bool ftdi)
    {
        this.manager = manager;
        this.device = device;
        this.connection = connection;
        this.port = port;
        this.input = input;
        this.output = output;
        this.ftdi = ftdi;
        pending = new byte[Math.Max(input.MaxPacketSize, (16384 / input.MaxPacketSize) * input.MaxPacketSize)];
    }
    /// <summary>Claims a selected bulk interface and rolls back ownership if initialization fails.</summary>
    /// <param name="manager">The Android USB service.</param>
    /// <param name="device">The permitted device.</param>
    /// <param name="chipset">The vendor serial protocol.</param>
    /// <param name="index">The zero-based bulk interface index.</param>
    /// <param name="settings">The requested serial options.</param>
    /// <param name="token">The opening cancellation token.</param>
    /// <returns>A configured stream owning its connection and interface.</returns>
    /// <exception cref="UnauthorizedAccessException">USB permission has not been granted.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The selected interface does not exist.</exception>
    /// <exception cref="IOException">Opening, claiming or configuring fails.</exception>
    internal static Stream Open(UsbManager manager, UsbDevice device, AndroidUsbChipset chipset, int index, SerialPortSettings settings, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        if (!manager.HasPermission(device))
        {
            throw new UnauthorizedAccessException(global::Gurux.Serial.Properties.Resources.Get("USBDevicePermissionHasNotBeenGranted"));
        }
        var ports = Enumerable.Range(0, device.InterfaceCount).Select(i => device.GetInterface(i)!)
            .Where(i => Enumerable.Range(0, i.EndpointCount).Select(e => i.GetEndpoint(e)!)
                .Any(e => e.Type == UsbAddressing.XferBulk && e.Direction == UsbAddressing.In) &&
                Enumerable.Range(0, i.EndpointCount).Select(e => i.GetEndpoint(e)!)
                .Any(e => e.Type == UsbAddressing.XferBulk && e.Direction == UsbAddressing.Out)).ToArray();
        if (index >= ports.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }
        UsbInterface port = ports[index];
        var endpoints = Enumerable.Range(0, port.EndpointCount).Select(i => port.GetEndpoint(i)!).Where(e => e.Type == UsbAddressing.XferBulk).ToArray();
        UsbEndpoint input = endpoints.First(e => e.Direction == UsbAddressing.In);
        UsbEndpoint output = endpoints.First(e => e.Direction == UsbAddressing.Out);
        if (input.MaxPacketSize < 3 || input.MaxPacketSize > 16384)
        {
            throw new NotSupportedException(global::Gurux.Serial.Properties.Resources.Get("USBSerialOptionsUnsupported"));
        }
        UsbDeviceConnection connection = manager.OpenDevice(device) ?? throw new IOException(global::Gurux.Serial.Properties.Resources.Get("CannotOpenUSBDevice"));
        bool claimed = false;
        try
        {
            claimed = connection.ClaimInterface(port, true);
            if (!claimed)
            {
                throw new IOException(global::Gurux.Serial.Properties.Resources.Get("USBSerialInterfaceClaimFailed"));
            }
            AndroidUsbConfiguration.Apply(connection, chipset, port.Id, settings, token);
            return new AndroidUsbStream(manager, device, connection, port, input, output, chipset == AndroidUsbChipset.Ftdi);
        }
        catch
        {
            try
            {
                if (claimed)
                {
                    connection.ReleaseInterface(port);
                }
            }
            finally
            {
                connection.Close();
                connection.Dispose();
            }
            throw;
        }
    }
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
    /// <summary>Performs one bounded transfer while preventing concurrent connection disposal.</summary>
    /// <param name="endpoint">The transfer endpoint.</param>
    /// <param name="bytes">The transfer buffer.</param>
    /// <param name="timeout">The finite native timeout in milliseconds.</param>
    /// <returns>The native transfer result.</returns>
    private int Transfer(UsbEndpoint endpoint, byte[] bytes, int timeout)
    {
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            return connection.BulkTransfer(endpoint, bytes, bytes.Length, timeout);
        }
    }
    /// <inheritdoc/>
    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (buffer.IsEmpty)
        {
            return 0;
        }
        await readGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            while (pendingCount == 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                int count = await Task.Run(() => Transfer(input, pending, 200), cancellationToken).ConfigureAwait(false);
                if (count > 0)
                {
                    pendingOffset = 0;
                    pendingCount = ftdi ? AndroidUsbProtocol.StripFtdiStatus(pending, count, input.MaxPacketSize) : count;
                }
                else if (!(manager.DeviceList?.ContainsKey(device.DeviceName) ?? false))
                {
                    throw new IOException(global::Gurux.Serial.Properties.Resources.Get("USBSerialDeviceWasDisconnected"));
                }
            }
            cancellationToken.ThrowIfCancellationRequested();
            ObjectDisposedException.ThrowIf(disposed, this);
            int length = Math.Min(buffer.Length, pendingCount);
            pending.AsMemory(pendingOffset, length).CopyTo(buffer);
            pendingOffset += length;
            pendingCount -= length;
            return length;
        }
        finally
        {
            readGate.Release();
        }
    }
    /// <inheritdoc/>
    public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        await writeGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            while (!buffer.IsEmpty)
            {
                cancellationToken.ThrowIfCancellationRequested();
                byte[] bytes = buffer[..Math.Min(buffer.Length, 16384)].ToArray();
                int count = await Task.Run(() => Transfer(output, bytes, 1000), cancellationToken).ConfigureAwait(false);
                if (count <= 0 || count > bytes.Length)
                {
                    throw new IOException(global::Gurux.Serial.Properties.Resources.Get("USBSerialWriteFailed"));
                }
                buffer = buffer[count..];
            }
            cancellationToken.ThrowIfCancellationRequested();
        }
        finally
        {
            writeGate.Release();
        }
    }
    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        lock (gate)
        {
            if (!disposed)
            {
                disposed = true;
                try
                {
                    connection.ReleaseInterface(port);
                }
                finally
                {
                    connection.Close();
                    connection.Dispose();
                }
            }
        }
        base.Dispose(disposing);
    }
    /// <inheritdoc/>
    public override int Read(byte[] buffer, int offset, int count) => ReadAsync(buffer.AsMemory(offset, count)).AsTask().GetAwaiter().GetResult();
    /// <inheritdoc/>
    public override void Write(byte[] buffer, int offset, int count) => WriteAsync(buffer.AsMemory(offset, count)).AsTask().GetAwaiter().GetResult();
    /// <inheritdoc/>
    public override void Flush()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
    }
    /// <inheritdoc/>
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    /// <inheritdoc/>
    public override void SetLength(long value) => throw new NotSupportedException();
}
#endif
