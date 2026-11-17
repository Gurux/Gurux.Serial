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

using System.Text;
using System.Globalization;
using Gurux.Common;
using Gurux.Serial.Example.Properties;
namespace Gurux.Serial.Example;
/// <summary>Owns one serial connection and serializes sending and receiving operations.</summary>
public sealed class ExampleSession : IAsyncDisposable
{
    /// <summary>Clears retained activity without changing received payloads, connection state or traffic counters.</summary>
    public void ClearActivity() => log.Clear();
    /// <summary>Serializes connection and send operations.</summary>
    private readonly SemaphoreSlim operations = new(1, 1);
    /// <summary>Protects cancellation and disposal state.</summary>
    private readonly object sync = new();
    /// <summary>Stores the bounded activity history.</summary>
    private readonly SessionLog log = new();
    /// <summary>Cancels active operations when closing.</summary>
    private CancellationTokenSource lifetime = new();
    /// <summary>Tracks whether disposal has started.</summary>
    private bool disposed;
    /// <summary>Tracks the active serialized operation.</summary>
    private int busy;
    /// <summary>Stores received frames independently of the activity history.</summary>
    private readonly Queue<byte[]> received = new();
    /// <summary>Limits retained payload storage to 64 KiB.</summary>
    private int receivedSize;
    /// <summary>Contains the automatic framing configuration, or null for legacy event reception.</summary>
    private ReceiveParameters<byte[]>? receiveOptions;
    /// <summary>Completes after the current connection's receive loop has stopped.</summary>
    private Task receiver = Task.CompletedTask;
    /// <summary>Stores the atomic receive echo option.</summary>
    private int echo;
    /// <summary>Gets or sets whether automatically received bytes are sent back unchanged.</summary>
    public bool Echo
    {
        get => Volatile.Read(ref echo) != 0;
        set => Volatile.Write(ref echo, value ? 1 : 0);
    }

    /// <summary>Clears retained received payloads without changing the connection, counters or activity history.</summary>
    public void ClearReceived()
    {
        lock (sync)
        {
            received.Clear();
            receivedSize = 0;
        }
    }
    /// <summary>Formats the bounded received history without modifying its original bytes.</summary>
    /// <param name="hex">Whether to display every frame in hexadecimal.</param>
    /// <returns>Received frames separated by platform line endings.</returns>
    public string Received(bool hex = false)
    {
        lock (sync)
        {
            return string.Join(Environment.NewLine, received.Select(data => ExampleDataDisplay.Format(data, hex)));
        }
    }

    /// <summary>Configures continuous receive framing for the next connection.</summary>
    /// <param name="hex">Whether the terminator is expressed as hexadecimal bytes.</param>
    /// <param name="eop">Optional terminator; text recognizes literal backslash-r and backslash-n escapes.</param>
    /// <param name="count">Minimum frame size, or zero to use only the terminator or the next available bytes.</param>
    /// <param name="waitTime">Positive receive timeout in milliseconds, or -1 to wait indefinitely.</param>
    /// <exception cref="ArgumentOutOfRangeException">Count is negative or the timeout is neither positive nor -1.</exception>
    /// <exception cref="InvalidOperationException">The media is open or another operation is in progress.</exception>
    /// <exception cref="FormatException">The hexadecimal terminator is malformed.</exception>
    /// <exception cref="ObjectDisposedException">The session is disposed.</exception>
    public void ConfigureReceive(bool hex, string? eop, int count, int waitTime)
    {
        if (count < 0 || waitTime == 0 || waitTime < -1)
        {
            throw new ArgumentOutOfRangeException(count < 0 ? nameof(count) : nameof(waitTime), ExampleResources.InvalidReplySettings);
        }
        byte[]? marker = string.IsNullOrEmpty(eop) ? null : hex
            ? Convert.FromHexString(string.Concat(eop.Where(c => !char.IsWhiteSpace(c))))
            : Encoding.UTF8.GetBytes(eop.Replace(@"\r", "\r", StringComparison.Ordinal).Replace(@"\n", "\n", StringComparison.Ordinal));
        if (marker is { Length: 0 })
        {
            throw new FormatException(ExampleResources.InvalidReplySettings);
        }
        lock (sync)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            if (Media.IsOpen || IsBusy)
            {
                throw new InvalidOperationException(ExampleResources.CloseBeforeReplySettings);
            }
            receiveOptions = new ReceiveParameters<byte[]>
            {
                Count = count == 0 && marker is null ? 1 : count,
                Eop = marker,
                WaitTime = waitTime
            };
        }
    }

    /// <summary>Receives complete frames and timeout partial data until the connection closes.</summary>
    /// <param name="options">The immutable framing snapshot for this connection.</param>
    /// <param name="scope">The buffered receive scope released when the loop ends.</param>
    /// <param name="cancellationToken">Cancels receiving and echo when closing.</param>
    /// <returns>A task that observes all receive and echo failures before completing.</returns>
    private async Task ReceiveLoopAsync(ReceiveParameters<byte[]> options, IDisposable scope, CancellationToken cancellationToken)
    {
        using (scope)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested && Media.IsOpen)
                {
                    var packet = new ReceiveParameters<byte[]>
                    {
                        Count = options.Count, Eop = options.Eop, WaitTime = options.WaitTime
                    };
                    bool complete = await Media.ReceiveAsync(packet, cancellationToken).ConfigureAwait(false);
                    if (!complete && Media.IsOpen)
                    {
                        var partial = new ReceiveParameters<byte[]> { WaitTime = 0, AllData = true };
                        await Media.ReceiveAsync(partial, cancellationToken).ConfigureAwait(false);
                        packet.Reply = partial.Reply;
                    }
                    if (packet.Reply is { Length: > 0 } bytes)
                    {
                        Received(this, new ReceiveEventArgs(bytes, null));
                        if (Echo)
                        {
                            await RunAsync(async token =>
                            {
                                await Media.SendAsync(bytes, cancellationToken: token).ConfigureAwait(false);
                                log.Add(string.Format(CultureInfo.CurrentCulture, ExampleResources.Tx, ExampleDataDisplay.Format(bytes)));
                                return true;
                            }, cancellationToken).ConfigureAwait(false);
                        }
                    }
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
            catch (Exception ex)
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    log.Add(string.Format(CultureInfo.CurrentCulture, ExampleResources.Error, ex.Message));
                }
            }
        }
    }
    /// <summary>Gets the serial media owned by this session.</summary>
    public GXSerial Media { get; }
    /// <summary>Allows an explicit open attempt for a configured BLE identity even when discovery cannot confirm its presence.</summary>
    /// <param name="selectedPortAvailable">Whether the current physical port or known transport was found by availability discovery.</param>
    /// <returns>True for a nonempty BLE identity or a nonempty selection confirmed available; false for empty selections.</returns>
    /// <remarks>BLE advertisements and unpaired devices may not appear in operating-system enumeration after restart. Opening performs the real connection and profile validation; callers still enforce closed, idle and cancellation state.</remarks>
    public bool CanAttemptOpen(bool selectedPortAvailable)
    {
        string? selected = Media.PortName;
        if (string.IsNullOrWhiteSpace(selected))
        {
            return false;
        }
        if (selected.StartsWith("ble:", StringComparison.OrdinalIgnoreCase))
        {
            return !string.IsNullOrWhiteSpace(selected[4..]);
        }
        return selectedPortAvailable;
    }
    /// <summary>Gets whether an open or send operation is running.</summary>
    public bool IsBusy => Volatile.Read(ref busy) != 0;
    /// <summary>Gets a snapshot of recent activity.</summary>
    public IReadOnlyList<string> Log => log.Snapshot;
    /// <summary>Creates the serial media using the platform transport when supplied.</summary>
    /// <param name="serialFactory">An optional platform serial stream factory.</param>
    public ExampleSession(Func<SerialPortSettings, CancellationToken, ValueTask<Stream>>? serialFactory = null)
        : this(serialFactory is null ? new GXSerial() : new GXSerial(serialFactory)) { }
    /// <summary>Takes ownership of an existing serial connection.</summary>
    /// <param name="media">The connection owned and disposed by this session.</param>
    /// <exception cref="ArgumentNullException">The serial connection is null.</exception>
    public ExampleSession(GXSerial media)
    {
        Media = media ?? throw new ArgumentNullException(nameof(media));
        Media.OnReceived += Received;
        Media.OnError += Error;
    }
    /// <summary>Retains a bounded copy of incoming frames and records their hexadecimal and ASCII representation.</summary>
    /// <param name="sender">The originating serial media.</param>
    /// <param name="args">The received payload.</param>
    private void Received(object? sender, ReceiveEventArgs args)
    {
        if (args.Data is byte[] bytes)
        {
            lock (sync)
            {
                byte[] retained = bytes.Length > 65536 ? bytes[^65536..] : bytes.ToArray();
                received.Enqueue(retained);
                receivedSize += retained.Length;
                while (received.Count > 500 || receivedSize > 65536)
                {
                    receivedSize -= received.Dequeue().Length;
                }
            }
            log.Add(ExampleDataDisplay.IsAscii(bytes)
                ? string.Format(CultureInfo.CurrentCulture, ExampleResources.RxBytes, args.SenderInfo, Convert.ToHexString(bytes), ExampleDataDisplay.Format(bytes))
                : string.Format(CultureInfo.CurrentCulture, ExampleResources.RxData, args.SenderInfo, ExampleDataDisplay.Format(bytes)));
        }
        else
        {
            log.Add(string.Format(CultureInfo.CurrentCulture, ExampleResources.RxData, args.SenderInfo, args.Data));
        }
    }
    /// <summary>Records transport errors for the native activity view.</summary>
    /// <param name="sender">The originating serial media.</param>
    /// <param name="args">The reported transport error.</param>
    private void Error(object? sender, MediaErrorEventArgs args) => log.Add(string.Format(CultureInfo.CurrentCulture, ExampleResources.Error, args.Exception.Message));
    /// <summary>Runs one operation under the session cancellation lifetime.</summary>
    /// <typeparam name="T">The operation result type.</typeparam>
    /// <param name="action">The asynchronous operation.</param>
    /// <param name="cancellationToken">Cancels waiting and execution.</param>
    /// <returns>The result produced by the operation.</returns>
    /// <exception cref="ObjectDisposedException">Disposal has started.</exception>
    private async Task<T> RunAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken)
    {
        CancellationToken token;
        lock (sync) { ObjectDisposedException.ThrowIf(disposed, this); token = lifetime.Token; }
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(token, cancellationToken);
        await operations.WaitAsync(linked.Token).ConfigureAwait(false);
        Interlocked.Exchange(ref busy, 1);
        try { linked.Token.ThrowIfCancellationRequested(); return await action(linked.Token).ConfigureAwait(false); }
        finally { Interlocked.Exchange(ref busy, 0); operations.Release(); }
    }
    /// <summary>Opens the selected serial transport and includes requested settings when the driver rejects an argument.</summary>
    /// <param name="cancellationToken">Cancels the open operation.</param>
    /// <returns>A task completing when the connection opens.</returns>
    /// <exception cref="InvalidOperationException">The transport rejects an argument; the message includes the requested settings and the original error is preserved.</exception>
    public Task OpenAsync(CancellationToken cancellationToken = default) => RunAsync(async token =>
    {
        if (Media.IsOpen)
        {
            return true;
        }
        IDisposable? scope = receiveOptions is not null ? Media.BeginSynchronous() : null;
        try
        {
            try
            {
                await Media.OpenAsync(token).ConfigureAwait(false);
            }
            catch (ArgumentException ex)
            {
                throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture,
                    ExampleResources.OpenRejectedSettings, Media.PortName, Media.BaudRate,
                    Media.DataBits, Media.Parity, Media.StopBits, Media.Handshake,
                    Media.DtrEnable, Media.RtsEnable, ex.Message), ex);
            }
            lock (sync)
            {
                received.Clear();
                receivedSize = 0;
            }
            if (scope is not null)
            {
                receiver = ReceiveLoopAsync(receiveOptions!, scope, lifetime.Token);
                scope = null;
            }
        }
        finally
        {
            scope?.Dispose();
        }
        log.Add(ExampleResources.Opened);
        return true;
    }, cancellationToken);
    /// <summary>Cancels active work and closes the serial transport.</summary>
    /// <returns>A task completing after active work and transport shutdown finish.</returns>
    public async Task CloseAsync()
    {
        lock (sync) { lifetime.Cancel(); }
        var closing = Media.CloseAsync();
        await operations.WaitAsync().ConfigureAwait(false);
        try
        {
            await closing.ConfigureAwait(false);
            await receiver.ConfigureAwait(false);
            lock (sync) { lifetime.Dispose(); lifetime = new(); }
            log.Add(ExampleResources.ClosedLog);
        }
        finally { operations.Release(); }
    }
    /// <summary>Sends text or hexadecimal bytes and optionally waits for a framed reply.</summary>
    /// <param name="text">The UTF-8 text or hexadecimal bytes to send; in text mode, literal \r and \n sequences become carriage returns and line feeds.</param>
    /// <param name="hex">Whether the text and end marker are hexadecimal.</param>
    /// <param name="receiveReply">Whether to wait synchronously for a reply.</param>
    /// <param name="eop">The optional end of packet marker.</param>
    /// <param name="count">The minimum reply byte count, or zero for marker based framing.</param>
    /// <param name="waitTime">The receive timeout in milliseconds.</param>
    /// <param name="cancellationToken">Cancels sending and receiving.</param>
    /// <returns>The reply as ASCII text or hexadecimal when selected or non-ASCII, or null when no reply was requested.</returns>
    /// <exception cref="TimeoutException">No complete reply arrives before the timeout.</exception>
    /// <exception cref="FormatException">A hexadecimal value is malformed.</exception>
    public Task<string?> SendAsync(string text, bool hex = false, bool receiveReply = false, string? eop = null, int count = 0, int waitTime = 5000, CancellationToken cancellationToken = default)
        => RunAsync<string?>(async token =>
        {
            var data = hex ? Convert.FromHexString(string.Concat(text.Where(c => !char.IsWhiteSpace(c)))) : Encoding.UTF8.GetBytes(text.Replace(@"\r", "\r", StringComparison.Ordinal).Replace(@"\n", "\n", StringComparison.Ordinal));
            using var scope = receiveReply ? Media.BeginSynchronous() : null;
            if (receiveReply) { Media.ResetSynchronousBuffer(); }
            await Media.SendAsync(data, cancellationToken: token).ConfigureAwait(false);
            log.Add(string.Format(CultureInfo.CurrentCulture, ExampleResources.Tx, ExampleDataDisplay.Format(data, hex)));
            if (!receiveReply) { return null; }
            var parameters = new ReceiveParameters<byte[]> { Count = count, WaitTime = waitTime, Eop = string.IsNullOrEmpty(eop) ? null : (hex ? Convert.FromHexString(string.Concat(eop.Where(c => !char.IsWhiteSpace(c)))) : Encoding.UTF8.GetBytes(eop)), AllData = count == 0 && string.IsNullOrEmpty(eop) };
            if (!await Media.ReceiveAsync(parameters, token).ConfigureAwait(false)) { throw new TimeoutException(ExampleResources.Timeout); }
            var reply = parameters.Reply ?? [];
            var result = ExampleDataDisplay.Format(reply, hex);
            log.Add(string.Format(CultureInfo.CurrentCulture, ExampleResources.Reply, result));
            return result;
        }, cancellationToken);
    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        lock (sync) { if (disposed) { return; } disposed = true; }
        await CloseAsync().ConfigureAwait(false);
        Media.OnReceived -= Received;
        Media.OnError -= Error;
        await Media.DisposeAsync().ConfigureAwait(false);
    }
}
