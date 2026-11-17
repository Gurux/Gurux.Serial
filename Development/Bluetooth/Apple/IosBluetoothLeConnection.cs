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

/// <summary>Owns a CoreBluetooth GATT connection and all native operations on a serial dispatch queue.</summary>
internal sealed class IosBluetoothLeConnection : IBluetoothLeConnection
{
    /// <summary>Serializes all native state and callbacks.</summary>
    private readonly DispatchQueue queue = new("fi.gurux.serial.ble.connection");
    /// <summary>Wakes pending native operations when ownership ends.</summary>
    private readonly CancellationTokenSource lifetime = new();
    /// <summary>Delivers notification payloads to the bounded stream.</summary>
    private readonly Action<byte[]> received;
    /// <summary>Reports native failures to the stream.</summary>
    private readonly Action<Exception> failed;
    /// <summary>Signals the initial powered-on state.</summary>
    private readonly TaskCompletionSource powered = new(TaskCreationOptions.RunContinuationsAsynchronously);
    /// <summary>Signals connection establishment.</summary>
    private readonly TaskCompletionSource connected = new(TaskCreationOptions.RunContinuationsAsynchronously);
    /// <summary>Signals the current service discovery pass; replaced on the native queue before an unfiltered fallback pass.</summary>
    private TaskCompletionSource services = new(TaskCreationOptions.RunContinuationsAsynchronously);
    /// <summary>Signals characteristic discovery completion.</summary>
    private readonly TaskCompletionSource characteristics = new(TaskCreationOptions.RunContinuationsAsynchronously);
    /// <summary>Signals successful notification subscription.</summary>
    private readonly TaskCompletionSource subscribed = new(TaskCreationOptions.RunContinuationsAsynchronously);
    /// <summary>Owns the central manager until cleanup completes.</summary>
    private CBCentralManager? manager;
    /// <summary>Retains the connected peripheral independently of discovery callbacks.</summary>
    private CBPeripheral? peripheral;
    /// <summary>Receives notifications.</summary>
    private CBCharacteristic? read;
    /// <summary>Accepts serial payloads.</summary>
    private CBCharacteristic? write;
    /// <summary>Matches the acknowledged write currently awaiting completion.</summary>
    private CBCharacteristic? writing;
    /// <summary>Signals completion of the current acknowledged write.</summary>
    private TaskCompletionSource? written;
    /// <summary>Signals availability of native output buffer space.</summary>
    private TaskCompletionSource? writable;
    /// <summary>Records the negotiated output payload size.</summary>
    private int maxWriteSize = 20;
    /// <summary>Prevents repeated failure reports.</summary>
    private int failureReported;
    /// <summary>Serializes cleanup task publication.</summary>
    private readonly object cleanupSync = new();
    /// <summary>Allows concurrent disposers to await the same cleanup.</summary>
    private Task? cleanup;
    /// <summary>Rejects work once disposal begins.</summary>
    private int disposed;

    /// <summary>Creates a native connection owner before any callbacks are registered.</summary>
    /// <param name="received">Receives independent notification buffers.</param>
    /// <param name="failed">Receives terminal transport errors.</param>
    private IosBluetoothLeConnection(Action<byte[]> received, Action<Exception> failed)
    {
        this.received = received;
        this.failed = failed;
    }

    /// <inheritdoc/>
    public int MaxWriteSize => Volatile.Read(ref maxWriteSize);

    /// <summary>Retrieves a discovered peripheral and opens its preferred serial profile or a named catalogue fallback when the preferred service is absent.</summary>
    /// <param name="deviceId">The CoreBluetooth peripheral UUID.</param>
    /// <param name="profile">The service and characteristics to use.</param>
    /// <param name="received">Receives notifications.</param>
    /// <param name="failed">Receives terminal native failures.</param>
    /// <param name="cancellationToken">Cancels all opening operations.</param>
    /// <param name="settings">UART settings used to evaluate a fallback catalogue profile, or null to use default UART settings.</param>
    /// <returns>The owned, subscribed native connection.</returns>
    /// <exception cref="IOException">The device, service, or required characteristic is unavailable.</exception>
    /// <exception cref="OperationCanceledException">Opening or the native connection lifetime is canceled.</exception>
    internal static async Task<IBluetoothLeConnection> OpenAsync(string deviceId, BluetoothLeProfile profile, Action<byte[]> received, Action<Exception> failed, CancellationToken cancellationToken, SerialPortSettings? settings = null)
    {
        var connection = new IosBluetoothLeConnection(received, failed);
        try
        {
            await connection.InitializeAsync(deviceId, profile, cancellationToken, settings ?? new SerialPortSettings()).ConfigureAwait(false);
            return connection;
        }
        catch
        {
            await connection.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    /// <summary>Performs sequential discovery and subscription, retaining native state only on its queue.</summary>
    /// <param name="deviceId">The UUID obtained by discovery.</param>
    /// <param name="profile">The fully evaluated serial profile.</param>
    /// <param name="cancellationToken">Cancels opening.</param>
    /// <param name="settings">UART settings used only if a catalogue fallback profile must be evaluated.</param>
    /// <returns>Completion when notifications and optional configuration are ready.</returns>
    private async Task InitializeAsync(string deviceId, BluetoothLeProfile profile, CancellationToken cancellationToken, SerialPortSettings settings)
    {
        using var cancel = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, lifetime.Token);
        CancellationToken token = cancel.Token;
        await OnQueueAsync(() =>
        {
            manager = new CBCentralManager(null, queue);
            manager.UpdatedState += (_, _) =>
            {
                if (manager.State == CBManagerState.PoweredOn)
                {
                    powered.TrySetResult();
                }
                else if (powered.Task.IsCompleted || (manager.State != CBManagerState.Unknown && manager.State != CBManagerState.Resetting))
                {
                    ReportFailure(new IOException(Properties.Resources.Get("BluetoothUnavailable")));
                }
            };
            manager.ConnectedPeripheral += (_, _) => connected.TrySetResult();
            manager.FailedToConnectPeripheral += (_, args) => ReportFailure(NativeError(args.Error));
            manager.DisconnectedPeripheral += (_, args) => ReportFailure(NativeError(args.Error));
        }).ConfigureAwait(false);
        await powered.Task.WaitAsync(token).ConfigureAwait(false);
        await OnQueueAsync(() =>
        {
            using var identifier = new NSUuid(deviceId);
            peripheral = manager!.RetrievePeripheralsWithIdentifiers(identifier).FirstOrDefault()
                ?? throw new IOException(Properties.Resources.Get("BluetoothLeDeviceNotFound"));
            AttachPeripheralHandlers(peripheral);
            manager.ConnectPeripheral(peripheral, (NSDictionary?)null);
        }).ConfigureAwait(false);
        await connected.Task.WaitAsync(token).ConfigureAwait(false);
        await OnQueueAsync(() => peripheral!.DiscoverServices(new[] { CBUUID.FromString(profile.Service.ToString()) })).ConfigureAwait(false);
        await services.Task.WaitAsync(token).ConfigureAwait(false);
        CBService? service = null;
        Task? fallbackDiscovery = null;
        await OnQueueAsync(() =>
        {
            token.ThrowIfCancellationRequested();
            service = peripheral!.Services?.FirstOrDefault(value => Matches(value.UUID, profile.Service));
            if (service == null)
            {
                services = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                fallbackDiscovery = services.Task;
                peripheral.DiscoverServices();
            }
        }).ConfigureAwait(false);
        if (fallbackDiscovery != null)
        {
            await fallbackDiscovery.WaitAsync(token).ConfigureAwait(false);
            await OnQueueAsync(() =>
            {
                token.ThrowIfCancellationRequested();
                Guid[] availableServices = (peripheral!.Services ?? Array.Empty<CBService>()).Select(value =>
                    Guid.TryParse(value.UUID.ToString(true), out Guid uuid) ? uuid : Guid.Empty).Where(uuid => uuid != Guid.Empty).ToArray();
                profile = BluetoothLeServiceSelection.SelectProfile(profile, peripheral.Name, availableServices, settings, deviceId);
                service = peripheral.Services?.FirstOrDefault(value => Matches(value.UUID, profile.Service))
                    ?? throw new IOException(Properties.Resources.Get("BluetoothLeServiceNotFound"));
            }).ConfigureAwait(false);
        }
        await OnQueueAsync(() =>
        {
            token.ThrowIfCancellationRequested();
            var ids = new List<CBUUID> { CBUUID.FromString(profile.ReadCharacteristic.ToString()), CBUUID.FromString(profile.WriteCharacteristic.ToString()) };
            if (profile.ConfigurationCharacteristic.HasValue)
            {
                ids.Add(CBUUID.FromString(profile.ConfigurationCharacteristic.Value.ToString()));
            }
            peripheral!.DiscoverCharacteristics(ids.ToArray(), service!);
        }).ConfigureAwait(false);
        await characteristics.Task.WaitAsync(token).ConfigureAwait(false);
        CBCharacteristic? configuration = null;
        await OnQueueAsync(() =>
        {
            read = FindCharacteristic(service!, profile.ReadCharacteristic);
            write = FindCharacteristic(service!, profile.WriteCharacteristic);
            GetWriteType(write);
            if ((read.Properties & (CBCharacteristicProperties.Notify | CBCharacteristicProperties.Indicate)) == 0)
            {
                throw new IOException(Properties.Resources.Get("BluetoothLeNotificationsNotSupported"));
            }
            if (profile.ConfigurationCharacteristic.HasValue)
            {
                configuration = FindCharacteristic(service!, profile.ConfigurationCharacteristic.Value);
            }
            maxWriteSize = Math.Clamp((int)peripheral!.GetMaximumWriteValueLength(GetWriteType(write)), 1, 512);
            peripheral.SetNotifyValue(true, read);
        }).ConfigureAwait(false);
        await subscribed.Task.WaitAsync(token).ConfigureAwait(false);
        if (configuration != null && profile.ConfigurationValue is byte[] value)
        {
            await WriteCharacteristicAsync(configuration, value, token).ConfigureAwait(false);
        }
        token.ThrowIfCancellationRequested();
    }

    /// <summary>Registers native callbacks before connection establishment can complete.</summary>
    /// <param name="peer">The owned peripheral.</param>
    private void AttachPeripheralHandlers(CBPeripheral peer)
    {
        peer.DiscoveredService += (_, args) => Complete(services, args.Error);
        peer.DiscoveredCharacteristics += (_, args) => Complete(characteristics, args.Error);
        peer.UpdatedNotificationState += (_, args) =>
        {
            if (args.Characteristic == read)
            {
                if (args.Error != null || !args.Characteristic.IsNotifying)
                {
                    ReportFailure(NativeError(args.Error));
                }
                else
                {
                    subscribed.TrySetResult();
                }
            }
        };
        peer.UpdatedCharacterteristicValue += (_, args) =>
        {
            if (args.Characteristic == read && Volatile.Read(ref disposed) == 0)
            {
                if (args.Error != null)
                {
                    ReportFailure(NativeError(args.Error));
                }
                else
                {
                    try
                    {
                        received(args.Characteristic.Value?.ToArray() ?? Array.Empty<byte>());
                    }
                    catch (Exception error)
                    {
                        ReportFailure(error);
                    }
                }
            }
        };
        peer.WroteCharacteristicValue += (_, args) =>
        {
            if (args.Characteristic == writing && written != null)
            {
                Complete(written, args.Error);
            }
        };
        peer.IsReadyToSendWriteWithoutResponse += (_, _) => writable?.TrySetResult();
        peer.ModifiedServices += (_, _) => ReportFailure(new IOException(Properties.Resources.Get("BluetoothLeDisconnected")));
    }

    /// <inheritdoc/>
    public async ValueTask WriteAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken)
    {
        await WriteCharacteristicAsync(write!, data, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Writes an attribute atomically using acknowledgments or native output-buffer flow control.</summary>
    /// <param name="destination">The output or configuration characteristic.</param>
    /// <param name="data">The complete attribute payload.</param>
    /// <param name="cancellationToken">Cancels the native operation.</param>
    /// <returns>Completion after acknowledgment or native acceptance of an unacknowledged write.</returns>
    /// <exception cref="IOException">The characteristic cannot accept the complete payload.</exception>
    private async Task WriteCharacteristicAsync(CBCharacteristic destination, ReadOnlyMemory<byte> data, CancellationToken cancellationToken)
    {
        using var cancel = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, lifetime.Token);
        while (true)
        {
            cancel.Token.ThrowIfCancellationRequested();
            Task? pending = null;
            bool sent = false;
            await OnQueueAsync(() =>
            {
                CBCharacteristicWriteType type = GetWriteType(destination);
                if (data.Length > (int)peripheral!.GetMaximumWriteValueLength(type))
                {
                    throw new IOException(Properties.Resources.Get("BluetoothLeOperationFailed"));
                }
                if (type == CBCharacteristicWriteType.WithoutResponse && !peripheral.CanSendWriteWithoutResponse)
                {
                    writable = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                    pending = writable.Task;
                    return;
                }
                if (type == CBCharacteristicWriteType.WithResponse)
                {
                    written = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                    writing = destination;
                    pending = written.Task;
                }
                using var payload = NSData.FromArray(data.ToArray());
                peripheral.WriteValue(payload, destination, type);
                sent = true;
            }).ConfigureAwait(false);
            if (pending != null)
            {
                await pending.WaitAsync(cancel.Token).ConfigureAwait(false);
            }
            if (sent)
            {
                return;
            }
        }
    }

    /// <summary>Selects acknowledged output whenever the characteristic supports it.</summary>
    /// <param name="characteristic">The destination attribute.</param>
    /// <returns>The supported native write mode.</returns>
    /// <exception cref="IOException">The attribute is not writable.</exception>
    private static CBCharacteristicWriteType GetWriteType(CBCharacteristic characteristic)
    {
        if ((characteristic.Properties & CBCharacteristicProperties.Write) != 0)
        {
            return CBCharacteristicWriteType.WithResponse;
        }
        if ((characteristic.Properties & CBCharacteristicProperties.WriteWithoutResponse) != 0)
        {
            return CBCharacteristicWriteType.WithoutResponse;
        }
        throw new IOException(Properties.Resources.Get("BluetoothLeWriteNotSupported"));
    }

    /// <summary>Resolves a required UUID in the selected service.</summary>
    /// <param name="service">The discovered serial service.</param>
    /// <param name="id">The required characteristic UUID.</param>
    /// <returns>The corresponding native characteristic.</returns>
    private static CBCharacteristic FindCharacteristic(CBService service, Guid id) => service.Characteristics?.FirstOrDefault(value => Matches(value.UUID, id))
        ?? throw new IOException(Properties.Resources.Get("BluetoothLeCharacteristicNotFound"));

    /// <summary>Compares UUIDs through CoreBluetooth so short Bluetooth UUIDs are expanded correctly.</summary>
    /// <param name="native">The native UUID.</param>
    /// <param name="id">The profile UUID.</param>
    /// <returns>Whether both values identify the same attribute.</returns>
    private static bool Matches(CBUUID native, Guid id)
    {
        using var expected = CBUUID.FromString(id.ToString());
        return native.Equals(expected);
    }

    /// <summary>Completes a discovery or acknowledged write callback.</summary>
    /// <param name="completion">The matching pending native operation.</param>
    /// <param name="error">The native error, if any.</param>
    private void Complete(TaskCompletionSource completion, NSError? error)
    {
        if (error != null)
        {
            ReportFailure(NativeError(error));
        }
        else
        {
            completion.TrySetResult();
        }
    }

    /// <summary>Wraps a native error without losing its platform diagnostic details.</summary>
    /// <param name="error">The native error, or null on an unexpected disconnect.</param>
    /// <returns>A localized transport exception.</returns>
    private static IOException NativeError(NSError? error) => new(Properties.Resources.Get("BluetoothLeOperationFailed"), error == null ? null : new NSErrorException(error));

    /// <summary>Signals a terminal error once and wakes every pending operation.</summary>
    /// <param name="error">The terminal error.</param>
    private void ReportFailure(Exception error)
    {
        if (Volatile.Read(ref disposed) == 0 && Interlocked.Exchange(ref failureReported, 1) == 0)
        {
            try
            {
                failed(error);
            }
            catch
            {
                // Consumer failures must not escape the native dispatcher.
            }
            finally
            {
                // Publish the transport failure before cancellation can wake the opening caller.
                lifetime.Cancel();
            }
        }
    }

    /// <summary>Schedules native work and rejects it after disposal begins.</summary>
    /// <param name="action">The native operation to execute.</param>
    /// <returns>Completion when the operation was submitted.</returns>
    private Task OnQueueAsync(Action action) => IosBluetoothLeDiscovery.DispatchAsync(queue, () =>
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref disposed) != 0, this);
        action();
    });

    /// <inheritdoc/>
    public ValueTask DisposeAsync()
    {
        lock (cleanupSync)
        {
            if (cleanup == null)
            {
                Interlocked.Exchange(ref disposed, 1);
                lifetime.Cancel();
                cleanup = CleanupAsync();
            }
            return new ValueTask(cleanup);
        }
    }

    /// <summary>Stops native callbacks and cancels pending connections without blocking a callback thread.</summary>
    /// <returns>Completion after all native handles have been released.</returns>
    private async Task CleanupAsync()
    {
        try
        {
            await IosBluetoothLeDiscovery.DispatchAsync(queue, () =>
            {
                if (manager != null)
                {
                    try
                    {
                        manager.StopScan();
                        if (peripheral != null)
                        {
                            try
                            {
                                if (read?.IsNotifying == true && peripheral.State == CBPeripheralState.Connected)
                                {
                                    peripheral.SetNotifyValue(false, read);
                                }
                            }
                            finally
                            {
                                peripheral.Delegate = null!;
                                manager.CancelPeripheralConnection(peripheral);
                            }
                        }
                    }
                    finally
                    {
                        peripheral?.Dispose();
                        manager.Delegate = null!;
                        manager.Dispose();
                    }
                }
            }).ConfigureAwait(false);
        }
        finally
        {
            queue.Dispose();
        }
    }
}
#endif
