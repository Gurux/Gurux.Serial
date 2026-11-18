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
using System.Resources;

namespace Gurux.Serial.Maui.Properties;

/// <summary>Resolves localized native settings labels and messages.</summary>
internal static class Resources
{
    /// <summary>Gets the localized title for standard serial port and framing settings.</summary>
    internal static string GeneralSettings => _manager.GetString(nameof(GeneralSettings), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized title for Bluetooth discovery and profile settings.</summary>
    internal static string BluetoothSettings => _manager.GetString(nameof(BluetoothSettings), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized accessible description identifying the selected settings tab.</summary>
    internal static string SelectedTabDescription => _manager.GetString(nameof(SelectedTabDescription), CultureInfo.CurrentUICulture)!;
    /// <summary>Reads this assembly's settings resources.</summary>
    private static readonly ResourceManager _manager = new("Gurux.Serial.Maui.Properties.Resources", typeof(Resources).Assembly);
    /// <summary>Gets the localized text: Media settings.</summary>
    internal static string Title => _manager.GetString(nameof(Title), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized text: Save.</summary>
    internal static string Save => _manager.GetString(nameof(Save), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized text: Cancel.</summary>
    internal static string Cancel => _manager.GetString(nameof(Cancel), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized text: This editor is already showing settings..</summary>
    internal static string Busy => _manager.GetString(nameof(Busy), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized text: Close the media before editing settings..</summary>
    internal static string Open => _manager.GetString(nameof(Open), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized text: The media settings changed while this form was open. Reopen the form..</summary>
    internal static string Changed => _manager.GetString(nameof(Changed), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized text: This editor does not support the supplied media type..</summary>
    internal static string WrongMedia => _manager.GetString(nameof(WrongMedia), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized text: The settings owner must be a ContentPage or a NavigationPage showing a ContentPage..</summary>
    internal static string Owner => _manager.GetString(nameof(Owner), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized text: The parent must be a MauiMediaWindow from this settings library..</summary>
    internal static string Parent => _manager.GetString(nameof(Parent), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized text: The MAUI window must have a page..</summary>
    internal static string WindowPage => _manager.GetString(nameof(WindowPage), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized text: {0} must be a whole number..</summary>
    internal static string WholeNumber => _manager.GetString(nameof(WholeNumber), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized text: Select a value for {0}..</summary>
    internal static string SelectValue => _manager.GetString(nameof(SelectValue), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized text: The settings could not be saved. Check the values and try again..</summary>
    internal static string Invalid => _manager.GetString(nameof(Invalid), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized text: Port name.</summary>
    internal static string PortName => _manager.GetString(nameof(PortName), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized text: Baud rate.</summary>
    internal static string BaudRate => _manager.GetString(nameof(BaudRate), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized text: Data bits.</summary>
    internal static string DataBits => _manager.GetString(nameof(DataBits), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized text: Parity.</summary>
    internal static string Parity => _manager.GetString(nameof(Parity), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized text: Stop bits.</summary>
    internal static string StopBits => _manager.GetString(nameof(StopBits), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized text: Handshake.</summary>
    internal static string Handshake => _manager.GetString(nameof(Handshake), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized text: DTR enabled.</summary>
    internal static string Dtr => _manager.GetString(nameof(Dtr), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized text: RTS enabled.</summary>
    internal static string Rts => _manager.GetString(nameof(Rts), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized text: Phone number.</summary>
    internal static string Phone => _manager.GetString(nameof(Phone), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized text: Initialization commands (one per line).</summary>
    internal static string Commands => _manager.GetString(nameof(Commands), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized text: Connection timeout (seconds).</summary>
    internal static string Timeout => _manager.GetString(nameof(Timeout), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized text: SIM PIN.</summary>
    internal static string Pin => _manager.GetString(nameof(Pin), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized text: Message encoding.</summary>
    internal static string Encoding => _manager.GetString(nameof(Encoding), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized text: Protocol.</summary>
    internal static string Protocol => _manager.GetString(nameof(Protocol), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized text: Host name.</summary>
    internal static string Host => _manager.GetString(nameof(Host), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized text: Port.</summary>
    internal static string Port => _manager.GetString(nameof(Port), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized text: Server.</summary>
    internal static string Server => _manager.GetString(nameof(Server), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized text: IPv6.</summary>
    internal static string IPv6 => _manager.GetString(nameof(IPv6), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized text: Maximum clients.</summary>
    internal static string Clients => _manager.GetString(nameof(Clients), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized text: Wait time (milliseconds).</summary>
    internal static string Wait => _manager.GetString(nameof(Wait), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized text: TCP keepalive.</summary>
    internal static string KeepAlive => _manager.GetString(nameof(KeepAlive), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized text: TCP keepalive idle (seconds).</summary>
    internal static string Idle => _manager.GetString(nameof(Idle), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized text: TCP keepalive interval (seconds).</summary>
    internal static string Interval => _manager.GetString(nameof(Interval), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized text: TCP keepalive retries.</summary>
    internal static string Retries => _manager.GetString(nameof(Retries), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the message shown when Bluetooth connection access is denied.</summary>
    internal static string BluetoothPermissionDenied => _manager.GetString(nameof(BluetoothPermissionDenied), System.Globalization.CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized Bluetooth input text: Bluetooth Classic (RFCOMM)</summary>
    internal static string BluetoothClassic => _manager.GetString(nameof(BluetoothClassic), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized Bluetooth input text: Bluetooth Low Energy (GATT)</summary>
    internal static string BluetoothLowEnergy => _manager.GetString(nameof(BluetoothLowEnergy), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized Bluetooth input text: Bluetooth transport</summary>
    internal static string BluetoothTransport => _manager.GetString(nameof(BluetoothTransport), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the caption for discovered and saved Bluetooth device choices.</summary>
    internal static string BluetoothDevices => _manager.GetString(nameof(BluetoothDevices), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized Bluetooth input text: Bluetooth address or native device identifier</summary>
    internal static string BluetoothIdentifier => _manager.GetString(nameof(BluetoothIdentifier), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized Bluetooth input text: RFCOMM channel</summary>
    internal static string BluetoothChannel => _manager.GetString(nameof(BluetoothChannel), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized Bluetooth input text: BLE service UUID</summary>
    internal static string BluetoothService => _manager.GetString(nameof(BluetoothService), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized Bluetooth input text: BLE notification characteristic UUID</summary>
    internal static string BluetoothRead => _manager.GetString(nameof(BluetoothRead), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized Bluetooth input text: BLE write characteristic UUID</summary>
    internal static string BluetoothWrite => _manager.GetString(nameof(BluetoothWrite), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized Bluetooth input text: BLE configuration characteristic UUID (optional)</summary>
    internal static string BluetoothConfiguration => _manager.GetString(nameof(BluetoothConfiguration), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the message shown when discovery finds no supported Bluetooth serial devices.</summary>
    internal static string BluetoothNoDevices => _manager.GetString(nameof(BluetoothNoDevices), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized Bluetooth input text: Bluetooth devices could not be refreshed. Check Windows Bluetooth settings and try again.</summary>
    internal static string BluetoothRefreshFailed => _manager.GetString(nameof(BluetoothRefreshFailed), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized Bluetooth input text: The RFCOMM channel must be a whole number between 1 and 30.</summary>
    internal static string BluetoothChannelInvalid => _manager.GetString(nameof(BluetoothChannelInvalid), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the message for an invalid six-byte Classic Bluetooth address.</summary>
    internal static string BluetoothAddressInvalid => _manager.GetString(nameof(BluetoothAddressInvalid), CultureInfo.CurrentUICulture)!;


    /// <summary>Gets the localized scan ble devices text.</summary>
    internal static string ScanBleDevices => _manager.GetString(nameof(ScanBleDevices), CultureInfo.CurrentUICulture)!;

    /// <summary>Gets the caption for stopping the current explicit Bluetooth search.</summary>
    internal static string StopBleScan => _manager.GetString(nameof(StopBleScan), CultureInfo.CurrentUICulture)!;

    /// <summary>Gets the localized ble scanning text.</summary>
    internal static string BleScanning => _manager.GetString(nameof(BleScanning), CultureInfo.CurrentUICulture)!;

    /// <summary>Gets the localized ble scan complete text.</summary>
    internal static string BleScanComplete => _manager.GetString(nameof(BleScanComplete), CultureInfo.CurrentUICulture)!;

    /// <summary>Gets the localized ble scan no devices text.</summary>
    internal static string BleScanNoDevices => _manager.GetString(nameof(BleScanNoDevices), CultureInfo.CurrentUICulture)!;

    /// <summary>Gets the localized ble device display text.</summary>
    internal static string BleDeviceDisplay => _manager.GetString(nameof(BleDeviceDisplay), CultureInfo.CurrentUICulture)!;

    /// <summary>Gets the localized ble scan failed text.</summary>
    internal static string BleScanFailed => _manager.GetString(nameof(BleScanFailed), CultureInfo.CurrentUICulture)!;

    /// <summary>Gets the localized ble scan permission denied text.</summary>
    internal static string BleScanPermissionDenied => _manager.GetString(nameof(BleScanPermissionDenied), CultureInfo.CurrentUICulture)!;

    /// <summary>Gets the localized save failure message with a placeholder for the actual reason.</summary>
    internal static string InvalidDetails => _manager.GetString(nameof(InvalidDetails), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the shared Bluetooth discovery button caption.</summary>
    internal static string FindDevices => _manager.GetString(nameof(FindDevices), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the explanation of serial UUID and device catalogue filtering for known devices.</summary>
    internal static string FindKnownDevicesHint => _manager.GetString(nameof(FindKnownDevicesHint), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the explanation of serial UUID and device catalogue filtering for nearby BLE devices.</summary>
    internal static string FindBleDevicesHint => _manager.GetString(nameof(FindBleDevicesHint), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the validation message identifying a device without a recognized GATT serial profile.</summary>
    internal static string BluetoothProfileMissing => _manager.GetString(nameof(BluetoothProfileMissing), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the complete collapsed disclosure caption, including its right-pointing indicator.</summary>
    internal static string BluetoothDeviceInformation => _manager.GetString(nameof(BluetoothDeviceInformation), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the accessible description of a collapsed device information section.</summary>
    internal static string BluetoothInformationCollapsed => _manager.GetString(nameof(BluetoothInformationCollapsed), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the accessible description of an expanded device information section.</summary>
    internal static string BluetoothInformationExpanded => _manager.GetString(nameof(BluetoothInformationExpanded), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the complete expanded disclosure caption, including its downward-pointing indicator.</summary>
    internal static string BluetoothDeviceInformationExpanded => _manager.GetString(nameof(BluetoothDeviceInformationExpanded), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized GATT information text: All GATT services on this device</summary>
    internal static string BluetoothGattServices => _manager.GetString(nameof(BluetoothGattServices), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized GATT information text: Read GATT services</summary>
    internal static string ReadGattServices => _manager.GetString(nameof(ReadGattServices), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized GATT information text: Cancel read</summary>
    internal static string CancelGattRead => _manager.GetString(nameof(CancelGattRead), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized GATT information text: Reading GATT services…</summary>
    internal static string GattServicesLoading => _manager.GetString(nameof(GattServicesLoading), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized GATT information text: The device returned no GATT services.</summary>
    internal static string GattServicesEmpty => _manager.GetString(nameof(GattServicesEmpty), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized GATT information text: Read the device to list all of its actual GATT service UUIDs. This briefly connects without applying UART settings.</summary>
    internal static string GattServicesHint => _manager.GetString(nameof(GattServicesHint), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized GATT information text: GATT services could not be read: {0}</summary>
    internal static string GattServicesFailed => _manager.GetString(nameof(GattServicesFailed), CultureInfo.CurrentUICulture)!;
}
