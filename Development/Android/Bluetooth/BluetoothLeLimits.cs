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
namespace Gurux.Serial;

/// <summary>Applies ATT framing and the GATT attribute-length limit to serial packets.</summary>
internal static class BluetoothLeLimits
{
    /// <summary>Maximum bytes in a GATT attribute, independent of the negotiated ATT MTU.</summary>
    internal const int MaximumAttributeLength = 512;
    /// <summary>Computes the output payload after subtracting the three-byte ATT write header.</summary>
    /// <param name="mtu">Negotiated ATT MTU; values below the minimum retain the default 20-byte payload.</param>
    /// <returns>A payload between 20 and 512 bytes, including when Android negotiates MTU 517.</returns>
    internal static int GetPayloadSize(int mtu) => mtu <= 23 ? 20 : Math.Min(mtu - 3, MaximumAttributeLength);
}
#endif
