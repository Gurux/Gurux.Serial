# Gurux.Serial.Example

This application owns its Core, UI/Desktop (Avalonia), UI/Maui, Host and Android
transport-selection source. It references the serial transport and serial settings
adapters from source.

Open one serial connection, send text or hexadecimal bytes, and optionally wait
for a reply using a byte count, end marker and timeout.

Windows uses native MAUI and Linux uses Avalonia. Android supports USB CDC ACM and
paired Bluetooth Classic serial transports. Physical serial is unavailable on iOS.

Use the application `.csproj` with `dotnet run --project`, not the `.sln`. A multi-target application also needs an explicit `-f` framework.

From this folder:

```powershell
dotnet run --project Gurux.Serial.Example.csproj -f net11.0-windows10.0.19041.0 -p:GuruxExamplesBuild=true -p:GeneratePackageOnBuild=false
dotnet run --project Gurux.Serial.Example.csproj -f net11.0 -p:BuildDesktop=true -p:GuruxExamplesBuild=true -p:GeneratePackageOnBuild=false
dotnet build Gurux.Serial.Example.csproj -f net11.0-android -p:BuildMobile=true -p:GuruxExamplesBuild=true -p:GeneratePackageOnBuild=false
```

The local solution includes the application and its Core, Desktop and Maui projects.
Device permissions and serial hardware behavior require physical device validation.


## Build requirements

Use .NET SDK 11.0.100 or a newer stable .NET 11 feature band selected by the repository's global.json. The example and its Core, Desktop and Maui projects target both .NET 10 and .NET 11. Select the desired target with `-f`; the repository still selects the .NET 11 SDK for building, including .NET 10 outputs.

For Windows MAUI, install the matching .NET 10 or .NET 11 `maui-windows` workload. For Android, install `maui-android`; for iOS, install `maui-ios` on a Mac with matching Xcode and signing tools. Use `dotnet workload list` to check the selected SDK's workloads. Linux uses Avalonia and does not require MAUI.

Microsoft.Maui.Controls is bounded to 10.x for .NET 10 targets and 11.x for .NET 11 targets. A MAUI major-version upgrade requires matching target frameworks and workloads; updating NuGet alone is insufficient. Building all library compatibility targets also requires the .NET 10 toolchain and matching workloads.


## Platform navigation

Windows MAUI and Avalonia desktop hosts expose a top menu bar with Connection, View and Settings groups. View contains independent Data and Activity Log choices; Settings contains the media editor and Communication settings. Activity history is kept in its own view instead of Communication settings. Existing connection and data actions remain available in the Data view.

Android and iOS use a native navigation-bar Menu action and platform action sheet. Opening an editor or navigation sheet temporarily reserves input, and media settings respect connection state and active operations. Switching views preserves the media session, staged data, counters and activity history. Closing the application cancels pending operations and ignores late menu results.

## Linux publication on .NET 10

Publish the application `.csproj` from this directory with the repository-selected SDK. Do not publish the `.sln`: it also contains platform-specific MAUI projects, and a shared solution output directory can mix unrelated project outputs. A Linux runtime identifier selects the Avalonia desktop target automatically, including when publishing from Windows:

```sh
dotnet publish Gurux.Serial.Example.csproj -c Release -f net10.0 -r linux-arm64 --self-contained false -p:GuruxExamplesBuild=true -p:GeneratePackageOnBuild=false -o publish/linux-net10
```

The target Linux machine needs the .NET 10 runtime for this framework-dependent build. Use `--self-contained true` to include the runtime, or replace `linux-arm64` with `linux-x64` for an x64 destination. .NET 11 remains available with `-f net11.0`. Publishing Linux desktop output does not require MAUI.

## Project layout

The single application project stays in the example root and directly compiles the selected platform UI. Avalonia views, startup sources and Linux launcher files are in `UI/Desktop/`. Native MAUI views and startup are in `UI/Maui/`, with platform entry points, manifests and Android transport helpers under `UI/Maui/Platforms/`. These UI folders have no separate projects. Shared application logic remains in `Core/`; optional settings adapter libraries remain in the repository's separate `UI/Gurux.*` directories.

## Application icons

The Windows executable, iOS app icon and Linux desktop launcher use the example's
serial connector artwork. iOS selects the generated `appicon` asset catalog set.
After rebuilding an installed iOS app, deploy the updated app to refresh its icon.

On Linux, the window advertises `org.gurux.examples.serial` as its X11 window class,
matching the supplied desktop launcher. Register the launcher and icon for the
current user after publishing, from the published application directory:

```sh
python3 Linux/install-desktop.py
```

Keep the published application in that location, because the registered launcher
uses its absolute executable path. Run the installer again after moving it.
The installer uses `XDG_DATA_HOME`, or `~/.local/share` when it is unset, and does
not require root access.
## Android USB selection and attachment

The last selected port is saved immediately after opening or accepting media
settings, and again when the window stops. USB identity is stored in the existing
settings document under `AndroidUsbDevice`, separately from transport-library
configuration. Vendor/product IDs and an available hardware serial number let the
example restore a unique adapter when Android assigns it a different device path.
Ambiguous matching adapters require a device choice; changing the selected port
invalidates the old association.

`UI/Maui/Platforms/Android/Resources/xml/device_filter.xml` registers USB attachment identifiers
from the Gurux.Serial.Android reference and additional identifiers handled by the
current transport. The activity receives attachment intents on cold and existing
starts and uses the attached device for the next explicit Open action. Android
still controls device permissions. Filter entries route attachment notifications;
actual communication depends on the selected transport's chipset/revision support.