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
using Gurux.Serial.Example.Properties;
using System.Globalization;
using Android.App;
using Android.Bluetooth;
using Android.Content;
using Android.Hardware.Usb;
using Gurux.Serial;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MauiApplication = Microsoft.Maui.Controls.Application;

namespace Gurux.Serial.Example.Platforms;

/// <summary>Selects the Android serial transport for this application.</summary>
public static class ExampleSerialConnection
{
    /// <summary>Stores only the latest USB attachment path until the next explicit Open action.</summary>
    private static string? _pendingUsbDeviceName;

    /// <summary>Records an Android USB attachment without opening the connection or retaining a Java device object.</summary>
    /// <param name="intent">The native activity intent, or null for a normal launcher start.</param>
    public static void HandleUsbIntent(Intent? intent)
    {
        if (intent?.Action != UsbManager.ActionUsbDeviceAttached)
        {
            return;
        }
        UsbDevice? device;
        if (OperatingSystem.IsAndroidVersionAtLeast(33))
        {
            device = intent.GetParcelableExtra(UsbManager.ExtraDevice, Java.Lang.Class.FromType(typeof(UsbDevice))) as UsbDevice;
        }
        else
        {
#pragma warning disable CA1422
            device = intent.GetParcelableExtra(UsbManager.ExtraDevice) as UsbDevice;
#pragma warning restore CA1422
        }
        if (device is not null)
        {
            Interlocked.Exchange(ref _pendingUsbDeviceName, device.DeviceName);
        }
    }

    /// <summary>Reads USB serial identity only when the operating system already permits access.</summary>
    /// <param name="manager">The Android USB service.</param>
    /// <param name="device">The attached device whose serial number may be readable.</param>
    /// <returns>The hardware serial number, or null when permission or serial information is unavailable.</returns>
    private static string? ReadUsbSerial(UsbManager manager, UsbDevice device)
    {
        if (!manager.HasPermission(device))
        {
            return null;
        }
        try
        {
            return device.SerialNumber;
        }
        catch (Java.Lang.SecurityException)
        {
            return null;
        }
    }
    /// <summary>Override to select the page belonging to the opening window.</summary>
    public static Func<Page?>? PageProvider { get; set; }

    /// <summary>Creates a factory that asks for the Android serial device when needed.</summary>
    /// <returns>The asynchronous Android serial stream factory.</returns>
    public static Func<SerialPortSettings, CancellationToken, ValueTask<Stream>> CreateFactory() => OpenAsync;

    /// <summary>Opens a selected supported USB serial, paired Bluetooth Classic or explicitly selected unpaired BLE transport.</summary>
    /// <param name="settings">The serial settings, updated with the selected device identifier.</param>
    /// <param name="cancellationToken">Cancels device selection, permissions and opening.</param>
    /// <returns>The opened platform serial stream.</returns>
    /// <exception cref="NotSupportedException">Android USB services are unavailable.</exception>
    /// <exception cref="InvalidOperationException">Bluetooth or the application window is unavailable.</exception>
    /// <exception cref="UnauthorizedAccessException">The required device permission is denied.</exception>
    public static async ValueTask<Stream> OpenAsync(SerialPortSettings settings, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var context = Platform.AppContext;
        string? attached = Interlocked.Exchange(ref _pendingUsbDeviceName, null);
        if (attached is not null
            && context.GetSystemService(Context.UsbService) is UsbManager attachedManager
            && attachedManager.DeviceList?.ContainsKey(attached) == true)
        {
            settings.PortName = "usb:" + attached;
        }
        string name = settings.PortName ?? "";
        if (name.StartsWith("ble:", StringComparison.OrdinalIgnoreCase))
        {
            await EnsureBluetoothPermissionAsync(cancellationToken);
            var adapter = ((BluetoothManager?)context.GetSystemService(Context.BluetoothService))?.Adapter;
            if (adapter?.IsEnabled != true) { throw new InvalidOperationException(ExampleResources.EnableBluetooth); }
            var device = adapter.GetRemoteDevice(name[4..]);
            var profile = settings.BluetoothProfile ?? BluetoothLeProfile.FromDeviceName(device?.Name);
            if (device is null || profile is null) { throw new NotSupportedException(ExampleResources.BluetoothProfileRequired); }
            settings.BluetoothProfile = profile;
            return await AndroidSerialTransports.BluetoothLe(context, device, profile)(settings, cancellationToken);
        }
        bool bluetooth = name.StartsWith("bluetooth:", StringComparison.OrdinalIgnoreCase);
        bool usb = name.StartsWith("usb:", StringComparison.OrdinalIgnoreCase);
        if (!bluetooth && !usb)
        {
            string choice = await ChooseAsync(ExampleResources.TransportTitle, new[] { ExampleResources.UsbTransport, ExampleResources.BluetoothTransport }, cancellationToken);
            bluetooth = choice == ExampleResources.BluetoothTransport;
        }
        if (bluetooth)
        {
            await EnsureBluetoothPermissionAsync(cancellationToken);
            var adapter = ((BluetoothManager?)context.GetSystemService(Context.BluetoothService))?.Adapter;
            if (adapter == null || !adapter.IsEnabled)
            {
                throw new InvalidOperationException(ExampleResources.EnableBluetooth);
            }
            var devices = adapter.BondedDevices?.OrderBy(it => it.Address).ToArray() ?? Array.Empty<BluetoothDevice>();
            var device = await SelectAsync(devices, name, "bluetooth:", it => it.Address ?? "", it => string.Format(CultureInfo.CurrentCulture, ExampleResources.BluetoothDeviceLabel, it.Name ?? ExampleResources.BluetoothDevice, it.Address), cancellationToken);
            settings.PortName = "bluetooth:" + device.Address;
            var profile = string.Equals(name, settings.PortName, StringComparison.OrdinalIgnoreCase)
                ? settings.BluetoothProfile ?? BluetoothLeProfile.FromDeviceName(device.Name)
                : BluetoothLeProfile.FromDeviceName(device.Name);
            settings.BluetoothProfile = profile;
            if (profile != null)
            {
                return await AndroidSerialTransports.BluetoothLe(context, device, profile)(settings, cancellationToken);
            }
            if (device.Type == BluetoothDeviceType.Le)
            {
                throw new NotSupportedException(ExampleResources.BluetoothProfileRequired);
            }
            return await AndroidSerialTransports.BluetoothRfcomm(device)(settings, cancellationToken);
        }
        else
        {
            var manager = (UsbManager?)context.GetSystemService(Context.UsbService)
                ?? throw new NotSupportedException(ExampleResources.UsbUnavailable);
            var devices = manager.DeviceList?.Values.OrderBy(it => it.DeviceName).ToArray() ?? Array.Empty<UsbDevice>();
            var resolved = ExampleUsbDeviceSelection.Resolve(settings, devices, it => it.DeviceName,
                it => it.VendorId, it => it.ProductId, it => ReadUsbSerial(manager, it));
            var device = resolved ?? await SelectAsync(devices, "", "usb:", it => it.DeviceName,
                    it => string.Format(CultureInfo.CurrentCulture, ExampleResources.UsbDeviceLabel, it.VendorId, it.ProductId, it.DeviceName), cancellationToken);
            await EnsureUsbPermissionAsync(context, manager, device, cancellationToken);
            if (resolved is not null && ExampleUsbDeviceSelection.Resolve(settings, new[] { device },
                it => it.DeviceName, it => it.VendorId, it => it.ProductId, it => ReadUsbSerial(manager, it)) is null)
            {
                // Permission can reveal a serial number that differs from the remembered adapter.
                device = await SelectAsync(devices, "", "usb:", it => it.DeviceName,
                    it => string.Format(CultureInfo.CurrentCulture, ExampleResources.UsbDeviceLabel, it.VendorId, it.ProductId, it.DeviceName), cancellationToken);
                await EnsureUsbPermissionAsync(context, manager, device, cancellationToken);
            }
            settings.PortName = "usb:" + device.DeviceName;
            ExampleUsbDeviceSelection.Remember(settings, device.DeviceName, device.VendorId, device.ProductId, ReadUsbSerial(manager, device));
            return await AndroidSerialTransports.Usb(manager, device)(settings, cancellationToken);
        }
    }

    /// <summary>Resolves a stored device identifier or asks the user to choose a device.</summary>
    /// <typeparam name="T">The platform device type.</typeparam>
    /// <param name="devices">The available devices.</param>
    /// <param name="requested">The stored device identifier.</param>
    /// <param name="prefix">The transport identifier prefix.</param>
    /// <param name="id">Reads a stable device identifier.</param>
    /// <param name="label">Formats a localized device label.</param>
    /// <param name="token">Cancels device selection.</param>
    /// <returns>The selected platform device.</returns>
    /// <exception cref="IOException">The selected device or all devices are unavailable.</exception>
    private static async Task<T> SelectAsync<T>(T[] devices, string requested, string prefix, Func<T, string> id, Func<T, string> label, CancellationToken token)
    {
        if (requested.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            string wanted = requested[prefix.Length..];
            foreach (var device in devices)
            {
                if (string.Equals(id(device), wanted, StringComparison.OrdinalIgnoreCase))
                {
                    return device;
                }
            }
            throw new IOException(string.Format(CultureInfo.CurrentCulture, ExampleResources.SelectedUnavailable, requested));
        }
        if (devices.Length == 0)
        {
            throw new IOException(ExampleResources.NoDevices);
        }
        string[] labels = devices.Select(label).ToArray();
        string selected = await ChooseAsync(ExampleResources.SelectDevice, labels, token);
        return devices[Array.IndexOf(labels, selected)];
    }

    /// <summary>Shows a device selection sheet on the application UI thread.</summary>
    /// <param name="title">The localized selection title.</param>
    /// <param name="choices">The localized available options.</param>
    /// <param name="token">Cancels selection.</param>
    /// <returns>The selected option.</returns>
    /// <exception cref="OperationCanceledException">Selection was canceled.</exception>
    private static async Task<string> ChooseAsync(string title, string[] choices, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        string? selected = await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            token.ThrowIfCancellationRequested();
            var page = PageProvider?.Invoke() ?? MauiApplication.Current?.Windows.FirstOrDefault()?.Page
                ?? throw new InvalidOperationException(ExampleResources.WindowNotReady);
            return await page.DisplayActionSheetAsync(title, ExampleResources.Cancel, null, choices);
        }).WaitAsync(token);
        token.ThrowIfCancellationRequested();
        if (selected == null || !choices.Contains(selected))
        {
            throw new OperationCanceledException(ExampleResources.SelectionCanceled, token);
        }
        return selected;
    }

    /// <summary>Requests the Bluetooth connection permission on supported Android versions.</summary>
    /// <param name="token">Cancels the permission request.</param>
    /// <returns>A task completing after permission is granted.</returns>
    /// <exception cref="UnauthorizedAccessException">The permission was denied.</exception>
    private static async Task EnsureBluetoothPermissionAsync(CancellationToken token)
    {
        if (OperatingSystem.IsAndroidVersionAtLeast(31))
        {
            var status = await MainThread.InvokeOnMainThreadAsync(() => Permissions.RequestAsync<BluetoothConnectPermission>()).WaitAsync(token);
            if (status != PermissionStatus.Granted)
            {
                throw new UnauthorizedAccessException(ExampleResources.BluetoothPermission);
            }
        }
        token.ThrowIfCancellationRequested();
    }

    /// <summary>Describes the runtime permission required for Bluetooth Classic connections.</summary>
    public sealed class BluetoothConnectPermission : Permissions.BasePlatformPermission
    {
        /// <inheritdoc/>
        public override (string androidPermission, bool isRuntime)[] RequiredPermissions =>
            new[] { ("android.permission.BLUETOOTH_CONNECT", true) };
    }

    /// <summary>Requests USB permission and verifies the operating system permission state.</summary>
    /// <param name="context">The Android application context.</param>
    /// <param name="manager">The Android USB manager.</param>
    /// <param name="device">The USB device to open.</param>
    /// <param name="token">Cancels the permission request.</param>
    /// <returns>A task completing after USB permission is granted.</returns>
    /// <exception cref="IOException">The permission request could not be created.</exception>
    /// <exception cref="UnauthorizedAccessException">USB permission was denied.</exception>
    private static async Task EnsureUsbPermissionAsync(Context context, UsbManager manager, UsbDevice device, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        if (manager.HasPermission(device))
        {
            return;
        }
        // Unique package-scoped action prevents another pending request from satisfying this one.
        string action = context.PackageName + ".USB_PERMISSION." + Guid.NewGuid().ToString("N");
        var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var receiver = new UsbPermissionReceiver(action, manager, device, completion);
        using var filter = new IntentFilter(action);
        if (OperatingSystem.IsAndroidVersionAtLeast(33))
        {
            context.RegisterReceiver(receiver, filter, ReceiverFlags.NotExported);
        }
        else
        {
#pragma warning disable CA1422
            context.RegisterReceiver(receiver, filter);
#pragma warning restore CA1422
        }
        try
        {
            using var intent = new Intent(action).SetPackage(context.PackageName);
            var flags = OperatingSystem.IsAndroidVersionAtLeast(23) ? PendingIntentFlags.Immutable : (PendingIntentFlags)0;
            using var pending = PendingIntent.GetBroadcast(context, 0, intent, flags)
                ?? throw new IOException(ExampleResources.UsbRequestFailed);
            using var registration = token.Register(() => completion.TrySetCanceled(token));
            try
            {
                token.ThrowIfCancellationRequested();
                manager.RequestPermission(device, pending);
                if (!await completion.Task.ConfigureAwait(false))
                {
                    throw new UnauthorizedAccessException(ExampleResources.UsbPermissionDenied);
                }
            }
            finally
            {
                pending.Cancel();
            }
        }
        finally
        {
            context.UnregisterReceiver(receiver);
        }
    }

    /// <summary>Completes a pending request after checking Android USB permission state.</summary>
    /// <param name="action">The unique permission request action.</param>
    /// <param name="manager">The USB service used to verify permission.</param>
    /// <param name="device">The USB device requiring permission.</param>
    /// <param name="completion">Receives the verified permission result.</param>
    private sealed class UsbPermissionReceiver(string action, UsbManager manager, UsbDevice device, TaskCompletionSource<bool> completion) : BroadcastReceiver
    {
        /// <inheritdoc/>
        public override void OnReceive(Context? context, Intent? intent)
        {
            if (intent?.Action == action)
            {
                // Check OS permission state directly; do not trust broadcast extras.
                completion.TrySetResult(manager.HasPermission(device));
            }
        }
    }
}
#endif
