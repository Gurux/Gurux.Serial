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
/// <summary>Selects the USB serial protocol used by an Android stream factory.</summary>
public enum AndroidUsbChipset
{
    /// <summary>Detects known identifiers or a single CDC ACM port.</summary>
    Auto,
    /// <summary>Uses the standard USB CDC ACM protocol.</summary>
    CdcAcm,
    /// <summary>Uses FTDI SIO and removes status bytes from each USB packet.</summary>
    Ftdi,
    /// <summary>Uses the Silicon Labs CP210x protocol.</summary>
    Cp210x,
    /// <summary>Uses the QinHeng CH340/CH341 protocol.</summary>
    Ch34x,
    /// <summary>Uses the legacy Prolific PL2303 protocol.</summary>
    Prolific
}
#endif
