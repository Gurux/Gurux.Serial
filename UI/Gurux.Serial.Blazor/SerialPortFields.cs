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
namespace Gurux.Serial.Blazor;
/// <summary>Reuses the serial port controls in serial, terminal and SMS settings forms.</summary>
public static class SerialPortFields
{
    /// <summary>Adds staged controls for the composed serial port settings.</summary>
    /// <param name="fields">The staged control builder.</param>
    /// <param name="settings">The private serial settings copy.</param>
    public static void Add(SettingsFields fields, SerialPortSettings settings)
    {
        Add(fields, settings, GXSerial.GetPortNames());
    }
    /// <summary>Adds staged serial controls using a snapshot of available port names.</summary>
    /// <remarks>Baud rates use common-rate choices. Bluetooth baud, parity and stop-bit fields require a configuration characteristic and a registered command generator; physical ports remain editable.</remarks>
    /// <param name="fields">The staged control builder.</param>
    /// <param name="settings">The private serial settings copy.</param>
    /// <param name="portNames">Available ports captured when the editing session opens.</param>
    /// <exception cref="ArgumentNullException">The control builder, settings or port snapshot is null.</exception>
    public static void Add(SettingsFields fields, SerialPortSettings settings, string[] portNames)
    {
        ArgumentNullException.ThrowIfNull(fields);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(portNames);
        string[] ports = (string[])portNames.Clone();
        if ((settings.PortName?.StartsWith("bluetooth:", StringComparison.OrdinalIgnoreCase) == true
            || settings.PortName?.StartsWith("ble:", StringComparison.OrdinalIgnoreCase) == true)
            && !ports.Contains(settings.PortName, StringComparer.Ordinal))
        {
            ports = ports.Append(settings.PortName!).ToArray();
        }
        string? profilePort = settings.PortName;
        fields.PortChoice(Properties.Resources.Portname, settings.PortName, ports, value =>
        {
            if (!string.Equals(value, profilePort, StringComparison.Ordinal)
                && (value.StartsWith("bluetooth:", StringComparison.OrdinalIgnoreCase) || value.StartsWith("ble:", StringComparison.OrdinalIgnoreCase)))
            {
                settings.BluetoothProfile = null;
            }
            settings.PortName = value;
        });
        SettingsField port = fields.Fields[^1];
        fields.Bluetooth = new BluetoothDeviceSelection(settings, port, fields.SelectBluetoothPort!);
        fields.NumberChoice(Properties.Resources.Baudrate, settings.BaudRate, GXSerial.GetAvailableBaudRates(settings.PortName), value => settings.BaudRate = value);
        SettingsField baud = fields.Fields[^1];
        fields.NumberChoice(Properties.Resources.Databits, settings.DataBits, [5, 6, 7, 8], value => settings.DataBits = value);
        fields.Choice(Properties.Resources.Parity, settings.Parity, value => settings.Parity = value);
        SettingsField parity = fields.Fields[^1];
        fields.Choice(Properties.Resources.Stopbits, settings.StopBits, value => settings.StopBits = value);
        SettingsField stop = fields.Fields[^1];
        fields.Choice(Properties.Resources.Handshake, settings.Handshake, value => settings.Handshake = value);
        fields.Toggle(Properties.Resources.DTRenabled, settings.DtrEnable, value => settings.DtrEnable = value);
        fields.Toggle(Properties.Resources.RTSenabled, settings.RtsEnable, value => settings.RtsEnable = value);
        Func<bool> configurable = () =>
        {
            string selected = port.Value;
            bool bluetooth = selected.StartsWith("bluetooth:", StringComparison.OrdinalIgnoreCase)
                || selected.StartsWith("ble:", StringComparison.OrdinalIgnoreCase);
            return !string.IsNullOrEmpty(selected) && (!bluetooth
                || (fields.Bluetooth.Profile?.CanConfigureUart == true));
        };
        foreach (var field in new[] { baud, parity, stop })
        {
            field.CanEdit = configurable;
        }
    }
}
