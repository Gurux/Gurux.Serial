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

/// <summary>Owns one selected device's explicit GATT service query and rejects stale results after selection changes.</summary>
public sealed class BluetoothGattServiceQuery : IDisposable, IAsyncDisposable
{
    /// <summary>Protects query identity, generation and result snapshots.</summary>
    private readonly object _gate = new();
    /// <summary>Reads metadata without opening or configuring a serial stream.</summary>
    private readonly Func<string, CancellationToken, Task<Guid[]>> _reader;
    /// <summary>Identifies the current selected BLE device.</summary>
    private string? _deviceId;
    /// <summary>Contains the immutable successful service result.</summary>
    private IReadOnlyList<Guid> _services = Array.Empty<Guid>();
    /// <summary>Distinguishes an empty successful response from information not read yet.</summary>
    private bool _hasResult;
    /// <summary>Stores the current localized native failure message.</summary>
    private string? _error;
    /// <summary>Owns cancellation until the current backend has completed cleanup.</summary>
    private CancellationTokenSource? _cancellation;
    /// <summary>Completes only after the active reader and its cleanup have ended.</summary>
    private Task _completion = Task.CompletedTask;
    /// <summary>Invalidates publication from earlier selections or canceled reads.</summary>
    private long _generation;
    /// <summary>Prevents publication or new work after disposal.</summary>
    private bool _disposed;

    /// <summary>Creates an idle query without connecting to a device.</summary>
    /// <param name="readServices">Optional metadata reader supplied by a host; defaults to the platform backend.</param>
    public BluetoothGattServiceQuery(Func<string, CancellationToken, Task<Guid[]>>? readServices = null)
    {
        _reader = readServices ?? BluetoothGattServiceDiscovery.GetServicesAsync;
    }

    /// <summary>Occurs when progress or results change; callbacks may run on a native worker thread.</summary>
    /// <remarks>Observer exceptions are isolated so UI notification failures cannot prevent native work or its cleanup.</remarks>
    public event Action? Changed;
    /// <summary>Gets the selected native BLE identity, or null when no BLE device is selected.</summary>
    public string? DeviceId { get { lock (_gate) { return _deviceId; } } }
    /// <summary>Gets the actual service result for the current selected identity.</summary>
    public IReadOnlyList<Guid> Services { get { lock (_gate) { return _services; } } }
    /// <summary>Returns only service UUIDs not already shown in the selected profile's four UUID fields.</summary>
    /// <param name="profile">The profile whose service, notification, output and optional configuration UUIDs are excluded; null excludes nothing.</param>
    /// <returns>An independent read-only snapshot of the remaining UUIDs without altering the complete native result.</returns>
    public IReadOnlyList<Guid> GetAdditionalServices(BluetoothLeProfile? profile)
    {
        lock (_gate)
        {
            return Array.AsReadOnly(_services.Where(uuid => profile is null
                || (uuid != profile.Service && uuid != profile.ReadCharacteristic
                    && uuid != profile.WriteCharacteristic && uuid != profile.ConfigurationCharacteristic)).ToArray());
        }
    }
    /// <summary>Gets whether a successful read has completed for the current selection, including an empty response.</summary>
    public bool HasResult { get { lock (_gate) { return _hasResult; } } }
    /// <summary>Gets the latest localized native error, or null when the query has not failed.</summary>
    public string? Error { get { lock (_gate) { return _error; } } }
    /// <summary>Gets whether native work or its cancellation cleanup is still pending.</summary>
    public bool IsBusy { get { lock (_gate) { return _cancellation is not null; } } }
    /// <summary>Gets a task completing after the current operation and native cleanup finish.</summary>
    public Task Completion { get { lock (_gate) { return _completion; } } }

    /// <summary>Changes the selected BLE identity without reading services and cancels any previous selection's work.</summary>
    /// <param name="deviceId">The selected native BLE identity; null or blank disables metadata reading.</param>
    /// <remarks>Updates after disposal are ignored so detached UI callbacks cannot restart discovery.</remarks>
    public void SelectDevice(string? deviceId)
    {
        CancellationTokenSource? previous;
        lock (_gate)
        {
            if (_disposed) { return; }
            string? selected = string.IsNullOrWhiteSpace(deviceId) ? null : deviceId;
            if (string.Equals(_deviceId, selected, StringComparison.Ordinal)) { return; }
            _deviceId = selected;
            _services = Array.Empty<Guid>();
            _hasResult = false;
            _error = null;
            ++_generation;
            previous = _cancellation;
        }
        CancelSource(previous);
        NotifyChanged();
    }

    /// <summary>Explicitly reads all services for the selected device, coalescing concurrent calls until cleanup finishes.</summary>
    /// <param name="cancellationToken">Cancels this read and its native session.</param>
    /// <returns>A task completing after result publication or failure and native cleanup; failures are exposed by <see cref="Error"/>.</returns>
    /// <exception cref="ObjectDisposedException">The query has been disposed.</exception>
    /// <exception cref="InvalidOperationException">No BLE identity is selected.</exception>
    public Task ReadAsync(CancellationToken cancellationToken = default)
    {
        string id;
        long generation;
        CancellationTokenSource source;
        TaskCompletionSource completion;
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (_cancellation is not null) { return _completion; }
            id = _deviceId ?? throw new InvalidOperationException(Properties.Resources.Get("BluetoothLeDeviceNotFound"));
            source = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _cancellation = source;
            generation = ++_generation;
            _services = Array.Empty<Guid>();
            _hasResult = false;
            _error = null;
            completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            _completion = completion.Task;
        }
        NotifyChanged();
        _ = ExecuteAsync(id, generation, source, completion);
        return completion.Task;
    }

    /// <summary>Cancels the current read and invalidates its results while retaining busy state until native cleanup ends.</summary>
    public void Cancel()
    {
        CancellationTokenSource? current;
        lock (_gate)
        {
            ++_generation;
            current = _cancellation;
            _error = null;
        }
        CancelSource(current);
    }

    /// <summary>Reads and publishes a result only for the generation that requested it.</summary>
    /// <param name="id">The identity captured before native work starts.</param>
    /// <param name="generation">The generation permitted to publish results.</param>
    /// <param name="source">The active operation cancellation owner.</param>
    /// <param name="completion">Signals that native cleanup and state publication have ended.</param>
    /// <returns>The backend and publication lifetime.</returns>
    private async Task ExecuteAsync(string id, long generation, CancellationTokenSource source, TaskCompletionSource completion)
    {
        try
        {
            source.Token.ThrowIfCancellationRequested();
            Guid[] result = await _reader(id, source.Token).ConfigureAwait(false);
            source.Token.ThrowIfCancellationRequested();
            lock (_gate)
            {
                if (!_disposed && generation == _generation)
                {
                    _services = Array.AsReadOnly(result.Where(uuid => uuid != Guid.Empty).Distinct().OrderBy(uuid => uuid).ToArray());
                    _hasResult = true;
                }
            }
        }
        catch (OperationCanceledException) when (source.IsCancellationRequested) { }
        catch (Exception error)
        {
            lock (_gate)
            {
                if (!_disposed && generation == _generation) { _error = error.GetBaseException().Message; }
            }
        }
        finally
        {
            bool notify;
            lock (_gate)
            {
                if (ReferenceEquals(_cancellation, source)) { _cancellation = null; }
                notify = !_disposed;
            }
            source.Dispose();
            completion.TrySetResult();
            if (notify) { NotifyChanged(); }
        }
    }

    /// <summary>Notifies observers independently without transferring native-operation ownership to UI code.</summary>
    private void NotifyChanged()
    {
        Action? observers;
        lock (_gate) { observers = _disposed ? null : Changed; }
        if (observers is null) { return; }
        foreach (Action observer in observers.GetInvocationList())
        {
            try { observer(); }
            catch (Exception) { /* Observers cannot prevent reader startup or native cleanup. */ }
        }
    }
    /// <summary>Cancels an operation while isolating callback failures and concurrent completed-reader cleanup.</summary>
    /// <param name="source">The cancellation owner, if a query is pending.</param>
    private static void CancelSource(CancellationTokenSource? source)
    {
        try { source?.Cancel(); }
        catch (ObjectDisposedException) { /* The reader already completed native cleanup. */ }
        catch (AggregateException) { /* Cancellation callbacks cannot bypass awaiting native cleanup. */ }
    }

    /// <summary>Cancels pending work and prevents further result publication; use asynchronous disposal to await native cleanup.</summary>
    public void Dispose()
    {
        lock (_gate) { _disposed = true; ++_generation; Changed = null; }
        Cancel();
    }

    /// <summary>Cancels work and awaits native cleanup before a caller starts another connection.</summary>
    /// <returns>The completion of the outstanding metadata session cleanup.</returns>
    public async ValueTask DisposeAsync()
    {
        Dispose();
        await Completion.ConfigureAwait(false);
    }
}