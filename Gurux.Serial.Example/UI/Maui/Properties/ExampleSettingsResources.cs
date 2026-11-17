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

using System.Resources;

namespace Gurux.Serial.Example.Maui.Properties;

/// <summary>Localized messages for this example's settings dispatcher.</summary>
internal static class ExampleSettingsResources
{
    /// <summary>Reads this assembly's embedded settings messages.</summary>
    private static readonly ResourceManager Manager = new("Gurux.Serial.Example.Maui.Properties.ExampleSettingsResources", typeof(ExampleSettingsResources).Assembly);

    /// <summary>Explains that a settings operation is already in progress.</summary>
    internal static string EditorAlreadyShowing => Manager.GetString(nameof(EditorAlreadyShowing), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(EditorAlreadyShowing);
    /// <summary>Gets the localized Connection caption for the example interface.</summary>
    internal static string ConnectionGroup => Manager.GetString(nameof(ConnectionGroup), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(ConnectionGroup);
    /// <summary>Gets the localized Data exchange caption for the example interface.</summary>
    internal static string DataGroup => Manager.GetString(nameof(DataGroup), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(DataGroup);
    /// <summary>Gets the localized Reply settings caption for the example interface.</summary>
    internal static string ReplyGroup => Manager.GetString(nameof(ReplyGroup), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(ReplyGroup);
    /// <summary>Gets the localized Activity log caption for the example interface.</summary>
    internal static string ActivityGroup => Manager.GetString(nameof(ActivityGroup), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(ActivityGroup);
    /// <summary>Gets the localized Connected caption for the example interface.</summary>
    internal static string ConnectionActive => Manager.GetString(nameof(ConnectionActive), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(ConnectionActive);
    /// <summary>Gets the localized Disconnected caption for the example interface.</summary>
    internal static string ConnectionInactive => Manager.GetString(nameof(ConnectionInactive), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(ConnectionInactive);
    /// <summary>Gets the localized Working… caption for the example interface.</summary>
    internal static string ConnectionBusy => Manager.GetString(nameof(ConnectionBusy), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(ConnectionBusy);
    /// <summary>Gets the localized Data caption for the example interface.</summary>
    internal static string PayloadLabel => Manager.GetString(nameof(PayloadLabel), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(PayloadLabel);
    /// <summary>Gets the localized Minimum reply bytes caption for the example interface.</summary>
    internal static string CountLabel => Manager.GetString(nameof(CountLabel), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(CountLabel);
    /// <summary>Gets the localized End of packet caption for the example interface.</summary>
    internal static string TerminatorLabel => Manager.GetString(nameof(TerminatorLabel), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(TerminatorLabel);
    /// <summary>Gets the localized Timeout (ms) caption for the example interface.</summary>
    internal static string TimeoutLabel => Manager.GetString(nameof(TimeoutLabel), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(TimeoutLabel);
    /// <summary>Gets the localized state caption for an open media connection.</summary>
    internal static string StatusOpen => Manager.GetString(nameof(StatusOpen), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(StatusOpen);

    /// <summary>Gets the localized state caption for a closed media connection.</summary>
    internal static string StatusClosed => Manager.GetString(nameof(StatusClosed), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(StatusClosed);

    /// <summary>Gets the localized placeholder for an unselected endpoint.</summary>
    internal static string StatusNotConfigured => Manager.GetString(nameof(StatusNotConfigured), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(StatusNotConfigured);

    /// <summary>Gets the localized format for connection state and selected serial port.</summary>
    internal static string StatusSerial => Manager.GetString(nameof(StatusSerial), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(StatusSerial);

}
