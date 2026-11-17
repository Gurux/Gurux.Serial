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
namespace Gurux.Serial.Blazor;

/// <summary>Stages direct Bluetooth device selection and owns discovery for one settings form.</summary>
public sealed class BluetoothDeviceSelection : IDisposable, IAsyncDisposable
{
    /// <summary>Owns explicit complete-service reads for the currently selected BLE device.</summary>
    private readonly BluetoothGattServiceQuery _gattServices;
    /// <summary>Guards discovery snapshots and cancellation identity.</summary>
    private readonly object _gate = new();
    /// <summary>Stores the private serial settings updated only during commit.</summary>
    private readonly SerialPortSettings _settings;
    /// <summary>Stores the original profile owner independently of later private draft mutations.</summary>
    private readonly string? _savedProfilePort;
    /// <summary>Preserves the original catalogue profile when returning to its saved device.</summary>
    private readonly BluetoothLeProfile? _savedProfile;
    /// <summary>Enumerates operating-system-known devices on the selected native backend.</summary>
    private readonly Func<BluetoothSerialTransport, CancellationToken, Task<BluetoothSerialDevice[]>> _enumerate;
    /// <summary>Runs the cancellable BLE advertisement backend.</summary>
    private readonly Func<Action<BluetoothSerialDevice>, CancellationToken, Task<BluetoothSerialDevice[]>> _scan;
    /// <summary>Tracks the general port choice without applying it to the live media.</summary>
    private readonly SettingsField _port;
    /// <summary>Adds explicitly selected Bluetooth identifiers to the validated port snapshot.</summary>
    private readonly Action<string> _selectPort;
    /// <summary>Maps identities to displayed devices for the selected protocol.</summary>
    private readonly Dictionary<string, BluetoothSerialDevice> _devices = new(StringComparer.OrdinalIgnoreCase);
    /// <summary>Owns cancellation for the current enumeration or scan.</summary>
    private CancellationTokenSource? _discovery;
    /// <summary>Associates the staged profile with exactly one port.</summary>
    private string? _profilePort;
    /// <summary>Stores the saved or recognized profile for the associated port.</summary>
    private BluetoothLeProfile? _profile;
    /// <summary>Prevents operations after the draft closes.</summary>
    private bool _disposed;
    /// <summary>Identifies the protocol used for discovery.</summary>
    private BluetoothSerialTransport _transport;

    /// <summary>Initializes selection from saved settings without starting discovery.</summary>
    /// <param name="settings">The private settings copy.</param>
    /// <param name="port">The general port selector.</param>
    /// <param name="selectPort">Stages an explicitly selected Bluetooth port.</param>
    /// <param name="enumerate">Optional native enumeration implementation; defaults to the transport backend.</param>
    /// <param name="scan">Optional native scan implementation; defaults to the transport backend.</param>
    /// <param name="gattServices">Optional explicit metadata query state supplied by a host.</param>
    internal BluetoothDeviceSelection(SerialPortSettings settings, SettingsField port, Action<string> selectPort,
        Func<BluetoothSerialTransport, CancellationToken, Task<BluetoothSerialDevice[]>>? enumerate = null,
        Func<Action<BluetoothSerialDevice>, CancellationToken, Task<BluetoothSerialDevice[]>>? scan = null,
        BluetoothGattServiceQuery? gattServices = null)
    {
        _gattServices = gattServices ?? new BluetoothGattServiceQuery();
        _gattServices.Changed += OnServicesChanged;
        _settings = settings;
        _savedProfilePort = settings.PortName;
        _savedProfile = settings.PortName?.StartsWith("ble:", StringComparison.OrdinalIgnoreCase) == true ? BluetoothLeProfile.FromSavedProfile(settings.BluetoothProfile) : null;
        _enumerate = enumerate ?? BluetoothSerialTransports.GetDevicesAsync;
        _scan = scan ?? ((found, token) => BluetoothLeDiscovery.ScanAsync(deviceFound: found, cancellationToken: token));
        _port = port;
        _selectPort = selectPort;
        _profilePort = settings.PortName;
        _profile = _savedProfile;
        Channel = settings.BluetoothChannel;
        _transport = settings.PortName?.StartsWith("ble:", StringComparison.OrdinalIgnoreCase) == true
            ? BluetoothSerialTransport.LowEnergy : BluetoothSerialTransport.Classic;
        RememberSelectedDevice();
        if (Transport == BluetoothSerialTransport.LowEnergy && !string.IsNullOrWhiteSpace(SelectedId) && _profile is null)
        {
            Status = string.Format(CultureInfo.CurrentCulture, Properties.Resources.BluetoothProfileMissing, SelectedId);
        }
    }

    /// <summary>Occurs when discovery results or progress change; callbacks may run on a native worker thread.</summary>
    public event Action? Changed;
    /// <summary>Gets or sets the discovery protocol, canceling previous work when it changes.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is not a supported Bluetooth protocol.</exception>
    public BluetoothSerialTransport Transport
    {
        get => _transport;
        set
        {
            if (!Enum.IsDefined(value)) { throw new ArgumentOutOfRangeException(nameof(value)); }
            if (_transport == value) { return; }
            Stop();
            _transport = value;
            lock (_gate) { _devices.Clear(); }
            RememberSelectedDevice();
            Status = null;
        }
    }
    /// <summary>Gets a detached snapshot of devices, including the current saved selection when applicable.</summary>
    public BluetoothSerialDevice[] Devices
    {
        get { lock (_gate) { return _devices.Values.OrderBy(device => device.Name, StringComparer.CurrentCulture).ToArray(); } }
    }
    /// <summary>Gets explicit complete-service query state; selection alone never starts a read.</summary>
    public BluetoothGattServiceQuery GattServices => _gattServices;
    /// <summary>Gets the currently selected native identity for this protocol, or an empty string.</summary>
    public string SelectedId => _port.Value.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase) ? _port.Value[Prefix.Length..] : string.Empty;
    /// <summary>Gets the authoritative catalogue profile only while its owning device remains selected.</summary>
    public BluetoothLeProfile? Profile => Transport == BluetoothSerialTransport.LowEnergy && !string.IsNullOrWhiteSpace(SelectedId)
        && string.Equals(_profilePort, _port.Value, StringComparison.OrdinalIgnoreCase) ? _profile : null;
    /// <summary>Gets whether the selected GATT port has a catalogue profile; Classic and physical ports do not require one.</summary>
    internal bool HasCatalogueSelection => !_port.Value.StartsWith("ble:", StringComparison.OrdinalIgnoreCase) || Profile is not null;
    /// <summary>Gets whether enumeration or scanning is in progress.</summary>
    public bool IsSearching { get { lock (_gate) { return _discovery is not null; } } }
    /// <summary>Gets whether the active operation is a stoppable BLE scan.</summary>
    public bool IsScanning => IsSearching && Transport == BluetoothSerialTransport.LowEnergy;
    /// <summary>Gets whether the host build supports the selected discovery backend.</summary>
    public bool CanFind => Transport != BluetoothSerialTransport.LowEnergy || BluetoothLeDiscovery.IsSupported;
    /// <summary>Gets or sets the legacy staged channel value. Settings commits always select automatic SDP resolution.</summary>
    public int Channel { get; set; } = 1;
    /// <summary>Gets the latest localized discovery feedback.</summary>
    public string? Status { get; private set; }
    /// <summary>Gets the technical port prefix for the selected protocol.</summary>
    private string Prefix => Transport == BluetoothSerialTransport.LowEnergy ? "ble:" : "bluetooth:";

    /// <summary>Formats the complete localized device label with both advertised name and address or native identity.</summary>
    /// <param name="device">The device to display.</param>
    /// <returns>The device name and technical address.</returns>
    public string DisplayName(BluetoothSerialDevice device) => string.Format(CultureInfo.CurrentCulture,
        Properties.Resources.BluetoothDeviceDisplay, device.Name, device.Address ?? device.Id);

    /// <summary>Selects a listed device and stages its recognized profile without opening a connection.</summary>
    /// <param name="identifier">The identity of an item in <see cref="Devices"/>.</param>
    /// <exception cref="ArgumentException">The identity is not in the current device list.</exception>
    public void SelectDevice(string identifier)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        BluetoothSerialDevice device;
        lock (_gate)
        {
            if (!_devices.TryGetValue(identifier, out device!)) { throw new ArgumentException(Properties.Resources.BluetoothDeviceRequired, nameof(identifier)); }
        }
        string port = Prefix + device.Id;
        BluetoothLeProfile? saved = string.Equals(port, _savedProfilePort, StringComparison.OrdinalIgnoreCase) ? _savedProfile
            : string.Equals(port, _profilePort, StringComparison.OrdinalIgnoreCase) ? _profile : null;
        BluetoothLeProfile? catalogue = device.Profile ?? saved;
        if (Transport == BluetoothSerialTransport.LowEnergy && catalogue is null && !device.SupportsSerialPort) { throw new ArgumentException(Properties.Resources.BluetoothDeviceRequired, nameof(identifier)); }
        _profile = Transport == BluetoothSerialTransport.LowEnergy ? catalogue : null;
        _profilePort = port;
        _selectPort(port);
        SynchronizeServiceSelection();
        Stop();
        Status = Transport == BluetoothSerialTransport.LowEnergy && catalogue is null
            ? string.Format(CultureInfo.CurrentCulture, Properties.Resources.BluetoothProfileMissing, device.Id) : null;
        Changed?.Invoke();
    }

    /// <summary>Enumerates known RFCOMM devices or scans BLE advertisements on the application host.</summary>
    /// <returns>The discovery lifetime, including native cleanup.</returns>
    public async Task FindAsync()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_gattServices.IsBusy) { return; }
        if (IsScanning) { Stop(); return; }
        if (IsSearching) { return; }
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        lock (_gate) { _discovery = cancellation; }
        Status = Properties.Resources.BluetoothSearching;
        Changed?.Invoke();
        try
        {
            BluetoothSerialDevice[] devices = Transport == BluetoothSerialTransport.LowEnergy
                ? await _scan(device => AcceptDevice(device, cancellation), cancellation.Token).ConfigureAwait(false)
                : await _enumerate(Transport, cancellation.Token).ConfigureAwait(false);
            devices = devices.Where(device => device.Transport == Transport && device.SupportsSerialPort).ToArray();
            lock (_gate)
            {
                if (!ReferenceEquals(_discovery, cancellation)) { return; }
                _devices.TryGetValue(SelectedId, out var selected);
                _devices.Clear();
                if (selected is not null) { _devices[selected.Id] = selected; }
                foreach (var device in devices) { _devices[device.Id] = device; }
            }
            RememberSelectedDevice();
            Status = devices.Length == 0 ? Properties.Resources.BluetoothNoDevices : null;
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
            if (ReferenceEquals(_discovery, cancellation)) { Status = Properties.Resources.BluetoothDiscoveryCanceled; }
        }
        catch (Exception error)
        {
            if (ReferenceEquals(_discovery, cancellation))
            {
                Status = string.Format(CultureInfo.CurrentCulture, Properties.Resources.BluetoothDiscoveryFailed, error.Message);
            }
        }
        finally
        {
            lock (_gate)
            {
                if (ReferenceEquals(_discovery, cancellation)) { _discovery = null; }
            }
            if (!_disposed) { Changed?.Invoke(); }
        }
    }

    /// <summary>Stops discovery and invalidates callbacks from its previous generation.</summary>
    public void Stop()
    {
        CancellationTokenSource? previous;
        lock (_gate) { previous = _discovery; _discovery = null; }
        try { previous?.Cancel(); }
        catch (ObjectDisposedException) { /* The completed operation already released its cancellation source. */ }
        Status = null;
        if (!_disposed) { Changed?.Invoke(); }
    }

    /// <summary>Preserves a saved selection even when it has not advertised during this scan.</summary>
    private void RememberSelectedDevice()
    {
        string? savedId = _savedProfilePort?.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase) == true
            ? _savedProfilePort[Prefix.Length..] : null;
        lock (_gate)
        {
            if (_devices.TryGetValue(SelectedId, out var discovered) && discovered.Profile is BluetoothLeProfile recognised)
            {
                _profile = recognised;
                _profilePort = _port.Value;
            }
            foreach (string? id in new[] { SelectedId, savedId })
            {
                BluetoothLeProfile? catalogue = string.Equals(Prefix + id, _savedProfilePort, StringComparison.OrdinalIgnoreCase)
                    ? _savedProfile : string.Equals(Prefix + id, _profilePort, StringComparison.OrdinalIgnoreCase) ? _profile : null;
                if (!string.IsNullOrWhiteSpace(id) && (Transport == BluetoothSerialTransport.Classic || catalogue is not null) && !_devices.ContainsKey(id))
                {
                    _devices[id] = new BluetoothSerialDevice(id, id, Transport, id);
                }
            }
        }
        SynchronizeServiceSelection();
    }

    /// <summary>Tracks the current BLE identity for metadata reads and clears services after general port changes.</summary>
    internal void SynchronizeServiceSelection()
    {
        _gattServices.SelectDevice(Transport == BluetoothSerialTransport.LowEnergy && (Profile is not null || Devices.Any(device => device.Id == SelectedId && device.SupportsSerialPort)) ? SelectedId : null);
    }
    /// <summary>Forwards metadata progress to the containing form without persisting it.</summary>
    private void OnServicesChanged()
    {
        if (!_disposed) { Changed?.Invoke(); }
    }
    /// <summary>Accepts a scan result only while its originating scan remains active.</summary>
    /// <param name="device">The newly advertised identity and name.</param>
    /// <param name="cancellation">The scan owning the callback.</param>
    private void AcceptDevice(BluetoothSerialDevice device, CancellationTokenSource cancellation)
    {
        lock (_gate)
        {
            if (_disposed || !ReferenceEquals(_discovery, cancellation) || cancellation.IsCancellationRequested || !device.SupportsSerialPort) { return; }
            _devices[device.Id] = device;
            if (string.Equals(Prefix + device.Id, _port.Value, StringComparison.OrdinalIgnoreCase))
            {
                _profile = device.Profile;
                _profilePort = _port.Value;
            }
        }
        Changed?.Invoke();
    }

    /// <summary>Validates and applies Bluetooth fields to the private settings copy during commit.</summary>
    /// <exception cref="FormatException">The selected GATT identity has no matching catalogue profile.</exception>
    internal void Apply()
    {
        bool lowEnergy = _port.Value.StartsWith("ble:", StringComparison.OrdinalIgnoreCase);
        bool classic = _port.Value.StartsWith("bluetooth:", StringComparison.OrdinalIgnoreCase);
        if (!lowEnergy && !classic) { return; }
        if (classic)
        {
            _settings.BluetoothChannel = 0;
            _settings.BluetoothProfile = null;
            return;
        }
        if (Profile is null || BluetoothLeProfile.FromSavedProfile(Profile) is not BluetoothLeProfile catalogue)
        {
            string id = _port.Value[(lowEnergy ? 4 : 10)..];
            throw new FormatException(string.Format(CultureInfo.CurrentCulture, Properties.Resources.BluetoothProfileMissing, id));
        }
        _settings.BluetoothProfile = catalogue;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _disposed = true;
        _gattServices.Changed -= OnServicesChanged;
        _gattServices.Dispose();
        Stop();
        Changed = null;
    }
    /// <summary>Cancels discovery and awaits the metadata session cleanup before another transport opens.</summary>
    /// <returns>Completion after native metadata cleanup.</returns>
    public async ValueTask DisposeAsync()
    {
        Dispose();
        await _gattServices.DisposeAsync();
    }
}
