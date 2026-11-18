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
using Android.Bluetooth.LE;
using Android.Content;
using Android.Content.PM;

namespace Gurux.Serial;

/// <summary>Owns one explicitly requested Android LE advertisement scan.</summary>
internal static class AndroidBluetoothLeDiscovery
{
    /// <summary>Scans until cancellation or a native failure, always stopping its own callback.</summary>
    /// <param name="deviceFound">Receives advertised connectable devices including service-list and service-data UUID metadata.</param>
    /// <param name="cancellationToken">Stops discovery.</param>
    /// <returns>The lifetime of the owned native scan.</returns>
    /// <exception cref="UnauthorizedAccessException">Required Bluetooth or location permissions are missing.</exception>
    /// <exception cref="IOException">Bluetooth is disabled, unavailable or fails discovery.</exception>
    internal static async Task ScanAsync(Action<BluetoothSerialDevice> deviceFound, CancellationToken cancellationToken)
    {
        var context = Android.App.Application.Context;
        string[] permissions = OperatingSystem.IsAndroidVersionAtLeast(31)
            ? [Android.Manifest.Permission.BluetoothScan, Android.Manifest.Permission.BluetoothConnect]
            : [Android.Manifest.Permission.AccessFineLocation];
        if (permissions.Any(permission => context.CheckCallingOrSelfPermission(permission) != Permission.Granted))
        {
            throw new UnauthorizedAccessException(Properties.Resources.Get("BluetoothLeScanPermissionRequired"));
        }
        cancellationToken.ThrowIfCancellationRequested();
        var manager = context.GetSystemService(Context.BluetoothService) as BluetoothManager;
        var adapter = manager?.Adapter;
        var scanner = adapter?.BluetoothLeScanner;
        if (adapter?.IsEnabled != true || scanner is null)
        {
            throw new IOException(Properties.Resources.Get("BluetoothUnavailable"));
        }
        var failed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var callback = new DiscoveryCallback(deviceFound, failed);
        using var settings = new ScanSettings.Builder().SetScanMode(Android.Bluetooth.LE.ScanMode.LowLatency)!.Build();
        bool started = false;
        try
        {
            scanner.StartScan(null, settings, callback);
            started = true;
            await failed.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            callback.Stop();
            try { if (started) { scanner.StopScan(callback); } }
            finally { _ = failed.Task.Exception; }
        }
    }

    /// <summary>Contains native callbacks and prevents late advertisements from reaching a completed scan.</summary>
    private sealed class DiscoveryCallback : ScanCallback
    {
        /// <summary>Publishes device metadata without retaining native device handles.</summary>
        private readonly Action<BluetoothSerialDevice> _found;
        /// <summary>Signals native or observer failures to the scan owner.</summary>
        private readonly TaskCompletionSource _failed;
        /// <summary>Rejects callbacks once cleanup begins.</summary>
        private int _stopped;
        /// <summary>Orders callback failures against scan shutdown.</summary>
        private readonly object _sync = new();

        /// <summary>Creates an observer for one scan.</summary>
        /// <param name="found">Receives discovered device identities.</param>
        /// <param name="failed">Receives failures from native callbacks.</param>
        internal DiscoveryCallback(Action<BluetoothSerialDevice> found, TaskCompletionSource failed) { this._found = found; this._failed = failed; }
        /// <summary>Prevents future callbacks from publishing devices.</summary>
        internal void Stop() { lock (_sync) { Interlocked.Exchange(ref _stopped, 1); } }
        /// <summary>Reports a callback failure only while this scan still owns its observer.</summary>
        /// <param name="error">The native or observer failure.</param>
        private void Fail(Exception error)
        {
            lock (_sync) { if (Volatile.Read(ref _stopped) == 0) { _failed.TrySetException(error); } }
        }
        /// <inheritdoc/>
        public override void OnScanResult(ScanCallbackType callbackType, ScanResult? result)
        {
            if (Volatile.Read(ref _stopped) != 0 || result?.Device is not BluetoothDevice device) { return; }
            try
            {
                if (OperatingSystem.IsAndroidVersionAtLeast(26) && !result.IsConnectable) { return; }
                string? address = device.Address;
                if (string.IsNullOrWhiteSpace(address)) { return; }
                string name = result.ScanRecord?.DeviceName ?? device.Name ?? address;
                Guid[] serviceUuids = (result.ScanRecord?.ServiceUuids?.AsEnumerable() ?? Enumerable.Empty<Android.OS.ParcelUuid>())
                    .Concat(result.ScanRecord?.ServiceData?.Keys.AsEnumerable() ?? Enumerable.Empty<Android.OS.ParcelUuid>())
                    .Select(value => Guid.TryParse(value?.Uuid?.ToString(), out Guid uuid) ? uuid : Guid.Empty)
                    .Where(uuid => uuid != Guid.Empty).Distinct().ToArray();
                _found(new BluetoothSerialDevice(address, name, BluetoothSerialTransport.LowEnergy, address, serviceUuids));
            }
            catch (Exception error) { Fail(error); }
        }
        /// <inheritdoc/>
        public override void OnBatchScanResults(IList<ScanResult>? results)
        {
            if (results is not null) { foreach (var result in results) { OnScanResult(ScanCallbackType.AllMatches, result); } }
        }
        /// <inheritdoc/>
        public override void OnScanFailed(ScanFailure errorCode) =>
            Fail(new IOException(Properties.Resources.Get("BluetoothLeScanFailed")));
    }
}
#endif
