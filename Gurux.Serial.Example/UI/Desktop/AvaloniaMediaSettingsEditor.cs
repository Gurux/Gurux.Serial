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
using Gurux.Serial.Example.Desktop.Properties;

namespace Gurux.Serial.Example.Desktop;

/// <summary>Routes this example's settings requests to its media-specific UI adapters.</summary>
internal sealed class AvaloniaMediaSettingsEditor : IGXMediaSettingsEditor
{
    /// <summary>The native page or window that owns the settings UI.</summary>
    private readonly global::Avalonia.Controls.Window _owner;
    /// <summary>The adapter currently handling the example's settings request.</summary>
    private IGXMediaSettingsEditor? _editor;
    /// <summary>Whether this dispatcher already owns an active request.</summary>
    private int _showing;

    /// <summary>Creates an application-local dispatcher for the supported media.</summary>
    /// <param name="owner">The native owner of the settings view.</param>
    /// <exception cref="ArgumentNullException">The owner is null.</exception>
    internal AvaloniaMediaSettingsEditor(global::Avalonia.Controls.Window owner)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    /// <inheritdoc />
    public IGXWindow? Window => _editor?.Window;

    /// <summary>Shows the settings adapter required by this example's media.</summary>
    /// <param name="media">The media whose settings will be staged.</param>
    /// <param name="parent">An optional native owner wrapper understood by that adapter.</param>
    /// <param name="cancellationToken">Cancels the pending settings operation.</param>
    /// <returns>True when the adapter commits accepted settings.</returns>
    /// <exception cref="InvalidOperationException">A request is already active.</exception>
    public async Task<bool> ShowAsync(IGXMedia media, IGXWindow? parent = null, CancellationToken cancellationToken = default)
    {
        if (Interlocked.Exchange(ref _showing, 1) != 0)
        {
            throw new InvalidOperationException(ExampleSettingsResources.EditorAlreadyShowing);
        }
        try
        {
            _editor = new Gurux.Serial.Avalonia.AvaloniaMediaSettingsEditor(_owner);
            return await _editor.ShowAsync(media, parent, cancellationToken);
        }
        finally
        {
            Interlocked.Exchange(ref _showing, 0);
        }
    }
}
