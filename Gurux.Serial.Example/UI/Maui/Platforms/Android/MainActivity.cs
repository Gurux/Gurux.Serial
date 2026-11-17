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

using Android.App;
using Android.Content;
using Android.Hardware.Usb;
using Android.OS;
using Gurux.Serial.Example.Platforms;
using Android.Content.PM;
using Microsoft.Maui;

namespace Gurux.Serial.Example.Host;

/// <summary>Hosts the native serial application activity.</summary>
[Activity(Theme = "@style/Maui.MainTheme.NoActionBar", MainLauncher = true, Exported = true, LaunchMode = LaunchMode.SingleTop,
    ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation |
        ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
[IntentFilter(new[] { UsbManager.ActionUsbDeviceAttached })]
[MetaData(UsbManager.ActionUsbDeviceAttached, Resource = "@xml/device_filter")]
public sealed class MainActivity : MauiAppCompatActivity
{
    /// <inheritdoc/>
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        ExampleSerialConnection.HandleUsbIntent(Intent);
        base.OnCreate(savedInstanceState);
    }

    /// <inheritdoc/>
    protected override void OnNewIntent(Intent? intent)
    {
        base.OnNewIntent(intent);
        Intent = intent;
        ExampleSerialConnection.HandleUsbIntent(intent);
    }
}
