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

namespace Gurux.Serial.Blazor;

/// <summary>Owns an unopened settings copy; independent of any UI runtime.</summary>
internal sealed class MediaSettingsSession : IDisposable
{
    /// <summary>Caller-owned media being edited.</summary>
    private readonly IGXMedia _source;
    /// <summary>Whether the private copy was discarded.</summary>
    private bool _disposed;
    /// <summary>Last accepted settings used to detect external changes between commits.</summary>
    private string? _original;
    /// <summary>Gets the unopened private media copy.</summary>
    internal IGXMedia Draft { get; }

    /// <summary>Copies settings into the owned media without opening it.</summary>
    /// <param name="source">Caller-owned closed media.</param>
    /// <param name="draft">Owned unopened copy.</param>
    internal MediaSettingsSession(IGXMedia source, IGXMedia draft)
    {
        _source = source;
        Draft = draft;
        try
        {
            if (source.IsOpen) { throw new InvalidOperationException(Properties.Resources.Closethemediabeforeeditingsettings); }
            _original = source.Settings;
            draft.Settings = _original;
        }
        catch { Dispose(); throw; }
    }

    /// <summary>Applies validated staged values while guarding source settings.</summary>
    /// <param name="readControls">Updates the private copy from staged values.</param>
    /// <exception cref="InvalidOperationException">The source is open, changed externally, or the staged settings contain validation errors.</exception>
    internal void Commit(Action readControls)
    {
        lock (_source.SyncRoot)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (_source.IsOpen) { throw new InvalidOperationException(Properties.Resources.Closethemediabeforesavingsettings); }
            if (_source.Settings != _original) { throw new InvalidOperationException(Properties.Resources.ThemediasettingschangedwhilethisformwasopenReopentheform); }
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

    /// <summary>Disposes only the private media copy.</summary>
    public void Dispose()
    {
        lock (_source.SyncRoot)
        {
            if (_disposed) { return; }
            _disposed = true;
            _original = null;
            (Draft as IDisposable)?.Dispose();
        }
    }
}
