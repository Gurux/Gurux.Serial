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
using System.Resources;
namespace Gurux.Serial.Avalonia.Properties;
/// <summary>Retrieves translated settings captions and validation messages.</summary>
internal static class Resources
{
    /// <summary>Resolves resources from this adapter assembly.</summary>
    private static readonly ResourceManager Manager = new("Gurux.Serial.Avalonia.Properties.Resources", typeof(Resources).Assembly);
    /// <summary>Retrieves a resource using the current user interface culture.</summary>
    /// <param name="key">The stable resource identifier.</param>
    /// <returns>The translated text.</returns>
    internal static string Get(string key) => Manager.GetString(key, CultureInfo.CurrentUICulture)!;
}
