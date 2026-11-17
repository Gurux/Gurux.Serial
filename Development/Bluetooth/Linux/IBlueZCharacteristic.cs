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

/// <summary>Exposes BlueZ GATT writes and notification sessions.</summary>
[DBusInterface("org.bluez.GattCharacteristic1")]
internal interface IBlueZCharacteristic : IDBusObject
{
    /// <summary>Writes characteristic bytes using the selected ATT write type.</summary>
    /// <param name="value">Bytes to send.</param>
    /// <param name="options">BlueZ write options.</param>
    /// <returns>The asynchronous write operation.</returns>
    Task WriteValueAsync(byte[] value, IDictionary<string, object> options);
    /// <summary>Starts this bus client's notification session.</summary>
    /// <returns>The asynchronous subscription operation.</returns>
    Task StartNotifyAsync();
    /// <summary>Stops this bus client's notification session.</summary>
    /// <returns>The asynchronous unsubscribe operation.</returns>
    Task StopNotifyAsync();
    /// <summary>Subscribes to characteristic changes and bus failures.</summary>
    /// <param name="handler">Receives changed values.</param>
    /// <param name="onError">Receives subscription failures.</param>
    /// <returns>The subscription that must be disposed.</returns>
    Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler, Action<Exception> onError);
}
#endif