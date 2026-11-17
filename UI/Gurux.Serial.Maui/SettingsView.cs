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
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using SettingsResources = Gurux.Serial.Maui.Properties.Resources;

namespace Gurux.Serial.Maui;

/// <summary>Displays native staged inputs and completes when saved or dismissed.</summary>
internal sealed class SettingsView : ContentView
{
    /// <summary>Reports the user's decision without blocking the UI thread.</summary>
    private readonly TaskCompletionSource<bool> _completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
    /// <summary>Gets the eventual save or dismiss result.</summary>
    internal Task<bool> Completion => _completion.Task;
    /// <summary>Completes the overlay once.</summary>
    /// <param name="accepted">Whether validated settings have been saved.</param>
    /// <returns>True when this call supplied the first result.</returns>
    internal bool Complete(bool accepted) => _completion.TrySetResult(accepted);
    /// <summary>Creates controls with explicit Save and Cancel actions.</summary>
    /// <param name="draft">The private settings copy and native form.</param>
    /// <param name="cancellationToken">Prevents saving after cancellation.</param>
    internal SettingsView(MediaSettingsDraft draft, CancellationToken cancellationToken)
    {
        this.SetAppThemeColor(BackgroundColorProperty, Colors.White, Colors.Black);
        var layout = new VerticalStackLayout { Padding = 20, Spacing = 12 };
        layout.Children.Add(new Label { Text = SettingsResources.Title, FontSize = 24 });
        layout.Children.Add(draft.Form);
        var error = new Label { TextColor = Colors.Red, IsVisible = false };
        var accept = new Button { Text = SettingsResources.Save, HorizontalOptions = LayoutOptions.Start, MinimumWidthRequest = 88, MinimumHeightRequest = 48 };
        accept.IsEnabled = draft.CanSave;
        draft.CanSaveChanged += (_, _) => accept.IsEnabled = draft.CanSave;
        accept.Clicked += (_, _) =>
        {
            if (Completion.IsCompleted) { return; }
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                draft.Commit();
                Complete(true);
            }
            catch (OperationCanceledException) { Complete(false); }
            catch (Exception failure)
            {
                error.Text = string.Format(System.Globalization.CultureInfo.CurrentCulture,
                    SettingsResources.InvalidDetails, failure.Message);
                error.IsVisible = true;
            }
        };
        var cancel = new Button { Text = SettingsResources.Cancel, HorizontalOptions = LayoutOptions.Start, MinimumWidthRequest = 88, MinimumHeightRequest = 48 };
        cancel.Clicked += (_, _) => Complete(false);
        layout.Children.Add(error);
        var actions = new HorizontalStackLayout { Spacing = 12, HorizontalOptions = LayoutOptions.Start };
        actions.Children.Add(accept);
        actions.Children.Add(cancel);
        layout.Children.Add(actions);
        Content = new ScrollView { Content = layout };
    }
}
