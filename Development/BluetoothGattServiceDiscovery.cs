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
namespace Gurux.Serial;

/// <summary>Reads all actual GATT service UUIDs through a temporary metadata-only native session.</summary>
public static class BluetoothGattServiceDiscovery
{
    /// <summary>Gets whether the current target includes a native GATT metadata backend.</summary>
    public static bool IsSupported => BluetoothLeDiscovery.IsSupported;

    /// <summary>Connects to one selected BLE device and returns its complete discovered service UUID list.</summary>
    /// <param name="deviceId">The selected Windows native identifier, Bluetooth address, BlueZ path or CoreBluetooth UUID.</param>
    /// <param name="cancellationToken">Cancels discovery and awaits native cleanup.</param>
    /// <returns>A distinct ordered snapshot of actual services, independent of advertisement UUIDs or the device catalogue.</returns>
    /// <remarks>This explicit operation may establish a temporary Bluetooth connection. It does not read characteristics, configure UART parameters, write data or subscribe to notifications. Native permissions and consent remain the application's responsibility.</remarks>
    /// <exception cref="ArgumentException">The device identity is empty.</exception>
    /// <exception cref="PlatformNotSupportedException">This target has no native GATT backend.</exception>
    /// <exception cref="IOException">Native discovery or connection access fails.</exception>
    /// <exception cref="TimeoutException">Discovery exceeds fifteen seconds.</exception>
    /// <exception cref="OperationCanceledException">The caller cancels discovery.</exception>
    public static async Task<Guid[]> GetServicesAsync(string deviceId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(deviceId);
        cancellationToken.ThrowIfCancellationRequested();
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(TimeSpan.FromSeconds(15));
        try
        {
            Guid[] services = await ReadNativeAsync(deviceId, deadline.Token).ConfigureAwait(false);
            return services.Where(uuid => uuid != Guid.Empty).Distinct().OrderBy(uuid => uuid).ToArray();
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested && deadline.IsCancellationRequested)
        {
            throw new TimeoutException(Properties.Resources.Get("BluetoothGattServiceReadTimedOut"));
        }
    }

    /// <summary>Routes metadata discovery to the platform backend without creating a serial stream.</summary>
    /// <param name="deviceId">The selected native identity.</param>
    /// <param name="cancellationToken">The bounded discovery lifetime.</param>
    /// <returns>The actual native service UUIDs after native cleanup.</returns>
    /// <exception cref="PlatformNotSupportedException">No native reader is included for this target.</exception>
    private static Task<Guid[]> ReadNativeAsync(string deviceId, CancellationToken cancellationToken)
    {
#if ANDROID
        return AndroidBluetoothGattServiceDiscovery.GetServicesAsync(deviceId, cancellationToken);
#elif IOS || MACCATALYST
        return IosBluetoothGattServiceDiscovery.GetServicesAsync(deviceId, cancellationToken);
#else
#if WINDOWS
        if (OperatingSystem.IsWindows())
        {
            return WindowsBluetoothLe.GetServicesAsync(deviceId, cancellationToken);
        }
#endif
        if (OperatingSystem.IsLinux())
        {
            return LinuxBluetoothLe.GetServicesAsync(deviceId, cancellationToken);
        }
        throw new PlatformNotSupportedException(Properties.Resources.Get("DesktopBluetoothTargetRequired"));
#endif
    }
}