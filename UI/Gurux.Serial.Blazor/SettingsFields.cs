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
namespace Gurux.Serial.Blazor;
/// <summary>Builds localized controls and applies staged values to an owned media copy.</summary>
public sealed class SettingsFields
{
    /// <summary>Gets the controls in display order.</summary>
    public List<SettingsField> Fields { get; } = [];
    /// <summary>Stores callbacks that update only the private copy.</summary>
    private readonly List<Action> _read = [];
    /// <summary>Stores validity predicates for controls that require an explicit selection.</summary>
    private readonly List<Func<bool>> _canSave = [];
    /// <summary>Gets whether every required selection belongs to its available choices.</summary>
    public bool CanSave => _canSave.All(check => check()) && (Bluetooth?.HasCatalogueSelection ?? true);
    /// <summary>Gets the Bluetooth selection associated with this form's serial port.</summary>
    internal BluetoothDeviceSelection? Bluetooth { get; set; }
    /// <summary>Stages only explicitly chosen Bluetooth identities in the private port validation snapshot.</summary>
    internal Action<string>? SelectBluetoothPort { get; private set; }
    /// <summary>Adds a serial-port dropdown constrained to a snapshot of available names.</summary>
    /// <param name="label">The localized control label.</param>
    /// <param name="value">The configured port, selected only when present in the snapshot.</param>
    /// <param name="choices">The available port names; the builder retains an independent copy.</param>
    /// <param name="set">Applies the selected port to the private media copy.</param>
    /// <exception cref="ArgumentNullException">The choices are null.</exception>
    public void PortChoice(string label, string? value, string[] choices, Action<string> set)
    {
        ArgumentNullException.ThrowIfNull(choices);
        var available = choices.ToArray();
        var field = new SettingsField(label,
            value is not null && available.Contains(value, StringComparer.Ordinal) ? value : "", "port")
        {
            Choices = available.ToArray()
        };
        SelectBluetoothPort = selected =>
        {
            if ((!selected.StartsWith("ble:", StringComparison.OrdinalIgnoreCase)
                && !selected.StartsWith("bluetooth:", StringComparison.OrdinalIgnoreCase))
                || string.IsNullOrWhiteSpace(selected[(selected.IndexOf(':') + 1)..]))
            {
                throw new FormatException(Properties.Resources.BluetoothDeviceRequired);
            }
            if (!available.Contains(selected, StringComparer.Ordinal)) { available = available.Append(selected).ToArray(); }
            field.OfferChoice(selected);
            field.Value = selected;
        };
        // Validity always uses the private enumeration snapshot rather than mutable display choices.
        Func<bool> isValid = () => !string.IsNullOrEmpty(field.Value) && available.Contains(field.Value, StringComparer.Ordinal);
        Fields.Add(field);
        _canSave.Add(() => !field.IsEnabled || isValid());
        _read.Add(() =>
        {
            if (!field.IsEnabled) { return; }
            if (!isValid())
            {
                throw new FormatException(string.Format(CultureInfo.CurrentCulture, Properties.Resources.ChoiceError, label));
            }
            set(field.Value);
        });
    }
    /// <summary>Applies staged control values to the private copy.</summary>
    public void Apply() { foreach (var read in _read) { read(); } Bluetooth?.Apply(); }
    /// <summary>Adds a staged text or password control.</summary>
    /// <param name="label">Localized control label.</param>
    /// <param name="value">Initial value.</param>
    /// <param name="set">Applies the staged value to the private media copy.</param>
    /// <param name="password">Whether to mask the value.</param>
    /// <param name="multiline">Whether to allow multiple lines.</param>
    public void Text(string label, string? value, Action<string> set, bool password = false, bool multiline = false)
    {
        var field = new SettingsField(label, value ?? "", password ? "password" : multiline ? "multiline" : "text");
        Fields.Add(field);
        _read.Add(() => set(field.Value));
    }
    /// <summary>Adds a staged integer control.</summary>
    /// <param name="label">Localized control label.</param>
    /// <param name="value">Initial value.</param>
    /// <param name="set">Applies the staged value to the private media copy.</param>
    public void Number(string label, int value, Action<int> set)
    {
        var field = new SettingsField(label, value.ToString(CultureInfo.CurrentCulture), "number");
        Fields.Add(field);
        _read.Add(() =>
        {
            if (!int.TryParse(field.Value, NumberStyles.Integer, CultureInfo.CurrentCulture, out var parsed))
            {
                throw new FormatException(string.Format(CultureInfo.CurrentCulture, Properties.Resources.WholeNumberRequired, label));
            }
            set(parsed);
        });
    }
    /// <summary>Adds a dropdown restricted to supplied integers and validates against an independent choice snapshot.</summary>
    /// <param name="label">The localized control label.</param>
    /// <param name="value">The configured value, selected only when present in the choices.</param>
    /// <param name="choices">The available numeric values copied by the control.</param>
    /// <param name="set">Applies a valid selection when the control is enabled; disabled values are preserved.</param>
    /// <exception cref="ArgumentNullException">The choices are null.</exception>
    public void NumberChoice(string label, int value, int[] choices, Action<int> set)
    {
        ArgumentNullException.ThrowIfNull(choices);
        var available = choices.ToArray();
        var field = new SettingsField(label, available.Contains(value) ? value.ToString(CultureInfo.CurrentCulture) : string.Empty, "choice")
        {
            Choices = available.Select(item => item.ToString(CultureInfo.CurrentCulture)).ToArray()
        };
        Func<bool> isValid = () => int.TryParse(field.Value, NumberStyles.Integer, CultureInfo.CurrentCulture, out int selected) && available.Contains(selected);
        Fields.Add(field);
        _canSave.Add(() => !field.IsEnabled || isValid());
        _read.Add(() =>
        {
            if (!field.IsEnabled) { return; }
            if (!isValid())
            {
                throw new FormatException(string.Format(CultureInfo.CurrentCulture, Properties.Resources.ChoiceError, label));
            }
            set(int.Parse(field.Value, NumberStyles.Integer, CultureInfo.CurrentCulture));
        });
    }
    /// <summary>Adds a staged Boolean control.</summary>
    /// <param name="label">Localized control label.</param>
    /// <param name="value">Initial value.</param>
    /// <param name="set">Applies the staged value to the private media copy.</param>
    public void Toggle(string label, bool value, Action<bool> set)
    {
        var field = new SettingsField(label, "", "toggle") { Checked = value };
        Fields.Add(field);
        _read.Add(() => set(field.Checked));
    }
    /// <summary>Adds a staged enumeration control.</summary>
    /// <param name="label">Localized control label.</param>
    /// <param name="value">Initial value.</param>
    /// <param name="set">Applies the staged value only when the control is enabled.</param>
    /// <typeparam name="T">The enumeration represented by the control.</typeparam>
    public void Choice<T>(string label, T value, Action<T> set) where T : struct, Enum
    {
        var field = new SettingsField(label, value.ToString(), "choice") { Choices = Enum.GetNames<T>() };
        Fields.Add(field);
        _read.Add(() => { if (field.IsEnabled) { set(Enum.Parse<T>(field.Value)); } });
    }
}
