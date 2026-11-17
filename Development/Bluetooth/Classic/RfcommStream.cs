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
#if !ANDROID && !IOS && !MACCATALYST
using System;
using System.IO;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Gurux.Serial.Properties;

namespace Gurux.Serial.Bluetooth.Classic;

/// <summary>Owns a nonblocking native RFCOMM socket and exposes cancellable asynchronous byte transfers.</summary>
internal sealed class RfcommStream : Stream
{
    /// <summary>Serializes short native calls with handle disposal to prevent descriptor reuse races.</summary>
    private readonly object gate = new();
    /// <summary>Preserves write ordering while a partial native send waits for capacity.</summary>
    private readonly SemaphoreSlim writes = new(1, 1);
    /// <summary>Preserves read ordering across concurrent consumers.</summary>
    private readonly SemaphoreSlim reads = new(1, 1);
    /// <summary>Records the platform ABI selected when creating the socket.</summary>
    private readonly bool windows;
    /// <summary>Stores the owned native socket, or minus one after disposal.</summary>
    private IntPtr socket = new(-1);
    /// <summary>Records ownership of one successful Winsock startup reference.</summary>
    private bool winsockStarted;

    /// <summary>Creates a native socket and configures nonblocking operation.</summary>
    /// <param name="windows">Whether to use Winsock instead of Linux sockets.</param>
    private RfcommStream(bool windows)
    {
        this.windows = windows;
        try
        {
            if (windows)
            {
                int error = RfcommNative.WindowsStartup(0x202, new byte[512]);
                if (error != 0)
                {
                    throw new SocketException(error);
                }
                winsockStarted = true;
                socket = RfcommNative.WindowsSocket(32, 1, 3);
            }
            else
            {
                socket = new IntPtr(RfcommNative.LinuxSocket(31, 1 | 0x800 | 0x80000, 3));
            }
            if (socket == new IntPtr(-1))
            {
                throw NativeException();
            }
            if (windows)
            {
                uint enabled = 1;
                if (RfcommNative.WindowsIoctl(socket, 0x8004667e, ref enabled) != 0)
                {
                    throw NativeException();
                }
            }
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    /// <summary>Releases an abandoned native socket and its Winsock reference.</summary>
    ~RfcommStream()
    {
        Dispose(false);
    }

    /// <summary>Connects a native socket without blocking a caller or retaining resources on failure.</summary>
    /// <param name="address">The numeric remote device address.</param>
    /// <param name="channel">The remote channel, or zero for Windows Serial Port SDP resolution.</param>
    /// <param name="cancellationToken">Cancels connection establishment.</param>
    /// <returns>The connected owning stream.</returns>
    internal static async ValueTask<Stream> ConnectAsync(ulong address, int channel, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        RfcommStream stream = new(OperatingSystem.IsWindows());
        try
        {
            byte[] endpoint = RfcommEndpoint.Serialize(address, channel, stream.windows);
            int result = stream.windows
                ? RfcommNative.WindowsConnect(stream.socket, endpoint, endpoint.Length)
                : RfcommNative.LinuxConnect(stream.socket, endpoint, endpoint.Length);
            if (result != 0)
            {
                int error = stream.LastError();
                if (!stream.IsPending(error))
                {
                    throw new SocketException(error);
                }
                while (!stream.ConnectionReady())
                {
                    await Task.Delay(10, cancellationToken).ConfigureAwait(false);
                }
            }
            cancellationToken.ThrowIfCancellationRequested();
            return stream;
        }
        catch
        {
            stream.Dispose();
            throw;
        }
    }

    /// <summary>Checks completion of a nonblocking connect and retrieves any deferred socket error.</summary>
    /// <returns>Whether the socket has finished connecting successfully.</returns>
    private bool ConnectionReady()
    {
        int ready;
        if (windows)
        {
            int offset = IntPtr.Size == 8 ? 8 : 4;
            byte[] write = new byte[offset + IntPtr.Size];
            BitConverter.GetBytes(1).CopyTo(write, 0);
            if (IntPtr.Size == 8)
            {
                BitConverter.GetBytes(socket.ToInt64()).CopyTo(write, offset);
            }
            else
            {
                BitConverter.GetBytes(socket.ToInt32()).CopyTo(write, offset);
            }
            byte[] error = (byte[])write.Clone();
            ready = RfcommNative.WindowsSelect(0, IntPtr.Zero, write, error, new byte[8]);
        }
        else
        {
            byte[] descriptor = new byte[8];
            BitConverter.GetBytes(socket.ToInt32()).CopyTo(descriptor, 0);
            BitConverter.GetBytes((short)4).CopyTo(descriptor, 4);
            ready = RfcommNative.LinuxPoll(descriptor, 1, 0);
        }
        if (ready < 0)
        {
            int error = LastError();
            if (error == 4)
            {
                return false;
            }
            throw new SocketException(error);
        }
        if (ready == 0)
        {
            return false;
        }
        int length = 4;
        int value;
        int result = windows
            ? RfcommNative.WindowsGetError(socket, 0xffff, 0x1007, out value, ref length)
            : RfcommNative.LinuxGetError(socket, 1, 4, out value, ref length);
        if (result != 0)
        {
            throw NativeException();
        }
        if (value != 0)
        {
            throw new SocketException(value);
        }
        return true;
    }

    /// <summary>Returns the platform error immediately after a failed native call.</summary>
    /// <returns>The native Winsock error or Linux errno.</returns>
    private int LastError() => windows ? RfcommNative.WindowsError() : Marshal.GetLastPInvokeError();
    /// <summary>Creates an exception from the most recent native socket error.</summary>
    /// <returns>The socket failure.</returns>
    private SocketException NativeException() => new(LastError());
    /// <summary>Recognizes retryable nonblocking and interrupted socket operations.</summary>
    /// <param name="error">The native socket error.</param>
    /// <returns>Whether an asynchronous retry can make progress.</returns>
    private bool IsPending(int error) => windows ? error is 10035 or 10036 or 10037 : error is 11 or 115 or 114 or 4;

    /// <inheritdoc/>
    public override bool CanRead => socket != new IntPtr(-1);
    /// <inheritdoc/>
    public override bool CanWrite => CanRead;
    /// <inheritdoc/>
    public override bool CanSeek => false;
    /// <inheritdoc/>
    public override long Length => throw new NotSupportedException(Resources.Get("BluetoothStreamNotSeekable"));
    /// <inheritdoc/>
    public override long Position
    {
        get => throw new NotSupportedException(Resources.Get("BluetoothStreamNotSeekable"));
        set => throw new NotSupportedException(Resources.Get("BluetoothStreamNotSeekable"));
    }
    /// <inheritdoc/>
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException(Resources.Get("BluetoothStreamNotSeekable"));
    /// <inheritdoc/>
    public override void SetLength(long value) => throw new NotSupportedException(Resources.Get("BluetoothStreamNotSeekable"));
    /// <inheritdoc/>
    public override void Flush()
    {
        ObjectDisposedException.ThrowIf(!CanWrite, this);
    }
    /// <inheritdoc/>
    public override Task FlushAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Flush();
        return Task.CompletedTask;
    }
    /// <inheritdoc/>
    public override int Read(byte[] buffer, int offset, int count) => ReadAsync(buffer.AsMemory(offset, count)).AsTask().GetAwaiter().GetResult();
    /// <inheritdoc/>
    public override void Write(byte[] buffer, int offset, int count) => WriteAsync(buffer.AsMemory(offset, count)).AsTask().GetAwaiter().GetResult();
    /// <inheritdoc/>
    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();
    /// <inheritdoc/>
    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => WriteAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

    /// <inheritdoc/>
    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        await reads.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            ObjectDisposedException.ThrowIf(!CanRead, this);
            if (buffer.IsEmpty)
            {
                return 0;
            }
            byte[] chunk = new byte[Math.Min(buffer.Length, 65536)];
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                int result = Transfer(chunk, false);
                if (result >= 0)
                {
                    chunk.AsMemory(0, result).CopyTo(buffer);
                    return result;
                }
                await Task.Delay(10, cancellationToken).ConfigureAwait(false);
            }
        }
        finally
        {
            reads.Release();
        }
    }

    /// <inheritdoc/>
    public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        await writes.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            ObjectDisposedException.ThrowIf(!CanWrite, this);
            while (!buffer.IsEmpty)
            {
                cancellationToken.ThrowIfCancellationRequested();
                byte[] chunk = buffer[..Math.Min(buffer.Length, 65536)].ToArray();
                int result = Transfer(chunk, true);
                if (result > 0)
                {
                    buffer = buffer[result..];
                }
                else
                {
                    await Task.Delay(10, cancellationToken).ConfigureAwait(false);
                }
            }
        }
        finally
        {
            writes.Release();
        }
    }

    /// <summary>Performs one nonblocking transfer while preventing concurrent descriptor disposal.</summary>
    /// <param name="buffer">The input or output transfer buffer.</param>
    /// <param name="write">Whether to send bytes instead of receiving them.</param>
    /// <returns>The transferred count, zero on peer EOF, or minus one when retrying is necessary.</returns>
    private int Transfer(byte[] buffer, bool write)
    {
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(!CanRead, this);
            int result;
            if (windows)
            {
                result = write ? RfcommNative.WindowsSend(socket, buffer, buffer.Length, 0) : RfcommNative.WindowsReceive(socket, buffer, buffer.Length, 0);
            }
            else
            {
                result = (int)(write ? RfcommNative.LinuxSend(socket, buffer, (nuint)buffer.Length, 0x4000) : RfcommNative.LinuxReceive(socket, buffer, (nuint)buffer.Length, 0));
            }
            if (result < 0)
            {
                int error = LastError();
                if (!IsPending(error))
                {
                    throw new IOException(new SocketException(error).Message, new SocketException(error));
                }
            }
            return result;
        }
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        lock (gate)
        {
            if (socket != new IntPtr(-1))
            {
                if (windows)
                {
                    RfcommNative.WindowsClose(socket);
                }
                else
                {
                    RfcommNative.LinuxClose(socket);
                }
                socket = new IntPtr(-1);
            }
            if (winsockStarted)
            {
                RfcommNative.WindowsCleanup();
                winsockStarted = false;
            }
        }
        base.Dispose(disposing);
    }
}

#endif
