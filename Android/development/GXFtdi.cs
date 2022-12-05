//
// --------------------------------------------------------------------------
//  Gurux Ltd
//
//
//
// Filename:        $HeadURL$
//
// Version:         $Revision$,
//                  $Date$
//                  $Author$
//
// Copyright (c) Gurux Ltd
//
//---------------------------------------------------------------------------
//
//  DESCRIPTION
//
// This file is a part of Gurux Device Framework.
//
// Gurux Device Framework is Open Source software; you can redistribute it
// and/or modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; version 2 of the License.
// Gurux Device Framework is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU General Public License for more details.
//
// More information of Gurux products: http://www.gurux.org
//
// This code is licensed under the GNU General Public License v2.
// Full text may be retrieved at http://www.gnu.org/licenses/gpl-2.0.txt
//---------------------------------------------------------------------------

using Android.Hardware.Usb;
using Gurux.Serial.Enums;

namespace Gurux.Serial
{
    /**
     * FTDI chipset settings.
     */
    class GXFtdi : GXChipset
    {

        /// <inheritdoc />
        public override Chipset Chipset
        {
            get
            {
                return Chipset.Ftdi;
            }
        }

        /* Setup Data Constants */

        private static int FTDI_SIO_SET_DATA_REQUEST_TYPE = 0x40;
        /* Reset the port */
        private static int FTDI_SIO_MODEM_CTRL = 1;
        /* Set flow control register */
        private static int FTDI_SIO_SET_BAUD_RATE = 3;
        /* Set baud rate */
        private static int FTDI_SIO_SET_DATA = 4;
        /**
         * Lenght of modem status header.
         */
        private static int STATUS_LENGTH = 2;

        private static int SIO_SET_DTR_ENABLED = 0x0101;
        private static int SIO_SET_DTR_DISABLED = 0x0100;

        private static int SIO_SET_RTS_ENABLED = 0x0202;
        private static int SIO_SET_RTS_DISABLED = 0x0200;

        private bool _DtrEnable = false;
        private bool _RtsEnable = false;

        public new static bool IsUsing(string manufacturer, int vendor, int product)
        {
            if ((vendor == 1027 && product == 24557) ||
                (vendor == 1027 && product == 24577) ||
                    "FTDI" == manufacturer)
            {
                return true;
            }
            return false;
        }

        private static int GetBaudRateValue(int baudRate)
        {
            int value;
            switch (baudRate)
            {
                case 1200:
                    value = 0x09C4;
                    break;
                case 14400:
                    value = 0x80D0;
                    break;
                case 19200:
                    value = 0x809C;
                    break;
                case 2400:
                    value = 0x04E2;
                    break;
                case 300:
                    value = 0x2710;
                    break;
                case 38400:
                    value = 0xC04E;
                    break;
                case 4800:
                    value = 0x0271;
                    break;
                case 600:
                    value = 0x1388;
                    break;
                case 9600:
                    value = 0x4138;
                    break;
                default:
                    throw new System.ArgumentOutOfRangeException("Invalid baud rate value.");
            }
            return value;
        }

        public override bool Open(GXSerial serial, UsbDeviceConnection connection, byte[] rawDescriptors)
        {
            // reset
            int ret = connection.ControlTransfer(0x40, 0, 0, 0, null, 0, 0);
            if (ret == -1)
            {
                return false;
            }
            int value = serial.DataBits + ((int)serial.Parity << 8) + ((int)serial.StopBits << 11);

            ret = connection.ControlTransfer(FTDI_SIO_SET_DATA_REQUEST_TYPE, FTDI_SIO_SET_DATA, value, 0, null,
                    0, 0);
            if (ret == -1)
            {
                return false;
            }

            ret = connection.ControlTransfer(FTDI_SIO_SET_DATA_REQUEST_TYPE, FTDI_SIO_SET_BAUD_RATE,
                    GetBaudRateValue(serial.BaudRate), 0, null, 0, 0);
            if (ret == -1)
            {
                return false;
            }
            return true;
        }

        /// <inheritdoc />
        public override bool GetDtrEnable(UsbDeviceConnection connection)
        {
            return _DtrEnable;
        }

        /// <inheritdoc />
        public override void SetDtrEnable(UsbDeviceConnection connection, bool value)
        {
            int ret = connection.ControlTransfer(FTDI_SIO_SET_DATA_REQUEST_TYPE, FTDI_SIO_MODEM_CTRL,
                    value ? SIO_SET_DTR_ENABLED : SIO_SET_DTR_DISABLED, 0, null, 0, 0);
            if (ret != 0)
            {
                throw new System.Exception("Set DTR failed: " + ret);
            }
            _DtrEnable = value;
        }

        /// <inheritdoc />
        public override bool GetRtsEnable(UsbDeviceConnection connection)
        {
            return _RtsEnable;
        }

        /// <inheritdoc />
        public override void SetRtsEnable(UsbDeviceConnection connection, bool value)
        {
            int ret = connection.ControlTransfer(FTDI_SIO_SET_DATA_REQUEST_TYPE, FTDI_SIO_MODEM_CTRL,
                    value ? SIO_SET_RTS_ENABLED : SIO_SET_RTS_DISABLED, 0, null, 0, 0);
            if (ret != 0)
            {
                throw new System.Exception("Set DTR failed: " + ret);
            }
            _RtsEnable = value;
        }

        /// <inheritdoc />
        public override int RemoveStatus(byte[] data, int size, int maxPacketSize)
        {
            System.Array.Copy(data, STATUS_LENGTH, data, 0, size - STATUS_LENGTH);
            return size - STATUS_LENGTH;
        }
    }
}