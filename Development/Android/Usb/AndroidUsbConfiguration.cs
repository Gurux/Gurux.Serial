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
#if ANDROID
using Android.Hardware.Usb;
using System.IO.Ports;

namespace Gurux.Serial;
/// <summary>Applies bounded vendor control transfers to an already claimed serial interface.</summary>
internal static class AndroidUsbConfiguration
{
    /// <summary>Checks cancellation and verifies a complete control transaction with a finite timeout.</summary>
    /// <param name="connection">The owned USB connection.</param>
    /// <param name="token">The opening cancellation token.</param>
    /// <param name="type">The direction, request kind and recipient flags.</param>
    /// <param name="request">The vendor or class request number.</param>
    /// <param name="value">The request-specific value field.</param>
    /// <param name="index">The request-specific index field.</param>
    /// <param name="data">The optional input or output buffer.</param>
    /// <exception cref="IOException">The transaction fails or returns an incomplete response.</exception>
    private static void Transfer(UsbDeviceConnection connection, CancellationToken token, int type, int request, int value, int index, byte[]? data = null)
    {
        token.ThrowIfCancellationRequested();
        int expected = data?.Length ?? 0;
        int result = connection.ControlTransfer((UsbAddressing)type, request, value, index, data, expected, 1000);
        if (result != expected)
        {
            throw new IOException(global::Gurux.Serial.Properties.Resources.Get("USBSerialControlTransferFailed"));
        }
    }
    /// <summary>Configures framing, disables flow control and applies the requested modem outputs.</summary>
    /// <param name="connection">The owned device connection.</param>
    /// <param name="chipset">The selected vendor protocol.</param>
    /// <param name="id">The claimed USB interface identifier.</param>
    /// <param name="settings">The requested framing and modem outputs.</param>
    /// <param name="token">The opening cancellation token.</param>
    /// <exception cref="NotSupportedException">The requested framing or handshake is unsupported.</exception>
    /// <exception cref="IOException">A control transfer fails or returns a short response.</exception>
    /// <exception cref="OperationCanceledException">Opening is cancelled before or between control transfers.</exception>
    /// <remarks>CH34x initialization reads a complete two-byte version response, resets the handshake before configuring framing, and enables short-packet transmission only for chip versions greater than 0x27.</remarks>
    internal static void Apply(UsbDeviceConnection connection, AndroidUsbChipset chipset, int id, SerialPortSettings settings, CancellationToken token)
    {
        if (settings.Handshake != Handshake.None || settings.DataBits is < 5 or > 8 ||
            settings.BaudRate <= 0 || !Enum.IsDefined(settings.Parity))
        {
            throw new NotSupportedException(global::Gurux.Serial.Properties.Resources.Get("USBSerialOptionsUnsupported"));
        }
        int stop = AndroidUsbProtocol.StopCode(settings.StopBits);
        byte[] rate = new byte[4];
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(rate, settings.BaudRate);
        int modem = (settings.DtrEnable ? 1 : 0) | (settings.RtsEnable ? 2 : 0);
        switch (chipset)
        {
            case AndroidUsbChipset.Ftdi:
                int divisor = AndroidUsbProtocol.FtdiDivisor(settings.BaudRate);
                byte[] descriptors = connection.GetRawDescriptors() ?? [];
                int revision = descriptors.Length >= 14 ? descriptors[12] | descriptors[13] << 8 : 0;
                if (revision < 0x0400)
                {
                    throw new NotSupportedException(global::Gurux.Serial.Properties.Resources.Get("USBSerialOptionsUnsupported"));
                }
                int channel = id + 1;
                bool multi = revision is 0x0500 or 0x0700 or 0x0800;
                Transfer(connection, token, 0x40, 0, 0, channel);
                Transfer(connection, token, 0x40, 2, 0, channel);
                Transfer(connection, token, 0x40, 3, divisor & 0xffff, multi ? ((divisor >> 16) << 8) | channel : divisor >> 16);
                Transfer(connection, token, 0x40, 4, settings.DataBits | ((int)settings.Parity << 8) | (stop << 11), channel);
                Transfer(connection, token, 0x40, 1, 0x0300 | modem, channel);
                break;
            case AndroidUsbChipset.Cp210x:
                Transfer(connection, token, 0x41, 0, 1, id);
                Transfer(connection, token, 0x41, 0x1e, 0, id, rate);
                Transfer(connection, token, 0x41, 3, (settings.DataBits << 8) | ((int)settings.Parity << 4) | stop, id);
                byte[] flow = new byte[16];
                // DTR and RTS are controlled manually through SET_MHS, with automatic flow control disabled.
                System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(flow, 1);
                System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(flow.AsSpan(4), 0x40);
                Transfer(connection, token, 0x41, 0x13, 0, id, flow);
                Transfer(connection, token, 0x41, 7, 0x0300 | modem, id);
                break;
            case AndroidUsbChipset.Ch34x:
                if (settings.StopBits == StopBits.OnePointFive)
                {
                    throw new NotSupportedException(global::Gurux.Serial.Properties.Resources.Get("USBSerialOptionsUnsupported"));
                }
                (int a, int b) = settings.BaudRate switch
                {
                    2400 => (0xd901, 0x38),
                    4800 => (0x6402, 0x1f),
                    9600 => (0xb202, 0x13),
                    19200 => (0xd902, 0x0d),
                    38400 => (0x6403, 0x0a),
                    115200 => (0xcc03, 8),
                    _ => throw new NotSupportedException(global::Gurux.Serial.Properties.Resources.Get("USBSerialOptionsUnsupported"))
                };
                Transfer(connection, token, 0x40, 0xa1, 0, 0);
                byte[] version = new byte[2];
                Transfer(connection, token, 0xc0, 0x5f, 0, 0, version);
                int chipVersion = version[0];
                Transfer(connection, token, 0x40, 0x9a, 0x1312, 0xd982);
                Transfer(connection, token, 0x40, 0x9a, 0x0f2c, 4);
                Transfer(connection, token, 0x40, 0x9a, 0x2727, 0);
                Transfer(connection, token, 0x40, 0xa4, 0xff, 0);
                int parity = settings.Parity switch
                {
                    Parity.None => 0,
                    Parity.Odd => 8,
                    Parity.Even => 24,
                    Parity.Mark => 40,
                    Parity.Space => 56,
                    _ => throw new ArgumentOutOfRangeException(nameof(settings))
                };
                int framing = 0xc0 | parity | (settings.DataBits - 5) | (stop == 2 ? 4 : 0);
                Transfer(connection, token, 0x40, 0xa1, 0x9c | (framing << 8), 0x88);
                Transfer(connection, token, 0x40, 0x9a, 0x1312, chipVersion > 0x27 ? a | 0x80 : a);
                Transfer(connection, token, 0x40, 0x9a, 0x0f2c, b);
                Transfer(connection, token, 0x40, 0xa4, (~((settings.DtrEnable ? 0x20 : 0) | (settings.RtsEnable ? 0x40 : 0))) & 0xffff, 0);
                break;
            case AndroidUsbChipset.Prolific:
                // Legacy/HX initialization, using the class request type corrected in the Java driver.
                byte[] raw = connection.GetRawDescriptors() ?? [];
                if (raw.Length < 14 || raw[13] > 3 || raw[3] >= 2)
                {
                    throw new NotSupportedException(global::Gurux.Serial.Properties.Resources.Get("USBSerialOptionsUnsupported"));
                }
                byte[] probe = new byte[1];
                Transfer(connection, token, 0xc0, 1, 0x8484, 0, probe);
                Transfer(connection, token, 0x40, 1, 0x0404, 0);
                Transfer(connection, token, 0xc0, 1, 0x8484, 0, probe);
                Transfer(connection, token, 0xc0, 1, 0x8383, 0, probe);
                Transfer(connection, token, 0xc0, 1, 0x8484, 0, probe);
                Transfer(connection, token, 0x40, 1, 0x0404, 1);
                Transfer(connection, token, 0xc0, 1, 0x8484, 0, probe);
                Transfer(connection, token, 0xc0, 1, 0x8383, 0, probe);
                Transfer(connection, token, 0x40, 1, 0, 1);
                Transfer(connection, token, 0x40, 1, 1, 0);
                Transfer(connection, token, 0x40, 1, 2, raw[7] == 0x40 ? 0x44 : 0x24);
                byte[] coding = new byte[7];
                rate.CopyTo(coding, 0);
                coding[4] = (byte)stop;
                coding[5] = (byte)settings.Parity;
                coding[6] = (byte)settings.DataBits;
                Transfer(connection, token, 0x21, 0x20, 0, id, coding);
                Transfer(connection, token, 0x21, 0x23, 0, id);
                Transfer(connection, token, 0x21, 0x22, modem, id);
                Transfer(connection, token, 0x40, 1, 8, 0);
                Transfer(connection, token, 0x40, 1, 9, 0);
                break;
            default:
                throw new NotSupportedException(global::Gurux.Serial.Properties.Resources.Get("USBSerialOptionsUnsupported"));
        }
        token.ThrowIfCancellationRequested();
    }
}
#endif
