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
using Tmds.DBus;
namespace Gurux.Serial;

/// <summary>Discovers nearby BLE devices through a dedicated BlueZ client session.</summary>
internal static class LinuxBluetoothLeDiscovery
{
    /// <summary>Starts LE discovery on a powered adapter and reports device snapshots until cancellation.</summary>
    /// <param name="discovered">Receives discovered native paths, names, Bluetooth addresses and advertised service UUID metadata.</param>
    /// <param name="cancellationToken">Ends discovery and releases this client's session.</param>
    /// <returns>The asynchronous scan lifetime.</returns>
    /// <remarks>BlueZ merges discovery filters from all clients. Devices with Classic class metadata require separate LE advertising evidence; unclassified RSSI results rely on the requested LE filter.</remarks>
    /// <exception cref="IOException">BlueZ is unavailable, rejects discovery or loses the adapter.</exception>
    /// <exception cref="OperationCanceledException">The scan is canceled.</exception>
    internal static async Task ScanAsync(Action<BluetoothSerialDevice> discovered, CancellationToken cancellationToken)
    {
        using var bus = new Connection(Address.System);
        // Disposing this dedicated client also releases a StartDiscovery whose reply was canceled.
        using var cancellation = cancellationToken.Register(bus.Dispose);
        IBlueZAdapter? adapter = null;
        bool started = false;
        try
        {
            await bus.ConnectAsync().WaitAsync(cancellationToken).ConfigureAwait(false);
            var manager = bus.CreateProxy<IBlueZObjectManager>("org.bluez", "/");
            var objects = await manager.GetManagedObjectsAsync().WaitAsync(cancellationToken).ConfigureAwait(false);
            var selected = objects.FirstOrDefault(item => item.Value.TryGetValue("org.bluez.Adapter1", out var properties) && properties.TryGetValue("Powered", out var powered) && powered is true);
            if (selected.Value == null)
            {
                throw new IOException(Properties.Resources.Get("BluetoothLeAdapterUnavailable"));
            }
            adapter = bus.CreateProxy<IBlueZAdapter>("org.bluez", selected.Key);
            await adapter.SetDiscoveryFilterAsync(new Dictionary<string, object> { ["Transport"] = "le", ["DuplicateData"] = false }).WaitAsync(cancellationToken).ConfigureAwait(false);
            await adapter.StartDiscoveryAsync().WaitAsync(cancellationToken).ConfigureAwait(false);
            started = true;
            var reported = new Dictionary<string, BluetoothSerialDevice>(StringComparer.Ordinal);
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                objects = await manager.GetManagedObjectsAsync().WaitAsync(cancellationToken).ConfigureAwait(false);
                if (!objects.TryGetValue(selected.Key, out var adapterInterfaces) || !adapterInterfaces.TryGetValue("org.bluez.Adapter1", out var adapterProperties) || !adapterProperties.TryGetValue("Powered", out var powered) || powered is not true)
                {
                    throw new IOException(Properties.Resources.Get("BluetoothLeAdapterUnavailable"));
                }
                foreach (var item in objects)
                {
                    if (!item.Value.TryGetValue("org.bluez.Device1", out var properties) || !properties.TryGetValue("Adapter", out var owner) || owner is not ObjectPath path || !path.Equals(selected.Key) || !IsLowEnergyCandidate(properties))
                    {
                        continue;
                    }
                    string id = item.Key.ToString();
                    string? address = properties.TryGetValue("Address", out var addressValue) ? addressValue as string : null;
                    string name = properties.TryGetValue("Name", out var nameValue) && nameValue is string text && !string.IsNullOrWhiteSpace(text) ? text : id;
                    Guid[] serviceUuids = LinuxBluetoothLe.GetServiceUuids(properties);
                    if (reported.TryGetValue(id, out var previous))
                    {
                        if (previous.Name == name && string.Equals(previous.Address, address, StringComparison.OrdinalIgnoreCase)
                            && previous.ServiceUuids.OrderBy(uuid => uuid).SequenceEqual(serviceUuids.OrderBy(uuid => uuid)))
                        {
                            continue;
                        }
                    }
                    else if (reported.Count >= 256)
                    {
                        continue;
                    }
                    var device = new BluetoothSerialDevice(id, name, BluetoothSerialTransport.LowEnergy, address, serviceUuids);
                    reported[id] = device;
                    discovered(device);
                }
                await Task.Delay(250, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (Exception error) when (error is not OperationCanceledException)
        {
            cancellationToken.ThrowIfCancellationRequested();
            throw new IOException(Properties.Resources.Get("BluetoothLeScanFailed"), error);
        }
        finally
        {
            if (started && adapter != null)
            {
                try
                {
                    await adapter.StopDiscoveryAsync().WaitAsync(TimeSpan.FromSeconds(2)).ConfigureAwait(false);
                }
                catch (Exception)
                {
                    // Disconnect releases only this client's discovery ownership if the bus is already closed.
                }
            }
        }
    }
    /// <summary>Rejects cached devices and identifiable Classic-only inquiry results.</summary>
    /// <param name="properties">The BlueZ device properties from the current adapter snapshot.</param>
    /// <returns>Whether the result contains RSSI and is consistent with the requested LE discovery transport.</returns>
    private static bool IsLowEnergyCandidate(IDictionary<string, object> properties)
    {
        if (!properties.ContainsKey("RSSI"))
        {
            return false;
        }
        bool advertisingEvidence = properties.ContainsKey("AdvertisingFlags") || properties.ContainsKey("AdvertisingData") || properties.ContainsKey("ServiceData") ||
            (properties.TryGetValue("AddressType", out var addressType) && addressType is "random");
        return advertisingEvidence || !properties.ContainsKey("Class");
    }
}
#endif
