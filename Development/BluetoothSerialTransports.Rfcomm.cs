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
using System.Threading;
using System.Threading.Tasks;
using Gurux.Serial.Bluetooth.Classic;
using Gurux.Serial.Properties;

namespace Gurux.Serial;

/// <summary>Creates Bluetooth streams for the native serial lifecycle.</summary>
public static partial class BluetoothSerialTransports
{
    /// <summary>Returns cached Windows Bluetooth Classic devices without a radio inquiry.</summary>
    /// <param name="cancellationToken">Cancels cached enumeration.</param>
    /// <returns>The cached device identities and enabled service UUIDs without opening a transport.</returns>
    /// <exception cref="PlatformNotSupportedException">The operating system is not Windows.</exception>
    /// <exception cref="OperationCanceledException">Cached enumeration is canceled.</exception>
    internal static Task<BluetoothSerialDevice[]> GetClassicDevicesAsync(CancellationToken cancellationToken = default)
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException(Resources.Get("BluetoothClassicUnsupported"));
        }
        return RfcommDevices.GetAsync(cancellationToken);
    }

    /// <summary>Creates a Windows or Linux Bluetooth Classic connection factory with automatic Serial Port SDP resolution by default.</summary>
    /// <param name="address">The remote Bluetooth address as six hexadecimal byte pairs or twelve hexadecimal digits.</param>
    /// <param name="channel">Zero to resolve the Serial Port service through SDP, or an explicit channel from one through thirty.</param>
    /// <param name="connectionTimeout">A positive finite open timeout; the default is thirty seconds.</param>
    /// <returns>A factory that opens an owning bidirectional stream and honors open cancellation.</returns>
    /// <exception cref="ArgumentException">The device address is invalid.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The channel or timeout is outside its supported range.</exception>
    /// <exception cref="PlatformNotSupportedException">The current operating system is neither Windows nor Linux.</exception>
    /// <remarks>Windows resolves the Serial Port service during connect. Linux queries BlueZ SDP before connecting. An explicitly supplied channel bypasses SDP. This factory does not pair devices or apply UART framing settings.</remarks>
    public static Func<SerialPortSettings, CancellationToken, ValueTask<Stream>> BluetoothRfcomm(
        string address, int channel = 0, TimeSpan? connectionTimeout = null)
    {
        ulong parsed = RfcommEndpoint.ParseAddress(address);
        if (channel < 0 || channel > 30)
        {
            throw new ArgumentOutOfRangeException(nameof(channel), Resources.Get("BluetoothChannelInvalid"));
        }
        TimeSpan timeout = connectionTimeout ?? TimeSpan.FromSeconds(30);
        if (timeout <= TimeSpan.Zero || timeout.TotalMilliseconds > uint.MaxValue - 1)
        {
            throw new ArgumentOutOfRangeException(nameof(connectionTimeout), Resources.Get("BluetoothTimeoutInvalid"));
        }
        if (!OperatingSystem.IsWindows() && !OperatingSystem.IsLinux())
        {
            throw new PlatformNotSupportedException(Resources.Get("BluetoothClassicUnsupported"));
        }
        return async (_, cancellationToken) =>
        {
            using CancellationTokenSource deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            deadline.CancelAfter(timeout);
            try
            {
                int resolved = channel == 0 && OperatingSystem.IsLinux()
                    ? await LinuxRfcommSdp.GetChannelAsync(parsed, deadline.Token).ConfigureAwait(false) : channel;
                return await RfcommStream.ConnectAsync(parsed, resolved, deadline.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested && deadline.IsCancellationRequested)
            {
                throw new TimeoutException(Resources.Get("BluetoothConnectionTimeout"), ex);
            }
        };
    }
}
#endif
