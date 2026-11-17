# Gurux.Serial — .NET 10 and 11

`GXSerial` provides one asynchronous media API on Windows, Linux and Android. Windows/Linux use `System.IO.Ports` or a direct Bluetooth transport; Android uses a selected native transport. Terminal and SMS share this transport through composition. `Settings` uses legacy XML and `JsonSettings` uses JSON. UI belongs to separate MAUI, Avalonia and Blazor adapters.

The library targets `net11.0` and `net10.0`, with corresponding Windows-specific Bluetooth targets. A .NET 10 application selects the .NET 10 library asset; it does not load the .NET 11 assembly. The repository uses the .NET 11 SDK. UI adapters retain .NET 10 targets alongside .NET 11; the example application targets .NET 11.

```csharp
await using var serial = new GXSerial { PortName = "COM3", BaudRate = 115200 };
// Linux: PortName = "/dev/ttyUSB0" (the account needs device permissions).
serial.OnReceived += (_, e) => Console.WriteLine(Convert.ToHexString((byte[])e.Data!));
await serial.OpenAsync(cancellationToken);
await serial.SendAsync(new byte[] { 1, 2, 3 }, cancellationToken: cancellationToken);
```

`PortName` must be explicitly configured before opening. `Validate()` rejects an empty or whitespace-only port or accessory name.

The serial-backed examples enable Open only after the selected port is found in current discovery. They refresh availability in the background while closed, including after settings changes and device removal. `GXSerial.IsPortAvailableAsync()` checks a physical port or a known native Bluetooth identifier without opening it; `SerialPortAvailability` provides a throttled snapshot for UI polling. A listed Bluetooth device is not guaranteed to be in radio range.

Example action availability follows `OnMediaStateChange` on the UI dispatcher: Open requires Closed, while Close/Cancel and Send require Open. Opening and Closing disable all three actions. The refresh timer still updates logs and device availability, but lifecycle changes do not wait for it.

For request/reply, use `using var scope = serial.BeginSynchronous()` before sending, then `ReceiveAsync` with `ReceiveParameters<byte[]>` (count/EOP/timeout). Close, EOF and transport failures wake pending receives. A second simultaneous open is rejected; close cancels a pending open. Stream factories must honor their cancellation token. Received event handlers may close the connection.

After a remote EOF, schedule reconnection outside the `Closed` event handler and
await `CloseAsync()` before `OpenAsync()`. The EOF reader still owns its cleanup
while invoking that callback; immediate reopening from that callback is rejected.

`PortSettings` configures baud, framing, handshake, DTR and RTS. `GetAvailableBaudRates()` returns common choices, including 57600, 128000, 230400, 460800 and 921600; it does not claim to probe hardware. The desktop driver validates the chosen rate. Configure settings while closed.

## Android

Build with `dotnet build Development/Gurux.Serial.csproj -p:BuildMobile=true -f net10.0-android`.

After the app obtains the required Android permissions:

```csharp
// Paired Bluetooth Classic device; BLUETOOTH_CONNECT on Android 12+.
var bluetoothFactory = AndroidSerialTransports.BluetoothRfcomm(bluetoothDevice);
// Or: await UsbManager.RequestPermission result first, then:
var usbFactory = AndroidSerialTransports.UsbCdcAcm(usbManager, usbDevice);
await using var serial = new GXSerial(bluetoothFactory) { PortName = "accessory", BaudRate = 9600 };
await serial.OpenAsync(cancellationToken);
```

Pass the appropriate factory to GXSerial (or GXTerminal/GXSMS). RFCOMM defaults to the standard Serial Port Profile UUID, with an optional custom service UUID. The library owns and closes the socket/USB connection. USB currently supports a single CDC ACM control/data interface pair, bulk endpoints, line coding and DTR/RTS. Hardware/software handshake and vendor protocols such as FTDI, CP210x and CH34x require a separate driver and are rejected. USB detach is detected during reads; the app remains responsible for permission UI and device selection. These native adapters compile against the Android SDK; physical-device verification is still required.

An owned readable/writable `Stream` factory is also supported for other accessory drivers and deterministic tests. Physical iPhone serial is outside the current scope; no arbitrary iOS COM-port support is claimed. `BuildApple=true` includes an iOS library target for applications supplying their own supported stream; Apple builds require the appropriate Apple toolchain.

## Verify

From the GXMedias directory:

```
dotnet run --project Gurux.Serial/Tests/Serial.Tests.csproj -p:TargetFrameworks=net10.0
dotnet build Gurux.SMS/Development/Gurux.SMS.csproj -p:BuildMobile=true -f net10.0-android
```

The executable regression suite exercises real stream transport, buffering/lifecycle, AT parsing, SMS prompt exchange, PDU codecs and settings roundtrips without attached hardware. `Gurux.Serial.Example` and both bridge examples now use the modern implementation; see `docs/media-examples.md` in GXMedias.

## Desktop Bluetooth

Direct Classic RFCOMM and BLE client streams are available on Windows and Linux.
See [desktop Bluetooth configuration and requirements](docs/desktop-bluetooth.md).

## Bluetooth UART configuration

The embedded `Development/Bluetooth/devices.csv` catalogue uses
`Name;GATT Uuid;Read service;Write Service;ConfigurationService;ConfigurationCommandProvider`.
The read, write and configuration UUIDs identify characteristics in the listed GATT service.
A trailing `*` matches a device-name prefix, ignoring case. The last two columns are optional.
Windows, Android, iOS and Linux share this catalogue. Discovery matches the advertised or
operating-system device name, independently of MAC addresses, Windows native IDs,
CoreBluetooth UUIDs or BlueZ paths. `BluetoothSerialDevice.Profile` exposes the matching
GATT profile; unknown or unnamed devices return null until a known name is available.
The settings editors use catalogue profiles for GATT only. RFCOMM lists devices offering
the Serial Port service (0x1101) without requiring a catalogue entry. Its channel is
resolved automatically through SDP when opening: Windows resolves the service UUID
during connect; Linux uses the system BlueZ client library (libbluetooth.so.3).
There is no channel input in the settings editors. The transport API still accepts
an explicit channel from 1 through 30; zero selects automatic SDP resolution.
The native identifier and Service, Read, Write and Configuration UUIDs are shown in a
collapsed, read-only Device information section. Manual identifiers and UUID entry are
not supported by these editors. Saved GATT endpoints restore without scanning only when their
service and characteristic UUIDs match a catalogue row; its configuration defaults are used.
When opening, a present configured service retains that complete profile. If the service
is absent, native discovery can select the matching device catalogue service and its full
characteristic/configuration profile. The fallback never accepts a service not actually
offered by that device. A final service error lists configured, catalogue and available UUIDs.
For GATT, Find devices lists devices whose names match this catalogue or whose service metadata advertises the Serial Port UUID (0x1101). An SPP-only match can be selected for GATT metadata reading; opening a GATT serial connection still requires a catalogue profile.
For RFCOMM, the Serial Port service UUID is required and catalogue names are irrelevant.
Discovery merges UUID and name metadata across advertisement and scan-response fragments.
Device information also offers an explicit **Read GATT services** action. It temporarily
connects to the selected BLE device to list all actual service UUIDs, including services
outside the catalogue. Advertisement UUIDs are not presented as a complete service list.
The read does not write configuration or subscribe to notifications, and never changes
the selected catalogue profile. Cancellation or closing the editor awaits native cleanup.
The shipped profiles have no configuration characteristic.

Bluetooth baud rate, parity and stop-bit controls require a configuration UUID and a registered
command generator. A UUID alone or a static payload does not enable UART editing.
COM and USB settings remain editable. The shared Serial controls provide the same rule to
Terminal and SMS editors in MAUI, Avalonia and Blazor.

Different modules use different AT commands. A host can implement
`IBluetoothConfigurationCommands.GetCommands(SerialPortSettings settings)` and register the
implementation at application startup:

```csharp
BluetoothConfigurationCommands.Register(
    configurationGuid,
    () => new GXAtCommand());
```

Here `configurationGuid` is the `ConfigurationService` characteristic UUID from the catalogue,
and `GXAtCommand` is the host's device-specific implementation, not a universal built-in AT
dialect. Its method returns the complete command sequence, including required separators
and line endings. Register before showing settings or opening a device. Registration and
capability checks do not instantiate the generator; opening creates a fresh instance.

The profile automatically uses its configuration UUID to find the registered factory.
The CSV's sixth column can stay empty. An explicit `BluetoothLeProfile.ConfigurationCommandProvider`
or sixth-column name overrides the UUID default, allowing devices sharing a UUID to use
different command dialects. Named factories remain supported:

```csharp
string provider = BluetoothConfigurationCommands.Register(
    typeof(GXAtCommand), () => new GXAtCommand(), "GXAtCommand");
```

For parameterless implementations, `BluetoothConfigurationCommands.Register<GXAtCommand>()`
uses the full type name as its key. Unknown explicit names fail when opening, rather than
silently selecting the UUID default. With no selected generator, static `ConfigurationValue`
bytes are still written if provided, but the UART editing controls remain disabled.

The native BLE opening path evaluates the provider against the current settings and writes its
UTF-8 command bytes to the configuration characteristic before returning the data stream.
The generated sequence is limited to a 512-byte GATT value. Responses and pauses between
commands are device-specific; this hook formats a single write sequence and does not parse
AT replies. An explicitly supplied `ConfigurationValue` remains supported when neither a named provider
nor a UUID default is selected. Provider names survive JSON and XML settings; saved names do not instantiate or load
arbitrary classes. Each application must register its implementations before opening.

## Explicit BLE discovery

MAUI settings on Android, Windows and iOS, and Avalonia settings on Linux or a Windows-targeted host,
provide a **Find BLE devices** action. Discovery runs for ten seconds and stops on
device selection, cancellation or closing the editor. It never starts merely by
opening settings. The device list shows advertised names and stable identifiers,
including unpaired devices. Device security can still require pairing when opening
its protected GATT service. Classic Bluetooth retains the existing paired/known-device flow.

The UI-independent API is `BluetoothLeDiscovery.ScanAsync(duration, deviceFound, cancellationToken)`.
It returns at most 256 devices and suppresses late callbacks after completion.
Recent results are cached for five minutes for selected-device availability checks;
a listed device is not a guarantee that it remains in range. Windows uses an
advertisement watcher, Linux an owned BlueZ LE discovery session, Android its LE scanner,
and iOS CoreBluetooth. iOS identifiers are peripheral UUIDs rather than MAC addresses.

Android applications declare Bluetooth scan/connect permissions; older Android versions
need location permission for discovery. The supplied examples request those permissions
only from the search button. iOS examples include a localized Bluetooth usage description
under Properties/iOS/InfoPlist.strings. Known device names load their GATT profile from
the catalogue; other devices require explicit service and read/write UUIDs in settings.

CoreBluetooth also supplies native iOS notification/write connections for selected
`ble:` UUIDs. Physical COM/USB serial remains outside the iOS transport implementation.

## Linux port discovery

`GXSerial.GetPortNames()` merges runtime discovery with existing numbered `/dev/ttyS*`, `/dev/ttyUSB*`, `/dev/ttyACM*`, `/dev/ttyAMA*` and `/dev/ttymxc*` device nodes. This retains UARTs that the runtime omits because their sysfs metadata is incomplete. Names use numeric ordering and duplicates are removed. Console names, directories and nonnumeric suffixes are excluded.

Discovery lists device identities without opening a connection or changing permissions. A listed device node does not guarantee working hardware or permission to open it. The caller still needs the device permissions, commonly membership in the `dialout` group. The example settings editors and selected-port availability checks use this same discovery path.

The GATT information list omits UUIDs already displayed in the selected profile's
Service, Read, Write and Configuration fields. The complete native result remains
available through the discovery API; this is a display filter only.
