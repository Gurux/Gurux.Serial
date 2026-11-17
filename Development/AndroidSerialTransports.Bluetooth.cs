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
using Android.Bluetooth;
using Android.Content;
namespace Gurux.Serial;
/// <summary>Provides Android serial device discovery and owned platform transport factories.</summary>
public static partial class AndroidSerialTransports
{
    /// <summary>Creates an owned Bluetooth LE notification stream factory.</summary>
    /// <remarks>The application supplies runtime permissions. UART framing is applied only through an explicitly supplied configuration payload or registered command generator; modem signals have no standard BLE representation. GATT write completion does not acknowledge application-level receipt at the peer.</remarks>
    /// <param name="context">Context used to connect.</param>
    /// <param name="device">Remote Bluetooth LE device.</param>
    /// <param name="profile">Service and characteristic UUIDs and optional configuration.</param>
    /// <param name="operationTimeout">Maximum duration per GATT operation; defaults to 15 seconds.</param>
    /// <param name="receiveCapacity">Maximum queued notification packets; defaults to 256.</param>
    /// <param name="requestedMtu">Requested ATT MTU from 23 through 517; defaults to 247.</param>
    /// <returns>A factory connecting and subscribing when the media opens.</returns>
    /// <exception cref="ArgumentOutOfRangeException">A timeout, capacity or MTU is outside its supported range.</exception>
    public static Func<SerialPortSettings, CancellationToken, ValueTask<Stream>> BluetoothLe(Context context, BluetoothDevice device, BluetoothLeProfile profile, TimeSpan? operationTimeout = null, int receiveCapacity = 256, int requestedMtu = 247)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(device);
        ArgumentNullException.ThrowIfNull(profile);
        TimeSpan timeout = operationTimeout ?? TimeSpan.FromSeconds(15);
        if (timeout <= TimeSpan.Zero || timeout.TotalMilliseconds > int.MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(operationTimeout));
        }
        if (receiveCapacity < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(receiveCapacity));
        }
        if (requestedMtu < 23 || requestedMtu > 517)
        {
            throw new ArgumentOutOfRangeException(nameof(requestedMtu));
        }
        return async (settings, token) =>
        {
            token.ThrowIfCancellationRequested();
            return await BluetoothLeStream.OpenAsync(context, device, profile.ForSettings(settings), timeout, receiveCapacity, requestedMtu, token, settings).ConfigureAwait(false);
        };
    }
}
#endif
