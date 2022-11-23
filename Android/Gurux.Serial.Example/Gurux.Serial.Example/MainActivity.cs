using System;
using Android.App;
using Android.OS;
using Android.Runtime;
using Android.Views;
using AndroidX.AppCompat.App;
using Android.Widget;
using System.Collections.Generic;
using Xamarin.Essentials;
using System.Linq;
using System.IO.Ports;
using Gurux.Common;
using System.Text;
using Android.Hardware.Usb;

namespace Gurux.Serial.Example
{
    //TODO: Add MetaData.
    [MetaData(UsbManager.ActionUsbDeviceAttached, Resource = "@xml/device_filter")]
    //TODO: Add IntentFilter.
    [IntentFilter(new[] { UsbManager.ActionUsbDeviceAttached })]
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        List<GXPort> _ports = new List<GXPort>();

        /// <summary>
        /// List of available serial ports.
        /// </summary>
        private Spinner portList;
        /// <summary>
        /// Used baud rate.
        /// </summary>
        private Spinner baudRate;
        /// <summary>
        /// Used data bits.
        /// </summary>
        private Spinner dataBits;
        /// <summary>
        /// Used parity.
        /// </summary>
        private Spinner parity;
        /// <summary>
        /// Used stop bits.
        /// </summary>
        private Spinner stopBits;

        private Button findPorts;
        private Button clearData;
        private Button showInfo;
        private Button openBtn;
        private Button sendBtn;
        private Button propertiesBtn;
        private GXSerial serial;
        private TextView receivedData;
        private EditText sendData;
        private CheckBox hex;


        ArrayAdapter<GXPort> portAdapter;

        private T GetValue<T>(int id, T def)
        {
            string value = SecureStorage.GetAsync(GetString(id)).Result;
            if (string.IsNullOrEmpty(value))
            {
                return def;
            }
            return (T)Convert.ChangeType(value, typeof(T));
        }

        private void SetValue(int id, object value)
        {
            SecureStorage.SetAsync(GetString(id), value.ToString()).Wait();
        }


        /// <summary>
        /// Read last used settings.
        /// </summary>
        private void ReadSettings()
        {
            int br = GetValue<int>(Resource.String.baudrate, 9600);
            int pos = ((ArrayAdapter)baudRate.Adapter).GetPosition(br);
            baudRate.SetSelection(pos);

            int db = GetValue<int>(Resource.String.dataBits, 8);
            pos = ((ArrayAdapter)dataBits.Adapter).GetPosition(db);
            dataBits.SetSelection(pos);

            pos = GetValue<int>(Resource.String.parity, 0);
            parity.SetSelection(pos);

            pos = GetValue<int>(Resource.String.stopBits, 0);
            stopBits.SetSelection(pos);
            hex.Checked = GetValue<bool>(Resource.String.Hex, true);
            sendData.Text = GetValue<string>(Resource.String.sendData, "");
        }

        /// <summary>
        /// Save last used settings.
        /// </summary>
        private void SaveSettings()
        {
            SetValue(Resource.String.baudrate, baudRate.SelectedItem);
            SetValue(Resource.String.dataBits, dataBits.SelectedItem);
            SetValue(Resource.String.parity, parity.SelectedItem);
            SetValue(Resource.String.stopBits, stopBits.SelectedItem);
            SetValue(Resource.String.Hex, hex.Checked);
            SetValue(Resource.String.sendData, sendData.Text);
        }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.activity_main);
            findPorts = FindViewById<Button>(Resource.Id.findPorts);
            findPorts.Click += (sender, e) =>
            {
                FindPorts();
            };
            clearData = FindViewById<Button>(Resource.Id.clearData);
            clearData.Click += (sender, e) =>
            {
                ClearData();
            };
            showInfo = FindViewById<Button>(Resource.Id.showInfo);
            showInfo.Click += (sender, e) =>
            {
                ShowInfo();
            };

            portList = FindViewById<Spinner>(Resource.Id.portList);
            portAdapter = new ArrayAdapter<GXPort>(this,
            Resource.Layout.support_simple_spinner_dropdown_item, _ports);
            portAdapter.SetDropDownViewResource(Resource.Layout.support_simple_spinner_dropdown_item);
            portList.Adapter = portAdapter;

            baudRate = FindViewById<Spinner>(Resource.Id.baudRate);
            dataBits = FindViewById<Spinner>(Resource.Id.dataBits);
            parity = FindViewById<Spinner>(Resource.Id.parity);
            stopBits = FindViewById<Spinner>(Resource.Id.stopBits);
            openBtn = FindViewById<Button>(Resource.Id.openBtn);
            openBtn.Click += (sender, e) =>
            {
                OpenSerialPort();
            };
            sendBtn = FindViewById<Button>(Resource.Id.sendBtn);
            sendBtn.Click += (sender, e) =>
            {
                SendData();
            };

            propertiesBtn = FindViewById<Button>(Resource.Id.propertiesBtn);
            propertiesBtn.Click += (sender, e) =>
            {
                serial.Properties(this);
            };

            receivedData = FindViewById<TextView>(Resource.Id.receivedData);
            sendData = FindViewById<EditText>(Resource.Id.sendData);
            hex = FindViewById<CheckBox>(Resource.Id.hex);
            try
            {
                //Add baud rates.
                int[] rates = GXSerial.GetAvailableBaudRates(null);
                ArrayAdapter<int> ratesAdapter = new ArrayAdapter<int>(this,
                        Resource.Layout.support_simple_spinner_dropdown_item, rates);
                ratesAdapter.SetDropDownViewResource(Resource.Layout.support_simple_spinner_dropdown_item);
                baudRate.Adapter = ratesAdapter;

                //Add data bits.
                List<int> dataBitsList = new List<int>
                {
                    7,
                    8
                };
                ArrayAdapter<int> dataBitsAdapter = new ArrayAdapter<int>(this,
                        Resource.Layout.support_simple_spinner_dropdown_item, dataBitsList);
                dataBitsAdapter.SetDropDownViewResource(Resource.Layout.support_simple_spinner_dropdown_item);
                dataBits.Adapter = dataBitsAdapter;

                //Add Parity.
                List<Parity> parityList = new List<Parity>
                {
                    Parity.None,
                    Parity.Odd,
                    Parity.Even,
                    Parity.Mark,
                    Parity.Space
                };
                ArrayAdapter<Parity> parityAdapter = new ArrayAdapter<Parity>(this,
                        Resource.Layout.support_simple_spinner_dropdown_item, parityList);
                parityAdapter.SetDropDownViewResource(Resource.Layout.support_simple_spinner_dropdown_item);
                parity.Adapter = parityAdapter;


                //Add stop bits.
                List<StopBits> stopBitsList = new List<StopBits>
                {
                    StopBits.One,
                    StopBits.OnePointFive,
                    StopBits.Two
                };
                ArrayAdapter<StopBits> stopBitsAdapter = new ArrayAdapter<StopBits>(this,
                        Resource.Layout.support_simple_spinner_dropdown_item, stopBitsList);
                stopBitsAdapter.SetDropDownViewResource(Resource.Layout.support_simple_spinner_dropdown_item);
                stopBits.Adapter = stopBitsAdapter;

                //Add serial ports.
                serial = new GXSerial(this);
                serial.OnError += (sender, ex) =>
                {
                    ShowError(ex);
                };
                serial.OnReceived += (sender, e) =>
                {
                    try
                    {
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            if (hex.Checked)
                            {
                                receivedData.Append(GXCommon.ToHex((byte[])e.Data));
                            }
                            else
                            {
                                receivedData.Append(ASCIIEncoding.ASCII.GetString((byte[])e.Data));
                            }
                        });
                    }
                    catch (System.Exception ex)
                    {
                        ShowError(ex);
                    }
                };
                serial.OnPortAdd += (port) =>
                {
                    openBtn.Enabled = true;
                    portAdapter.Add(port);
                    portAdapter.NotifyDataSetChanged();
                };
                serial.OnPortRemove += (port, index) =>
                {
                    portAdapter.Remove(port);
                    portAdapter.NotifyDataSetChanged();
                };
                serial.OnMediaStateChange += (sender, e) =>
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        if (e.State == MediaState.Open)
                        {
                            sendBtn.Enabled = true;
                            openBtn.Text = GetString(Resource.String.close);
                        }
                        else if (e.State == MediaState.Closed)
                        {
                            sendBtn.Enabled = false;
                            openBtn.Text = GetString(Resource.String.open);
                        }
                    });
                };
                if (!_ports.Any())
                {
                    openBtn.Enabled = false;
                }
                try
                {
                    ReadSettings();
                }
                catch (Exception)
                {
                    //Select 9600 as default baud rate value.
                    baudRate.SetSelection(5);
                    //Select 8 as default data bit value.
                    dataBits.SetSelection(1);
                    //Select NONE as default parity value.
                    parity.SetSelection(0);
                    //Select ONE as default value.
                    stopBits.SetSelection(0);
                    hex.Checked = true;
                }
            }
            catch (Exception ex)
            {
                openBtn.Enabled = false;
                ShowError(ex);
            }
        }

        private void ShowError(Exception ex)
        {
            Console.WriteLine(ex.Message);
            new Android.App.AlertDialog.Builder(this)
                    .SetTitle("Error")
                    .SetMessage(ex.Message)
                    .SetPositiveButton(GetString(Resource.String.ok), (senderAlert, args) => { })
                .Show();
        }

        /// <summary>
        /// Open selected serial port.
        /// </summary>
        public void OpenSerialPort()
        {
            try
            {
                string open = GetString(Resource.String.open);
                String close = GetString(Resource.String.close);
                if (openBtn.Text == open)
                {
                    serial.Port = Cast<GXPort>(portList.SelectedItem);
                    serial.BaudRate = int.Parse(baudRate.SelectedItem.ToString());
                    serial.DataBits = int.Parse(dataBits.SelectedItem.ToString());
                    serial.Parity = Enum.Parse<Parity>(parity.SelectedItem.ToString());
                    serial.StopBits = Enum.Parse<StopBits>(stopBits.SelectedItem.ToString());
                    serial.Open();
                }
                else
                {
                    serial.Close();
                }
            }
            catch (Exception ex)
            {
                serial.Close();
                ShowError(ex);
            }
        }

        /// <summary>
        /// Send data to the serial port.
        /// </summary>
        public void SendData()
        {
            try
            {
                string str = sendData.Text;
                if (hex.Checked)
                {
                    serial.Send(GXCommon.HexToBytes(str));
                }
                else
                {
                    serial.Send(str);
                }
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        /// <summary>
        /// Find serial ports.
        /// </summary>
        public void FindPorts()
        {
            try
            {
                portAdapter.Clear();
                portAdapter.AddAll(serial.GetPorts());
                portAdapter.NotifyDataSetChanged();
                if (portAdapter.Count == 0)
                {
                    throw new Exception("No serial ports available.");
                }
                openBtn.Enabled = true;
            }
            catch (Exception ex)
            {
                openBtn.Enabled = false;
                ShowError(ex);
            }
        }

        static T Cast<T>(Java.Lang.Object? obj) where T : class
        {
            var propertyInfo = obj.GetType().GetProperty("Instance");
            return propertyInfo == null ? null : propertyInfo.GetValue(obj, null) as T;
        }

        /// <summary>
        /// Show serial port info.
        /// </summary>
        public void ShowInfo()
        {
            try
            {
                if (portList.SelectedItem != null)
                {
                    GXPort port = Cast<GXPort>(portList.SelectedItem);
                    String info = "";
                    if (port != null)
                    {
                        info = port.GetInfo();
                    }
                    new Android.App.AlertDialog.Builder(this)
                            .SetTitle("Info")
                            .SetMessage(info)
                            .SetPositiveButton(GetString(Resource.String.ok), (senderAlert, args) => { })
                        .Show();
                }
            }
            catch (Exception ex)
            {
                openBtn.Enabled = false;
                ShowError(ex);
            }
        }

        /// <summary>
        /// Clear received data.
        /// </summary>
        public void ClearData()
        {
            try
            {
                receivedData.Text = "";
            }
            catch (Exception ex)
            {
                openBtn.Enabled = false;
                ShowError(ex);
            }
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.menu_main, menu);
            return true;
        }

        /// <inheritdoc />
        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            int id = item.ItemId;
            if (id == Resource.Id.action_settings)
            {
                return true;
            }

            return base.OnOptionsItemSelected(item);
        }

        /// <inheritdoc />
        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        /// <inheritdoc />
        protected override void OnResume()
        {
            Console.WriteLine("OnResume");
            portAdapter.Clear();
            portAdapter.AddAll(serial.GetPorts());
            portAdapter.NotifyDataSetChanged();
            openBtn.Enabled = portAdapter.Count != 0;
            base.OnResume();
        }

        /// <inheritdoc />
        protected override void OnPause()
        {
            Console.WriteLine("OnPause");
            base.OnPause();
            try
            {
                serial.Close();
            }
            catch (Exception)
            {
                //It's OK if this fails.
            }
        }
    }
}
