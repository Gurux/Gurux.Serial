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

/// <summary>Reads actual remote service UUIDs using an independent metadata-only CoreBluetooth connection.</summary>
internal static class IosBluetoothGattServiceDiscovery
{
    /// <summary>Connects a known peripheral, discovers all services and releases this request's native connection.</summary>
    /// <param name="deviceId">The CoreBluetooth peripheral UUID obtained from device discovery.</param>
    /// <param name="cancellationToken">Cancels connection, service discovery and pending native replies.</param>
    /// <returns>Distinct, sorted service UUIDs reported by the peripheral.</returns>
    /// <remarks>No characteristic values, configuration, notification subscriptions or serial streams are accessed.</remarks>
    /// <exception cref="IOException">Bluetooth is unavailable or the device cannot complete connection or discovery.</exception>
    /// <exception cref="OperationCanceledException">The caller cancels the operation.</exception>
    internal static async Task<Guid[]> GetServicesAsync(string deviceId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await using var session = new Session();
        try
        {
            return await session.ReadAsync(deviceId, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception error) when (error is not IOException && error is not OperationCanceledException)
        {
            cancellationToken.ThrowIfCancellationRequested();
            throw new IOException(Properties.Resources.Get("BluetoothLeOperationFailed"), error);
        }
    }

    /// <summary>Owns a central manager, peripheral connection and all native callbacks on a private serial queue.</summary>
    private sealed class Session : IAsyncDisposable
    {
        /// <summary>Serializes all CoreBluetooth state, requests and cleanup.</summary>
        private readonly DispatchQueue _queue = new("fi.gurux.serial.ble.services");
        /// <summary>Signals that the manager can connect to peripherals.</summary>
        private readonly TaskCompletionSource _powered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        /// <summary>Signals establishment of this manager's peripheral connection.</summary>
        private readonly TaskCompletionSource _connected = new(TaskCreationOptions.RunContinuationsAsynchronously);
        /// <summary>Signals the discovered service UUID snapshot.</summary>
        private readonly TaskCompletionSource<Guid[]> _services = new(TaskCreationOptions.RunContinuationsAsynchronously);
        /// <summary>Owns only this metadata request's central manager.</summary>
        private CBCentralManager? _manager;
        /// <summary>Retains the peripheral while this request is active.</summary>
        private CBPeripheral? _peripheral;
        /// <summary>Records whether this manager submitted its own connection request.</summary>
        private bool _ownsConnection;
        /// <summary>Rejects callbacks after native cleanup begins on the queue.</summary>
        private bool _stopped;
        /// <summary>Prevents repeated cleanup requests.</summary>
        private int _disposed;

        /// <summary>Creates the manager, connects the selected peripheral and requests its complete service list.</summary>
        /// <param name="deviceId">The UUID identifying a previously discovered peripheral.</param>
        /// <param name="cancellationToken">Cancels every asynchronous native stage.</param>
        /// <returns>The actual distinct, sorted service UUIDs returned by CoreBluetooth.</returns>
        /// <exception cref="IOException">The peripheral or native Bluetooth operation is unavailable.</exception>
        /// <exception cref="OperationCanceledException">The caller cancels a pending stage.</exception>
        internal async Task<Guid[]> ReadAsync(string deviceId, CancellationToken cancellationToken)
        {
            await OnQueueAsync(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                _manager = new CBCentralManager(null, _queue);
                _manager.UpdatedState += OnUpdatedState;
                _manager.ConnectedPeripheral += OnConnected;
                _manager.FailedToConnectPeripheral += OnConnectionFailed;
                _manager.DisconnectedPeripheral += OnDisconnected;
            }).ConfigureAwait(false);
            await _powered.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
            await OnQueueAsync(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                using var identifier = new NSUuid(deviceId);
                _peripheral = _manager!.RetrievePeripheralsWithIdentifiers(identifier).FirstOrDefault()
                    ?? throw new IOException(Properties.Resources.Get("BluetoothLeDeviceNotFound"));
                _peripheral.DiscoveredService += OnServicesDiscovered;
                _ownsConnection = true;
                _manager.ConnectPeripheral(_peripheral, (NSDictionary?)null);
            }).ConfigureAwait(false);
            await _connected.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
            await OnQueueAsync(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                _peripheral!.DiscoverServices();
            }).ConfigureAwait(false);
            Guid[] result = await _services.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            return result;
        }

        /// <summary>Accepts a powered-on manager or fails pending requests when Bluetooth becomes unavailable.</summary>
        /// <param name="sender">The owning central manager.</param>
        /// <param name="args">The native state change notification.</param>
        private void OnUpdatedState(object? sender, EventArgs args)
        {
            if (_stopped) { return; }
            if (_manager!.State == CBManagerState.PoweredOn)
            {
                _powered.TrySetResult();
            }
            else if (_manager.State != CBManagerState.Unknown && _manager.State != CBManagerState.Resetting)
            {
                Fail(new IOException(Properties.Resources.Get("BluetoothUnavailable")));
            }
        }

        /// <summary>Completes only the connection requested by this session.</summary>
        /// <param name="sender">The owning central manager.</param>
        /// <param name="args">The peripheral whose connection completed.</param>
        private void OnConnected(object? sender, CBPeripheralEventArgs args)
        {
            if (!_stopped && args.Peripheral == _peripheral) { _connected.TrySetResult(); }
        }

        /// <summary>Reports failure of this manager's peripheral connection request.</summary>
        /// <param name="sender">The owning central manager.</param>
        /// <param name="args">The peripheral and its native connection failure.</param>
        private void OnConnectionFailed(object? sender, CBPeripheralErrorEventArgs args)
        {
            if (!_stopped && args.Peripheral == _peripheral) { Fail(NativeError(args.Error)); }
        }

        /// <summary>Wakes pending discovery if this request's connection is lost.</summary>
        /// <param name="sender">The owning central manager.</param>
        /// <param name="args">The peripheral and optional native disconnect error.</param>
        private void OnDisconnected(object? sender, CBPeripheralErrorEventArgs args)
        {
            if (!_stopped && args.Peripheral == _peripheral)
            {
                Fail(args.Error is null ? new IOException(Properties.Resources.Get("BluetoothLeDisconnected")) : NativeError(args.Error));
            }
        }

        /// <summary>Publishes the complete service UUID list without discovering or accessing characteristics.</summary>
        /// <param name="sender">The owned peripheral.</param>
        /// <param name="args">The optional native discovery error.</param>
        private void OnServicesDiscovered(object? sender, NSErrorEventArgs args)
        {
            if (_stopped) { return; }
            if (args.Error is not null)
            {
                Fail(NativeError(args.Error));
                return;
            }
            try
            {
                Guid[] result = (_peripheral!.Services ?? Array.Empty<CBService>())
                    .Select(service => Guid.TryParse(service.UUID.ToString(true), out Guid uuid) ? uuid : Guid.Empty)
                    .Where(uuid => uuid != Guid.Empty).Distinct().OrderBy(uuid => uuid).ToArray();
                _services.TrySetResult(result);
            }
            catch (Exception error)
            {
                Fail(new IOException(Properties.Resources.Get("BluetoothLeOperationFailed"), error));
            }
        }

        /// <summary>Signals every incomplete stage so native failures cannot leave another stage waiting indefinitely.</summary>
        /// <param name="error">The localized native operation error.</param>
        private void Fail(Exception error)
        {
            if (_stopped) { return; }
            _powered.TrySetException(error);
            _connected.TrySetException(error);
            _services.TrySetException(error);
        }

        /// <summary>Wraps the platform diagnostic with a localized operation message.</summary>
        /// <param name="error">The optional native error.</param>
        /// <returns>The localized I/O exception retaining native diagnostic details.</returns>
        private static IOException NativeError(NSError? error) => new(Properties.Resources.Get("BluetoothLeOperationFailed"),
            error is null ? null : new NSErrorException(error));

        /// <summary>Runs one native action on this session's owning serial queue.</summary>
        /// <param name="action">The native operation to execute.</param>
        /// <returns>Completion after the queued native action has run.</returns>
        private Task OnQueueAsync(Action action) => IosBluetoothLeDiscovery.DispatchAsync(_queue, action);

        /// <summary>Releases each owned native resource independently so one teardown failure cannot prevent the remaining cleanup.</summary>
        private void ReleaseNative()
        {
            _stopped = true;
            if (_peripheral is not null)
            {
                try
                {
                    _peripheral.DiscoveredService -= OnServicesDiscovered;
                }
                catch (Exception)
                {
                    // The manager is still released even if a native peripheral already ended.
                }
                if (_ownsConnection && _manager is not null)
                {
                    try
                    {
                        _manager.CancelPeripheralConnection(_peripheral);
                    }
                    catch (Exception)
                    {
                        // Disposing this request's manager still releases its connection ownership.
                    }
                }
                try
                {
                    _peripheral.Dispose();
                }
                catch (Exception)
                {
                    // Release the manager even when peripheral teardown races native shutdown.
                }
            }
            if (_manager is not null)
            {
                try
                {
                    _manager.UpdatedState -= OnUpdatedState;
                    _manager.ConnectedPeripheral -= OnConnected;
                    _manager.FailedToConnectPeripheral -= OnConnectionFailed;
                    _manager.DisconnectedPeripheral -= OnDisconnected;
                }
                catch (Exception)
                {
                    // Clearing this owned manager's delegate below also detaches native delivery.
                }
                try
                {
                    _manager.Delegate = null!;
                }
                catch (Exception)
                {
                    // Disposing the owned manager remains necessary if delegate detachment races shutdown.
                }
                try
                {
                    _manager.Dispose();
                }
                catch (Exception)
                {
                    // Native cleanup errors must not overwrite the operation result or cancellation.
                }
            }
        }
        /// <summary>Detaches callbacks, cancels only this manager's connection and releases native handles on their owning queue.</summary>
        /// <returns>Completion once the owned peripheral, manager and queue have been released.</returns>
        public async ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0) { return; }
            try
            {
                await OnQueueAsync(ReleaseNative).ConfigureAwait(false);
            }
            catch (Exception)
            {
                // Cleanup must preserve the original cancellation or native operation failure.
            }
            finally
            {
                _ = _powered.Task.Exception;
                _ = _connected.Task.Exception;
                _ = _services.Task.Exception;
                try
                {
                    _queue.Dispose();
                }
                catch (Exception)
                {
                    // Disposing an already terminated native queue must not replace the original failure.
                }
            }
        }
    }
}
#endif