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
using Gurux.UI.Components;
using Gurux.UI.Components.Appearance;
using Microsoft.Extensions.DependencyInjection;

namespace Gurux.Serial.Blazor;

/// <summary>Registers the optional settings component appearance services.</summary>
public static class MediaComponentServices
{
    /// <summary>Registers adaptive appearance using the browser platform.</summary>
    /// <param name="services">The host service collection.</param>
    /// <returns>The same collection for chained registrations.</returns>
    public static IServiceCollection AddGuruxMediaComponents(this IServiceCollection services)
    {
        services.AddGXAppearance(options => options.Defaults.Layout = Gurux.UI.Components.Enums.LayoutStyle.Auto);
        return services;
    }
}
