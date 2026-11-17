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

namespace Gurux.Serial;

/// <summary>Builds the device-specific commands written to a BLE UART configuration characteristic when opening.</summary>
/// <remarks>Implementations format commands only: they must not open connections or modify the supplied settings.</remarks>
public interface IBluetoothConfigurationCommands
{
    /// <summary>Builds the exact configuration command sequence for the selected UART settings.</summary>
    /// <param name="settings">The baud rate, data bits, parity, stop bits and other settings selected by the application.</param>
    /// <returns>The complete command sequence, including any device-specific separators and line endings.</returns>
    string GetCommands(SerialPortSettings settings);
}
