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
using Android.Hardware.Usb;

namespace Gurux.Serial.Usb.Tests;

/// <summary>Records configuration requests and supplies a configurable CH34x version response.</summary>
internal sealed class ScriptedUsbConnection : UsbDeviceConnection
{
    /// <summary>Gets or sets the first byte returned by a successful version request.</summary>
    internal byte Version { get; set; }
    /// <summary>Gets or sets the return count for a two-byte version read.</summary>
    internal int VersionResult { get; set; } = 2;
    /// <summary>Gets or sets the zero-based request position at which the transfer fails.</summary>
    internal int FailAt { get; set; } = -1;
    /// <summary>Gets the control requests recorded before completion or failure.</summary>
    internal List<(int Type, int Request, int Value, int Index, int Length)> Requests { get; } = [];

    /// <inheritdoc/>
    public override int ControlTransfer(UsbAddressing type, int request, int value, int index, byte[]? data, int length, int timeout)
    {
        Requests.Add(((int)type, request, value, index, length));
        if (Requests.Count - 1 == FailAt)
        {
            return -1;
        }
        if ((int)type == 0xc0 && request == 0x5f)
        {
            data![0] = Version;
            data[1] = 0xff;
            return VersionResult;
        }
        return length;
    }
}
