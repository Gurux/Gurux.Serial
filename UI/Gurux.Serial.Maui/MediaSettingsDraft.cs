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
using Gurux.Serial.Maui.Properties;

namespace Gurux.Serial.Maui;

/// <summary>Connects native controls to an unopened private media copy.</summary>
internal sealed class MediaSettingsDraft : IDisposable, IAsyncDisposable
{
    /// <summary>Owns the staged copy and conflict checks.</summary>
    private readonly MediaSettingsSession _session;
    /// <summary>Reads this media's native fields.</summary>
    private readonly SettingsFields _fields;
    /// <summary>Owns the shared serial picker and deferred settings readers.</summary>
    private readonly Gurux.Serial.Maui.SerialSettingsForm _serial;
    /// <summary>Gets the native form layout.</summary>
    internal View Form => _fields.Layout;
    /// <summary>Copies settings and creates fields without opening a transport.</summary>
    /// <param name="source">The closed media whose settings will be staged.</param>
    internal MediaSettingsDraft(GXSerial source)
        : this(source, settings => new SerialSettingsForm(settings))
    {
    }
    /// <summary>Creates staged controls from a port enumeration captured once for this dialog.</summary>
    /// <param name="source">The closed media whose settings will be staged.</param>
    /// <param name="getPortNames">Provides the available port names without changing transport behavior.</param>
    internal MediaSettingsDraft(GXSerial source, Func<string[]> getPortNames)
        : this(source, settings => new SerialSettingsForm(settings, getPortNames()))
    {
    }
    /// <summary>Creates one private form with the requested platform discovery policy.</summary>
    /// <param name="source">The closed media whose settings will be staged.</param>
    /// <param name="createForm">Creates controls for the private port settings.</param>
    private MediaSettingsDraft(GXSerial source, Func<SerialPortSettings, SerialSettingsForm> createForm)
    {
        var copy = new GXSerial();
        _session = new MediaSettingsSession(source, copy);
        _fields = new SettingsFields();
        try
        {
            SettingsFields fields = _fields;
            _serial = createForm(copy.PortSettings);
            fields.Layout.Children.Add(_serial);
        }
        catch { _session.Dispose(); throw; }
    }
    /// <summary>Gets whether the shared serial form has a valid enumerated port selected.</summary>
    internal bool CanSave => _serial.CanSave;
    /// <summary>Notifies the view immediately when serial selection changes save availability.</summary>
    internal event EventHandler? CanSaveChanged
    {
        add { _serial.CanSaveChanged += value; }
        remove { _serial.CanSaveChanged -= value; }
    }
    /// <summary>Reads native controls and saves only after validation and conflict checks.</summary>
    /// <exception cref="FormatException">No enumerated port is selected or another input is invalid.</exception>
    internal void Commit() => _session.Commit(() => { _serial.Read(); _fields.Read(); });
    /// <summary>Disconnects platform controls to stop device watchers and releases the unopened private media instance.</summary>
    public void Dispose()
    {
        _serial.Handler?.DisconnectHandler();
        _serial.Handler = null;
        (_serial as object as IDisposable)?.Dispose();
        _session.Dispose();
    }
    /// <summary>Cancels metadata discovery and awaits native cleanup before detaching controls and releasing the private media.</summary>
    /// <returns>Completion after the native metadata session has ended.</returns>
    public async ValueTask DisposeAsync()
    {
        await _serial.DisposeAsync();
        Dispose();
    }
}
