# Gurux.Serial.Maui

## Why use this component?

Use this optional package to add a ready-made settings editor for `GXSerial` to an existing native .NET MAUI application. It supplies localized fields, input validation and a staged Save/Cancel workflow so the host does not have to implement the media settings form itself.

This adapter edits serial device selection, line settings and supported Bluetooth device/profile selection.

The media library can be used without this package in services, command-line tools, automated tests or applications that provide their own settings UI. Reference this adapter when users need to edit those settings interactively.

## Choose the UI for your application

| Host application | Settings adapter |
| --- | --- |
| Avalonia desktop | [Gurux.Serial.Avalonia](../Gurux.Serial.Avalonia/README.md) |
| Interactive Blazor / Razor UI | [Gurux.Serial.Blazor](../Gurux.Serial.Blazor/README.md) |
| Native .NET MAUI | [Gurux.Serial.Maui](../Gurux.Serial.Maui/README.md) |

Choose the adapter matching the host's UI framework; installing all three is unnecessary. A native MAUI host can use the MAUI adapter directly without a BlazorWebView. The Blazor adapter provides web components rather than native MAUI or Avalonia controls.

## How the pieces fit together

| Layer | Responsibility |
| --- | --- |
| [Gurux.Serial transport](../../README.md) | Connection lifecycle, protocol operations and media settings. It remains independent of UI frameworks. |
| `Gurux.Serial.Maui` | Settings controls and editing transactions for this framework and media type. |
| Host application | Owns the media, opens/closes connections, sends/receives data, persists accepted settings and supplies platform permissions. |
| [Gurux.Serial.Example](../../Gurux.Serial.Example/Gurux.Serial.Example.csproj) | A complete example application with connection/data controls and activity views. Its application screens are separate from this reusable settings adapter. |

This package is for configuration forms. Adding it does not supply a complete communication application, automatically connect a device or persist settings.

## Editing and ownership

Keep the supplied media closed while editing. The editor stages changes in a private, unopened draft; Save validates before applying settings to the caller-owned media. Cancel discards pending edits. Open media and conflicting external changes are rejected.

The host retains ownership of the media and disposes it when appropriate. Dispose host-owned embedded views or drafts when their containing form ends. A successful Save updates the media configuration; the application decides whether to persist it and when to open a connection.

Use MauiMediaSettingsEditor for native modal/overlay editing. Adapters exposing MediaSettingsView also support embedding the same controls in a host-owned form.

Serial/Bluetooth discovery and explicit GATT inspection can access native device services; the host remains responsible for permission requests and supported platform transports. Displaying settings does not grant hardware access. Terminal and SMS adapters reuse Serial settings controls where applicable.

## Integration details

Optional native .NET MAUI settings UI for `GXSerial`. The transport remains independent of MAUI. No Blazor or aggregate media UI package is required.

Reference this project/package from a MAUI host and register an editor on the media:

```csharp
media.SettingsEditor = new Gurux.Serial.Maui.MauiMediaSettingsEditor(this);
bool saved = await media.PropertiesAsync(cancellationToken: cancellationToken);
```

Here `this` is a `ContentPage` (or a `NavigationPage` showing one). Constructors also accept a MAUI `Window` with a page. Use `Gurux.Serial.Maui.MauiMediaWindow` to supply another owner to `PropertiesAsync`. The editor accepts exactly `GXSerial`; use the corresponding package for other media kinds.

The overlay restores the original owner content and enabled state. Save validates a private unopened copy, rejecting an open media or settings changed during editing. Cancel, owner disappearance, handler removal and cancellation discard the draft. Forward platform Back actions to `editor.Cancel()`. A concurrent call on the same editor is rejected. `Window` is populated while the overlay is shown.

Targets: Windows and Android; iOS is opt-in with `-p:EnableIosTarget=true`. Android transport builds require `-p:BuildMobile=true`; iOS library targets use `-p:BuildApple=true`; application signing and deployment require Mac/Xcode. Physical serial is unavailable on iOS; displaying settings does not add transport support. Android serial permissions and supported USB CDC ACM or paired Bluetooth Classic devices remain host responsibilities.

Terminal and SMS reuse `Gurux.Serial.Maui.SerialSettingsForm` for native port controls. This control must receive a private settings copy; `Read()` transfers input into that copy without opening a port. Net has no serial, terminal or SMS dependency.

Localized labels and validation messages are in `Properties/Resources.resx`. Add culture-specific satellite resources using the same keys.

Build with `dotnet build -f net10.0-windows10.0.19041.0 -p:GeneratePackageOnBuild=false`. Frameworkless staging checks are in the Net adapter's `Tests` folder. Native interaction and physical transport behavior require platform/device validation.

Serial port selection uses a native picker containing the exact names returned by `GXSerial.GetPortNames()` each time the dialog opens. An existing name is selected only if it is still listed. Save stays disabled until a listed port is selected; programmatic reads and saves also reject missing selections. Terminal and SMS reuse the same selection and save rules. Cancel discards the draft.

`SerialSettingsForm(SerialPortSettings, string[])` accepts a deterministic snapshot for hosts that already enumerate ports and for hardware-independent tests. The original constructor enumerates current system ports and adds the platform Bluetooth controls. Both the embedded view and modal editor use this live form constructor; Android also watches USB attachment changes while a handler is attached. Disposing the draft removes the handler and stops its watcher. The explicit snapshot constructor intentionally omits discovery controls and watchers. `CanSave` and `CanSaveChanged` expose selection availability without modifying the staged settings.

Run `dotnet run --project Tests/Native/Tests.csproj -p:GeneratePackageOnBuild=false` from this directory for actual managed MAUI control checks across Serial, Terminal and SMS. Tests cover exact lists, preselection, empty and stale lists, guarded commits, immediate Save enablement, Cancel and source isolation. These tests do not create platform handlers or open physical ports.

## Framework compatibility

The adapter targets .NET 10 and .NET 11 on the configured platforms. Each target uses its matching Microsoft.Maui.Controls major version: [10.0.110,11.0.0) for net10.0 and [11.0.10,12.0.0) for net11.0. .NET 11 Android targets require API 24 or later; .NET 10 Android targets retain API 21.

The example applications target .NET 10 and .NET 11. Use the .NET 11 SDK and matching MAUI workloads for examples; install the .NET 10 SDK and matching compatibility workloads when building the library's complete multi-target matrix. The SDK 11 installation does not by itself replace the MAUI 10 compatibility workload. Keep both SDK/workload environments available, as in the publishing workflow where present.

## Embedding settings

Use the public `MediaSettingsView(IGXMedia)` to place the existing native controls directly inside an owning form. The supplied media must be closed and remains owned by the caller. Constructing or committing the view never opens its transport.

Call `Commit()` only from the owning form's Save action. Inputs are validated before copying settings to the supplied media. A successful commit allows later edits or a persistence retry, while external settings changes still cause a conflict. Changing which embedded view is visible does not require a commit.

Call `Dispose()` on every embedded view when the owning form is saved, canceled or destroyed, then dispose any caller-owned media. Disposal discards pending input. Existing `MauiMediaSettingsEditor` and `PropertiesAsync` modal editing remain supported.
On Windows, default builds select the Windows targets. Pass `-p:BuildMobile=true` to restore/build Android targets throughout the adapter and transport project graph; the publishing workflow already supplies this flag. Both .NET 10 and .NET 11 remain supported.

The native harness also checks that live and snapshot drafts leave the source unopened and unchanged. With a Windows target (-p:TargetFramework=net10.0-windows10.0.19041.0), it additionally checks that the production default draft exposes Bluetooth protocol selection and refresh controls. GUI permissions, discovery and watcher lifecycle still require platform interaction testing.

The Communication Bridge Android runtime connector currently supports USB only. Displaying the adapter's Bluetooth settings does not enable Bluetooth forwarding in that host.
