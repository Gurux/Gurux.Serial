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
using Tmds.DBus;
namespace Gurux.Serial;

/// <summary>Owns one caller's filtered BlueZ adapter discovery session.</summary>
[DBusInterface("org.bluez.Adapter1")]
internal interface IBlueZAdapter : IDBusObject
{
    /// <summary>Configures this bus client's discovery filter without changing other clients' filters.</summary>
    /// <param name="filter">BlueZ discovery options, including the requested transport.</param>
    /// <returns>The asynchronous filter update.</returns>
    Task SetDiscoveryFilterAsync(IDictionary<string, object> filter);
    /// <summary>Acquires this bus client's discovery session.</summary>
    /// <returns>The asynchronous discovery startup.</returns>
    Task StartDiscoveryAsync();
    /// <summary>Releases only this bus client's discovery session.</summary>
    /// <returns>The asynchronous discovery shutdown.</returns>
    Task StopDiscoveryAsync();
}
#endif
