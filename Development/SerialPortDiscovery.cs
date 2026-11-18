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
using System.Globalization;

namespace Gurux.Serial;

/// <summary>Checks known device identities without opening serial ports or initiating Bluetooth connections.</summary>
internal static class SerialPortDiscovery
{
    /// <summary>Compares digit sequences numerically so COM3 precedes COM10 independently of the UI culture.</summary>
    private static readonly StringComparer _naturalNameComparer = StringComparer.Create(CultureInfo.InvariantCulture, CompareOptions.NumericOrdering);

    /// <summary>Identifies conventional Linux UART, USB serial and embedded serial device-node families.</summary>
    private static readonly string[] _linuxSerialPrefixes = ["ttyS", "ttyUSB", "ttyACM", "ttyAMA", "ttymxc"];

    /// <summary>Combines runtime discovery with existing numbered Linux serial device nodes without opening them.</summary>
    /// <param name="enumeratePlatformPorts">Returns the runtime-discovered port names, including driver-specific families.</param>
    /// <param name="deviceDirectory">The device directory to inspect, normally /dev.</param>
    /// <returns>A naturally ordered, duplicate-free snapshot of runtime ports and conventional device nodes.</returns>
    /// <remarks>Device nodes may exist without working hardware or permission to open them. Inaccessible discovery sources are skipped independently; discovery never changes permissions or opens a serial connection.</remarks>
    /// <exception cref="ArgumentNullException">The runtime discovery callback or device directory is null.</exception>
    internal static string[] GetLinuxPortNames(Func<string[]> enumeratePlatformPorts, string deviceDirectory)
    {
        ArgumentNullException.ThrowIfNull(enumeratePlatformPorts);
        ArgumentNullException.ThrowIfNull(deviceDirectory);
        var ports = new HashSet<string>(StringComparer.Ordinal);
        try
        {
            ports.UnionWith(enumeratePlatformPorts());
        }
        catch (IOException)
        {
            // Existing device nodes remain discoverable when sysfs is unavailable.
        }
        catch (UnauthorizedAccessException)
        {
            // Listing a device does not require permission to open its transport.
        }
        try
        {
            foreach (string path in Directory.EnumerateFiles(deviceDirectory))
            {
                string name = Path.GetFileName(path);
                foreach (string prefix in _linuxSerialPrefixes)
                {
                    if (name.StartsWith(prefix, StringComparison.Ordinal) && name.Length > prefix.Length
                        && name.AsSpan(prefix.Length).IndexOfAnyExceptInRange('0', '9') < 0 && File.Exists(path))
                    {
                        ports.Add(path);
                        break;
                    }
                }
            }
        }
        catch (IOException)
        {
            // Keep runtime results if the device directory disappears or cannot be read.
        }
        catch (UnauthorizedAccessException)
        {
            // Keep runtime results when device-directory enumeration is not permitted.
        }
        return SortPortNames(ports);
    }
    /// <summary>Orders device names naturally with an ordinal tie-breaker for equivalent numeric names.</summary>
    /// <param name="portNames">The available device names.</param>
    /// <returns>An independent snapshot with numeric portions ordered by value.</returns>
    /// <exception cref="ArgumentNullException">The name sequence is null.</exception>
    internal static string[] SortPortNames(IEnumerable<string> portNames)
    {
        ArgumentNullException.ThrowIfNull(portNames);
        return portNames.OrderBy(name => name, _naturalNameComparer).ThenBy(name => name, StringComparer.Ordinal).ToArray();
    }

    /// <summary>Checks physical ports, native known devices or the recent explicit BLE discovery cache without starting a scan.</summary>
    /// <param name="portName">The selected port or native transport identifier.</param>
    /// <param name="cancellationToken">Cancels discovery.</param>
    /// <returns>Whether the device is currently enumerated; permission and platform failures return false.</returns>
    /// <exception cref="OperationCanceledException">The caller cancels discovery.</exception>
    internal static async Task<bool> IsAvailableAsync(string? portName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(portName))
        {
            return false;
        }
        cancellationToken.ThrowIfCancellationRequested();
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(TimeSpan.FromSeconds(3));
        try
        {
            if (portName.StartsWith("ble:", StringComparison.OrdinalIgnoreCase)
                && BluetoothLeDiscovery.GetKnownDevices().Any(device => MatchesBluetoothDevice(portName[4..], device)))
            {
                return true;
            }
#if !ANDROID && !IOS && !MACCATALYST
            bool classic = portName.StartsWith("bluetooth:", StringComparison.OrdinalIgnoreCase);
            bool lowEnergy = portName.StartsWith("ble:", StringComparison.OrdinalIgnoreCase);
            if (classic || lowEnergy)
            {
                string identifier = portName[(classic ? 10 : 4)..];
                if (string.IsNullOrWhiteSpace(identifier))
                {
                    return false;
                }
                if (OperatingSystem.IsLinux() && lowEnergy && identifier.StartsWith("/org/bluez/", StringComparison.Ordinal))
                {
                    return await LinuxBluetoothLe.IsDeviceKnownAsync(identifier, deadline.Token).WaitAsync(deadline.Token).ConfigureAwait(false);
                }
                var kind = classic ? BluetoothSerialTransport.Classic : BluetoothSerialTransport.LowEnergy;
                var devices = await BluetoothSerialTransports.GetDevicesAsync(kind, deadline.Token).WaitAsync(deadline.Token).ConfigureAwait(false);
                return devices.Any(device => MatchesBluetoothDevice(identifier, device));
            }
#endif
            var names = await Task.Run(GXSerial.GetPortNames, deadline.Token).WaitAsync(deadline.Token).ConfigureAwait(false);
            var comparer = OperatingSystem.IsWindows() || portName.StartsWith("bluetooth:", StringComparison.OrdinalIgnoreCase)
                ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
            return names.Contains(portName, comparer);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>Matches an opaque native identity or a separately supplied Bluetooth address.</summary>
    /// <param name="identifier">The selected device identifier without its transport prefix.</param>
    /// <param name="device">The native discovery result.</param>
    /// <returns>Whether either identity belongs to the selected device.</returns>
    internal static bool MatchesBluetoothDevice(string identifier, BluetoothSerialDevice device)
    {
        if (string.Equals(identifier, device.Id, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
        {
            return true;
        }
        if (Guid.TryParse(identifier, out Guid selectedUuid) && Guid.TryParse(device.Id, out Guid deviceUuid) && selectedUuid == deviceUuid)
        {
            return true;
        }
        string? selectedAddress = NormalizeAddress(identifier);
        return selectedAddress != null && (selectedAddress == NormalizeAddress(device.Address) || selectedAddress == NormalizeAddress(device.Id));
    }

    /// <summary>Normalizes only valid Bluetooth addresses, leaving opaque native IDs unparsed.</summary>
    /// <param name="address">An optional colon-separated, hyphen-separated or compact address.</param>
    /// <returns>A twelve-digit uppercase address, or null for other identifiers.</returns>
    private static string? NormalizeAddress(string? address)
    {
        if (address == null || address.Length is not (12 or 17))
        {
            return null;
        }
        if (address.Length == 17)
        {
            char separator = address[2];
            if (separator is not (':' or '-'))
            {
                return null;
            }
            for (int index = 2; index < address.Length; index += 3)
            {
                if (address[index] != separator)
                {
                    return null;
                }
            }
        }
        string? compact = address?.Replace(":", string.Empty).Replace("-", string.Empty);
        return compact?.Length == 12 && ulong.TryParse(compact, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out ulong value)
            ? value.ToString("X12", CultureInfo.InvariantCulture) : null;
    }
}
