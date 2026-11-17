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
using Microsoft.AspNetCore.Components;
namespace Gurux.Serial.Blazor;

/// <summary>Renders catalogue-only Bluetooth selection and collapsed read-only device information for the application host.</summary>
public partial class BluetoothDevicePicker : IDisposable, IAsyncDisposable
{
    /// <summary>Gets or sets the settings draft's Bluetooth state.</summary>
    [Parameter, EditorRequired]
    public BluetoothDeviceSelection Model { get; set; } = default!;
    /// <summary>Notifies the containing form to refresh its port selection and Save availability.</summary>
    [Parameter]
    public EventCallback Changed { get; set; }
    /// <summary>Gets or sets cancellation for the containing editing session.</summary>
    [Parameter]
    public CancellationToken CancellationToken { get; set; }
    /// <summary>Tracks the subscribed model when the containing draft is replaced.</summary>
    private BluetoothDeviceSelection? _subscribed;
    /// <summary>Stores a localized input rejection without changing source settings.</summary>
    private string? error;
    /// <summary>Prevents queued callbacks from rendering after removal.</summary>
    private bool _disposed;

    /// <inheritdoc/>
    protected override void OnParametersSet()
    {
        Model.SynchronizeServiceSelection();
        if (ReferenceEquals(Model, _subscribed)) { return; }
        if (_subscribed is not null) { _subscribed.Changed -= OnChanged; }
        _subscribed = Model;
        _subscribed.Changed += OnChanged;
    }
    /// <summary>Dispatches native discovery notifications onto the component's render context.</summary>
    private void OnChanged()
    {
        if (!_disposed)
        {
            _ = InvokeAsync(async () =>
            {
                if (!_disposed) { await Changed.InvokeAsync(); StateHasChanged(); }
            });
        }
    }
    /// <summary>Starts or stops the protocol's discovery operation.</summary>
    /// <returns>The discovery and native cleanup lifetime.</returns>
    private async Task Find()
    {
        error = null;
        await Model.FindAsync();
    }
    /// <summary>Changes discovery protocol and discards feedback from previous inputs.</summary>
    /// <param name="args">The selected protocol identifier.</param>
    private void ChangeTransport(ChangeEventArgs args)
    {
        error = null;
        if (Enum.TryParse(args.Value?.ToString(), out BluetoothSerialTransport transport)) { Model.Transport = transport; }
    }
    /// <summary>Uses a listed device without a second confirmation action.</summary>
    /// <param name="args">The selected native device identity.</param>
    private void SelectDevice(ChangeEventArgs args)
    {
        error = null;
        try
        {
            if (args.Value is string id && !string.IsNullOrEmpty(id)) { Model.SelectDevice(id); }
        }
        catch (ArgumentException) { error = Properties.Resources.BluetoothDeviceRequired; }
    }
    /// <summary>Reads complete native services only after explicit activation, or cancels the active read.</summary>
    /// <returns>The metadata read and native cleanup lifetime.</returns>
    private async Task ReadServices()
    {
        if (Model.GattServices.IsBusy) { Model.GattServices.Cancel(); return; }
        Model.SynchronizeServiceSelection();
        await Model.GattServices.ReadAsync(CancellationToken);
    }
    /// <inheritdoc/>
    public void Dispose()
    {
        _disposed = true;
        if (_subscribed is not null)
        {
            _subscribed.Changed -= OnChanged;
            _subscribed.GattServices.Cancel();
        }
    }
    /// <summary>Cancels an in-progress read and awaits cleanup when this component leaves the UI.</summary>
    /// <returns>Completion after the selected metadata reader has released its native session.</returns>
    public async ValueTask DisposeAsync()
    {
        Dispose();
        if (_subscribed is not null) { await _subscribed.GattServices.Completion; }
    }
}