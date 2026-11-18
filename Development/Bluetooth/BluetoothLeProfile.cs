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
namespace Gurux.Serial;

/// <summary>Describes a Bluetooth LE serial service and its notification and output characteristics.</summary>
/// <remarks>Serial framing is device-specific; supply configuration bytes or register a command generator when required.</remarks>
public sealed class BluetoothLeProfile
{
    /// <summary>Gets the service containing the serial characteristics.</summary>
    public Guid Service { get; }
    /// <summary>Gets the characteristic publishing received bytes.</summary>
    public Guid ReadCharacteristic { get; }
    /// <summary>Gets the characteristic accepting outgoing bytes.</summary>
    public Guid WriteCharacteristic { get; }
    /// <summary>Gets the optional configuration characteristic.</summary>
    public Guid? ConfigurationCharacteristic { get; }
    /// <summary>Gets the registered command generator name used to derive UART commands when opening.</summary>
    /// <remarks>A configuration characteristic is required. Applications must register this name before opening the device. When present, this provider takes precedence over the UUID default and static configuration bytes.</remarks>
    public string? ConfigurationCommandProvider { get; init; }
    /// <summary>Gets whether the configuration characteristic has a registered generator for the selected UART settings.</summary>
    /// <remarks>An explicit provider overrides the UUID default. Static configuration bytes alone do not enable UART editing.</remarks>
    [System.Text.Json.Serialization.JsonIgnore]
    public bool CanConfigureUart => BluetoothConfigurationCommands.CanConfigure(ConfigurationCharacteristic, ConfigurationCommandProvider);
    /// <summary>Stores an independent copy of the configuration payload.</summary>
    private readonly byte[]? _configurationValue;
    /// <summary>Gets a copy of the optional device-specific configuration bytes.</summary>
    public byte[]? ConfigurationValue => _configurationValue?.ToArray();
    /// <summary>Initializes a service profile with optional device-specific configuration.</summary>
    /// <param name="service">Serial service UUID.</param>
    /// <param name="readCharacteristic">Notification or indication characteristic UUID.</param>
    /// <param name="writeCharacteristic">Output characteristic UUID.</param>
    /// <param name="configurationCharacteristic">Optional configuration characteristic UUID.</param>
    /// <param name="configurationValue">Optional configuration bytes copied by this constructor.</param>
    /// <exception cref="ArgumentException">A required UUID is empty or configuration bytes have no destination characteristic.</exception>
    public BluetoothLeProfile(Guid service, Guid readCharacteristic, Guid writeCharacteristic, Guid? configurationCharacteristic = null, byte[]? configurationValue = null)
    {
        if (service == Guid.Empty || readCharacteristic == Guid.Empty || writeCharacteristic == Guid.Empty || configurationCharacteristic == Guid.Empty)
        {
            throw new ArgumentException(global::Gurux.Serial.Properties.Resources.Get("BluetoothLeProfileInvalid"));
        }
        if (configurationValue != null && configurationCharacteristic == null)
        {
            throw new ArgumentException(global::Gurux.Serial.Properties.Resources.Get("BluetoothLeConfigurationRequiresCharacteristic"), nameof(configurationCharacteristic));
        }
        Service = service;
        ReadCharacteristic = readCharacteristic;
        WriteCharacteristic = writeCharacteristic;
        ConfigurationCharacteristic = configurationCharacteristic;
        this._configurationValue = configurationValue?.ToArray();
    }
    /// <summary>Builds commands using the explicit provider, then the configuration UUID default, or returns the static payload.</summary>
    /// <param name="settings">The settings to supply to a registered command generator.</param>
    /// <returns>A copy of explicit configuration bytes, generated UTF-8 command bytes, or null when no configuration is supplied.</returns>
    /// <exception cref="ArgumentNullException">The settings are null.</exception>
    /// <exception cref="ArgumentException">Commands have no configuration characteristic or exceed the supported GATT value size.</exception>
    /// <exception cref="NotSupportedException">The command generator name has not been registered by the application.</exception>
    /// <exception cref="InvalidOperationException">The registered provider does not return a generator or command sequence.</exception>
    public byte[]? GetConfiguration(SerialPortSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (string.IsNullOrWhiteSpace(ConfigurationCommandProvider))
        {
            return ConfigurationCharacteristic.HasValue
                ? BluetoothConfigurationCommands.Create(ConfigurationCharacteristic.Value, settings) ?? ConfigurationValue
                : ConfigurationValue;
        }
        if (!ConfigurationCharacteristic.HasValue)
        {
            throw new ArgumentException(Properties.Resources.Get("BluetoothLeConfigurationRequiresCharacteristic"));
        }
        return BluetoothConfigurationCommands.Create(ConfigurationCommandProvider, settings);
    }

    /// <summary>Creates the opening profile with its device-specific commands evaluated for this connection.</summary>
    /// <param name="settings">The serial settings selected for the device.</param>
    /// <returns>A profile containing the configuration bytes to write during native opening.</returns>
    internal BluetoothLeProfile ForSettings(SerialPortSettings settings) =>
        new(Service, ReadCharacteristic, WriteCharacteristic, ConfigurationCharacteristic, GetConfiguration(settings))
        {
            ConfigurationCommandProvider = ConfigurationCommandProvider
        };

    /// <summary>Finds a device profile in the embedded Gurux Bluetooth device catalogue.</summary>
    /// <param name="deviceName">Advertised device name; matching ignores case and supports a trailing wildcard in catalogue names.</param>
    /// <returns>The matching service, read, write and optional configuration UUIDs, or null for an unknown name.</returns>
    /// <exception cref="InvalidDataException">A matching catalogue row is malformed or the embedded catalogue is missing.</exception>
    public static BluetoothLeProfile? FromDeviceName(string? deviceName)
    {
        if (string.IsNullOrWhiteSpace(deviceName))
        {
            return null;
        }
        using Stream stream = typeof(BluetoothLeProfile).Assembly.GetManifestResourceStream("Gurux.Serial.BluetoothDevices.csv")
            ?? throw new InvalidDataException(Properties.Resources.Get("BluetoothLeProfileInvalid"));
        using var reader = new StreamReader(stream);
        return ReadCatalogue(deviceName, reader);
    }

    /// <summary>Restores an identifier-only saved selection only when its service and characteristic UUIDs match a catalogue row.</summary>
    /// <param name="savedProfile">The previously saved profile, or null when no profile was saved.</param>
    /// <returns>The authoritative catalogue profile, including its configuration defaults, or null when no row matches.</returns>
    /// <remarks>Saved configuration payloads and provider names are not copied into the result. This permits reopening catalogue devices without a fresh name advertisement while rejecting arbitrary custom UUID profiles in selection editors.</remarks>
    /// <exception cref="InvalidDataException">The embedded catalogue is missing or contains an invalid row.</exception>
    public static BluetoothLeProfile? FromSavedProfile(BluetoothLeProfile? savedProfile)
    {
        if (savedProfile is null)
        {
            return null;
        }
        using Stream stream = typeof(BluetoothLeProfile).Assembly.GetManifestResourceStream("Gurux.Serial.BluetoothDevices.csv")
            ?? throw new InvalidDataException(Properties.Resources.Get("BluetoothLeProfileInvalid"));
        using var reader = new StreamReader(stream);
        while (reader.ReadLine() is string row)
        {
            if (string.IsNullOrWhiteSpace(row) || row.TrimStart().StartsWith('#'))
            {
                continue;
            }
            string name = row.Split(';')[0].Trim();
            if (name.EndsWith('*')) { name = name[..^1] + "saved-device"; }
            BluetoothLeProfile? candidate = ReadCatalogue(name, new StringReader(row));
            if (candidate is not null && candidate.Service == savedProfile.Service
                && candidate.ReadCharacteristic == savedProfile.ReadCharacteristic
                && candidate.WriteCharacteristic == savedProfile.WriteCharacteristic
                && candidate.ConfigurationCharacteristic == savedProfile.ConfigurationCharacteristic)
            {
                return candidate;
            }
        }
        return null;
    }
    /// <summary>Matches a device against a semicolon-separated catalogue without taking ownership of its reader.</summary>
    /// <param name="deviceName">The advertised name to match.</param>
    /// <param name="reader">The catalogue containing service, characteristic and optional provider columns.</param>
    /// <returns>The matching profile or null for an unknown or blank device name.</returns>
    /// <exception cref="ArgumentNullException">The reader is null.</exception>
    /// <exception cref="InvalidDataException">A matching catalogue row contains invalid UUIDs, configuration bytes or column counts.</exception>
    internal static BluetoothLeProfile? ReadCatalogue(string? deviceName, TextReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);
        if (string.IsNullOrWhiteSpace(deviceName)) { return null; }
        while (reader.ReadLine() is string row)
        {
            if (string.IsNullOrWhiteSpace(row) || row.TrimStart().StartsWith('#'))
            {
                continue;
            }
            string[] cells = row.Split(';');
            string name = cells[0].Trim();
            bool wildcard = name.EndsWith('*');
            bool matches = wildcard
                ? deviceName.StartsWith(name[..^1], StringComparison.OrdinalIgnoreCase)
                : string.Equals(name, deviceName, StringComparison.OrdinalIgnoreCase);
            if (!matches)
            {
                continue;
            }
            if (cells.Length < 4 || cells.Length > 7)
            {
                throw new InvalidDataException(Properties.Resources.Get("BluetoothLeProfileInvalid"));
            }
            if (!Guid.TryParse(cells[1].Trim(), out Guid service) || !Guid.TryParse(cells[2].Trim(), out Guid read)
                || !Guid.TryParse(cells[3].Trim(), out Guid write) || service == Guid.Empty || read == Guid.Empty || write == Guid.Empty)
            {
                throw new InvalidDataException(Properties.Resources.Get("BluetoothLeProfileInvalid"));
            }
            Guid? configuration = null;
            if (cells.Length >= 5 && !string.IsNullOrWhiteSpace(cells[4]))
            {
                if (!Guid.TryParse(cells[4].Trim(), out Guid value) || value == Guid.Empty)
                {
                    throw new InvalidDataException(Properties.Resources.Get("BluetoothLeProfileInvalid"));
                }
                configuration = value;
            }
            if (cells.Length >= 6 && !string.IsNullOrWhiteSpace(cells[5]) && !configuration.HasValue)
            {
                throw new InvalidDataException(Properties.Resources.Get("BluetoothLeProfileInvalid"));
            }
            byte[]? payload = null;
            if (cells.Length == 7 && !string.IsNullOrWhiteSpace(cells[6]))
            {
                try
                {
                    payload = Convert.FromHexString(cells[6].Trim());
                }
                catch (FormatException)
                {
                    throw new InvalidDataException(Properties.Resources.Get("BluetoothLeProfileInvalid"));
                }
                if (!configuration.HasValue || payload.Length > 512)
                {
                    throw new InvalidDataException(Properties.Resources.Get("BluetoothLeProfileInvalid"));
                }
            }
            return new BluetoothLeProfile(service, read, write, configuration, payload)
            {
                ConfigurationCommandProvider = cells.Length >= 6 && !string.IsNullOrWhiteSpace(cells[5]) ? cells[5].Trim() : null
            };
        }
        return null;
    }
}
