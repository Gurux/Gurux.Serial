// --------------------------------------------------------------------------
// Gurux Ltd
//
// Copyright (c) Gurux Ltd.
//
// This file is part of Gurux Device Framework.
//
// Commit: $Format:%H$
// Date:   $Format:%cI$
//
// This software is available under either of the following licenses:
//
// 1. GNU General Public License, version 2 only (GPL-2.0-only).
//    See the LICENSE file included with this distribution.
//
// 2. A commercial license granted by Gurux Ltd under a separate written
//    agreement. Use under this option requires a valid commercial license
//    and is governed by the terms of that agreement.
//
// If you have not obtained a commercial license from Gurux Ltd,
// use of this software is governed by GPL-2.0-only.
//
// Commercial licensing: https://www.gurux.fi/
//
// SPDX-License-Identifier: GPL-2.0-only OR LicenseRef-Gurux-Commercial
// --------------------------------------------------------------------------
using SerialPort = System.IO.Ports.SerialPort;
using System.Text.Json;
using System.Xml;
using System.Xml.Linq;
using Gurux.Common;
using System.IO.Ports;

namespace Gurux.Serial;
/// <summary>One asynchronous serial API for desktop ports and platform accessory streams.</summary>
/// <remarks>The stream factory must return a readable, writable stream owned by this instance.
/// On iOS use a supported MFi ExternalAccessory or BLE adapter stream. iOS does not expose arbitrary COM ports.</remarks>
public partial class GXSerial : GXMediaBase, IGXSerialSettings, IGXMediaIconProvider
{
    /// <summary>Opens the embedded PNG icon for this media without loading a UI framework.</summary>
    /// <returns>A new readable stream positioned at the beginning, or null if the embedded icon is unavailable.</returns>
    /// <remarks>The caller owns the stream and must dispose it. Each call returns an independent stream.</remarks>
    public Stream? OpenIcon()
    {
        return typeof(GXSerial).Assembly.GetManifestResourceStream("Gurux.Serial.MediaIcon.png");
    }

    /// <summary>Creates an owned platform stream instead of opening a desktop serial port.</summary>
    readonly Func<SerialPortSettings, CancellationToken, ValueTask<Stream>>? _factory;
    /// <summary>Serializes access to lifecycle transitions and pending cleanup state.</summary>
    readonly object _lifecycle = new();
    /// <summary>Prevents concurrent writes from interleaving bytes on the transport.</summary>
    readonly SemaphoreSlim _writer = new(1, 1);
    /// <summary>Identifies callbacks executing within the current asynchronous open operation.</summary>
    readonly AsyncLocal<bool> _inOpening = new();
    /// <summary>Cancels the currently pending serial stream creation.</summary>
    CancellationTokenSource? _openingCancellation;
    /// <summary>Signals completion of the currently pending open operation.</summary>
    TaskCompletionSource? _openingDone;
    /// <summary>Identifies callbacks executing within the receive loop to avoid waiting on themselves.</summary>
    readonly AsyncLocal<bool> _inReader = new();
    /// <summary>Holds the owned readable and writable connection while the media is open.</summary>
    Stream? _stream;
    /// <summary>Owns the desktop serial port associated with the active stream.</summary>
    SerialPort? _port;
    /// <summary>Cancels the active receive loop when the connection closes.</summary>
    CancellationTokenSource? _stopping;
    /// <summary>Tracks receive and cleanup tasks that must finish before a subsequent open.</summary>
    readonly HashSet<Task> _workers = [];
    /// <summary>Identifies close callbacks to prevent recursive cleanup waits.</summary>
    readonly AsyncLocal<bool> _inClosing = new();
    /// <summary>Identifies cleanup work whose closed callback is permitted to reopen the media.</summary>
    readonly AsyncLocal<Task?> _completedCleanup = new();
    /// <summary>Creates serial media that opens an operating system serial port.</summary>
    public GXSerial()
    {
    }

    /// <summary>Creates serial media that owns the stream returned by the supplied transport factory.</summary>
    /// <param name="streamFactory">Creates an owned readable and writable stream for the supplied serial settings and cancellation token.</param>
    /// <exception cref="ArgumentNullException">The stream factory is null.</exception>
    public GXSerial(Func<SerialPortSettings, CancellationToken, ValueTask<Stream>> streamFactory) => _factory = streamFactory ?? throw new ArgumentNullException(nameof(streamFactory));
    /// <summary>Gets the framing and device settings used by the underlying serial connection.</summary>
    public SerialPortSettings PortSettings { get; private set; } = new();
    /// <summary>Gets or sets the operating system port name or platform accessory identifier.</summary>
    public string? PortName { get => PortSettings.PortName; set => PortSettings.PortName = value; }
    /// <summary>Gets or sets the requested transfer rate in bits per second.</summary>
    public int BaudRate { get => PortSettings.BaudRate; set => PortSettings.BaudRate = value; }
    /// <summary>Gets or sets the number of payload bits in each serial frame.</summary>
    public int DataBits { get => PortSettings.DataBits; set => PortSettings.DataBits = value; }
    /// <summary>Gets or sets the parity check used for each serial frame.</summary>
    public Parity Parity { get => PortSettings.Parity; set => PortSettings.Parity = value; }
    /// <summary>Gets or sets the stop interval terminating each serial frame.</summary>
    public StopBits StopBits { get => PortSettings.StopBits; set => PortSettings.StopBits = value; }
    /// <summary>Gets or sets the hardware or software flow control policy.</summary>
    public Handshake Handshake { get => PortSettings.Handshake; set => PortSettings.Handshake = value; }
    /// <summary>Gets or sets whether the data terminal ready signal is asserted.</summary>
    public bool DtrEnable { get => PortSettings.DtrEnable; set => PortSettings.DtrEnable = value; }
    /// <summary>Gets or sets whether the request to send signal is asserted.</summary>
    public bool RtsEnable { get => PortSettings.RtsEnable; set => PortSettings.RtsEnable = value; }
    /// <inheritdoc/>
    public override string Name => PortName ?? string.Empty;
    /// <inheritdoc/>
    public override string MediaType => "Serial";
    /// <inheritdoc/>
    public override bool IsOpen => _stream != null;

    /// <summary>Gets or replaces serial settings using the legacy Port, Bps and ByteSize XML element names.</summary>
    /// <remarks>Accepts XML fragments or a Serial wrapper. Null or empty input restores defaults.
    /// Numeric and named framing enumeration values are accepted. Optional BluetoothChannel and BluetoothProfile elements preserve Bluetooth configuration, including an application-registered command provider name.
    /// Changes require a closed transport.</remarks>
    /// <exception cref="InvalidOperationException">The media is open, opening or awaiting cleanup.</exception>
    /// <exception cref="XmlException">The supplied XML is malformed.</exception>
    /// <exception cref="FormatException">A numeric, Boolean, Bluetooth UUID or base64 configuration setting is malformed.</exception>
    /// <exception cref="ArgumentException">A framing enumeration cannot be parsed or a Bluetooth profile is invalid.</exception>
    public override string? Settings
    {
        get
        {
            var xml = new XElement("Serial",
                PortName == null ? null : new XElement("Port", PortName),
                new XElement("Bps", BaudRate), new XElement("ByteSize", DataBits),
                new XElement("StopBits", (int)StopBits), new XElement("Parity", (int)Parity),
                new XElement("Handshake", (int)Handshake), new XElement("DtrEnable", DtrEnable),
                new XElement("RtsEnable", RtsEnable),
                PortSettings.BluetoothChannel == 0 ? null : new XElement("BluetoothChannel", PortSettings.BluetoothChannel),
                PortSettings.BluetoothProfile == null ? null : new XElement("BluetoothProfile",
                    new XElement("Service", PortSettings.BluetoothProfile.Service),
                    new XElement("ReadCharacteristic", PortSettings.BluetoothProfile.ReadCharacteristic),
                    new XElement("WriteCharacteristic", PortSettings.BluetoothProfile.WriteCharacteristic),
                    PortSettings.BluetoothProfile.ConfigurationCharacteristic is Guid configuration ? new XElement("ConfigurationCharacteristic", configuration) : null,
                    PortSettings.BluetoothProfile.ConfigurationValue is byte[] bytes ? new XElement("ConfigurationValue", Convert.ToBase64String(bytes)) : null,
                    PortSettings.BluetoothProfile.ConfigurationCommandProvider is string provider ? new XElement("ConfigurationCommandProvider", provider) : null));
            return string.Concat(xml.Elements().Select(element => element.ToString(SaveOptions.DisableFormatting)));
        }
        set
        {
            lock (_lifecycle)
            {
                if (IsOpen || _openingDone != null || _workers.Count != 0)
                {
                    throw new InvalidOperationException(global::Gurux.Serial.Properties.Resources.Get("CloseTheMediaAndAwaitCleanupBeforeChangingSettings"));
                }
                var root = XElement.Parse("<Settings>" + value + "</Settings>");
                var xml = root.Element("Serial") ?? root;
                var settings = new SerialPortSettings
                {
                    PortName = (string?)xml.Element("Port") ?? (string?)xml.Element("PortName"),
                    BaudRate = (int?)xml.Element("Bps") ?? (int?)xml.Element("BaudRate") ?? 9600,
                    DataBits = (int?)xml.Element("ByteSize") ?? (int?)xml.Element("DataBits") ?? 8,
                    StopBits = Enum.Parse<StopBits>((string?)xml.Element("StopBits") ?? "One"),
                    Parity = Enum.Parse<Parity>((string?)xml.Element("Parity") ?? "None"),
                    Handshake = Enum.Parse<Handshake>((string?)xml.Element("Handshake") ?? "None"),
                    DtrEnable = (bool?)xml.Element("DtrEnable") ?? false,
                    RtsEnable = (bool?)xml.Element("RtsEnable") ?? false,
                    BluetoothChannel = (int?)xml.Element("BluetoothChannel") ?? 0,
                    BluetoothProfile = xml.Element("BluetoothProfile") is XElement bluetooth ? new BluetoothLeProfile(
                        Guid.Parse((string)bluetooth.Element("Service")!),
                        Guid.Parse((string)bluetooth.Element("ReadCharacteristic")!),
                        Guid.Parse((string)bluetooth.Element("WriteCharacteristic")!),
                        bluetooth.Element("ConfigurationCharacteristic") is XElement configuration ? Guid.Parse(configuration.Value) : null,
                        bluetooth.Element("ConfigurationValue") is XElement bytes ? Convert.FromBase64String(bytes.Value) : null)
                    {
                        ConfigurationCommandProvider = (string?)bluetooth.Element("ConfigurationCommandProvider")
                    } : null
                };
                PortSettings = settings;
            }
        }
    }

    /// <summary>Gets or replaces the JSON-serialized serial port settings.</summary>
    /// <remarks>Assigning null resets the settings. Replacement requires closed media with no pending open or cleanup work.</remarks>
    /// <exception cref="InvalidOperationException">The media is open, opening or awaiting cleanup.</exception>
    /// <exception cref="JsonException">The value is not valid JSON for serial port settings.</exception>
    /// <exception cref="ArgumentException">The JSON value is null.</exception>
    public override string? JsonSettings
    {
        get => JsonSerializer.Serialize(PortSettings, SerialJsonContext.Default.SerialPortSettings);
        set
        {
            lock (_lifecycle)
            {
                if (IsOpen || _openingDone != null || _workers.Count != 0)
                {
                    throw new InvalidOperationException(global::Gurux.Serial.Properties.Resources.Get("CloseTheMediaAndAwaitCleanupBeforeChangingSettings"));
                }

                PortSettings = value == null ? new SerialPortSettings() : JsonSerializer.Deserialize(value, SerialJsonContext.Default.SerialPortSettings) ?? throw new ArgumentException(global::Gurux.Serial.Properties.Resources.Get("InvalidSettings"));
            }
        }
    }

    /// <summary>Checks the configured port name, baud rate, data bits and framing values.</summary>
    /// <returns>All localized configuration errors, or an empty list when the settings are valid.</returns>
    public override IReadOnlyList<GXValidationError> Validate() => PortSettings.GetValidationErrors();
    /// <summary>Common baud rates, not a hardware capability probe. The device driver validates a selected rate on open.</summary>
    /// <param name="portName">The optional port name; the conventional rate list is independent of the selected hardware.</param>
    /// <returns>The conventional transfer rates offered to settings editors.</returns>
    public static int[] GetAvailableBaudRates(string? portName = null) => [300, 600, 1200, 2400, 4800, 9600, 14400, 19200, 38400, 56000, 57600, 115200, 128000, 230400, 256000, 460800, 921600];
    /// <summary>Enumerates physical ports, permitted Android devices and recently discovered mobile BLE identifiers.</summary>
    /// <remarks>Linux includes existing numbered serial device nodes even when runtime sysfs discovery omits them. Listing a node does not open it or verify hardware availability or access permissions.</remarks>
    /// <returns>The available device identifiers; physical ports use natural numeric order and mobile BLE identifiers come from explicit discovery.</returns>
    public static string[] GetPortNames()
    {
#if ANDROID
        return SerialPortDiscovery.SortPortNames(AndroidSerialTransports.GetPortNames(Android.App.Application.Context));
#elif IOS || MACCATALYST
        return BluetoothLeDiscovery.GetKnownDevices().Select(device => "ble:" + device.Id).ToArray();
#else
        if (OperatingSystem.IsLinux())
        {
            return SerialPortDiscovery.GetLinuxPortNames(SerialPort.GetPortNames, "/dev");
        }
        return OperatingSystem.IsWindows() || OperatingSystem.IsMacOS()
            ? SerialPortDiscovery.SortPortNames(SerialPort.GetPortNames())
            : [];
#endif
    }
    /// <summary>Checks whether a selected port or known Bluetooth identifier is currently listed without opening it.</summary>
    /// <param name="portName">The selected operating-system port or native accessory identifier.</param>
    /// <param name="cancellationToken">Cancels discovery.</param>
    /// <returns>False for blank selections, missing devices or unavailable discovery services; otherwise true for a matching listed device.</returns>
    /// <remarks>Bluetooth discovery uses operating-system-known devices without scanning or connecting. A known device is not guaranteed to be in radio range.</remarks>
    /// <exception cref="OperationCanceledException">The caller cancels discovery.</exception>
    public static Task<bool> IsPortAvailableAsync(string? portName, CancellationToken cancellationToken = default) => SerialPortDiscovery.IsAvailableAsync(portName, cancellationToken);

    /// <inheritdoc/>
    public override async Task OpenAsync(CancellationToken cancellationToken = default)
    {
        CancellationTokenSource cancel;
        TaskCompletionSource done;
        lock (_lifecycle)
        {
            if (_openingDone != null)
            {
                throw new InvalidOperationException(global::Gurux.Serial.Properties.Resources.Get("SerialMediaIsAlreadyOpening"));
            }

            if (IsOpen)
            {
                return;
            }

            if (_workers.Any(task => !task.IsCompleted && !ReferenceEquals(task, _completedCleanup.Value)))
            {
                throw new InvalidOperationException(global::Gurux.Serial.Properties.Resources.Get("SerialMediaIsStillClosingAwaitCloseAsyncBeforeReopening"));
            }

            _openingCancellation = cancel = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _openingDone = done = new(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        Stream? source = null;
        SerialPort? owner = null;
        bool previous = _inOpening.Value;
        _inOpening.Value = true;
        try
        {
            SetState(MediaState.Opening);
            cancel.Token.ThrowIfCancellationRequested();
            var errors = Validate();
            if (errors.Count != 0)
            {
                PortSettings.Validate();
                throw new ArgumentException(errors[0].Message);
            }
            if (_factory != null)
            {
                source = await _factory(PortSettings, cancel.Token).ConfigureAwait(false);
            }
#if IOS || MACCATALYST
            else if (PortName?.StartsWith("ble:", StringComparison.OrdinalIgnoreCase) == true)
            {
                source = await BluetoothSerialTransports.BluetoothLe(PortName[4..], PortSettings.BluetoothProfile!)(PortSettings, cancel.Token).ConfigureAwait(false);
            }
#endif
#if !ANDROID && !IOS && !MACCATALYST
            else if (PortName?.StartsWith("bluetooth:", StringComparison.OrdinalIgnoreCase) == true)
            {
                source = await BluetoothSerialTransports.BluetoothRfcomm(PortName[10..], PortSettings.BluetoothChannel)(PortSettings, cancel.Token).ConfigureAwait(false);
            }
            else if (PortName?.StartsWith("ble:", StringComparison.OrdinalIgnoreCase) == true)
            {
                source = await BluetoothSerialTransports.BluetoothLe(PortName[4..], PortSettings.BluetoothProfile!)(PortSettings, cancel.Token).ConfigureAwait(false);
            }
#endif
            else
            {
                if (!(OperatingSystem.IsWindows() || OperatingSystem.IsLinux() || OperatingSystem.IsMacOS()))
                {
                    throw new PlatformNotSupportedException(global::Gurux.Serial.Properties.Resources.Get("SupplyAPlatformAccessoryStreamFactoryOnAndroidIOS"));
                }

#if !IOS && !MACCATALYST
                owner = new SerialPort(PortName, BaudRate, (System.IO.Ports.Parity)Parity, DataBits, (System.IO.Ports.StopBits)StopBits)
                {
                    Handshake = (System.IO.Ports.Handshake)Handshake,
                    DtrEnable = DtrEnable,
                    RtsEnable = RtsEnable
                };
                owner.Open();
                source = owner.BaseStream;
#else
                throw new PlatformNotSupportedException(global::Gurux.Serial.Properties.Resources.Get("PhysicalIPhoneSerialIsOutsideTheCurrentScopeSupplyAnOwnedSupportedStream"));
#endif
            }

            if (source == null || !source.CanRead || !source.CanWrite)
            {
                throw new IOException(global::Gurux.Serial.Properties.Resources.Get("TransportMustBeReadableAndWritable"));
            }

            lock (_lifecycle)
            {
                cancel.Token.ThrowIfCancellationRequested();
                _stream = source;
                _port = owner;
                _stopping = new CancellationTokenSource();
                ReceiveBuffer.Open();
            }

            SetState(MediaState.Open);
            lock (_lifecycle)
            {
                if (ReferenceEquals(_stream, source) && _stopping != null)
                {
                    var active = source;
                    var serialPort = owner;
                    var token = _stopping.Token;
                    TrackWorker(Task.Run(() => ReadLoopAsync(active, serialPort, token)));
                }
            }
        }
        catch
        {
            lock (_lifecycle)
            {
                if (ReferenceEquals(_stream, source))
                {
                    _stream = null;
                    _port = null;
                    _stopping?.Cancel();
                    _stopping?.Dispose();
                    _stopping = null;
                }

                ReceiveBuffer.Close();
            }

            if (source != null)
            {
                await DisposeTransportAsync(source).ConfigureAwait(false);
            }
            owner?.Dispose();
            SetState(MediaState.Closed);
            throw;
        }
        finally
        {
            lock (_lifecycle)
            {
                _openingCancellation = null;
                _openingDone = null;
            }

            cancel.Dispose();
            done.TrySetResult();
            _inOpening.Value = previous;
        }
    }

    /// <summary>Provides a subclass hook before incoming transport bytes are published to receive subscribers.</summary>
    /// <param name="data">The received bytes, valid only for the duration of this callback.</param>
    protected virtual void OnDataReceived(ReadOnlySpan<byte> data)
    {
    }

    /// <summary>Reads the active stream until cancellation, disconnect or failure and closes the media afterward.</summary>
    /// <param name="source">The active owned stream to read.</param>
    /// <param name="owner">The desktop serial port owning the stream, or null for an injected stream.</param>
    /// <param name="token">Cancels the pending connection or receive operation.</param>
    /// <returns>A task completing when the receive loop and its cleanup finish.</returns>
    async Task ReadLoopAsync(Stream source, SerialPort? owner, CancellationToken token)
    {
        _inReader.Value = true;
        byte[] buffer = new byte[4096];
        try
        {
            while (!token.IsCancellationRequested)
            {
                int count = await source.ReadAsync(buffer, token).ConfigureAwait(false);
                if (count == 0)
                {
                    break;
                }

                OnDataReceived(buffer.AsSpan(0, count));
                PublishReceived(buffer.AsSpan(0, count).ToArray(), PortName);
            }
        }
        catch (Exception) when (token.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            ReportError(ex);
        }
        finally
        {
            bool disconnected = false;
            lock (_lifecycle)
            {
                if (ReferenceEquals(_stream, source))
                {
                    _stream = null;
                    if (ReferenceEquals(_port, owner))
                    {
                        _port = null;
                    }

                    ReceiveBuffer.Close();
                    disconnected = true;
                }
            }

            if (disconnected)
            {
                await DisposeTransportAsync(source).ConfigureAwait(false);
                owner?.Dispose();
                SetState(MediaState.Closed);
            }

            _inReader.Value = false;
        }
    }

    /// <inheritdoc/>
    public override async ValueTask SendAsync(ReadOnlyMemory<byte> data, string? receiver = null, CancellationToken cancellationToken = default)
    {
        await _writer.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var active = _stream ?? throw new InvalidOperationException(global::Gurux.Serial.Properties.Resources.Get("SerialMediaIsClosed"));
            await active.WriteAsync(data, cancellationToken).ConfigureAwait(false);
            await active.FlushAsync(cancellationToken).ConfigureAwait(false);
            AddBytesSent(data.Length);
        }
        finally
        {
            _writer.Release();
        }
    }

    /// <summary>Tracks asynchronous connection work until completion so reopening waits for cleanup.</summary>
    /// <param name="task">The asynchronous work to retain until it completes.</param>
    void TrackWorker(Task task)
    {
        lock (_lifecycle)
        {
            _workers.Add(task);
        }
        _ = task.ContinueWith(completed =>
        {
            lock (_lifecycle)
            {
                _workers.Remove(completed);
            }
        }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
    }

    /// <summary>Awaits native BLE subscription cleanup while preserving synchronous close behavior for other streams.</summary>
    /// <param name="source">The owned transport to release.</param>
    /// <returns>An operation completing after all required native cleanup.</returns>
    private static ValueTask DisposeTransportAsync(Stream source)
    {
        if (source is DesktopBluetoothLeStream)
        {
            return source.DisposeAsync();
        }
        source.Dispose();
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc/>
    public override async Task CloseAsync()
    {
        Task[] pending;
        Stream? active;
        SerialPort? owner;
        CancellationTokenSource? cancel;
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        lock (_lifecycle)
        {
            _openingCancellation?.Cancel();
            pending = _workers.Concat(_openingDone == null ? [] : new[] { _openingDone.Task }).ToArray();
            _workers.Add(completion.Task);
            active = _stream;
            _stream = null;
            owner = _port;
            _port = null;
            cancel = _stopping;
            _stopping = null;
            ReceiveBuffer.Close();
            cancel?.Cancel();
        }

        bool reentrant = _inClosing.Value || _inReader.Value || _inOpening.Value || _inSettingsApply.Value;
        bool previous = _inClosing.Value;
        _inClosing.Value = true;
        Exception? failure = null;
        // Attempts one cleanup action while retaining the first failure for the caller.
        // action: The resource cleanup operation to attempt.
        void Cleanup(Action action)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                failure ??= ex;
            }
        }

        try
        {
            if (active != null)
            {
                Cleanup(() => SetState(MediaState.Closing));
            }

            if (active != null)
            {
                try
                {
                    await DisposeTransportAsync(active).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    failure ??= ex;
                }
            }

            if (owner != null)
            {
                Cleanup(owner.Dispose);
            }

            if (!reentrant)
            {
                try
                {
                    await Task.WhenAll(pending).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    failure ??= ex;
                }
            }

            if (cancel != null)
            {
                Cleanup(cancel.Dispose);
            }

            if (active != null)
            {
                var previousCleanup = _completedCleanup.Value;
                _completedCleanup.Value = completion.Task;
                try
                {
                    Cleanup(() => SetState(MediaState.Closed));
                }
                finally
                {
                    _completedCleanup.Value = previousCleanup;
                }
            }
        }
        finally
        {
            _inClosing.Value = previous;
            completion.TrySetResult();
            lock (_lifecycle)
            {
                _workers.Remove(completion.Task);
            }
        }

        if (failure != null)
        {
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
        }
    }
}
