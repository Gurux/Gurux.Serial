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
using System.IO.Ports;

namespace Gurux.Serial
{
    /**
     * Ch 34x chipset settings.
     */
    class GXCh34x : GXChipset
    {
        /// <inheritdoc />
        public override Chipset Chipset
        {
            get
            {
                return Chipset.Ch34x;
            }
        }

        /// <inheritdoc />
        public new static bool IsUsing(string manufacturer, int vendor, int product)
        {
            //QinHeng Electronics.
            if ((vendor == 0x1a86))
            {
                return true;
            }
            return false;
        }

        /// <inheritdoc />
        public void SetBaudRate(UsbDeviceConnection connection, int baudRate)
        {
            int a, b;
            switch (baudRate)
            {
                case 2400:
                    a = 0xd901;
                    b = 0x0038;
                    break;
                case 4800:
                    a = 0x6402;
                    b = 0x001f;
                    break;
                case 9600:
                    a = 0xb202;
                    b = 0x0013;
                    break;
                case 19200:
                    a = 0xd902;
                    b = 0x000d;
                    break;
                case 38400:
                    a = 0x6403;
                    b = 0x000a;
                    break;
                case 115200:
                    a = 0xcc03;
                    b = 0x0008;
                    break;
                default:
                    throw new System.ArgumentOutOfRangeException("Invalid baud rate: " + baudRate);
            }
            int ret = connection.ControlTransfer(64, 0x9a, 0x1312, a, null, 0, 1000);
            if (ret < 0)
            {
                throw new System.Exception("Failed to set baud rate. #1");
            }
            ret = connection.ControlTransfer(64, 0x9a, 0x0f2c, b, null, 0, 1000);
            if (ret < 0)
            {
                throw new System.Exception("Failed to set baud rate. #2");
            }
        }

        private void SetConfig(GXSerial serial, UsbDeviceConnection connection)
        {
            int value1, value2;
            switch (serial.Parity)
            {
                case Parity.None:
                    value1 = 0;
                    break;
                case Parity.Odd:
                    value1 = 8;
                    break;
                case Parity.Even:
                    value1 = 24;
                    break;
                case Parity.Mark:
                    value1 = 40;
                    break;
                case Parity.Space:
                    value1 = 56;
                    break;
                default:
                    {
                        throw new System.ArgumentOutOfRangeException("Invalid parity.");
                    }
            }
            if (serial.StopBits == StopBits.Two)
            {
                value1 = (value1 | 4);
            }
            switch (serial.DataBits)
            {
                case 5:
                    break;
                case 6:
                    value1 |= 1;
                    break;
                case 7:
                    value1 |= 2;
                    break;
                case 8:
                    value1 |= 3;
                    break;
                default:
                    throw new System.ArgumentOutOfRangeException("Invalid data bits value.");
            }
            value1 = (value1 | 192);
            value1 = (156 | value1 << 8) & 0xFF;
            value2 = 0x88;
            switch (serial.BaudRate)
            {
                /*
                 case 50: {
                     value2 |= 0;
                     value2 |= 22 << 8;
                     break;
                 }
                 case 75: {
                     value2 |= 0;
                     value2 |= 100 << 8;
                     break;
                 }
                 case 110: {
                     value2 |= 0;
                     value2 |= 150 << 8;
                     break;
                 }
                 case 135: {
                     value2 |= 0;
                     value2 |= 169 << 8;
                     break;
                 }
                 case 150: {
                     value2 |= 0;
                     value2 |= 178 << 8;
                     break;
                 }
                 */
                case 300:
                    {
                        value2 |= 0;
                        value2 |= 217 << 8;
                        break;
                    }
                case 600:
                    {
                        value2 |= 1;
                        value2 |= 100 << 8;
                        break;
                    }
                case 1200:
                    {
                        value2 |= 1;
                        value2 |= 178 << 8;
                        break;
                    }
                /*
                case 1800: {
                    value2 |= 1;
                    value2 |= 204 << 8;
                    break;
                }
                */
                case 2400:
                    {
                        value2 |= 1;
                        value2 |= 217 << 8;
                        break;
                    }
                case 4800:
                    {
                        value2 |= 2;
                        value2 |= 100 << 8;
                        break;
                    }
                case 9600:
                    {
                        value2 |= 2;
                        value2 |= 178 << 8;
                        break;
                    }
                case 19200:
                    {
                        value2 |= 2;
                        value2 |= 217 << 8;
                        break;
                    }
                case 38400:
                    {
                        value2 |= 3;
                        value2 |= 100 << 8;
                        break;
                    }
                /*
                case 57600: {
                    value2 |= 3;
                    value2 |= 152 << 8;
                    break;
                }
                case 115200: {
                    value2 |= 3;
                    value2 |= 204 << 8;
                    break;
                }
                case 230400: {
                    value2 |= 3;
                    value2 |= 230 << 8;
                    break;
                }
                case 460800: {
                    value2 |= 3;
                    value2 |= 243 << 8;
                    break;
                }
                case 500000: {
                    value2 |= 3;
                    value2 |= 244 << 8;
                    break;
                }
                case 921600: {
                    value2 |= 7;
                    value2 |= 243 << 8;
                    break;
                }
                case 1000000: {
                    value2 |= 3;
                    value2 |= 250 << 8;
                    break;
                }
                case 2000000: {
                    value2 |= 3;
                    value2 |= 253 << 8;
                    break;
                }
                case 3000000: {
                    value2 |= 3;
                    value2 |= 254 << 8;
                    break;
                }
                */
                default:
                    {
                        throw new System.ArgumentOutOfRangeException("Invalid baud rate value.");
                    }
            }
            int ret = connection.ControlTransfer(64, 161, value1, value2, null, 0, serial.WriteTimeout);
            if (ret < 0)
            {
                throw new System.Exception("Status failed: " + ret);
            }
        }

        public override bool Open(GXSerial serial, UsbDeviceConnection connection, byte[] rawDescriptors)
        {
            byte[] buffer = new byte[8];
            int ret = connection.ControlTransfer(64, 161, 0, 0, null, 0, serial.WriteTimeout);
            if (ret < 0)
            {
                throw new System.Exception("Status failed: " + ret);
            }
            ret = connection.ControlTransfer(192, 95, 0, 0, buffer, buffer.Length, serial.WriteTimeout);
            if (ret < 0)
            {
                throw new System.Exception("Init failed1." + ret);
            }
            //Set baud rate.
            ret = connection.ControlTransfer(64, 154, 4882, 55682, null, 0, serial.WriteTimeout);
            if (ret < 0)
            {
                throw new System.Exception("Init set baud rate failed: " + ret);
            }
            ret = connection.ControlTransfer(64, 154, 3884, 4, null, 0, serial.WriteTimeout);
            if (ret < 0)
            {
                throw new System.Exception("Init failed3: " + ret);
            }
            //End baud rate.
            ret = connection.ControlTransfer(64, 154, 10023, 0, null, 0, serial.WriteTimeout);
            if (ret < 0)
            {
                throw new System.Exception("Init End baud rate failed: " + ret);
            }
            //writeHandshakeByte
            ret = connection.ControlTransfer(64, 164, 255, 0, null, 0, serial.WriteTimeout);
            if (ret < 0)
            {
                throw new System.Exception("Init writeHandshakeByte failed: " + ret);
            }
            SetConfig(serial, connection);

            //Set baud rate
            //setBaudRate(mConnection, serial.BaudRate);
            return true;
        }

        public override bool GetDtrEnable(UsbDeviceConnection connection)
        {
            throw new System.NotImplementedException();
        }

        public override void SetDtrEnable(UsbDeviceConnection connection, bool value)
        {
            throw new System.NotImplementedException();
        }

        public override bool GetRtsEnable(UsbDeviceConnection connection)
        {
            throw new System.NotImplementedException();
        }

        public override void SetRtsEnable(UsbDeviceConnection connection, bool value)
        {
            throw new System.NotImplementedException();
        }
    }
}