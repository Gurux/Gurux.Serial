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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Gurux.Common;

namespace Gurux.Shared
{
    class GXSynchronousMediaBase
    {
        internal Exception Exception;
        internal byte[] m_Received = null;
        internal AutoResetEvent m_ReceivedEvent = new AutoResetEvent(false);
        internal readonly object m_ReceivedSync = new object();
        internal int m_ReceivedSize;
        internal int m_LastPosition;
        /// <summary>
        /// Trace level.
        /// </summary>
        public System.Diagnostics.TraceLevel Trace;

        public GXSynchronousMediaBase(int bufferSize)
        {
            m_Received = new byte[bufferSize];
            m_LastPosition = 0;
        }

        public void AppendData(byte[] data, int index, int count)
        {
            lock (m_ReceivedSync)
            {
                //Alocate new buffer.
                if (m_ReceivedSize + count > m_Received.Length)
                {
                    byte[] tmp = new byte[2 * m_Received.Length];
                    Array.Copy(m_Received, 0, tmp, 0, m_ReceivedSize);
                    m_Received = tmp;
                }
                Array.Copy(data, index, m_Received, m_ReceivedSize, count);
                m_ReceivedSize += count - index;
            }
        }

        public bool Receive<T>(ReceiveParameters<T> args)
        {            
            if (args.Eop == null && args.Count == 0 && !args.AllData)
            {
                throw new ArgumentException("Either Count or Eop must be set.");
            }
            int nSize = 0;
            byte[] terminator = null;
            if (args.Eop != null)
            {
                if (args.Eop is Array)
                {
                    Array arr = args.Eop as Array;
                    terminator = GXCommon.GetAsByteArray(arr.GetValue(0));
                }
                else
                {
                    terminator = GXCommon.GetAsByteArray(args.Eop);
                }
                nSize = terminator.Length;
            }

            int nMinSize = (int)Math.Max(args.Count, nSize);
            if (nMinSize == 0)
            {
                nMinSize = 1;
            }
            int waitTime = args.WaitTime;
            if (waitTime <= 0)
            {
                waitTime = -1;
            }

            //Wait until reply occurred.		
            int nFound = -1;
            int LastBuffSize = 0;
            DateTime StartTime = DateTime.Now;
            bool retValue = true;
            do
            {
                if (waitTime == 0)
                {
                    //If we do not want to read all data.
                    if (!args.AllData)
                    {
                        return false;
                    }
                    retValue = false;
                    break;
                }
                if (waitTime != -1)
                {
                    waitTime -= (int)(DateTime.Now - StartTime).TotalMilliseconds;
                    StartTime = DateTime.Now;
                    if (waitTime < 0)
                    {
                        waitTime = 0;
                    }
                }
                bool received;
                lock (m_ReceivedSync)
                {
                    received = !(LastBuffSize == m_ReceivedSize || m_ReceivedSize < nMinSize);
                }
                //Do not wait if there is data on the buffer...
                if (!received)
                {
                    if (waitTime == -1)
                    {
                        received = m_ReceivedEvent.WaitOne();
                    }
                    else
                    {
                        received = m_ReceivedEvent.WaitOne(waitTime);
                    }
                    if (!received && args.Eop == null)
                    {
                        lock (m_ReceivedSync)
                        {
                            received = !(LastBuffSize == m_ReceivedSize || m_ReceivedSize < nMinSize);
                        }
                    }
                }
                if (this.Exception != null)
                {
                    Exception ex = this.Exception;
                    this.Exception = null;
                    throw ex;
                }
                //If timeout occurred.
                if (!received)
                {
                    //If we do not want to read all data.
                    if (!args.AllData)
                    {
                        return false;
                    }
                    retValue = false;
                    break;
                }
                lock (m_ReceivedSync)
                {
                    LastBuffSize = m_ReceivedSize;
                    //Read more data, if not enough
                    if (m_ReceivedSize < nMinSize)
                    {
                        continue;
                    }
                    //If only byte count matters.
                    if (nSize == 0)
                    {
                        nFound = args.Count;
                    }
                    else
                    {
                        int index = 0;//  m_LastPosition != 0 && m_LastPosition < m_ReceivedSize ? m_LastPosition : args.Count;
                        //If terminator found.
                        if (args.Eop is Array)
                        {
                            foreach (object it in args.Eop as Array)
                            {
                                byte[] term = GXCommon.GetAsByteArray(it);
                                if (term.Length != 1 && m_ReceivedSize - index < term.Length)
                                {
                                    index = m_ReceivedSize - term.Length;
                                }
                                nFound = GXCommon.IndexOf(m_Received, term, index, m_ReceivedSize);
                                if (nFound != -1)
                                {
                                    break;
                                }
                            }
                        }
                        else
                        {
                            if (terminator.Length != 1 && m_ReceivedSize - index < terminator.Length)
                            {
                                index = m_ReceivedSize - terminator.Length;
                            }
                            nFound = GXCommon.IndexOf(m_Received, terminator, index, m_ReceivedSize);
                            System.Diagnostics.Debug.WriteLine("B " + nFound);
                        }
                        m_LastPosition = m_ReceivedSize;
                        if (nFound != -1)
                        {
                            nFound += terminator.Length;
                        }
                    }
                }
            }
            while (nFound == -1);
            if (nSize == 0) //If terminator is not given read only bytes that are needed.
            {
                nFound = args.Count;
            }
            if (args.AllData) //If all data is copied.
            {
                nFound = m_ReceivedSize;
            }
            //Convert bytes to object.
            byte[] tmp = new byte[nFound];
            lock (m_ReceivedSync)
            {
                Array.Copy(m_Received, tmp, nFound);
            }
            int readBytes = 0;
            object data = GXCommon.ByteArrayToObject(tmp, typeof(T), out readBytes);
            //Remove read data.
            //Mikko
            if (m_ReceivedSize - nFound > 0)
            {
                byte[] tmp2 = new byte[m_ReceivedSize];
                Array.Copy(m_Received, 0, tmp2, 0, m_ReceivedSize);                
                System.Diagnostics.Debug.WriteLine("A" + GXCommon.ToHex(tmp2, true));
            }
            m_ReceivedSize -= nFound;
            //Received size can go less than zero if we have received data and we try to read more.
            if (m_ReceivedSize < 0)
            {
                m_ReceivedSize = 0;
            }
            if (m_ReceivedSize != 0)
            {
                lock (m_ReceivedSync)
                {
                    Array.Copy(m_Received, nFound, m_Received, 0, m_ReceivedSize);
                }
            }
            else
            {
                m_LastPosition = 0;
            }
            //Reset count after read.
            args.Count = 0;
            //Append data.
            int oldReplySize = 0;
            if (args.Reply == null)
            {
                args.Reply = (T)data;
            }
            else
            {
                if (args.Reply is Array)
                {
                    Array oldArray = args.Reply as Array;
                    Array newArray = data as Array;
                    if (newArray == null)
                    {
                        throw new ArgumentException();
                    }
                    oldReplySize = oldArray.Length;
                    int len = oldArray.Length + newArray.Length;
                    Array arr = (Array)Activator.CreateInstance(typeof(T), len);
                    //Copy old values.
                    Array.Copy(args.Reply as Array, arr, oldArray.Length);
                    //Copy new values.
                    Array.Copy(newArray, 0, arr, oldArray.Length, newArray.Length);
                    object tmp2 = arr;
                    args.Reply = (T)tmp2;
                }
                else if (args.Reply is string)
                {
                    string str = args.Reply as string;
                    str += (string)data;
                    data = str;
                    args.Reply = (T)data;
                }
            }
            return retValue;
        }
    }
}