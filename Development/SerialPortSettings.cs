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

using System.IO.Ports;

namespace Gurux.Serial;
/// <summary>Stores portable framing, flow control and device selection used when opening a serial transport.</summary>
public sealed class SerialPortSettings
{
    /// <summary>Gets or sets the operating system port name or platform accessory identifier.</summary>
    /// <remarks>Desktop bluetooth: identifiers open RFCOMM; ble: identifiers use <see cref="BluetoothProfile"/>.</remarks>
    public string? PortName { get; set; }
    /// <summary>Gets or sets the Bluetooth Classic channel: zero resolves the Serial Port service through SDP, while one through thirty select an explicit channel.</summary>
    /// <remarks>This setting is used only for desktop identifiers beginning with bluetooth:.</remarks>
    public int BluetoothChannel { get; set; }
    /// <summary>Gets or sets the GATT service and characteristics used by desktop identifiers beginning with ble:.</summary>
    public BluetoothLeProfile? BluetoothProfile { get; set; }
    /// <summary>Gets or sets the requested transfer rate in bits per second.</summary>
    public int BaudRate { get; set; } = 9600;
    /// <summary>Gets or sets the number of payload bits in each serial frame.</summary>
    public int DataBits { get; set; } = 8;
    /// <summary>Gets or sets the parity check used for each serial frame.</summary>
    public Parity Parity { get; set; }
    /// <summary>Gets or sets the stop interval terminating each serial frame.</summary>
    public StopBits StopBits { get; set; } = StopBits.One;
    /// <summary>Gets or sets the hardware or software flow control policy.</summary>
    public Handshake Handshake { get; set; }
    /// <summary>Gets or sets whether the data terminal ready signal is asserted. Defaults to true for optical-head and legacy example compatibility.</summary>
    /// <remarks>Set to false explicitly when the connected device requires the signal to remain inactive.</remarks>
    public bool DtrEnable { get; set; } = true;
    /// <summary>Gets or sets whether the request to send signal is asserted. Defaults to true for optical-head and legacy example compatibility.</summary>
    /// <remarks>Set to false explicitly when the connected device requires the signal to remain inactive.</remarks>
    public bool RtsEnable { get; set; } = true;

    /// <summary>Collects invalid device selection and framing settings without opening or changing the transport.</summary>
    /// <returns>All localized configuration errors, or an empty list when the settings are valid.</returns>
    internal IReadOnlyList<Gurux.Common.GXValidationError> GetValidationErrors()
    {
        var errors = new List<Gurux.Common.GXValidationError>();
        if (string.IsNullOrWhiteSpace(PortName))
        {
            errors.Add(new(nameof(PortName), "Required", Properties.Resources.Get("APortOrAccessoryNameIsRequired")));
        }
        if (BaudRate <= 0)
        {
            errors.Add(new(nameof(BaudRate), "OutOfRange", Properties.Resources.Get("BaudRateMustBePositive")));
        }
        if (PortName?.StartsWith("bluetooth:", StringComparison.OrdinalIgnoreCase) == true && BluetoothChannel is < 0 or > 30)
        {
            errors.Add(new(nameof(BluetoothChannel), "OutOfRange", Properties.Resources.Get("BluetoothChannelInvalid")));
        }
        if (PortName?.StartsWith("ble:", StringComparison.OrdinalIgnoreCase) == true && BluetoothProfile == null)
        {
            errors.Add(new(nameof(BluetoothProfile), "Required", Properties.Resources.Get("BluetoothLeProfileRequired")));
        }
        if (DataBits is < 5 or > 8)
        {
            errors.Add(new(nameof(DataBits), "OutOfRange", Properties.Resources.Get("DataBitsMustBeBetweenFiveAndEight")));
        }
        if (!Enum.IsDefined(Parity))
        {
            errors.Add(new(nameof(Parity), "InvalidValue", Properties.Resources.Get("InvalidSerialFraming")));
        }
        if (!Enum.IsDefined(StopBits) || StopBits == StopBits.None)
        {
            errors.Add(new(nameof(StopBits), "InvalidValue", Properties.Resources.Get("InvalidSerialFraming")));
        }
        if (!Enum.IsDefined(Handshake))
        {
            errors.Add(new(nameof(Handshake), "InvalidValue", Properties.Resources.Get("InvalidSerialFraming")));
        }
        return errors;
    }

    /// <summary>Checks that the configured device and framing values can be used by the transport.</summary>
    /// <exception cref="ArgumentException">The device name is blank, a framing enumeration is invalid, or a BLE identifier has no profile.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The baud rate, data bit count or Bluetooth Classic channel is outside its allowed range.</exception>
    public void Validate()
    {
        var errors = GetValidationErrors();
        if (errors.Count == 0)
        {
            return;
        }
        var error = errors[0];
        if (error.PropertyName == nameof(BaudRate) || error.PropertyName == nameof(DataBits))
        {
            throw new ArgumentOutOfRangeException(error.PropertyName);
        }
        if (error.PropertyName == nameof(BluetoothChannel))
        {
            throw new ArgumentOutOfRangeException(error.PropertyName, error.Message);
        }
        throw new ArgumentException(error.Message);
    }
}
