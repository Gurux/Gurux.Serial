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
namespace Android.Hardware.Usb;

/// <summary>Replaces the Android device boundary while executing the production configuration code.</summary>
internal abstract class UsbDeviceConnection
{
    /// <summary>Executes a scripted USB control request.</summary>
    /// <param name="type">The request direction and recipient flags.</param>
    /// <param name="request">The control request number.</param>
    /// <param name="value">The request value.</param>
    /// <param name="index">The request index.</param>
    /// <param name="data">The optional response or output buffer.</param>
    /// <param name="length">The requested transfer length.</param>
    /// <param name="timeout">The finite timeout in milliseconds.</param>
    /// <returns>The scripted number of transferred bytes, or a negative failure result.</returns>
    public abstract int ControlTransfer(UsbAddressing type, int request, int value, int index, byte[]? data, int length, int timeout);

    /// <summary>Provides descriptor bytes for configurations that inspect USB device revisions.</summary>
    /// <returns>No descriptor data for these CH34x scenarios.</returns>
    public byte[]? GetRawDescriptors() => null;
}
