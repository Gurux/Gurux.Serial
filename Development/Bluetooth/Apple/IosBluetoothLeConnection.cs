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
    private readonly DispatchQueue _queue = new("fi.gurux.serial.ble.connection");
    /// <summary>Wakes pending native operations when ownership ends.</summary>
    private readonly CancellationTokenSource _lifetime = new();
    /// <summary>Delivers notification payloads to the bounded stream.</summary>
    private readonly Action<byte[]> _received;
    /// <summary>Reports native failures to the stream.</summary>
    private readonly Action<Exception> _failed;
    /// <summary>Signals the initial powered-on state.</summary>
    private readonly TaskCompletionSource _powered = new(TaskCreationOptions.RunContinuationsAsynchronously);
    /// <summary>Signals connection establishment.</summary>
    private readonly TaskCompletionSource _connected = new(TaskCreationOptions.RunContinuationsAsynchronously);
    /// <summary>Signals the current service discovery pass; replaced on the native queue before an unfiltered fallback pass.</summary>
    private TaskCompletionSource _services = new(TaskCreationOptions.RunContinuationsAsynchronously);
    /// <summary>Signals characteristic discovery completion.</summary>
    private readonly TaskCompletionSource _characteristics = new(TaskCreationOptions.RunContinuationsAsynchronously);
    /// <summary>Signals successful notification subscription.</summary>
    private readonly TaskCompletionSource _subscribed = new(TaskCreationOptions.RunContinuationsAsynchronously);
    /// <summary>Owns the central manager until cleanup completes.</summary>
    private CBCentralManager? _manager;
    /// <summary>Retains the connected peripheral independently of discovery callbacks.</summary>
    private CBPeripheral? _peripheral;
    /// <summary>Receives notifications.</summary>
    private CBCharacteristic? _read;
    /// <summary>Accepts serial payloads.</summary>
    private CBCharacteristic? _write;
    /// <summary>Matches the acknowledged write currently awaiting completion.</summary>
    private CBCharacteristic? _writing;
    /// <summary>Signals completion of the current acknowledged write.</summary>
    private TaskCompletionSource? _written;
    /// <summary>Signals availability of native output buffer space.</summary>
    private TaskCompletionSource? _writable;
    /// <summary>Records the negotiated output payload size.</summary>
    private int _maxWriteSize = 20;
    /// <summary>Prevents repeated failure reports.</summary>
    private int _failureReported;
    /// <summary>Serializes cleanup task publication.</summary>
    private readonly object _cleanupSync = new();
    /// <summary>Allows concurrent disposers to await the same cleanup.</summary>
    private Task? _cleanup;
    /// <summary>Rejects work once disposal begins.</summary>
    private int _disposed;

    /// <summary>Creates a native connection owner before any callbacks are registered.</summary>
    /// <param name="received">Receives independent notification buffers.</param>
    /// <param name="failed">Receives terminal transport errors.</param>
    private IosBluetoothLeConnection(Action<byte[]> received, Action<Exception> failed)
    {
        this._received = received;
        this._failed = failed;
    }

    /// <inheritdoc/>
    public int MaxWriteSize => Volatile.Read(ref _maxWriteSize);

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
        using var cancel = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _lifetime.Token);
        CancellationToken token = cancel.Token;
        await OnQueueAsync(() =>
        {
            _manager = new CBCentralManager(null, _queue);
            _manager.UpdatedState += (_, _) =>
            {
                if (_manager.State == CBManagerState.PoweredOn)
                {
                    _powered.TrySetResult();
                }
                else if (_powered.Task.IsCompleted || (_manager.State != CBManagerState.Unknown && _manager.State != CBManagerState.Resetting))
                {
                    ReportFailure(new IOException(Properties.Resources.Get("BluetoothUnavailable")));
                }
            };
            _manager.ConnectedPeripheral += (_, _) => _connected.TrySetResult();
            _manager.FailedToConnectPeripheral += (_, args) => ReportFailure(NativeError(args.Error));
            _manager.DisconnectedPeripheral += (_, args) => ReportFailure(NativeError(args.Error));
        }).ConfigureAwait(false);
        await _powered.Task.WaitAsync(token).ConfigureAwait(false);
        await OnQueueAsync(() =>
        {
            using var identifier = new NSUuid(deviceId);
            _peripheral = _manager!.RetrievePeripheralsWithIdentifiers(identifier).FirstOrDefault()
                ?? throw new IOException(Properties.Resources.Get("BluetoothLeDeviceNotFound"));
            AttachPeripheralHandlers(_peripheral);
            _manager.ConnectPeripheral(_peripheral, (NSDictionary?)null);
        }).ConfigureAwait(false);
        await _connected.Task.WaitAsync(token).ConfigureAwait(false);
        await OnQueueAsync(() => _peripheral!.DiscoverServices(new[] { CBUUID.FromString(profile.Service.ToString()) })).ConfigureAwait(false);
        await _services.Task.WaitAsync(token).ConfigureAwait(false);
        CBService? service = null;
        Task? fallbackDiscovery = null;
        await OnQueueAsync(() =>
        {
            token.ThrowIfCancellationRequested();
            service = _peripheral!.Services?.FirstOrDefault(value => Matches(value.UUID, profile.Service));
            if (service == null)
            {
                _services = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                fallbackDiscovery = _services.Task;
                _peripheral.DiscoverServices();
            }
        }).ConfigureAwait(false);
        if (fallbackDiscovery != null)
        {
            await fallbackDiscovery.WaitAsync(token).ConfigureAwait(false);
            await OnQueueAsync(() =>
            {
                token.ThrowIfCancellationRequested();
                Guid[] availableServices = (_peripheral!.Services ?? Array.Empty<CBService>()).Select(value =>
                    Guid.TryParse(value.UUID.ToString(true), out Guid uuid) ? uuid : Guid.Empty).Where(uuid => uuid != Guid.Empty).ToArray();
                profile = BluetoothLeServiceSelection.SelectProfile(profile, _peripheral.Name, availableServices, settings, deviceId);
                service = _peripheral.Services?.FirstOrDefault(value => Matches(value.UUID, profile.Service))
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
            _peripheral!.DiscoverCharacteristics(ids.ToArray(), service!);
        }).ConfigureAwait(false);
        await _characteristics.Task.WaitAsync(token).ConfigureAwait(false);
        CBCharacteristic? configuration = null;
        await OnQueueAsync(() =>
        {
            _read = FindCharacteristic(service!, profile.ReadCharacteristic);
            _write = FindCharacteristic(service!, profile.WriteCharacteristic);
            GetWriteType(_write);
            if ((_read.Properties & (CBCharacteristicProperties.Notify | CBCharacteristicProperties.Indicate)) == 0)
            {
                throw new IOException(Properties.Resources.Get("BluetoothLeNotificationsNotSupported"));
            }
            if (profile.ConfigurationCharacteristic.HasValue)
            {
                configuration = FindCharacteristic(service!, profile.ConfigurationCharacteristic.Value);
            }
            _maxWriteSize = Math.Clamp((int)_peripheral!.GetMaximumWriteValueLength(GetWriteType(_write)), 1, 512);
            _peripheral.SetNotifyValue(true, _read);
        }).ConfigureAwait(false);
        await _subscribed.Task.WaitAsync(token).ConfigureAwait(false);
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
        peer.DiscoveredService += (_, args) => Complete(_services, args.Error);
        peer.DiscoveredCharacteristics += (_, args) => Complete(_characteristics, args.Error);
        peer.UpdatedNotificationState += (_, args) =>
        {
            if (args.Characteristic == _read)
            {
                if (args.Error != null || !args.Characteristic.IsNotifying)
                {
                    ReportFailure(NativeError(args.Error));
                }
                else
                {
                    _subscribed.TrySetResult();
                }
            }
        };
        peer.UpdatedCharacterteristicValue += (_, args) =>
        {
            if (args.Characteristic == _read && Volatile.Read(ref _disposed) == 0)
            {
                if (args.Error != null)
                {
                    ReportFailure(NativeError(args.Error));
                }
                else
                {
                    try
                    {
                        _received(args.Characteristic.Value?.ToArray() ?? Array.Empty<byte>());
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
            if (args.Characteristic == _writing && _written != null)
            {
                Complete(_written, args.Error);
            }
        };
        peer.IsReadyToSendWriteWithoutResponse += (_, _) => _writable?.TrySetResult();
        peer.ModifiedServices += (_, _) => ReportFailure(new IOException(Properties.Resources.Get("BluetoothLeDisconnected")));
    }

    /// <inheritdoc/>
    public async ValueTask WriteAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken)
    {
        await WriteCharacteristicAsync(_write!, data, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Writes an attribute atomically using acknowledgments or native output-buffer flow control.</summary>
    /// <param name="destination">The output or configuration characteristic.</param>
    /// <param name="data">The complete attribute payload.</param>
    /// <param name="cancellationToken">Cancels the native operation.</param>
    /// <returns>Completion after acknowledgment or native acceptance of an unacknowledged write.</returns>
    /// <exception cref="IOException">The characteristic cannot accept the complete payload.</exception>
    private async Task WriteCharacteristicAsync(CBCharacteristic destination, ReadOnlyMemory<byte> data, CancellationToken cancellationToken)
    {
        using var cancel = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _lifetime.Token);
        while (true)
        {
            cancel.Token.ThrowIfCancellationRequested();
            Task? pending = null;
            bool sent = false;
            await OnQueueAsync(() =>
            {
                CBCharacteristicWriteType type = GetWriteType(destination);
                if (data.Length > (int)_peripheral!.GetMaximumWriteValueLength(type))
                {
                    throw new IOException(Properties.Resources.Get("BluetoothLeOperationFailed"));
                }
                if (type == CBCharacteristicWriteType.WithoutResponse && !_peripheral.CanSendWriteWithoutResponse)
                {
                    _writable = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                    pending = _writable.Task;
                    return;
                }
                if (type == CBCharacteristicWriteType.WithResponse)
                {
                    _written = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                    _writing = destination;
                    pending = _written.Task;
                }
                using var payload = NSData.FromArray(data.ToArray());
                _peripheral.WriteValue(payload, destination, type);
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
        if (Volatile.Read(ref _disposed) == 0 && Interlocked.Exchange(ref _failureReported, 1) == 0)
        {
            try
            {
                _failed(error);
            }
            catch
            {
                // Consumer failures must not escape the native dispatcher.
            }
            finally
            {
                // Publish the transport failure before cancellation can wake the opening caller.
                _lifetime.Cancel();
            }
        }
    }

    /// <summary>Schedules native work and rejects it after disposal begins.</summary>
    /// <param name="action">The native operation to execute.</param>
    /// <returns>Completion when the operation was submitted.</returns>
    private Task OnQueueAsync(Action action) => IosBluetoothLeDiscovery.DispatchAsync(_queue, () =>
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
        action();
    });

    /// <inheritdoc/>
    public ValueTask DisposeAsync()
    {
        lock (_cleanupSync)
        {
            if (_cleanup == null)
            {
                Interlocked.Exchange(ref _disposed, 1);
                _lifetime.Cancel();
                _cleanup = CleanupAsync();
            }
            return new ValueTask(_cleanup);
        }
    }

    /// <summary>Stops native callbacks and cancels pending connections without blocking a callback thread.</summary>
    /// <returns>Completion after all native handles have been released.</returns>
    private async Task CleanupAsync()
    {
        try
        {
            await IosBluetoothLeDiscovery.DispatchAsync(_queue, () =>
            {
                if (_manager != null)
                {
                    try
                    {
                        _manager.StopScan();
                        if (_peripheral != null)
                        {
                            try
                            {
                                if (_read?.IsNotifying == true && _peripheral.State == CBPeripheralState.Connected)
                                {
                                    _peripheral.SetNotifyValue(false, _read);
                                }
                            }
                            finally
                            {
                                _peripheral.Delegate = null!;
                                _manager.CancelPeripheralConnection(_peripheral);
                            }
                        }
                    }
                    finally
                    {
                        _peripheral?.Dispose();
                        _manager.Delegate = null!;
                        _manager.Dispose();
                    }
                }
            }).ConfigureAwait(false);
        }
        finally
        {
            _queue.Dispose();
        }
    }
}
#endif
