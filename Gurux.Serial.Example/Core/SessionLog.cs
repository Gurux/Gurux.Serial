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

namespace Gurux.Serial.Example;
/// <summary>Keeps a bounded, thread safe activity history for the example.</summary>
internal sealed class SessionLog
{
    /// <summary>Removes retained activity entries atomically while allowing subsequent logging.</summary>
    public void Clear()
    {
        lock (_sync)
        {
            _entries.Clear();
        }
    }

    /// <summary>Protects the activity entries.</summary>
    private readonly object _sync = new();
    /// <summary>Contains the most recent activity entries.</summary>
    private readonly Queue<string> _entries = new();
    /// <summary>Gets an independent copy of the current activity history.</summary>
    public IReadOnlyList<string> Snapshot { get { lock (_sync) { return _entries.ToArray(); } } }
    /// <summary>Appends a timestamped entry and discards entries beyond the history limit.</summary>
    /// <param name="text">The localized activity description.</param>
    public void Add(string text)
    {
        lock (_sync)
        {
            _entries.Enqueue(string.Format(System.Globalization.CultureInfo.CurrentCulture, "{0:HH:mm:ss} {1}", DateTime.Now, text));
            while (_entries.Count > 500) { _entries.Dequeue(); }
        }
    }
}
