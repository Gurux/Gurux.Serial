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

using System.Text;

namespace Gurux.Serial.Example;

/// <summary>Formats received and transmitted bytes without replacing non-ASCII data with decoded characters.</summary>
public static class ExampleDataDisplay
{
    /// <summary>Tests whether every byte belongs to the seven-bit ASCII character set.</summary>
    /// <param name="data">The unmodified bytes to inspect.</param>
    /// <returns>True for empty data or bytes from 0x00 through 0x7F; otherwise false.</returns>
    public static bool IsAscii(ReadOnlySpan<byte> data)
    {
        foreach (byte value in data)
        {
            if (value > 0x7F)
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>Displays ASCII data as text and all other data as uppercase hexadecimal bytes.</summary>
    /// <param name="data">The original received or transmitted bytes.</param>
    /// <param name="hex">Whether to force hexadecimal output even for ASCII data.</param>
    /// <returns>The complete hexadecimal payload when forced or when any byte exceeds 0x7F; otherwise ASCII text.</returns>
    public static string Format(ReadOnlySpan<byte> data, bool hex = false) =>
        hex || !IsAscii(data) ? Convert.ToHexString(data) : Encoding.ASCII.GetString(data);
}
