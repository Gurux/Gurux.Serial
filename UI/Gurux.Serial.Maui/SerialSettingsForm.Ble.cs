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
    private readonly Picker mobileBluetoothProtocol = new();
#endif
    /// <summary>Lists paired devices or starts and cancels the current explicit BLE scan.</summary>
    private readonly Button mobileBleScanButton = new() { AutomationId = "ScanBleDevices" };
    /// <summary>Shows advertised names together with stable device identifiers.</summary>
    private readonly Picker mobileBleDevices = new() { AutomationId = "BleDevices" };
    /// <summary>Displays the selected native identifier without accepting manual input.</summary>
    private readonly Entry mobileBluetoothIdentifier = new() { IsReadOnly = true };
    /// <summary>Reports scan progress and failures.</summary>
    private readonly Label mobileBleFeedback = new() { LineBreakMode = LineBreakMode.WordWrap };
    /// <summary>Displays the selected serial service UUID without allowing edits.</summary>
    private readonly Entry mobileBleService = new() { IsReadOnly = true };
    /// <summary>Displays the canonical notification characteristic UUID without accepting input.</summary>
    private readonly Entry mobileBleRead = new() { IsReadOnly = true };
    /// <summary>Displays the canonical output characteristic UUID without accepting input.</summary>
    private readonly Entry mobileBleWrite = new() { IsReadOnly = true };
    /// <summary>Displays the canonical optional configuration characteristic UUID without accepting input.</summary>
    private readonly Entry mobileBleConfiguration = new() { IsReadOnly = true };
    /// <summary>Stores canonical catalogue profile data independently of presentation fields.</summary>
    private BluetoothLeProfile? mobileBluetoothProfile;
    /// <summary>Contains collapsed read-only identifier and UUID information for the selected device.</summary>
    private readonly VerticalStackLayout mobileBleProfileFields = new() { Spacing = 6, IsVisible = false, Padding = new(16, 0, 0, 0), AutomationId = "BluetoothInformationFields" };
    /// <summary>Stores the immutable identities displayed in the picker.</summary>
    private BluetoothSerialDevice[] mobileBleSnapshot = [];
    /// <summary>Owns cancellation of the currently active native scan.</summary>
    private CancellationTokenSource? mobileBleScan;
    /// <summary>Counts supported identities observed by discovery independently of retained selections.</summary>
    private int mobileBleScanCount;
    /// <summary>Associates the displayed GATT fields with one selected device.</summary>
    private string? mobileBleProfilePort;
    /// <summary>Prevents profile association during transient picker refreshes.</summary>
    private bool selectingMobileBle;
    /// <summary>Prevents refreshed picker choices from acting as fresh user selections.</summary>
    private bool updatingMobileBleDevices;
    /// <summary>Marks completed mobile BLE field construction.</summary>
    private bool mobileBleReady;

    /// <summary>Adds mobile BLE discovery without starting a scan or requesting permission during construction.</summary>
    private void AddMobileBleControls()
    {
        var panel = new VerticalStackLayout { Spacing = 8 };
#if ANDROID
        mobileBluetoothProtocol.ItemsSource = new[] { SettingsResources.BluetoothClassic, SettingsResources.BluetoothLowEnergy };
        mobileBluetoothProtocol.SelectedIndex = _settings.PortName?.StartsWith("ble:", StringComparison.OrdinalIgnoreCase) == true ? 1 : 0;
        panel.Children.Add(new Label { Text = SettingsResources.BluetoothTransport });
        panel.Children.Add(mobileBluetoothProtocol);
        mobileBluetoothProtocol.SelectedIndexChanged += (_, _) =>
        {
            CancelMobileBleScan();
            ShowMobileBleDevices(Array.Empty<BluetoothSerialDevice>());
        };
#endif
        mobileBleScanButton.Text = SettingsResources.FindDevices;
        panel.Children.Add(mobileBleScanButton);
        panel.Children.Add(mobileBleFeedback);
        AddMobileBleField(SettingsResources.BluetoothIdentifier, mobileBluetoothIdentifier);
        AddMobileBleField(SettingsResources.BluetoothService, mobileBleService);
        AddMobileBleField(SettingsResources.BluetoothRead, mobileBleRead);
        AddMobileBleField(SettingsResources.BluetoothWrite, mobileBleWrite);
        AddMobileBleField(SettingsResources.BluetoothConfiguration, mobileBleConfiguration);
        AddGattServiceControls(mobileBleProfileFields);
        panel.Children.Add(new Label { Text = SettingsResources.BluetoothDevices });
        panel.Children.Add(mobileBleDevices);
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
            mobileBleProfileFields.IsVisible = !mobileBleProfileFields.IsVisible;
            informationToggle.Text = mobileBleProfileFields.IsVisible
                ? SettingsResources.BluetoothDeviceInformationExpanded : SettingsResources.BluetoothDeviceInformation;
            SemanticProperties.SetDescription(informationToggle, mobileBleProfileFields.IsVisible
                ? SettingsResources.BluetoothInformationExpanded : SettingsResources.BluetoothInformationCollapsed);
        };
        panel.Children.Add(informationToggle);
        panel.Children.Add(mobileBleProfileFields);
        AddBluetoothControl(panel);
        mobileBleReady = true;
        mobileBleScanButton.Clicked += async (_, _) =>
        {
            if (mobileBleScan is not null) { CancelMobileBleScan(); }
#if ANDROID
            else if (mobileBluetoothProtocol.SelectedIndex == 0) { await RefreshMobileClassicDevicesAsync(); }
#endif
            else { await ScanMobileBleAsync(); }
        };
        mobileBleDevices.SelectedIndexChanged += (_, _) =>
        {
            int index = mobileBleDevices.SelectedIndex;
            if (updatingMobileBleDevices || index < 0 || index >= mobileBleSnapshot.Length) { return; }
            BluetoothSerialDevice device = mobileBleSnapshot[index];
            SelectMobileBluetoothPort((device.Transport == BluetoothSerialTransport.LowEnergy ? "ble:" : "bluetooth:") + device.Id, device.Name);
        };
        HandlerChanging += (_, args) => { if (args.NewHandler is null) { CancelMobileBleScan(); } };

        if (_settings.PortName is string saved && (saved.StartsWith("ble:", StringComparison.OrdinalIgnoreCase)
            || saved.StartsWith("bluetooth:", StringComparison.OrdinalIgnoreCase)))
        {
            RefreshPorts(_portNames.Append(saved).Distinct(StringComparer.Ordinal).ToArray());
            _ports.SelectedItem = saved;
            mobileBluetoothIdentifier.Text = saved[(saved.IndexOf(':') + 1)..];
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
        mobileBleScan = source;
        mobileBleScanCount = 0;
        mobileBleScanButton.Text = SettingsResources.StopBleScan;
        try
        {
            var permission = OperatingSystem.IsAndroidVersionAtLeast(31)
                ? await Permissions.RequestAsync<BluetoothConnectionPermission>() : PermissionStatus.Granted;
            if (!ReferenceEquals(mobileBleScan, source) || source.IsCancellationRequested) { return; }
            if (permission != PermissionStatus.Granted)
            {
                mobileBleFeedback.Text = SettingsResources.BluetoothPermissionDenied;
                return;
            }
            var manager = Android.App.Application.Context.GetSystemService(Android.Content.Context.BluetoothService) as Android.Bluetooth.BluetoothManager;
            var paired = manager?.Adapter?.BondedDevices;
            BluetoothSerialDevice[] devices = paired?.Select(device => new BluetoothSerialDevice(device.Address!,
                string.IsNullOrWhiteSpace(device.Name) ? device.Address! : device.Name,
                BluetoothSerialTransport.Classic, device.Address!, device.GetUuids()?.Select(uuid => Guid.Parse(uuid.ToString()!)))).ToArray() ?? [];
            ShowMobileBleDevices(devices);
            mobileBleFeedback.Text = !devices.Any(device => device.SupportsSerialPort) ? SettingsResources.BluetoothNoDevices : string.Empty;
        }
        catch (Exception)
        {
            if (ReferenceEquals(mobileBleScan, source)) { mobileBleFeedback.Text = SettingsResources.BluetoothRefreshFailed; }
        }
        finally
        {
            if (ReferenceEquals(mobileBleScan, source))
            {
                mobileBleScan = null;
                mobileBleScanButton.Text = SettingsResources.FindDevices;
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
        mobileBluetoothIdentifier.Text = port[(port.IndexOf(':') + 1)..];
        {
            mobileBleProfilePort = port;
            string prefix = port.StartsWith("ble:", StringComparison.OrdinalIgnoreCase) ? "ble:" : "bluetooth:";
            deviceName ??= mobileBleSnapshot.FirstOrDefault(device =>
                string.Equals(prefix + device.Id, port, StringComparison.OrdinalIgnoreCase))?.Name;
            PopulateMobileBleProfile(prefix == "ble:" ? BluetoothLeProfile.FromDeviceName(deviceName) ?? GetSavedBluetoothProfile(port) : null);
        }
        selectingMobileBle = true;
        try
        {
            RefreshPorts(_portNames.Append(port).Distinct(StringComparer.Ordinal).ToArray());
            _ports.SelectedItem = port;
        }
        finally
        {
            selectingMobileBle = false;
        }
        UpdateUartAvailability();
    }
    /// <summary>Adds a localized label and input to the mobile GATT profile group.</summary>
    /// <param name="caption">The localized field caption.</param>
    /// <param name="entry">The staged profile input.</param>
    private void AddMobileBleField(string caption, Entry entry)
    {
        mobileBleProfileFields.Children.Add(new Label { Text = caption });
        mobileBleProfileFields.Children.Add(entry);
    }

    /// <summary>Stops the current scan and invalidates queued notifications before native cleanup completes.</summary>
    private void CancelMobileBleScan()
    {
        CancellationTokenSource? source = mobileBleScan;
        mobileBleScan = null;
        source?.Cancel();
        mobileBleScanButton.Text = SettingsResources.FindDevices;
        if (source is not null)
        {
            mobileBleFeedback.Text = mobileBleScanCount == 0 ? SettingsResources.BleScanNoDevices
                : string.Format(CultureInfo.CurrentCulture, SettingsResources.BleScanComplete, mobileBleScanCount);
        }
    }

    /// <summary>Requests only the permissions needed for an explicitly started, time-limited scan.</summary>
    /// <returns>A task completing after discovery or cancellation and native cleanup.</returns>
    private async Task ScanMobileBleAsync()
    {
        using var source = new CancellationTokenSource();
        mobileBleScan = source;
        mobileBleScanCount = 0;
        mobileBleScanButton.Text = SettingsResources.StopBleScan;
        mobileBleFeedback.Text = SettingsResources.BleScanning;
        ShowMobileBleDevices(Array.Empty<BluetoothSerialDevice>());
        var found = new Dictionary<string, BluetoothSerialDevice>(StringComparer.OrdinalIgnoreCase);
        try
        {
#if ANDROID
            var permission = await Permissions.RequestAsync<BleDiscoveryPermission>();
            if (!ReferenceEquals(mobileBleScan, source) || Handler is null) { return; }
            if (permission != PermissionStatus.Granted)
            {
                mobileBleFeedback.Text = SettingsResources.BleScanPermissionDenied;
                return;
            }
#endif
            BluetoothSerialDevice[] devices = await BluetoothLeDiscovery.ScanAsync(TimeSpan.FromSeconds(10), device =>
            {
                Dispatcher.Dispatch(() =>
                {
                    if (!ReferenceEquals(mobileBleScan, source) || source.IsCancellationRequested || Handler is null || !device.SupportsSerialPort) { return; }
                    found[device.Id] = device;
                    mobileBleScanCount = found.Count;
                    ShowMobileBleDevices(found.Values);
                });
            }, source.Token);
            if (ReferenceEquals(mobileBleScan, source) && Handler is not null)
            {
                mobileBleScanCount = devices.Count(device => device.SupportsSerialPort);
                ShowMobileBleDevices(devices);
                mobileBleFeedback.Text = !devices.Any(device => device.SupportsSerialPort) ? SettingsResources.BleScanNoDevices
                    : string.Format(CultureInfo.CurrentCulture, SettingsResources.BleScanComplete, devices.Count(device => device.SupportsSerialPort));
            }
        }
        catch (OperationCanceledException) when (source.IsCancellationRequested) { }
        catch (Exception)
        {
            if (ReferenceEquals(mobileBleScan, source) && Handler is not null) { mobileBleFeedback.Text = SettingsResources.BleScanFailed; }
        }
        finally
        {
            if (ReferenceEquals(mobileBleScan, source)) { mobileBleScan = null; mobileBleScanButton.Text = SettingsResources.FindDevices; }
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
        if (mobileBluetoothProtocol.SelectedIndex == 0)
        {
            prefix = "bluetooth:";
            transport = BluetoothSerialTransport.Classic;
        }
#endif
        var available = devices.Where(device => device.Transport == transport && device.SupportsSerialPort).ToList();
        foreach (string? retainedPort in new[] { savedBluetoothPort, selected })
        {
            if (retainedPort?.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) == true
                && (prefix == "bluetooth:" || GetSavedBluetoothProfile(retainedPort) is not null || mobileBleSnapshot.Any(device =>
                    string.Equals(prefix + device.Id, retainedPort, StringComparison.OrdinalIgnoreCase) && device.SupportsSerialPort))
                && !available.Any(device => string.Equals(prefix + device.Id, retainedPort, StringComparison.OrdinalIgnoreCase)))
            {
                string id = retainedPort[prefix.Length..];
                BluetoothSerialDevice? previous = mobileBleSnapshot.FirstOrDefault(device =>
                    string.Equals(prefix + device.Id, retainedPort, StringComparison.OrdinalIgnoreCase));
                available.Add(previous ?? new BluetoothSerialDevice(id, id, transport, id));
            }
        }
        updatingMobileBleDevices = true;
        try
        {
            mobileBleSnapshot = available.OrderBy(device => device.Name, StringComparer.OrdinalIgnoreCase).ToArray();
            BluetoothSerialDevice? selectedDevice = mobileBleSnapshot.FirstOrDefault(device =>
                string.Equals(prefix + device.Id, selected, StringComparison.OrdinalIgnoreCase));
            if (selectedDevice?.Profile is BluetoothLeProfile recognized)
            {
                mobileBleProfilePort = prefix + selectedDevice.Id;
                mobileBluetoothIdentifier.Text = selectedDevice.Id;
                PopulateMobileBleProfile(recognized);
            }
            mobileBleDevices.ItemsSource = mobileBleSnapshot.Select(device =>
                string.Format(CultureInfo.CurrentCulture, SettingsResources.BleDeviceDisplay, device.Name, device.Address ?? device.Id)).ToArray();
            mobileBleDevices.SelectedIndex = Array.FindIndex(mobileBleSnapshot,
                device => string.Equals(prefix + device.Id, selected, StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            updatingMobileBleDevices = false;
        }
        UpdateUartAvailability();
    }

    /// <summary>Associates the displayed profile with the selected port, clearing unrelated device metadata.</summary>
    /// <param name="port">The selected serial or BLE port identifier.</param>
    private void BindMobileBleProfile(string? port)
    {
        if (!mobileBleReady || selectingMobileBle) { return; }
        if (port is null || string.Equals(port, mobileBleProfilePort, StringComparison.OrdinalIgnoreCase)) { return; }
        mobileBleProfilePort = port;
        mobileBluetoothIdentifier.Text = port[(port.IndexOf(':') + 1)..];
        string prefix = port.StartsWith("ble:", StringComparison.OrdinalIgnoreCase) ? "ble:" : "bluetooth:";
        BluetoothLeProfile? profile = mobileBleSnapshot.FirstOrDefault(device =>
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
        mobileBluetoothProfile = BluetoothLeProfile.FromSavedProfile(profile);
        profile = mobileBluetoothProfile;
        mobileBleService.Text = profile?.Service.ToString() ?? string.Empty;
        mobileBleRead.Text = profile?.ReadCharacteristic.ToString() ?? string.Empty;
        mobileBleWrite.Text = profile?.WriteCharacteristic.ToString() ?? string.Empty;
        mobileBleConfiguration.Text = profile?.ConfigurationCharacteristic?.ToString() ?? string.Empty;
    }

    /// <summary>Applies only the selected canonical catalogue profile; read-only presentation text is never parsed.</summary>
    /// <exception cref="FormatException">The selected Bluetooth identity has no supported catalogue profile.</exception>
    private void ReadMobileBleSettings()
    {
        string? port = _ports.SelectedItem as string;
        if (!mobileBleReady || port is null || (!port.StartsWith("ble:", StringComparison.OrdinalIgnoreCase)
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
