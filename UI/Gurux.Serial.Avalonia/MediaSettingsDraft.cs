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
using Avalonia.Controls;
using Gurux.Common;
using Gurux.Serial;
using Gurux.Serial.Avalonia.Properties;
namespace Gurux.Serial.Avalonia;
/// <summary>Owns the typed form and its private validated media draft.</summary>
internal sealed class MediaSettingsDraft : IDisposable, IAsyncDisposable
{
    /// <summary>Owns the isolated media copy and conflict checks.</summary>
    private readonly MediaSettingsSession _session;
    /// <summary>Stores the controls and their draft setters.</summary>
    private readonly SettingsForm _form;
    /// <summary>Owns the tabbed view over the same staged controls for the lifetime of this draft.</summary>
    private readonly Control _view;
    /// <summary>Initializes a staged form.</summary>
    /// <param name="session">The private media session.</param>
    /// <param name="form">The typed controls for this session.</param>
    private MediaSettingsDraft(MediaSettingsSession session, SettingsForm form) { _session = session; _form = form; _view = form.CreateView(); }
    /// <summary>Gets the native tabbed editor without rebuilding or resetting its staged fields.</summary>
    internal Control Form => _view;
    /// <summary>Gets the translated labels and corresponding controls.</summary>
    internal List<(string Label, Control View)> Fields => _form.Fields;
    /// <summary>Gets whether a discovered serial port is selected.</summary>
    internal bool CanSave => _form.CanSave;
    /// <summary>Occurs when selecting a port changes whether the form can be saved.</summary>
    internal event EventHandler? CanSaveChanged
    {
        add { _form.CanSaveChanged += value; }
        remove { _form.CanSaveChanged -= value; }
    }
    /// <summary>Copies settings and constructs this adapter's typed controls.</summary>
    /// <param name="source">The live media, whose exact type must match this adapter.</param>
    /// <returns>An isolated form ready for display.</returns>
    /// <exception cref="NotSupportedException">The source belongs to a different media adapter.</exception>
    internal static MediaSettingsDraft Create(IGXMedia source) => Create(source, global::Gurux.Serial.GXSerial.GetPortNames());
    /// <summary>Constructs a private form using a snapshot of discovered serial port names.</summary>
    /// <param name="source">The live media whose settings are staged.</param>
    /// <param name="portNames">The discovered names allowed by the port selector.</param>
    /// <returns>An owned form that can be saved only with a discovered port selection.</returns>
    /// <exception cref="NotSupportedException">The media type is not supported by this adapter.</exception>
    internal static MediaSettingsDraft Create(IGXMedia source, IReadOnlyList<string> portNames)
    {
        if (source.GetType() != typeof(GXSerial)) { throw new NotSupportedException(Resources.Get("Unsupported")); }
        var copy = new GXSerial();
        var session = new MediaSettingsSession(source, copy);
        try
        {
            var form = new SettingsForm();
            form.AddSerial(copy.PortSettings, portNames);
            return new MediaSettingsDraft(session, form);
        }
        catch { session.Dispose(); throw; }
    }
    /// <summary>Validates form values and saves them if the source remains unchanged and closed.</summary>
    internal void Commit() => _session.Commit(_form.Read);
    /// <inheritdoc/>
    public void Dispose() { _form.Dispose(); _session.Dispose(); }
    /// <summary>Cancels metadata discovery and awaits native cleanup before releasing the private media.</summary>
    /// <returns>Completion after the native metadata session has ended.</returns>
    public async ValueTask DisposeAsync()
    {
        await _form.DisposeAsync();
        _session.Dispose();
    }
}
