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
using System.Threading.Channels;

namespace Gurux.Serial;

/// <summary>Turns GATT notifications and writes into an owned, bounded asynchronous byte stream.</summary>
internal sealed class DesktopBluetoothLeStream : Stream
{
    /// <summary>Bounds complete notifications awaiting a reader.</summary>
    private readonly Channel<byte[]> _incoming;
    /// <summary>Serializes readers so partial notifications retain their ordering.</summary>
    private readonly SemaphoreSlim _readers = new(1, 1);
    /// <summary>Serializes entire writes to prevent interleaved attribute chunks.</summary>
    private readonly SemaphoreSlim _writers = new(1, 1);
    /// <summary>Ends blocked operations when the stream fails or closes.</summary>
    private readonly CancellationTokenSource _lifetime = new();
    /// <summary>Limits the duration of a complete output operation.</summary>
    private readonly TimeSpan _operationTimeout;
    /// <summary>Owns the active native connection until atomically released.</summary>
    private IBluetoothLeConnection? _connection;
    /// <summary>Retains the current notification across small caller buffers.</summary>
    private byte[]? _pending;
    /// <summary>Indexes the first unread byte in the retained notification.</summary>
    private int _pendingOffset;
    /// <summary>Records the first transport failure for blocked and future operations.</summary>
    private Exception? _failure;
    /// <summary>Gets whether a native callback has reported a terminal failure.</summary>
    private bool HasFailure => Volatile.Read(ref _failure) != null;
    /// <summary>Indicates that disposal has begun.</summary>
    private int _disposed;
    /// <summary>Serializes cleanup task publication without holding a lock across awaits.</summary>
    private readonly object _cleanupSync = new();
    /// <summary>Allows every disposer to await the same native cleanup operation.</summary>
    private Task _cleanup = Task.CompletedTask;

    /// <summary>Initializes a bounded notification stream before native callbacks can occur.</summary>
    /// <param name="timeout">Maximum time for opening and complete writes.</param>
    /// <param name="capacity">Maximum queued notifications.</param>
    private DesktopBluetoothLeStream(TimeSpan timeout, int capacity)
    {
        _operationTimeout = timeout;
        _incoming = Channel.CreateBounded<byte[]>(new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false,
            AllowSynchronousContinuations = false
        });
    }

    /// <summary>Opens a native connection and rolls back failed or late completion.</summary>
    /// <param name="connect">Creates the native connection using stream-owned callbacks.</param>
    /// <param name="timeout">Maximum time for opening and complete writes.</param>
    /// <param name="capacity">Maximum queued notifications before overflow fails the connection.</param>
    /// <param name="cancellationToken">Cancels opening.</param>
    /// <returns>The owned notification stream.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The timeout or queue capacity is invalid.</exception>
    /// <exception cref="TimeoutException">Opening exceeds the configured timeout.</exception>
    internal static async Task<Stream> OpenAsync(Func<Action<byte[]>, Action<Exception>, CancellationToken, Task<IBluetoothLeConnection>> connect, TimeSpan timeout, int capacity, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(connect);
        if (timeout <= TimeSpan.Zero || timeout.TotalMilliseconds > uint.MaxValue - 1)
        {
            throw new ArgumentOutOfRangeException(nameof(timeout), Properties.Resources.Get("BluetoothTimeoutInvalid"));
        }
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);
        var stream = new DesktopBluetoothLeStream(timeout, capacity);
        using var cancel = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, stream._lifetime.Token);
        cancel.CancelAfter(timeout);
        Task<IBluetoothLeConnection>? opening = null;
        bool claimed = false;
        bool succeeded = false;
        try
        {
            opening = connect(stream.Receive, stream.Fail, cancel.Token);
            var native = await opening.WaitAsync(cancel.Token).ConfigureAwait(false);
            stream.ClaimConnection(native);
            claimed = true;
            cancel.Token.ThrowIfCancellationRequested();
            stream.CheckAvailable();
            succeeded = true;
            return stream;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested && !stream._lifetime.IsCancellationRequested)
        {
            throw new TimeoutException(Properties.Resources.Get("BluetoothLeOperationTimedOut"));
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested && stream.HasFailure)
        {
            stream.CheckAvailable();
            throw;
        }
        finally
        {
            if (!succeeded)
            {
                await stream.DisposeAsync().ConfigureAwait(false);
                if (opening != null && !claimed)
                {
                    // Observe late success and failure even when a native API ignores cancellation.
                    _ = ReleaseLateConnectionAsync(opening);
                }
            }
        }
    }

    /// <summary>Transfers native connection ownership to this stream.</summary>
    /// <param name="native">The successfully opened connection.</param>
    private void ClaimConnection(IBluetoothLeConnection native) => Interlocked.Exchange(ref _connection, native);

    /// <summary>Releases a connection completing after its opening caller stopped waiting.</summary>
    /// <param name="opening">The in-flight native open operation.</param>
    /// <returns>An operation completing after late results have been observed.</returns>
    private static async Task ReleaseLateConnectionAsync(Task<IBluetoothLeConnection> opening)
    {
        try
        {
            await (await opening.ConfigureAwait(false)).DisposeAsync().ConfigureAwait(false);
        }
        catch
        {
            // The opening caller has already received the original failure.
        }
    }

    /// <summary>Queues a copy of one attribute notification or fails on overflow.</summary>
    /// <param name="value">The received attribute payload.</param>
    private void Receive(byte[] value)
    {
        if (Volatile.Read(ref _disposed) != 0 || Volatile.Read(ref _failure) != null || value.Length == 0)
        {
            return;
        }
        if (value.Length > 512 || !_incoming.Writer.TryWrite(value.ToArray()))
        {
            Fail(new IOException(Properties.Resources.Get("BluetoothLeReceiveOverflow")));
        }
    }

    /// <summary>Records the first failure and wakes readers and writers without blocking callbacks.</summary>
    /// <param name="error">The connection or queue failure.</param>
    private void Fail(Exception error)
    {
        if (Interlocked.CompareExchange(ref _failure, error, null) == null)
        {
            _incoming.Writer.TryComplete(error);
            _lifetime.Cancel();
            _ = ReleaseConnectionAsync();
        }
    }

    /// <summary>Rejects operations after transport failure or disposal.</summary>
    /// <exception cref="IOException">The connection failed.</exception>
    /// <exception cref="ObjectDisposedException">The stream was disposed.</exception>
    private void CheckAvailable()
    {
        var error = Volatile.Read(ref _failure);
        if (error != null)
        {
            throw new IOException(Properties.Resources.Get("BluetoothLeDisconnected"), error);
        }
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
    }

    /// <inheritdoc/>
    public override bool CanRead => Volatile.Read(ref _disposed) == 0 && Volatile.Read(ref _failure) == null;
    /// <inheritdoc/>
    public override bool CanWrite => CanRead;
    /// <inheritdoc/>
    public override bool CanSeek => false;
    /// <inheritdoc/>
    public override long Length => throw new NotSupportedException();
    /// <inheritdoc/>
    public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
    /// <inheritdoc/>
    public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException(Properties.Resources.Get("BluetoothLeAsyncOnly"));
    /// <inheritdoc/>
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException(Properties.Resources.Get("BluetoothLeAsyncOnly"));
    /// <inheritdoc/>
    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();
    /// <inheritdoc/>
    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => WriteAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

    /// <inheritdoc/>
    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        CheckAvailable();
        if (buffer.Length == 0)
        {
            return 0;
        }
        using var cancel = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _lifetime.Token);
        bool entered = false;
        try
        {
            await _readers.WaitAsync(cancel.Token).ConfigureAwait(false);
            entered = true;
            CheckAvailable();
            if (_pending == null)
            {
                _pending = await _incoming.Reader.ReadAsync(cancel.Token).ConfigureAwait(false);
                _pendingOffset = 0;
            }
            CheckAvailable();
            cancellationToken.ThrowIfCancellationRequested();
            int count = Math.Min(buffer.Length, _pending.Length - _pendingOffset);
            _pending.AsMemory(_pendingOffset, count).CopyTo(buffer);
            _pendingOffset += count;
            if (_pendingOffset == _pending.Length)
            {
                _pending = null;
            }
            return count;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            CheckAvailable();
            throw;
        }
        catch (ChannelClosedException error)
        {
            CheckAvailable();
            throw new IOException(Properties.Resources.Get("BluetoothLeDisconnected"), error);
        }
        finally
        {
            if (entered)
            {
                _readers.Release();
            }
        }
    }

    /// <inheritdoc/>
    public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        CheckAvailable();
        using var cancel = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _lifetime.Token);
        cancel.CancelAfter(_operationTimeout);
        bool entered = false;
        bool started = false;
        try
        {
            await _writers.WaitAsync(cancel.Token).ConfigureAwait(false);
            entered = true;
            CheckAvailable();
            var native = Volatile.Read(ref _connection) ?? throw new ObjectDisposedException(nameof(DesktopBluetoothLeStream));
            int chunkSize = Math.Clamp(native.MaxWriteSize, 1, 512);
            while (!buffer.IsEmpty)
            {
                cancel.Token.ThrowIfCancellationRequested();
                int count = Math.Min(buffer.Length, chunkSize);
                started = true;
                await native.WriteAsync(buffer[..count], cancel.Token).AsTask().WaitAsync(cancel.Token).ConfigureAwait(false);
                buffer = buffer[count..];
            }
        }
        catch (Exception error)
        {
            bool timedOut = error is OperationCanceledException && cancel.IsCancellationRequested && !cancellationToken.IsCancellationRequested && !_lifetime.IsCancellationRequested;
            if (started)
            {
                Fail(error);
            }
            if (timedOut)
            {
                throw new TimeoutException(Properties.Resources.Get("BluetoothLeOperationTimedOut"), error);
            }
            throw;
        }
        finally
        {
            if (entered)
            {
                _writers.Release();
            }
        }
    }

    /// <summary>Atomically releases the native connection at most once.</summary>
    /// <returns>An operation completing when connection cleanup finishes.</returns>
    private Task ReleaseConnectionAsync()
    {
        lock (_cleanupSync)
        {
            var native = Interlocked.Exchange(ref _connection, null);
            if (native != null)
            {
                _cleanup = DisposeNativeAsync(native);
            }
            return _cleanup;
        }
    }

    /// <summary>Observes cleanup failures without throwing from native notification callbacks.</summary>
    /// <param name="native">The connection whose ownership was released.</param>
    /// <returns>The observed native cleanup operation.</returns>
    private async Task DisposeNativeAsync(IBluetoothLeConnection native)
    {
        try
        {
            await native.DisposeAsync().ConfigureAwait(false);
        }
        catch (Exception error)
        {
            Interlocked.CompareExchange(ref _failure, error, null);
        }
    }

    /// <inheritdoc/>
    public override async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 0)
        {
            _incoming.Writer.TryComplete();
            _lifetime.Cancel();
        }
        await ReleaseConnectionAsync().ConfigureAwait(false);
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            // Callback threads must not block waiting for native GATT cleanup.
            _ = DisposeAsync();
        }
        base.Dispose(disposing);
    }
    /// <inheritdoc/>
    public override void Flush() => CheckAvailable();
    /// <inheritdoc/>
    public override Task FlushAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        CheckAvailable();
        return Task.CompletedTask;
    }
    /// <inheritdoc/>
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    /// <inheritdoc/>
    public override void SetLength(long value) => throw new NotSupportedException();
}
