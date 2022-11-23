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
// This code is licensed under the GNU General Public License v2.
// Full text may be retrieved at http://www.gnu.org/licenses/gpl-2.0.txt
//---------------------------------------------------------------------------

using Android.Hardware.Usb;
using Gurux.Serial.Enums;
using Java.Lang;
using System;

namespace Gurux.Serial
{
    /// <summary>
    /// Serial port settings. Settings vary between vendors.
    /// </summary>
    abstract class GXChipset
    {
        /// <summary>
        /// Chipset.
        /// </summary>
        public abstract Chipset Chipset
        {
            get;
        }

        /// <summary>
        /// Is vendor using this chip set.
        /// </summary>
        /// <param name="manufacturer">Manufacturer</param>
        /// <param name="vendor">Vendor ID.</param>
        /// <param name="product">Product ID.</param>
        /// <returns>True, if used chipset.</returns>
        public static bool IsUsing(string manufacturer, int vendor, int product)
        {
            throw new System.Exception("IsUsing is not implemented.");
        }

        public abstract bool Open(GXSerial serial, UsbDeviceConnection connection, byte[] rawDescriptors);

        /// <summary>
        /// Get is Data Terminal Ready (DTR) signal enabled.
        /// </summary>
        /// <param name="connection">Usb device connection.</param>
        /// <returns>Is DTR enabled.</returns>
        public abstract bool GetDtrEnable(UsbDeviceConnection connection);

        /// <summary>
        /// Set is Data Terminal Ready (DTR) signal enabled.
        /// </summary>
        /// <param name="connection">Usb device connection.</param>
        /// <param name="value">Is DTR enabled.</param>
        public abstract void SetDtrEnable(UsbDeviceConnection connection, bool value);

        /// <summary>
        /// Gets a value indicating whether the Request to Send (RTS) signal is
        /// enabled during serial communication.
        /// </summary>
        /// <param name="connection">Usb device connection.</param>
        /// <returns>Is RTS enabled.</returns>
        public abstract bool GetRtsEnable(UsbDeviceConnection connection);

        /// <summary>
        /// Sets a value indicating whether the Request to Send (RTS) signal is
        /// enabled during serial communication.
        /// </summary>
        /// <param name="connection">Usb device connection.</param>
        /// <param name="value">Is RTS enabled.</param>
        public abstract void SetRtsEnable(UsbDeviceConnection connection, bool value);

        /// <summary>
        /// Remove status bytes from data.
        /// </summary>
        /// <param name="data">Received data.</param>
        /// <param name="size">Data length.</param>
        /// <param name="maxSize">Max packet length.</param>
        /// <returns>Data size.</returns>
        public virtual int RemoveStatus(byte[] data, int size, int maxSize)
        {
            throw new System.Exception("removeStatus is not implemented.");
        }
    }
}
