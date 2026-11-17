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
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Gurux.Common;
using Gurux.Serial.Avalonia.Properties;
namespace Gurux.Serial.Avalonia;
/// <summary>Optional desktop settings dialog. Use one instance per media.</summary>
public sealed class AvaloniaMediaSettingsEditor : IGXMediaSettingsEditor
{
    /// <summary>Owns dialogs when no explicit parent is supplied.</summary>
    private readonly Window _owner;
    /// <summary>Snapshots discovered port names whenever a settings dialog is opened.</summary>
    private readonly Func<string[]> _getPortNames;
    /// <summary>Initializes a settings editor bound to an owner.</summary>
    /// <param name="owner">The visible window that will own settings dialogs.</param>
    /// <exception cref="ArgumentNullException">The owner is null.</exception>
    public AvaloniaMediaSettingsEditor(Window owner) : this(owner, global::Gurux.Serial.GXSerial.GetPortNames) { }
    /// <summary>Initializes an editor with a host-supplied serial port discovery method.</summary>
    /// <param name="owner">The visible window that owns settings dialogs.</param>
    /// <param name="getPortNames">Returns a fresh snapshot of selectable ports whenever the editor opens.</param>
    /// <exception cref="ArgumentNullException">The owner or discovery method is null.</exception>
    public AvaloniaMediaSettingsEditor(Window owner, Func<string[]> getPortNames)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        _getPortNames = getPortNames ?? throw new ArgumentNullException(nameof(getPortNames));
    }
    /// <summary>Tracks an active modal editing session.</summary>
    private bool _showing;
    /// <inheritdoc/>
    public IGXWindow? Window { get; private set; }

    /// <summary>Shows this media's typed settings dialog and applies validated settings only on Save.</summary>
    /// <param name="media">The closed media supported by this adapter.</param>
    /// <param name="parent">An optional wrapper for the visible owning Avalonia window.</param>
    /// <param name="cancellationToken">Cancels the dialog and discards draft changes.</param>
    /// <returns>True when settings were saved; false when the dialog was dismissed.</returns>
    /// <exception cref="ArgumentNullException">The media is null.</exception>
    /// <exception cref="ArgumentException">The parent belongs to another window adapter.</exception>
    /// <exception cref="InvalidOperationException">The owner is hidden, another dialog is active, or the media is open.</exception>
    /// <exception cref="NotSupportedException">The media type belongs to another adapter.</exception>
    /// <exception cref="OperationCanceledException">The cancellation token was canceled.</exception>
    public async Task<bool> ShowAsync(IGXMedia media, IGXWindow? parent = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(media);
        if (Dispatcher.UIThread.CheckAccess())
        {
            return await ShowCoreAsync(media, parent, cancellationToken);
        }
        return await Dispatcher.UIThread.InvokeAsync(() => ShowCoreAsync(media, parent, cancellationToken));
    }

    /// <summary>Creates, owns and closes a draft dialog on the UI thread.</summary>
    /// <param name="media">The source media to edit.</param>
    /// <param name="parent">An optional native owner wrapper.</param>
    /// <param name="cancellationToken">Cancels the modal session without saving.</param>
    /// <returns>Whether validated settings were saved.</returns>
    private async Task<bool> ShowCoreAsync(IGXMedia media, IGXWindow? parent, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (_showing) { throw new InvalidOperationException(Resources.Get("AlreadyShowing")); }
        if (parent is not null && parent is not AvaloniaMediaWindow)
        {
            throw new ArgumentException(Resources.Get("ParentType"), nameof(parent));
        }
        var owner = (parent as AvaloniaMediaWindow)?.NativeWindow ?? _owner;
        if (!owner.IsVisible) { throw new InvalidOperationException(Resources.Get("OwnerVisible")); }
        _showing = true;
        try
        {
            await using var draft = MediaSettingsDraft.Create(media, _getPortNames());
            var dialog = new Window { Title = Resources.Get("Title"), Width = 480, Height = 640, WindowStartupLocation = WindowStartupLocation.CenterOwner };
            var layout = new StackPanel { Margin = new Thickness(20), Spacing = 8 };
            layout.Children.Add(draft.Form);
            var error = new TextBlock { Name = "SettingsError", Foreground = Brushes.Red, TextWrapping = TextWrapping.Wrap, IsVisible = false };
            var save = new Button { Name = "SaveSettings", Content = Resources.Get("Save"), IsDefault = true, IsEnabled = draft.CanSave };
            draft.CanSaveChanged += (_, _) => save.IsEnabled = draft.CanSave;
            var cancel = new Button { Name = "CancelSettings", Content = Resources.Get("Cancel"), IsCancel = true };
            bool finished = false;
            bool tokenCanceled = false;
            bool cleanupStarted = false;
            bool allowClose = false;
            bool acceptedClose = false;
            save.Click += (_, _) =>
            {
                if (finished || !draft.CanSave) { return; }
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    draft.Commit();
                    finished = true;
                    acceptedClose = true;
                    dialog.Close(true);
                }
                catch (Exception failure)
                {
                    error.Text = string.Format(System.Globalization.CultureInfo.CurrentCulture,
                        Resources.Get("InvalidSettingsDetails"), failure.Message);
                    error.IsVisible = true;
                }
            };
            cancel.Click += (_, _) => dialog.Close(false);
            dialog.Closing += async (_, args) =>
            {
                if (allowClose) { return; }
                args.Cancel = true;
                if (cleanupStarted) { return; }
                cleanupStarted = true;
                layout.IsEnabled = false;
                try { await draft.DisposeAsync(); }
                finally
                {
                    allowClose = true;
                    Dispatcher.UIThread.Post(() => dialog.Close(acceptedClose));
                }
            };
            dialog.Closed += (_, _) => finished = true;
            layout.Children.Add(error);
            layout.Children.Add(new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Children = { save, cancel } });
            dialog.Content = new ScrollViewer { Content = layout };
            EventHandler ownerClosed = (_, _) =>
            {
                if (!finished) { dialog.Close(false); }
            };
            owner.Closed += ownerClosed;
            Window = new AvaloniaMediaWindow(dialog);
            try
            {
                var completion = dialog.ShowDialog<bool>(owner);
                using var registration = cancellationToken.Register(() => Dispatcher.UIThread.Post(() =>
                {
                    if (!finished) { tokenCanceled = true; dialog.Close(false); }
                }));
                var result = await completion;
                if (tokenCanceled) { cancellationToken.ThrowIfCancellationRequested(); }
                return result;
            }
            finally
            {
                owner.Closed -= ownerClosed;
                if (!finished) { dialog.Close(false); }
            }
        }
        finally { Window = null; _showing = false; }
    }
}
