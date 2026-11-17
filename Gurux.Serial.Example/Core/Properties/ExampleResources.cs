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
    public static string Clear => Manager.GetString(nameof(Clear), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(Clear);
    /// <summary>Gets the localized desktop menu caption for connection lifecycle actions.</summary>
    public static string ConnectionMenu => Manager.GetString(nameof(ConnectionMenu), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(ConnectionMenu);
    /// <summary>Gets the localized desktop menu caption for switching content views.</summary>
    public static string ViewMenu => Manager.GetString(nameof(ViewMenu), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(ViewMenu);
    /// <summary>Gets the accessible caption for the persistent navigation button.</summary>
    public static string Menu => Manager.GetString(nameof(Menu), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(Menu);
    /// <summary>Gets the navigation caption for sending and receiving data.</summary>
    public static string Data => Manager.GetString(nameof(Data), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(Data);
    /// <summary>Gets the navigation caption for transport configuration.</summary>
    public static string MediaSettings => Manager.GetString(nameof(MediaSettings), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(MediaSettings);
    /// <summary>Gets the navigation caption for receive framing and activity.</summary>
    public static string CommunicationSettings => Manager.GetString(nameof(CommunicationSettings), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(CommunicationSettings);
    /// <summary>Gets the caption for returning received bytes to the sender.</summary>
    public static string Echo => Manager.GetString(nameof(Echo), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(Echo);
    /// <summary>Gets the caption for the continuously received payload history.</summary>
    public static string ReceivedData => Manager.GetString(nameof(ReceivedData), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(ReceivedData);
    /// <summary>Explains the supported receive count and timeout ranges.</summary>
    public static string InvalidReplySettings => Manager.GetString(nameof(InvalidReplySettings), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(InvalidReplySettings);
    /// <summary>Explains that framing changes apply to the next connection.</summary>
    public static string CloseBeforeReplySettings => Manager.GetString(nameof(CloseBeforeReplySettings), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(CloseBeforeReplySettings);
    /// <summary>Gets the open failure message containing the requested serial settings and the driver's error.</summary>
    public static string OpenRejectedSettings => Manager.GetString(nameof(OpenRejectedSettings), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(OpenRejectedSettings);
    /// <summary>Reads the text embedded in the example core assembly.</summary>
    private static readonly ResourceManager Manager = new("Gurux.Serial.Example.Properties.ExampleResources", typeof(ExampleResources).Assembly);
    /// <summary>Gurux serial example</summary>
    public static string Title => Manager.GetString(nameof(Title), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(Title);
    /// <summary>Settings</summary>
    public static string Settings => Manager.GetString(nameof(Settings), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(Settings);
    /// <summary>Right settings</summary>
    public static string RightSettings => Manager.GetString(nameof(RightSettings), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(RightSettings);
    /// <summary>Open</summary>
    public static string Open => Manager.GetString(nameof(Open), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(Open);
    /// <summary>Closed</summary>
    public static string Closed => Manager.GetString(nameof(Closed), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(Closed);
    /// <summary>Close / cancel</summary>
    public static string Close => Manager.GetString(nameof(Close), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(Close);
    /// <summary>Send</summary>
    public static string Send => Manager.GetString(nameof(Send), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(Send);
    /// <summary>Canceled.</summary>
    public static string Canceled => Manager.GetString(nameof(Canceled), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(Canceled);
    /// <summary>Text or hexadecimal bytes</summary>
    public static string SendPlaceholder => Manager.GetString(nameof(SendPlaceholder), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(SendPlaceholder);
    /// <summary>End of packet</summary>
    public static string EopPlaceholder => Manager.GetString(nameof(EopPlaceholder), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(EopPlaceholder);
    /// <summary>Hexadecimal</summary>
    public static string Hexadecimal => Manager.GetString(nameof(Hexadecimal), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(Hexadecimal);
    /// <summary>Receive reply</summary>
    public static string ReceiveReply => Manager.GetString(nameof(ReceiveReply), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(ReceiveReply);
    /// <summary>Reply count (0 = EOP), EOP, timeout in milliseconds</summary>
    public static string ReplyOptions => Manager.GetString(nameof(ReplyOptions), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(ReplyOptions);
    /// <summary>Gets the localized guidance for selecting an iOS Bluetooth Low Energy device.</summary>
    public static string Unsupported => Manager.GetString(nameof(Unsupported), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(Unsupported);
    /// <summary>Opened.</summary>
    public static string Opened => Manager.GetString(nameof(Opened), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(Opened);
    /// <summary>Closed.</summary>
    public static string ClosedLog => Manager.GetString(nameof(ClosedLog), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(ClosedLog);
    /// <summary>No reply was received before the timeout.</summary>
    public static string Timeout => Manager.GetString(nameof(Timeout), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(Timeout);
    /// <summary>RX the first value: the second value | the connection state</summary>
    public static string RxBytes => Manager.GetString(nameof(RxBytes), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(RxBytes);
    /// <summary>RX the first value: the second value</summary>
    public static string RxData => Manager.GetString(nameof(RxData), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(RxData);
    /// <summary>Error: the first value</summary>
    public static string Error => Manager.GetString(nameof(Error), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(Error);
    /// <summary>TX the first value</summary>
    public static string Tx => Manager.GetString(nameof(Tx), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(Tx);
    /// <summary>Reply: the first value</summary>
    public static string Reply => Manager.GetString(nameof(Reply), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(Reply);
    /// <summary>Explains that Android USB services are unavailable.</summary>
    public static string UsbUnavailable => Manager.GetString(nameof(UsbUnavailable), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(UsbUnavailable);
    /// <summary>Labels the Android transport selection sheet.</summary>
    public static string TransportTitle => Manager.GetString(nameof(TransportTitle), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(TransportTitle);
    /// <summary>Labels the supported USB CDC ACM transport.</summary>
    public static string UsbTransport => Manager.GetString(nameof(UsbTransport), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(UsbTransport);
    /// <summary>Labels the paired Bluetooth Classic transport.</summary>
    public static string BluetoothTransport => Manager.GetString(nameof(BluetoothTransport), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(BluetoothTransport);
    /// <summary>Explains how to enable Bluetooth and pair a serial device.</summary>
    public static string EnableBluetooth => Manager.GetString(nameof(EnableBluetooth), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(EnableBluetooth);
    /// <summary>Names a Bluetooth device whose platform name is unavailable.</summary>
    public static string BluetoothDevice => Manager.GetString(nameof(BluetoothDevice), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(BluetoothDevice);
    /// <summary>Formats a Bluetooth device name and address.</summary>
    public static string BluetoothDeviceLabel => Manager.GetString(nameof(BluetoothDeviceLabel), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(BluetoothDeviceLabel);
    /// <summary>Formats a USB vendor identifier, product identifier and device name.</summary>
    public static string UsbDeviceLabel => Manager.GetString(nameof(UsbDeviceLabel), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(UsbDeviceLabel);
    /// <summary>Explains that the stored serial device is unavailable and how to select another.</summary>
    public static string SelectedUnavailable => Manager.GetString(nameof(SelectedUnavailable), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(SelectedUnavailable);
    /// <summary>Explains how to make a supported Android serial device available.</summary>
    public static string NoDevices => Manager.GetString(nameof(NoDevices), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(NoDevices);
    /// <summary>Labels the serial device selection sheet.</summary>
    public static string SelectDevice => Manager.GetString(nameof(SelectDevice), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(SelectDevice);
    /// <summary>Explains that device selection requires an initialized application window.</summary>
    public static string WindowNotReady => Manager.GetString(nameof(WindowNotReady), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(WindowNotReady);
    /// <summary>Labels the device selection cancellation action.</summary>
    public static string Cancel => Manager.GetString(nameof(Cancel), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(Cancel);
    /// <summary>Explains that the serial device selection was canceled.</summary>
    public static string SelectionCanceled => Manager.GetString(nameof(SelectionCanceled), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(SelectionCanceled);
    /// <summary>Explains why nearby device permission is required.</summary>
    public static string BluetoothPermission => Manager.GetString(nameof(BluetoothPermission), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(BluetoothPermission);
    /// <summary>Explains that Android could not create a USB permission request.</summary>
    public static string UsbRequestFailed => Manager.GetString(nameof(UsbRequestFailed), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(UsbRequestFailed);
    /// <summary>Explains that access to the USB device was denied.</summary>
    public static string UsbPermissionDenied => Manager.GetString(nameof(UsbPermissionDenied), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(UsbPermissionDenied);
    /// <summary>Gets the message shown when a BLE device has no configured serial characteristic profile.</summary>
    public static string BluetoothProfileRequired => Manager.GetString(nameof(BluetoothProfileRequired), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(BluetoothProfileRequired);
}
