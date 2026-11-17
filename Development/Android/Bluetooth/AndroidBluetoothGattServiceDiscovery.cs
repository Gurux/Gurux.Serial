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

namespace Gurux.Serial;

/// <summary>Reads actual remote GATT service UUIDs through an independent Android connection without opening a serial stream.</summary>
internal static class AndroidBluetoothGattServiceDiscovery
{
#if ANDROID37_0_OR_GREATER
    /// <summary>Dispatches native callbacks inline without allocating an application worker thread.</summary>
    private static readonly CallbackExecutor callbackExecutor = new();

    /// <summary>Invokes the native callback command on Android's delivering thread.</summary>
    private sealed class CallbackExecutor : Java.Lang.Object, Java.Util.Concurrent.IExecutor
    {
        /// <summary>Runs a native callback immediately without changing its ordering.</summary>
        /// <param name="command">The callback supplied by Android.</param>
        /// <exception cref="ArgumentNullException">The callback is null.</exception>
        public void Execute(Java.Lang.IRunnable? command)
        {
            ArgumentNullException.ThrowIfNull(command);
            command.Run();
        }
    }
#endif

    /// <summary>Connects a dedicated GATT client, discovers all remote services and closes the client on every exit.</summary>
    /// <param name="deviceId">The remote Bluetooth MAC address.</param>
    /// <param name="cancellationToken">Cancels connection, discovery and waiting for native replies.</param>
    /// <returns>Distinct, sorted service UUIDs reported by the remote device.</returns>
    /// <remarks>The caller supplies Bluetooth runtime permissions. No characteristic values, descriptors, notifications, configuration or serial data are accessed.</remarks>
    /// <exception cref="IOException">Bluetooth is unavailable or Android rejects connection or service discovery.</exception>
    /// <exception cref="OperationCanceledException">The caller cancels the operation.</exception>
    internal static async Task<Guid[]> GetServicesAsync(string deviceId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var context = Android.App.Application.Context;
        using var callback = new ServiceCallback();
        BluetoothGatt? connection = null;
        try
        {
            using var manager = context.GetSystemService(Context.BluetoothService) as BluetoothManager;
            using var adapter = manager?.Adapter;
            if (adapter?.IsEnabled != true)
            {
                throw Error("BluetoothUnavailable");
            }
            using var device = adapter.GetRemoteDevice(deviceId) ?? throw Error("BluetoothLeDeviceNotFound");
            cancellationToken.ThrowIfCancellationRequested();
            connection = Connect(context, device, callback) ?? throw Error("BluetoothLeConnectionFailed");
            await callback.Connected.WaitAsync(cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            if (!connection.DiscoverServices())
            {
                throw Error("BluetoothLeOperationFailed");
            }
            Guid[] services = await callback.Services.WaitAsync(cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            return services;
        }
        catch (Exception error) when (error is not IOException && error is not OperationCanceledException)
        {
            cancellationToken.ThrowIfCancellationRequested();
            throw new IOException(Properties.Resources.Get("BluetoothLeOperationFailed"), error);
        }
        finally
        {
            callback.Stop();
            if (connection is not null)
            {
                Close(connection);
            }
        }
    }

    /// <summary>Creates a direct, owned LE connection without automatic MTU negotiation or background reconnect behavior.</summary>
    /// <param name="context">The application context.</param>
    /// <param name="device">The remote device resolved from its address.</param>
    /// <param name="callback">The callback owned by this metadata request.</param>
    /// <returns>The native GATT client, or null when Android rejects creation.</returns>
    private static BluetoothGatt? Connect(Context context, BluetoothDevice device, BluetoothGattCallback callback)
    {
#if ANDROID37_0_OR_GREATER
        if (OperatingSystem.IsAndroidVersionAtLeast(37))
        {
            using var builder = new BluetoothGattConnectionSettings.Builder();
            builder.SetAutoConnectEnabled(false);
            builder.SetTransport((int)BluetoothTransports.Le);
            builder.SetAutomaticMtuEnabled(false);
            using var settings = builder.Build();
            return device.ConnectGatt(settings!, callbackExecutor, callback);
        }
#endif
        if (OperatingSystem.IsAndroidVersionAtLeast(23))
        {
            return device.ConnectGatt(context.ApplicationContext ?? context, false, callback, BluetoothTransports.Le);
        }
        return device.ConnectGatt(context.ApplicationContext ?? context, false, callback);
    }

    /// <summary>Releases only this request's native GATT client while preserving any earlier operation error.</summary>
    /// <param name="connection">The dedicated native client to release.</param>
    private static void Close(BluetoothGatt connection)
    {
        try
        {
            connection.Disconnect();
        }
        catch (Exception)
        {
            // A disabled Bluetooth service can reject disconnect; closing still releases this client.
        }
        try
        {
            connection.Close();
        }
        catch (Exception)
        {
            // The managed peer must also be released when native shutdown races cleanup.
        }
        try
        {
            connection.Dispose();
        }
        catch (Exception)
        {
            // Cleanup must not replace a cancellation or discovery failure.
        }
    }

    /// <summary>Creates a localized native operation error.</summary>
    /// <param name="key">The transport resource key identifying the failure.</param>
    /// <returns>The localized I/O exception.</returns>
    private static IOException Error(string key) => new(Properties.Resources.Get(key));

    /// <summary>Completes metadata-only native requests and rejects callbacks after cleanup begins.</summary>
    private sealed class ServiceCallback : BluetoothGattCallback
    {
        /// <summary>Orders callback processing against request shutdown.</summary>
        private readonly object sync = new();
        /// <summary>Signals successful establishment of this client's connection.</summary>
        private readonly TaskCompletionSource connected = new(TaskCreationOptions.RunContinuationsAsynchronously);
        /// <summary>Signals the remote service UUID snapshot.</summary>
        private readonly TaskCompletionSource<Guid[]> services = new(TaskCreationOptions.RunContinuationsAsynchronously);
        /// <summary>Prevents late callbacks from completing a finished request.</summary>
        private bool stopped;

        /// <summary>Gets the asynchronous connection result.</summary>
        internal Task Connected => connected.Task;
        /// <summary>Gets the asynchronous service UUID result.</summary>
        internal Task<Guid[]> Services => services.Task;

        /// <summary>Stops accepting callbacks and observes failures from stages the caller did not reach.</summary>
        internal void Stop()
        {
            lock (sync)
            {
                stopped = true;
                _ = connected.Task.Exception;
                _ = services.Task.Exception;
            }
        }

        /// <inheritdoc/>
        public override void OnConnectionStateChange(BluetoothGatt? gatt, GattStatus status, ProfileState newState)
        {
            lock (sync)
            {
                if (stopped) { return; }
                if (status != GattStatus.Success || newState == ProfileState.Disconnected)
                {
                    IOException error = status != GattStatus.Success
                        ? new IOException(string.Format(System.Globalization.CultureInfo.CurrentCulture, Properties.Resources.Get("BluetoothLeOperationFailedWithStatus"), status))
                        : Error("BluetoothLeDisconnected");
                    connected.TrySetException(error);
                    services.TrySetException(error);
                }
                else if (newState == ProfileState.Connected)
                {
                    connected.TrySetResult();
                }
            }
        }

        /// <inheritdoc/>
        public override void OnServicesDiscovered(BluetoothGatt? gatt, GattStatus status)
        {
            lock (sync)
            {
                if (stopped) { return; }
                if (status != GattStatus.Success || gatt is null)
                {
                    services.TrySetException(status != GattStatus.Success
                        ? new IOException(string.Format(System.Globalization.CultureInfo.CurrentCulture, Properties.Resources.Get("BluetoothLeOperationFailedWithStatus"), status))
                        : Error("BluetoothLeOperationFailed"));
                    return;
                }
                try
                {
                    Guid[] result = (gatt.Services ?? Array.Empty<BluetoothGattService>())
                        .Select(service => Guid.TryParse(service.Uuid?.ToString(), out Guid uuid) ? uuid : Guid.Empty)
                        .Where(uuid => uuid != Guid.Empty).Distinct().OrderBy(uuid => uuid).ToArray();
                    services.TrySetResult(result);
                }
                catch (Exception error)
                {
                    services.TrySetException(new IOException(Properties.Resources.Get("BluetoothLeOperationFailed"), error));
                }
            }
        }
    }
}
#endif