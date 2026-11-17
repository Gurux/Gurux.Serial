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
using Android.Content;
using Android.Hardware.Usb;

namespace Gurux.Serial;

/// <summary>Monitors Android USB host attachment events until disposed.</summary>
/// <remarks>This object does not request USB permission or open devices. Events are raised on the Android
/// broadcast thread; UI consumers must dispatch changes onto their own UI thread. Dispose when no longer needed.</remarks>
public sealed class AndroidUsbDeviceWatcher : IDisposable
{
    /// <summary>Owns the application-level receiver registration without retaining an activity.</summary>
    private readonly Context context;
    /// <summary>Receives USB topology broadcasts for this watcher.</summary>
    private readonly UsbReceiver receiver;
    /// <summary>Prevents notifications after receiver unregistration.</summary>
    private int disposed;
    /// <summary>Occurs when Android reports a USB device attachment or detachment.</summary>
    public event EventHandler<AndroidUsbDeviceEventArgs>? DevicesChanged;

    /// <summary>Starts watching USB devices using the application context.</summary>
    /// <param name="context">An Android context used to register the application-level receiver.</param>
    /// <exception cref="ArgumentNullException">The context is null.</exception>
    public AndroidUsbDeviceWatcher(Context context)
    {
        ArgumentNullException.ThrowIfNull(context);
        this.context = context.ApplicationContext ?? context;
        receiver = new UsbReceiver(this);
        using var filter = new IntentFilter(UsbManager.ActionUsbDeviceAttached);
        filter.AddAction(UsbManager.ActionUsbDeviceDetached);
        if (OperatingSystem.IsAndroidVersionAtLeast(33))
        {
            this.context.RegisterReceiver(receiver, filter, ReceiverFlags.NotExported);
        }
        else
        {
            this.context.RegisterReceiver(receiver, filter);
        }
    }

    /// <summary>Unregisters the receiver and stops delivering device change notifications.</summary>
    public void Dispose()
    {
        if (Interlocked.Exchange(ref disposed, 1) == 0)
        {
            context.UnregisterReceiver(receiver);
            receiver.Dispose();
            DevicesChanged = null;
        }
    }

    /// <summary>Publishes a topology change after checking the current registration lifetime.</summary>
    /// <param name="device">The USB device reported by Android.</param>
    /// <param name="isAttached">Whether the device was attached.</param>
    private void Notify(UsbDevice device, bool isAttached)
    {
        if (Volatile.Read(ref disposed) == 0)
        {
            DevicesChanged?.Invoke(this, new AndroidUsbDeviceEventArgs(device, isAttached));
        }
    }

    /// <summary>Converts Android USB broadcasts to library-level topology notifications.</summary>
    /// <param name="owner">The watcher receiving topology notifications.</param>
    private sealed class UsbReceiver(AndroidUsbDeviceWatcher owner) : BroadcastReceiver
    {
        /// <inheritdoc/>
        public override void OnReceive(Context? context, Intent? intent)
        {
            if (intent?.Action is not (UsbManager.ActionUsbDeviceAttached or UsbManager.ActionUsbDeviceDetached))
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
#pragma warning disable CS0618 // Required compatibility overload on Android versions below API 33.
                device = intent.GetParcelableExtra(UsbManager.ExtraDevice) as UsbDevice;
#pragma warning restore CS0618
            }
            if (device != null)
            {
                owner.Notify(device, intent.Action == UsbManager.ActionUsbDeviceAttached);
            }
        }
    }
}
#endif
