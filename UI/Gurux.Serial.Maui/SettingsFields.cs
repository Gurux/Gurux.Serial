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
using System.Globalization;
using Microsoft.Maui.Controls;
using Gurux.Serial.Maui.Properties;

namespace Gurux.Serial.Maui;

/// <summary>Builds native controls whose values are read only when saving.</summary>
internal sealed class SettingsFields
{
    /// <summary>Callbacks that update the private settings copy.</summary>
    private readonly List<Action> _read = [];
    /// <summary>Gets the native layout containing labels and input controls.</summary>
    internal VerticalStackLayout Layout { get; } = new() { Spacing = 8 };
    /// <summary>Creates an empty native form with deferred settings writers.</summary>
    internal SettingsFields() { }
    /// <summary>Copies the current control values into their settings targets.</summary>
    internal void Read()
    {
        foreach (Action read in _read) { read(); }
    }
    /// <summary>Adds a text input backed by a private settings property.</summary>
    /// <param name="label">Localized field label.</param><param name="value">Initial text.</param>
    /// <param name="set">Updates the private property.</param><param name="password">Masks sensitive text.</param>
    /// <param name="multiline">Allows multiple lines of text.</param>
    internal void Text(string label, string? value, Action<string> set, bool password = false, bool multiline = false)
    {
        Layout.Children.Add(new Label { Text = label });
        if (multiline)
        {
            var control = new Editor { Text = value, AutoSize = EditorAutoSizeOption.TextChanges };
            Layout.Children.Add(control);
            _read.Add(() => set(control.Text ?? string.Empty));
        }
        else
        {
            var control = new Entry { Text = value, IsPassword = password };
            Layout.Children.Add(control);
            _read.Add(() => set(control.Text ?? string.Empty));
        }
    }
    /// <summary>Adds an integer input parsed using the current culture.</summary>
    /// <param name="label">Localized field label.</param><param name="value">Initial integer.</param>
    /// <param name="set">Updates the private property.</param>
    internal void Number(string label, int value, Action<int> set)
    {
        var control = new Entry { Text = value.ToString(CultureInfo.CurrentCulture), Keyboard = Keyboard.Numeric };
        Layout.Children.Add(new Label { Text = label });
        Layout.Children.Add(control);
        _read.Add(() =>
        {
            if (!int.TryParse(control.Text, NumberStyles.Integer, CultureInfo.CurrentCulture, out int parsed))
            { throw new FormatException(string.Format(CultureInfo.CurrentCulture, Resources.WholeNumber, label)); }
            set(parsed);
        });
    }
    /// <summary>Adds a picker restricted to the supplied integer values without replacing an unknown initial value.</summary>
    /// <param name="label">The localized field label.</param>
    /// <param name="value">The configured value, selected only when present in the choices.</param>
    /// <param name="choices">The available values copied by this control.</param>
    /// <param name="set">Updates the private settings property when the enabled control is read; disabled values are preserved.</param>
    /// <exception cref="ArgumentNullException">The choices are null.</exception>
    internal void NumberChoice(string label, int value, int[] choices, Action<int> set)
    {
        ArgumentNullException.ThrowIfNull(choices);
        var available = choices.ToArray();
        var control = new Picker
        {
            ItemsSource = available.Select(item => item.ToString(CultureInfo.CurrentCulture)).ToArray(),
            SelectedIndex = Array.IndexOf(available, value)
        };
        Layout.Children.Add(new Label { Text = label });
        Layout.Children.Add(control);
        _read.Add(() =>
        {
            if (!control.IsEnabled) { return; }
            if (control.SelectedIndex < 0 || control.SelectedIndex >= available.Length)
            {
                throw new FormatException(string.Format(CultureInfo.CurrentCulture, Resources.SelectValue, label));
            }
            set(available[control.SelectedIndex]);
        });
    }
    /// <summary>Adds a native switch for a Boolean property.</summary>
    /// <param name="label">Localized field label.</param><param name="value">Initial state.</param>
    /// <param name="set">Updates the private property.</param>
    internal void Toggle(string label, bool value, Action<bool> set)
    {
        var control = new Switch { IsToggled = value };
        Layout.Children.Add(new Label { Text = label });
        Layout.Children.Add(control);
        _read.Add(() => set(control.IsToggled));
    }
    /// <summary>Adds a picker containing technical enum identifiers.</summary>
    /// <typeparam name="T">The settings enumeration.</typeparam>
    /// <param name="label">Localized field label.</param><param name="value">Initial selection.</param>
    /// <param name="set">Updates the private property only when the control is enabled.</param>
    internal void Choice<T>(string label, T value, Action<T> set) where T : struct, Enum
    {
        T[] values = Enum.GetValues<T>();
        var control = new Picker { ItemsSource = values.Select(item => item.ToString()).ToArray(), SelectedIndex = Array.IndexOf(values, value) };
        Layout.Children.Add(new Label { Text = label });
        Layout.Children.Add(control);
        _read.Add(() =>
        {
            if (!control.IsEnabled) { return; }
            if (control.SelectedIndex < 0)
            { throw new FormatException(string.Format(CultureInfo.CurrentCulture, Resources.SelectValue, label)); }
            set(values[control.SelectedIndex]);
        });
    }
}
