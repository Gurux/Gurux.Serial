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

namespace Gurux.Serial;

/// <summary>Runs an explicitly requested, time-limited native BLE scan without pairing or opening devices.</summary>
public static class BluetoothLeDiscovery
{
    /// <summary>Bounds both per-scan results and the recent device cache.</summary>
    private const int MaximumDevices = 256;
    /// <summary>Protects recently observed device identities.</summary>
    private static readonly object _sync = new();
    /// <summary>Stores recent advertisements for availability checks after an explicit scan.</summary>
    private static readonly Dictionary<string, (BluetoothSerialDevice Device, long Seen)> _known = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Gets whether this build and operating system provide a native BLE discovery backend.</summary>
    /// <remarks>This does not start discovery, request permissions, or test whether the radio is enabled.</remarks>
    public static bool IsSupported
    {
        get
        {
#if ANDROID || IOS || MACCATALYST
            return true;
#elif WINDOWS
            return OperatingSystem.IsWindows() || OperatingSystem.IsLinux();
#else
            return OperatingSystem.IsLinux();
#endif
        }
    }

    /// <summary>Gets recently advertised devices without starting a scan or requesting permissions.</summary>
    /// <returns>A detached snapshot of devices seen during the last five minutes.</returns>
    public static BluetoothSerialDevice[] GetKnownDevices()
    {
        lock (_sync)
        {
            long now = Environment.TickCount64;
            foreach (string id in _known.Where(item => now - item.Value.Seen > 300000).Select(item => item.Key).ToArray())
            {
                _known.Remove(id);
            }
            return _known.Values.Select(item => item.Device).OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase).ToArray();
        }
    }

    /// <summary>Scans for nearby BLE devices, returning partial results when the requested duration expires.</summary>
    /// <param name="duration">Scan duration, defaulting to ten seconds and limited to one minute.</param>
    /// <param name="deviceFound">Optional callback for new or renamed devices; callbacks may run on native background threads.</param>
    /// <param name="cancellationToken">Cancels discovery, including native scanner cleanup.</param>
    /// <returns>At most 256 distinct advertised devices; no pairing or connection is started.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The duration is not positive or exceeds one minute.</exception>
    /// <exception cref="OperationCanceledException">The caller cancels discovery.</exception>
    /// <exception cref="IOException">The native Bluetooth scanner is unavailable or fails.</exception>
    /// <exception cref="PlatformNotSupportedException">No native BLE discovery backend is available.</exception>
    public static Task<BluetoothSerialDevice[]> ScanAsync(TimeSpan? duration = null, Action<BluetoothSerialDevice>? deviceFound = null, CancellationToken cancellationToken = default)
    {
        return ScanAsync(ScanNativeAsync, duration ?? TimeSpan.FromSeconds(10), deviceFound, cancellationToken);
    }

    /// <summary>Collects bounded results while preserving timeout, cancellation and late-callback isolation.</summary>
    /// <param name="scan">The native scan lifetime, which must release its scanner before completing.</param>
    /// <param name="duration">Maximum discovery duration.</param>
    /// <param name="deviceFound">Optional notification for accepted discovery updates.</param>
    /// <param name="cancellationToken">Cancels the caller's scan.</param>
    /// <returns>The detached discovery snapshot after native cleanup.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The duration is outside the supported range.</exception>
    /// <exception cref="OperationCanceledException">The caller cancels discovery.</exception>
    internal static async Task<BluetoothSerialDevice[]> ScanAsync(Func<Action<BluetoothSerialDevice>, CancellationToken, Task> scan, TimeSpan duration, Action<BluetoothSerialDevice>? deviceFound, CancellationToken cancellationToken)
    {
        if (duration <= TimeSpan.Zero || duration > TimeSpan.FromMinutes(1))
        {
            throw new ArgumentOutOfRangeException(nameof(duration));
        }
        cancellationToken.ThrowIfCancellationRequested();
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(duration);
        var results = new Dictionary<string, BluetoothSerialDevice>(StringComparer.OrdinalIgnoreCase);
        bool active = true;
        try
        {
            await scan(device =>
            {
                lock (results)
                {
                    if (!active || deadline.IsCancellationRequested || string.IsNullOrWhiteSpace(device.Id) || device.Transport != BluetoothSerialTransport.LowEnergy)
                    {
                        return;
                    }
                    if (results.TryGetValue(device.Id, out var previous))
                    {
                        device = previous.Merge(device);
                        if (previous.Name == device.Name && previous.Address == device.Address
                            && previous.ServiceUuids.SequenceEqual(device.ServiceUuids))
                        {
                            return;
                        }
                    }
                    if (!results.ContainsKey(device.Id) && results.Count >= MaximumDevices) { return; }
                    results[device.Id] = device;
                    lock (_sync)
                    {
                        if (!_known.ContainsKey(device.Id) && _known.Count >= MaximumDevices)
                        {
                            _known.Remove(_known.MinBy(item => item.Value.Seen).Key);
                        }
                        _known[device.Id] = (device, Environment.TickCount64);
                    }
                    deviceFound?.Invoke(device);
                }
            }, deadline.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested && deadline.IsCancellationRequested)
        {
        }
        finally
        {
            lock (results) { active = false; }
        }
        cancellationToken.ThrowIfCancellationRequested();
        lock (results) { return results.Values.OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase).ToArray(); }
    }

    /// <summary>Selects the native scanner without introducing any UI dependency.</summary>
    /// <param name="deviceFound">Receives advertised device identities.</param>
    /// <param name="cancellationToken">Ends the native scan lifetime.</param>
    /// <returns>The scanner lifetime, including cleanup.</returns>
    /// <exception cref="PlatformNotSupportedException">No backend is available on this target.</exception>
    private static Task ScanNativeAsync(Action<BluetoothSerialDevice> deviceFound, CancellationToken cancellationToken)
    {
#if ANDROID
        return AndroidBluetoothLeDiscovery.ScanAsync(deviceFound, cancellationToken);
#elif IOS || MACCATALYST
        return IosBluetoothLeDiscovery.ScanAsync(deviceFound, cancellationToken);
#else
#if WINDOWS
        if (OperatingSystem.IsWindows()) { return WindowsBluetoothLeDiscovery.ScanAsync(deviceFound, cancellationToken); }
#endif
        if (OperatingSystem.IsLinux()) { return LinuxBluetoothLeDiscovery.ScanAsync(deviceFound, cancellationToken); }
        throw new PlatformNotSupportedException(Properties.Resources.Get("DesktopBluetoothTargetRequired"));
#endif
    }
}
