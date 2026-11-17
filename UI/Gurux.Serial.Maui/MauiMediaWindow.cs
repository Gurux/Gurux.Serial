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

/// <summary>Adapts a native MAUI settings owner to the Gurux window contract.</summary>
public sealed class MauiMediaWindow : IGXWindow
{
    /// <summary>Gets the page owning the settings overlay.</summary>
    public Page Page { get; }
    /// <summary>Uses a page as the settings owner.</summary>
    /// <param name="page">The non-null owner page.</param>
    /// <exception cref="ArgumentNullException">The page is null.</exception>
    public MauiMediaWindow(Page page) { Page = page ?? throw new ArgumentNullException(nameof(page)); }
    /// <summary>Uses the page currently assigned to a MAUI window.</summary>
    /// <param name="window">The native window with an assigned page.</param>
    /// <exception cref="ArgumentException">The window has no page.</exception>
    public MauiMediaWindow(Microsoft.Maui.Controls.Window window)
        : this(window?.Page ?? throw new ArgumentException(Resources.WindowPage, nameof(window))) { }
}
