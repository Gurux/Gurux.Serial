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
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Gurux.Serial.Bluetooth.Classic;

/// <summary>Reads remembered and paired Windows Bluetooth Classic devices without initiating a radio inquiry.</summary>
internal static class RfcommDevices
{
    /// <summary>Enumerates the Windows Bluetooth cache on a worker thread.</summary>
    /// <param name="cancellationToken">Cancels before starting or between cached device records.</param>
    /// <returns>The canonical address, reported name and installed service UUIDs of each remembered, authenticated or connected device.</returns>
    /// <exception cref="OperationCanceledException">The cached device enumeration is canceled.</exception>
    /// <exception cref="Win32Exception">Windows rejects device-cache enumeration.</exception>
    internal static Task<BluetoothSerialDevice[]> GetAsync(CancellationToken cancellationToken) => Task.Run(() => Enumerate(cancellationToken), cancellationToken);

    /// <summary>Reads cached device records and always releases the native enumeration handle.</summary>
    /// <param name="cancellationToken">Cancels between native cache operations.</param>
    /// <returns>The unique cached devices with installed service metadata; failed service lookups contribute no service UUIDs.</returns>
    /// <exception cref="OperationCanceledException">The cached device enumeration is canceled.</exception>
    /// <exception cref="Win32Exception">Windows rejects device-cache enumeration.</exception>
    private static BluetoothSerialDevice[] Enumerate(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        byte[] search = new byte[IntPtr.Size == 8 ? 40 : 32];
        BitConverter.GetBytes(search.Length).CopyTo(search, 0);
        // Return authenticated, remembered and connected devices, but do not perform an inquiry.
        BitConverter.GetBytes(1).CopyTo(search, 4);
        BitConverter.GetBytes(1).CopyTo(search, 8);
        BitConverter.GetBytes(1).CopyTo(search, 16);
        byte[] device = new byte[560];
        BitConverter.GetBytes(device.Length).CopyTo(device, 0);
        IntPtr find = BluetoothFindFirstDevice(search, device);
        if (find == IntPtr.Zero)
        {
            int error = Marshal.GetLastPInvokeError();
            if (error == 259)
            {
                return [];
            }
            throw new Win32Exception(error);
        }
        try
        {
            Dictionary<string, BluetoothSerialDevice> result = new(StringComparer.OrdinalIgnoreCase);
            do
            {
                cancellationToken.ThrowIfCancellationRequested();
                string[] bytes = new string[6];
                for (int pos = 0; pos != bytes.Length; ++pos)
                {
                    bytes[pos] = device[13 - pos].ToString("X2", CultureInfo.InvariantCulture);
                }
                string id = string.Join(":", bytes);
                string name = Encoding.Unicode.GetString(device, 64, 496).Split('\0')[0];
                Guid[] services = GetInstalledServices(device, cancellationToken);
                result[id] = new BluetoothSerialDevice(id, string.IsNullOrEmpty(name) ? id : name,
                    BluetoothSerialTransport.Classic, id, services);
                BitConverter.GetBytes(device.Length).CopyTo(device, 0);
            }
            while (BluetoothFindNextDevice(find, device));
            int error = Marshal.GetLastPInvokeError();
            if (error != 259)
            {
                throw new Win32Exception(error);
            }
            cancellationToken.ThrowIfCancellationRequested();
            List<BluetoothSerialDevice> devices = new();
            foreach (KeyValuePair<string, BluetoothSerialDevice> pair in result)
            {
                devices.Add(pair.Value);
            }
            return devices.ToArray();
        }
        finally
        {
            BluetoothFindDeviceClose(find);
        }
    }

    /// <summary>Reads enabled service UUIDs from the existing Windows device record without pairing or opening a connection.</summary>
    /// <param name="device">The cached BLUETOOTH_DEVICE_INFO record being enumerated.</param>
    /// <param name="cancellationToken">Cancels before or between native service metadata queries.</param>
    /// <returns>The complete installed service list, or an empty array if Windows rejects the lookup or the metadata keeps changing.</returns>
    /// <exception cref="OperationCanceledException">The enumeration is canceled.</exception>
    private static Guid[] GetInstalledServices(byte[] device, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        uint count = 0;
        uint status = BluetoothEnumerateInstalledServices(IntPtr.Zero, device, ref count, null);
        if ((status != 0 && status != 234) || count == 0)
        {
            return [];
        }
        for (int attempt = 0; attempt != 3; ++attempt)
        {
            cancellationToken.ThrowIfCancellationRequested();
            // Bound allocation even when a driver supplies an invalid service count.
            if (count == 0 || count > 4096)
            {
                return [];
            }
            var services = new Guid[(int)count];
            status = BluetoothEnumerateInstalledServices(IntPtr.Zero, device, ref count, services);
            if (status == 0 && count <= services.Length)
            {
                Array.Resize(ref services, (int)count);
                return services;
            }
            if (status != 234)
            {
                return [];
            }
        }
        return [];
    }

    /// <summary>Reads the service UUIDs enabled for a cached Bluetooth device across local radios.</summary>
    /// <param name="radio">The local radio handle, or zero to consult all local radios.</param>
    /// <param name="device">The cached BLUETOOTH_DEVICE_INFO record identifying the remote device.</param>
    /// <param name="count">The input buffer capacity and output number of enabled services.</param>
    /// <param name="services">Receives service UUIDs, or null to query the required capacity.</param>
    /// <returns>A Windows error code; zero means complete success and 234 means more buffer space is required.</returns>
    [DllImport("bthprops.cpl")]
    private static extern uint BluetoothEnumerateInstalledServices(IntPtr radio, [In] byte[] device, ref uint count, [Out] Guid[]? services);

    /// <summary>Begins cached Bluetooth device enumeration across all local radios.</summary>
    /// <param name="search">The correctly sized BLUETOOTH_DEVICE_SEARCH_PARAMS buffer.</param>
    /// <param name="device">Receives the first BLUETOOTH_DEVICE_INFO record.</param>
    /// <returns>An owned enumeration handle, or zero with a native error.</returns>
    [DllImport("bthprops.cpl", SetLastError = true)]
    private static extern IntPtr BluetoothFindFirstDevice(byte[] search, [In, Out] byte[] device);

    /// <summary>Advances a cached Bluetooth device enumeration.</summary>
    /// <param name="find">The active device enumeration handle.</param>
    /// <param name="device">Receives the next BLUETOOTH_DEVICE_INFO record.</param>
    /// <returns>Whether another record was found.</returns>
    [DllImport("bthprops.cpl", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool BluetoothFindNextDevice(IntPtr find, [In, Out] byte[] device);

    /// <summary>Releases the cached device enumeration handle.</summary>
    /// <param name="find">The enumeration handle to release.</param>
    /// <returns>Whether Windows accepted the handle.</returns>
    [DllImport("bthprops.cpl", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool BluetoothFindDeviceClose(IntPtr find);
}
#endif
