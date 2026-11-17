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
using System.Globalization;
namespace Gurux.Serial;

/// <summary>Selects an available native service while preserving explicit profiles and matching device catalogue fallbacks.</summary>
internal static class BluetoothLeServiceSelection
{
    /// <summary>Uses the preferred profile when available, otherwise resolves the named device's catalogue service.</summary>
    /// <param name="preferred">The already configured explicit opening profile.</param>
    /// <param name="deviceName">The device's advertised or operating-system name used for catalogue matching.</param>
    /// <param name="availableServices">Services actually discovered on the selected device.</param>
    /// <param name="settings">Current serial settings used to evaluate catalogue configuration commands.</param>
    /// <param name="deviceId">Optional selected native identity used to recover its recently advertised name.</param>
    /// <returns>The original profile or a complete catalogue profile configured for this opening.</returns>
    /// <exception cref="ArgumentNullException">A required argument is null.</exception>
    /// <exception cref="IOException">Neither the configured service nor a matching catalogue service is offered by the device.</exception>
    /// <exception cref="InvalidDataException">The matching catalogue entry is invalid.</exception>
    /// <exception cref="NotSupportedException">The fallback profile names a configuration generator that is not registered.</exception>
    /// <exception cref="ArgumentException">The fallback configuration exceeds the GATT payload limit.</exception>
    internal static BluetoothLeProfile SelectProfile(BluetoothLeProfile preferred, string? deviceName, IEnumerable<Guid> availableServices, SerialPortSettings settings, string? deviceId = null)
    {
        ArgumentNullException.ThrowIfNull(preferred);
        ArgumentNullException.ThrowIfNull(availableServices);
        ArgumentNullException.ThrowIfNull(settings);
        var available = availableServices.ToHashSet();
        if (available.Contains(preferred.Service))
        {
            return preferred;
        }
        BluetoothLeProfile? catalogue = BluetoothLeProfile.FromDeviceName(deviceName);
        if (catalogue is null && !string.IsNullOrWhiteSpace(deviceId))
        {
            bool peripheralId = Guid.TryParse(deviceId, out Guid selectedId);
            catalogue = BluetoothLeDiscovery.GetKnownDevices().FirstOrDefault(device =>
                string.Equals(device.Id, deviceId, StringComparison.OrdinalIgnoreCase)
                || string.Equals(device.Address, deviceId, StringComparison.OrdinalIgnoreCase)
                || (peripheralId && Guid.TryParse(device.Id, out Guid id) && id == selectedId))?.Profile;
        }
        if (catalogue is not null && available.Contains(catalogue.Service))
        {
            return catalogue.ForSettings(settings);
        }
        throw new IOException(string.Format(CultureInfo.CurrentCulture,
            Properties.Resources.Get("BluetoothLeServiceNotFoundDetails"), preferred.Service,
            catalogue?.Service.ToString() ?? "-", string.Join(", ", available.OrderBy(value => value))));
    }
}