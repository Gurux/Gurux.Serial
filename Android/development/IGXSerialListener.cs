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

namespace Gurux.Serial
{
    /// <summary>
    /// Called when the when new USB port is added.
    /// </summary>
    /// <param name="port">Added serial port.</param>
    public delegate void PortAddEventHandler(GXPort port);
    /// <summary>
    /// Called when the when new USB port is removed.
    /// </summary>
    /// <param name="port">Removed serial port.</param>
    /// <param name="index">Index where port is removed.</param>
    public delegate void PortRemoveEventHandler(GXPort port, int index);
}
