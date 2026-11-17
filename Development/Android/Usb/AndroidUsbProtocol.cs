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
using System.IO.Ports;

namespace Gurux.Serial;
/// <summary>Encodes serial framing values and filters FTDI packet headers.</summary>
internal static class AndroidUsbProtocol
{
    /// <summary>Detects conservative default chipset identifiers.</summary>
    /// <param name="vendor">The USB vendor identifier.</param>
    /// <param name="product">The USB product identifier.</param>
    /// <returns>The detected chipset or Auto when CDC descriptor inspection is needed.</returns>
    internal static AndroidUsbChipset Detect(int vendor, int product)
    {
        if ((vendor == 0x0403 && product is 0x6001 or 0x6010 or 0x6011 or 0x6014 or 0x6015) || (vendor == 403 && product == 6001))
        {
            return AndroidUsbChipset.Ftdi;
        }
        if (vendor == 0x10c4 && product is 0xea60 or 0xea61 or 0xea70 or 0xea71)
        {
            return AndroidUsbChipset.Cp210x;
        }
        if (vendor == 0x1a86 && product is 0x7523 or 0x5523)
        {
            return AndroidUsbChipset.Ch34x;
        }
        if ((vendor == 0x067b && product == 0x2303) || (vendor == 0x0557 && product == 0x2008))
        {
            return AndroidUsbChipset.Prolific;
        }
        return AndroidUsbChipset.Auto;
    }
    /// <summary>Maps portable stop intervals to USB CDC and CP210x encoding.</summary>
    /// <param name="bits">The requested stop interval.</param>
    /// <returns>The encoded stop interval.</returns>
    internal static int StopCode(StopBits bits) => bits switch
    {
        StopBits.One => 0,
        StopBits.OnePointFive => 1,
        StopBits.Two => 2,
        _ => throw new ArgumentOutOfRangeException(nameof(bits))
    };
    /// <summary>Compacts the transfer by removing two status bytes from every FTDI USB packet.</summary>
    /// <param name="bytes">The received buffer to compact in place.</param>
    /// <param name="count">The received transfer length.</param>
    /// <param name="packetSize">The endpoint maximum packet size.</param>
    /// <returns>The compacted payload length.</returns>
    internal static int StripFtdiStatus(byte[] bytes, int count, int packetSize)
    {
        if (packetSize < 3 || count < 0 || count > bytes.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }
        int target = 0;
        for (int offset = 0; offset < count; offset += packetSize)
        {
            int payload = Math.Max(0, Math.Min(packetSize, count - offset) - 2);
            Array.Copy(bytes, offset + Math.Min(2, count - offset), bytes, target, payload);
            target += payload;
        }
        return target;
    }
    /// <summary>Encodes an FTDI baud divisor using its standard 3 MHz clock.</summary>
    /// <param name="baud">The requested baud rate.</param>
    /// <returns>The encoded divisor, including its high fractional bit.</returns>
    /// <exception cref="NotSupportedException">The rate cannot be represented within three percent.</exception>
    internal static int FtdiDivisor(int baud)
    {
        if (baud <= 0 || baud > 3000000)
        {
            throw new NotSupportedException(global::Gurux.Serial.Properties.Resources.Get("USBSerialOptionsUnsupported"));
        }
        int eighths = (int)Math.Round(24000000.0 / baud);
        if (eighths < 8 || eighths > 0x1ffff || Math.Abs(24000000.0 / eighths - baud) / baud > 0.03)
        {
            throw new NotSupportedException(global::Gurux.Serial.Properties.Resources.Get("USBSerialOptionsUnsupported"));
        }
        int[] fractions = [0, 3, 2, 4, 1, 5, 6, 7];
        int encoded = (eighths >> 3) | (fractions[eighths & 7] << 14);
        return encoded == 1 ? 0 : encoded == 0x4001 ? 1 : encoded;
    }
}
#endif
