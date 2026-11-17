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
namespace Gurux.Serial.Blazor.Properties;
/// <summary>Provides localized settings labels and complete validation messages.</summary>
internal static class Resources
{
    /// <summary>Looks up the embedded resources using the current UI culture.</summary>
    private static readonly System.Resources.ResourceManager Manager = new("Gurux.Serial.Blazor.Properties.Resources", typeof(Resources).Assembly);
    /// <summary>Gets the complete validation message for a missing or unavailable choice.</summary>
    internal static string ChoiceError => Manager.GetString(nameof(ChoiceError), System.Globalization.CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized WholeNumberRequired text.</summary>
    internal static string WholeNumberRequired => Manager.GetString(nameof(WholeNumberRequired), System.Globalization.CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized Protocol text.</summary>
    internal static string Protocol => Manager.GetString(nameof(Protocol), System.Globalization.CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized Hostname text.</summary>
    internal static string Hostname => Manager.GetString(nameof(Hostname), System.Globalization.CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized Port text.</summary>
    internal static string Port => Manager.GetString(nameof(Port), System.Globalization.CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized Server text.</summary>
    internal static string Server => Manager.GetString(nameof(Server), System.Globalization.CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized IPv6 text.</summary>
    internal static string IPv6 => Manager.GetString(nameof(IPv6), System.Globalization.CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized Maximumclients text.</summary>
    internal static string Maximumclients => Manager.GetString(nameof(Maximumclients), System.Globalization.CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized Waittimemilliseconds text.</summary>
    internal static string Waittimemilliseconds => Manager.GetString(nameof(Waittimemilliseconds), System.Globalization.CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized TCPkeepalive text.</summary>
    internal static string TCPkeepalive => Manager.GetString(nameof(TCPkeepalive), System.Globalization.CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized TCPkeepaliveidleseconds text.</summary>
    internal static string TCPkeepaliveidleseconds => Manager.GetString(nameof(TCPkeepaliveidleseconds), System.Globalization.CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized TCPkeepaliveintervalseconds text.</summary>
    internal static string TCPkeepaliveintervalseconds => Manager.GetString(nameof(TCPkeepaliveintervalseconds), System.Globalization.CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized TCPkeepaliveretries text.</summary>
    internal static string TCPkeepaliveretries => Manager.GetString(nameof(TCPkeepaliveretries), System.Globalization.CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized Closethemediabeforeeditingsettings text.</summary>
    internal static string Closethemediabeforeeditingsettings => Manager.GetString(nameof(Closethemediabeforeeditingsettings), System.Globalization.CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized Closethemediabeforesavingsettings text.</summary>
    internal static string Closethemediabeforesavingsettings => Manager.GetString(nameof(Closethemediabeforesavingsettings), System.Globalization.CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized ThemediasettingschangedwhilethisformwasopenReopentheform text.</summary>
    internal static string ThemediasettingschangedwhilethisformwasopenReopentheform => Manager.GetString(nameof(ThemediasettingschangedwhilethisformwasopenReopentheform), System.Globalization.CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized Phonenumber text.</summary>
    internal static string Phonenumber => Manager.GetString(nameof(Phonenumber), System.Globalization.CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized Initializationcommandsoneperline text.</summary>
    internal static string Initializationcommandsoneperline => Manager.GetString(nameof(Initializationcommandsoneperline), System.Globalization.CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized Connectiontimeoutseconds text.</summary>
    internal static string Connectiontimeoutseconds => Manager.GetString(nameof(Connectiontimeoutseconds), System.Globalization.CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized SIMPIN text.</summary>
    internal static string SIMPIN => Manager.GetString(nameof(SIMPIN), System.Globalization.CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized Messageencoding text.</summary>
    internal static string Messageencoding => Manager.GetString(nameof(Messageencoding), System.Globalization.CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized Portname text.</summary>
    internal static string Portname => Manager.GetString(nameof(Portname), System.Globalization.CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized Baudrate text.</summary>
    internal static string Baudrate => Manager.GetString(nameof(Baudrate), System.Globalization.CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized Databits text.</summary>
    internal static string Databits => Manager.GetString(nameof(Databits), System.Globalization.CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized Parity text.</summary>
    internal static string Parity => Manager.GetString(nameof(Parity), System.Globalization.CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized Stopbits text.</summary>
    internal static string Stopbits => Manager.GetString(nameof(Stopbits), System.Globalization.CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized Handshake text.</summary>
    internal static string Handshake => Manager.GetString(nameof(Handshake), System.Globalization.CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized DTRenabled text.</summary>
    internal static string DTRenabled => Manager.GetString(nameof(DTRenabled), System.Globalization.CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized RTSenabled text.</summary>
    internal static string RTSenabled => Manager.GetString(nameof(RTSenabled), System.Globalization.CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized MediaSettings text.</summary>
    internal static string MediaSettings => Manager.GetString(nameof(MediaSettings), System.Globalization.CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized Save text.</summary>
    internal static string Save => Manager.GetString(nameof(Save), System.Globalization.CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized Cancel text.</summary>
    internal static string Cancel => Manager.GetString(nameof(Cancel), System.Globalization.CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized UnsupportedMedia text.</summary>
    internal static string UnsupportedMedia => Manager.GetString(nameof(UnsupportedMedia), System.Globalization.CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized InvalidSettings text.</summary>
    internal static string InvalidSettings => Manager.GetString(nameof(InvalidSettings), System.Globalization.CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the Bluetooth settings section caption.</summary>
    internal static string BluetoothSettings => Manager.GetString(nameof(BluetoothSettings), System.Globalization.CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the Bluetooth protocol selector caption.</summary>
    internal static string BluetoothTransport => Manager.GetString(nameof(BluetoothTransport), System.Globalization.CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the RFCOMM transport label.</summary>
    internal static string BluetoothClassic => Manager.GetString(nameof(BluetoothClassic), System.Globalization.CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the GATT transport label.</summary>
    internal static string BluetoothLowEnergy => Manager.GetString(nameof(BluetoothLowEnergy), System.Globalization.CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the directly selectable Bluetooth device list caption.</summary>
    internal static string BluetoothDevices => Manager.GetString(nameof(BluetoothDevices), System.Globalization.CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the complete device display format with name and address placeholders.</summary>
    internal static string BluetoothDeviceDisplay => Manager.GetString(nameof(BluetoothDeviceDisplay), System.Globalization.CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the missing Bluetooth device validation message.</summary>
    internal static string BluetoothDeviceRequired => Manager.GetString(nameof(BluetoothDeviceRequired), System.Globalization.CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the missing serial profile message with a device identifier placeholder.</summary>
    internal static string BluetoothProfileMissing => Manager.GetString(nameof(BluetoothProfileMissing), System.Globalization.CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the RFCOMM server channel validation message.</summary>
    internal static string BluetoothChannelInvalid => Manager.GetString(nameof(BluetoothChannelInvalid), System.Globalization.CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the RFCOMM channel caption.</summary>
    internal static string BluetoothChannel => Manager.GetString(nameof(BluetoothChannel), System.Globalization.CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the read-only GATT service identifier caption.</summary>
    internal static string BluetoothService => Manager.GetString(nameof(BluetoothService), System.Globalization.CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the read-only native Bluetooth identity caption.</summary>
    internal static string BluetoothIdentifier => Manager.GetString(nameof(BluetoothIdentifier), System.Globalization.CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the collapsed read-only device information section caption.</summary>
    internal static string AdvancedBluetoothSettings => Manager.GetString(nameof(AdvancedBluetoothSettings), System.Globalization.CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the shared discovery button caption.</summary>
    internal static string FindDevices => Manager.GetString(nameof(FindDevices), System.Globalization.CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the active BLE scan cancellation caption.</summary>
    internal static string StopSearch => Manager.GetString(nameof(StopSearch), System.Globalization.CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the progress message during device discovery.</summary>
    internal static string BluetoothSearching => Manager.GetString(nameof(BluetoothSearching), System.Globalization.CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the empty discovery result message.</summary>
    internal static string BluetoothNoDevices => Manager.GetString(nameof(BluetoothNoDevices), System.Globalization.CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the discovery failure message with the underlying reason placeholder.</summary>
    internal static string BluetoothDiscoveryFailed => Manager.GetString(nameof(BluetoothDiscoveryFailed), System.Globalization.CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the RFCOMM discovery explanation.</summary>
    internal static string BluetoothKnownHint => Manager.GetString(nameof(BluetoothKnownHint), System.Globalization.CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the location of the BLE discovery radio.</summary>
    internal static string BluetoothScanHint => Manager.GetString(nameof(BluetoothScanHint), System.Globalization.CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the unsupported BLE backend explanation.</summary>
    internal static string BluetoothScanUnavailable => Manager.GetString(nameof(BluetoothScanUnavailable), System.Globalization.CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the save failure message with the underlying reason placeholder.</summary>
    internal static string InvalidSettingsDetails => Manager.GetString(nameof(InvalidSettingsDetails), System.Globalization.CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the feedback for an expired or canceled discovery operation.</summary>
    internal static string BluetoothDiscoveryCanceled => Manager.GetString(nameof(BluetoothDiscoveryCanceled), System.Globalization.CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the read-only receive characteristic UUID caption.</summary>
    internal static string BluetoothRead => Manager.GetString(nameof(BluetoothRead), System.Globalization.CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the read-only output characteristic UUID caption.</summary>
    internal static string BluetoothWrite => Manager.GetString(nameof(BluetoothWrite), System.Globalization.CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the read-only configuration characteristic UUID caption.</summary>
    internal static string BluetoothConfiguration => Manager.GetString(nameof(BluetoothConfiguration), System.Globalization.CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the additional native GATT service list heading excluding displayed profile UUIDs.</summary>
    internal static string BluetoothGattServices => Manager.GetString(nameof(BluetoothGattServices), System.Globalization.CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the explicit native metadata read action caption.</summary>
    internal static string ReadGattServices => Manager.GetString(nameof(ReadGattServices), System.Globalization.CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the metadata read cancellation caption.</summary>
    internal static string CancelGattRead => Manager.GetString(nameof(CancelGattRead), System.Globalization.CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the metadata read progress message.</summary>
    internal static string GattServicesLoading => Manager.GetString(nameof(GattServicesLoading), System.Globalization.CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the message for a successful read with no additional service UUIDs.</summary>
    internal static string GattServicesEmpty => Manager.GetString(nameof(GattServicesEmpty), System.Globalization.CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the explanation of explicit metadata reading and its temporary connection.</summary>
    internal static string GattServicesHint => Manager.GetString(nameof(GattServicesHint), System.Globalization.CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the complete metadata failure message with its native reason placeholder.</summary>
    internal static string GattServicesFailed => Manager.GetString(nameof(GattServicesFailed), System.Globalization.CultureInfo.CurrentUICulture)!;
}
