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
using Gurux.Serial;

namespace Gurux.Serial.Blazor;
/// <summary>Owns a private unopened media copy until validated settings are committed.</summary>
public sealed class MediaSettingsDraft : IDisposable, IAsyncDisposable
{
    /// <summary>Tracks ownership and concurrent source changes.</summary>
    private readonly MediaSettingsSession _session;
    /// <summary>Builds and applies staged controls.</summary>
    private readonly SettingsFields _controls = new();
    /// <summary>Gets the staged controls in display order.</summary>
    public List<SettingsField> Fields => _controls.Fields;
    /// <summary>Gets whether required serial inputs are valid and any selected Bluetooth identity has a catalogue profile.</summary>
    public bool CanSave => _controls.CanSave;
    /// <summary>Gets the staged Bluetooth selector shared by the form and its private port controls.</summary>
    public BluetoothDeviceSelection Bluetooth => _controls.Bluetooth!;
    /// <summary>Creates an owned transaction over an unopened media copy.</summary>
    /// <param name="source">The caller-owned media.</param>
    /// <param name="copy">The editor-owned media copy.</param>
    private MediaSettingsDraft(IGXMedia source, IGXMedia copy) { _session = new(source, copy); }
    /// <summary>Copies settings without opening or taking ownership of the source.</summary>
    /// <param name="source">The closed media to edit.</param>
    /// <returns>An independently owned settings draft.</returns>
    /// <exception cref="ArgumentException">The media is not the exact supported type.</exception>
    public static MediaSettingsDraft Create(IGXMedia source)
    {
        return Create(source, Gurux.Serial.GXSerial.GetPortNames());
    }
    /// <summary>Copies media settings and offers only ports in the supplied discovery snapshot.</summary>
    /// <param name="source">The closed caller-owned media to edit.</param>
    /// <param name="portNames">Port names captured for this editing session; this array is copied.</param>
    /// <returns>An independently owned settings draft requiring an explicit available port selection.</returns>
    /// <exception cref="ArgumentNullException">The source or port snapshot is null.</exception>
    /// <exception cref="ArgumentException">The media is not the exact supported type.</exception>
    public static MediaSettingsDraft Create(IGXMedia source, string[] portNames)
    {
        ArgumentNullException.ThrowIfNull(portNames);
        ArgumentNullException.ThrowIfNull(source);
        if (source.GetType() != typeof(GXSerial)) { throw new ArgumentException(Properties.Resources.UnsupportedMedia, nameof(source)); }
        var copy = new GXSerial();
        var result = new MediaSettingsDraft(source, copy);
        try
        {
            Gurux.Serial.Blazor.SerialPortFields.Add(result._controls, copy.PortSettings, portNames);

            return result;
        }
        catch { result.Dispose(); throw; }
    }
    /// <summary>Validates staged settings and applies them if the closed source has not changed.</summary>
    /// <exception cref="InvalidOperationException">The source opened or changed during editing.</exception>
    /// <exception cref="ObjectDisposedException">The draft was already discarded.</exception>
    /// <exception cref="FormatException">No available port is selected, the Bluetooth identity has no catalogue profile, or a staged number is invalid.</exception>
    public void Commit() { _session.Commit(_controls.Apply); }
    /// <summary>Discards and disposes the private copy without disposing the caller's media.</summary>
    public void Dispose()
    {
        foreach (var field in Fields) { if (field.Kind == "password") { field.Value = string.Empty; } }
        _controls.Bluetooth?.Dispose();
        _session.Dispose();
    }
    /// <summary>Awaits metadata session cleanup before discarding the private media.</summary>
    /// <returns>Completion after all explicit metadata reads release their native resources.</returns>
    public async ValueTask DisposeAsync()
    {
        if (_controls.Bluetooth is not null) { await _controls.Bluetooth.DisposeAsync(); }
        Dispose();
    }
}
