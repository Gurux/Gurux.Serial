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

using Microsoft.Maui;
using Microsoft.Maui.Controls;
namespace Gurux.Serial.Example.Maui;
/// <summary>Creates serial windows with navigation hosting for desktop menus and mobile navigation.</summary>
public sealed class MediaExampleApplication : Application
{
    /// <summary>The optional platform serial stream factory passed to each window.</summary>
    private readonly Func<SerialPortSettings, CancellationToken, ValueTask<Stream>>? serialFactory;
    /// <summary>Creates an application using the supplied platform transport.</summary>
    /// <param name="serialFactory">An optional platform serial stream factory.</param>
    public MediaExampleApplication(Func<SerialPortSettings, CancellationToken, ValueTask<Stream>>? serialFactory = null)
    {
        this.serialFactory = serialFactory;
    }
    /// <inheritdoc/>
    protected override Window CreateWindow(IActivationState? activationState)
    {
        var page = new MediaExamplePage(serialFactory);
        var window = new Window(new NavigationPage(page)) { Title = global::Gurux.Serial.Example.Properties.ExampleResources.Title };
        if (OperatingSystem.IsWindows())
        {
            window.Width = 820;
            window.Height = 800;
            window.MinimumWidth = 420;
            window.MinimumHeight = 560;
        }
        window.Stopped += async (_, _) => { await page.SaveStateAsync(); };
        window.Destroying += async (_, _) => { await page.ShutdownAsync(); };
        return window;
    }
}
