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
using System.Runtime.InteropServices;

namespace Gurux.Serial.Bluetooth.Classic;

/// <summary>Imports native nonblocking socket operations without managed address-family translation.</summary>
internal static class RfcommNative
{
    /// <summary>Invokes native socket from ws2_32.dll.</summary>
    /// <param name="family">The native address family identifying Bluetooth sockets.</param>
    /// <param name="type">The stream socket type and any nonblocking or close-on-exec flags.</param>
    /// <param name="protocol">The RFCOMM protocol identifier.</param>
    /// <returns>The native result; callers inspect the platform error on failure.</returns>
    [DllImport("ws2_32.dll", EntryPoint = "socket", SetLastError = true)]
    internal static extern IntPtr WindowsSocket(int family, int type, int protocol);

    /// <summary>Invokes native socket from libc.</summary>
    /// <param name="family">The native address family identifying Bluetooth sockets.</param>
    /// <param name="type">The stream socket type and any nonblocking or close-on-exec flags.</param>
    /// <param name="protocol">The RFCOMM protocol identifier.</param>
    /// <returns>The native result; callers inspect the platform error on failure.</returns>
    [DllImport("libc", EntryPoint = "socket", SetLastError = true)]
    internal static extern int LinuxSocket(int family, int type, int protocol);

    /// <summary>Invokes native connect from ws2_32.dll.</summary>
    /// <param name="socket">The owned native socket descriptor.</param>
    /// <param name="address">The encoded native RFCOMM remote endpoint.</param>
    /// <param name="length">The buffer size in bytes; socket option calls update it with the returned size.</param>
    /// <returns>The native result; callers inspect the platform error on failure.</returns>
    [DllImport("ws2_32.dll", EntryPoint = "connect", SetLastError = true)]
    internal static extern int WindowsConnect(IntPtr socket, byte[] address, int length);

    /// <summary>Invokes native connect from libc.</summary>
    /// <param name="socket">The owned native socket descriptor.</param>
    /// <param name="address">The encoded native RFCOMM remote endpoint.</param>
    /// <param name="length">The buffer size in bytes; socket option calls update it with the returned size.</param>
    /// <returns>The native result; callers inspect the platform error on failure.</returns>
    [DllImport("libc", EntryPoint = "connect", SetLastError = true)]
    internal static extern int LinuxConnect(IntPtr socket, byte[] address, int length);

    /// <summary>Invokes native ioctlsocket from ws2_32.dll.</summary>
    /// <param name="socket">The owned native socket descriptor.</param>
    /// <param name="command">The ioctl identifier enabling nonblocking socket operation.</param>
    /// <param name="value">The nonblocking setting or returned pending socket error.</param>
    /// <returns>The native result; callers inspect the platform error on failure.</returns>
    [DllImport("ws2_32.dll", EntryPoint = "ioctlsocket", SetLastError = true)]
    internal static extern int WindowsIoctl(IntPtr socket, uint command, ref uint value);

    /// <summary>Invokes native closesocket from ws2_32.dll.</summary>
    /// <param name="socket">The owned native socket descriptor.</param>
    /// <returns>The native result; callers inspect the platform error on failure.</returns>
    [DllImport("ws2_32.dll", EntryPoint = "closesocket", SetLastError = true)]
    internal static extern int WindowsClose(IntPtr socket);

    /// <summary>Invokes native close from libc.</summary>
    /// <param name="socket">The owned native socket descriptor.</param>
    /// <returns>The native result; callers inspect the platform error on failure.</returns>
    [DllImport("libc", EntryPoint = "close", SetLastError = true)]
    internal static extern int LinuxClose(IntPtr socket);

    /// <summary>Invokes native recv from ws2_32.dll.</summary>
    /// <param name="socket">The owned native socket descriptor.</param>
    /// <param name="buffer">The bytes to send or the storage receiving available bytes.</param>
    /// <param name="length">The buffer size in bytes; socket option calls update it with the returned size.</param>
    /// <param name="flags">Native transfer flags, including suppression of SIGPIPE on Linux sends.</param>
    /// <returns>The native result; callers inspect the platform error on failure.</returns>
    [DllImport("ws2_32.dll", EntryPoint = "recv", SetLastError = true)]
    internal static extern int WindowsReceive(IntPtr socket, [Out] byte[] buffer, int length, int flags);

    /// <summary>Invokes native recv from libc.</summary>
    /// <param name="socket">The owned native socket descriptor.</param>
    /// <param name="buffer">The bytes to send or the storage receiving available bytes.</param>
    /// <param name="length">The buffer size in bytes; socket option calls update it with the returned size.</param>
    /// <param name="flags">Native transfer flags, including suppression of SIGPIPE on Linux sends.</param>
    /// <returns>The native result; callers inspect the platform error on failure.</returns>
    [DllImport("libc", EntryPoint = "recv", SetLastError = true)]
    internal static extern nint LinuxReceive(IntPtr socket, [Out] byte[] buffer, nuint length, int flags);

    /// <summary>Invokes native send from ws2_32.dll.</summary>
    /// <param name="socket">The owned native socket descriptor.</param>
    /// <param name="buffer">The bytes to send or the storage receiving available bytes.</param>
    /// <param name="length">The buffer size in bytes; socket option calls update it with the returned size.</param>
    /// <param name="flags">Native transfer flags, including suppression of SIGPIPE on Linux sends.</param>
    /// <returns>The native result; callers inspect the platform error on failure.</returns>
    [DllImport("ws2_32.dll", EntryPoint = "send", SetLastError = true)]
    internal static extern int WindowsSend(IntPtr socket, byte[] buffer, int length, int flags);

    /// <summary>Invokes native send from libc.</summary>
    /// <param name="socket">The owned native socket descriptor.</param>
    /// <param name="buffer">The bytes to send or the storage receiving available bytes.</param>
    /// <param name="length">The buffer size in bytes; socket option calls update it with the returned size.</param>
    /// <param name="flags">Native transfer flags, including suppression of SIGPIPE on Linux sends.</param>
    /// <returns>The native result; callers inspect the platform error on failure.</returns>
    [DllImport("libc", EntryPoint = "send", SetLastError = true)]
    internal static extern nint LinuxSend(IntPtr socket, byte[] buffer, nuint length, int flags);

    /// <summary>Invokes native WSAGetLastError from ws2_32.dll.</summary>
    /// <returns>The native result; callers inspect the platform error on failure.</returns>
    [DllImport("ws2_32.dll", EntryPoint = "WSAGetLastError")]
    internal static extern int WindowsError();

    /// <summary>Invokes native WSAStartup from ws2_32.dll.</summary>
    /// <param name="version">The requested Winsock API version.</param>
    /// <param name="data">Storage for the Winsock version and implementation information.</param>
    /// <returns>The native result; callers inspect the platform error on failure.</returns>
    [DllImport("ws2_32.dll", EntryPoint = "WSAStartup", SetLastError = true)]
    internal static extern int WindowsStartup(ushort version, byte[] data);

    /// <summary>Invokes native WSACleanup from ws2_32.dll.</summary>
    /// <returns>The native result; callers inspect the platform error on failure.</returns>
    [DllImport("ws2_32.dll", EntryPoint = "WSACleanup", SetLastError = true)]
    internal static extern int WindowsCleanup();

    /// <summary>Invokes native select from ws2_32.dll.</summary>
    /// <param name="ignored">The unused Winsock select descriptor count.</param>
    /// <param name="read">A null pointer because connection establishment does not wait for readable data.</param>
    /// <param name="write">A single-socket fd_set receiving writable connection completion.</param>
    /// <param name="error">A single-socket fd_set receiving failed connection completion.</param>
    /// <param name="timeout">The zero timeval buffer requesting an immediate readiness check.</param>
    /// <returns>The native result; callers inspect the platform error on failure.</returns>
    [DllImport("ws2_32.dll", EntryPoint = "select", SetLastError = true)]
    internal static extern int WindowsSelect(int ignored, IntPtr read, byte[] write, byte[] error, byte[] timeout);

    /// <summary>Invokes native poll from libc.</summary>
    /// <param name="descriptors">A pollfd buffer describing the connecting socket and requested readiness.</param>
    /// <param name="count">The number of pollfd records in the buffer.</param>
    /// <param name="timeout">The zero timeval buffer requesting an immediate readiness check.</param>
    /// <returns>The native result; callers inspect the platform error on failure.</returns>
    [DllImport("libc", EntryPoint = "poll", SetLastError = true)]
    internal static extern int LinuxPoll(byte[] descriptors, nuint count, int timeout);

    /// <summary>Invokes native getsockopt from ws2_32.dll.</summary>
    /// <param name="socket">The owned native socket descriptor.</param>
    /// <param name="level">The native socket option level.</param>
    /// <param name="option">The native SO_ERROR identifier retrieving deferred connection failure.</param>
    /// <param name="value">The nonblocking setting or returned pending socket error.</param>
    /// <param name="length">The buffer size in bytes; socket option calls update it with the returned size.</param>
    /// <returns>The native result; callers inspect the platform error on failure.</returns>
    [DllImport("ws2_32.dll", EntryPoint = "getsockopt", SetLastError = true)]
    internal static extern int WindowsGetError(IntPtr socket, int level, int option, out int value, ref int length);

    /// <summary>Invokes native getsockopt from libc.</summary>
    /// <param name="socket">The owned native socket descriptor.</param>
    /// <param name="level">The native socket option level.</param>
    /// <param name="option">The native SO_ERROR identifier retrieving deferred connection failure.</param>
    /// <param name="value">The nonblocking setting or returned pending socket error.</param>
    /// <param name="length">The buffer size in bytes; socket option calls update it with the returned size.</param>
    /// <returns>The native result; callers inspect the platform error on failure.</returns>
    [DllImport("libc", EntryPoint = "getsockopt", SetLastError = true)]
    internal static extern int LinuxGetError(IntPtr socket, int level, int option, out int value, ref int length);

}

#endif
