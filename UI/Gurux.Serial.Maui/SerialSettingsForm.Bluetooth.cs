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
#if WINDOWS
using System.Globalization;
using Microsoft.Maui.Controls;
using SettingsResources = Gurux.Serial.Maui.Properties.Resources;

namespace Gurux.Serial.Maui;

/// <summary>Provides explicit desktop Bluetooth discovery and draft connection configuration.</summary>
public sealed partial class SerialSettingsForm
{
    /// <summary>Selects the native Bluetooth protocol for explicit device selection.</summary>
    private readonly Picker desktopBluetoothProtocol = new();
    /// <summary>Displays device names and native identifiers from explicit discovery and the saved selection.</summary>
    private readonly Picker desktopBluetoothDevices = new();
    /// <summary>Displays the selected Bluetooth address or native identifier without accepting input.</summary>
    private readonly Entry desktopBluetoothIdentifier = new() { IsReadOnly = true };
    /// <summary>Displays the selected BLE service UUID without allowing edits.</summary>
    private readonly Entry desktopBluetoothService = new() { IsReadOnly = true };
    /// <summary>Displays the catalogue notification characteristic UUID without accepting input.</summary>
    private readonly Entry desktopBluetoothRead = new() { IsReadOnly = true };
    /// <summary>Displays the catalogue write characteristic UUID without accepting input.</summary>
    private readonly Entry desktopBluetoothWrite = new() { IsReadOnly = true };
    /// <summary>Displays the optional catalogue configuration characteristic UUID without accepting input.</summary>
    private readonly Entry desktopBluetoothConfiguration = new() { IsReadOnly = true };
    /// <summary>Stores the canonical catalogue profile separately from read-only presentation text.</summary>
    private BluetoothLeProfile? desktopBluetoothProfile;
    /// <summary>Indicates that the live desktop Bluetooth profile fields have been initialized.</summary>
    private bool desktopBluetoothFieldsReady;
    /// <summary>Identifies the device owning the displayed service and configuration UUIDs.</summary>
    private string? desktopBluetoothProfilePort;
    /// <summary>Prevents transient picker selection events from rebinding an explicitly chosen Bluetooth profile.</summary>
    private bool selectingDesktopBluetoothPort;
    /// <summary>Displays localized enumeration and input feedback.</summary>
    private readonly Label desktopBluetoothFeedback = new() { LineBreakMode = LineBreakMode.WordWrap };
    /// <summary>Lists known RFCOMM devices or starts and stops nearby GATT discovery.</summary>
    private readonly Button desktopBluetoothRefresh = new();
    /// <summary>Contains controls specific to RFCOMM connections.</summary>
    private readonly VerticalStackLayout desktopBluetoothClassicFields = new() { Spacing = 8 };
    /// <summary>Maps displayed device names back to native identifiers.</summary>
    private BluetoothSerialDevice[] desktopBluetoothSnapshot = [];
    /// <summary>Prevents refreshed picker items from acting as a user device selection.</summary>
    private bool updatingDesktopBluetoothDevices;
    /// <summary>Owns cancellation for the current explicit enumeration.</summary>
    private CancellationTokenSource? desktopBluetoothDiscovery;
    /// <summary>Explains discovery for the selected Bluetooth protocol.</summary>
    private readonly Label desktopBluetoothDiscoveryHint = new() { LineBreakMode = LineBreakMode.WordWrap };
    /// <summary>Owns the active scan and invalidates queued callbacks when the form changes.</summary>
    private CancellationTokenSource? desktopBluetoothScanCancellation;
    /// <summary>Counts supported identities observed by the current scan independently of retained selections.</summary>
    private int desktopBluetoothScanCount;

    /// <summary>Adds Bluetooth inputs without requesting permissions, scanning or enumerating devices.</summary>
    private void AddDesktopBluetoothControls()
    {
        var panel = new VerticalStackLayout { Spacing = 8 };
        desktopBluetoothProtocol.ItemsSource = new[] { SettingsResources.BluetoothClassic, SettingsResources.BluetoothLowEnergy };
        desktopBluetoothProtocol.SelectedIndex = _settings.PortName?.StartsWith("ble:", StringComparison.OrdinalIgnoreCase) == true ? 1 : 0;
        desktopBluetoothRefresh.Text = SettingsResources.FindDevices;
        desktopBluetoothProfilePort = _settings.PortName;
        SetDesktopBluetoothProfile(GetSavedBluetoothProfile(_settings.PortName));
        AddDesktopBluetoothField(panel, SettingsResources.BluetoothTransport, desktopBluetoothProtocol);
        panel.Children.Add(desktopBluetoothRefresh);
        panel.Children.Add(desktopBluetoothDiscoveryHint);
        var information = new VerticalStackLayout { Spacing = 8, IsVisible = false, Padding = new(16, 0, 0, 0), AutomationId = "BluetoothInformationFields" };
        var informationToggle = new Button
        {
            Text = SettingsResources.BluetoothDeviceInformation,
            AutomationId = "BluetoothInformation",
            HorizontalOptions = LayoutOptions.Start,
            Padding = new(0, 2),
            MinimumHeightRequest = 0,
            MinimumWidthRequest = 0,
            BorderWidth = 0,
            CornerRadius = 0,
            BackgroundColor = Microsoft.Maui.Graphics.Colors.Transparent
        };
        informationToggle.SetAppThemeColor(Button.TextColorProperty, Microsoft.Maui.Graphics.Colors.Black, Microsoft.Maui.Graphics.Colors.White);
        SemanticProperties.SetDescription(informationToggle, SettingsResources.BluetoothInformationCollapsed);
        informationToggle.Clicked += (_, _) =>
        {
            information.IsVisible = !information.IsVisible;
            informationToggle.Text = information.IsVisible
                ? SettingsResources.BluetoothDeviceInformationExpanded : SettingsResources.BluetoothDeviceInformation;
            SemanticProperties.SetDescription(informationToggle, information.IsVisible
                ? SettingsResources.BluetoothInformationExpanded : SettingsResources.BluetoothInformationCollapsed);
        };
        AddDesktopBluetoothField(information, SettingsResources.BluetoothIdentifier, desktopBluetoothIdentifier);
        AddDesktopBluetoothField(information, SettingsResources.BluetoothService, desktopBluetoothService);
        AddDesktopBluetoothField(information, SettingsResources.BluetoothRead, desktopBluetoothRead);
        AddDesktopBluetoothField(information, SettingsResources.BluetoothWrite, desktopBluetoothWrite);
        AddDesktopBluetoothField(information, SettingsResources.BluetoothConfiguration, desktopBluetoothConfiguration);
        AddGattServiceControls(information);
        panel.Children.Add(desktopBluetoothClassicFields);
        panel.Children.Add(desktopBluetoothFeedback);
        AddDesktopBluetoothField(panel, SettingsResources.BluetoothDevices, desktopBluetoothDevices);
        panel.Children.Add(informationToggle);
        panel.Children.Add(information);
        AddBluetoothControl(panel);
        desktopBluetoothFieldsReady = true;
        UpdateDesktopBluetoothProtocol();
        UpdateUartAvailability();
        if (_settings.PortName is string selected && (selected.StartsWith("ble:", StringComparison.OrdinalIgnoreCase) || selected.StartsWith("bluetooth:", StringComparison.OrdinalIgnoreCase)))
        {
            desktopBluetoothIdentifier.Text = selected[(selected.IndexOf(':') + 1)..];
            SelectDesktopBluetoothPort(selected);
        }
        desktopBluetoothProtocol.SelectedIndexChanged += (_, _) => UpdateDesktopBluetoothProtocol();
        desktopBluetoothRefresh.Clicked += async (_, _) =>
        {
            if (desktopBluetoothProtocol.SelectedIndex != 1)
            {
                await RefreshDesktopBluetoothDevicesAsync();
            }
            else if (desktopBluetoothScanCancellation != null)
            {
                CancelDesktopBluetoothScan();
            }
            else
            {
                await ScanDesktopBluetoothDevicesAsync();
            }
        };
        desktopBluetoothDevices.SelectedIndexChanged += (_, _) =>
        {
            int index = desktopBluetoothDevices.SelectedIndex;
            if (!updatingDesktopBluetoothDevices && index >= 0 && index < desktopBluetoothSnapshot.Length)
            {
                BluetoothSerialDevice device = desktopBluetoothSnapshot[index];
                CancelDesktopBluetoothScan();
                desktopBluetoothIdentifier.Text = device.Id;
                {
                    string selectedPort = (device.Transport == BluetoothSerialTransport.LowEnergy ? "ble:" : "bluetooth:") + device.Id;
                    {
                        desktopBluetoothProfilePort = selectedPort;
                        SetDesktopBluetoothProfile(device.Transport == BluetoothSerialTransport.LowEnergy ? device.Profile ?? GetSavedBluetoothProfile(selectedPort) : null);
                    }
                }
                SelectDesktopBluetoothPort((device.Transport == BluetoothSerialTransport.LowEnergy ? "ble:" : "bluetooth:") + device.Id);
            }
        };
        HandlerChanging += (_, args) =>
        {
            if (args.NewHandler == null)
            {
                CancelDesktopBluetoothDiscovery();
            }
        };
    }

    /// <summary>Adds a localized caption and native input to a Bluetooth field group.</summary>
    /// <param name="layout">The field group receiving the input.</param>
    /// <param name="caption">The localized field caption.</param>
    /// <param name="input">The native input control.</param>
    private static void AddDesktopBluetoothField(VerticalStackLayout layout, string caption, View input)
    {
        layout.Children.Add(new Label { Text = caption });
        layout.Children.Add(input);
    }

    /// <summary>Switches protocol-specific fields and invalidates any older discovery snapshot.</summary>
    private void UpdateDesktopBluetoothProtocol()
    {
        CancelDesktopBluetoothDiscovery();
        desktopBluetoothDiscoveryHint.Text = desktopBluetoothProtocol.SelectedIndex == 1
            ? SettingsResources.FindBleDevicesHint : SettingsResources.FindKnownDevicesHint;
        ShowDesktopBluetoothDevices(Array.Empty<BluetoothSerialDevice>());
        desktopBluetoothClassicFields.IsVisible = desktopBluetoothProtocol.SelectedIndex != 1;
    }

    /// <summary>Lists serial-capable results while retaining both the original saved device and current selection without treating refreshes as user input.</summary>
    /// <param name="devices">The latest explicitly discovered devices.</param>
    private void ShowDesktopBluetoothDevices(IEnumerable<BluetoothSerialDevice> devices)
    {
        string prefix = desktopBluetoothProtocol.SelectedIndex == 1 ? "ble:" : "bluetooth:";
        string? selected = _ports.SelectedItem as string ?? _settings.PortName;
        var available = devices.Where(device => device.SupportsSerialPort).ToList();
        foreach (string? retainedPort in new[] { savedBluetoothPort, selected })
        {
            if (retainedPort?.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) == true
                && (prefix == "bluetooth:" || GetSavedBluetoothProfile(retainedPort) is not null || desktopBluetoothSnapshot.Any(device =>
                    string.Equals(prefix + device.Id, retainedPort, StringComparison.OrdinalIgnoreCase) && device.SupportsSerialPort))
                && !available.Any(device => string.Equals(prefix + device.Id, retainedPort, StringComparison.OrdinalIgnoreCase)))
            {
                string id = retainedPort[prefix.Length..];
                BluetoothSerialDevice? previous = desktopBluetoothSnapshot.FirstOrDefault(device =>
                    string.Equals(prefix + device.Id, retainedPort, StringComparison.OrdinalIgnoreCase));
                available.Add(previous ?? new BluetoothSerialDevice(id, id,
                    desktopBluetoothProtocol.SelectedIndex == 1 ? BluetoothSerialTransport.LowEnergy : BluetoothSerialTransport.Classic, id));
            }
        }
        updatingDesktopBluetoothDevices = true;
        try
        {
            desktopBluetoothSnapshot = available.ToArray();
            BluetoothSerialDevice? selectedDevice = desktopBluetoothSnapshot.FirstOrDefault(device =>
                string.Equals(prefix + device.Id, selected, StringComparison.OrdinalIgnoreCase));
            if (selectedDevice?.Profile is BluetoothLeProfile recognized)
            {
                desktopBluetoothProfilePort = prefix + selectedDevice.Id;
                desktopBluetoothIdentifier.Text = selectedDevice.Id;
                SetDesktopBluetoothProfile(recognized);
            }
            desktopBluetoothDevices.ItemsSource = desktopBluetoothSnapshot.Select(device =>
                string.Format(CultureInfo.CurrentCulture, SettingsResources.BleDeviceDisplay, device.Name, device.Address ?? device.Id)).ToArray();
            desktopBluetoothDevices.SelectedIndex = Array.FindIndex(desktopBluetoothSnapshot,
                device => string.Equals(prefix + device.Id, selected, StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            updatingDesktopBluetoothDevices = false;
        }
        UpdateUartAvailability();
    }
    /// <summary>Cancels an outstanding refresh and permits an explicit later refresh.</summary>
    private void CancelDesktopBluetoothDiscovery()
    {
        CancelDesktopBluetoothScan();
        CancellationTokenSource? previous = desktopBluetoothDiscovery;
        desktopBluetoothDiscovery = null;
        previous?.Cancel();
        desktopBluetoothRefresh.IsEnabled = true;
    }

    /// <summary>Stops scanning and prevents callbacks already queued on the dispatcher from updating the form.</summary>
    private void CancelDesktopBluetoothScan()
    {
        CancellationTokenSource? previous = desktopBluetoothScanCancellation;
        desktopBluetoothScanCancellation = null;
        previous?.Cancel();
        desktopBluetoothRefresh.Text = SettingsResources.FindDevices;
        desktopBluetoothRefresh.IsEnabled = true;
        if (previous != null)
        {
            desktopBluetoothFeedback.Text = desktopBluetoothScanCount == 0 ? SettingsResources.BleScanNoDevices
                : string.Format(CultureInfo.CurrentCulture, SettingsResources.BleScanComplete, desktopBluetoothScanCount);
        }
    }

    /// <summary>Scans nearby unpaired BLE devices for the default bounded duration and streams selectable results.</summary>
    /// <returns>A task completing after native discovery and localized feedback finish.</returns>
    private async Task ScanDesktopBluetoothDevicesAsync()
    {
        CancelDesktopBluetoothDiscovery();
        using var source = new CancellationTokenSource();
        desktopBluetoothScanCancellation = source;
        desktopBluetoothScanCount = 0;
        var found = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        desktopBluetoothRefresh.Text = SettingsResources.StopBleScan;
        desktopBluetoothFeedback.Text = SettingsResources.BleScanning;
        ShowDesktopBluetoothDevices(Array.Empty<BluetoothSerialDevice>());
        try
        {
            BluetoothSerialDevice[] snapshot = await BluetoothLeDiscovery.ScanAsync(deviceFound: device => Dispatcher.Dispatch(() =>
            {
                if (!ReferenceEquals(desktopBluetoothScanCancellation, source) || source.IsCancellationRequested || !device.SupportsSerialPort)
                {
                    return;
                }
                found.Add(device.Id);
                desktopBluetoothScanCount = found.Count;
                int index = Array.FindIndex(desktopBluetoothSnapshot, item => string.Equals(item.Id, device.Id, StringComparison.OrdinalIgnoreCase));
                if (index < 0)
                {
                    desktopBluetoothSnapshot = desktopBluetoothSnapshot.Append(device).ToArray();
                }
                else
                {
                    desktopBluetoothSnapshot[index] = device;
                }
                ShowDesktopBluetoothDevices(desktopBluetoothSnapshot);
            }), cancellationToken: source.Token);
            if (ReferenceEquals(desktopBluetoothScanCancellation, source))
            {
                desktopBluetoothScanCount = snapshot.Count(device => device.SupportsSerialPort);
                ShowDesktopBluetoothDevices(snapshot);
                desktopBluetoothFeedback.Text = desktopBluetoothScanCount == 0 ? SettingsResources.BleScanNoDevices
                    : string.Format(CultureInfo.CurrentCulture, SettingsResources.BleScanComplete, desktopBluetoothScanCount);
            }
        }
        catch (OperationCanceledException) when (source.IsCancellationRequested)
        {
        }
        catch (Exception)
        {
            if (ReferenceEquals(desktopBluetoothScanCancellation, source))
            {
                desktopBluetoothFeedback.Text = SettingsResources.BleScanFailed;
            }
        }
        finally
        {
            if (ReferenceEquals(desktopBluetoothScanCancellation, source))
            {
                desktopBluetoothScanCancellation = null;
                desktopBluetoothRefresh.Text = SettingsResources.FindDevices;
                desktopBluetoothRefresh.IsEnabled = true;
            }
        }
    }

    /// <summary>Enumerates only cached devices and ignores responses from a detached or superseded form.</summary>
    /// <returns>A task completing when enumeration and its localized feedback are applied.</returns>
    private async Task RefreshDesktopBluetoothDevicesAsync()
    {
        CancelDesktopBluetoothDiscovery();
        using var source = new CancellationTokenSource();
        desktopBluetoothDiscovery = source;
        desktopBluetoothRefresh.IsEnabled = false;
        try
        {
            BluetoothSerialTransport transport = desktopBluetoothProtocol.SelectedIndex == 1 ? BluetoothSerialTransport.LowEnergy : BluetoothSerialTransport.Classic;
            BluetoothSerialDevice[] devices = await BluetoothSerialTransports.GetDevicesAsync(transport, source.Token);
            if (ReferenceEquals(desktopBluetoothDiscovery, source) && !source.IsCancellationRequested)
            {
                ShowDesktopBluetoothDevices(devices);
                desktopBluetoothFeedback.Text = !devices.Any(device => device.SupportsSerialPort) ? SettingsResources.BluetoothNoDevices : string.Empty;
            }
        }
        catch (OperationCanceledException) when (source.IsCancellationRequested)
        {
        }
        catch (Exception)
        {
            if (ReferenceEquals(desktopBluetoothDiscovery, source))
            {
                desktopBluetoothFeedback.Text = SettingsResources.BluetoothRefreshFailed;
            }
        }
        finally
        {
            if (ReferenceEquals(desktopBluetoothDiscovery, source))
            {
                desktopBluetoothDiscovery = null;
                desktopBluetoothRefresh.IsEnabled = true;
            }
        }
    }

    /// <summary>Adds an explicitly selected Bluetooth identifier to the draft port snapshot.</summary>
    /// <param name="identifier">The complete bluetooth: or ble: port identifier.</param>
    private void SelectDesktopBluetoothPort(string identifier)
    {
        BindDesktopBluetoothProfile(identifier);
        selectingDesktopBluetoothPort = true;
        try
        {
            if (Array.IndexOf(_portNames, identifier) < 0)
            {
                RefreshPorts(_portNames.Append(identifier).ToArray());
            }
            _ports.SelectedItem = identifier;
        }
        finally
        {
            selectingDesktopBluetoothPort = false;
        }
        UpdateUartAvailability();
    }

    /// <summary>Associates draft GATT fields with one selected device instead of carrying them to another device.</summary>
    /// <param name="identifier">The complete selected port identifier; transient empty selections are ignored.</param>
    private void BindDesktopBluetoothProfile(string? identifier)
    {
        if (!desktopBluetoothFieldsReady || selectingDesktopBluetoothPort || identifier is null
            || string.Equals(desktopBluetoothProfilePort, identifier, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }
        desktopBluetoothProfilePort = identifier;
        desktopBluetoothIdentifier.Text = identifier[(identifier.IndexOf(':') + 1)..];
        if (identifier.StartsWith("ble:", StringComparison.OrdinalIgnoreCase) || identifier.StartsWith("bluetooth:", StringComparison.OrdinalIgnoreCase))
        {
            string prefix = identifier.StartsWith("ble:", StringComparison.OrdinalIgnoreCase) ? "ble:" : "bluetooth:";
            BluetoothLeProfile? discoveredProfile = desktopBluetoothSnapshot.FirstOrDefault(device =>
                string.Equals(prefix + device.Id, identifier, StringComparison.OrdinalIgnoreCase))?.Profile;
            SetDesktopBluetoothProfile(discoveredProfile ?? GetSavedBluetoothProfile(identifier));
        }
    }

    /// <summary>Copies a known or saved GATT profile into draft fields without changing media settings.</summary>
    /// <param name="profile">The supported profile whose canonical catalogue values are displayed, or null to clear information.</param>
    private void SetDesktopBluetoothProfile(BluetoothLeProfile? profile)
    {
        desktopBluetoothProfile = desktopBluetoothProtocol.SelectedIndex == 1 ? BluetoothLeProfile.FromSavedProfile(profile) : null;
        profile = desktopBluetoothProfile;
        desktopBluetoothService.Text = profile?.Service.ToString() ?? string.Empty;
        desktopBluetoothRead.Text = profile?.ReadCharacteristic.ToString() ?? string.Empty;
        desktopBluetoothWrite.Text = profile?.WriteCharacteristic.ToString() ?? string.Empty;
        desktopBluetoothConfiguration.Text = profile?.ConfigurationCharacteristic?.ToString() ?? string.Empty;
    }

    /// <summary>Validates the Classic address and selects automatic SDP channel resolution.</summary>
    /// <exception cref="FormatException">The selected Classic address is invalid.</exception>
    private void ReadDesktopBluetoothSettings()
    {
        string selected = _settings.PortName ?? string.Empty;
        if (selected.StartsWith("bluetooth:", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                _ = BluetoothSerialTransports.BluetoothRfcomm(selected["bluetooth:".Length..], 0);
            }
            catch (ArgumentException)
            {
                throw new FormatException(SettingsResources.BluetoothAddressInvalid);
            }
            _settings.BluetoothChannel = 0;
        }
    }
}
#endif
