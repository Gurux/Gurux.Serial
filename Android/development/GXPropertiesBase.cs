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

using Android.App;
using Android.Content;
using Android.Widget;
using Gurux.Serial.Enums;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System;

namespace Gurux.Serial
{
    internal class GXPropertiesBase
    {
        private readonly Context activity;
        private readonly ListView listView;
        private List<string> rows = new List<string>();
        private static GXSerial serial;

        public GXPropertiesBase(ListView lv, Context c)
        {
            activity = c;
            listView = lv;
            if (serial.Port == null)
            {
                GXPort[] ports = serial.GetPorts();
                if (ports.Any())
                {
                    serial.Port = ports[0];
                }
            }
            rows.Add(GetPort());
            rows.Add(GetBaudRate());
            rows.Add(GetDataBits());
            rows.Add(GetParity());
            rows.Add(GetStopBits());
            rows.Add(GetChipset());

            //Add serial port settings.
            ArrayAdapter<string> adapter = new ArrayAdapter<string>(activity,
                    Resource.Layout.support_simple_spinner_dropdown_item, rows);
            listView.Adapter = adapter;
            listView.ItemClick += (sender, e) =>
            {
                switch (e.Position)
                {
                    case 0:
                        UpdatePort();
                        break;
                    case 1:
                        UpdateBaudRate();
                        break;
                    case 2:
                        UpdateDataBits();
                        break;
                    case 3:
                        UpdateParity();
                        break;
                    case 4:
                        UpdateStopBits();
                        break;
                    case 5:
                        UpdateChipset();
                        break;
                    default:
                        //Do nothing.
                        break;
                };
            };
        }

        public void Close()
        {
        }

        public static GXSerial GetSerial()
        {
            return serial;
        }

        public static void SetSerial(GXSerial value)
        {
            serial = value;
        }

        private string GetPort()
        {
            return activity.GetString(Resource.String.port) + "\r\n" + serial.Port;
        }

        private string GetBaudRate()
        {
            return activity.GetString(Resource.String.baudRate) + "\r\n" + serial.BaudRate;
        }

        private string GetDataBits()
        {
            return activity.GetString(Resource.String.dataBits) + "\r\n" + serial.DataBits;
        }

        private string GetParity()
        {
            return activity.GetString(Resource.String.parity) + "\r\n" + serial.Parity;
        }

        private string GetStopBits()
        {
            string value = "1";
            if (serial.StopBits == StopBits.Two)
            {
                value = "2";
            }
            return activity.GetString(Resource.String.stopBits) + "\r\n" + value;
        }

        private string GetChipset()
        {
            if (serial._Chipset != null)
            {
                return activity.GetString(Resource.String.chipset) + "\r\n" + serial._Chipset.Chipset.ToString();
            }
            return "";
        }


        /// <summary>
        /// Update serial port.
        /// </summary>
        private void UpdatePort()
        {
            AlertDialog.Builder builder = new AlertDialog.Builder(activity);
            GXPort[] values = serial.GetPorts();
            string[] tmp = new string[values.Length];
            GXPort actual = serial.Port;
            int selected = -1;
            int pos = 0;
            foreach (GXPort it in values)
            {
                tmp[pos] = it.ToString();
                //Find selected item.
                if (actual != null && actual.Port.Equals(it.Port))
                {
                    selected = pos;
                }
                ++pos;
            }
            if (values.Length != 0)
            {
                builder.SetTitle(Resource.String.port)
                        .SetSingleChoiceItems(tmp, selected, new EventHandler<DialogClickEventArgs>(delegate (object sender, DialogClickEventArgs e)
                {
                    serial.Port = values[e.Which];
                    rows[0] = GetPort();
                    rows[5] = GetChipset();
                    ArrayAdapter<string> adapter = new ArrayAdapter<string>(activity,
                    Resource.Layout.support_simple_spinner_dropdown_item, rows);
                    listView.Adapter = adapter;
                    var d = (sender as AlertDialog);
                    d.Dismiss();
                }
            )).Show();
            }
            else
            {
                serial.Port = null;
                rows[0] = GetPort();
                rows[5] = GetChipset();
                ArrayAdapter<string> adapter = new ArrayAdapter<string>(activity,
                Resource.Layout.support_simple_spinner_dropdown_item, rows);
                listView.Adapter = adapter;
            }
        }

        /**
         * Update baud rate.
         */
        private void UpdateBaudRate()
        {
            AlertDialog.Builder builder = new AlertDialog.Builder(activity);
            int[] tmp = GXSerial.GetAvailableBaudRates(serial.Port.ToString());
            string[] values = new string[tmp.Length];
            int actual = serial.BaudRate;
            int selected = -1;
            int pos = 0;
            foreach (int it in tmp)
            {
                values[pos] = it.ToString();
                //Get selected item.
                if (actual == it)
                {
                    selected = pos;
                }
                ++pos;
            }
            builder.SetTitle(Resource.String.baudRate)
                    .SetSingleChoiceItems(values, selected, new EventHandler<DialogClickEventArgs>(delegate (object sender, DialogClickEventArgs e)
                    {
                        serial.BaudRate = tmp[e.Which];
                        rows[1] = GetBaudRate();
                        ArrayAdapter<string> adapter = new ArrayAdapter<string>(activity,
                        Resource.Layout.support_simple_spinner_dropdown_item, rows);
                        listView.Adapter = adapter;
                        //Close wnd.
                        var d = (sender as AlertDialog);
                        d.Dismiss();
                    })).Show();
        }


        /// <summary>
        /// Update data bits.
        /// </summary>
        private void UpdateDataBits()
        {
            AlertDialog.Builder builder = new AlertDialog.Builder(activity);
            int[] tmp = new int[] { 6, 7, 8 };
            string[] values = new string[tmp.Length];
            int actual = serial.DataBits;
            int selected = -1;
            int pos = 0;
            foreach (int it in tmp)
            {
                values[pos] = it.ToString();
                //Get selected item.
                if (actual == it)
                {
                    selected = pos;
                }
                ++pos;
            }
            builder.SetTitle(Resource.String.dataBits)
                    .SetSingleChoiceItems(values, selected, new EventHandler<DialogClickEventArgs>(delegate (object sender, DialogClickEventArgs e)
            {
                serial.DataBits = tmp[e.Which];
                rows[2] = GetDataBits();
                ArrayAdapter<string> adapter = new ArrayAdapter<string>(activity,
                Resource.Layout.support_simple_spinner_dropdown_item, rows);
                listView.Adapter = adapter;
                //Close wnd.
                var d = (sender as AlertDialog);
                d.Dismiss();
            })).Show();
        }

        /// <summary>
        /// Update parity.
        /// </summary>
        private void UpdateParity()
        {
            AlertDialog.Builder builder = new AlertDialog.Builder(activity);
            int actual = (int)serial.Parity;
            string[] values = new string[] { "None", "Odd", "Even", "Mark", "Space" };
            builder.SetTitle(Resource.String.parity)
                    .SetSingleChoiceItems(values, actual, new EventHandler<DialogClickEventArgs>(delegate (object sender, DialogClickEventArgs e)
                    {
                        serial.Parity = System.Enum.Parse<Parity>(values[e.Which]);
                        rows[3] = GetParity();
                        ArrayAdapter<string> adapter = new ArrayAdapter<string>(activity,
                        Resource.Layout.support_simple_spinner_dropdown_item, rows);
                        listView.Adapter = adapter;
                        //Close wnd.
                        var d = (sender as AlertDialog);
                        d.Dismiss();
                    })).Show();
        }

        /**
         * Update stop bits.
         */
        private void UpdateStopBits()
        {
            AlertDialog.Builder builder = new AlertDialog.Builder(activity);
            string[] values = new string[] { "1", "2" };
            int actual = 0;
            if (serial.StopBits == StopBits.Two)
            {
                actual = 1;
            }
            builder.SetTitle(Resource.String.stopBits)
                    .SetSingleChoiceItems(values, actual, new EventHandler<DialogClickEventArgs>(delegate (object sender, DialogClickEventArgs e)
            {
                StopBits tmp;
                if (e.Which == 0)
                {
                    tmp = StopBits.One;
                }
                else
                {
                    tmp = StopBits.Two;
                }
                serial.StopBits = tmp;
                rows[4] = GetStopBits();
                ArrayAdapter<string> adapter = new ArrayAdapter<string>(activity,
                Resource.Layout.support_simple_spinner_dropdown_item, rows);
                listView.Adapter = adapter;
                //Close wnd.
                var d = (sender as AlertDialog);
                d.Dismiss();

            })).Show();
        }

        /**
         * Update chipset.
         */
        private void UpdateChipset()
        {
            AlertDialog.Builder builder = new AlertDialog.Builder(activity);
            List<string> values = new List<string>();
            int selected = (int)serial._Chipset.Chipset;
            foreach (var it in System.Enum.GetValues(typeof(Chipset)))
            {
                values.Add(it.ToString());
            }
            builder.SetTitle(Resource.String.chipset)
            .SetSingleChoiceItems(values.ToArray(), selected, new EventHandler<DialogClickEventArgs>(delegate (object sender, DialogClickEventArgs e)
    {
        serial.Chipset = System.Enum.Parse<Chipset>(values[e.Which]); ;
        rows[5] = GetChipset();
        ArrayAdapter<string> adapter = new ArrayAdapter<string>(activity,
        Resource.Layout.support_simple_spinner_dropdown_item, rows);
        listView.Adapter = adapter;
        //Close wnd.
        var d = (sender as AlertDialog);
        d.Dismiss();

    })).Show();
        }
    }
}
