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
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace Gurux.Serial.Bluetooth.Classic;

/// <summary>Resolves the Serial Port RFCOMM channel using the system BlueZ SDP client.</summary>
internal static class LinuxRfcommSdp
{
    /// <summary>Connects to SDP without blocking the caller, then reads the Serial Port protocol descriptors.</summary>
    /// <param name="address">The numeric remote Bluetooth address.</param>
    /// <param name="cancellationToken">Cancels both SDP connection and service retrieval.</param>
    /// <returns>The first valid RFCOMM channel advertised by a Serial Port record.</returns>
    /// <exception cref="SocketException">The native SDP connection or query fails.</exception>
    /// <exception cref="IOException">No Serial Port record contains a valid RFCOMM channel.</exception>
    /// <exception cref="OperationCanceledException">The query is canceled; native cleanup completes before returning.</exception>
    internal static async Task<int> GetChannelAsync(ulong address, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        byte[] destination = new byte[6];
        for (int index = 0; index != destination.Length; ++index)
        {
            destination[index] = (byte)(address >> (8 * index));
        }
        IntPtr session = Connect(new byte[6], destination, 4);
        if (session == IntPtr.Zero)
        {
            throw new SocketException(Marshal.GetLastPInvokeError());
        }
        try
        {
            int socket = GetSocket(session);
            byte[] descriptor = new byte[8];
            BitConverter.GetBytes(socket).CopyTo(descriptor, 0);
            BitConverter.GetBytes((short)4).CopyTo(descriptor, 4);
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                int ready = RfcommNative.LinuxPoll(descriptor, 1, 0);
                if (ready > 0) { break; }
                if (ready < 0 && Marshal.GetLastPInvokeError() != 4)
                {
                    throw new SocketException(Marshal.GetLastPInvokeError());
                }
                await Task.Delay(10, cancellationToken).ConfigureAwait(false);
            }
            int length = sizeof(int);
            if (RfcommNative.LinuxGetError(new IntPtr(socket), 1, 4, out int error, ref length) != 0)
            {
                throw new SocketException(Marshal.GetLastPInvokeError());
            }
            if (error != 0) { throw new SocketException(error); }
            int flags = Fcntl(socket, 3, 0);
            if (flags < 0 || Fcntl(socket, 4, flags & ~0x800) < 0)
            {
                throw new SocketException(Marshal.GetLastPInvokeError());
            }
            // Shutdown interrupts a blocking SDP receive. Dispose the registration before closing
            // the session so its descriptor cannot be reused while a callback is still running.
            using CancellationTokenRegistration registration = cancellationToken.Register(() => Shutdown(socket, 2));
            try
            {
                return await Task.Run(() => Query(session, cancellationToken), cancellationToken).ConfigureAwait(false);
            }
            catch (Exception) when (cancellationToken.IsCancellationRequested)
            {
                throw new OperationCanceledException(cancellationToken);
            }
        }
        finally
        {
            Close(session);
        }
    }

    /// <summary>Reads protocol descriptor lists and releases all BlueZ-owned records and list nodes.</summary>
    /// <param name="session">The connected SDP session.</param>
    /// <param name="cancellationToken">Cancels before retrieval and before accepting a channel.</param>
    /// <returns>A channel between one and thirty.</returns>
    /// <exception cref="SocketException">The remote service query fails.</exception>
    /// <exception cref="IOException">No valid Serial Port RFCOMM endpoint is advertised.</exception>
    /// <exception cref="OperationCanceledException">The query is canceled.</exception>
    private static int Query(IntPtr session, CancellationToken cancellationToken)
    {
        IntPtr uuid = Marshal.AllocHGlobal(20);
        IntPtr range = Marshal.AllocHGlobal(4);
        IntPtr search = IntPtr.Zero;
        IntPtr attributes = IntPtr.Zero;
        IntPtr records = IntPtr.Zero;
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            CreateUuid(uuid, 0x1101);
            Marshal.WriteInt32(range, 0x0000ffff);
            search = Append(IntPtr.Zero, uuid);
            attributes = Append(IntPtr.Zero, range);
            if (Search(session, search, 2, attributes, out records) != 0)
            {
                throw new SocketException(Marshal.GetLastPInvokeError());
            }
            for (IntPtr node = records; node != IntPtr.Zero; node = Marshal.ReadIntPtr(node))
            {
                cancellationToken.ThrowIfCancellationRequested();
                IntPtr protocols = IntPtr.Zero;
                try
                {
                    if (GetProtocols(Marshal.ReadIntPtr(node, IntPtr.Size), out protocols) == 0)
                    {
                        int channel = GetPort(protocols, 3);
                        if (channel is >= 1 and <= 30) { return channel; }
                    }
                }
                finally
                {
                    for (IntPtr item = protocols; item != IntPtr.Zero; item = Marshal.ReadIntPtr(item))
                    {
                        FreeList(Marshal.ReadIntPtr(item, IntPtr.Size), IntPtr.Zero);
                    }
                    FreeList(protocols, IntPtr.Zero);
                }
            }
            throw new IOException(Properties.Resources.Get("BluetoothSerialServiceNotFound"));
        }
        finally
        {
            for (IntPtr node = records; node != IntPtr.Zero; node = Marshal.ReadIntPtr(node))
            {
                FreeRecord(Marshal.ReadIntPtr(node, IntPtr.Size));
            }
            FreeList(records, IntPtr.Zero);
            FreeList(attributes, IntPtr.Zero);
            FreeList(search, IntPtr.Zero);
            Marshal.FreeHGlobal(range);
            Marshal.FreeHGlobal(uuid);
        }
    }

    /// <summary>Begins a nonblocking SDP connection.</summary>
    /// <param name="source">The local address, all zero to select any adapter.</param>
    /// <param name="destination">The remote address in native byte order.</param>
    /// <param name="flags">BlueZ connection flags.</param>
    /// <returns>An owned session or zero with errno set.</returns>
    [DllImport("libbluetooth.so.3", EntryPoint = "sdp_connect", SetLastError = true)]
    private static extern IntPtr Connect(byte[] source, byte[] destination, uint flags);
    /// <summary>Releases a session and its socket.</summary>
    /// <param name="session">The owned session.</param>
    /// <returns>The native close result.</returns>
    [DllImport("libbluetooth.so.3", EntryPoint = "sdp_close")]
    private static extern int Close(IntPtr session);
    /// <summary>Gets a session's borrowed socket.</summary>
    /// <param name="session">The connected or pending session.</param>
    /// <returns>The native socket descriptor.</returns>
    [DllImport("libbluetooth.so.3", EntryPoint = "sdp_get_socket")]
    private static extern int GetSocket(IntPtr session);
    /// <summary>Initializes a native 16-bit SDP UUID.</summary>
    /// <param name="uuid">Storage for the native UUID structure.</param>
    /// <param name="value">The Bluetooth service identifier.</param>
    /// <returns>The initialized structure pointer.</returns>
    [DllImport("libbluetooth.so.3", EntryPoint = "sdp_uuid16_create")]
    private static extern IntPtr CreateUuid(IntPtr uuid, ushort value);
    /// <summary>Appends borrowed data to a BlueZ list.</summary>
    /// <param name="list">The existing list or zero.</param>
    /// <param name="data">The borrowed data pointer.</param>
    /// <returns>The list head.</returns>
    [DllImport("libbluetooth.so.3", EntryPoint = "sdp_list_append")]
    private static extern IntPtr Append(IntPtr list, IntPtr data);
    /// <summary>Frees list nodes without freeing borrowed data when no callback is supplied.</summary>
    /// <param name="list">The list head or zero.</param>
    /// <param name="free">The data destructor, or zero.</param>
    [DllImport("libbluetooth.so.3", EntryPoint = "sdp_list_free")]
    private static extern void FreeList(IntPtr list, IntPtr free);
    /// <summary>Retrieves matching service records with requested attributes.</summary>
    /// <param name="session">The connected session.</param>
    /// <param name="search">The service UUID search list.</param>
    /// <param name="type">The attribute request mode.</param>
    /// <param name="attributes">The requested attribute range.</param>
    /// <param name="records">Receives owned service records.</param>
    /// <returns>Zero on success, otherwise a native error.</returns>
    [DllImport("libbluetooth.so.3", EntryPoint = "sdp_service_search_attr_req", SetLastError = true)]
    private static extern int Search(IntPtr session, IntPtr search, int type, IntPtr attributes, out IntPtr records);
    /// <summary>Builds borrowed protocol descriptor lists for a service record.</summary>
    /// <param name="record">The service record.</param>
    /// <param name="protocols">Receives lists whose nodes must be released.</param>
    /// <returns>Zero when descriptors exist.</returns>
    [DllImport("libbluetooth.so.3", EntryPoint = "sdp_get_access_protos")]
    private static extern int GetProtocols(IntPtr record, out IntPtr protocols);
    /// <summary>Extracts the RFCOMM channel from protocol descriptors.</summary>
    /// <param name="protocols">The protocol descriptor lists.</param>
    /// <param name="protocol">The RFCOMM protocol identifier.</param>
    /// <returns>The advertised channel or a negative error.</returns>
    [DllImport("libbluetooth.so.3", EntryPoint = "sdp_get_proto_port")]
    private static extern int GetPort(IntPtr protocols, int protocol);
    /// <summary>Releases a returned service record and its attributes.</summary>
    /// <param name="record">The owned service record.</param>
    [DllImport("libbluetooth.so.3", EntryPoint = "sdp_record_free")]
    private static extern void FreeRecord(IntPtr record);
    /// <summary>Reads or changes descriptor flags.</summary>
    /// <param name="socket">The descriptor.</param>
    /// <param name="command">The F_GETFL or F_SETFL operation.</param>
    /// <param name="value">Flags for F_SETFL.</param>
    /// <returns>The flags or zero on success; negative on failure.</returns>
    [DllImport("libc", EntryPoint = "fcntl", SetLastError = true)]
    private static extern int Fcntl(int socket, int command, int value);
    /// <summary>Interrupts in-flight socket operations without releasing the descriptor.</summary>
    /// <param name="socket">The descriptor owned by the SDP session.</param>
    /// <param name="how">Two to disable both directions.</param>
    /// <returns>The native shutdown result.</returns>
    [DllImport("libc", EntryPoint = "shutdown")]
    private static extern int Shutdown(int socket, int how);
}
#endif
