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

using System.Resources;
namespace Gurux.Serial.Example.Properties;
/// <summary>Provides localized text for the serial example and its native hosts.</summary>
public static class ExampleResources
{
    /// <summary>Gets the caption for clearing displayed history.</summary>
    public static string Clear => _manager.GetString(nameof(Clear), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(Clear);
    /// <summary>Gets the localized desktop menu caption for connection lifecycle actions.</summary>
    public static string ConnectionMenu => _manager.GetString(nameof(ConnectionMenu), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(ConnectionMenu);
    /// <summary>Gets the localized desktop menu caption for switching content views.</summary>
    public static string ViewMenu => _manager.GetString(nameof(ViewMenu), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(ViewMenu);
    /// <summary>Gets the accessible caption for the persistent navigation button.</summary>
    public static string Menu => _manager.GetString(nameof(Menu), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(Menu);
    /// <summary>Gets the navigation caption for sending and receiving data.</summary>
    public static string Data => _manager.GetString(nameof(Data), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(Data);
    /// <summary>Gets the navigation caption for transport configuration.</summary>
    public static string MediaSettings => _manager.GetString(nameof(MediaSettings), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(MediaSettings);
    /// <summary>Gets the navigation caption for receive framing and activity.</summary>
    public static string CommunicationSettings => _manager.GetString(nameof(CommunicationSettings), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(CommunicationSettings);
    /// <summary>Gets the caption for returning received bytes to the sender.</summary>
    public static string Echo => _manager.GetString(nameof(Echo), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(Echo);
    /// <summary>Gets the caption for the continuously received payload history.</summary>
    public static string ReceivedData => _manager.GetString(nameof(ReceivedData), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(ReceivedData);
    /// <summary>Explains the supported receive count and timeout ranges.</summary>
    public static string InvalidReplySettings => _manager.GetString(nameof(InvalidReplySettings), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(InvalidReplySettings);
    /// <summary>Explains that framing changes apply to the next connection.</summary>
    public static string CloseBeforeReplySettings => _manager.GetString(nameof(CloseBeforeReplySettings), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(CloseBeforeReplySettings);
    /// <summary>Gets the open failure message containing the requested serial settings and the driver's error.</summary>
    public static string OpenRejectedSettings => _manager.GetString(nameof(OpenRejectedSettings), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(OpenRejectedSettings);
    /// <summary>Reads the text embedded in the example core assembly.</summary>
    private static readonly ResourceManager _manager = new("Gurux.Serial.Example.Properties.ExampleResources", typeof(ExampleResources).Assembly);
    /// <summary>Gurux serial example</summary>
    public static string Title => _manager.GetString(nameof(Title), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(Title);
    /// <summary>Settings</summary>
    public static string Settings => _manager.GetString(nameof(Settings), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(Settings);
    /// <summary>Right settings</summary>
    public static string RightSettings => _manager.GetString(nameof(RightSettings), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(RightSettings);
    /// <summary>Open</summary>
    public static string Open => _manager.GetString(nameof(Open), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(Open);
    /// <summary>Closed</summary>
    public static string Closed => _manager.GetString(nameof(Closed), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(Closed);
    /// <summary>Close / cancel</summary>
    public static string Close => _manager.GetString(nameof(Close), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(Close);
    /// <summary>Send</summary>
    public static string Send => _manager.GetString(nameof(Send), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(Send);
    /// <summary>Canceled.</summary>
    public static string Canceled => _manager.GetString(nameof(Canceled), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(Canceled);
    /// <summary>Text or hexadecimal bytes</summary>
    public static string SendPlaceholder => _manager.GetString(nameof(SendPlaceholder), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(SendPlaceholder);
    /// <summary>End of packet</summary>
    public static string EopPlaceholder => _manager.GetString(nameof(EopPlaceholder), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(EopPlaceholder);
    /// <summary>Hexadecimal</summary>
    public static string Hexadecimal => _manager.GetString(nameof(Hexadecimal), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(Hexadecimal);
    /// <summary>Receive reply</summary>
    public static string ReceiveReply => _manager.GetString(nameof(ReceiveReply), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(ReceiveReply);
    /// <summary>Reply count (0 = EOP), EOP, timeout in milliseconds</summary>
    public static string ReplyOptions => _manager.GetString(nameof(ReplyOptions), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(ReplyOptions);
    /// <summary>Gets the localized guidance for selecting an iOS Bluetooth Low Energy device.</summary>
    public static string Unsupported => _manager.GetString(nameof(Unsupported), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(Unsupported);
    /// <summary>Opened.</summary>
    public static string Opened => _manager.GetString(nameof(Opened), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(Opened);
    /// <summary>Closed.</summary>
    public static string ClosedLog => _manager.GetString(nameof(ClosedLog), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(ClosedLog);
    /// <summary>No reply was received before the timeout.</summary>
    public static string Timeout => _manager.GetString(nameof(Timeout), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(Timeout);
    /// <summary>RX the first value: the second value | the connection state</summary>
    public static string RxBytes => _manager.GetString(nameof(RxBytes), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(RxBytes);
    /// <summary>RX the first value: the second value</summary>
    public static string RxData => _manager.GetString(nameof(RxData), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(RxData);
    /// <summary>Error: the first value</summary>
    public static string Error => _manager.GetString(nameof(Error), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(Error);
    /// <summary>TX the first value</summary>
    public static string Tx => _manager.GetString(nameof(Tx), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(Tx);
    /// <summary>Reply: the first value</summary>
    public static string Reply => _manager.GetString(nameof(Reply), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(Reply);
    /// <summary>Explains that Android USB services are unavailable.</summary>
    public static string UsbUnavailable => _manager.GetString(nameof(UsbUnavailable), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(UsbUnavailable);
    /// <summary>Labels the Android transport selection sheet.</summary>
    public static string TransportTitle => _manager.GetString(nameof(TransportTitle), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(TransportTitle);
    /// <summary>Labels the supported USB CDC ACM transport.</summary>
    public static string UsbTransport => _manager.GetString(nameof(UsbTransport), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(UsbTransport);
    /// <summary>Labels the paired Bluetooth Classic transport.</summary>
    public static string BluetoothTransport => _manager.GetString(nameof(BluetoothTransport), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(BluetoothTransport);
    /// <summary>Explains how to enable Bluetooth and pair a serial device.</summary>
    public static string EnableBluetooth => _manager.GetString(nameof(EnableBluetooth), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(EnableBluetooth);
    /// <summary>Names a Bluetooth device whose platform name is unavailable.</summary>
    public static string BluetoothDevice => _manager.GetString(nameof(BluetoothDevice), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(BluetoothDevice);
    /// <summary>Formats a Bluetooth device name and address.</summary>
    public static string BluetoothDeviceLabel => _manager.GetString(nameof(BluetoothDeviceLabel), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(BluetoothDeviceLabel);
    /// <summary>Formats a USB vendor identifier, product identifier and device name.</summary>
    public static string UsbDeviceLabel => _manager.GetString(nameof(UsbDeviceLabel), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(UsbDeviceLabel);
    /// <summary>Explains that the stored serial device is unavailable and how to select another.</summary>
    public static string SelectedUnavailable => _manager.GetString(nameof(SelectedUnavailable), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(SelectedUnavailable);
    /// <summary>Explains how to make a supported Android serial device available.</summary>
    public static string NoDevices => _manager.GetString(nameof(NoDevices), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(NoDevices);
    /// <summary>Labels the serial device selection sheet.</summary>
    public static string SelectDevice => _manager.GetString(nameof(SelectDevice), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(SelectDevice);
    /// <summary>Explains that device selection requires an initialized application window.</summary>
    public static string WindowNotReady => _manager.GetString(nameof(WindowNotReady), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(WindowNotReady);
    /// <summary>Labels the device selection cancellation action.</summary>
    public static string Cancel => _manager.GetString(nameof(Cancel), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(Cancel);
    /// <summary>Explains that the serial device selection was canceled.</summary>
    public static string SelectionCanceled => _manager.GetString(nameof(SelectionCanceled), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(SelectionCanceled);
    /// <summary>Explains why nearby device permission is required.</summary>
    public static string BluetoothPermission => _manager.GetString(nameof(BluetoothPermission), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(BluetoothPermission);
    /// <summary>Explains that Android could not create a USB permission request.</summary>
    public static string UsbRequestFailed => _manager.GetString(nameof(UsbRequestFailed), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(UsbRequestFailed);
    /// <summary>Explains that access to the USB device was denied.</summary>
    public static string UsbPermissionDenied => _manager.GetString(nameof(UsbPermissionDenied), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(UsbPermissionDenied);
    /// <summary>Gets the message shown when a BLE device has no configured serial characteristic profile.</summary>
    public static string BluetoothProfileRequired => _manager.GetString(nameof(BluetoothProfileRequired), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(BluetoothProfileRequired);
}
