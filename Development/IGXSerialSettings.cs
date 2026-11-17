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

/// <summary>Applies serial line settings to a media or capable transport without reopening its device.</summary>
public interface IGXSerialSettings
{
    /// <summary>Gets the configured settings; use ApplySettingsAsync to commit an independent request to active hardware.</summary>
    SerialPortSettings PortSettings { get; }

    /// <summary>Validates and applies framing, flow control and manually controlled modem signals.</summary>
    /// <param name="settings">Requested settings for the same device; the caller may reuse the object after completion.</param>
    /// <param name="cancellationToken">Cancels waiting and supported native configuration operations.</param>
    /// <returns>A task completing after the settings are applied or staged while closed.</returns>
    /// <exception cref="NotSupportedException">The open transport cannot change line settings.</exception>
    /// <exception cref="ArgumentException">The settings are invalid or change the active device identity.</exception>
    /// <exception cref="InvalidOperationException">The connection is opening or closing.</exception>
    /// <exception cref="OperationCanceledException">The request or connection lifetime is canceled.</exception>
    /// <exception cref="AggregateException">Applying and restoring physical settings both failed.</exception>
    /// <remarks>An implementation must not report success without applying the settings. Failure must preserve its previous settings or report failed recovery.</remarks>
    Task ApplySettingsAsync(SerialPortSettings settings, CancellationToken cancellationToken = default);
}