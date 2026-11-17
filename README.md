# Gurux.Serial

Gurux.Serial provides asynchronous serial communication for .NET 10 and .NET 11. The same `GXSerial` API supports operating-system serial ports, Bluetooth serial connections and application-supplied accessory streams. Gurux.Terminal and Gurux.SMS reuse this transport.

The transport library has no dependency on MAUI, Avalonia, Blazor or WinForms. Settings editors and example application views live in separate projects.

## Packages and requirements

| Package | Purpose |
| --- | --- |
| Gurux.Serial | UI-independent transport, settings and shared receive integration. |
| Gurux.Serial.Avalonia | Optional Avalonia settings editor. |
| Gurux.Serial.Blazor | Optional Blazor settings components. |
| Gurux.Serial.Maui | Optional native MAUI settings editor. |

This README describes the **9.0 API**. Earlier 8.x packages use different APIs; use a matching 9.0 package release or build the current source.

The library targets `net10.0` and `net11.0`, with matching Windows-specific Bluetooth targets. Android targets are enabled with `BuildMobile=true`; `BuildApple=true` also enables iOS targets. Applications select the matching library asset for their target framework.

Building from source uses the stable .NET 11 SDK selected by [global.json](global.json), including for .NET 10 outputs. Native mobile builds require the corresponding SDK workloads; iOS builds also require the appropriate Apple toolchain.

## Platform support

| Platform | Available transports | Requirements and limits |
| --- | --- | --- |
| Windows | COM ports, Bluetooth Classic RFCOMM, BLE GATT | Device drivers and Bluetooth permissions. Native BLE requires the Windows-specific library target. |
| Linux | Serial device nodes, Bluetooth Classic RFCOMM, BLE GATT | Device permissions; Bluetooth uses BlueZ, with `libbluetooth.so.3` for automatic RFCOMM channel resolution. |
| macOS | Serial ports exposed by the operating system | Device drivers and access permissions. The desktop direct Bluetooth implementations target Windows and Linux. |
| Android | Native Bluetooth Classic, BLE and USB host serial adapters | Application-managed runtime permissions, device selection and the appropriate native stream factory. |
| iOS | Native CoreBluetooth BLE or an application-supplied supported accessory stream | Bluetooth usage description and Apple tooling. Arbitrary COM ports and physical USB serial are not exposed by this implementation. |

Compilation and injected-stream tests do not establish physical device compatibility. Validate the selected adapter, firmware, permissions and background behavior on the target device.

## Open a serial port

This example opens a Windows serial port, sends three bytes and briefly prints incoming data as hexadecimal. Replace `COM3` with the selected device name, such as `/dev/ttyUSB0` on Linux.

~~~csharp
using Gurux.Serial;

using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
await using var serial = new GXSerial
{
    PortName = "COM3",
    BaudRate = 115200
};

serial.OnReceived += (_, args) =>
{
    if (args.Data is byte[] data)
    {
        Console.WriteLine(Convert.ToHexString(data));
    }
};

await serial.OpenAsync(timeout.Token);
await serial.SendAsync(new byte[] { 1, 2, 3 }, cancellationToken: timeout.Token);
await Task.Delay(TimeSpan.FromSeconds(1), timeout.Token);
await serial.CloseAsync();
~~~

The delay keeps the event-driven example alive briefly; it does not identify a complete protocol reply. For request/reply protocols, use buffered receive.

`PortName` must be explicitly configured. `Validate()` reports localized settings errors without opening the device; `OpenAsync()` rejects invalid settings.

## Buffered request and reply

Enter buffered mode **before sending** so that a fast response is not lost. Keep the scope alive across both awaited operations.

~~~csharp
using Gurux.Common;
using Gurux.Serial;

using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
await using var serial = new GXSerial { PortName = "COM3", BaudRate = 115200 };
await serial.OpenAsync(timeout.Token);

using (serial.BeginSynchronous())
{
    var reply = new ReceiveParameters<byte[]>
    {
        Count = 3,
        WaitTime = 5000
    };

    await serial.SendAsync(new byte[] { 1, 2, 3 }, cancellationToken: timeout.Token);
    if (await serial.ReceiveAsync(reply, timeout.Token) && reply.Reply is byte[] response)
    {
        Console.WriteLine(Convert.ToHexString(response));
    }
}
~~~

This example expects three reply bytes. Set `Count`, `Eop` and `WaitTime` to match your protocol. Delimiters can span incoming fragments; a completed delimiter-based reply includes the terminator bytes.

Timeout or closure returns `false`; cancellation throws `OperationCanceledException`. With `AllData` enabled, a failed receive can still contain partial data. Do not share one receive-parameter object between concurrent operations.

`BeginSynchronous()` enables buffering without holding a Monitor lock across `await`. The legacy `Synchronous` lock is suitable only for entirely blocking exchanges.

## Serial settings

Configure the device and line settings before opening.

| Setting | Default | Purpose |
| --- | --- | --- |
| `PortName` | Unset | Operating-system port name or selected accessory identifier. |
| `BaudRate` | `9600` | Requested baud rate; the driver/device determines support. |
| `DataBits` | `8` | Payload bits per serial frame; supported validation range is 5–8. |
| `Parity` | `None` | Parity checking. |
| `StopBits` | `One` | Stop interval. |
| `Handshake` | `None` | Hardware/software flow control policy. |
| `DtrEnable` / `RtsEnable` | `false` | Manually controlled modem signals, where supported. |
| `PortSettings.BluetoothChannel` | `0` | Desktop RFCOMM automatic SDP resolution; 1–30 selects an explicit channel. |
| `PortSettings.BluetoothProfile` | `null` | BLE service and characteristic profile. Required for `ble:` identifiers. |

The properties on `GXSerial` forward to its `PortSettings` object. `GetAvailableBaudRates()` returns conventional choices, including 57600, 128000, 230400, 460800 and 921600; it does not probe hardware.

`Settings` stores legacy XML configuration, including the `Port`, `Bps` and `ByteSize` names. `JsonSettings` stores source-generated JSON. Both preserve Bluetooth profile configuration; replacing either requires closed media with no pending open or cleanup operation.

### Changing settings while connected

Use `ApplySettingsAsync()` to commit supported live changes. Changing properties on `PortSettings` alone does not apply them to active hardware.

~~~csharp
using Gurux.Serial;

using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
await using var serial = new GXSerial { PortName = "COM3", BaudRate = 9600 };
await serial.OpenAsync(timeout.Token);

var settings = new SerialPortSettings
{
    PortName = serial.PortName,
    BaudRate = 115200,
    DataBits = serial.DataBits,
    Parity = serial.Parity,
    StopBits = serial.StopBits,
    Handshake = serial.Handshake,
    DtrEnable = serial.DtrEnable,
    RtsEnable = serial.RtsEnable,
    BluetoothChannel = serial.PortSettings.BluetoothChannel,
    BluetoothProfile = serial.PortSettings.BluetoothProfile
};

await serial.ApplySettingsAsync(settings, timeout.Token);
~~~

The request is copied before asynchronous work. It cannot change the active device identity, Bluetooth channel or profile. An open desktop serial port applies framing and modem-signal changes through its driver; an injected transport must implement `IGXSerialSettings` to support live updates. Unsupported live changes throw `NotSupportedException`. While closed, the method stages the validated settings.

If applying physical settings fails, the implementation attempts to restore the previous configuration and reports recovery failure if restoration also fails.

## Device discovery and availability

`GXSerial.GetPortNames()` lists physical ports and supported platform identifiers. `GXSerial.IsPortAvailableAsync()` checks whether a selected identity is currently listed without opening it. `SerialPortAvailability` supplies throttled snapshots for UI polling.

A discovered identity does not guarantee access permissions, functioning hardware or Bluetooth radio range.

On Linux, discovery merges runtime results with existing numbered `/dev/ttyS*`, `/dev/ttyUSB*`, `/dev/ttyACM*`, `/dev/ttyAMA*` and `/dev/ttymxc*` nodes. Names are deduplicated and naturally ordered. Console names, directories and nonnumeric suffixes are excluded. The account still needs device access, commonly through the `dialout` group.

Android discovery lists USB host identities (`usb:`), permitted paired Bluetooth identities (`bluetooth:`) and recent BLE identities (`ble:`). iOS uses CoreBluetooth peripheral UUIDs rather than MAC addresses.

## Desktop Bluetooth

Windows and Linux support direct Classic RFCOMM and BLE GATT streams.

- `bluetooth:` identifiers select Classic RFCOMM. Channel zero resolves the Serial Port service through SDP; channels 1–30 select an explicit channel. Windows resolves the service UUID during connect; Linux uses BlueZ.
- `ble:` identifiers select a GATT connection using `PortSettings.BluetoothProfile`.
- `BluetoothSerialTransports.GetDevicesAsync()` enumerates known devices for the selected `BluetoothSerialTransport`.
- Windows native BLE requires a Windows-specific target such as `net10.0-windows10.0.19041.0`. Linux uses its BlueZ backend.

The settings editors list RFCOMM devices offering the Serial Port service (0x1101), without requiring a catalogue-name match. They resolve the channel automatically and do not expose a manual channel field.

## Android adapters

Build the Android library target from this repository directory:

~~~powershell
dotnet build Development/Gurux.Serial.csproj -f net10.0-android -p:BuildMobile=true -p:GeneratePackageOnBuild=false
~~~

After obtaining permissions and selecting the native device, pass an adapter factory to `new GXSerial(factory)`. Configure a nonempty `PortName` identifying the selected accessory.

| Factory | Purpose |
| --- | --- |
| `AndroidSerialTransports.BluetoothRfcomm(device)` | Connects a paired Bluetooth Classic device using the standard Serial Port Profile UUID by default. Supports a custom service UUID and an explicit insecure-socket option. |
| `AndroidSerialTransports.BluetoothLe(context, device, profile)` | Connects and subscribes to the selected BLE service and characteristics. |
| `AndroidSerialTransports.Usb(manager, device)` | Detects a known USB serial chipset or falls back to single-port CDC ACM. |
| `AndroidSerialTransports.UsbCdcAcm(manager, device)` | Explicitly selects a single CDC ACM control/data interface pair. |

The USB adapter includes CDC ACM, FTDI, CP210x, CH340/CH341 and legacy Prolific PL2303 implementations. The `Usb` overload accepts an explicit `AndroidUsbChipset` and port index for supported vendor adapters. Explicit selection can accommodate rebranded identifiers but does not guarantee compatibility with every hardware revision.

CDC ACM supports a single port. Vendor adapters select interfaces with bulk IN/OUT endpoints. Line settings and manual DTR/RTS are supported where implemented; automatic hardware/software handshaking is rejected by these USB adapters.

Applications own permission requests and device-selection UI. Wait for USB permission before opening. Bluetooth Classic requires `BLUETOOTH_CONNECT` on Android 12 and later; BLE discovery requires the corresponding scan/connect permissions and location permission on older Android versions where applicable.

The returned stream owns the socket or USB connection. USB detach is observed during reads. The same stream factories can be passed to `GXTerminal` and `GXSMS`.

## BLE discovery and profiles

`BluetoothLeDiscovery.ScanAsync(duration, deviceFound, cancellationToken)` performs explicit discovery without opening a serial data connection. The default duration is ten seconds, with a maximum of one minute and at most 256 distinct results. Completion suppresses late callbacks; recent results are cached for five minutes for availability checks.

Windows uses an advertisement watcher, Linux an owned BlueZ LE discovery session, Android its LE scanner and iOS CoreBluetooth. Discovery can find unpaired devices, although protected characteristics can require pairing when opened.

The native example editors start scanning from the search action, not merely by opening settings. Selection, cancellation or closing the editor stops discovery. UI updates must use the application dispatcher.

### Shared device catalogue

The embedded [Bluetooth device catalogue](Development/Bluetooth/devices.csv) is shared by Windows, Linux, Android and iOS. Its columns are:

~~~text
Name;GATT Uuid;Read service;Write Service;ConfigurationService;ConfigurationCommandProvider;ConfigurationBytesHex
~~~

The read, write and configuration UUID columns identify characteristics in the GATT service. A trailing `*` matches a device-name prefix, ignoring case. The optional columns describe configuration generation or static bytes.

Profiles match advertised or operating-system names independently of MAC addresses, Windows native IDs, CoreBluetooth UUIDs and BlueZ paths. `BluetoothSerialDevice.Profile` exposes the matching profile. Unknown or unnamed devices have no automatic profile until a known name becomes available.

The editors use catalogue profiles for GATT and show native identifiers and profile UUIDs as read-only device information. They do not provide manual identifier/UUID entry. Applications can supply their own `BluetoothLeProfile` programmatically.

Saved GATT endpoints restore without scanning only when their service and characteristic UUIDs match a catalogue row; the row supplies configuration defaults. If the configured service is absent when opening, native discovery can select the device's catalogue service and its complete characteristic/configuration profile. It never accepts a service the device does not actually offer. A final service error reports configured, catalogue and available UUIDs.

For GATT selection, the editors list catalogue-name matches or devices advertising the Serial Port UUID (0x1101). An SPP-only match can be inspected, but opening GATT serial still requires a supported profile. Discovery merges UUID and name metadata across advertisement and scan-response fragments.

The explicit **Read GATT services** action temporarily connects to list actual services, including those outside the catalogue. Advertisement UUIDs are not treated as a complete service list. This operation does not configure UART settings, subscribe to notifications or replace the selected profile. Cancellation waits for native cleanup. The information list omits UUIDs already shown in profile fields; the discovery API retains the full result.

### Device-specific UART configuration

BLE has no universal baud-rate or parity configuration protocol. The editors enable Bluetooth UART editing only when the profile has a configuration characteristic and an application-registered command generator. A characteristic UUID alone or static configuration bytes do not enable those controls. COM and USB settings remain editable. Terminal and SMS reuse the same editor rules.

Implement `IBluetoothConfigurationCommands.GetCommands(SerialPortSettings settings)` for the module's command dialect and register its factory before displaying settings or opening the device:

- `BluetoothConfigurationCommands.Register(configurationGuid, factory)` associates a default generator with a configuration characteristic UUID.
- `BluetoothConfigurationCommands.Register(type, factory, name)` registers a named generator.
- `BluetoothConfigurationCommands.Register<T>()` registers a parameterless implementation using its full type name.

Registration and capability checks do not instantiate generators; opening creates a fresh instance. An explicit `BluetoothLeProfile.ConfigurationCommandProvider` or sixth-column catalogue name overrides the UUID default. Unknown explicit names fail when opening rather than falling back silently.

The generator returns one complete command sequence, including separators and line endings. Opening writes its UTF-8 bytes to the configuration characteristic before returning the data stream. The payload is limited to 512 bytes. This hook does not parse replies or schedule device-specific pauses between commands.

When no generator is selected, explicitly supplied `ConfigurationValue` bytes remain supported. Provider names survive XML/JSON settings, but saved names never load arbitrary classes: each application must register its implementations. The shipped catalogue profiles currently have no configuration characteristic.

## iOS and custom transports

iOS supplies native notification/write connections for selected `ble:` peripheral UUIDs and a configured `BluetoothLeProfile`. The application must include its Bluetooth usage description and request appropriate authorization.

For other supported accessory drivers, construct `GXSerial` with a `Func<SerialPortSettings, CancellationToken, ValueTask<Stream>>`. The factory must honor cancellation and return a readable, writable stream owned by the media. Closing/disposal releases that stream. An MFi/ExternalAccessory integration must supply its own supported adapter; this library does not expose arbitrary iPhone COM ports.

## Lifecycle and threading

Prefer `OpenAsync`, `SendAsync`, `ReceiveAsync` and `CloseAsync` in asynchronous applications. Blocking APIs remain available for compatibility.

A second simultaneous open is rejected. Closing cancels an in-progress open, closes the active transport and wakes pending receives. Remote EOF and transport failures also wake buffered receives.

Receive handlers may close the connection. After remote EOF, schedule reconnection outside the `Closed` event handler and await `CloseAsync()` before calling `OpenAsync()`. The reader still owns cleanup while invoking its close notification, so immediate reopening there is rejected.

Events are not UI-dispatched. Keep handlers short, marshal UI changes to the platform dispatcher and never hold a Monitor lock across `await`.

`IGXMediaIconProvider.OpenIcon()` opens the embedded PNG as a new caller-owned stream without a UI framework dependency.

## Example application

[Gurux.Serial.Example](Gurux.Serial.Example/Gurux.Serial.Example.csproj) owns its Core logic, Avalonia desktop views and native MAUI views. It targets both .NET 10 and .NET 11. The views include connection controls, buffered request/reply, traffic counters, device discovery and activity history.

Run the Avalonia desktop example from this repository directory:

~~~powershell
dotnet run --project Gurux.Serial.Example/Gurux.Serial.Example.csproj -f net11.0 -p:BuildDesktop=true -p:GuruxExamplesBuild=true -p:GeneratePackageOnBuild=false
~~~

Build its Android host:

~~~powershell
dotnet build Gurux.Serial.Example/Gurux.Serial.Example.csproj -f net11.0-android -p:BuildMobile=true -p:GuruxExamplesBuild=true -p:GeneratePackageOnBuild=false
~~~

The serial-backed examples refresh device availability while closed. Physical-port Open actions require an available selected port; restored BLE selections can permit an explicit retry without a new scan. Lifecycle actions follow media-state notifications on the UI dispatcher. A discovered identity is not a guarantee that opening will succeed.

## Build and test

Run from this repository directory:

~~~powershell
dotnet build Development/Gurux.Serial.csproj -p:GeneratePackageOnBuild=false
dotnet run --project Tests/Serial.Tests.csproj -p:GeneratePackageOnBuild=false
~~~

The regression executable exercises injected stream transports, receive buffering, cancellation and lifecycle behavior, live settings, AT modem parsing, SMS prompt exchange, PDU codecs and settings round trips without attached hardware.

Additional focused projects cover [Bluetooth profiles and configuration](Tests/Bluetooth/Bluetooth.Tests.csproj), [desktop Bluetooth](Tests/DesktopBluetooth/DesktopBluetooth.Tests.csproj), [Linux BLE](Tests/DesktopLinux/LinuxBluetoothLeTests.csproj), and [Android USB protocols](Tests/Android/Usb/UsbProtocolTests.csproj). Use each project's platform requirements when running it.

Headless tests and successful native compilation do not replace physical COM/USB/Bluetooth checks, real SMS/modem validation or mobile background testing.

## Migrating from 8.x

The 9.0 implementation uses current Gurux.Common contracts, shared receive buffering and asynchronous lifecycle/cancellation APIs. Optional settings adapters replace bundled platform dialogs.

Use `IGXSerialSettings.ApplySettingsAsync()` for supported live line changes. Use an explicit native stream factory for Android accessories and a supported BLE/accessory path on iOS.

## License

Gurux.Serial is available under either [GPL-2.0-only](LICENSE) or a commercial license from Gurux Ltd. See [commercial licensing information](LICENSES/LicenseRef-Gurux-Commercial) or contact [Gurux Ltd](https://www.gurux.fi/).
