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

using System.Collections.Concurrent;
using System.Globalization;
using System.Text;

namespace Gurux.Serial;

/// <summary>Registers application-owned command generators without loading arbitrary types from saved configuration.</summary>
/// <remarks>Use a stable registration name in the device catalogue or BluetoothLeProfile.ConfigurationCommandProvider.
/// A configuration characteristic UUID can also select a default generator. A named profile provider takes precedence.
/// Register a factory during application startup; no generator is instantiated while a settings editor is open.</remarks>
public static class BluetoothConfigurationCommands
{
    /// <summary>Maps stable profile names to explicitly registered, AOT-compatible factories.</summary>
    private static readonly ConcurrentDictionary<string, Func<IBluetoothConfigurationCommands>> providers = new(StringComparer.Ordinal);

    /// <summary>Maps configuration characteristic UUIDs to application-owned default generators.</summary>
    private static readonly ConcurrentDictionary<Guid, Func<IBluetoothConfigurationCommands>> characteristicProviders = new();

    /// <summary>Registers the default generator for a catalogue configuration characteristic UUID.</summary>
    /// <param name="configurationCharacteristic">The nonempty ConfigurationService UUID from the device catalogue.</param>
    /// <param name="factory">Creates the generator when opening a device; registration does not invoke it.</param>
    /// <remarks>Register before displaying settings or opening connections. Registering the same UUID replaces its factory.
    /// A profile's explicitly named provider takes precedence over this default.</remarks>
    /// <exception cref="ArgumentException">The characteristic UUID is empty.</exception>
    /// <exception cref="ArgumentNullException">The factory is null.</exception>
    public static void Register(Guid configurationCharacteristic, Func<IBluetoothConfigurationCommands> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        if (configurationCharacteristic == Guid.Empty)
        {
            throw new ArgumentException(Properties.Resources.Get("BluetoothLeProfileInvalid"), nameof(configurationCharacteristic));
        }
        characteristicProviders[configurationCharacteristic] = factory;
    }

    /// <summary>Checks whether a configuration destination has a registered UART command generator without invoking its factory.</summary>
    /// <param name="configurationCharacteristic">The optional configuration characteristic UUID.</param>
    /// <param name="provider">The optional explicit provider name, which overrides UUID lookup.</param>
    /// <returns>True when the destination is nonempty and the selected generator is registered.</returns>
    public static bool CanConfigure(Guid? configurationCharacteristic, string? provider = null) =>
        configurationCharacteristic.HasValue && configurationCharacteristic.Value != Guid.Empty
        && (string.IsNullOrWhiteSpace(provider)
            ? characteristicProviders.ContainsKey(configurationCharacteristic.Value)
            : providers.ContainsKey(provider));

    /// <summary>Registers a generator with a public parameterless constructor under its full type name.</summary>
    /// <typeparam name="T">The device-specific command generator to instantiate when opening.</typeparam>
    /// <returns>The stable registration name to store in the device profile.</returns>
    public static string Register<T>() where T : IBluetoothConfigurationCommands, new() =>
        Register(typeof(T), () => new T());

    /// <summary>Registers an explicitly supplied generator factory under a stable name.</summary>
    /// <param name="commandType">The concrete generator type, such as typeof(GXAtCommand).</param>
    /// <param name="factory">Creates a generator when the device is opened.</param>
    /// <param name="name">An optional catalogue name; defaults to the full type name.</param>
    /// <returns>The stable registration name to assign to a BLE device profile.</returns>
    /// <exception cref="ArgumentNullException">The type or factory is null.</exception>
    /// <exception cref="ArgumentException">The type does not implement the generator interface, or its registration name is blank.</exception>
    public static string Register(Type commandType, Func<IBluetoothConfigurationCommands> factory, string? name = null)
    {
        ArgumentNullException.ThrowIfNull(commandType);
        ArgumentNullException.ThrowIfNull(factory);
        if (!typeof(IBluetoothConfigurationCommands).IsAssignableFrom(commandType))
        {
            throw new ArgumentException(Properties.Resources.Get("BluetoothConfigurationProviderInvalid"), nameof(commandType));
        }
        string key = name ?? commandType.FullName ?? commandType.Name;
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        providers[key] = factory;
        return key;
    }

    /// <summary>Formats and encodes the commands returned by an explicitly registered provider.</summary>
    /// <param name="name">The registered provider name stored in the profile.</param>
    /// <param name="settings">The current settings supplied to the provider.</param>
    /// <returns>UTF-8 bytes preserving the provider's exact command separators and line endings.</returns>
    /// <exception cref="NotSupportedException">The application has not registered the requested provider.</exception>
    /// <exception cref="InvalidOperationException">A provider returns no generator or no command sequence.</exception>
    /// <exception cref="ArgumentException">The generated configuration exceeds the supported GATT value size.</exception>
    internal static byte[] Create(string name, SerialPortSettings settings)
    {
        if (!providers.TryGetValue(name, out var factory))
        {
            throw new NotSupportedException(string.Format(CultureInfo.CurrentCulture,
                Properties.Resources.Get("BluetoothConfigurationProviderNotRegistered"), name));
        }
        return Encode(factory, settings);
    }

    /// <summary>Generates default commands for a registered configuration UUID.</summary>
    /// <param name="characteristic">The configuration destination UUID.</param>
    /// <param name="settings">The selected UART settings.</param>
    /// <returns>Generated bytes, or null if no default is registered.</returns>
    /// <exception cref="InvalidOperationException">The factory or generator returns null.</exception>
    /// <exception cref="ArgumentException">The encoded commands exceed 512 bytes.</exception>
    internal static byte[]? Create(Guid characteristic, SerialPortSettings settings) =>
        characteristicProviders.TryGetValue(characteristic, out var factory) ? Encode(factory, settings) : null;

    /// <summary>Invokes a selected factory and validates its exact UTF-8 command payload.</summary>
    /// <param name="factory">The registered application factory.</param>
    /// <param name="settings">The selected UART settings passed unchanged to the generator.</param>
    /// <returns>The encoded command sequence, limited to one GATT value.</returns>
    /// <exception cref="InvalidOperationException">The factory or generator returns null.</exception>
    /// <exception cref="ArgumentException">The encoded commands exceed 512 bytes.</exception>
    private static byte[] Encode(Func<IBluetoothConfigurationCommands> factory, SerialPortSettings settings)
    {
        IBluetoothConfigurationCommands generator = factory()
            ?? throw new InvalidOperationException(Properties.Resources.Get("BluetoothConfigurationProviderInvalid"));
        string commands = generator.GetCommands(settings)
            ?? throw new InvalidOperationException(Properties.Resources.Get("BluetoothConfigurationProviderInvalid"));
        byte[] bytes = Encoding.UTF8.GetBytes(commands);
        if (bytes.Length > 512)
        {
            throw new ArgumentException(Properties.Resources.Get("BluetoothConfigurationCommandsTooLarge"));
        }
        return bytes;
    }
}
