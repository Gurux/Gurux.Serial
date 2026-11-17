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
using Avalonia.Styling;
using UiResources = Gurux.Serial.Example.Desktop.Properties.ExampleSettingsResources;

namespace Gurux.Serial.Example.Desktop;

/// <summary>Applies the example's compact, theme-aware visual conventions without owning transport behavior.</summary>
internal static class ExampleLayout
{
    /// <summary>Sets consistent desktop dimensions while supporting narrow windows.</summary>
    /// <param name="window">The example window to size.</param>
    internal static void ConfigureWindow(Window window)
    {
        window.Width = 820; window.Height = 800;
        window.MinWidth = 420; window.MinHeight = 560;
    }

    /// <summary>Creates the connection-status header while the host window displays the application title.</summary>
    /// <param name="state">The status text updated by the example.</param>
    /// <returns>The root stack panel receiving this example's sections.</returns>
    internal static StackPanel CreateBody(TextBlock state)
    {
        var body = new StackPanel { Name = "ExampleRoot", Margin = new Thickness(16), Spacing = 12 };
        var header = new Grid { Name = "ExampleHeader", ColumnDefinitions = new ColumnDefinitions("*,Auto"), ColumnSpacing = 12 };
        state.Name = "ConnectionState"; state.FontSize = 12; state.FontWeight = FontWeight.SemiBold;
        var badge = new Border { Padding = new Thickness(10, 6), CornerRadius = new CornerRadius(12), BorderThickness = new Thickness(1), Child = state, VerticalAlignment = VerticalAlignment.Center };
        Grid.SetColumn(badge, 1); header.Children.Add(badge);
        body.Children.Add(header);
        return body;
    }

    /// <summary>Adds a wrapping connection toolbar in the standard section.</summary>
    /// <param name="body">The root receiving the connection section.</param>
    /// <returns>The panel receiving content-sized command buttons.</returns>
    internal static WrapPanel Toolbar(Panel body)
    {
        var actions = new WrapPanel { Name = "ConnectionActions", Orientation = Orientation.Horizontal };
        body.Children.Add(Group(UiResources.ConnectionGroup, actions));
        return actions;
    }

    /// <summary>Creates a subtle titled surface that remains readable in either native theme.</summary>
    /// <param name="title">The localized section title.</param>
    /// <param name="content">The section's native controls.</param>
    /// <returns>The section border and caption.</returns>
    internal static Border Group(string title, Control content) => new()
    {
        Tag = "ExampleSection", CornerRadius = new CornerRadius(9), BorderThickness = new Thickness(1),
        BorderBrush = new SolidColorBrush(Color.FromArgb(75, 128, 128, 128)),
        Background = new SolidColorBrush(Color.FromArgb(5, 128, 128, 128)), Padding = new Thickness(12),
        Child = new StackPanel { Spacing = 8, Children = { new TextBlock { Text = title, FontSize = 13, FontWeight = FontWeight.SemiBold, TextWrapping = TextWrapping.Wrap }, content } }
    };

    /// <summary>Associates a visible caption with a compact input in a wrapping settings row.</summary>
    /// <param name="label">The localized field caption, including units when appropriate.</param>
    /// <param name="input">The native input to label.</param>
    /// <returns>A vertically grouped caption and input.</returns>
    internal static StackPanel Field(string label, Control input) => new()
    {
        Spacing = 4, MinWidth = 140, MaxWidth = 240, HorizontalAlignment = HorizontalAlignment.Left,
        Children = { new TextBlock { Text = label, FontSize = 12, TextWrapping = TextWrapping.Wrap }, input }
    };

    /// <summary>Arranges options into a horizontal row that wraps at the available width.</summary>
    /// <param name="controls">The controls displayed in their original order.</param>
    /// <returns>The wrapping native layout.</returns>
    internal static WrapPanel Row(params Control[] controls)
    {
        var row = new WrapPanel { Orientation = Orientation.Horizontal };
        foreach (var control in controls) { control.Margin = new Thickness(0, 0, 10, 6); row.Children.Add(control); }
        return row;
    }

    /// <summary>Keeps command buttons compact and emphasizes primary actions with the native accent style.</summary>
    /// <param name="button">The command button to style.</param>
    /// <param name="primary">Whether this is a primary connection or send command.</param>
    internal static void ConfigureAction(Button button, bool primary = false)
    {
        button.HorizontalAlignment = HorizontalAlignment.Left;
        button.MinWidth = 88; button.MinHeight = 34;
        button.Padding = new Thickness(14, 7); button.Margin = new Thickness(0, 0, 8, 4);
        if (primary) { button.Classes.Add("accent"); }
    }

    /// <summary>Shows a localized connection badge without changing the connection state.</summary>
    /// <param name="state">The header text hosted by the badge border.</param>
    /// <param name="connected">Whether the transport is open.</param>
    /// <param name="busy">Whether an asynchronous operation is active.</param>
    internal static void UpdateStatus(TextBlock state, bool connected, bool busy)
    {
        state.Text = busy ? UiResources.ConnectionBusy : connected ? UiResources.ConnectionActive : UiResources.ConnectionInactive;
        if (state.Parent is Border badge)
        {
            var color = Color.Parse(busy ? "#2563EB" : connected ? "#198754" : "#6B7280");
            badge.BorderBrush = new SolidColorBrush(color) { Opacity = 0.55 };
            badge.Background = new SolidColorBrush(color) { Opacity = 0.08 };
        }
    }

    /// <summary>Hides empty operation feedback while retaining native theme colors for replies and failures.</summary>
    /// <param name="error">The current operation feedback control.</param>
    internal static void UpdateError(TextBlock error)
    {
        error.IsVisible = !string.IsNullOrWhiteSpace(error.Text);
        error.TextWrapping = TextWrapping.Wrap;
        error.ClearValue(TextBlock.ForegroundProperty);
    }

    /// <summary>Gives technical activity output a bounded, monospaced native editor.</summary>
    /// <param name="log">The read-only activity output.</param>
    internal static void ConfigureLog(TextBox log)
    {
        log.IsReadOnly = true; log.AcceptsReturn = true;
        log.MinHeight = 200; log.Height = 240;
        log.FontFamily = new FontFamily("Cascadia Mono, DejaVu Sans Mono, Consolas, Menlo, monospace");
        log.TextWrapping = TextWrapping.NoWrap;
    }

    /// <summary>Places two sections side by side on wide windows and stacks them on narrow windows.</summary>
    /// <param name="first">The leading section.</param>
    /// <param name="second">The trailing section.</param>
    /// <returns>A grid that adapts after its available width changes.</returns>
    internal static Grid TwoColumns(Control first, Control second)
    {
        var grid = new Grid { ColumnSpacing = 12, RowSpacing = 12 };
        grid.Children.Add(first); grid.Children.Add(second);
        bool? stacked = null;
        grid.SizeChanged += (_, _) =>
        {
            bool narrow = grid.Bounds.Width < 620;
            if (stacked == narrow) { return; }
            stacked = narrow;
            grid.ColumnDefinitions = new ColumnDefinitions(narrow ? "*" : "*,*");
            grid.RowDefinitions = new RowDefinitions(narrow ? "Auto,Auto" : "Auto");
            Grid.SetColumn(second, narrow ? 0 : 1); Grid.SetRow(second, narrow ? 1 : 0);
        };
        return grid;
    }
    /// <summary>Places scrolling example content above a fixed, theme-aware connection status bar.</summary>
    /// <param name="body">The complete example controls to scroll.</param>
    /// <param name="status">The text showing connection state and configured endpoints.</param>
    /// <returns>A two-row layout whose status bar remains visible below the scrolling content.</returns>
    internal static Grid WithStatus(Control body, TextBlock status)
    {
        status.FontSize = 12;
        status.TextWrapping = TextWrapping.Wrap;
        var bar = new Border
        {
            Name = "StatusBar",
            Padding = new Thickness(12, 8),
            BorderThickness = new Thickness(0, 1, 0, 0),
            BorderBrush = new SolidColorBrush(Color.FromArgb(80, 128, 128, 128)),
            Child = status
        };
        var frame = new Grid { RowDefinitions = new RowDefinitions("*,Auto") };
        frame.Children.Add(new ScrollViewer { Content = body });
        Grid.SetRow(bar, 1);
        frame.Children.Add(bar);
        return frame;
    }

}