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
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Layouts;
using LayoutResources = Gurux.Serial.Example.Maui.Properties.ExampleSettingsResources;

namespace Gurux.Serial.Example.Maui;

/// <summary>Builds this example's compact native layout and system light or dark appearance.</summary>
internal static class ExampleLayout
{
    /// <summary>Applies a system-themed background to the owning native page.</summary>
    /// <param name="page">The page whose background follows the system theme.</param>
    internal static void Apply(ContentPage page) => page.SetAppThemeColor(VisualElement.BackgroundColorProperty, Color.FromArgb(OperatingSystem.IsWindows() ? "#FFFFFF" : "#F5F7FA"), Color.FromArgb(OperatingSystem.IsWindows() ? "#202020" : "#14171C"));

    /// <summary>Creates the consistently spaced outer content stack.</summary>
    /// <returns>A root stack with compact page padding.</returns>
    internal static VerticalStackLayout Body() => new() { Padding = 16, Spacing = 12 };

    /// <summary>Creates a compact connection status badge while the host page displays the application title.</summary>
    /// <param name="state">The existing status label updated by the page controller.</param>
    /// <returns>The connection status header without a duplicate application title.</returns>
    internal static View Header(Label state)
    {
        state.FontSize = 12;
        state.VerticalOptions = LayoutOptions.Center;
        var badge = new Border { Padding = new Thickness(10, 6), StrokeThickness = 0, StrokeShape = new RoundRectangle { CornerRadius = 8 }, Content = state, VerticalOptions = LayoutOptions.Center };
        badge.SetAppThemeColor(VisualElement.BackgroundColorProperty, Color.FromArgb("#E7EDF5"), Color.FromArgb("#293444"));
        var header = new Grid { ColumnSpacing = 12, ColumnDefinitions = new ColumnDefinitionCollection { new(GridLength.Star), new(GridLength.Auto) } };
        Grid.SetColumn(badge, 1);
        header.Children.Add(badge);
        return header;
    }

    /// <summary>Updates the status badge using localized connection and busy states.</summary>
    /// <param name="label">The connection status label.</param>
    /// <param name="busy">Whether an operation is in progress.</param>
    /// <param name="connected">Whether the transport is open.</param>
    internal static void Status(Label label, bool busy, bool connected)
    {
        label.Text = busy ? LayoutResources.ConnectionBusy : connected ? LayoutResources.ConnectionActive : LayoutResources.ConnectionInactive;
        label.SetAppThemeColor(Label.TextColorProperty,
            Color.FromArgb(busy ? "#805B00" : connected ? "#17633C" : "#42526A"),
            Color.FromArgb(busy ? "#F4D26B" : connected ? "#8BE0B0" : "#CBD7E8"));
    }

    /// <summary>Creates a section with a subtle themed border and rounded surface.</summary>
    /// <param name="title">The localized section title.</param>
    /// <param name="content">The controls inside the section.</param>
    /// <returns>The titled section card.</returns>
    internal static View Group(string title, View content)
    {
        var caption = new Label { Text = title, FontSize = 14, FontAttributes = FontAttributes.Bold };
        caption.SetAppThemeColor(Label.TextColorProperty, Color.FromArgb("#35445B"), Color.FromArgb("#D2DEEF"));
        var border = new Border { Padding = 12, StrokeThickness = 1, StrokeShape = new RoundRectangle { CornerRadius = 10 }, Content = new VerticalStackLayout { Spacing = 8, Children = { caption, content } } };
        border.SetAppThemeColor(VisualElement.BackgroundColorProperty, Colors.White, Color.FromArgb("#1E242D"));
        border.SetAppTheme<Brush>(Border.StrokeProperty, new SolidColorBrush(Color.FromArgb("#DCE2EB")), new SolidColorBrush(Color.FromArgb("#394352")));
        if (OperatingSystem.IsWindows())
        {
            caption.FontSize = 13;
            caption.FontFamily = "Segoe UI Semibold";
            caption.FontAttributes = FontAttributes.None;
            border.StrokeShape = new RoundRectangle { CornerRadius = 9 };
            border.SetAppThemeColor(VisualElement.BackgroundColorProperty, Color.FromArgb("#05808080"), Color.FromArgb("#05808080"));
            border.SetAppTheme<Brush>(Border.StrokeProperty,
                new SolidColorBrush(Color.FromArgb("#4B808080")), new SolidColorBrush(Color.FromArgb("#4B808080")));
        }
        return border;
    }

    /// <summary>Arranges compact controls in a row that wraps within the available width.</summary>
    /// <param name="controls">The controls in reading order.</param>
    /// <returns>The wrapping row, also usable as a toolbar.</returns>
    internal static FlexLayout Row(params View[] controls)
    {
        var row = new FlexLayout { Direction = FlexDirection.Row, Wrap = FlexWrap.Wrap, AlignItems = FlexAlignItems.Center, AlignContent = FlexAlignContent.Start };
        foreach (View control in controls)
        {
            control.Margin = new Thickness(0, 0, 8, 8);
            row.Children.Add(control);
        }
        return row;
    }

    /// <summary>Stacks controls with the standard field spacing.</summary>
    /// <param name="controls">The controls in reading order.</param>
    /// <returns>The populated content stack.</returns>
    internal static VerticalStackLayout Stack(params View[] controls)
    {
        var stack = new VerticalStackLayout { Spacing = 8 };
        foreach (View control in controls)
        {
            stack.Children.Add(control);
        }
        return stack;
    }

    /// <summary>Labels an input above its value so wrapped fields remain understandable.</summary>
    /// <param name="caption">The localized field caption.</param>
    /// <param name="input">The native input control.</param>
    /// <param name="width">An optional compact width for inputs arranged in wrapping rows.</param>
    /// <returns>The labeled input stack.</returns>
    internal static View Field(string caption, View input, double width = -1)
    {
        input.MinimumHeightRequest = 40;
        if (input is InputView text)
        {
            text.SetAppThemeColor(InputView.TextColorProperty, Color.FromArgb("#17202D"), Color.FromArgb("#EDF2F8"));
        }
        return new VerticalStackLayout { Spacing = 4, WidthRequest = width, Children = { new Label { Text = caption, FontSize = 12 }, input } };
    }

    /// <summary>Combines a native checkbox with a localized caption.</summary>
    /// <param name="control">The existing checkbox and its event handlers.</param>
    /// <param name="caption">The localized option text.</param>
    /// <returns>The compact option row.</returns>
    internal static View Check(CheckBox control, string caption) => new HorizontalStackLayout { Spacing = 0, Children = { control, new Label { Text = caption, VerticalOptions = LayoutOptions.Center } } };

    /// <summary>Combines a native switch with its localized visible and accessible caption.</summary>
    /// <param name="control">The existing switch and its selected state.</param>
    /// <param name="caption">The localized option text.</param>
    /// <returns>The compact labeled switch row.</returns>
    internal static View Toggle(Switch control, string caption)
    {
        SemanticProperties.SetDescription(control, caption);
        return new HorizontalStackLayout { Spacing = 6, Children = { control, new Label { Text = caption, VerticalOptions = LayoutOptions.Center } } };
    }

    /// <summary>Styles content-sized actions with a forty-unit touch target.</summary>
    /// <param name="button">The existing action button.</param>
    /// <param name="primary">Whether this is a primary Open or Send action.</param>
    internal static void StyleButton(Button button, bool primary)
    {
        button.HorizontalOptions = LayoutOptions.Start;
        button.MinimumHeightRequest = OperatingSystem.IsWindows() ? 34 : 40;
        if (OperatingSystem.IsWindows())
        {
            button.MinimumWidthRequest = 88;
            button.Margin = new Thickness(0, 0, 8, 4);
        }
        button.Padding = new Thickness(14, 8);
        button.CornerRadius = 8;
        button.FontSize = 14;
        button.SetAppThemeColor(VisualElement.BackgroundColorProperty, Color.FromArgb(primary ? "#245FBB" : "#E7EDF5"), Color.FromArgb(primary ? "#4384E1" : "#303B4C"));
        button.SetAppThemeColor(Button.TextColorProperty, primary ? Colors.White : Color.FromArgb("#20324C"), Colors.White);
    }

    /// <summary>Formats a read-only activity view using a platform monospace face.</summary>
    /// <param name="editor">The existing bounded log control.</param>
    internal static void Activity(Editor editor)
    {
        editor.HeightRequest = 240;
        editor.FontSize = 12;
        editor.FontFamily = OperatingSystem.IsWindows() ? "Consolas" : OperatingSystem.IsIOS() ? "Menlo" : "monospace";
        editor.SetAppThemeColor(InputView.TextColorProperty, Color.FromArgb("#26374F"), Color.FromArgb("#D9E5F5"));
        editor.SetAppThemeColor(VisualElement.BackgroundColorProperty, Color.FromArgb("#F7F9FC"), Color.FromArgb("#171D26"));
    }

    /// <summary>Shows an action message only while it contains text.</summary>
    /// <param name="label">The existing operation result or error label.</param>
    internal static void Notice(Label label)
    {
        label.IsVisible = !string.IsNullOrWhiteSpace(label.Text);
        label.LineBreakMode = LineBreakMode.WordWrap;
        label.Padding = 12;
        label.SetAppThemeColor(Label.TextColorProperty, Color.FromArgb("#42526A"), Color.FromArgb("#CBD7E8"));
        label.SetAppThemeColor(VisualElement.BackgroundColorProperty, Color.FromArgb("#E7EDF5"), Color.FromArgb("#293444"));
        label.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(Label.Text))
            {
                label.IsVisible = !string.IsNullOrWhiteSpace(label.Text);
            }
        };
    }

    /// <summary>Places related sections side by side on wide screens and stacks them on narrow screens.</summary>
    /// <param name="first">The first section.</param>
    /// <param name="second">The second section.</param>
    /// <returns>The responsive paired layout.</returns>
    internal static View Pair(View first, View second)
    {
        var grid = new Grid { ColumnSpacing = 12, RowSpacing = 12,
            ColumnDefinitions = new ColumnDefinitionCollection { new(GridLength.Star), new(GridLength.Star) },
            RowDefinitions = new RowDefinitionCollection { new(GridLength.Auto), new(GridLength.Auto) } };
        grid.Children.Add(first);
        grid.Children.Add(second);
        grid.SizeChanged += (_, _) =>
        {
            bool narrow = grid.Width < 760;
            Grid.SetColumn(second, narrow ? 0 : 1);
            Grid.SetRow(second, narrow ? 1 : 0);
            Grid.SetColumnSpan(first, narrow ? 2 : 1);
            Grid.SetColumnSpan(second, narrow ? 2 : 1);
        };
        Grid.SetColumnSpan(first, 2);
        Grid.SetColumnSpan(second, 2);
        Grid.SetRow(second, 1);
        return grid;
    }
    /// <summary>Places scrolling example content above a fixed native connection status bar.</summary>
    /// <param name="body">The complete example controls to scroll.</param>
    /// <param name="status">The text showing connection state and configured endpoints.</param>
    /// <returns>A two-row layout whose status bar remains visible below the scrolling content.</returns>
    internal static Grid WithStatus(View body, Label status)
    {
        status.FontSize = 12;
        status.LineBreakMode = LineBreakMode.WordWrap;
        var bar = new Microsoft.Maui.Controls.Border
        {
            AutomationId = "StatusBar",
            Padding = new Thickness(12, 8),
            StrokeThickness = 1,
            Stroke = new SolidColorBrush(Microsoft.Maui.Graphics.Color.FromArgb("#808080")),
            Content = status
        };
        var frame = new Grid
        {
            RowDefinitions = new RowDefinitionCollection { new(GridLength.Star), new(GridLength.Auto) }
        };
        frame.Children.Add(new ScrollView { Content = body });
        Grid.SetRow(bar, 1);
        frame.Children.Add(bar);
        return frame;
    }

}