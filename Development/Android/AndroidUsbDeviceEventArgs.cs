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
using Android.Hardware.Usb;

namespace Gurux.Serial;

/// <summary>Identifies a USB device attached to or detached from the Android USB host.</summary>
public sealed class AndroidUsbDeviceEventArgs : EventArgs
{
    /// <summary>Creates a USB topology notification without requesting device permission or opening it.</summary>
    /// <param name="device">The device reported by Android.</param>
    /// <param name="isAttached">Whether the device was attached rather than detached.</param>
    public AndroidUsbDeviceEventArgs(UsbDevice device, bool isAttached)
    {
        ArgumentNullException.ThrowIfNull(device);
        Device = device;
        IsAttached = isAttached;
    }
    /// <summary>Gets the device whose USB host presence changed.</summary>
    public UsbDevice Device { get; }
    /// <summary>Gets whether the device was attached; false means detached.</summary>
    public bool IsAttached { get; }
}
#endif
