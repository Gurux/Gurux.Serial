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
namespace Gurux.Serial;

/// <summary>Creates direct desktop Bluetooth stream factories for GXSerial.</summary>
/// <remarks>Classic connections resolve the Serial Port channel through SDP unless an explicit channel is supplied. BLE connections require a GATT serial profile; UART framing is applied only through explicit device-specific BLE configuration bytes or registered command generators.</remarks>
public static partial class BluetoothSerialTransports
{
    /// <summary>Creates an owned Bluetooth Low Energy notification and write stream.</summary>
    /// <remarks>Opening preserves the configured profile when its service is present. If the service is absent, the native backend can use the selected device's matching catalogue profile when that service is actually available.</remarks>
    /// <param name="deviceId">The Windows device ID, Bluetooth MAC address, or Linux BlueZ device path.</param>
    /// <param name="profile">The service, notification and output characteristic UUIDs.</param>
    /// <param name="operationTimeout">Maximum opening and complete-write duration; defaults to fifteen seconds.</param>
    /// <param name="receiveCapacity">Maximum queued notifications; overflow fails the stream instead of losing bytes.</param>
    /// <returns>A cancellable stream factory suitable for the GXSerial constructor.</returns>
    /// <exception cref="ArgumentException">The device identifier is blank.</exception>
    /// <exception cref="ArgumentNullException">The profile is null.</exception>
    /// <exception cref="PlatformNotSupportedException">The runtime or selected package target lacks the native backend.</exception>
    public static Func<SerialPortSettings, CancellationToken, ValueTask<Stream>> BluetoothLe(string deviceId, BluetoothLeProfile profile, TimeSpan? operationTimeout = null, int receiveCapacity = 256)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(deviceId);
        ArgumentNullException.ThrowIfNull(profile);
        var timeout = operationTimeout ?? TimeSpan.FromSeconds(15);
        return async (settings, cancellationToken) =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            BluetoothLeProfile configured = profile.ForSettings(settings);
            return await DesktopBluetoothLeStream.OpenAsync(
                (received, failed, token) => OpenLeAsync(deviceId, configured, settings, received, failed, token),
                timeout, receiveCapacity, cancellationToken).ConfigureAwait(false);
        };
    }

    /// <summary>Enumerates operating-system-known devices without starting a Bluetooth scan or pairing prompt.</summary>
    /// <param name="transport">The protocol to enumerate.</param>
    /// <param name="cancellationToken">Cancels native enumeration.</param>
    /// <returns>Known devices; an empty list means no matching devices are currently available.</returns>
    /// <remarks>Pair or discover devices using the operating system first. Windows BLE requires the Windows-specific library target.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The transport value is not a declared protocol.</exception>
    /// <exception cref="PlatformNotSupportedException">The runtime or selected package target lacks the native backend.</exception>
    public static async Task<BluetoothSerialDevice[]> GetDevicesAsync(BluetoothSerialTransport transport, CancellationToken cancellationToken = default)
    {
        if (!Enum.IsDefined(transport))
        {
            throw new ArgumentOutOfRangeException(nameof(transport));
        }
        cancellationToken.ThrowIfCancellationRequested();
#if !ANDROID && !IOS && !MACCATALYST
        if (OperatingSystem.IsLinux())
        {
            return await LinuxBluetoothLe.GetDevicesAsync(cancellationToken, transport).ConfigureAwait(false);
        }
        else if (OperatingSystem.IsWindows() && transport == BluetoothSerialTransport.Classic)
        {
            return await GetClassicDevicesAsync(cancellationToken).ConfigureAwait(false);
        }
#if WINDOWS
        else if (OperatingSystem.IsWindows())
        {
            var windowsDevices = await WindowsBluetoothLe.GetDevicesAsync(cancellationToken).ConfigureAwait(false);
            return windowsDevices.Select(device => new BluetoothSerialDevice(device.Id, device.Name, transport, device.Address)).ToArray();
        }
#endif
        else
#endif
        {
            throw new PlatformNotSupportedException(Properties.Resources.Get("DesktopBluetoothTargetRequired"));
        }
    }

    /// <summary>Selects the native GATT backend without introducing a windowing dependency.</summary>
    /// <param name="deviceId">The native identifier or Bluetooth address.</param>
    /// <param name="profile">The configured serial profile.</param>
    /// <param name="settings">Serial settings for evaluating a catalogue fallback after native service discovery.</param>
    /// <param name="received">Receives complete notification payloads.</param>
    /// <param name="failed">Reports native connection loss.</param>
    /// <param name="cancellationToken">Cancels connection establishment.</param>
    /// <returns>The owned native connection.</returns>
    private static Task<IBluetoothLeConnection> OpenLeAsync(string deviceId, BluetoothLeProfile profile, SerialPortSettings settings, Action<byte[]> received, Action<Exception> failed, CancellationToken cancellationToken)
    {
#if WINDOWS
        if (OperatingSystem.IsWindows())
        {
            return WindowsBluetoothLe.OpenAsync(deviceId, profile, received, failed, cancellationToken, settings);
        }
#endif
#if !ANDROID && !IOS && !MACCATALYST
        if (OperatingSystem.IsLinux())
        {
            return LinuxBluetoothLe.OpenAsync(deviceId, profile, received, failed, cancellationToken, settings);
        }
#endif
        throw new PlatformNotSupportedException(Properties.Resources.Get("DesktopBluetoothTargetRequired"));
    }
}
#endif
