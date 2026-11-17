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
using Windows.Storage.Streams;

namespace Gurux.Serial;

/// <summary>Owns a Windows GATT service, notification subscription and serialized output.</summary>
internal sealed class WindowsBluetoothLeConnection : IBluetoothLeConnection
{
    /// <summary>Owns the native peer device.</summary>
    private readonly BluetoothLEDevice device;
    /// <summary>Owns every service returned during discovery.</summary>
    private readonly List<GattDeviceService> services;
    /// <summary>Publishes incoming values.</summary>
    private readonly GattCharacteristic read;
    /// <summary>Accepts serial output.</summary>
    private readonly GattCharacteristic write;
    /// <summary>Delivers independent received buffers.</summary>
    private readonly Action<byte[]> received;
    /// <summary>Reports an unexpected transport failure.</summary>
    private readonly Action<Exception> failed;
    /// <summary>Serializes native writes and disposal.</summary>
    private readonly SemaphoreSlim gate = new(1, 1);
    /// <summary>Signals that close has begun, suppressing further callbacks.</summary>
    private int disposed;
    /// <summary>Prevents repeated disconnect reports.</summary>
    private int failureReported;
    /// <summary>Tracks whether notification setup was attempted and needs rollback.</summary>
    private bool subscriptionAttempted;

    /// <summary>Initializes ownership of the discovered native objects.</summary>
    /// <param name="device">Peer device.</param>
    /// <param name="services">Owned service handles.</param>
    /// <param name="read">Notification characteristic.</param>
    /// <param name="write">Output characteristic.</param>
    /// <param name="received">Receives payloads.</param>
    /// <param name="failed">Receives failures.</param>
    internal WindowsBluetoothLeConnection(BluetoothLEDevice device, List<GattDeviceService> services, GattCharacteristic read, GattCharacteristic write, Action<byte[]> received, Action<Exception> failed)
    {
        this.device = device;
        this.services = services;
        this.read = read;
        this.write = write;
        this.received = received;
        this.failed = failed;
    }

    /// <inheritdoc/>
    public int MaxWriteSize => 20;

    /// <summary>Checks characteristic capabilities, subscribes, and applies optional configuration.</summary>
    /// <param name="profile">Serial profile configuration.</param>
    /// <param name="cancellationToken">Cancels GATT operations.</param>
    /// <returns>A task completing when the serial profile is ready.</returns>
    /// <exception cref="IOException">A required capability or GATT operation fails.</exception>
    internal async Task InitializeAsync(BluetoothLeProfile profile, CancellationToken cancellationToken)
    {
        GetWriteOption(write);
        var properties = read.CharacteristicProperties;
        var notification = (properties & GattCharacteristicProperties.Notify) != 0
            ? GattClientCharacteristicConfigurationDescriptorValue.Notify
            : (properties & GattCharacteristicProperties.Indicate) != 0
                ? GattClientCharacteristicConfigurationDescriptorValue.Indicate
                : GattClientCharacteristicConfigurationDescriptorValue.None;
        if (notification == GattClientCharacteristicConfigurationDescriptorValue.None)
        {
            throw new IOException(Properties.Resources.Get("BluetoothLeNotificationsNotSupported"));
        }
        read.ValueChanged += OnValueChanged;
        device.ConnectionStatusChanged += OnConnectionStatusChanged;
        subscriptionAttempted = true;
        CheckStatus(await read.WriteClientCharacteristicConfigurationDescriptorAsync(notification).AsTask(cancellationToken).ConfigureAwait(false));
        if (profile.ConfigurationCharacteristic.HasValue && profile.ConfigurationValue is byte[] configuration)
        {
            var characteristic = await WindowsBluetoothLe.GetCharacteristicAsync(services[0], profile.ConfigurationCharacteristic.Value, cancellationToken).ConfigureAwait(false);
            if (configuration.Length > 512)
            {
                throw new IOException(Properties.Resources.Get("WindowsBluetoothLeConfigurationTooLarge"));
            }
            CheckStatus((await characteristic.WriteValueWithResultAsync(configuration.AsBuffer(), GetWriteOption(characteristic)).AsTask(cancellationToken).ConfigureAwait(false)).Status);
        }
        cancellationToken.ThrowIfCancellationRequested();
        if (device.ConnectionStatus != BluetoothConnectionStatus.Connected || Volatile.Read(ref failureReported) != 0)
        {
            throw new IOException(Properties.Resources.Get("BluetoothLeDisconnected"));
        }
    }

    /// <inheritdoc/>
    public async ValueTask WriteAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken)
    {
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            ObjectDisposedException.ThrowIf(Volatile.Read(ref disposed) != 0, this);
            var option = GetWriteOption(write);
            for (int offset = 0; offset < data.Length; offset += MaxWriteSize)
            {
                byte[] chunk = data.Slice(offset, Math.Min(MaxWriteSize, data.Length - offset)).ToArray();
                CheckStatus((await write.WriteValueWithResultAsync(chunk.AsBuffer(), option).AsTask(cancellationToken).ConfigureAwait(false)).Status);
            }
        }
        finally
        {
            gate.Release();
        }
    }

    /// <summary>Selects acknowledged writes where available, otherwise unacknowledged writes.</summary>
    /// <param name="characteristic">Destination characteristic.</param>
    /// <returns>A supported write mode.</returns>
    /// <exception cref="IOException">The characteristic cannot be written.</exception>
    private static GattWriteOption GetWriteOption(GattCharacteristic characteristic)
    {
        if ((characteristic.CharacteristicProperties & GattCharacteristicProperties.Write) != 0)
        {
            return GattWriteOption.WriteWithResponse;
        }
        if ((characteristic.CharacteristicProperties & GattCharacteristicProperties.WriteWithoutResponse) != 0)
        {
            return GattWriteOption.WriteWithoutResponse;
        }
        throw new IOException(Properties.Resources.Get("BluetoothLeWriteNotSupported"));
    }

    /// <summary>Converts a failed Windows GATT result into a localized transport error.</summary>
    /// <param name="status">Native communication result.</param>
    /// <exception cref="IOException">The operation was unsuccessful.</exception>
    internal static void CheckStatus(GattCommunicationStatus status)
    {
        if (status != GattCommunicationStatus.Success)
        {
            throw new IOException(string.Format(CultureInfo.CurrentCulture, Properties.Resources.Get("BluetoothLeOperationFailedWithStatus"), status));
        }
    }

    /// <summary>Copies a notification before delivering it to shared receive buffering.</summary>
    /// <param name="sender">Notification characteristic.</param>
    /// <param name="args">Native received value.</param>
    private void OnValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
    {
        if (Volatile.Read(ref disposed) != 0)
        {
            return;
        }
        try
        {
            received(args.CharacteristicValue.ToArray());
        }
        catch (Exception ex)
        {
            ReportFailure(ex);
        }
    }

    /// <summary>Reports a remote disconnect while the connection remains owned by the caller.</summary>
    /// <param name="sender">Peer whose state changed.</param>
    /// <param name="args">Unused Windows event payload.</param>
    private void OnConnectionStatusChanged(BluetoothLEDevice sender, object args)
    {
        if (sender.ConnectionStatus == BluetoothConnectionStatus.Disconnected)
        {
            ReportFailure(new IOException(Properties.Resources.Get("BluetoothLeDisconnected")));
        }
    }

    /// <summary>Delivers at most one failure while protecting the native event dispatcher from callback exceptions.</summary>
    /// <param name="exception">Transport or receive callback failure.</param>
    private void ReportFailure(Exception exception)
    {
        if (Volatile.Read(ref disposed) == 0 && Interlocked.Exchange(ref failureReported, 1) == 0)
        {
            try
            {
                failed(exception);
            }
            catch
            {
                // Consumer failure handlers must not escape a native event dispatcher.
            }
        }
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref disposed, 1) != 0)
        {
            return;
        }
        read.ValueChanged -= OnValueChanged;
        device.ConnectionStatusChanged -= OnConnectionStatusChanged;
        bool entered = false;
        try
        {
            // Native cancellation is advisory; closing must also release a stalled write's handles.
            entered = await gate.WaitAsync(TimeSpan.FromSeconds(2)).ConfigureAwait(false);
            if (entered && subscriptionAttempted)
            {
                try
                {
                    using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                    var unsubscribe = read.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.None).AsTask(timeout.Token);
                    _ = unsubscribe.ContinueWith(task => { _ = task.Exception; }, CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
                    await unsubscribe.WaitAsync(timeout.Token).ConfigureAwait(false);
                }
                catch
                {
                    // Release all local handles even when the peer cannot acknowledge unsubscribe.
                }
            }
        }
        finally
        {
            foreach (var service in services)
            {
                service.Dispose();
            }
            device.Dispose();
            if (entered)
            {
                gate.Release();
            }
        }
    }
}
#endif
