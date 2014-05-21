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

using System;
using System.Threading;

namespace Gurux.Serial
{
    class GXReceiveThread
    {
        public ManualResetEvent Closing;
        GXSerial m_Parent;
        public GXReceiveThread(GXSerial parent)
        {
            Closing = new ManualResetEvent(false);
            m_Parent = parent;
        }

        /// <summary>
        /// Receive data from the server using the established socket connection
        /// </summary>
        /// <returns>The data received from the server</returns>
        public void Receive()
        {
            try
            {
                while (!Closing.WaitOne(1))
                {
                    if (m_Parent.BytesToRead > 0)
                    {
                        m_Parent.GXSerial_DataReceived(m_Parent, null);
                    }
                    else
                    {
                        Closing.WaitOne(100);
                    }
                }
            }
            catch (Exception ex)
            {
                m_Parent.NotifyError(ex);
                if (!Closing.WaitOne(1))
                {
                    m_Parent.Close();
                }
            }
        }
    }
}
