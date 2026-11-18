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

#if ANDROID || IOS || MACCATALYST
using System.Globalization;
using Microsoft.Maui.Controls;
using Microsoft.Maui.ApplicationModel;
using SettingsResources = Gurux.Serial.Maui.Properties.Resources;

namespace Gurux.Serial.Maui;

/// <summary>Provides explicit, bounded mobile BLE discovery and staged GATT profile selection.</summary>
public sealed partial class SerialSettingsForm
{
#if ANDROID
    /// <summary>Selects paired RFCOMM enumeration or active BLE discovery on Android.</summary>
    private readonly Picker _mobileBluetoothProtocol = new();
#endif
    /// <summary>Lists paired devices or starts and cancels the current explicit BLE scan.</summary>
    private readonly Button _mobileBleScanButton = new() { AutomationId = "ScanBleDevices" };
    /// <summary>Shows advertised names together with stable device identifiers.</summary>
    private readonly Picker _mobileBleDevices = new() { AutomationId = "BleDevices" };
    /// <summary>Displays the selected native identifier without accepting manual input.</summary>
    private readonly Entry _mobileBluetoothIdentifier = new() { IsReadOnly = true };
    /// <summary>Reports scan progress and failures.</summary>
    private readonly Label _mobileBleFeedback = new() { LineBreakMode = LineBreakMode.WordWrap };
    /// <summary>Displays the selected serial service UUID without allowing edits.</summary>
    private readonly Entry _mobileBleService = new() { IsReadOnly = true };
    /// <summary>Displays the canonical notification characteristic UUID without accepting input.</summary>
    private readonly Entry _mobileBleRead = new() { IsReadOnly = true };
    /// <summary>Displays the canonical output characteristic UUID without accepting input.</summary>
    private readonly Entry _mobileBleWrite = new() { IsReadOnly = true };
    /// <summary>Displays the canonical optional configuration characteristic UUID without accepting input.</summary>
    private readonly Entry _mobileBleConfiguration = new() { IsReadOnly = true };
    /// <summary>Stores canonical catalogue profile data independently of presentation fields.</summary>
    private BluetoothLeProfile? _mobileBluetoothProfile;
    /// <summary>Contains collapsed read-only identifier and UUID information for the selected device.</summary>
    private readonly VerticalStackLayout _mobileBleProfileFields = new() { Spacing = 6, IsVisible = false, Padding = new(16, 0, 0, 0), AutomationId = "BluetoothInformationFields" };
    /// <summary>Stores the immutable identities displayed in the picker.</summary>
    private BluetoothSerialDevice[] _mobileBleSnapshot = [];
    /// <summary>Owns cancellation of the currently active native scan.</summary>
    private CancellationTokenSource? _mobileBleScan;
    /// <summary>Counts supported identities observed by discovery independently of retained selections.</summary>
    private int _mobileBleScanCount;
    /// <summary>Associates the displayed GATT fields with one selected device.</summary>
    private string? _mobileBleProfilePort;
    /// <summary>Prevents profile association during transient picker refreshes.</summary>
    private bool _selectingMobileBle;
    /// <summary>Prevents refreshed picker choices from acting as fresh user selections.</summary>
    private bool _updatingMobileBleDevices;
    /// <summary>Marks completed mobile BLE field construction.</summary>
    private bool _mobileBleReady;

    /// <summary>Adds mobile BLE discovery without starting a scan or requesting permission during construction.</summary>
    private void AddMobileBleControls()
    {
        var panel = new VerticalStackLayout { Spacing = 8 };
#if ANDROID
        _mobileBluetoothProtocol.ItemsSource = new[] { SettingsResources.BluetoothClassic, SettingsResources.BluetoothLowEnergy };
        _mobileBluetoothProtocol.SelectedIndex = _settings.PortName?.StartsWith("ble:", StringComparison.OrdinalIgnoreCase) == true ? 1 : 0;
        panel.Children.Add(new Label { Text = SettingsResources.BluetoothTransport });
        panel.Children.Add(_mobileBluetoothProtocol);
        _mobileBluetoothProtocol.SelectedIndexChanged += (_, _) =>
        {
            CancelMobileBleScan();
            ShowMobileBleDevices(Array.Empty<BluetoothSerialDevice>());
        };
#endif
        _mobileBleScanButton.Text = SettingsResources.FindDevices;
        panel.Children.Add(_mobileBleScanButton);
        panel.Children.Add(_mobileBleFeedback);
        AddMobileBleField(SettingsResources.BluetoothIdentifier, _mobileBluetoothIdentifier);
        AddMobileBleField(SettingsResources.BluetoothService, _mobileBleService);
        AddMobileBleField(SettingsResources.BluetoothRead, _mobileBleRead);
        AddMobileBleField(SettingsResources.BluetoothWrite, _mobileBleWrite);
        AddMobileBleField(SettingsResources.BluetoothConfiguration, _mobileBleConfiguration);
        AddGattServiceControls(_mobileBleProfileFields);
        panel.Children.Add(new Label { Text = SettingsResources.BluetoothDevices });
        panel.Children.Add(_mobileBleDevices);
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
            _mobileBleProfileFields.IsVisible = !_mobileBleProfileFields.IsVisible;
            informationToggle.Text = _mobileBleProfileFields.IsVisible
                ? SettingsResources.BluetoothDeviceInformationExpanded : SettingsResources.BluetoothDeviceInformation;
            SemanticProperties.SetDescription(informationToggle, _mobileBleProfileFields.IsVisible
                ? SettingsResources.BluetoothInformationExpanded : SettingsResources.BluetoothInformationCollapsed);
        };
        panel.Children.Add(informationToggle);
        panel.Children.Add(_mobileBleProfileFields);
        AddBluetoothControl(panel);
        _mobileBleReady = true;
        _mobileBleScanButton.Clicked += async (_, _) =>
        {
            if (_mobileBleScan is not null) { CancelMobileBleScan(); }
#if ANDROID
            else if (_mobileBluetoothProtocol.SelectedIndex == 0) { await RefreshMobileClassicDevicesAsync(); }
#endif
            else { await ScanMobileBleAsync(); }
        };
        _mobileBleDevices.SelectedIndexChanged += (_, _) =>
        {
            int index = _mobileBleDevices.SelectedIndex;
            if (_updatingMobileBleDevices || index < 0 || index >= _mobileBleSnapshot.Length) { return; }
            BluetoothSerialDevice device = _mobileBleSnapshot[index];
            SelectMobileBluetoothPort((device.Transport == BluetoothSerialTransport.LowEnergy ? "ble:" : "bluetooth:") + device.Id, device.Name);
        };
        HandlerChanging += (_, args) => { if (args.NewHandler is null) { CancelMobileBleScan(); } };

        if (_settings.PortName is string saved && (saved.StartsWith("ble:", StringComparison.OrdinalIgnoreCase)
            || saved.StartsWith("bluetooth:", StringComparison.OrdinalIgnoreCase)))
        {
            RefreshPorts(_portNames.Append(saved).Distinct(StringComparer.Ordinal).ToArray());
            _ports.SelectedItem = saved;
            _mobileBluetoothIdentifier.Text = saved[(saved.IndexOf(':') + 1)..];
        }
        BindMobileBleProfile(_ports.SelectedItem as string);
        ShowMobileBleDevices(BluetoothLeDiscovery.GetKnownDevices());
    }

#if ANDROID
    /// <summary>Requests paired-device access and lists RFCOMM devices advertising cached Serial Port service UUIDs without requiring a GATT catalogue profile.</summary>
    /// <returns>A task completing after permission and paired-device enumeration are applied.</returns>
    private async Task RefreshMobileClassicDevicesAsync()
    {
        using var source = new CancellationTokenSource();
        _mobileBleScan = source;
        _mobileBleScanCount = 0;
        _mobileBleScanButton.Text = SettingsResources.StopBleScan;
        try
        {
            var permission = OperatingSystem.IsAndroidVersionAtLeast(31)
                ? await Permissions.RequestAsync<BluetoothConnectionPermission>() : PermissionStatus.Granted;
            if (!ReferenceEquals(_mobileBleScan, source) || source.IsCancellationRequested) { return; }
            if (permission != PermissionStatus.Granted)
            {
                _mobileBleFeedback.Text = SettingsResources.BluetoothPermissionDenied;
                return;
            }
            var manager = Android.App.Application.Context.GetSystemService(Android.Content.Context.BluetoothService) as Android.Bluetooth.BluetoothManager;
            var paired = manager?.Adapter?.BondedDevices;
            BluetoothSerialDevice[] devices = paired?.Select(device => new BluetoothSerialDevice(device.Address!,
                string.IsNullOrWhiteSpace(device.Name) ? device.Address! : device.Name,
                BluetoothSerialTransport.Classic, device.Address!, device.GetUuids()?.Select(uuid => Guid.Parse(uuid.ToString()!)))).ToArray() ?? [];
            ShowMobileBleDevices(devices);
            _mobileBleFeedback.Text = !devices.Any(device => device.SupportsSerialPort) ? SettingsResources.BluetoothNoDevices : string.Empty;
        }
        catch (Exception)
        {
            if (ReferenceEquals(_mobileBleScan, source)) { _mobileBleFeedback.Text = SettingsResources.BluetoothRefreshFailed; }
        }
        finally
        {
            if (ReferenceEquals(_mobileBleScan, source))
            {
                _mobileBleScan = null;
                _mobileBleScanButton.Text = SettingsResources.FindDevices;
            }
        }
    }
#endif
    /// <summary>Stages a listed mobile Bluetooth device and binds its canonical catalogue profile.</summary>
    /// <param name="port">The complete ble: or bluetooth: device identifier.</param>
    /// <param name="deviceName">The current advertised name whose catalogue row takes precedence over saved endpoint fallback.</param>
    private void SelectMobileBluetoothPort(string port, string? deviceName = null)
    {
        CancelMobileBleScan();
        _mobileBluetoothIdentifier.Text = port[(port.IndexOf(':') + 1)..];
        {
            _mobileBleProfilePort = port;
            string prefix = port.StartsWith("ble:", StringComparison.OrdinalIgnoreCase) ? "ble:" : "bluetooth:";
            deviceName ??= _mobileBleSnapshot.FirstOrDefault(device =>
                string.Equals(prefix + device.Id, port, StringComparison.OrdinalIgnoreCase))?.Name;
            PopulateMobileBleProfile(prefix == "ble:" ? BluetoothLeProfile.FromDeviceName(deviceName) ?? GetSavedBluetoothProfile(port) : null);
        }
        _selectingMobileBle = true;
        try
        {
            RefreshPorts(_portNames.Append(port).Distinct(StringComparer.Ordinal).ToArray());
            _ports.SelectedItem = port;
        }
        finally
        {
            _selectingMobileBle = false;
        }
        UpdateUartAvailability();
    }
    /// <summary>Adds a localized label and input to the mobile GATT profile group.</summary>
    /// <param name="caption">The localized field caption.</param>
    /// <param name="entry">The staged profile input.</param>
    private void AddMobileBleField(string caption, Entry entry)
    {
        _mobileBleProfileFields.Children.Add(new Label { Text = caption });
        _mobileBleProfileFields.Children.Add(entry);
    }

    /// <summary>Stops the current scan and invalidates queued notifications before native cleanup completes.</summary>
    private void CancelMobileBleScan()
    {
        CancellationTokenSource? source = _mobileBleScan;
        _mobileBleScan = null;
        source?.Cancel();
        _mobileBleScanButton.Text = SettingsResources.FindDevices;
        if (source is not null)
        {
            _mobileBleFeedback.Text = _mobileBleScanCount == 0 ? SettingsResources.BleScanNoDevices
                : string.Format(CultureInfo.CurrentCulture, SettingsResources.BleScanComplete, _mobileBleScanCount);
        }
    }

    /// <summary>Requests only the permissions needed for an explicitly started, time-limited scan.</summary>
    /// <returns>A task completing after discovery or cancellation and native cleanup.</returns>
    private async Task ScanMobileBleAsync()
    {
        using var source = new CancellationTokenSource();
        _mobileBleScan = source;
        _mobileBleScanCount = 0;
        _mobileBleScanButton.Text = SettingsResources.StopBleScan;
        _mobileBleFeedback.Text = SettingsResources.BleScanning;
        ShowMobileBleDevices(Array.Empty<BluetoothSerialDevice>());
        var found = new Dictionary<string, BluetoothSerialDevice>(StringComparer.OrdinalIgnoreCase);
        try
        {
#if ANDROID
            var permission = await Permissions.RequestAsync<BleDiscoveryPermission>();
            if (!ReferenceEquals(_mobileBleScan, source) || Handler is null) { return; }
            if (permission != PermissionStatus.Granted)
            {
                _mobileBleFeedback.Text = SettingsResources.BleScanPermissionDenied;
                return;
            }
#endif
            BluetoothSerialDevice[] devices = await BluetoothLeDiscovery.ScanAsync(TimeSpan.FromSeconds(10), device =>
            {
                Dispatcher.Dispatch(() =>
                {
                    if (!ReferenceEquals(_mobileBleScan, source) || source.IsCancellationRequested || Handler is null || !device.SupportsSerialPort) { return; }
                    found[device.Id] = device;
                    _mobileBleScanCount = found.Count;
                    ShowMobileBleDevices(found.Values);
                });
            }, source.Token);
            if (ReferenceEquals(_mobileBleScan, source) && Handler is not null)
            {
                _mobileBleScanCount = devices.Count(device => device.SupportsSerialPort);
                ShowMobileBleDevices(devices);
                _mobileBleFeedback.Text = !devices.Any(device => device.SupportsSerialPort) ? SettingsResources.BleScanNoDevices
                    : string.Format(CultureInfo.CurrentCulture, SettingsResources.BleScanComplete, devices.Count(device => device.SupportsSerialPort));
            }
        }
        catch (OperationCanceledException) when (source.IsCancellationRequested) { }
        catch (Exception)
        {
            if (ReferenceEquals(_mobileBleScan, source) && Handler is not null) { _mobileBleFeedback.Text = SettingsResources.BleScanFailed; }
        }
        finally
        {
            if (ReferenceEquals(_mobileBleScan, source)) { _mobileBleScan = null; _mobileBleScanButton.Text = SettingsResources.FindDevices; }
        }
    }

    /// <summary>Lists serial-capable results while retaining both the original saved device and current selection without treating refreshes as user selections.</summary>
    /// <param name="devices">The bounded current discovery snapshot.</param>
    private void ShowMobileBleDevices(IEnumerable<BluetoothSerialDevice> devices)
    {
        string? selected = _ports.SelectedItem as string ?? _settings.PortName;
        string prefix = "ble:";
        BluetoothSerialTransport transport = BluetoothSerialTransport.LowEnergy;
#if ANDROID
        if (_mobileBluetoothProtocol.SelectedIndex == 0)
        {
            prefix = "bluetooth:";
            transport = BluetoothSerialTransport.Classic;
        }
#endif
        var available = devices.Where(device => device.Transport == transport && device.SupportsSerialPort).ToList();
        foreach (string? retainedPort in new[] { _savedBluetoothPort, selected })
        {
            if (retainedPort?.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) == true
                && (prefix == "bluetooth:" || GetSavedBluetoothProfile(retainedPort) is not null || _mobileBleSnapshot.Any(device =>
                    string.Equals(prefix + device.Id, retainedPort, StringComparison.OrdinalIgnoreCase) && device.SupportsSerialPort))
                && !available.Any(device => string.Equals(prefix + device.Id, retainedPort, StringComparison.OrdinalIgnoreCase)))
            {
                string id = retainedPort[prefix.Length..];
                BluetoothSerialDevice? previous = _mobileBleSnapshot.FirstOrDefault(device =>
                    string.Equals(prefix + device.Id, retainedPort, StringComparison.OrdinalIgnoreCase));
                available.Add(previous ?? new BluetoothSerialDevice(id, id, transport, id));
            }
        }
        _updatingMobileBleDevices = true;
        try
        {
            _mobileBleSnapshot = available.OrderBy(device => device.Name, StringComparer.OrdinalIgnoreCase).ToArray();
            BluetoothSerialDevice? selectedDevice = _mobileBleSnapshot.FirstOrDefault(device =>
                string.Equals(prefix + device.Id, selected, StringComparison.OrdinalIgnoreCase));
            if (selectedDevice?.Profile is BluetoothLeProfile recognized)
            {
                _mobileBleProfilePort = prefix + selectedDevice.Id;
                _mobileBluetoothIdentifier.Text = selectedDevice.Id;
                PopulateMobileBleProfile(recognized);
            }
            _mobileBleDevices.ItemsSource = _mobileBleSnapshot.Select(device =>
                string.Format(CultureInfo.CurrentCulture, SettingsResources.BleDeviceDisplay, device.Name, device.Address ?? device.Id)).ToArray();
            _mobileBleDevices.SelectedIndex = Array.FindIndex(_mobileBleSnapshot,
                device => string.Equals(prefix + device.Id, selected, StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            _updatingMobileBleDevices = false;
        }
        UpdateUartAvailability();
    }

    /// <summary>Associates the displayed profile with the selected port, clearing unrelated device metadata.</summary>
    /// <param name="port">The selected serial or BLE port identifier.</param>
    private void BindMobileBleProfile(string? port)
    {
        if (!_mobileBleReady || _selectingMobileBle) { return; }
        if (port is null || string.Equals(port, _mobileBleProfilePort, StringComparison.OrdinalIgnoreCase)) { return; }
        _mobileBleProfilePort = port;
        _mobileBluetoothIdentifier.Text = port[(port.IndexOf(':') + 1)..];
        string prefix = port.StartsWith("ble:", StringComparison.OrdinalIgnoreCase) ? "ble:" : "bluetooth:";
        BluetoothLeProfile? profile = _mobileBleSnapshot.FirstOrDefault(device =>
            string.Equals(prefix + device.Id, port, StringComparison.OrdinalIgnoreCase))?.Profile;
        profile ??= GetSavedBluetoothProfile(port);
        if (profile is null)
        {
            var device = BluetoothLeDiscovery.GetKnownDevices().FirstOrDefault(item => string.Equals("ble:" + item.Id, port, StringComparison.OrdinalIgnoreCase));
            profile = device?.Profile;
        }
        PopulateMobileBleProfile(profile);
    }

    /// <summary>Populates saved or recognised GATT fields without opening a connection.</summary>
    /// <param name="profile">The profile whose canonical catalogue values are displayed, or null to clear information.</param>
    private void PopulateMobileBleProfile(BluetoothLeProfile? profile)
    {
        _mobileBluetoothProfile = BluetoothLeProfile.FromSavedProfile(profile);
        profile = _mobileBluetoothProfile;
        _mobileBleService.Text = profile?.Service.ToString() ?? string.Empty;
        _mobileBleRead.Text = profile?.ReadCharacteristic.ToString() ?? string.Empty;
        _mobileBleWrite.Text = profile?.WriteCharacteristic.ToString() ?? string.Empty;
        _mobileBleConfiguration.Text = profile?.ConfigurationCharacteristic?.ToString() ?? string.Empty;
    }

    /// <summary>Applies only the selected canonical catalogue profile; read-only presentation text is never parsed.</summary>
    /// <exception cref="FormatException">The selected Bluetooth identity has no supported catalogue profile.</exception>
    private void ReadMobileBleSettings()
    {
        string? port = _ports.SelectedItem as string;
        if (!_mobileBleReady || port is null || (!port.StartsWith("ble:", StringComparison.OrdinalIgnoreCase)
            && !port.StartsWith("bluetooth:", StringComparison.OrdinalIgnoreCase))) { return; }
        BluetoothLeProfile? profile = GetSelectedBluetoothProfile(port);
        if (port.StartsWith("bluetooth:", StringComparison.OrdinalIgnoreCase))
        {
            _settings.BluetoothProfile = null;
            _settings.BluetoothChannel = 0;
            return;
        }
        if (profile is null)
        {
            throw new FormatException(string.Format(CultureInfo.CurrentCulture,
                SettingsResources.BluetoothProfileMissing, port[(port.IndexOf(':') + 1)..]));
        }
        _settings.BluetoothProfile = profile;
    }
#if ANDROID
    /// <summary>Requests Android's nearby-device or legacy location permission only after an explicit BLE search.</summary>
    private sealed class BleDiscoveryPermission : Permissions.BasePlatformPermission
    {
        /// <inheritdoc/>
        public override (string androidPermission, bool isRuntime)[] RequiredPermissions => OperatingSystem.IsAndroidVersionAtLeast(31)
            ? [(Android.Manifest.Permission.BluetoothScan, true), (Android.Manifest.Permission.BluetoothConnect, true)]
            : [(Android.Manifest.Permission.AccessFineLocation, true)];
    }
#endif
}
#endif
