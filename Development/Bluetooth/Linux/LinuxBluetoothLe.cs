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

/// <summary>Connects BLE serial profiles through the Linux BlueZ system service.</summary>
internal static class LinuxBluetoothLe
{
    /// <summary>Names the BlueZ remote device interface.</summary>
    private const string DeviceInterface = "org.bluez.Device1";
    /// <summary>Names the BlueZ remote GATT service interface.</summary>
    private const string ServiceInterface = "org.bluez.GattService1";
    /// <summary>Names the BlueZ remote GATT characteristic interface.</summary>
    private const string CharacteristicInterface = "org.bluez.GattCharacteristic1";
    /// <summary>Opens a dedicated bus connection and resolves the preferred profile or a named catalogue profile when its service is absent.</summary>
    /// <param name="deviceId">A known BlueZ device path or Bluetooth address.</param>
    /// <param name="profile">The service and characteristic selection.</param>
    /// <param name="received">Receives notification payloads.</param>
    /// <param name="failed">Receives connection failures.</param>
    /// <param name="cancellationToken">Cancels opening and outstanding bus operations.</param>
    /// <param name="settings">UART settings used to evaluate a fallback catalogue profile, or null to use default UART settings.</param>
    /// <returns>The connected BLE session.</returns>
    /// <exception cref="IOException">The selected device, matching serial service or required characteristic is unavailable.</exception>
    /// <exception cref="OperationCanceledException">Opening is canceled before the session becomes active.</exception>
    internal static async Task<IBluetoothLeConnection> OpenAsync(string deviceId, BluetoothLeProfile profile, Action<byte[]> received, Action<Exception> failed, CancellationToken cancellationToken, SerialPortSettings? settings = null)
    {
        var session = new Session(received, failed);
        try
        {
            using var cancellation = cancellationToken.Register(session.Abort);
            await session.Bus.ConnectAsync().WaitAsync(cancellationToken).ConfigureAwait(false);
            session.ServiceWatch = await session.Bus.ResolveServiceOwnerAsync("org.bluez", change =>
            {
                if (string.IsNullOrEmpty(change.NewOwner) || !string.IsNullOrEmpty(change.OldOwner))
                {
                    session.ReportFailure(Error("BluetoothLeDisconnected"));
                }
            }, session.ReportFailure).WaitAsync(cancellationToken).ConfigureAwait(false);
            var manager = session.Bus.CreateProxy<IBlueZObjectManager>("org.bluez", "/");
            var objects = await manager.GetManagedObjectsAsync().WaitAsync(cancellationToken).ConfigureAwait(false);
            var device = objects.FirstOrDefault(item => item.Value.TryGetValue(DeviceInterface, out var properties) &&
                (string.Equals(item.Key.ToString(), deviceId, StringComparison.Ordinal) ||
                (properties.TryGetValue("Address", out var address) && string.Equals(address as string, deviceId, StringComparison.OrdinalIgnoreCase))));
            if (device.Value == null)
            {
                throw Error("BluetoothLeDeviceNotFound");
            }
            session.Device = session.Bus.CreateProxy<IBlueZDevice>("org.bluez", device.Key);
            session.DeviceWatch = await session.Device.WatchPropertiesAsync(changes =>
            {
                if (changes.Changed.Any(item => item.Key == "Connected" && item.Value is false))
                {
                    session.ReportFailure(Error("BluetoothLeDisconnected"));
                }
            }, session.ReportFailure).WaitAsync(cancellationToken).ConfigureAwait(false);
            bool connected = device.Value[DeviceInterface].TryGetValue("Connected", out var value) && value is true;
            if (!connected)
            {
                session.OwnsConnection = true;
                session.ConnectOperation = session.Device.ConnectAsync();
                await session.ConnectOperation.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
            while (true)
            {
                var properties = await session.Device.GetAllAsync().WaitAsync(cancellationToken).ConfigureAwait(false);
                if (properties.TryGetValue("ServicesResolved", out var resolved) && resolved is true)
                {
                    break;
                }
                await Task.Delay(100, cancellationToken).ConfigureAwait(false);
            }
            objects = await manager.GetManagedObjectsAsync().WaitAsync(cancellationToken).ConfigureAwait(false);
            var ownServices = objects.Where(item => item.Value.TryGetValue(ServiceInterface, out var properties) &&
                properties.TryGetValue("Device", out var owner) && owner is ObjectPath path && path.Equals(device.Key)).ToArray();
            var deviceProperties = objects.TryGetValue(device.Key, out var latestDevice) && latestDevice.TryGetValue(DeviceInterface, out var latestProperties)
                ? latestProperties : device.Value[DeviceInterface];
            string? deviceName = deviceProperties.TryGetValue("Name", out var name) && name is string advertised && !string.IsNullOrWhiteSpace(advertised)
                ? advertised : deviceProperties.TryGetValue("Alias", out var alias) ? alias as string : null;
            Guid[] availableServices = ownServices.Select(item =>
                item.Value[ServiceInterface].TryGetValue("UUID", out var uuid) && uuid is string text && Guid.TryParse(text, out Guid parsed)
                    ? parsed : Guid.Empty).Where(uuid => uuid != Guid.Empty).ToArray();
            cancellationToken.ThrowIfCancellationRequested();
            profile = BluetoothLeServiceSelection.SelectProfile(profile, deviceName, availableServices, settings ?? new SerialPortSettings(), deviceId);
            var service = ownServices.FirstOrDefault(item => Matches(item.Value[ServiceInterface], profile.Service));
            if (service.Value == null)
            {
                throw Error("BluetoothLeServiceNotFound");
            }
            var read = FindCharacteristic(objects, service.Key, profile.ReadCharacteristic);
            var write = FindCharacteristic(objects, service.Key, profile.WriteCharacteristic);
            var readFlags = (string[])read.Value[CharacteristicInterface]["Flags"];
            if (!readFlags.Contains("notify") && !readFlags.Contains("indicate"))
            {
                throw Error("BluetoothLeNotificationsNotSupported");
            }
            session.WriteType = WriteType(write.Value[CharacteristicInterface]);
            session.Output = session.Bus.CreateProxy<IBlueZCharacteristic>("org.bluez", write.Key);
            session.Input = session.Bus.CreateProxy<IBlueZCharacteristic>("org.bluez", read.Key);
            session.ValueWatch = await session.Input.WatchPropertiesAsync(changes =>
            {
                foreach (var property in changes.Changed)
                {
                    if (property.Key == "Value" && property.Value is byte[] bytes && bytes.Length != 0)
                    {
                        session.Receive(bytes);
                    }
                }
            }, session.ReportFailure).WaitAsync(cancellationToken).ConfigureAwait(false);
            await session.Input.StartNotifyAsync().WaitAsync(cancellationToken).ConfigureAwait(false);
            session.Notifying = true;
            if (profile.ConfigurationCharacteristic is Guid configuration && profile.ConfigurationValue is byte[] configurationValue)
            {
                var target = FindCharacteristic(objects, service.Key, configuration);
                var proxy = session.Bus.CreateProxy<IBlueZCharacteristic>("org.bluez", target.Key);
                string type = WriteType(target.Value[CharacteristicInterface]);
                for (int offset = 0; offset < configurationValue.Length; offset += session.MaxWriteSize)
                {
                    await proxy.WriteValueAsync(configurationValue.AsSpan(offset, Math.Min(session.MaxWriteSize, configurationValue.Length - offset)).ToArray(), new Dictionary<string, object> { ["type"] = type }).WaitAsync(cancellationToken).ConfigureAwait(false);
                }
            }
            cancellationToken.ThrowIfCancellationRequested();
            session.Activate();
            return session;
        }
        catch
        {
            await session.DisposeAsync().ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            throw;
        }
    }

    /// <summary>Lists devices already discovered by BlueZ without starting a system-wide discovery session.</summary>
    /// <param name="cancellationToken">Cancels the bus query.</param>
    /// <param name="transport">The transport requested by the caller; enumeration does not initiate discovery or a connection.</param>
    /// <returns>Known Bluetooth addresses, names and service UUID metadata.</returns>
    internal static async Task<BluetoothSerialDevice[]> GetDevicesAsync(CancellationToken cancellationToken, BluetoothSerialTransport transport = BluetoothSerialTransport.Classic)
    {
        using var bus = new Connection(Address.System);
        using var cancellation = cancellationToken.Register(bus.Dispose);
        await bus.ConnectAsync().WaitAsync(cancellationToken).ConfigureAwait(false);
        var objects = await bus.CreateProxy<IBlueZObjectManager>("org.bluez", "/").GetManagedObjectsAsync().WaitAsync(cancellationToken).ConfigureAwait(false);
        return MapDevices(objects).Select(device => new BluetoothSerialDevice(device.Id, device.Name, transport, device.Address, device.ServiceUuids)).ToArray();
    }

    /// <summary>Resolves all actual GATT services belonging to one selected BlueZ device without reading characteristic values.</summary>
    /// <param name="deviceId">The exact BlueZ device object path or its Bluetooth address.</param>
    /// <param name="cancellationToken">Cancels connection establishment, service resolution and managed-object queries.</param>
    /// <returns>Distinct UUIDs from the selected device's resolved GATT service objects, without catalogue filtering.</returns>
    /// <remarks>A dedicated bus is always released. An existing device connection is preserved; only a connection started by this operation is disconnected, including canceled or pending connection attempts. Pairing, serial configuration and notifications are not requested.</remarks>
    /// <exception cref="IOException">The selected device is not known to BlueZ.</exception>
    /// <exception cref="DBusException">BlueZ rejects connection or service-property access.</exception>
    /// <exception cref="OperationCanceledException">Connection or service resolution is canceled.</exception>
    internal static async Task<Guid[]> GetServicesAsync(string deviceId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var session = new Session(_ => { }, _ => { });
        try
        {
            using var cancellation = cancellationToken.Register(session.Abort);
            await session.Bus.ConnectAsync().WaitAsync(cancellationToken).ConfigureAwait(false);
            var manager = session.Bus.CreateProxy<IBlueZObjectManager>("org.bluez", "/");
            var objects = await manager.GetManagedObjectsAsync().WaitAsync(cancellationToken).ConfigureAwait(false);
            var device = objects.FirstOrDefault(item => item.Value.TryGetValue(DeviceInterface, out var deviceProperties)
                && (string.Equals(item.Key.ToString(), deviceId, StringComparison.Ordinal)
                    || (deviceProperties.TryGetValue("Address", out var address) && string.Equals(address as string, deviceId, StringComparison.OrdinalIgnoreCase))));
            if (device.Value == null)
            {
                throw Error("BluetoothLeDeviceNotFound");
            }
            session.Device = session.Bus.CreateProxy<IBlueZDevice>("org.bluez", device.Key);
            var properties = await session.Device.GetAllAsync().WaitAsync(cancellationToken).ConfigureAwait(false);
            bool connected = properties.TryGetValue("Connected", out var state) && state is true;
            if (!connected)
            {
                session.OwnsConnection = true;
                session.ConnectOperation = session.Device.ConnectAsync();
                await session.ConnectOperation.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
            while (true)
            {
                properties = await session.Device.GetAllAsync().WaitAsync(cancellationToken).ConfigureAwait(false);
                if (properties.TryGetValue("ServicesResolved", out var resolved) && resolved is true)
                {
                    break;
                }
                await Task.Delay(100, cancellationToken).ConfigureAwait(false);
            }
            objects = await manager.GetManagedObjectsAsync().WaitAsync(cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            return GetResolvedServices(objects, device.Key);
        }
        catch
        {
            cancellationToken.ThrowIfCancellationRequested();
            throw;
        }
        finally
        {
            await session.DisposeAsync().ConfigureAwait(false);
        }
    }

    /// <summary>Reads UUIDs only from resolved service objects owned by the selected BlueZ device.</summary>
    /// <param name="objects">The managed-object snapshot obtained after service resolution.</param>
    /// <param name="device">The selected device's exact native object path.</param>
    /// <returns>Distinct nonempty service UUIDs, excluding advertisements, characteristic UUIDs and services belonging to other devices.</returns>
    internal static Guid[] GetResolvedServices(IDictionary<ObjectPath, IDictionary<string, IDictionary<string, object>>> objects, ObjectPath device)
    {
        return objects.Where(item => item.Value.TryGetValue(ServiceInterface, out var properties)
            && properties.TryGetValue("Device", out var owner) && owner is ObjectPath path && path.Equals(device))
            .Select(item => item.Value[ServiceInterface].TryGetValue("UUID", out var uuid)
                && uuid is string text && Guid.TryParse(text, out Guid parsed) ? parsed : Guid.Empty)
            .Where(uuid => uuid != Guid.Empty).Distinct().ToArray();
    }

    /// <summary>Maps BlueZ objects to transport addresses shared by Classic and BLE.</summary>
    /// <param name="objects">The BlueZ managed object snapshot.</param>
    /// <returns>Known Bluetooth addresses with their aliases and service UUIDs, deduplicated across adapters and tagged as Classic until the caller selects a transport.</returns>
    private static BluetoothSerialDevice[] MapDevices(IDictionary<ObjectPath, IDictionary<string, IDictionary<string, object>>> objects)
    {
        return objects.Where(item => item.Value.TryGetValue(DeviceInterface, out var properties) && properties.TryGetValue("Address", out var address) && address is string text && !string.IsNullOrEmpty(text)).Select(item =>
        {
            var properties = item.Value[DeviceInterface];
            string address = (string)properties["Address"];
            string name = properties.TryGetValue("Alias", out var alias) && alias is string text ? text : address;
            return new BluetoothSerialDevice(address, name, BluetoothSerialTransport.Classic, address, GetServiceUuids(properties));
        }).DistinctBy(item => item.Id, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    /// <summary>Reads valid nonempty service UUIDs from optional BlueZ device metadata without rejecting malformed entries.</summary>
    /// <param name="properties">The BlueZ device properties.</param>
    /// <returns>Distinct parsed service identifiers, or an empty array when metadata is absent or has an unexpected type.</returns>
    internal static Guid[] GetServiceUuids(IDictionary<string, object> properties)
    {
        if (!properties.TryGetValue("UUIDs", out var value) || value is not IEnumerable<string> values)
        {
            return Array.Empty<Guid>();
        }
        return values.Select(text => Guid.TryParse(text, out Guid uuid) ? uuid : Guid.Empty)
            .Where(uuid => uuid != Guid.Empty).Distinct().ToArray();
    }
    /// <summary>Checks an explicit BlueZ device path without connecting to it or substituting another adapter.</summary>
    /// <param name="devicePath">The exact native device object path.</param>
    /// <param name="cancellationToken">Cancels the system-bus query.</param>
    /// <returns>Whether the BlueZ snapshot contains that device object.</returns>
    internal static async Task<bool> IsDeviceKnownAsync(string devicePath, CancellationToken cancellationToken)
    {
        using var bus = new Connection(Address.System);
        using var cancellation = cancellationToken.Register(bus.Dispose);
        await bus.ConnectAsync().WaitAsync(cancellationToken).ConfigureAwait(false);
        var manager = bus.CreateProxy<IBlueZObjectManager>("org.bluez", "/");
        var objects = await manager.GetManagedObjectsAsync().WaitAsync(cancellationToken).ConfigureAwait(false);
        return objects.Any(item => string.Equals(item.Key.ToString(), devicePath, StringComparison.Ordinal) && item.Value.ContainsKey(DeviceInterface));
    }

    /// <summary>Finds a characteristic in the selected service, rejecting UUIDs in other services.</summary>
    /// <param name="objects">The managed object snapshot.</param>
    /// <param name="service">The owning service path.</param>
    /// <param name="uuid">The required characteristic UUID.</param>
    /// <returns>The matching characteristic object.</returns>
    private static KeyValuePair<ObjectPath, IDictionary<string, IDictionary<string, object>>> FindCharacteristic(IDictionary<ObjectPath, IDictionary<string, IDictionary<string, object>>> objects, ObjectPath service, Guid uuid)
    {
        var result = objects.FirstOrDefault(item => item.Value.TryGetValue(CharacteristicInterface, out var properties) && properties.TryGetValue("Service", out var owner) && owner is ObjectPath path && path.Equals(service) && Matches(properties, uuid));
        if (result.Value == null)
        {
            throw Error("BluetoothLeCharacteristicNotFound");
        }
        return result;
    }

    /// <summary>Compares a BlueZ UUID property with a configured UUID.</summary>
    /// <param name="properties">The interface properties.</param>
    /// <param name="uuid">The expected UUID.</param>
    /// <returns>Whether the property identifies the configured UUID.</returns>
    private static bool Matches(IDictionary<string, object> properties, Guid uuid) => properties.TryGetValue("UUID", out var value) && value is string text && Guid.TryParse(text, out var parsed) && parsed == uuid;

    /// <summary>Selects a write request where supported, otherwise a write command.</summary>
    /// <param name="properties">The characteristic properties.</param>
    /// <returns>The BlueZ write type.</returns>
    private static string WriteType(IDictionary<string, object> properties)
    {
        var flags = (string[])properties["Flags"];
        if (flags.Contains("write"))
        {
            return "request";
        }
        if (flags.Contains("write-without-response"))
        {
            return "command";
        }
        throw Error("BluetoothLeWriteNotSupported");
    }

    /// <summary>Creates a localized transport error.</summary>
    /// <param name="key">The resource identifier.</param>
    /// <returns>The transport exception.</returns>
    private static IOException Error(string key) => new(Properties.Resources.Get(key));

    /// <summary>Owns a bus connection, notification subscription and optional device connection.</summary>
    private sealed class Session : IBluetoothLeConnection
    {
        /// <summary>Receives payloads from BlueZ notifications.</summary>
        private readonly Action<byte[]> received;
        /// <summary>Receives transport failures.</summary>
        private readonly Action<Exception> failed;
        /// <summary>Prevents duplicate cleanup and callbacks after disposal.</summary>
        private int disposed;
        /// <summary>Prevents duplicate terminal failure callbacks.</summary>
        private int failureReported;
        /// <summary>Records whether cancellation already closed the dedicated bus.</summary>
        private int aborted;
        /// <summary>Records whether opening completed before failure delivery.</summary>
        private int ready;
        /// <summary>Retains the first failure encountered during opening.</summary>
        private Exception? pendingFailure;
        /// <summary>Owns the BlueZ service-owner subscription.</summary>
        internal IDisposable? ServiceWatch;
        /// <summary>Retains the connect reply so rollback can observe its completion.</summary>
        internal Task? ConnectOperation;
        /// <summary>Owns the dedicated system bus connection.</summary>
        internal readonly Connection Bus = new(Address.System);
        /// <summary>References the selected BlueZ device.</summary>
        internal IBlueZDevice? Device;
        /// <summary>References the notification characteristic.</summary>
        internal IBlueZCharacteristic? Input;
        /// <summary>References the output characteristic.</summary>
        internal IBlueZCharacteristic? Output;
        /// <summary>Owns the characteristic property subscription.</summary>
        internal IDisposable? ValueWatch;
        /// <summary>Owns the device property subscription.</summary>
        internal IDisposable? DeviceWatch;
        /// <summary>Records whether this session initiated the device connection.</summary>
        internal bool OwnsConnection;
        /// <summary>Records whether StartNotify completed successfully.</summary>
        internal bool Notifying;
        /// <summary>Selects ATT write requests or commands.</summary>
        internal string WriteType = "request";
        /// <summary>Initializes the callbacks for this session.</summary>
        /// <param name="received">Receives notification bytes.</param>
        /// <param name="failed">Receives terminal errors.</param>
        internal Session(Action<byte[]> received, Action<Exception> failed)
        {
            this.received = received;
            this.failed = failed;
            Bus.StateChanged += BusStateChanged;
        }
        /// <inheritdoc/>
        public int MaxWriteSize => 20;
        /// <summary>Delivers payloads while the session remains alive.</summary>
        /// <param name="bytes">The received payload.</param>
        internal void Receive(byte[] bytes)
        {
            if (Volatile.Read(ref disposed) == 0)
            {
                received(bytes);
            }
        }
        /// <summary>Reports the first asynchronous transport failure.</summary>
        /// <param name="error">The connection error.</param>
        internal void ReportFailure(Exception error)
        {
            Interlocked.CompareExchange(ref pendingFailure, error, null);
            if (Volatile.Read(ref disposed) == 0 && Interlocked.Exchange(ref failureReported, 1) == 0)
            {
                if (Volatile.Read(ref ready) != 0)
                {
                    failed(error);
                }
            }
        }
        /// <summary>Completes startup or propagates an asynchronous startup failure.</summary>
        internal void Activate()
        {
            Volatile.Write(ref ready, 1);
            if (Volatile.Read(ref pendingFailure) is Exception error)
            {
                throw error;
            }
        }
        /// <summary>Reports a system bus loss even when no GATT signal is pending.</summary>
        /// <param name="sender">The bus connection.</param>
        /// <param name="change">The new bus state and disconnect reason.</param>
        private void BusStateChanged(object? sender, ConnectionStateChangedEventArgs change)
        {
            if (change.State == ConnectionState.Disconnected)
            {
                ReportFailure(change.DisconnectReason ?? Error("BluetoothLeDisconnected"));
            }
        }
        /// <summary>Aborts bus operations that cannot accept a cancellation token.</summary>
        internal void Abort()
        {
            Interlocked.Exchange(ref aborted, 1);
            Bus.Dispose();
        }
        /// <inheritdoc/>
        public async ValueTask WriteAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken)
        {
            ObjectDisposedException.ThrowIf(Volatile.Read(ref disposed) != 0, this);
            using var cancellation = cancellationToken.Register(Abort);
            for (int offset = 0; offset < data.Length; offset += MaxWriteSize)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Output!.WriteValueAsync(data.Slice(offset, Math.Min(MaxWriteSize, data.Length - offset)).ToArray(), new Dictionary<string, object> { ["type"] = WriteType }).WaitAsync(cancellationToken).ConfigureAwait(false);
            }
        }
        /// <inheritdoc/>
        public async ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref disposed, 1) != 0)
            {
                return;
            }
            Bus.StateChanged -= BusStateChanged;
            ServiceWatch?.Dispose();
            ValueWatch?.Dispose();
            DeviceWatch?.Dispose();
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
            try
            {
                if (Notifying && Input != null)
                {
                    await Input.StopNotifyAsync().WaitAsync(timeout.Token).ConfigureAwait(false);
                }
            }
            catch (Exception)
            {
                // Closing the dedicated bus also releases its BlueZ notification session.
            }
            try
            {
                if (OwnsConnection && Device != null)
                {
                    if (ConnectOperation != null)
                    {
                        try
                        {
                            await ConnectOperation.WaitAsync(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
                        }
                        catch (Exception)
                        {
                            // A fresh Disconnect below also cancels a pending BlueZ Connect.
                            _ = ConnectOperation.ContinueWith(operation => { _ = operation.Exception; }, CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
                        }
                    }
                    if (Volatile.Read(ref aborted) != 0)
                    {
                        using var cleanup = new Connection(Address.System);
                        using var cleanupTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                        using var abortCleanup = cleanupTimeout.Token.Register(cleanup.Dispose);
                        await cleanup.ConnectAsync().WaitAsync(cleanupTimeout.Token).ConfigureAwait(false);
                        await cleanup.CreateProxy<IBlueZDevice>("org.bluez", Device.ObjectPath).DisconnectAsync().WaitAsync(cleanupTimeout.Token).ConfigureAwait(false);
                    }
                    else
                    {
                        await Device.DisconnectAsync().WaitAsync(timeout.Token).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception)
            {
                // Cleanup must still release the bus when BlueZ is unavailable.
            }
            finally
            {
                Bus.Dispose();
            }
        }
    }
}
#endif
