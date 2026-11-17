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
using Android.Content.PM;
using Android.Hardware.Usb;

namespace Gurux.Serial;

/// <summary>Provides Android serial device discovery and owned platform transport factories.</summary>
public static partial class AndroidSerialTransports
{
    /// <summary>Enumerates USB host devices and paired Bluetooth devices without requesting permissions.</summary>
    /// <remarks>USB identifiers use usb: and Bluetooth identifiers use bluetooth:. Bluetooth devices are omitted
    /// until the application has BLUETOOTH_CONNECT permission on Android 12 or later. Unknown USB chipsets may
    /// appear in this discovery list and are validated by the selected transport when opened.</remarks>
    /// <param name="context">The context providing Android USB and Bluetooth services.</param>
    /// <returns>A sorted snapshot suitable for serial device selection.</returns>
    /// <exception cref="ArgumentNullException">The context is null.</exception>
    public static string[] GetPortNames(Context context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var names = new List<string>();
        var usb = context.GetSystemService(Context.UsbService) as UsbManager;
        if (usb?.DeviceList != null)
        {
            names.AddRange(usb.DeviceList.Values.Select(device => "usb:" + device.DeviceName));
        }
        string permission = OperatingSystem.IsAndroidVersionAtLeast(31)
            ? Android.Manifest.Permission.BluetoothConnect : Android.Manifest.Permission.Bluetooth;
        bool bluetoothAllowed = context.CheckCallingOrSelfPermission(permission) == Permission.Granted;
        if (bluetoothAllowed)
        {
            var manager = context.GetSystemService(Context.BluetoothService) as BluetoothManager;
            if (manager?.Adapter is { IsEnabled: true } adapter && adapter.BondedDevices != null)
            {
                names.AddRange(adapter.BondedDevices.Select(device => "bluetooth:" + device.Address));
            }
        }
        names.AddRange(BluetoothLeDiscovery.GetKnownDevices().Select(device => "ble:" + device.Id));
        return names.Distinct(StringComparer.Ordinal).OrderBy(name => name, StringComparer.Ordinal).ToArray();
    }
}
#endif
