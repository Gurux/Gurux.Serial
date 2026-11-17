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

using Gurux.Serial.Avalonia.Properties;
namespace Gurux.Serial.Avalonia;

/// <summary>Owns an unopened settings copy; independent of any UI runtime.</summary>
internal sealed class MediaSettingsSession : IDisposable
{
    /// <summary>The live media updated only after validation.</summary>
    private readonly IGXMedia _source;
    /// <summary>The serialized settings last accepted by this session.</summary>
    private string? _original;
    /// <summary>Gets the private unopened settings copy.</summary>
    internal IGXMedia Draft { get; }

    /// <summary>Copies source settings into a private media instance.</summary>
    /// <param name="source">The live source transport.</param>
    /// <param name="draft">The private transport owned by this session.</param>
    internal MediaSettingsSession(IGXMedia source, IGXMedia draft)
    {
        _source = source;
        Draft = draft;
        try
        {
            if (source.IsOpen) { throw new InvalidOperationException(Resources.Get("CloseBeforeEdit")); }
            _original = source.Settings;
            draft.Settings = _original;
        }
        catch { Dispose(); throw; }
    }

    /// <summary>Reads and validates controls before atomically updating unchanged source settings.</summary>
    /// <param name="readControls">Transfers form values into the private draft.</param>
    /// <remarks>Each successful commit becomes the next conflict baseline, so host persistence can be retried.</remarks>
    /// <exception cref="InvalidOperationException">The source is open, changed externally, or the staged settings contain validation errors.</exception>
    internal void Commit(Action readControls)
    {
        lock (_source.SyncRoot)
        {
            if (_source.IsOpen) { throw new InvalidOperationException(Resources.Get("CloseBeforeSave")); }
            if (_source.Settings != _original) { throw new InvalidOperationException(Resources.Get("Conflict")); }
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

    /// <inheritdoc/>
    public void Dispose() => (Draft as IDisposable)?.Dispose();
}
