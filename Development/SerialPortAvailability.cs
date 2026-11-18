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

/// <summary>Maintains a throttled availability snapshot for one selected serial device without opening it.</summary>
/// <remarks>Call RefreshAsync while the media is closed. A changed selection is unavailable until its own probe succeeds.
/// Discovery uses a background task; a missing device is never replaced by another port.</remarks>
public sealed class SerialPortAvailability
{
    /// <summary>Protects snapshot identity and pending discovery state.</summary>
    private readonly object _sync = new();
    /// <summary>Checks a physical port or a known native Bluetooth identifier.</summary>
    private readonly Func<string?, CancellationToken, Task<bool>> _probe;
    /// <summary>Limits how often an unchanged selection is enumerated.</summary>
    private readonly long _refreshMilliseconds;
    /// <summary>Identifies the currently selected device.</summary>
    private string? _selected;
    /// <summary>Records the last completed result for the current selection.</summary>
    private bool _available;
    /// <summary>Prevents stale discovery results from enabling a different selection.</summary>
    private long _generation;
    /// <summary>Stores the next monotonic time at which a repeated probe may start.</summary>
    private long _nextRefresh;
    /// <summary>Coalesces repeated refresh calls for an unchanged selection.</summary>
    private Task _pending = Task.CompletedTask;

    /// <summary>Creates an initially unavailable snapshot with optional host-provided discovery.</summary>
    /// <param name="probe">A discovery callback; defaults to GXSerial.IsPortAvailableAsync.</param>
    /// <param name="refreshInterval">Minimum interval for an unchanged selection; defaults to one second. Zero disables throttling.</param>
    /// <exception cref="ArgumentOutOfRangeException">The interval is negative or exceeds the supported timer range.</exception>
    public SerialPortAvailability(Func<string?, CancellationToken, Task<bool>>? probe = null, TimeSpan? refreshInterval = null)
    {
        var interval = refreshInterval ?? TimeSpan.FromSeconds(1);
        if (interval < TimeSpan.Zero || interval.TotalMilliseconds > int.MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(refreshInterval));
        }
        this._probe = probe ?? GXSerial.IsPortAvailableAsync;
        _refreshMilliseconds = (long)interval.TotalMilliseconds;
    }

    /// <summary>Gets whether the current selection was found by its latest successful discovery.</summary>
    public bool IsAvailable
    {
        get
        {
            lock (_sync)
            {
                return _available;
            }
        }
    }

    /// <summary>Refreshes the selected device in the background, coalescing repeated probes and rejecting stale results.</summary>
    /// <param name="portName">The selected port or native accessory identifier.</param>
    /// <param name="cancellationToken">Cancels discovery, making its result unavailable.</param>
    /// <returns>The current probe or a completed task when no probe is needed. Discovery failures are represented by IsAvailable being false.</returns>
    public Task RefreshAsync(string? portName, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            bool changed = !string.Equals(_selected, portName, StringComparison.Ordinal);
            if (changed)
            {
                _selected = portName;
                _available = false;
                ++_generation;
                _nextRefresh = 0;
            }
            if (string.IsNullOrWhiteSpace(portName) || cancellationToken.IsCancellationRequested)
            {
                _available = false;
                ++_generation;
                return Task.CompletedTask;
            }
            if (!changed && !_pending.IsCompleted)
            {
                return _pending;
            }
            long now = Environment.TickCount64;
            if (!changed && now < _nextRefresh)
            {
                return Task.CompletedTask;
            }
            _nextRefresh = now + _refreshMilliseconds;
            long version = _generation;
            _pending = Task.Run(() => ProbeAsync(portName, version, cancellationToken));
            return _pending;
        }
    }

    /// <summary>Publishes a discovery result only while it still belongs to the selected device.</summary>
    /// <param name="portName">The device being checked.</param>
    /// <param name="version">The selection generation that started this probe.</param>
    /// <param name="cancellationToken">Cancels the native lookup.</param>
    /// <returns>An operation completing after the result is published or discarded.</returns>
    private async Task ProbeAsync(string portName, long version, CancellationToken cancellationToken)
    {
        bool found;
        try
        {
            found = await _probe(portName, cancellationToken).ConfigureAwait(false) && !cancellationToken.IsCancellationRequested;
        }
        catch (Exception)
        {
            // Permission, driver, service and cancellation failures all disable opening.
            found = false;
        }
        lock (_sync)
        {
            if (version == _generation && string.Equals(_selected, portName, StringComparison.Ordinal))
            {
                _available = found;
            }
        }
    }
}
