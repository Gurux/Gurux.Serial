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
    /// <p>Join the Gurux Community or follow <a href="https://twitter.com/guruxorg" title="@Gurux">@Gurux</a> for project updates.</p>
    /// <p>Open Source GXSerial media component, made by Gurux Ltd, is a part of GXMedias set of media components, which programming interfaces help you implement communication by chosen connection type. Our media components also support the following connection types: network, terminal.</p>
    /// <p>For more info check out <a href="http://www.gurux.org/" title="Gurux">Gurux</a>.</p>
    /// <p>We are updating documentation on Gurux web page. </p>
    /// <p>If you have problems you can ask your questions in Gurux <a href="http://www.gurux.org/forum">Forum</a>.</p>
    /// <h1><a name="simple-example" class="anchor" href="#simple-example"><span class="mini-icon mini-icon-link"></span></a>Simple example</h1>
    /// <p>Before use you must set following settings:</p>
    /// <ul>
    /// <li><see cref="Gurux.Serial.GXSerial.PortName"/></li>
    /// <li><see cref="Gurux.Serial.GXSerial.BaudRate"/></li>
    /// <li><see cref="Gurux.Serial.GXSerial.DataBits"/></li>
    /// <li><see cref="Gurux.Serial.GXSerial.Parity"/></li>
    /// <li><see cref="Gurux.Serial.GXSerial.StopBits"/></li>
    /// </ul><p>It is also good to listen following events:</p>
    /// <ul>
    /// <li><see cref="Gurux.Serial.GXSerial.OnError"/></li>
    /// <li><see cref="Gurux.Serial.GXSerial.OnReceived"/></li>
    /// <li><see cref="Gurux.Serial.GXSerial.OnMediaStateChange"/></li>
    /// </ul>
    /// <example>
    /// <code>
    /// GXSerial cl = new GXSerial();
    /// cl.PortName = Gurux.Serial.GXSerial.GetPortNames()[0];
    /// cl.BaudRate = 9600;
    /// cl.DataBits = 8;
    /// cl.Parity = System.IO.Ports.Parity.Odd;
    /// cl.StopBits = System.IO.Ports.StopBits.One;
    /// cl.Open();
    /// </code>
    /// </example>
    /// Data is send with send command.
    /// <example>
    /// <code>
    /// cl.Send("Hello World!");
    /// </code>
    /// </example>
    /// In default mode received data is coming as asynchronously from OnReceived event.
    /// <example>
    /// <code>
    /// cl.OnReceived += new ReceivedEventHandler(this.OnReceived);
    /// </code>
    /// </example>
    /// Data can be send as syncronous if needed:
    /// <example>
    /// <code lang="csharp">
    /// lock (cl.Synchronous)
    /// {
    ///     string reply = "";
    ///     ReceiveParameters&lt;string&gt; p = new ReceiveParameters&lt;string&gt;()
    ///     //ReceiveParameters&lt;byte[]&gt; p = new ReceiveParameters&lt;byte[]&gt;()
    ///     {
    ///         //Wait time tells how long data is waited.
    ///         WaitTime = 1000,
    ///         //Eop tells End Of Packet charachter.
    ///         p.Eop = '\\r'
    ///     }      
    ///     gxSerial1.Send("Hello World!");
    ///     if (gxSerial1.Receive(p))
    ///     {
    ///         reply = Convert.ToString(p.Reply)
    ///     }
    /// }
    /// </code>
    /// </example>
    /// </summary>
    [System.Runtime.CompilerServices.CompilerGenerated]
    class NamespaceDoc
    {
    }
}