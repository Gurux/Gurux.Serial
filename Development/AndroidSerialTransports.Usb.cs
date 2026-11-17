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
/// <summary>Provides Android serial device discovery and owned platform transport factories.</summary>
public static partial class AndroidSerialTransports
{
    /// <summary>Creates a USB factory using a detected or explicitly selected serial chipset.</summary>
    /// <remarks>Permission must be granted before opening. Vendor port numbers index interfaces with bulk IN and OUT endpoints.
    /// CDC ACM supports one port. Vendor adapters support manual DTR/RTS and reject automatic handshaking.
    /// Explicit selection permits rebranded devices but does not guarantee compatibility with newer revisions.</remarks>
    /// <param name="manager">The Android USB service.</param>
    /// <param name="device">The USB device to open.</param>
    /// <param name="chipset">The protocol selection or automatic identifier detection.</param>
    /// <param name="port">The zero-based bulk interface index.</param>
    /// <returns>A factory that claims, configures and owns the USB connection when invoked.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The port or chipset selection is invalid.</exception>
    /// <exception cref="NotSupportedException">The device or serial options are unsupported.</exception>
    public static Func<SerialPortSettings, CancellationToken, ValueTask<Stream>> Usb(
        UsbManager manager, UsbDevice device, AndroidUsbChipset chipset = AndroidUsbChipset.Auto, int port = 0)
    {
        ArgumentNullException.ThrowIfNull(manager);
        ArgumentNullException.ThrowIfNull(device);
        if (port < 0 || !Enum.IsDefined(chipset))
        {
            throw new ArgumentOutOfRangeException(nameof(port));
        }
        if (chipset == AndroidUsbChipset.Auto)
        {
            chipset = AndroidUsbProtocol.Detect(device.VendorId, device.ProductId);
        }
        if (chipset == AndroidUsbChipset.Auto || chipset == AndroidUsbChipset.CdcAcm)
        {
            if (port != 0)
            {
                throw new ArgumentOutOfRangeException(nameof(port));
            }
            return UsbCdcAcm(manager, device);
        }
        return (settings, token) => new ValueTask<Stream>(Task.Run<Stream>(() =>
            AndroidUsbStream.Open(manager, device, chipset, port, settings, token), token));
    }
}
#endif
