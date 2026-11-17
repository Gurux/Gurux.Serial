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
using CoreBluetooth;
using CoreFoundation;
using Foundation;

namespace Gurux.Serial;

/// <summary>Discovers advertising CoreBluetooth peripherals without requiring prior pairing.</summary>
internal static class IosBluetoothLeDiscovery
{
    /// <summary>Scans until cancellation and releases the native scan before returning.</summary>
    /// <param name="discovered">Receives native UUID identifiers, advertised names and advertised or service-data UUIDs.</param>
    /// <param name="cancellationToken">Stops discovery.</param>
    /// <returns>The running scan operation.</returns>
    /// <exception cref="IOException">Bluetooth is unavailable or permission is denied.</exception>
    internal static async Task ScanAsync(Action<BluetoothSerialDevice> discovered, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(discovered);
        cancellationToken.ThrowIfCancellationRequested();
        using var queue = new DispatchQueue("fi.gurux.serial.ble.discovery");
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        CBCentralManager? manager = null;
        await DispatchAsync(queue, () =>
        {
            manager = new CBCentralManager(null, queue);
            manager.UpdatedState += (_, _) =>
            {
                if (manager.State == CBManagerState.PoweredOn)
                {
                    manager.ScanForPeripherals((CBUUID[]?)null, (NSDictionary?)null);
                }
                else if (manager.State != CBManagerState.Unknown && manager.State != CBManagerState.Resetting)
                {
                    completion.TrySetException(new IOException(Properties.Resources.Get("BluetoothUnavailable")));
                }
            };
            manager.DiscoveredPeripheral += (_, args) =>
            {
                try
                {
                    string id = args.Peripheral.Identifier.AsString();
                    var advertisement = new AdvertisementData(args.AdvertisementData);
                    string? name = advertisement.LocalName;
                    name = string.IsNullOrWhiteSpace(name) ? args.Peripheral.Name : name;
                    Guid[] serviceUuids = (advertisement.ServiceUuids ?? Array.Empty<CBUUID>())
                        .Concat(advertisement.OverflowServiceUuids ?? Array.Empty<CBUUID>())
                        .Concat(advertisement.ServiceData?.Keys.OfType<CBUUID>() ?? Enumerable.Empty<CBUUID>())
                        .Select(value => Guid.TryParse(value.ToString(true), out Guid uuid) ? uuid : Guid.Empty)
                        .Where(uuid => uuid != Guid.Empty).Distinct().ToArray();
                    discovered(new BluetoothSerialDevice(id, string.IsNullOrWhiteSpace(name) ? id : name, BluetoothSerialTransport.LowEnergy, null, serviceUuids));
                }
                catch (Exception error)
                {
                    completion.TrySetException(error);
                }
            };
        }).ConfigureAwait(false);
        try
        {
            await completion.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            await DispatchAsync(queue, () =>
            {
                manager!.StopScan();
                manager.Dispose();
            }).ConfigureAwait(false);
        }
    }

    /// <summary>Runs native work on its owning serial queue and observes synchronous failures.</summary>
    /// <param name="queue">The owning CoreBluetooth queue.</param>
    /// <param name="action">The work to execute.</param>
    /// <returns>Completion after the action runs.</returns>
    internal static Task DispatchAsync(DispatchQueue queue, Action action)
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        queue.DispatchAsync(() =>
        {
            try
            {
                action();
                completion.TrySetResult();
            }
            catch (Exception error)
            {
                completion.TrySetException(error);
            }
        });
        return completion.Task;
    }
}
#endif
