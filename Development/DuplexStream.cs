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
namespace Gurux.Serial;
/// <summary>A readable and writable stream pair with an owned native connection.</summary>
/// <param name="input">The readable stream or bulk IN endpoint owned by this adapter.</param>
/// <param name="output">The writable stream or bulk OUT endpoint owned by this adapter.</param>
/// <param name="close">Releases the platform connection when the duplex stream is disposed.</param>
internal sealed class DuplexStream(Stream input, Stream output, Action close) : Stream
{
    /// <summary>Records whether this stream has released its owned connection.</summary>
    int disposed;
    /// <inheritdoc/>
    public override bool CanRead => Volatile.Read(ref disposed) == 0 && input.CanRead;
    /// <inheritdoc/>
    public override bool CanWrite => Volatile.Read(ref disposed) == 0 && output.CanWrite;
    /// <inheritdoc/>
    public override bool CanSeek => false;
    /// <inheritdoc/>
    public override long Length => throw new NotSupportedException();
    /// <inheritdoc/>
    public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

    /// <inheritdoc/>
    public override int Read(byte[] b, int o, int c) => input.Read(b, o, c);
    /// <inheritdoc/>
    public override ValueTask<int> ReadAsync(Memory<byte> b, CancellationToken cancellationToken = default) => input.ReadAsync(b, cancellationToken);
    /// <inheritdoc/>
    public override void Write(byte[] b, int o, int c) => output.Write(b, o, c);
    /// <inheritdoc/>
    public override ValueTask WriteAsync(ReadOnlyMemory<byte> b, CancellationToken cancellationToken = default) => output.WriteAsync(b, cancellationToken);
    /// <inheritdoc/>
    public override void Flush() => output.Flush();
    /// <inheritdoc/>
    public override Task FlushAsync(CancellationToken cancellationToken) => output.FlushAsync(cancellationToken);
    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (Interlocked.Exchange(ref disposed, 1) == 0)
        {
            try
            {
                close();
            }
            finally
            {
                input.Dispose();
                if (!ReferenceEquals(input, output))
                {
                    output.Dispose();
                }
            }
        }

        base.Dispose(disposing);
    }

    /// <inheritdoc/>
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    /// <inheritdoc/>
    public override void SetLength(long value) => throw new NotSupportedException();
}
