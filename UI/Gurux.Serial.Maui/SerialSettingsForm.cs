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
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using SettingsResources = Gurux.Serial.Maui.Properties.Resources;

namespace Gurux.Serial.Maui;

/// <summary>Reusable native serial settings inputs shared by Serial, Terminal and SMS editors.</summary>
/// <remarks>Pass a private settings copy. Input is applied only when Read is called.</remarks>
public sealed partial class SerialSettingsForm : ContentView, IDisposable, IAsyncDisposable
{
    /// <summary>Owns explicit native service reads independently of catalogue and advertisement metadata.</summary>
    private readonly BluetoothGattServiceQuery _gattQuery;
    /// <summary>Starts a native GATT read or cancels the outstanding request.</summary>
    private readonly Button _gattReadButton = new() { AutomationId = "ReadGattServices" };
    /// <summary>Displays complete native service UUIDs without offering editable values.</summary>
    private readonly VerticalStackLayout _gattServices = new() { Spacing = 4, AutomationId = "GattServices" };
    /// <summary>Reports native GATT progress, empty results and failures separately from device discovery.</summary>
    private readonly Label _gattFeedback = new() { LineBreakMode = LineBreakMode.WordWrap };
    /// <summary>Invalidates permission continuations when selection changes or the form closes.</summary>
    private int _gattSelectionGeneration;
    /// <summary>Prevents concurrent permission prompts before native service reading begins.</summary>
    private bool _requestingGattPermission = false;
    /// <summary>Retains a localized permission denial without altering the shared service query.</summary>
    private string? _gattPermissionError;
    /// <summary>Prevents queued discovery updates from accessing a closed form.</summary>
    private bool _disposed;
    /// <summary>Contains native controls and deferred property writers.</summary>
    private readonly SettingsFields _fields = new();
    /// <summary>Identifies controls shown only on the Bluetooth tab.</summary>
    private readonly HashSet<View> _bluetoothControls = [];
    /// <summary>Contains the persistent tab selectors above the settings fields.</summary>
    private readonly Grid _tabHeaders = new()
    {
        AutomationId = "SerialSettingsTabs",
        ColumnDefinitions = new ColumnDefinitionCollection { new(GridLength.Star), new(GridLength.Star) }
    };
    /// <summary>Selects the general port and framing settings.</summary>
    private readonly Button _generalTab = new() { AutomationId = "SerialGeneralTab", CornerRadius = 0 };
    /// <summary>Selects Bluetooth discovery and connection settings.</summary>
    private readonly Button _bluetoothTab = new() { AutomationId = "SerialBluetoothTab", CornerRadius = 0 };
    /// <summary>Tracks which group is visible when platform-specific controls are added.</summary>
    private bool _showBluetooth;
    /// <summary>The current device enumeration used to validate the picker selection.</summary>
    private string[] _portNames;
    /// <summary>Selects a port from the enumeration without accepting arbitrary text.</summary>
    private readonly Picker _ports;
    /// <summary>Displays the staged baud rate, disabled for Bluetooth profiles without configuration support.</summary>
    private readonly Picker _baudRate;
    /// <summary>Displays the staged parity, disabled for Bluetooth profiles without configuration support.</summary>
    private readonly Picker _parity;
    /// <summary>Displays the staged stop interval, disabled for Bluetooth profiles without configuration support.</summary>
    private readonly Picker _stopBits;
    /// <summary>Tracks the selected device profile without changing the private settings until Read.</summary>
    private BluetoothLeProfile? _selectedBluetoothProfile;
    /// <summary>Retains the original Bluetooth device identity across failed reads of the private draft.</summary>
    private readonly string? _savedBluetoothPort;
    /// <summary>Retains only canonical catalogue endpoints independently of later draft mutations.</summary>
    private readonly BluetoothLeProfile? _savedBluetoothProfile;
    /// <summary>The private settings receiving a valid selected port when reading.</summary>
    private readonly SerialPortSettings _settings;
#if ANDROID
    /// <summary>Identifies forms using live device enumeration rather than an explicit injected snapshot.</summary>
    private readonly bool _watchDevices;
    /// <summary>Owns USB attachment notifications while this native form has a handler.</summary>
    private AndroidUsbDeviceWatcher? _usbWatcher;
#endif
    /// <summary>Reports changes to whether a listed port is selected.</summary>
    public event EventHandler? CanSaveChanged;
    /// <summary>Gets whether the selected port belongs to this form's enumerated snapshot.</summary>
    public bool CanSave => _ports.SelectedItem is string selected && Array.IndexOf(_portNames, selected) >= 0;
    /// <summary>Creates port controls using the currently enumerated serial port names.</summary>
    /// <param name="settings">The private port settings receiving input on Read.</param>
    /// <exception cref="ArgumentNullException">The settings are null.</exception>
    public SerialSettingsForm(SerialPortSettings settings)
        : this(settings, GXSerial.GetPortNames())
    {
#if ANDROID
        _watchDevices = true;
#endif
    }
    /// <summary>Creates port controls from an explicit snapshot without enumerating hardware.</summary>
    /// <remarks>Baud rates use the common-rate list, and data bits are selected from five through eight. Bluetooth controls are available on their own tab; discovery starts only on explicit request.</remarks>
    /// <param name="settings">The private port settings receiving input on Read.</param>
    /// <param name="portNames">Available physical ports. Saved catalogue-supported Bluetooth identities may be retained; no replacement port is selected automatically.</param>
    /// <exception cref="ArgumentNullException">The settings or port name snapshot is null.</exception>
    public SerialSettingsForm(SerialPortSettings settings, string[] portNames)
        : this(settings, portNames, new BluetoothGattServiceQuery())
    {
    }
    /// <summary>Creates staged controls with an owned service query for explicit native information reads.</summary>
    /// <param name="settings">Private settings updated only on a successful read of the form.</param>
    /// <param name="portNames">The physical-port snapshot offered by the host.</param>
    /// <param name="query">The service query owned and disposed by this form.</param>
    /// <exception cref="ArgumentNullException">A required argument is null.</exception>
    internal SerialSettingsForm(SerialPortSettings settings, string[] portNames, BluetoothGattServiceQuery query)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(portNames);
        ArgumentNullException.ThrowIfNull(query);
        _gattQuery = query;
        _gattQuery.Changed += OnGattServicesChanged;
        HandlerChanging += (_, args) =>
        {
            if (args.NewHandler is null)
            {
                ++_gattSelectionGeneration;
                _gattQuery.Cancel();
            }
        };
        _settings = settings;
        _savedBluetoothPort = settings.PortName;
        _savedBluetoothProfile = BluetoothLeProfile.FromSavedProfile(settings.BluetoothProfile);
        _portNames = (string[])portNames.Clone();
        _ports = new Picker { ItemsSource = (string[])_portNames.Clone(), SelectedIndex = Array.IndexOf(_portNames, settings.PortName) };
        _ports.SelectedIndexChanged += (_, _) =>
        {
#if WINDOWS
            BindDesktopBluetoothProfile(_ports.SelectedItem as string);
#endif
#if ANDROID || IOS || MACCATALYST
            BindMobileBleProfile(_ports.SelectedItem as string);
#endif
            UpdateUartAvailability();
            CanSaveChanged?.Invoke(this, EventArgs.Empty);
        };
        _fields.Layout.Children.Add(new Label { Text = SettingsResources.PortName });
        _fields.Layout.Children.Add(_ports);
        _fields.NumberChoice(SettingsResources.BaudRate, settings.BaudRate, GXSerial.GetAvailableBaudRates(settings.PortName), value => settings.BaudRate = value);
        _baudRate = (Picker)_fields.Layout.Children[^1];
        _fields.NumberChoice(SettingsResources.DataBits, settings.DataBits, [5, 6, 7, 8], value => settings.DataBits = value);
        _fields.Choice(SettingsResources.Parity, settings.Parity, value => settings.Parity = value);
        _parity = (Picker)_fields.Layout.Children[^1];
        _fields.Choice(SettingsResources.StopBits, settings.StopBits, value => settings.StopBits = value);
        _stopBits = (Picker)_fields.Layout.Children[^1];
        _fields.Choice(SettingsResources.Handshake, settings.Handshake, value => settings.Handshake = value);
        _fields.Toggle(SettingsResources.Dtr, settings.DtrEnable, value => settings.DtrEnable = value);
        _fields.Toggle(SettingsResources.Rts, settings.RtsEnable, value => settings.RtsEnable = value);
        _generalTab.Text = SettingsResources.GeneralSettings;
        _bluetoothTab.Text = SettingsResources.BluetoothSettings;
        _generalTab.Clicked += (_, _) => ShowSettingsTab(false);
        _bluetoothTab.Clicked += (_, _) => ShowSettingsTab(true);
        _tabHeaders.Children.Add(_generalTab);
        Grid.SetColumn(_bluetoothTab, 1);
        _tabHeaders.Children.Add(_bluetoothTab);
        _fields.Layout.Children.Insert(0, _tabHeaders);
        Content = _fields.Layout;
#if ANDROID
        AddMobileBleControls();
#endif
#if IOS || MACCATALYST
        AddMobileBleControls();
#endif
#if WINDOWS
        AddDesktopBluetoothControls();
#endif
        ShowSettingsTab(false);
        UpdateUartAvailability();
    }
    /// <summary>Displays the chosen settings group while preserving every staged control and reader.</summary>
    /// <param name="bluetooth">Whether Bluetooth settings are selected instead of general serial settings.</param>
    private void ShowSettingsTab(bool bluetooth)
    {
        _showBluetooth = bluetooth;
        foreach (View control in _fields.Layout.Children.OfType<View>())
        {
            control.IsVisible = ReferenceEquals(control, _tabHeaders) || _bluetoothControls.Contains(control) == bluetooth;
        }
        _generalTab.FontAttributes = bluetooth ? FontAttributes.None : FontAttributes.Bold;
        _bluetoothTab.FontAttributes = bluetooth ? FontAttributes.Bold : FontAttributes.None;
        SemanticProperties.SetDescription(_generalTab, bluetooth ? _generalTab.Text
            : string.Format(System.Globalization.CultureInfo.CurrentCulture, SettingsResources.SelectedTabDescription, _generalTab.Text));
        SemanticProperties.SetDescription(_bluetoothTab, bluetooth
            ? string.Format(System.Globalization.CultureInfo.CurrentCulture, SettingsResources.SelectedTabDescription, _bluetoothTab.Text) : _bluetoothTab.Text);
        _generalTab.SetAppThemeColor(Button.TextColorProperty, Color.FromArgb("#17202D"), Color.FromArgb("#EDF2F8"));
        _bluetoothTab.SetAppThemeColor(Button.TextColorProperty, Color.FromArgb("#17202D"), Color.FromArgb("#EDF2F8"));
        _generalTab.SetAppThemeColor(BackgroundColorProperty, bluetooth ? Colors.Transparent : Color.FromArgb("#E7EDF5"), bluetooth ? Colors.Transparent : Color.FromArgb("#293444"));
        _bluetoothTab.SetAppThemeColor(BackgroundColorProperty, bluetooth ? Color.FromArgb("#E7EDF5") : Colors.Transparent, bluetooth ? Color.FromArgb("#293444") : Colors.Transparent);
    }
    /// <summary>Adds a platform-specific Bluetooth control to the existing draft without exposing it on the General tab.</summary>
    /// <param name="control">The discovery, permission or profile control retained while switching tabs.</param>
    private void AddBluetoothControl(View control)
    {
        _bluetoothControls.Add(control);
        control.IsVisible = _showBluetooth;
        _fields.Layout.Children.Add(control);
    }
    /// <summary>Replaces the available device snapshot and preserves selection only while that device remains listed.</summary>
    /// <param name="portNames">The current enumerated device identifiers.</param>
    /// <remarks>Call on the UI thread. Missing selections are cleared; a replacement device is never selected automatically.</remarks>
    /// <exception cref="ArgumentNullException">The snapshot is null.</exception>
    public void RefreshPorts(string[] portNames)
    {
        ArgumentNullException.ThrowIfNull(portNames);
        string? selected = _ports.SelectedItem as string;
        _portNames = (string[])portNames.Clone();
        _ports.ItemsSource = (string[])_portNames.Clone();
        _ports.SelectedIndex = selected == null ? -1 : Array.IndexOf(_portNames, selected);
        CanSaveChanged?.Invoke(this, EventArgs.Empty);
    }

#if ANDROID
    /// <summary>Requests only paired-device connection access; discovery and location permissions are not needed.</summary>
    [System.Runtime.Versioning.SupportedOSPlatform("android31.0")]
    private sealed class BluetoothConnectionPermission : Microsoft.Maui.ApplicationModel.Permissions.BasePlatformPermission
    {
        /// <inheritdoc/>
        public override (string androidPermission, bool isRuntime)[] RequiredPermissions =>
            [(Android.Manifest.Permission.BluetoothConnect, true)];
    }

    /// <inheritdoc/>
    protected override void OnHandlerChanged()
    {
        if (_usbWatcher != null)
        {
            _usbWatcher.DevicesChanged -= OnUsbDevicesChanged;
            _usbWatcher.Dispose();
            _usbWatcher = null;
        }
        base.OnHandlerChanged();
        if (Handler != null && _watchDevices)
        {
            _usbWatcher = new AndroidUsbDeviceWatcher(Android.App.Application.Context);
            _usbWatcher.DevicesChanged += OnUsbDevicesChanged;
            RefreshPorts(GXSerial.GetPortNames());
        }
    }

    /// <summary>Dispatches the device list refresh to the UI thread while ignoring notifications from an old handler.</summary>
    /// <param name="sender">The watcher that observed the attachment change.</param>
    /// <param name="args">The attached or detached USB device.</param>
    private void OnUsbDevicesChanged(object? sender, AndroidUsbDeviceEventArgs args)
    {
        Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(() =>
        {
            if (ReferenceEquals(sender, _usbWatcher) && Handler != null)
            {
                RefreshPorts(GXSerial.GetPortNames());
            }
        });
    }
#endif

    /// <summary>Parses input and updates the private settings object without opening a port.</summary>
    /// <exception cref="FormatException">No enumerated port is selected, the GATT device has no canonical catalogue profile, or another integer or selection is invalid.</exception>
    public void Read()
    {
        if (!CanSave)
        {
            throw new FormatException(string.Format(CultureInfo.CurrentCulture, SettingsResources.SelectValue, SettingsResources.PortName));
        }
        string selected = (string)_ports.SelectedItem;
        if (selected.StartsWith("bluetooth:", StringComparison.OrdinalIgnoreCase) || selected.StartsWith("ble:", StringComparison.OrdinalIgnoreCase))
        {
            BluetoothLeProfile? profile = GetSelectedBluetoothProfile(selected);
            if (selected.StartsWith("ble:", StringComparison.OrdinalIgnoreCase) && profile is null)
            {
                throw new FormatException(string.Format(CultureInfo.CurrentCulture, SettingsResources.BluetoothProfileMissing, selected[(selected.IndexOf(':') + 1)..]));
            }
            _settings.BluetoothProfile = selected.StartsWith("ble:", StringComparison.OrdinalIgnoreCase) ? profile : null;
        }
        _settings.PortName = selected;
#if ANDROID || IOS || MACCATALYST
        ReadMobileBleSettings();
#endif
        _fields.Read();
#if WINDOWS
        if (_desktopBluetoothFieldsReady) { ReadDesktopBluetoothSettings(); }
#endif
    }
    /// <summary>Retrieves the original profile only for its original device, ignoring identifier letter case.</summary>
    /// <param name="portName">The complete staged Bluetooth port identifier.</param>
    /// <returns>The saved canonical catalogue profile for the same device, or null when the identity or endpoints are unsupported.</returns>
    private BluetoothLeProfile? GetSavedBluetoothProfile(string? portName)
    {
        return portName?.StartsWith("ble:", StringComparison.OrdinalIgnoreCase) == true && string.Equals(portName, _savedBluetoothPort, StringComparison.OrdinalIgnoreCase) ? _savedBluetoothProfile : null;
    }
    /// <summary>Resolves the selected device's canonical catalogue profile without reading presentation fields.</summary>
    /// <param name="portName">The staged port whose immutable device metadata is required.</param>
    /// <returns>The canonical devices.csv profile associated with this device, or null when it is unsupported.</returns>
    private BluetoothLeProfile? GetSelectedBluetoothProfile(string? portName)
    {
        if (portName?.StartsWith("ble:", StringComparison.OrdinalIgnoreCase) != true) { return null; }
        BluetoothLeProfile? profile = GetSavedBluetoothProfile(portName);
#if WINDOWS
        if (string.Equals(portName, _desktopBluetoothProfilePort, StringComparison.OrdinalIgnoreCase)) { profile = _desktopBluetoothProfile; }
#endif
#if ANDROID || IOS || MACCATALYST
        if (string.Equals(portName, _mobileBleProfilePort, StringComparison.OrdinalIgnoreCase)) { profile = _mobileBluetoothProfile; }
#endif

        return BluetoothLeProfile.FromSavedProfile(profile);
    }
    /// <summary>Enables UART fields only for physical ports or Bluetooth profiles with a registered configuration command generator.</summary>
    private void UpdateUartAvailability()
    {
        string? selected = _ports.SelectedItem as string;
        bool bluetooth = selected?.StartsWith("bluetooth:", StringComparison.OrdinalIgnoreCase) == true
            || selected?.StartsWith("ble:", StringComparison.OrdinalIgnoreCase) == true;
        _selectedBluetoothProfile = GetSelectedBluetoothProfile(selected);
        bool configuration = _selectedBluetoothProfile?.CanConfigureUart == true;
        bool enabled = selected is not null && (!bluetooth || configuration);
        _baudRate.IsEnabled = enabled;
        _parity.IsEnabled = enabled;
        _stopBits.IsEnabled = enabled;
        UpdateGattServiceSelection();
    }

    /// <summary>Adds explicit native service reading below the catalogue information without starting any native operation.</summary>
    /// <param name="information">The existing collapsed device information group.</param>
    private void AddGattServiceControls(VerticalStackLayout information)
    {
        information.Children.Add(new Label { Text = SettingsResources.BluetoothGattServices });
        information.Children.Add(_gattReadButton);
        information.Children.Add(_gattFeedback);
        information.Children.Add(_gattServices);
        _gattReadButton.Clicked += async (_, _) => await ReadGattServicesAsync();
        RenderGattServices();
    }

    /// <summary>Associates native information with a selected catalogue or SPP-advertising GATT endpoint without requiring UART characteristic metadata.</summary>
    private void UpdateGattServiceSelection()
    {
        string? selected = _ports.SelectedItem as string;
        bool eligible = selected?.StartsWith("ble:", StringComparison.OrdinalIgnoreCase) == true
            && _selectedBluetoothProfile is not null;
#if WINDOWS
        eligible |= selected?.StartsWith("ble:", StringComparison.OrdinalIgnoreCase) == true
            && _desktopBluetoothSnapshot.Any(device => "ble:" + device.Id == selected && device.SupportsSerialPort);
        eligible &= _desktopBluetoothProtocol.SelectedIndex == 1;
#endif
#if ANDROID || IOS || MACCATALYST
        eligible |= selected?.StartsWith("ble:", StringComparison.OrdinalIgnoreCase) == true
            && _mobileBleSnapshot.Any(device => "ble:" + device.Id == selected && device.SupportsSerialPort);
#endif
#if ANDROID
        eligible &= _mobileBluetoothProtocol.SelectedIndex == 1;
#endif
        string? id = eligible ? selected![4..] : null;
        if (!string.Equals(_gattQuery.DeviceId, id, StringComparison.OrdinalIgnoreCase))
        {
            ++_gattSelectionGeneration;
            _gattPermissionError = null;
            _gattQuery.SelectDevice(id);
        }
        RenderGattServices();
    }

    /// <summary>Requests connection permission only for an explicit read, then reads all native services without configuring the serial profile.</summary>
    /// <returns>Completion after the service request or cancellation finishes.</returns>
    private async Task ReadGattServicesAsync()
    {
        if (_disposed || _requestingGattPermission) { return; }
        if (_gattQuery.IsBusy) { _gattQuery.Cancel(); return; }
        if (string.IsNullOrWhiteSpace(_gattQuery.DeviceId)) { return; }
        int generation = _gattSelectionGeneration;
        _gattPermissionError = null;
#if ANDROID
        _requestingGattPermission = true;
        RenderGattServices();
        try
        {
            var permission = OperatingSystem.IsAndroidVersionAtLeast(31)
                ? await Microsoft.Maui.ApplicationModel.Permissions.RequestAsync<BluetoothConnectionPermission>()
                : Microsoft.Maui.ApplicationModel.PermissionStatus.Granted;
            if (_disposed || generation != _gattSelectionGeneration) { return; }
            if (permission != Microsoft.Maui.ApplicationModel.PermissionStatus.Granted)
            {
                _gattPermissionError = SettingsResources.BluetoothPermissionDenied;
                return;
            }
        }
        catch (Exception error)
        {
            if (!_disposed && generation == _gattSelectionGeneration)
            {
                _gattPermissionError = string.Format(CultureInfo.CurrentCulture, SettingsResources.GattServicesFailed, error.Message);
            }
            return;
        }
        finally
        {
            _requestingGattPermission = false;
            RenderGattServices();
        }
#endif
        if (_disposed || generation != _gattSelectionGeneration) { return; }
#if WINDOWS
        CancelDesktopBluetoothDiscovery();
#endif
#if ANDROID || IOS || MACCATALYST
        CancelMobileBleScan();
#endif
        await _gattQuery.ReadAsync();
    }

    /// <summary>Marshals native query notifications onto the owning UI dispatcher.</summary>
    private void OnGattServicesChanged()
    {
        if (_disposed) { return; }
        if (Handler is null || !Dispatcher.IsDispatchRequired) { RenderGattServices(); }
        else { Dispatcher.Dispatch(RenderGattServices); }
    }

    /// <summary>Renders actual services separately from CSV UUIDs and reflects read progress without modifying selection.</summary>
    private void RenderGattServices()
    {
        if (_disposed) { return; }
        bool busy = _gattQuery.IsBusy;
        _gattReadButton.Text = busy ? SettingsResources.CancelGattRead : SettingsResources.ReadGattServices;
        _gattReadButton.IsEnabled = !_requestingGattPermission && !string.IsNullOrWhiteSpace(_gattQuery.DeviceId);
        _gattServices.Children.Clear();
        var additionalServices = _gattQuery.GetAdditionalServices(_selectedBluetoothProfile);
        if (additionalServices.Count != 0)
        {
            _gattServices.Children.Add(new Editor
            {
                Text = string.Join(Environment.NewLine, additionalServices),
                IsReadOnly = true,
                AutoSize = EditorAutoSizeOption.TextChanges
            });
        }
        _gattFeedback.Text = _gattPermissionError ?? (busy || _requestingGattPermission ? SettingsResources.GattServicesLoading
            : _gattQuery.Error is string error ? string.Format(CultureInfo.CurrentCulture, SettingsResources.GattServicesFailed, error)
            : _gattQuery.HasResult && additionalServices.Count == 0 ? SettingsResources.GattServicesEmpty : SettingsResources.GattServicesHint);
#if WINDOWS
        if (_desktopBluetoothFieldsReady) { _desktopBluetoothRefresh.IsEnabled = !busy && !_requestingGattPermission && _desktopBluetoothDiscovery is null; }
#endif
#if ANDROID || IOS || MACCATALYST
        if (_mobileBleReady) { _mobileBleScanButton.IsEnabled = !busy && !_requestingGattPermission; }
#endif
    }

    /// <summary>Cancels outstanding information reads and stops callbacks when the settings draft is discarded.</summary>
    public void Dispose()
    {
        if (_disposed) { return; }
        _disposed = true;
        ++_gattSelectionGeneration;
        _gattQuery.Changed -= OnGattServicesChanged;
        _gattQuery.Dispose();
#if WINDOWS
        CancelDesktopBluetoothDiscovery();
#endif
#if ANDROID || IOS || MACCATALYST
        CancelMobileBleScan();
#endif
    }

    /// <summary>Cancels outstanding reads and awaits native service cleanup before the draft is released.</summary>
    /// <returns>Completion after native GATT query resources have been released.</returns>
    public async ValueTask DisposeAsync()
    {
        Dispose();
        await _gattQuery.DisposeAsync().ConfigureAwait(false);
    }



}
