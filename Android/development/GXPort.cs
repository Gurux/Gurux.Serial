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

using Gurux.Common;
using Gurux.Serial.Enums;
using Java.Lang;

namespace Gurux.Serial
{
    public class GXPort
    {
        /// <summary>
        /// USB port.
        /// </summary>
        public string Port { get; set; }

        /// <summary>
        /// Manufacturer name. 
        /// </summary>
        public string Manufacturer { get; set; }

        /// <summary>
        /// Vendor name. 
        /// </summary>
        public string Vendor { get; set; }

        /// <summary>
        /// Product name. 
        /// </summary>
        public string Product { get; set; }

        /// <summary>
        /// Product ID. 
        /// </summary>
        public int ProductId { get; set; }

        /// <summary>
        /// Vendor ID. 
        /// </summary>
        public int VendorId { get; set; }

        /// <summary>
        /// Serial number. 
        /// </summary>
        public string Serial { get; set; }

        /// <summary>
        /// Raw descriptors. 
        /// </summary>
        public byte[] RawDescriptors { get; set; }

        /// <summary>
        /// Used serial port chipset. 
        /// </summary>
        public Chipset Chipset { get; set; }

        public override string ToString()
        {
            if (!string.IsNullOrEmpty(Product))
            {
                return Product;
            }
            if (!string.IsNullOrEmpty(Vendor))
            {
                return Vendor;
            }
            return Port;
        }

        public string GetInfo()
        {
            StringBuffer sb = new StringBuffer();
            sb.Append("Manufacturer info: ");
            sb.Append(Manufacturer);
            sb.Append(System.Environment.NewLine);
            if (VendorId != 0)
            {
                sb.Append("Vendor: ");
                sb.Append(Vendor);
                sb.Append(" ID: ");
                sb.Append(VendorId.ToString("X"));
                sb.Append(System.Environment.NewLine);
            }
            sb.Append("Product: ");
            sb.Append(Product);
            sb.Append(" ID: ");
            sb.Append(ProductId.ToString("X"));
            sb.Append(System.Environment.NewLine);
            if (Serial != null)
            {
                sb.Append("Serial: ");
                sb.Append(Serial);
                sb.Append(System.Environment.NewLine);
            }
            sb.Append("Chipset: ");
            if (Chipset != Chipset.None)
            {
                sb.Append(Chipset.ToString());
            }
            else
            {
                sb.Append("Unknown");
            }
            sb.Append(System.Environment.NewLine);
            if (RawDescriptors != null)
            {
                sb.Append("Raw: ");
                sb.Append(GXCommon.ToHex(RawDescriptors));
                sb.Append(System.Environment.NewLine);
            }
            return sb.ToString();
        }
    }
}
