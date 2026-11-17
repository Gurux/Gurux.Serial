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
#if ANDROID
using Android.Bluetooth;
using Android.Content;
using System.Threading.Channels;
using Java.Util;
namespace Gurux.Serial;

/// <summary>Owns a connected GATT session and exposes notifications and serialized characteristic writes.</summary>
internal sealed class BluetoothLeStream : Stream
{
#if ANDROID37_0_OR_GREATER
    /// <summary>Retains a process-lifetime Java executor without allocating a worker thread or callback queue.</summary>
    private static readonly InlineCallbackExecutor callbackExecutor = new();

    /// <summary>Runs GATT callbacks on Android's delivering thread without imposing a UI-thread dependency.</summary>
    private sealed class InlineCallbackExecutor : Java.Lang.Object, Java.Util.Concurrent.IExecutor
    {
        /// <summary>Executes one native callback immediately on the calling thread.</summary>
        /// <param name="command">Android callback command to execute.</param>
        /// <exception cref="ArgumentNullException">The callback command is null.</exception>
        public void Execute(Java.Lang.IRunnable? command)
        {
            ArgumentNullException.ThrowIfNull(command);
            command.Run();
        }
    }
#endif

    /// <summary>Serializes all GATT requests, including initialization.</summary>
    private readonly SemaphoreSlim operations = new(1, 1);
    /// <summary>Preserves complete-write ordering across concurrent callers.</summary>
    private readonly SemaphoreSlim writers = new(1, 1);
    /// <summary>Serializes consumers while preserving partial notification reads.</summary>
    private readonly SemaphoreSlim readers = new(1, 1);
    /// <summary>Bounds incoming packet storage without blocking Android callback threads.</summary>
    private readonly Channel<byte[]> incoming;
    /// <summary>Protects pending callback completion and terminal state.</summary>
    private readonly object sync = new();
    /// <summary>Maximum duration of a native request.</summary>
    private readonly TimeSpan timeout;
    /// <summary>Receives native lifecycle and operation callbacks.</summary>
    private readonly BluetoothLeCallback callback;
    /// <summary>Owns the native connection.</summary>
    private BluetoothGatt? gatt;
    /// <summary>Publishes connection establishment.</summary>
    private readonly TaskCompletionSource<bool> connected = new(TaskCreationOptions.RunContinuationsAsynchronously);
    /// <summary>Publishes the currently outstanding native operation.</summary>
    private TaskCompletionSource<bool>? pending;
    /// <summary>Identifies the expected callback category.</summary>
    private string? pendingKind;
    /// <summary>Stores the first terminal failure.</summary>
    private Exception? failure;
    /// <summary>Tracks disposal independently of native callbacks.</summary>
    private bool closed;
    /// <summary>Characteristic providing notifications.</summary>
    private BluetoothGattCharacteristic? input;
    /// <summary>Characteristic receiving outgoing bytes.</summary>
    private BluetoothGattCharacteristic? output;
    /// <summary>Negotiated ATT payload size, initially the minimum MTU minus its header.</summary>
    private int payloadSize = 20;
    /// <summary>Partially consumed notification packet.</summary>
    private byte[]? current;
    /// <summary>Offset into the current packet.</summary>
    private int currentOffset;
    /// <summary>Initializes bounded receive storage and the callback owner.</summary>
    /// <param name="timeout">Maximum native operation duration.</param>
    /// <param name="capacity">Maximum queued packets.</param>
    private BluetoothLeStream(TimeSpan timeout, int capacity)
    {
        this.timeout = timeout;
        incoming = Channel.CreateBounded<byte[]>(new BoundedChannelOptions(capacity) { FullMode = BoundedChannelFullMode.Wait, SingleReader = true, SingleWriter = false });
        callback = new BluetoothLeCallback(this);
    }
    /// <summary>Connects, discovers characteristics, configures the device and subscribes to its input.</summary>
    /// <param name="context">Application connection context.</param>
    /// <param name="device">Remote device.</param>
    /// <param name="profile">Service and characteristic mapping.</param>
    /// <param name="timeout">Per-operation deadline.</param>
    /// <param name="capacity">Queued notification packet limit.</param>
    /// <param name="mtu">Requested ATT MTU.</param>
    /// <param name="token">Opening cancellation.</param>
    /// <param name="settings">Serial settings used for catalogue fallback configuration; omitted callers use default serial settings.</param>
    /// <returns>An initialized owned stream.</returns>
    internal static async Task<Stream> OpenAsync(Context context, BluetoothDevice device, BluetoothLeProfile profile, TimeSpan timeout, int capacity, int mtu, CancellationToken token, SerialPortSettings? settings = null)
    {
        var stream = new BluetoothLeStream(timeout, capacity);
        try
        {
            token.ThrowIfCancellationRequested();
            BluetoothGatt connection = ConnectGatt(context, device, stream.callback)
                ?? throw Error("BluetoothLeConnectionFailed");
            lock (stream.sync)
            {
                if (stream.closed)
                {
                    CloseNative(connection);
                    stream.ThrowIfClosed();
                }
                stream.gatt = connection;
            }
            await stream.connected.Task.WaitAsync(timeout, token).ConfigureAwait(false);
            await stream.RequestAsync("services", () => stream.gatt.DiscoverServices(), token).ConfigureAwait(false);
            var service = stream.gatt.GetService(UUID.FromString(profile.Service.ToString())!);
            if (service is null)
            {
                profile = BluetoothLeServiceSelection.SelectProfile(profile, device.Name,
                    stream.gatt.Services?.Select(item => Guid.Parse(item.Uuid!.ToString()!)) ?? Enumerable.Empty<Guid>(),
                    settings ?? new SerialPortSettings(), device.Address);
                service = stream.gatt.GetService(UUID.FromString(profile.Service.ToString())!) ?? throw Error("BluetoothLeProfileNotFound");
            }
            stream.input = service.GetCharacteristic(UUID.FromString(profile.ReadCharacteristic.ToString())!) ?? throw Error("BluetoothLeProfileNotFound");
            stream.output = service.GetCharacteristic(UUID.FromString(profile.WriteCharacteristic.ToString())!) ?? throw Error("BluetoothLeProfileNotFound");
            if ((stream.input.Properties & (GattProperty.Notify | GattProperty.Indicate)) == 0 || (stream.output.Properties & (GattProperty.Write | GattProperty.WriteNoResponse)) == 0)
            {
                throw Error("BluetoothLeUnsupportedCharacteristics");
            }
            await stream.RequestAsync("mtu", () => stream.gatt.RequestMtu(mtu), token).ConfigureAwait(false);
            if (profile.ConfigurationValue is byte[] configuration)
            {
                var characteristic = service.GetCharacteristic(UUID.FromString(profile.ConfigurationCharacteristic!.Value.ToString())!) ?? throw Error("BluetoothLeProfileNotFound");
                await stream.WriteCharacteristicAsync(characteristic, configuration, token).ConfigureAwait(false);
            }
            var descriptor = stream.input.GetDescriptor(UUID.FromString("00002902-0000-1000-8000-00805f9b34fb")!) ?? throw Error("BluetoothLeProfileNotFound");
            if (!stream.gatt.SetCharacteristicNotification(stream.input, true))
            {
                throw Error("BluetoothLeOperationFailed");
            }
            byte[] subscription = (stream.input.Properties & GattProperty.Notify) != 0 ? new byte[] { 1, 0 } : new byte[] { 2, 0 };
            await stream.RequestAsync("descriptor", () => stream.WriteDescriptor(descriptor, subscription), token).ConfigureAwait(false);
            return stream;
        }
        catch
        {
            stream.Dispose();
            throw;
        }
    }
    /// <summary>Builds a localized connection failure.</summary>
    /// <param name="key">Resource identifying the failure.</param>
    /// <returns>A transport exception.</returns>
    private static IOException Error(string key) => new(global::Gurux.Serial.Properties.Resources.Get(key));
    /// <summary>Creates a native GATT session using the non-obsolete API supported by the running Android version.</summary>
    /// <param name="context">Application context used by legacy Android connection APIs.</param>
    /// <param name="device">Remote BLE device to connect directly without automatic reconnection.</param>
    /// <param name="callbacks">Receives native lifecycle, notification and operation results.</param>
    /// <returns>The owned native GATT session, or null when Android cannot create it.</returns>
    /// <remarks>Android 37 uses connection settings with explicit MTU negotiation. Its direct executor
    /// adds no UI-thread dependency or intermediate callback queue. Earlier Android versions retain their existing APIs.</remarks>
    private static BluetoothGatt? ConnectGatt(Context context, BluetoothDevice device, BluetoothGattCallback callbacks)
    {
#if ANDROID37_0_OR_GREATER
        if (OperatingSystem.IsAndroidVersionAtLeast(37))
        {
            using var builder = new BluetoothGattConnectionSettings.Builder();
            builder.SetAutoConnectEnabled(false);
            builder.SetTransport((int)BluetoothTransports.Le);
            builder.SetAutomaticMtuEnabled(false);
            using var settings = builder.Build();
            return device.ConnectGatt(settings!, callbackExecutor, callbacks);
        }
#endif
        if (OperatingSystem.IsAndroidVersionAtLeast(23))
        {
            return device.ConnectGatt(context.ApplicationContext ?? context, false, callbacks, BluetoothTransports.Le);
        }
        return device.ConnectGatt(context.ApplicationContext ?? context, false, callbacks);
    }

    /// <summary>Starts one native request and waits for its matching callback.</summary>
    /// <param name="kind">Expected callback category.</param>
    /// <param name="start">Native operation returning whether it was accepted.</param>
    /// <param name="token">Operation cancellation.</param>
    /// <returns>A task completing after the native callback.</returns>
    private async Task RequestAsync(string kind, Func<bool> start, CancellationToken token)
    {
        await operations.WaitAsync(token).ConfigureAwait(false);
        try
        {
            TaskCompletionSource<bool> completion;
            lock (sync)
            {
                ThrowIfClosed();
                completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
                pending = completion;
                pendingKind = kind;
            }
            if (!start())
            {
                throw Error("BluetoothLeOperationFailed");
            }
            await completion.Task.WaitAsync(timeout, token).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // A timed-out native request cannot be safely reused: its late callback has no request identifier.
            Fail(ex);
            throw;
        }
        finally
        {
            lock (sync)
            {
                pending = null;
                pendingKind = null;
            }
            operations.Release();
        }
    }
    /// <summary>Completes the expected request without allowing notifications to consume operation completions.</summary>
    /// <param name="kind">Callback category.</param>
    /// <param name="status">Native result.</param>
    internal void Complete(string kind, GattStatus status)
    {
        lock (sync)
        {
            if (pendingKind == kind)
            {
                if (status == GattStatus.Success || kind == "mtu")
                {
                    pending?.TrySetResult(true);
                }
                else
                {
                    pending?.TrySetException(Error("BluetoothLeOperationFailed"));
                }
            }
        }
    }
    /// <summary>Handles native connection transitions.</summary>
    /// <param name="status">Native connection result.</param>
    /// <param name="state">New connection state.</param>
    internal void ConnectionChanged(GattStatus status, ProfileState state)
    {
        if (status == GattStatus.Success && state == ProfileState.Connected)
        {
            connected.TrySetResult(true);
        }
        else if (status != GattStatus.Success || state == ProfileState.Disconnected)
        {
            Fail(Error("BluetoothLeDisconnected"));
        }
    }
    /// <summary>Updates the payload limit only after a successful MTU exchange.</summary>
    /// <param name="mtu">Negotiated ATT MTU.</param>
    /// <param name="status">Native negotiation result.</param>
    internal void MtuChanged(int mtu, GattStatus status)
    {
        if (status == GattStatus.Success)
        {
            payloadSize = BluetoothLeLimits.GetPayloadSize(mtu);
        }
        Complete("mtu", status);
    }
    /// <summary>Copies a matching notification into bounded storage, failing on overflow.</summary>
    /// <param name="characteristic">Notification source.</param>
    /// <param name="value">Native packet bytes.</param>
    internal void Receive(BluetoothGattCharacteristic characteristic, byte[] value)
    {
        if (characteristic.Uuid?.Equals(input?.Uuid) != true || value.Length == 0)
        {
            return;
        }
        if (value.Length > BluetoothLeLimits.MaximumAttributeLength || !incoming.Writer.TryWrite(value.ToArray()))
        {
            Fail(Error("BluetoothLeReceiveOverflow"));
        }
    }
    /// <summary>Stops all pending work and closes the native session exactly once.</summary>
    /// <param name="exception">Terminal reason delivered to blocked operations.</param>
    private void Fail(Exception exception)
    {
        BluetoothGatt? owned;
        lock (sync)
        {
            if (closed)
            {
                return;
            }
            closed = true;
            failure = exception;
            connected.TrySetException(exception);
            pending?.TrySetException(exception);
            incoming.Writer.TryComplete(exception);
            owned = gatt;
            gatt = null;
        }
        if (owned != null)
        {
            CloseNative(owned);
        }
    }
    /// <summary>Releases every native ownership stage even if Android rejects disconnection during teardown.</summary>
    /// <param name="connection">Connection being abandoned or disposed.</param>
    private static void CloseNative(BluetoothGatt connection)
    {
        try
        {
            connection.Disconnect();
        }
        catch (Exception)
        {
            // Teardown must not escape an Android callback or replace the original transport error.
        }
        try
        {
            connection.Close();
        }
        catch (Exception)
        {
            // Closing can race Bluetooth service shutdown; the Java peer must still be released.
        }
        connection.Dispose();
    }
    /// <summary>Rejects use after disconnect or disposal.</summary>
    private void ThrowIfClosed()
    {
        if (closed)
        {
            throw failure ?? new ObjectDisposedException(nameof(BluetoothLeStream));
        }
    }
    /// <summary>Writes a descriptor using the memory-safe Android 13 API when available.</summary>
    /// <param name="descriptor">Descriptor to configure.</param>
    /// <param name="value">Descriptor bytes.</param>
    /// <returns>Whether Android accepted the operation.</returns>
    private bool WriteDescriptor(BluetoothGattDescriptor descriptor, byte[] value)
    {
        if (OperatingSystem.IsAndroidVersionAtLeast(33))
        {
            return (int)gatt!.WriteDescriptor(descriptor, value) == 0;
        }
#pragma warning disable CA1422
        descriptor.SetValue(value);
        return gatt!.WriteDescriptor(descriptor);
#pragma warning restore CA1422
    }
    /// <summary>Writes one characteristic in negotiated-MTU chunks.</summary>
    /// <param name="characteristic">Output or configuration characteristic.</param>
    /// <param name="value">Bytes to send.</param>
    /// <param name="token">Write cancellation.</param>
    /// <returns>A task completing after Android confirms every chunk.</returns>
    private async Task WriteCharacteristicAsync(BluetoothGattCharacteristic characteristic, ReadOnlyMemory<byte> value, CancellationToken token)
    {
        GattWriteType type = (characteristic.Properties & GattProperty.Write) != 0 ? GattWriteType.Default : GattWriteType.NoResponse;
        if ((characteristic.Properties & (GattProperty.Write | GattProperty.WriteNoResponse)) == 0)
        {
            throw Error("BluetoothLeUnsupportedCharacteristics");
        }
        for (int offset = 0; offset < value.Length; offset += payloadSize)
        {
            byte[] chunk = value.Slice(offset, Math.Min(payloadSize, value.Length - offset)).ToArray();
            await RequestAsync("write", () => StartWrite(characteristic, chunk, type), token).ConfigureAwait(false);
        }
    }
    /// <summary>Submits a write; both write modes complete through Android's characteristic-write callback.</summary>
    /// <param name="characteristic">Destination characteristic.</param>
    /// <param name="value">Independent chunk buffer.</param>
    /// <param name="type">Acknowledged or no-response ATT write mode.</param>
    /// <returns>Whether Android accepted the request.</returns>
    private bool StartWrite(BluetoothGattCharacteristic characteristic, byte[] value, GattWriteType type)
    {
        if (OperatingSystem.IsAndroidVersionAtLeast(33))
        {
            return (int)gatt!.WriteCharacteristic(characteristic, value, (int)type) == 0;
        }
#pragma warning disable CA1422
        characteristic.WriteType = type;
        characteristic.SetValue(value);
        return gatt!.WriteCharacteristic(characteristic);
#pragma warning restore CA1422
    }
    /// <inheritdoc/>
    public override bool CanRead => !closed;
    /// <inheritdoc/>
    public override bool CanSeek => false;
    /// <inheritdoc/>
    public override bool CanWrite => !closed;
    /// <inheritdoc/>
    public override long Length => throw new NotSupportedException();
    /// <inheritdoc/>
    public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
    /// <inheritdoc/>
    public override void Flush() { ThrowIfClosed(); }
    /// <inheritdoc/>
    public override Task FlushAsync(CancellationToken cancellationToken) { cancellationToken.ThrowIfCancellationRequested(); ThrowIfClosed(); return Task.CompletedTask; }
    /// <inheritdoc/>
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    /// <inheritdoc/>
    public override void SetLength(long value) => throw new NotSupportedException();
    /// <inheritdoc/>
    public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException(global::Gurux.Serial.Properties.Resources.Get("BluetoothLeAsyncOnly"));
    /// <inheritdoc/>
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException(global::Gurux.Serial.Properties.Resources.Get("BluetoothLeAsyncOnly"));
    /// <inheritdoc/>
    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();
    /// <inheritdoc/>
    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => WriteAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();
    /// <inheritdoc/>
    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        if (buffer.Length == 0)
        {
            return 0;
        }
        await readers.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            ThrowIfClosed();
            if (current == null)
            {
                current = await incoming.Reader.ReadAsync(cancellationToken).ConfigureAwait(false);
                currentOffset = 0;
            }
            int count = Math.Min(buffer.Length, current.Length - currentOffset);
            current.AsMemory(currentOffset, count).CopyTo(buffer);
            currentOffset += count;
            if (currentOffset == current.Length)
            {
                current = null;
            }
            return count;
        }
        finally
        {
            readers.Release();
        }
    }
    /// <inheritdoc/>
    public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        ThrowIfClosed();
        await writers.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await WriteCharacteristicAsync(output!, buffer, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            writers.Release();
        }
    }
    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Fail(new ObjectDisposedException(nameof(BluetoothLeStream)));
        }
        base.Dispose(disposing);
    }
}
#endif
