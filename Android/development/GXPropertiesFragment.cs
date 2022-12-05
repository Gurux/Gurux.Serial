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

using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using AndroidX.AppCompat.App;
using Java.Lang;

namespace Gurux.Serial
{
    /// <summary>
    /// Properties fragment.
    /// </summary>
    public class GXPropertiesFragment : AndroidX.Fragment.App.Fragment
    {
        private GXPropertiesBase _base;
        private Button _showInfo;

        /// <inheritdoc/>
        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            View view = base.OnCreateView(inflater, container, savedInstanceState);
            _base = new GXPropertiesBase((ListView)view.FindViewById(Resource.Id.properties), Activity);
            //Show serial port info.
            _showInfo = view.FindViewById<Button>(Resource.Id.showInfo);
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
            return view;
        }

        /// <inheritdoc/>
        public override void OnDestroy()
        {
            if (_base != null)
            {
                _base.Close();
            }
            base.OnDestroy();
        }
    }
}
