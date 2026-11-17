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

/// <summary>Exposes connection and property operations for a BlueZ remote device.</summary>
[DBusInterface("org.bluez.Device1")]
internal interface IBlueZDevice : IDBusObject
{
    /// <summary>Connects the remote device's supported profiles.</summary>
    /// <returns>The asynchronous BlueZ operation.</returns>
    Task ConnectAsync();
    /// <summary>Disconnects a device connected by this transport.</summary>
    /// <returns>The asynchronous BlueZ operation.</returns>
    Task DisconnectAsync();
    /// <summary>Reads current device properties.</summary>
    /// <returns>The property dictionary.</returns>
    Task<IDictionary<string, object>> GetAllAsync();
    /// <summary>Subscribes to changes and bus failures.</summary>
    /// <param name="handler">Receives device property changes.</param>
    /// <param name="onError">Receives subscription failures.</param>
    /// <returns>The subscription that must be disposed.</returns>
    Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler, Action<Exception> onError);
}
#endif