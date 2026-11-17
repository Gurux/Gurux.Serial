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
using Avalonia.Controls;
using Gurux.Common;

namespace Gurux.Serial.Avalonia;

/// <summary>Embeds native media settings in a host form with a shared Save and Cancel lifecycle.</summary>
/// <remarks>Controls stage an isolated closed media copy. Disposing the view discards uncommitted changes.</remarks>
public sealed class MediaSettingsView : UserControl, IDisposable, IAsyncDisposable
{
    /// <summary>Owns native field definitions, validation and the isolated media copy.</summary>
    private readonly MediaSettingsDraft _draft;
    /// <summary>Whether the host has ended this editing session.</summary>
    private bool _disposed;

    /// <summary>Creates native settings controls without opening the media.</summary>
    /// <param name="media">The closed media supported by this adapter, owned by the caller.</param>
    /// <exception cref="ArgumentNullException">The media is null.</exception>
    /// <exception cref="InvalidOperationException">The media is open.</exception>
    /// <exception cref="NotSupportedException">The media belongs to another adapter.</exception>
    public MediaSettingsView(IGXMedia media) : this(media, global::Gurux.Serial.GXSerial.GetPortNames) { }

    /// <summary>Creates native serial settings with host-supplied port discovery.</summary>
    /// <param name="media">The caller-owned closed serial media.</param>
    /// <param name="getPortNames">Returns the snapshot of ports offered by the selector.</param>
    /// <exception cref="ArgumentNullException">The media or discovery method is null.</exception>
    /// <exception cref="InvalidOperationException">The media is open.</exception>
    /// <exception cref="NotSupportedException">The media belongs to another adapter.</exception>
    public MediaSettingsView(IGXMedia media, Func<string[]> getPortNames)
    {
        ArgumentNullException.ThrowIfNull(media);
        ArgumentNullException.ThrowIfNull(getPortNames);
        _draft = MediaSettingsDraft.Create(media, getPortNames());
        Content = _draft.Form;
    }

    /// <summary>Validates current native controls and applies them to the caller's closed media.</summary>
    /// <remarks>Successful commits can be repeated when the host must retry persistence. Changes made
    /// externally to the caller's media after the last successful commit remain conflicts.</remarks>
    /// <exception cref="ObjectDisposedException">The view has been disposed.</exception>
    /// <exception cref="InvalidOperationException">The media is open, changed externally or contains invalid settings.</exception>
    /// <exception cref="FormatException">A control value cannot be parsed or a required choice is missing.</exception>
    public void Commit()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _draft.Commit();
    }

    /// <summary>Discards the private settings copy without disposing the caller's media.</summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            _draft.Dispose();
        }
    }
    /// <summary>Discards settings after pending native metadata discovery has fully cleaned up.</summary>
    /// <returns>Completion after metadata cleanup and view disposal.</returns>
    public async ValueTask DisposeAsync()
    {
        _disposed = true;
        await _draft.DisposeAsync();
    }
}
