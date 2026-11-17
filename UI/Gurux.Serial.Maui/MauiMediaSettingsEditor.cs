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
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Gurux.Serial.Maui.Properties;

namespace Gurux.Serial.Maui;

/// <summary>Edits GXSerial settings in a native, asynchronous MAUI overlay.</summary>
/// <remarks>Register one editor per media. Only Save applies the validated private copy.</remarks>
public sealed class MauiMediaSettingsEditor : IGXMediaSettingsEditor
{
    /// <summary>The default owner supplied by the host.</summary>
    private readonly Page _parent;
    /// <summary>Whether an editing operation is active on the UI thread.</summary>
    private bool _showing;
    /// <summary>Completes the current overlay without applying settings.</summary>
    private Action? _cancel;
    /// <inheritdoc/>
    public IGXWindow? Window { get; private set; }
    /// <summary>Creates an editor owned by a MAUI page.</summary>
    /// <param name="parent">The page that will host the native overlay.</param>
    /// <exception cref="ArgumentNullException">The parent is null.</exception>
    public MauiMediaSettingsEditor(Page parent) { _parent = parent ?? throw new ArgumentNullException(nameof(parent)); }
    /// <summary>Creates an editor owned by the current page of a MAUI window.</summary>
    /// <param name="parent">The native window with an assigned page.</param>
    /// <exception cref="ArgumentException">The window has no page.</exception>
    public MauiMediaSettingsEditor(Microsoft.Maui.Controls.Window parent)
        : this(parent?.Page ?? throw new ArgumentException(Resources.WindowPage, nameof(parent))) { }
    /// <summary>Cancels the active overlay; hosts can forward their Back action here.</summary>
    public void Cancel()
    {
        Action? cancel = _cancel;
        if (cancel is not null) { MainThread.BeginInvokeOnMainThread(cancel); }
    }
    /// <summary>Shows settings for this library's media kind and applies only accepted changes.</summary>
    /// <param name="media">The closed GXSerial instance to edit.</param>
    /// <param name="parent">An optional owner adapter from this library.</param>
    /// <param name="cancellationToken">Cancels editing without saving.</param>
    /// <returns>True when validated settings were saved; false when dismissed.</returns>
    /// <exception cref="ArgumentNullException">The media is null.</exception>
    /// <exception cref="ArgumentException">The media kind or owner is unsupported.</exception>
    /// <exception cref="InvalidOperationException">The media is open or this editor is already showing.</exception>
    /// <exception cref="OperationCanceledException">Cancellation is requested.</exception>
    public Task<bool> ShowAsync(IGXMedia media, IGXWindow? parent = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(media);
        if (media.GetType() != typeof(GXSerial)) { throw new ArgumentException(Resources.WrongMedia, nameof(media)); }
        return MainThread.InvokeOnMainThreadAsync(async () =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (_showing) { throw new InvalidOperationException(Resources.Busy); }
            if (media.IsOpen) { throw new InvalidOperationException(Resources.Open); }
            if (parent is not null && parent is not MauiMediaWindow) { throw new ArgumentException(Resources.Parent, nameof(parent)); }
            Page selected = (parent as MauiMediaWindow)?.Page ?? _parent;
            if (selected is NavigationPage navigation) { selected = navigation.CurrentPage; }
            if (selected is not ContentPage owner) { throw new ArgumentException(Resources.Owner, nameof(parent)); }
            _showing = true;
            try
            {
                await using var draft = new MediaSettingsDraft((GXSerial)media);
                var view = new SettingsView(draft, cancellationToken);
                View? original = owner.Content;
                bool originalEnabled = original?.IsEnabled ?? true;
                var layer = new Grid();
                _cancel = () => view.Complete(false);
                EventHandler disappearing = (_, _) => view.Complete(false);
                EventHandler<Microsoft.Maui.Controls.HandlerChangingEventArgs> handlerChanging = (_, args) =>
                {
                    if (args.NewHandler is null) { view.Complete(false); }
                };
                owner.Disappearing += disappearing;
                owner.HandlerChanging += handlerChanging;
                try
                {
                    owner.Content = null;
                    if (original is not null) { original.IsEnabled = false; layer.Children.Add(original); }
                    layer.Children.Add(view);
                    owner.Content = layer;
                    Window = new MauiMediaWindow(owner);
                    using var registration = cancellationToken.Register(() => MainThread.BeginInvokeOnMainThread(() => view.Complete(false)));
                    bool accepted = await view.Completion;
                    if (!accepted) { cancellationToken.ThrowIfCancellationRequested(); }
                    return accepted;
                }
                finally
                {
                    view.IsEnabled = false;
                    try { await draft.DisposeAsync(); }
                    finally
                    {
                        owner.Disappearing -= disappearing;
                        owner.HandlerChanging -= handlerChanging;
                        layer.Children.Remove(view);
                        if (original is not null) { layer.Children.Remove(original); original.IsEnabled = originalEnabled; }
                        if (ReferenceEquals(owner.Content, layer)) { owner.Content = original; }
                    }
                }
            }
            finally { _cancel = null; Window = null; _showing = false; }
        });
    }
}
