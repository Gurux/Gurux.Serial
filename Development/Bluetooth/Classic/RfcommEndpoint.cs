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
using System;
using System.Globalization;
using Gurux.Serial.Properties;

namespace Gurux.Serial.Bluetooth.Classic;

/// <summary>Encodes Bluetooth addresses for the native RFCOMM socket ABIs.</summary>
internal static class RfcommEndpoint
{
    /// <summary>Parses a colon-separated, hyphen-separated or compact Bluetooth address.</summary>
    /// <param name="address">The six-byte Bluetooth device address in display order.</param>
    /// <returns>The numeric address used by both native socket implementations.</returns>
    /// <exception cref="ArgumentException">The address does not contain six hexadecimal bytes.</exception>
    internal static ulong ParseAddress(string address)
    {
        ArgumentNullException.ThrowIfNull(address);
        if (address.Length != 12 && address.Length != 17)
        {
            throw new ArgumentException(Resources.Get("BluetoothAddressInvalid"), nameof(address));
        }
        if (address.Length == 17)
        {
            char separator = address[2];
            for (int pos = 2; pos < address.Length; pos += 3)
            {
                if ((separator != ':' && separator != '-') || address[pos] != separator)
                {
                    throw new ArgumentException(Resources.Get("BluetoothAddressInvalid"), nameof(address));
                }
            }
        }
        string compact = address.Replace(":", string.Empty).Replace("-", string.Empty);
        if (compact.Length != 12 || !ulong.TryParse(compact, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out ulong value))
        {
            throw new ArgumentException(Resources.Get("BluetoothAddressInvalid"), nameof(address));
        }
        return value;
    }

    /// <summary>Creates the packed Windows SOCKADDR_BTH or Linux sockaddr_rc structure.</summary>
    /// <param name="address">The numeric device address.</param>
    /// <param name="channel">The remote RFCOMM channel, or zero to request Windows SDP resolution of the Serial Port service.</param>
    /// <param name="windows">Whether to emit the packed Windows ABI.</param>
    /// <returns>The socket address, including the native address family.</returns>
    internal static byte[] Serialize(ulong address, int channel, bool windows)
    {
        byte[] result = new byte[windows ? 30 : 10];
        byte[] family = BitConverter.GetBytes((ushort)(windows ? 32 : 31));
        Array.Copy(family, result, family.Length);
        for (int pos = 0; pos != 6; ++pos)
        {
            result[pos + 2] = (byte)(address >> (pos * 8));
        }
        if (windows)
        {
            if (channel == 0)
            {
                BluetoothSerialDevice.SerialPortServiceUuid.ToByteArray().CopyTo(result, 10);
            }
            Array.Copy(BitConverter.GetBytes(channel), 0, result, 26, 4);
        }
        else
        {
            result[8] = (byte)channel;
        }
        return result;
    }
}

#endif
