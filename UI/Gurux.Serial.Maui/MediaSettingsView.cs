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
using Microsoft.Maui.Controls;
using SettingsResources = Gurux.Serial.Maui.Properties.Resources;

namespace Gurux.Serial.Maui;

/// <summary>Embeds native media settings in an owning form with an explicit commit boundary.</summary>
/// <remarks>The supplied media remains closed and is owned by the caller. Disposing this view discards uncommitted inputs.</remarks>
public sealed class MediaSettingsView : ContentView, IDisposable, IAsyncDisposable
{
    /// <summary>Owns the detached media copy and the native input controls.</summary>
    private readonly MediaSettingsDraft draft;
    /// <summary>Prevents access after the embedded editing session ends.</summary>
    private bool disposed;

    /// <summary>Creates native controls for a closed media without opening its transport.</summary>
    /// <param name="media">The caller-owned media that receives settings only when committed.</param>
    /// <exception cref="ArgumentNullException">The media is null.</exception>
    /// <exception cref="ArgumentException">The media is not supported by this adapter.</exception>
    /// <exception cref="InvalidOperationException">The supplied media is open.</exception>
    public MediaSettingsView(IGXMedia media)
    {
        ArgumentNullException.ThrowIfNull(media);
        if (media is not GXSerial source)
        {
            throw new ArgumentException(SettingsResources.WrongMedia, nameof(media));
        }
        draft = new MediaSettingsDraft(source);
        Content = draft.Form;
    }

    /// <summary>Validates current native inputs and copies them to the supplied closed media.</summary>
    /// <remarks>A successful commit establishes the baseline for subsequent commits; changes made externally are still rejected.</remarks>
    /// <exception cref="ObjectDisposedException">The view has been disposed.</exception>
    /// <exception cref="FormatException">An input cannot be converted to its required value.</exception>
    /// <exception cref="InvalidOperationException">The media is open, changed externally, or has invalid settings.</exception>
    public void Commit()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        draft.Commit();
    }

    /// <summary>Releases the detached settings session without applying pending inputs.</summary>
    public void Dispose()
    {
        if (!disposed)
        {
            disposed = true;
            draft.Dispose();
            Content = null;
        }
    }
    /// <summary>Discards settings after pending native metadata discovery has fully cleaned up.</summary>
    /// <returns>Completion after metadata cleanup and view disposal.</returns>
    public async ValueTask DisposeAsync()
    {
        disposed = true;
        try { await draft.DisposeAsync(); }
        finally { Content = null; }
    }
}