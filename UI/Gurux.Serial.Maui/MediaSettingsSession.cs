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
using Gurux.Common;
using Gurux.Serial.Maui.Properties;

namespace Gurux.Serial.Maui;

/// <summary>Owns a private settings copy, rebases successful commits and rejects external changes or open media.</summary>
internal sealed class MediaSettingsSession : IDisposable
{
    /// <summary>The media receiving accepted settings.</summary>
    private readonly IGXMedia _source;
    /// <summary>The serialized state observed at creation or the last successful commit.</summary>
    private string? _original;
    /// <summary>Whether the private media has been disposed.</summary>
    private bool _disposed;
    /// <summary>Gets the unopened media used for staging.</summary>
    internal IGXMedia Draft { get; }

    /// <summary>Copies settings without opening either media.</summary>
    /// <param name="source">The media that will receive accepted settings.</param>
    /// <param name="draft">An owned private media of the same kind.</param>
    /// <exception cref="InvalidOperationException">The source is open.</exception>
    internal MediaSettingsSession(IGXMedia source, IGXMedia draft)
    {
        _source = source;
        Draft = draft;
        try
        {
            lock (source.SyncRoot)
            {
                if (source.IsOpen) { throw new InvalidOperationException(Resources.Open); }
                _original = source.Settings;
                draft.Settings = _original;
            }
        }
        catch { Dispose(); throw; }
    }

    /// <summary>Reads and validates controls, applies accepted settings and rebases the conflict baseline after success.</summary>
    /// <param name="readControls">Copies native input values into the private media.</param>
    /// <exception cref="InvalidOperationException">The media is open, has changed, or contains invalid settings.</exception>
    /// <exception cref="ObjectDisposedException">The session has already ended.</exception>
    internal void Commit(Action readControls)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        lock (_source.SyncRoot)
        {
            if (_source.IsOpen) { throw new InvalidOperationException(Resources.Open); }
            if (_source.Settings != _original) { throw new InvalidOperationException(Resources.Changed); }
            readControls();
            IReadOnlyList<GXValidationError> errors = Draft.Validate();
            if (errors.Count != 0)
            {
                throw new InvalidOperationException(string.Join(Environment.NewLine, errors.Select(error => error.Message)));
            }
            _source.Settings = Draft.Settings;
            _original = _source.Settings;
        }
    }

    /// <summary>Disposes the private media without applying unaccepted changes.</summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            (Draft as IDisposable)?.Dispose();
        }
    }
}
