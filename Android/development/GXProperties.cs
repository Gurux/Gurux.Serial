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

using Android.OS;
using Android.Widget;
using AndroidX.AppCompat.App;
using Java.Lang;

namespace Gurux.Serial
{
    /// <summary>
    /// Serial port properties.
    /// </summary>
    [Android.App.Activity(Name = "Gurux.Serial.GXProperties")]
    public class GXProperties : AppCompatActivity
    {
        private GXPropertiesBase _base;
        private Button _showInfo;

        /// <inheritdoc />
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activity_properties);
            _base = new GXPropertiesBase((ListView)FindViewById(Resource.Id.properties), this);
            //Show serial port info.
            _showInfo = FindViewById<Button>(Resource.Id.showInfo);
            _showInfo.Click += (sender, e) =>
            {
                try
                {
                    GXPort port = GXPropertiesBase.GetSerial().Port;
                    string info = "";
                    if (port != null)
                    {
                        info = port.GetInfo();
                    }
                    new AlertDialog.Builder(_showInfo.RootView.Context)
                            .SetTitle("Info")
                            .SetMessage(info)
                            .SetPositiveButton(Resource.String.ok, (senderAlert, args) => { })
                            .Show();
                }
                catch (Exception)
                {
                }
            };
        }

        /// <inheritdoc />
        protected override void OnDestroy()
        {
            if (_base != null)
            {
                _base.Close();
            }
            base.OnDestroy();
        }
    }
}
