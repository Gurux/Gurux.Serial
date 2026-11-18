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
    private readonly Picker _desktopBluetoothProtocol = new();
    /// <summary>Displays device names and native identifiers from explicit discovery and the saved selection.</summary>
    private readonly Picker _desktopBluetoothDevices = new();
    /// <summary>Displays the selected Bluetooth address or native identifier without accepting input.</summary>
    private readonly Entry _desktopBluetoothIdentifier = new() { IsReadOnly = true };
    /// <summary>Displays the selected BLE service UUID without allowing edits.</summary>
    private readonly Entry _desktopBluetoothService = new() { IsReadOnly = true };
    /// <summary>Displays the catalogue notification characteristic UUID without accepting input.</summary>
    private readonly Entry _desktopBluetoothRead = new() { IsReadOnly = true };
    /// <summary>Displays the catalogue write characteristic UUID without accepting input.</summary>
    private readonly Entry _desktopBluetoothWrite = new() { IsReadOnly = true };
    /// <summary>Displays the optional catalogue configuration characteristic UUID without accepting input.</summary>
    private readonly Entry _desktopBluetoothConfiguration = new() { IsReadOnly = true };
    /// <summary>Stores the canonical catalogue profile separately from read-only presentation text.</summary>
    private BluetoothLeProfile? _desktopBluetoothProfile;
    /// <summary>Indicates that the live desktop Bluetooth profile fields have been initialized.</summary>
    private bool _desktopBluetoothFieldsReady;
    /// <summary>Identifies the device owning the displayed service and configuration UUIDs.</summary>
    private string? _desktopBluetoothProfilePort;
    /// <summary>Prevents transient picker selection events from rebinding an explicitly chosen Bluetooth profile.</summary>
    private bool _selectingDesktopBluetoothPort;
    /// <summary>Displays localized enumeration and input feedback.</summary>
    private readonly Label _desktopBluetoothFeedback = new() { LineBreakMode = LineBreakMode.WordWrap };
    /// <summary>Lists known RFCOMM devices or starts and stops nearby GATT discovery.</summary>
    private readonly Button _desktopBluetoothRefresh = new();
    /// <summary>Contains controls specific to RFCOMM connections.</summary>
    private readonly VerticalStackLayout _desktopBluetoothClassicFields = new() { Spacing = 8 };
    /// <summary>Maps displayed device names back to native identifiers.</summary>
    private BluetoothSerialDevice[] _desktopBluetoothSnapshot = [];
    /// <summary>Prevents refreshed picker items from acting as a user device selection.</summary>
    private bool _updatingDesktopBluetoothDevices;
    /// <summary>Owns cancellation for the current explicit enumeration.</summary>
    private CancellationTokenSource? _desktopBluetoothDiscovery;
    /// <summary>Explains discovery for the selected Bluetooth protocol.</summary>
    private readonly Label _desktopBluetoothDiscoveryHint = new() { LineBreakMode = LineBreakMode.WordWrap };
    /// <summary>Owns the active scan and invalidates queued callbacks when the form changes.</summary>
    private CancellationTokenSource? _desktopBluetoothScanCancellation;
    /// <summary>Counts supported identities observed by the current scan independently of retained selections.</summary>
    private int _desktopBluetoothScanCount;

    /// <summary>Adds Bluetooth inputs without requesting permissions, scanning or enumerating devices.</summary>
    private void AddDesktopBluetoothControls()
    {
        var panel = new VerticalStackLayout { Spacing = 8 };
        _desktopBluetoothProtocol.ItemsSource = new[] { SettingsResources.BluetoothClassic, SettingsResources.BluetoothLowEnergy };
        _desktopBluetoothProtocol.SelectedIndex = _settings.PortName?.StartsWith("ble:", StringComparison.OrdinalIgnoreCase) == true ? 1 : 0;
        _desktopBluetoothRefresh.Text = SettingsResources.FindDevices;
        _desktopBluetoothProfilePort = _settings.PortName;
        SetDesktopBluetoothProfile(GetSavedBluetoothProfile(_settings.PortName));
        AddDesktopBluetoothField(panel, SettingsResources.BluetoothTransport, _desktopBluetoothProtocol);
        panel.Children.Add(_desktopBluetoothRefresh);
        panel.Children.Add(_desktopBluetoothDiscoveryHint);
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
        AddDesktopBluetoothField(information, SettingsResources.BluetoothIdentifier, _desktopBluetoothIdentifier);
        AddDesktopBluetoothField(information, SettingsResources.BluetoothService, _desktopBluetoothService);
        AddDesktopBluetoothField(information, SettingsResources.BluetoothRead, _desktopBluetoothRead);
        AddDesktopBluetoothField(information, SettingsResources.BluetoothWrite, _desktopBluetoothWrite);
        AddDesktopBluetoothField(information, SettingsResources.BluetoothConfiguration, _desktopBluetoothConfiguration);
        AddGattServiceControls(information);
        panel.Children.Add(_desktopBluetoothClassicFields);
        panel.Children.Add(_desktopBluetoothFeedback);
        AddDesktopBluetoothField(panel, SettingsResources.BluetoothDevices, _desktopBluetoothDevices);
        panel.Children.Add(informationToggle);
        panel.Children.Add(information);
        AddBluetoothControl(panel);
        _desktopBluetoothFieldsReady = true;
        UpdateDesktopBluetoothProtocol();
        UpdateUartAvailability();
        if (_settings.PortName is string selected && (selected.StartsWith("ble:", StringComparison.OrdinalIgnoreCase) || selected.StartsWith("bluetooth:", StringComparison.OrdinalIgnoreCase)))
        {
            _desktopBluetoothIdentifier.Text = selected[(selected.IndexOf(':') + 1)..];
            SelectDesktopBluetoothPort(selected);
        }
        _desktopBluetoothProtocol.SelectedIndexChanged += (_, _) => UpdateDesktopBluetoothProtocol();
        _desktopBluetoothRefresh.Clicked += async (_, _) =>
        {
            if (_desktopBluetoothProtocol.SelectedIndex != 1)
            {
                await RefreshDesktopBluetoothDevicesAsync();
            }
            else if (_desktopBluetoothScanCancellation != null)
            {
                CancelDesktopBluetoothScan();
            }
            else
            {
                await ScanDesktopBluetoothDevicesAsync();
            }
        };
        _desktopBluetoothDevices.SelectedIndexChanged += (_, _) =>
        {
            int index = _desktopBluetoothDevices.SelectedIndex;
            if (!_updatingDesktopBluetoothDevices && index >= 0 && index < _desktopBluetoothSnapshot.Length)
            {
                BluetoothSerialDevice device = _desktopBluetoothSnapshot[index];
                CancelDesktopBluetoothScan();
                _desktopBluetoothIdentifier.Text = device.Id;
                {
                    string selectedPort = (device.Transport == BluetoothSerialTransport.LowEnergy ? "ble:" : "bluetooth:") + device.Id;
                    {
                        _desktopBluetoothProfilePort = selectedPort;
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
        _desktopBluetoothDiscoveryHint.Text = _desktopBluetoothProtocol.SelectedIndex == 1
            ? SettingsResources.FindBleDevicesHint : SettingsResources.FindKnownDevicesHint;
        ShowDesktopBluetoothDevices(Array.Empty<BluetoothSerialDevice>());
        _desktopBluetoothClassicFields.IsVisible = _desktopBluetoothProtocol.SelectedIndex != 1;
    }

    /// <summary>Lists serial-capable results while retaining both the original saved device and current selection without treating refreshes as user input.</summary>
    /// <param name="devices">The latest explicitly discovered devices.</param>
    private void ShowDesktopBluetoothDevices(IEnumerable<BluetoothSerialDevice> devices)
    {
        string prefix = _desktopBluetoothProtocol.SelectedIndex == 1 ? "ble:" : "bluetooth:";
        string? selected = _ports.SelectedItem as string ?? _settings.PortName;
        var available = devices.Where(device => device.SupportsSerialPort).ToList();
        foreach (string? retainedPort in new[] { _savedBluetoothPort, selected })
        {
            if (retainedPort?.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) == true
                && (prefix == "bluetooth:" || GetSavedBluetoothProfile(retainedPort) is not null || _desktopBluetoothSnapshot.Any(device =>
                    string.Equals(prefix + device.Id, retainedPort, StringComparison.OrdinalIgnoreCase) && device.SupportsSerialPort))
                && !available.Any(device => string.Equals(prefix + device.Id, retainedPort, StringComparison.OrdinalIgnoreCase)))
            {
                string id = retainedPort[prefix.Length..];
                BluetoothSerialDevice? previous = _desktopBluetoothSnapshot.FirstOrDefault(device =>
                    string.Equals(prefix + device.Id, retainedPort, StringComparison.OrdinalIgnoreCase));
                available.Add(previous ?? new BluetoothSerialDevice(id, id,
                    _desktopBluetoothProtocol.SelectedIndex == 1 ? BluetoothSerialTransport.LowEnergy : BluetoothSerialTransport.Classic, id));
            }
        }
        _updatingDesktopBluetoothDevices = true;
        try
        {
            _desktopBluetoothSnapshot = available.ToArray();
            BluetoothSerialDevice? selectedDevice = _desktopBluetoothSnapshot.FirstOrDefault(device =>
                string.Equals(prefix + device.Id, selected, StringComparison.OrdinalIgnoreCase));
            if (selectedDevice?.Profile is BluetoothLeProfile recognized)
            {
                _desktopBluetoothProfilePort = prefix + selectedDevice.Id;
                _desktopBluetoothIdentifier.Text = selectedDevice.Id;
                SetDesktopBluetoothProfile(recognized);
            }
            _desktopBluetoothDevices.ItemsSource = _desktopBluetoothSnapshot.Select(device =>
                string.Format(CultureInfo.CurrentCulture, SettingsResources.BleDeviceDisplay, device.Name, device.Address ?? device.Id)).ToArray();
            _desktopBluetoothDevices.SelectedIndex = Array.FindIndex(_desktopBluetoothSnapshot,
                device => string.Equals(prefix + device.Id, selected, StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            _updatingDesktopBluetoothDevices = false;
        }
        UpdateUartAvailability();
    }
    /// <summary>Cancels an outstanding refresh and permits an explicit later refresh.</summary>
    private void CancelDesktopBluetoothDiscovery()
    {
        CancelDesktopBluetoothScan();
        CancellationTokenSource? previous = _desktopBluetoothDiscovery;
        _desktopBluetoothDiscovery = null;
        previous?.Cancel();
        _desktopBluetoothRefresh.IsEnabled = true;
    }

    /// <summary>Stops scanning and prevents callbacks already queued on the dispatcher from updating the form.</summary>
    private void CancelDesktopBluetoothScan()
    {
        CancellationTokenSource? previous = _desktopBluetoothScanCancellation;
        _desktopBluetoothScanCancellation = null;
        previous?.Cancel();
        _desktopBluetoothRefresh.Text = SettingsResources.FindDevices;
        _desktopBluetoothRefresh.IsEnabled = true;
        if (previous != null)
        {
            _desktopBluetoothFeedback.Text = _desktopBluetoothScanCount == 0 ? SettingsResources.BleScanNoDevices
                : string.Format(CultureInfo.CurrentCulture, SettingsResources.BleScanComplete, _desktopBluetoothScanCount);
        }
    }

    /// <summary>Scans nearby unpaired BLE devices for the default bounded duration and streams selectable results.</summary>
    /// <returns>A task completing after native discovery and localized feedback finish.</returns>
    private async Task ScanDesktopBluetoothDevicesAsync()
    {
        CancelDesktopBluetoothDiscovery();
        using var source = new CancellationTokenSource();
        _desktopBluetoothScanCancellation = source;
        _desktopBluetoothScanCount = 0;
        var found = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        _desktopBluetoothRefresh.Text = SettingsResources.StopBleScan;
        _desktopBluetoothFeedback.Text = SettingsResources.BleScanning;
        ShowDesktopBluetoothDevices(Array.Empty<BluetoothSerialDevice>());
        try
        {
            BluetoothSerialDevice[] snapshot = await BluetoothLeDiscovery.ScanAsync(deviceFound: device => Dispatcher.Dispatch(() =>
            {
                if (!ReferenceEquals(_desktopBluetoothScanCancellation, source) || source.IsCancellationRequested || !device.SupportsSerialPort)
                {
                    return;
                }
                found.Add(device.Id);
                _desktopBluetoothScanCount = found.Count;
                int index = Array.FindIndex(_desktopBluetoothSnapshot, item => string.Equals(item.Id, device.Id, StringComparison.OrdinalIgnoreCase));
                if (index < 0)
                {
                    _desktopBluetoothSnapshot = _desktopBluetoothSnapshot.Append(device).ToArray();
                }
                else
                {
                    _desktopBluetoothSnapshot[index] = device;
                }
                ShowDesktopBluetoothDevices(_desktopBluetoothSnapshot);
            }), cancellationToken: source.Token);
            if (ReferenceEquals(_desktopBluetoothScanCancellation, source))
            {
                _desktopBluetoothScanCount = snapshot.Count(device => device.SupportsSerialPort);
                ShowDesktopBluetoothDevices(snapshot);
                _desktopBluetoothFeedback.Text = _desktopBluetoothScanCount == 0 ? SettingsResources.BleScanNoDevices
                    : string.Format(CultureInfo.CurrentCulture, SettingsResources.BleScanComplete, _desktopBluetoothScanCount);
            }
        }
        catch (OperationCanceledException) when (source.IsCancellationRequested)
        {
        }
        catch (Exception)
        {
            if (ReferenceEquals(_desktopBluetoothScanCancellation, source))
            {
                _desktopBluetoothFeedback.Text = SettingsResources.BleScanFailed;
            }
        }
        finally
        {
            if (ReferenceEquals(_desktopBluetoothScanCancellation, source))
            {
                _desktopBluetoothScanCancellation = null;
                _desktopBluetoothRefresh.Text = SettingsResources.FindDevices;
                _desktopBluetoothRefresh.IsEnabled = true;
            }
        }
    }

    /// <summary>Enumerates only cached devices and ignores responses from a detached or superseded form.</summary>
    /// <returns>A task completing when enumeration and its localized feedback are applied.</returns>
    private async Task RefreshDesktopBluetoothDevicesAsync()
    {
        CancelDesktopBluetoothDiscovery();
        using var source = new CancellationTokenSource();
        _desktopBluetoothDiscovery = source;
        _desktopBluetoothRefresh.IsEnabled = false;
        try
        {
            BluetoothSerialTransport transport = _desktopBluetoothProtocol.SelectedIndex == 1 ? BluetoothSerialTransport.LowEnergy : BluetoothSerialTransport.Classic;
            BluetoothSerialDevice[] devices = await BluetoothSerialTransports.GetDevicesAsync(transport, source.Token);
            if (ReferenceEquals(_desktopBluetoothDiscovery, source) && !source.IsCancellationRequested)
            {
                ShowDesktopBluetoothDevices(devices);
                _desktopBluetoothFeedback.Text = !devices.Any(device => device.SupportsSerialPort) ? SettingsResources.BluetoothNoDevices : string.Empty;
            }
        }
        catch (OperationCanceledException) when (source.IsCancellationRequested)
        {
        }
        catch (Exception)
        {
            if (ReferenceEquals(_desktopBluetoothDiscovery, source))
            {
                _desktopBluetoothFeedback.Text = SettingsResources.BluetoothRefreshFailed;
            }
        }
        finally
        {
            if (ReferenceEquals(_desktopBluetoothDiscovery, source))
            {
                _desktopBluetoothDiscovery = null;
                _desktopBluetoothRefresh.IsEnabled = true;
            }
        }
    }

    /// <summary>Adds an explicitly selected Bluetooth identifier to the draft port snapshot.</summary>
    /// <param name="identifier">The complete bluetooth: or ble: port identifier.</param>
    private void SelectDesktopBluetoothPort(string identifier)
    {
        BindDesktopBluetoothProfile(identifier);
        _selectingDesktopBluetoothPort = true;
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
            _selectingDesktopBluetoothPort = false;
        }
        UpdateUartAvailability();
    }

    /// <summary>Associates draft GATT fields with one selected device instead of carrying them to another device.</summary>
    /// <param name="identifier">The complete selected port identifier; transient empty selections are ignored.</param>
    private void BindDesktopBluetoothProfile(string? identifier)
    {
        if (!_desktopBluetoothFieldsReady || _selectingDesktopBluetoothPort || identifier is null
            || string.Equals(_desktopBluetoothProfilePort, identifier, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }
        _desktopBluetoothProfilePort = identifier;
        _desktopBluetoothIdentifier.Text = identifier[(identifier.IndexOf(':') + 1)..];
        if (identifier.StartsWith("ble:", StringComparison.OrdinalIgnoreCase) || identifier.StartsWith("bluetooth:", StringComparison.OrdinalIgnoreCase))
        {
            string prefix = identifier.StartsWith("ble:", StringComparison.OrdinalIgnoreCase) ? "ble:" : "bluetooth:";
            BluetoothLeProfile? discoveredProfile = _desktopBluetoothSnapshot.FirstOrDefault(device =>
                string.Equals(prefix + device.Id, identifier, StringComparison.OrdinalIgnoreCase))?.Profile;
            SetDesktopBluetoothProfile(discoveredProfile ?? GetSavedBluetoothProfile(identifier));
        }
    }

    /// <summary>Copies a known or saved GATT profile into draft fields without changing media settings.</summary>
    /// <param name="profile">The supported profile whose canonical catalogue values are displayed, or null to clear information.</param>
    private void SetDesktopBluetoothProfile(BluetoothLeProfile? profile)
    {
        _desktopBluetoothProfile = _desktopBluetoothProtocol.SelectedIndex == 1 ? BluetoothLeProfile.FromSavedProfile(profile) : null;
        profile = _desktopBluetoothProfile;
        _desktopBluetoothService.Text = profile?.Service.ToString() ?? string.Empty;
        _desktopBluetoothRead.Text = profile?.ReadCharacteristic.ToString() ?? string.Empty;
        _desktopBluetoothWrite.Text = profile?.WriteCharacteristic.ToString() ?? string.Empty;
        _desktopBluetoothConfiguration.Text = profile?.ConfigurationCharacteristic?.ToString() ?? string.Empty;
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
