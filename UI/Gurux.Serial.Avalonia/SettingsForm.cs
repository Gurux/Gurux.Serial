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
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Layout;
using Avalonia.Media;
using Gurux.Serial;
using Gurux.Serial.Avalonia.Properties;
namespace Gurux.Serial.Avalonia;
/// <summary>Builds reusable typed settings controls without touching live transport state.</summary>
public sealed class SettingsForm : IDisposable, IAsyncDisposable
{
    /// <summary>Initializes an empty staged settings form using native cached Bluetooth discovery.</summary>
    public SettingsForm() : this(BluetoothSerialTransports.GetDevicesAsync) { }
    /// <summary>Initializes a staged form with the cached Bluetooth discovery provider used by explicit RFCOMM searches.</summary>
    /// <param name="discoverBluetooth">Returns cached device identities and metadata only when the user starts discovery.</param>
    /// <exception cref="ArgumentNullException">The discovery provider is null.</exception>
    internal SettingsForm(Func<BluetoothSerialTransport, CancellationToken, Task<BluetoothSerialDevice[]>> discoverBluetooth)
        : this(discoverBluetooth, new BluetoothGattServiceQuery())
    {
    }
    /// <summary>Initializes a staged form with explicit discovery and an owned native GATT service query.</summary>
    /// <param name="discoverBluetooth">Enumerates cached devices only after an explicit user action.</param>
    /// <param name="gattQuery">Reads actual native services only after the information action is activated.</param>
    /// <exception cref="ArgumentNullException">A provider or query is null.</exception>
    internal SettingsForm(Func<BluetoothSerialTransport, CancellationToken, Task<BluetoothSerialDevice[]>> discoverBluetooth, BluetoothGattServiceQuery gattQuery)
    {
        ArgumentNullException.ThrowIfNull(discoverBluetooth);
        ArgumentNullException.ThrowIfNull(gattQuery);
        _discoverBluetooth = discoverBluetooth;
        _gattQuery = gattQuery;
    }
    /// <summary>Owns information reads separately from catalogue profile selection and advertisement metadata.</summary>
    private readonly BluetoothGattServiceQuery _gattQuery;
    /// <summary>Tracks render subscriptions released when the form closes.</summary>
    private readonly List<Action> _gattSubscriptions = [];
    /// <summary>Rejects queued native information updates after disposal.</summary>
    private bool _disposed;
    /// <summary>Lists cached Bluetooth devices only in response to an explicit discovery action.</summary>
    private readonly Func<BluetoothSerialTransport, CancellationToken, Task<BluetoothSerialDevice[]>> _discoverBluetooth;
    /// <summary>Stores transfers from controls into a private draft.</summary>
    private readonly List<Action> _read = [];
    /// <summary>Identifies Bluetooth discovery and profile controls independently of the public field order.</summary>
    private readonly HashSet<Control> bluetoothFields = [];
    /// <summary>Tracks required port selections and their discovery snapshots.</summary>
    private readonly List<(ComboBox Control, string[] Ports)> _portSelections = [];
    /// <summary>Gets whether every serial input has selected a port from its discovery snapshot.</summary>
    public bool CanSave => _portSelections.All(selection => selection.Control.SelectedItem is string selected && Array.IndexOf(selection.Ports, selected) >= 0);
    /// <summary>Occurs when a port selection changes and save availability must be refreshed.</summary>
    public event EventHandler? CanSaveChanged;
    /// <summary>Gets labeled controls in display order.</summary>
    public List<(string Label, Control View)> Fields { get; } = [];
    /// <summary>Reads all current control values into their draft properties.</summary>
    /// <exception cref="FormatException">A required port selection or another typed field is invalid.</exception>
    public void Read() { foreach (var read in _read) { read(); } }
    /// <summary>Builds native tabs over the existing staged fields without copying or applying their values.</summary>
    /// <returns>A tab control with general serial settings selected first and Bluetooth settings second.</returns>
    internal Control CreateView()
    {
        var general = new StackPanel { Spacing = 8 };
        var bluetooth = new StackPanel { Spacing = 8 };
        foreach (var field in Fields)
        {
            if (bluetoothFields.Contains(field.View))
            {
                bluetooth.Children.Add(field.View);
            }
            else
            {
                general.Children.Add(new TextBlock { Text = field.Label });
                general.Children.Add(field.View);
            }
            global::Avalonia.Automation.AutomationProperties.SetName(field.View, field.Label);
        }
        return new TabControl
        {
            Name = "SerialSettingsTabs",
            SelectedIndex = 0,
            ItemsSource = new TabItem[]
            {
                new() { Name = "SerialGeneralTab", Header = Resources.Get("GeneralSettings"), Content = general },
                new() { Name = "SerialBluetoothTab", Header = Resources.Get("BluetoothSettings"), Content = bluetooth }
            }
        };
    }
    /// <summary>Adds the standard serial port controls for a private settings object.</summary>
    /// <param name="serial">The private port configuration to edit.</param>
    /// <exception cref="ArgumentNullException">The private settings object is null.</exception>
    public void AddSerial(SerialPortSettings serial) => AddSerial(serial, GXSerial.GetPortNames());
    /// <summary>Adds serial controls using a fixed snapshot of the discovered port names.</summary>
    /// <remarks>Baud rates use the common-rate list, and data bits are selected from five through eight.</remarks>
    /// <param name="serial">The private port configuration updated only when the form is read.</param>
    /// <param name="portNames">Port names offered by the host's discovery method.</param>
    /// <exception cref="ArgumentNullException">The settings or port snapshot is null.</exception>
    public void AddSerial(SerialPortSettings serial, IReadOnlyList<string> portNames)
    {
        ArgumentNullException.ThrowIfNull(serial);
        ArgumentNullException.ThrowIfNull(portNames);
        var ports = portNames.ToArray();
        if (((serial.PortName ?? string.Empty).StartsWith("bluetooth:", StringComparison.OrdinalIgnoreCase) || (serial.PortName ?? string.Empty).StartsWith("ble:", StringComparison.OrdinalIgnoreCase)) && !ports.Contains(serial.PortName, StringComparer.Ordinal))
        {
            ports = ports.Append(serial.PortName!).ToArray();
        }
        var control = new ComboBox { ItemsSource = (string[])ports.Clone(), SelectedIndex = Array.IndexOf(ports, serial.PortName) };
        int selectionIndex = _portSelections.Count;
        _portSelections.Add((control, ports));
        control.SelectionChanged += (_, _) => CanSaveChanged?.Invoke(this, EventArgs.Empty);
        Fields.Add((Resources.Get("PortName"), control));
        _read.Add(() =>
        {
            if (control.SelectedItem is not string selected || Array.IndexOf(ports, selected) < 0)
            {
                throw new FormatException(string.Format(CultureInfo.CurrentCulture, Resources.Get("ChoiceError"), Resources.Get("PortName")));
            }
            serial.PortName = selected;
        });
        Action updateFraming = () => { };
        Func<bool> hasConfiguration = AddBluetooth(serial, control, identifier =>
        {
            ports = ports.Append(identifier).Distinct(StringComparer.Ordinal).ToArray();
            _portSelections[selectionIndex] = (control, ports);
            control.ItemsSource = (string[])ports.Clone();
            control.SelectedItem = identifier;
            CanSaveChanged?.Invoke(this, EventArgs.Empty);
        }, () => updateFraming());
        NumberChoice(Resources.Get("BaudRate"), serial.BaudRate, GXSerial.GetAvailableBaudRates(serial.PortName), value => serial.BaudRate = value);
        Control baud = Fields[^1].View;
        NumberChoice(Resources.Get("DataBits"), serial.DataBits, [5, 6, 7, 8], value => serial.DataBits = value);
        Choice(Resources.Get("Parity"), serial.Parity, value => serial.Parity = value);
        Control parity = Fields[^1].View;
        Choice(Resources.Get("StopBits"), serial.StopBits, value => serial.StopBits = value);
        Control stop = Fields[^1].View;
        Choice(Resources.Get("Handshake"), serial.Handshake, value => serial.Handshake = value);
        Toggle(Resources.Get("Dtr"), serial.DtrEnable, value => serial.DtrEnable = value);
        Toggle(Resources.Get("Rts"), serial.RtsEnable, value => serial.RtsEnable = value);
        updateFraming = () =>
        {
            string? selected = control.SelectedItem as string;
            bool bluetooth = selected?.StartsWith("bluetooth:", StringComparison.OrdinalIgnoreCase) == true
                || selected?.StartsWith("ble:", StringComparison.OrdinalIgnoreCase) == true;
            bool enabled = selected is not null && (!bluetooth || (selected.StartsWith("ble:", StringComparison.OrdinalIgnoreCase) && hasConfiguration()));
            baud.IsEnabled = enabled;
            parity.IsEnabled = enabled;
            stop.IsEnabled = enabled;
        };
        control.SelectionChanged += (_, _) => updateFraming();
        updateFraming();
    }
    /// <summary>Adds catalogue-only Bluetooth device selection, protocol-specific discovery and collapsed read-only identity and GATT details.</summary>
    /// <remarks>GATT identities require canonical devices.csv profiles. Classic discovery requires the Serial Port service and resolves its channel through SDP; saved Classic identities restore without GATT metadata.</remarks>
    /// <param name="serial">Private settings updated only by <see cref="Read"/>.</param>
    /// <param name="port">The shared port selector.</param>
    /// <param name="select">Offers and selects a validated Bluetooth identifier.</param>
    /// <param name="changed">Refreshes UART field availability when the configuration UUID changes.</param>
    /// <returns>A predicate indicating whether the selected device has a registered configuration command generator.</returns>
    private Func<bool> AddBluetooth(SerialPortSettings serial, ComboBox port, Action<string> select, Action changed)
    {
        string? savedPort = serial.PortName;
        BluetoothLeProfile? savedProfile = savedPort?.StartsWith("ble:", StringComparison.OrdinalIgnoreCase) == true ? BluetoothLeProfile.FromSavedProfile(serial.BluetoothProfile) : null;
        BluetoothLeProfile? selectedProfile = savedPort?.StartsWith("ble:", StringComparison.OrdinalIgnoreCase) == true
            || savedPort?.StartsWith("bluetooth:", StringComparison.OrdinalIgnoreCase) == true ? savedProfile : null;
        var panel = new StackPanel { Spacing = 6 };
        var protocol = new ComboBox { ItemsSource = new[] { Resources.Get("BluetoothClassic"), Resources.Get("BluetoothLowEnergy") }, SelectedIndex = (serial.PortName ?? string.Empty).StartsWith("ble:", StringComparison.OrdinalIgnoreCase) ? 1 : 0 };
        var discover = new Button { Name = "FindBluetoothDevices", Content = Resources.Get("FindDevices") };
        var devices = new ComboBox { Name = "BluetoothDevices" };
        var identifier = new TextBox { IsReadOnly = true, Text = (serial.PortName ?? string.Empty).StartsWith("ble:", StringComparison.OrdinalIgnoreCase) ? (serial.PortName ?? string.Empty)[4..] : (serial.PortName ?? string.Empty).StartsWith("bluetooth:", StringComparison.OrdinalIgnoreCase) ? (serial.PortName ?? string.Empty)[10..] : string.Empty };
        var status = new TextBlock { TextWrapping = global::Avalonia.Media.TextWrapping.Wrap };
        var service = new TextBox { IsReadOnly = true, Text = selectedProfile?.Service.ToString() };
        var read = new TextBox { IsReadOnly = true, Text = selectedProfile?.ReadCharacteristic.ToString() };
        var write = new TextBox { IsReadOnly = true, Text = selectedProfile?.WriteCharacteristic.ToString() };
        var configuration = new TextBox { IsReadOnly = true, Text = selectedProfile?.ConfigurationCharacteristic?.ToString() };
        Action<BluetoothLeProfile?> populate = profile =>
        {
            selectedProfile = profile;
            service.Text = profile?.Service.ToString();
            read.Text = profile?.ReadCharacteristic.ToString();
            write.Text = profile?.WriteCharacteristic.ToString();
            configuration.Text = profile?.ConfigurationCharacteristic?.ToString();
        };
        string? profilePort = savedPort;
        bool selectingBluetoothPort = false;
        bool updatingDevices = false;
        BluetoothSerialDevice[] discovered = [];
        BluetoothSerialDevice[] listedDevices = [];
        Action updateGattSelection = () => { };
        Action<BluetoothSerialDevice[]> showDevices = snapshot =>
        {
            BluetoothSerialDevice[] previousDevices = listedDevices;
            discovered = snapshot.Where(device => device.SupportsSerialPort).ToArray();
            listedDevices = discovered;
            var transport = protocol.SelectedIndex == 1 ? BluetoothSerialTransport.LowEnergy : BluetoothSerialTransport.Classic;
            string prefix = transport == BluetoothSerialTransport.LowEnergy ? "ble:" : "bluetooth:";
            string? selected = port.SelectedItem as string;
            string? selectedId = selected?.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) == true ? selected[prefix.Length..] : null;
            BluetoothSerialDevice? selectedDevice = discovered.FirstOrDefault(device => device.Transport == transport
                && string.Equals(device.Id, selectedId, StringComparison.OrdinalIgnoreCase));
            if (transport == BluetoothSerialTransport.LowEnergy && selectedDevice?.Profile is BluetoothLeProfile canonical)
            {
                profilePort = selected;
                identifier.Text = selectedDevice.Id;
                populate(canonical);
                changed();
            }
            string? savedId = (transport == BluetoothSerialTransport.Classic || savedProfile is not null) && savedPort?.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) == true ? savedPort[prefix.Length..] : null;
            foreach (string? id in new[] { savedId, selectedId })
            {
                BluetoothSerialDevice? previous = previousDevices.FirstOrDefault(device => device.Transport == transport
                    && string.Equals(device.Id, id, StringComparison.OrdinalIgnoreCase) && device.SupportsSerialPort);
                bool catalogueMatch = string.Equals(id, savedId, StringComparison.OrdinalIgnoreCase)
                    || (selectedProfile is not null && string.Equals(profilePort, selected, StringComparison.OrdinalIgnoreCase)) || previous is not null;
                if (id is not null && catalogueMatch && !listedDevices.Any(device => string.Equals(device.Id, id, StringComparison.OrdinalIgnoreCase)))
                {
                    listedDevices = listedDevices.Append(previous ?? new BluetoothSerialDevice(id, id, transport)).ToArray();
                }
            }
            updatingDevices = true;
            try
            {
                devices.ItemsSource = listedDevices.Select(device => string.Format(CultureInfo.CurrentCulture,
                    Resources.Get("BluetoothDeviceDisplay"), device.Name, device.Address ?? device.Id)).ToArray();
                devices.SelectedIndex = Array.FindIndex(listedDevices, device => string.Equals(device.Id, selectedId, StringComparison.OrdinalIgnoreCase));
            }
            finally
            {
                updatingDevices = false;
            }
            updateGattSelection();
        };
        CancellationTokenSource? discovery = null;
        Task? discoveryTask = null;
        CancellationTokenSource? scanCancellation = null;
        var discoveryHint = new TextBlock { TextWrapping = global::Avalonia.Media.TextWrapping.Wrap };
        Action updateDiscovery = () =>
        {
            bool lowEnergy = protocol.SelectedIndex == 1;
            discover.IsEnabled = !_gattQuery.IsBusy && discovery is null && (!lowEnergy || BluetoothLeDiscovery.IsSupported);
            ToolTip.SetTip(discover, lowEnergy && !BluetoothLeDiscovery.IsSupported ? Resources.Get("BluetoothScanUnavailable") : null);
            discoveryHint.Text = Resources.Get(lowEnergy ? "FindBleDevicesHint" : "FindKnownDevicesHint");
        };
        Action stopScan = () =>
        {
            CancellationTokenSource? previous = scanCancellation;
            scanCancellation = null;
            previous?.Cancel();
            discover.Content = Resources.Get("FindDevices");
            updateDiscovery();
            if (previous != null)
            {
                status.Text = discovered.Length == 0 ? Resources.Get("BleScanNoDevices")
                    : string.Format(CultureInfo.CurrentCulture, Resources.Get("BleScanComplete"), discovered.Length);
            }
        };
        Action<string, Control> addField = (key, field) =>
        {
            panel.Children.Add(new TextBlock { Text = Resources.Get(key) });
            panel.Children.Add(field);
        };
        addField("BluetoothProtocol", protocol);
        panel.Children.Add(discover);
        panel.Children.Add(discoveryHint);
        updateDiscovery();
        var details = new StackPanel { Spacing = 6 };
        foreach (var field in new[] { ("BluetoothIdentifier", identifier), ("BluetoothService", service), ("BluetoothRead", read), ("BluetoothWrite", write), ("BluetoothConfiguration", configuration) })
        {
            details.Children.Add(new TextBlock { Text = Resources.Get(field.Item1) });
            details.Children.Add(field.Item2);
        }
        var readGatt = new Button { Name = "ReadGattServices", Content = Resources.Get("ReadGattServices") };
        var gattStatus = new TextBlock { Name = "GattServicesStatus", TextWrapping = TextWrapping.Wrap };
        var gattServices = new StackPanel { Name = "GattServices", Spacing = 4 };
        details.Children.Add(new TextBlock { Text = Resources.Get("BluetoothGattServices") });
        details.Children.Add(readGatt);
        details.Children.Add(gattStatus);
        details.Children.Add(gattServices);
        Action renderGatt = () =>
        {
            if (_disposed) { return; }
            readGatt.Content = Resources.Get(_gattQuery.IsBusy ? "CancelGattRead" : "ReadGattServices");
            readGatt.IsEnabled = !string.IsNullOrWhiteSpace(_gattQuery.DeviceId);
            gattServices.Children.Clear();
            var additionalServices = _gattQuery.GetAdditionalServices(selectedProfile);
            if (additionalServices.Count != 0)
            {
                gattServices.Children.Add(new TextBox
                {
                    Text = string.Join(Environment.NewLine, additionalServices),
                    IsReadOnly = true,
                    AcceptsReturn = true
                });
            }
            gattStatus.Text = _gattQuery.IsBusy ? Resources.Get("GattServicesLoading")
                : _gattQuery.Error is string error ? string.Format(CultureInfo.CurrentCulture, Resources.Get("GattServicesFailed"), error)
                : _gattQuery.HasResult && additionalServices.Count == 0 ? Resources.Get("GattServicesEmpty") : Resources.Get("GattServicesHint");
            updateDiscovery();
        };
        Action onGattChanged = () =>
        {
            if (_disposed) { return; }
            if (global::Avalonia.Threading.Dispatcher.UIThread.CheckAccess()) { renderGatt(); }
            else { global::Avalonia.Threading.Dispatcher.UIThread.Post(renderGatt); }
        };
        _gattQuery.Changed += onGattChanged;
        _gattSubscriptions.Add(onGattChanged);
        updateGattSelection = () =>
        {
            string? selected = port.SelectedItem as string;
            bool eligible = protocol.SelectedIndex == 1 && selected?.StartsWith("ble:", StringComparison.OrdinalIgnoreCase) == true
                && ((string.Equals(profilePort, selected, StringComparison.OrdinalIgnoreCase) && selectedProfile is not null) || listedDevices.Any(device => "ble:" + device.Id == selected && device.SupportsSerialPort));
            _gattQuery.SelectDevice(eligible ? selected![4..] : null);
            renderGatt();
        };
        readGatt.Click += async (_, _) =>
        {
            if (_disposed) { return; }
            if (_gattQuery.IsBusy) { _gattQuery.Cancel(); return; }
            if (string.IsNullOrWhiteSpace(_gattQuery.DeviceId)) { return; }
            stopScan();
            discovery?.Cancel();
            discovery = null;
            await _gattQuery.ReadAsync();
        };
        panel.Children.Add(status);
        addField("BluetoothDevices", devices);
        panel.Children.Add(CreateBluetoothInformation(details));
        Fields.Add((Resources.Get("BluetoothSettings"), panel));
        bluetoothFields.Add(panel);
        Action<string> bindProfile = selected =>
        {
            if (string.Equals(profilePort, selected, StringComparison.OrdinalIgnoreCase)) { return; }
            profilePort = selected;
            bool bluetooth = selected.StartsWith("ble:", StringComparison.OrdinalIgnoreCase) || selected.StartsWith("bluetooth:", StringComparison.OrdinalIgnoreCase);
            identifier.Text = bluetooth ? selected[(selected.IndexOf(':') + 1)..] : string.Empty;
            if (selected.StartsWith("ble:", StringComparison.OrdinalIgnoreCase))
            {
                BluetoothSerialTransport transport = selected.StartsWith("ble:", StringComparison.OrdinalIgnoreCase) ? BluetoothSerialTransport.LowEnergy : BluetoothSerialTransport.Classic;
                populate(string.Equals(selected, savedPort, StringComparison.OrdinalIgnoreCase) ? savedProfile
                    : listedDevices.FirstOrDefault(device => device.Transport == transport && string.Equals(device.Id, identifier.Text, StringComparison.OrdinalIgnoreCase))?.Profile);
            }
            else
            {
                populate(null);
            }
        };
        port.SelectionChanged += (_, _) =>
        {
            if (!selectingBluetoothPort && port.SelectedItem is string selected)
            {
                bindProfile(selected);
                showDevices(discovered);
                changed();
            }
        };
        devices.SelectionChanged += (_, _) =>
        {
            if (!updatingDevices && devices.SelectedIndex >= 0 && devices.SelectedIndex < listedDevices.Length)
            {
                var selected = listedDevices[devices.SelectedIndex];
                stopScan();
                identifier.Text = selected.Id;
                string chosen = (selected.Transport == BluetoothSerialTransport.LowEnergy ? "ble:" : "bluetooth:") + selected.Id;
                profilePort = chosen;
                populate(selected.Transport == BluetoothSerialTransport.LowEnergy ? selected.Profile ?? (string.Equals(chosen, savedPort, StringComparison.OrdinalIgnoreCase) ? savedProfile : null) : null);
                selectingBluetoothPort = true;
                try { select(chosen); }
                finally { selectingBluetoothPort = false; }
                updateGattSelection();
                changed();
            }
        };
        protocol.SelectionChanged += (_, _) =>
        {
            stopScan();
            discovery?.Cancel();
            discovery = null;
            updateDiscovery();
            showDevices([]);
            status.Text = string.Empty;
        };
        Func<Task> discoverAsync = async () =>
        {
            stopScan();
            using var cancel = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            discovery = cancel;
            discover.IsEnabled = false;
            status.Text = Resources.Get("BluetoothDiscovering");
            try
            {
                var transport = protocol.SelectedIndex == 1 ? BluetoothSerialTransport.LowEnergy : BluetoothSerialTransport.Classic;
                var snapshot = await _discoverBluetooth(transport, cancel.Token);
                cancel.Token.ThrowIfCancellationRequested();
                if (!ReferenceEquals(discovery, cancel))
                {
                    return;
                }
                showDevices(snapshot);
                status.Text = discovered.Length == 0 ? Resources.Get("BluetoothNoDevices") : string.Empty;
            }
            catch (OperationCanceledException)
            {
                if (ReferenceEquals(discovery, cancel))
                {
                    status.Text = Resources.Get("BluetoothDiscoveryCanceled");
                }
            }
            catch (Exception error)
            {
                if (ReferenceEquals(discovery, cancel))
                {
                    status.Text = string.Format(CultureInfo.CurrentCulture, Resources.Get("BluetoothDiscoveryFailed"), error.Message);
                }
            }
            finally
            {
                if (ReferenceEquals(discovery, cancel))
                {
                    discovery = null;
                    updateDiscovery();
                }
            }
        };
        discover.Click += async (_, _) =>
        {
            if (protocol.SelectedIndex != 1)
            {
                if (discoveryTask == null || discoveryTask.IsCompleted)
                {
                    discoveryTask = discoverAsync();
                    await discoveryTask;
                }
                return;
            }
            if (scanCancellation != null)
            {
                stopScan();
                return;
            }
            discovery?.Cancel();
            discovery = null;
            using var cancel = new CancellationTokenSource();
            scanCancellation = cancel;
            discover.Content = Resources.Get("StopBleScan");
            status.Text = Resources.Get("BleScanning");
            showDevices([]);
            try
            {
                BluetoothSerialDevice[] snapshot = await BluetoothLeDiscovery.ScanAsync(deviceFound: device => global::Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    if (!ReferenceEquals(scanCancellation, cancel) || cancel.IsCancellationRequested)
                    {
                        return;
                    }
                    if (!device.SupportsSerialPort) { return; }
                    int index = Array.FindIndex(discovered, item => string.Equals(item.Id, device.Id, StringComparison.Ordinal));
                    if (index < 0)
                    {
                        discovered = discovered.Append(device).ToArray();
                    }
                    else
                    {
                        discovered[index] = device;
                    }
                    showDevices(discovered);
                }), cancellationToken: cancel.Token);
                if (ReferenceEquals(scanCancellation, cancel))
                {
                    showDevices(snapshot);
                    status.Text = discovered.Length == 0 ? Resources.Get("BleScanNoDevices")
                        : string.Format(CultureInfo.CurrentCulture, Resources.Get("BleScanComplete"), discovered.Length);
                }
            }
            catch (OperationCanceledException) when (cancel.IsCancellationRequested)
            {
            }
            catch (Exception)
            {
                if (ReferenceEquals(scanCancellation, cancel))
                {
                    status.Text = Resources.Get("BleScanFailed");
                }
            }
            finally
            {
                if (ReferenceEquals(scanCancellation, cancel))
                {
                    scanCancellation = null;
                    discover.Content = Resources.Get("FindDevices");
                    updateDiscovery();
                }
            }
        };
        panel.DetachedFromVisualTree += (_, _) =>
        {
            _gattQuery.Cancel();
            stopScan();
            discovery?.Cancel();
            discovery = null;
        };
        showDevices([]);
        _read.Add(() =>
        {
            if (port.SelectedItem is not string selected)
            {
                return;
            }
            bool classic = selected.StartsWith("bluetooth:", StringComparison.OrdinalIgnoreCase);
            bool lowEnergy = selected.StartsWith("ble:", StringComparison.OrdinalIgnoreCase);
            if (!classic && !lowEnergy)
            {
                return;
            }
            if (lowEnergy && (selectedProfile is null || !string.Equals(selected, profilePort, StringComparison.OrdinalIgnoreCase)))
            {
                throw new FormatException(string.Format(CultureInfo.CurrentCulture,
                    Resources.Get("BluetoothCatalogueRequired"), selected[(selected.IndexOf(':') + 1)..]));
            }
            if (classic)
            {
                serial.BluetoothChannel = 0;
            }
            serial.BluetoothProfile = lowEnergy ? selectedProfile : null;
            populate(selectedProfile);
        });
        return () => string.Equals(profilePort, port.SelectedItem as string, StringComparison.OrdinalIgnoreCase)
            && selectedProfile?.CanConfigureUart == true;
    }
    /// <summary>Cancels native information reads and releases render subscriptions when the settings draft closes.</summary>
    public void Dispose()
    {
        if (_disposed) { return; }
        _disposed = true;
        foreach (Action subscription in _gattSubscriptions) { _gattQuery.Changed -= subscription; }
        _gattSubscriptions.Clear();
        _gattQuery.Dispose();
    }
    /// <summary>Cancels information reads and waits for their native connection resources to be released.</summary>
    /// <returns>Completion after the service query has finished native cleanup.</returns>
    public async ValueTask DisposeAsync()
    {
        Dispose();
        await _gattQuery.DisposeAsync().ConfigureAwait(false);
    }
    /// <summary>Creates a compact native disclosure with a single left triangle and indented device information.</summary>
    /// <param name="details">The existing read-only identity and GATT fields revealed by the disclosure.</param>
    /// <returns>A collapsed expander with native toggle keyboard handling and an unfilled header.</returns>
    private static Expander CreateBluetoothInformation(Control details)
    {
        return new Expander
        {
            Name = "BluetoothAdvancedSettings",
            Header = Resources.Get("BluetoothAdvancedSettings"),
            Content = details,
            IsExpanded = false,
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Padding = new Thickness(0),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            Template = new FuncControlTemplate<Expander>((owner, scope) =>
            {
                var triangle = new TextBlock { FontSize = 10, VerticalAlignment = VerticalAlignment.Center };
                triangle.Bind(TextBlock.TextProperty, new Binding(nameof(Expander.IsExpanded))
                {
                    Source = owner,
                    Converter = new FuncValueConverter<bool, string>(expanded => expanded ? "▼" : "▶")
                });
                var caption = new ContentPresenter { VerticalAlignment = VerticalAlignment.Center };
                caption.Bind(ContentPresenter.ContentProperty, new Binding(nameof(Expander.Header)) { Source = owner });
                var header = new ToggleButton
                {
                    Name = "PART_toggle",
                    HorizontalAlignment = HorizontalAlignment.Left,
                    HorizontalContentAlignment = HorizontalAlignment.Left,
                    Padding = new Thickness(0),
                    MinHeight = 0,
                    Background = Brushes.Transparent,
                    BorderThickness = new Thickness(0),
                    FocusAdorner = null,
                    Content = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, Children = { triangle, caption } },
                    Template = new FuncControlTemplate<ToggleButton>((button, _) =>
                    {
                        var focus = new Border { BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(2), IsHitTestVisible = false };
                        focus.Bind(Border.BorderBrushProperty, new Binding(nameof(ToggleButton.Foreground)) { Source = button });
                        focus.Bind(Control.IsVisibleProperty, new Binding(nameof(ToggleButton.IsFocused)) { Source = button });
                        return new Grid
                        {
                            Children = { new ContentPresenter { Content = button.Content, Margin = new Thickness(3, 2) }, focus }
                        };
                    })
                };
                global::Avalonia.Automation.AutomationProperties.SetName(header, owner.Header?.ToString() ?? string.Empty);
                header.Bind(ToggleButton.IsCheckedProperty, new Binding(nameof(Expander.IsExpanded)) { Source = owner, Mode = BindingMode.TwoWay });
                var content = new ContentPresenter
                {
                    Name = "PART_ContentPresenter",
                    Margin = new Thickness(16, 6, 0, 0),
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    HorizontalContentAlignment = HorizontalAlignment.Stretch
                };
                content.Bind(ContentPresenter.ContentProperty, new Binding(nameof(Expander.Content)) { Source = owner });
                content.Bind(Control.IsVisibleProperty, new Binding(nameof(Expander.IsExpanded)) { Source = owner });
                scope.Register("PART_toggle", header);
                scope.Register("PART_ContentPresenter", content);
                return new StackPanel { Children = { header, content } };
            })
        };
    }
    /// <summary>Adds a dropdown restricted to supplied integer values without substituting an unknown initial value.</summary>
    /// <param name="label">The translated field caption.</param>
    /// <param name="value">The configured value, selected only when present in the choices.</param>
    /// <param name="choices">The available values copied by the control.</param>
    /// <param name="set">Updates the private settings property when the enabled control is read; disabled values are preserved.</param>
    /// <exception cref="ArgumentNullException">The choices are null.</exception>
    public void NumberChoice(string label, int value, int[] choices, Action<int> set)
    {
        ArgumentNullException.ThrowIfNull(choices);
        var available = choices.ToArray();
        var control = new ComboBox { ItemsSource = available.ToArray(), SelectedIndex = Array.IndexOf(available, value) };
        Fields.Add((label, control));
        _read.Add(() =>
        {
            if (!control.IsEnabled) { return; }
            if (control.SelectedIndex < 0 || control.SelectedIndex >= available.Length)
            {
                throw new FormatException(string.Format(CultureInfo.CurrentCulture, Resources.Get("ChoiceError"), label));
            }
            set(available[control.SelectedIndex]);
        });
    }
    /// <summary>Adds an editable string field.</summary>
    /// <param name="label">The translated field caption.</param>
    /// <param name="value">The initial draft value.</param>
    /// <param name="set">Writes the edited string to the draft.</param>
    /// <param name="password">Whether to mask the displayed value.</param>
    /// <param name="multiline">Whether to accept multiple lines.</param>
    public void Text(string label, string? value, Action<string> set, bool password = false, bool multiline = false)
    {
        var control = new TextBox { Text = value, AcceptsReturn = multiline, MinHeight = multiline ? 80 : 0, PasswordChar = password ? '\u25cf' : '\0' };
        Fields.Add((label, control));
        _read.Add(() => set(control.Text ?? ""));
    }
    /// <summary>Adds a whole number field using the current culture.</summary>
    /// <param name="label">The translated field caption.</param>
    /// <param name="value">The initial draft value.</param>
    /// <param name="set">Writes the parsed number to the draft.</param>
    public void Number(string label, int value, Action<int> set)
    {
        var control = new TextBox { Text = value.ToString(CultureInfo.CurrentCulture) };
        Fields.Add((label, control));
        _read.Add(() =>
        {
            if (!int.TryParse(control.Text, NumberStyles.Integer, CultureInfo.CurrentCulture, out var parsed))
            {
                throw new FormatException(string.Format(CultureInfo.CurrentCulture, Resources.Get("NumberError"), label));
            }
            set(parsed);
        });
    }
    /// <summary>Adds a Boolean field.</summary>
    /// <param name="label">The translated field caption.</param>
    /// <param name="value">The initial draft value.</param>
    /// <param name="set">Writes the checked state to the draft.</param>
    public void Toggle(string label, bool value, Action<bool> set)
    {
        var control = new CheckBox { IsChecked = value };
        Fields.Add((label, control));
        _read.Add(() => set(control.IsChecked == true));
    }
    /// <summary>Adds a choice constrained to declared enumeration values.</summary>
    /// <typeparam name="T">The enumeration of allowed values.</typeparam>
    /// <param name="label">The translated field caption.</param>
    /// <param name="value">The initial draft selection.</param>
    /// <param name="set">Writes the selected enumeration value only when the control is enabled.</param>
    public void Choice<T>(string label, T value, Action<T> set) where T : struct, Enum
    {
        var values = Enum.GetValues<T>();
        var control = new ComboBox { ItemsSource = values, SelectedIndex = Array.IndexOf(values, value) };
        Fields.Add((label, control));
        _read.Add(() =>
        {
            if (!control.IsEnabled) { return; }
            if (control.SelectedIndex < 0 || control.SelectedIndex >= values.Length)
            {
                throw new FormatException(string.Format(CultureInfo.CurrentCulture, Resources.Get("ChoiceError"), label));
            }
            set(values[control.SelectedIndex]);
        });
    }
}
