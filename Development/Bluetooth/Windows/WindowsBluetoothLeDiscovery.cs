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
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Foundation;
namespace Gurux.Serial;

/// <summary>Receives active Windows BLE advertisements without pairing or opening a GATT connection.</summary>
internal static class WindowsBluetoothLeDiscovery
{
    /// <summary>Reports native Bluetooth addresses, advertised names and advertised service UUIDs until canceled.</summary>
    /// <param name="discovered">Receives advertisements, including later scan-response name updates.</param>
    /// <param name="cancellationToken">Ends discovery and releases the native watcher.</param>
    /// <returns>The asynchronous watcher lifetime.</returns>
    /// <exception cref="IOException">Windows rejects scanning or terminates the watcher unexpectedly.</exception>
    /// <exception cref="OperationCanceledException">The scan is canceled.</exception>
    internal static async Task ScanAsync(Action<BluetoothSerialDevice> discovered, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var watcher = new BluetoothLEAdvertisementWatcher { ScanningMode = BluetoothLEScanningMode.Active };
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var stopped = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        object gate = new();
        bool active = true;
        TypedEventHandler<BluetoothLEAdvertisementWatcher, BluetoothLEAdvertisementReceivedEventArgs> received = (_, advertisement) =>
        {
            lock (gate)
            {
                if (!active)
                {
                    return;
                }
                try
                {
                    string digits = advertisement.BluetoothAddress.ToString("X12", CultureInfo.InvariantCulture);
                    string address = string.Join(":", Enumerable.Range(0, 6).Select(index => digits.Substring(index * 2, 2)));
                    string name = advertisement.Advertisement.LocalName;
                    discovered(new BluetoothSerialDevice(address, string.IsNullOrWhiteSpace(name) ? address : name,
                        BluetoothSerialTransport.LowEnergy, address, advertisement.Advertisement.ServiceUuids));
                }
                catch (Exception error)
                {
                    active = false;
                    completion.TrySetException(new IOException(Properties.Resources.Get("BluetoothLeScanFailed"), error));
                }
            }
        };
        TypedEventHandler<BluetoothLEAdvertisementWatcher, BluetoothLEAdvertisementWatcherStoppedEventArgs> ended = (_, _) =>
        {
            stopped.TrySetResult();
            completion.TrySetException(new IOException(Properties.Resources.Get("BluetoothLeScanFailed")));
        };
        watcher.Received += received;
        watcher.Stopped += ended;
        try
        {
            watcher.Start();
            await completion.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception error) when (error is not OperationCanceledException && error is not IOException)
        {
            throw new IOException(Properties.Resources.Get("BluetoothLeScanFailed"), error);
        }
        finally
        {
            lock (gate)
            {
                active = false;
            }
            watcher.Received -= received;
            try
            {
                watcher.Stop();
                await stopped.Task.WaitAsync(TimeSpan.FromSeconds(2)).ConfigureAwait(false);
            }
            catch (Exception)
            {
                // Detach callbacks even if Windows has already aborted the watcher.
            }
            finally
            {
                watcher.Stopped -= ended;
                // Observe a simultaneous native failure when cancellation won the scan lifetime race.
                _ = completion.Task.Exception;
            }
        }
    }
}
#endif
