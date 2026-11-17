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

/// <summary>Describes a discovered native Bluetooth device and its advertised or cached service metadata without owning its connection.</summary>
public sealed class BluetoothSerialDevice
{
    /// <summary>Identifies the standard Bluetooth Classic Serial Port service.</summary>
    public static readonly Guid SerialPortServiceUuid = new("00001101-0000-1000-8000-00805f9b34fb");
    /// <summary>Gets an immutable snapshot of advertised or operating-system-known service UUIDs.</summary>
    public IReadOnlyList<Guid> ServiceUuids { get; }
    /// <summary>Gets the native device identifier accepted by the selected transport factory.</summary>
    public string Id { get; }
    /// <summary>Gets the device name supplied by the operating system.</summary>
    public string Name { get; }
    /// <summary>Gets the Bluetooth MAC address when supplied by the operating system; otherwise, null.</summary>
    public string? Address { get; }
    /// <summary>Gets the shared catalogue GATT profile matched against the device name, or null when the name is unknown.</summary>
    /// <remarks>Windows, Android, iOS and Linux use the same embedded catalogue. Native identifiers and MAC addresses are not used as profile names.</remarks>
    /// <exception cref="InvalidDataException">A matching catalogue entry is malformed or the embedded catalogue is missing.</exception>
    public BluetoothLeProfile? Profile => BluetoothLeProfile.FromDeviceName(Name);
    /// <summary>Gets whether service metadata advertises Serial Port support, or a Low Energy device has a catalogue GATT profile.</summary>
    public bool SupportsSerialPort => Transport == BluetoothSerialTransport.Classic
        ? ServiceUuids.Contains(SerialPortServiceUuid) : Profile is not null || ServiceUuids.Contains(SerialPortServiceUuid);
    /// <summary>Gets the protocol used to open this device.</summary>
    public BluetoothSerialTransport Transport { get; }
    /// <summary>Initializes an immutable Bluetooth discovery result.</summary>
    /// <param name="id">The native device identifier.</param>
    /// <param name="name">The operating system device name.</param>
    /// <param name="transport">The connection protocol.</param>
    public BluetoothSerialDevice(string id, string name, BluetoothSerialTransport transport)
        : this(id, name, transport, null)
    {
    }
    /// <summary>Initializes a Bluetooth discovery result with separately supplied address metadata.</summary>
    /// <param name="id">The native device identifier.</param>
    /// <param name="name">The operating system device name.</param>
    /// <param name="transport">The connection protocol.</param>
    /// <param name="address">The Bluetooth MAC address, or null when address metadata is unavailable.</param>
    public BluetoothSerialDevice(string id, string name, BluetoothSerialTransport transport, string? address)
        : this(id, name, transport, address, null)
    {
    }
    /// <summary>Initializes device metadata with an independent immutable service UUID snapshot.</summary>
    /// <param name="id">The native identity accepted by the selected transport.</param>
    /// <param name="name">The advertised or operating-system device name.</param>
    /// <param name="transport">The protocol used to open this device.</param>
    /// <param name="address">The Bluetooth MAC address when available.</param>
    /// <param name="serviceUuids">Advertised or cached services; null means no service evidence is available.</param>
    public BluetoothSerialDevice(string id, string name, BluetoothSerialTransport transport, string? address, IEnumerable<Guid>? serviceUuids)
    {
        ServiceUuids = Array.AsReadOnly((serviceUuids ?? []).Where(uuid => uuid != Guid.Empty).Distinct().OrderBy(uuid => uuid).ToArray());
        Id = id;
        Name = name;
        Transport = transport;
        Address = address;
    }
    /// <summary>Combines fragmented advertisements without discarding known names or serial service evidence.</summary>
    /// <param name="update">Later metadata for the same native identity.</param>
    /// <returns>A new immutable device description containing the accumulated service UUIDs.</returns>
    internal BluetoothSerialDevice Merge(BluetoothSerialDevice update)
    {
        bool unnamed = string.IsNullOrWhiteSpace(update.Name)
            || string.Equals(update.Name, update.Id, StringComparison.OrdinalIgnoreCase)
            || string.Equals(update.Name, update.Address, StringComparison.OrdinalIgnoreCase);
        return new BluetoothSerialDevice(Id, unnamed ? Name : update.Name, Transport,
            update.Address ?? Address, ServiceUuids.Concat(update.ServiceUuids));
    }
}
