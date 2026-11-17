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
#if WINDOWS
using System.Globalization;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Storage.Streams;

namespace Gurux.Serial;

/// <summary>Opens native Windows GATT serial sessions without depending on a UI framework.</summary>
internal static class WindowsBluetoothLe
{
    /// <summary>Enumerates paired Windows Bluetooth LE devices.</summary>
    /// <param name="cancellationToken">Cancels device enumeration.</param>
    /// <returns>Windows device identifiers, display names and Bluetooth addresses when supplied by Windows.</returns>
    internal static async Task<(string Id, string Name, string? Address)[]> GetDevicesAsync(CancellationToken cancellationToken)
    {
        const string addressProperty = "System.Devices.Aep.DeviceAddress";
        var devices = await DeviceInformation.FindAllAsync(
            BluetoothLEDevice.GetDeviceSelectorFromPairingState(true),
            new[] { addressProperty },
            DeviceInformationKind.AssociationEndpoint).AsTask(cancellationToken).ConfigureAwait(false);
        return devices.Select(it => (
            it.Id,
            it.Name,
            Address: it.Properties.TryGetValue(addressProperty, out var address) ? address as string : null)).ToArray();
    }

    /// <summary>Reads every actual GATT service exposed by one device through an uncached Windows discovery operation.</summary>
    /// <param name="deviceId">A native Windows device identifier or a twelve-digit Bluetooth address with optional colon or hyphen separators.</param>
    /// <param name="cancellationToken">Cancels device access and uncached service discovery.</param>
    /// <returns>Distinct service UUIDs reported by the selected device, including services outside the serial catalogue.</returns>
    /// <remarks>No characteristic values, configuration commands or notification subscriptions are accessed. Native consent and access errors are propagated.</remarks>
    /// <exception cref="IOException">Windows cannot access the selected device or rejects GATT service discovery.</exception>
    /// <exception cref="OperationCanceledException">Device access or service discovery is canceled.</exception>
    internal static async Task<Guid[]> GetServicesAsync(string deviceId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        BluetoothLEDevice? device = null;
        var services = new List<GattDeviceService>();
        try
        {
            string addressText = deviceId.Replace(":", string.Empty).Replace("-", string.Empty);
            device = addressText.Length == 12 && ulong.TryParse(addressText, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out ulong address)
                ? await BluetoothLEDevice.FromBluetoothAddressAsync(address).AsTask(cancellationToken).ConfigureAwait(false)
                : await BluetoothLEDevice.FromIdAsync(deviceId).AsTask(cancellationToken).ConfigureAwait(false);
            if (device == null)
            {
                throw new IOException(Properties.Resources.Get("WindowsBluetoothLeDeviceUnavailable"));
            }
            var result = await device.GetGattServicesAsync(BluetoothCacheMode.Uncached).AsTask(cancellationToken).ConfigureAwait(false);
            services.AddRange(result.Services);
            WindowsBluetoothLeConnection.CheckStatus(result.Status);
            cancellationToken.ThrowIfCancellationRequested();
            return services.Select(service => service.Uuid).Where(uuid => uuid != Guid.Empty).Distinct().ToArray();
        }
        finally
        {
            foreach (var service in services)
            {
                service.Dispose();
            }
            device?.Dispose();
        }
    }

    /// <summary>Opens a serial profile using a Windows device identifier or a hexadecimal Bluetooth MAC address.</summary>
    /// <param name="deviceId">Windows device ID, or a 12-digit MAC address with optional colon or hyphen separators.</param>
    /// <param name="profile">Service and characteristic UUIDs and optional configuration.</param>
    /// <param name="received">Receives notification payloads.</param>
    /// <param name="failed">Receives unexpected disconnect and notification errors.</param>
    /// <param name="cancellationToken">Cancels opening and GATT discovery.</param>
    /// <param name="settings">Serial settings used to resolve configuration commands if discovery requires a catalogue profile; defaults to an unconfigured settings instance.</param>
    /// <returns>An owned connection whose disposal disables notifications and releases native handles.</returns>
    /// <remarks>Windows may require device access consent; applications must arrange any required UI-thread consent before opening from a worker thread. A successful targeted query with no matching service triggers uncached discovery of all services and device-name catalogue fallback; access and communication failures do not trigger fallback.</remarks>
    /// <exception cref="ArgumentNullException">The profile or either notification callback is null.</exception>
    /// <exception cref="IOException">Windows rejects access, discovery, configuration, or subscription.</exception>
    /// <exception cref="OperationCanceledException">Opening or GATT discovery is canceled.</exception>
    internal static async Task<IBluetoothLeConnection> OpenAsync(string deviceId, BluetoothLeProfile profile, Action<byte[]> received, Action<Exception> failed, CancellationToken cancellationToken, SerialPortSettings? settings = null)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(received);
        ArgumentNullException.ThrowIfNull(failed);
        BluetoothLEDevice? device = null;
        WindowsBluetoothLeConnection? connection = null;
        var services = new List<GattDeviceService>();
        try
        {
            string addressText = deviceId.Replace(":", string.Empty).Replace("-", string.Empty);
            device = addressText.Length == 12 && ulong.TryParse(addressText, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out ulong address)
                ? await BluetoothLEDevice.FromBluetoothAddressAsync(address).AsTask(cancellationToken).ConfigureAwait(false)
                : await BluetoothLEDevice.FromIdAsync(deviceId).AsTask(cancellationToken).ConfigureAwait(false);
            if (device == null)
            {
                throw new IOException(Properties.Resources.Get("WindowsBluetoothLeDeviceUnavailable"));
            }
            var result = await device.GetGattServicesForUuidAsync(profile.Service, BluetoothCacheMode.Uncached).AsTask(cancellationToken).ConfigureAwait(false);
            services.AddRange(result.Services);
            WindowsBluetoothLeConnection.CheckStatus(result.Status);
            if (services.Count == 0)
            {
                var available = await device.GetGattServicesAsync(BluetoothCacheMode.Uncached).AsTask(cancellationToken).ConfigureAwait(false);
                services.AddRange(available.Services);
                WindowsBluetoothLeConnection.CheckStatus(available.Status);
                string? deviceName = device.Name;
                string deviceAddress = device.BluetoothAddress.ToString("X12", CultureInfo.InvariantCulture);
                if (string.IsNullOrWhiteSpace(deviceName)
                    || string.Equals(deviceName, deviceId, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(deviceName.Replace(":", string.Empty).Replace("-", string.Empty), deviceAddress, StringComparison.OrdinalIgnoreCase))
                {
                    string? advertisedName = BluetoothLeDiscovery.GetKnownDevices().FirstOrDefault(known =>
                        string.Equals(known.Id, deviceId, StringComparison.OrdinalIgnoreCase)
                        || string.Equals(known.Id.Replace(":", string.Empty).Replace("-", string.Empty), deviceAddress, StringComparison.OrdinalIgnoreCase)
                        || string.Equals(known.Address?.Replace(":", string.Empty).Replace("-", string.Empty), deviceAddress, StringComparison.OrdinalIgnoreCase))?.Name;
                    if (!string.IsNullOrWhiteSpace(advertisedName))
                    {
                        deviceName = advertisedName;
                    }
                }
                profile = BluetoothLeServiceSelection.SelectProfile(profile, deviceName, services.Select(item => item.Uuid), settings ?? new SerialPortSettings(), deviceId);
            }
            var service = services.First(item => item.Uuid == profile.Service);
            var read = await GetCharacteristicAsync(service, profile.ReadCharacteristic, cancellationToken).ConfigureAwait(false);
            var write = await GetCharacteristicAsync(service, profile.WriteCharacteristic, cancellationToken).ConfigureAwait(false);
            connection = new WindowsBluetoothLeConnection(device, services, read, write, received, failed);
            device = null;
            services = new List<GattDeviceService>();
            await connection.InitializeAsync(profile, cancellationToken).ConfigureAwait(false);
            return connection;
        }
        catch
        {
            if (connection != null)
            {
                await connection.DisposeAsync().ConfigureAwait(false);
            }
            foreach (var service in services)
            {
                service.Dispose();
            }
            device?.Dispose();
            throw;
        }
    }

    /// <summary>Finds a required characteristic within the selected service.</summary>
    /// <param name="service">Service to inspect.</param>
    /// <param name="uuid">Required characteristic UUID.</param>
    /// <param name="cancellationToken">Cancels discovery.</param>
    /// <returns>The first matching characteristic.</returns>
    /// <exception cref="IOException">Discovery fails or no matching characteristic exists.</exception>
    internal static async Task<GattCharacteristic> GetCharacteristicAsync(GattDeviceService service, Guid uuid, CancellationToken cancellationToken)
    {
        var result = await service.GetCharacteristicsForUuidAsync(uuid, BluetoothCacheMode.Uncached).AsTask(cancellationToken).ConfigureAwait(false);
        WindowsBluetoothLeConnection.CheckStatus(result.Status);
        if (result.Characteristics.Count == 0)
        {
            throw new IOException(Properties.Resources.Get("BluetoothLeCharacteristicNotFound"));
        }
        return result.Characteristics[0];
    }
}
#endif
