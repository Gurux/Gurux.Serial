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
#if IOS || MACCATALYST
namespace Gurux.Serial;

/// <summary>Creates owned CoreBluetooth serial streams for Apple mobile platforms.</summary>
public static partial class BluetoothSerialTransports
{
    /// <summary>Creates an asynchronous serial stream using a CoreBluetooth peripheral UUID.</summary>
    /// <remarks>If the configured service is absent, opening discovers all peripheral services and can use its matching catalogue profile. Existing configured services retain their original characteristics and configuration.</remarks>
    /// <param name="deviceId">The UUID returned by CoreBluetooth discovery, rather than a MAC address.</param>
    /// <param name="profile">The serial service, notification, output and optional configuration characteristics.</param>
    /// <param name="operationTimeout">Maximum opening and complete-write duration; defaults to fifteen seconds.</param>
    /// <param name="receiveCapacity">Maximum queued notifications before the connection fails on overflow.</param>
    /// <returns>A cancellable stream factory for GXSerial.</returns>
    /// <exception cref="ArgumentException">The device identifier is blank or is not a UUID.</exception>
    /// <exception cref="ArgumentNullException">The profile is null.</exception>
    public static Func<SerialPortSettings, CancellationToken, ValueTask<Stream>> BluetoothLe(string deviceId, BluetoothLeProfile profile, TimeSpan? operationTimeout = null, int receiveCapacity = 256)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(deviceId);
        ArgumentNullException.ThrowIfNull(profile);
        if (!Guid.TryParse(deviceId, out _))
        {
            throw new ArgumentException(Properties.Resources.Get("BluetoothLeDeviceNotFound"), nameof(deviceId));
        }
        return async (settings, cancellationToken) => await DesktopBluetoothLeStream.OpenAsync(
            (received, failed, token) => IosBluetoothLeConnection.OpenAsync(deviceId, profile.ForSettings(settings), received, failed, token, settings),
            operationTimeout ?? TimeSpan.FromSeconds(15), receiveCapacity, cancellationToken).ConfigureAwait(false);
    }
}
#endif
