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

using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;

namespace Gurux.Serial.Example;

/// <summary>Associates stable Android USB identity with each window's serial settings and existing JSON preferences.</summary>
public sealed class ExampleUsbDeviceSelection
{
    /// <summary>Keeps identity separate from the transport library and releases it with its settings owner.</summary>
    private static readonly ConditionalWeakTable<SerialPortSettings, ExampleUsbDeviceSelection> _selections = new();
    /// <summary>Identifies the device path for which this remembered identity was selected.</summary>
    private readonly string _deviceName;
    /// <summary>Identifies the selected USB vendor.</summary>
    private readonly int _vendorId;
    /// <summary>Identifies the selected USB product.</summary>
    private readonly int _productId;
    /// <summary>Distinguishes identical adapters when Android exposes a serial number.</summary>
    private readonly string? _serialNumber;

    /// <summary>Captures the immutable identity of a selected USB device.</summary>
    /// <param name="deviceName">The Android device path at selection time.</param>
    /// <param name="vendorId">The USB vendor identifier.</param>
    /// <param name="productId">The USB product identifier.</param>
    /// <param name="serialNumber">The hardware serial number, when available.</param>
    private ExampleUsbDeviceSelection(string deviceName, int vendorId, int productId, string? serialNumber)
    {
        this._deviceName = deviceName;
        this._vendorId = vendorId;
        this._productId = productId;
        this._serialNumber = serialNumber;
    }

    /// <summary>Remembers a selected adapter without changing transport settings or opening a device.</summary>
    /// <param name="settings">The settings owning this selection.</param>
    /// <param name="deviceName">The selected Android device path.</param>
    /// <param name="vendorId">The USB vendor identifier.</param>
    /// <param name="productId">The USB product identifier.</param>
    /// <param name="serialNumber">The hardware serial number, when available.</param>
    /// <exception cref="ArgumentNullException">The settings or device path is null.</exception>
    public static void Remember(SerialPortSettings settings, string deviceName, int vendorId, int productId, string? serialNumber)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(deviceName);
        _selections.AddOrUpdate(settings, new(deviceName, vendorId, productId, serialNumber));
    }

    /// <summary>Captures identity only while the selected port still belongs to the remembered adapter.</summary>
    /// <param name="settings">The settings whose identity should be persisted.</param>
    /// <returns>A detached JSON identity, or null when no matching USB selection exists.</returns>
    /// <exception cref="ArgumentNullException">The settings are null.</exception>
    public static JsonObject? Capture(SerialPortSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (!_selections.TryGetValue(settings, out var selected) || !MatchesPort(settings.PortName, selected._deviceName))
        {
            return null;
        }
        return new JsonObject
        {
            ["DeviceName"] = selected._deviceName,
            ["VendorId"] = selected._vendorId,
            ["ProductId"] = selected._productId,
            ["SerialNumber"] = selected._serialNumber
        };
    }

    /// <summary>Restores optional USB identity from this example's existing preference document.</summary>
    /// <param name="settings">The restored transport settings owning the identity.</param>
    /// <param name="values">The saved USB identity, or null for older preference files.</param>
    /// <exception cref="ArgumentNullException">The settings are null.</exception>
    /// <exception cref="InvalidOperationException">A saved identity value has an incompatible JSON type.</exception>
    public static void Restore(SerialPortSettings settings, JsonObject? values)
    {
        ArgumentNullException.ThrowIfNull(settings);
        _selections.Remove(settings);
        if (values is null)
        {
            return;
        }
        string? name = values["DeviceName"]?.GetValue<string>();
        int vendor = values["VendorId"]?.GetValue<int>() ?? -1;
        int product = values["ProductId"]?.GetValue<int>() ?? -1;
        if (!string.IsNullOrWhiteSpace(name) && vendor is >= 0 and <= 65535 && product is >= 0 and <= 65535
            && MatchesPort(settings.PortName, name))
        {
            Remember(settings, name, vendor, product, values["SerialNumber"]?.GetValue<string>());
        }
    }

    /// <summary>Resolves a unique remembered adapter across path changes, preserving legacy exact-path settings.</summary>
    /// <typeparam name="T">The platform device descriptor type.</typeparam>
    /// <param name="settings">The current serial selection.</param>
    /// <param name="devices">The attached device snapshot.</param>
    /// <param name="name">Reads an Android device path.</param>
    /// <param name="vendor">Reads a USB vendor identifier.</param>
    /// <param name="product">Reads a USB product identifier.</param>
    /// <param name="serial">Reads a serial number when access is already permitted.</param>
    /// <returns>The uniquely matching device, or null when a choice or permission is required.</returns>
    /// <exception cref="ArgumentNullException">The settings, device collection or a descriptor accessor is null.</exception>
    public static T? Resolve<T>(SerialPortSettings settings, IReadOnlyList<T> devices,
        Func<T, string> name, Func<T, int> vendor, Func<T, int> product, Func<T, string?> serial) where T : class
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(devices);
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(vendor);
        ArgumentNullException.ThrowIfNull(product);
        ArgumentNullException.ThrowIfNull(serial);
        if (!_selections.TryGetValue(settings, out var selected) || !MatchesPort(settings.PortName, selected._deviceName))
        {
            return devices.FirstOrDefault(device => MatchesPort(settings.PortName, name(device)));
        }
        var candidates = devices.Where(device => vendor(device) == selected._vendorId && product(device) == selected._productId).ToArray();
        if (!string.IsNullOrEmpty(selected._serialNumber))
        {
            var known = candidates.Where(device => string.Equals(serial(device), selected._serialNumber, StringComparison.Ordinal)).ToArray();
            if (known.Length != 0)
            {
                return known.Length == 1 ? known[0] : null;
            }
            candidates = candidates.Where(device => string.IsNullOrEmpty(serial(device))).ToArray();
        }
        return candidates.Length == 1 ? candidates[0] : null;
    }

    /// <summary>Compares the transport prefix without case while preserving Android device-path case.</summary>
    /// <param name="portName">The selected serial port identifier.</param>
    /// <param name="name">The Android USB device path.</param>
    /// <returns>True when the identifier selects this USB path.</returns>
    private static bool MatchesPort(string? portName, string name) => portName?.StartsWith("usb:", StringComparison.OrdinalIgnoreCase) == true
        && string.Equals(portName[4..], name, StringComparison.Ordinal);}