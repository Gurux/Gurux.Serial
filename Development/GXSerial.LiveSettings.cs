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

using System.IO.Ports;

namespace Gurux.Serial;

/// <summary>Applies live framing changes without replacing the active serial transport.</summary>
public partial class GXSerial
{
    /// <summary>Identifies injected configuration callbacks that may close this media reentrantly.</summary>
    private readonly AsyncLocal<bool> inSettingsApply = new();

    /// <inheritdoc/>
    public async Task ApplySettingsAsync(SerialPortSettings settings, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);
        SerialPortSettings next = CopyLineSettings(settings);
        next.Validate();
        cancellationToken.ThrowIfCancellationRequested();
        Stream? observed;
        CancellationTokenSource waiting;
        lock (lifecycle)
        {
            observed = stream;
            waiting = stopping == null
                ? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken)
                : CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, stopping.Token);
        }
        using var waitingLifetime = waiting;
        await writer.WaitAsync(waiting.Token).ConfigureAwait(false);
        TaskCompletionSource? completed = null;
        Stream? active = null;
        CancellationTokenSource? linked = null;
        bool previousCallback = inSettingsApply.Value;
        try
        {
            SerialPort? owner;
            lock (lifecycle)
            {
                waiting.Token.ThrowIfCancellationRequested();
                if (!ReferenceEquals(stream, observed))
                {
                    throw new OperationCanceledException(waiting.Token);
                }
                if (openingDone != null || (stream == null && workers.Any(task => !task.IsCompleted)))
                {
                    throw new InvalidOperationException(global::Gurux.Serial.Properties.Resources.Get("LiveSettingsLifecycleBusy"));
                }
                active = stream;
                owner = port;
                if (active == null)
                {
                    PortSettings = next;
                    return;
                }
                if (!SameDevice(PortSettings, next))
                {
                    throw new ArgumentException(global::Gurux.Serial.Properties.Resources.Get("LiveSettingsDeviceChange"), nameof(settings));
                }
                if (owner == null && active is not IGXSerialSettings)
                {
                    throw new NotSupportedException(global::Gurux.Serial.Properties.Resources.Get("LiveSettingsUnsupported"));
                }
                linked = CancellationTokenSource.CreateLinkedTokenSource(waiting.Token, stopping!.Token);
                completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                workers.Add(completed.Task);
            }
            inSettingsApply.Value = true;
            if (owner != null)
            {
                await Task.Run(() =>
                {
                    lock (lifecycle)
                    {
                        linked.Token.ThrowIfCancellationRequested();
                        if (!ReferenceEquals(stream, active) || !ReferenceEquals(port, owner))
                        {
                            throw new OperationCanceledException(linked.Token);
                        }
                        ApplyPhysicalSettings(owner, next);
                        PortSettings = next;
                    }
                }, linked.Token).ConfigureAwait(false);
            }
            else
            {
                await ((IGXSerialSettings)active).ApplySettingsAsync(next, linked.Token).ConfigureAwait(false);
                lock (lifecycle)
                {
                    if (!ReferenceEquals(stream, active))
                    {
                        throw new OperationCanceledException(linked.Token);
                    }
                    PortSettings = next;
                }
            }
        }
        finally
        {
            inSettingsApply.Value = previousCallback;
            linked?.Dispose();
            if (completed != null)
            {
                completed.TrySetResult();
                lock (lifecycle) { workers.Remove(completed.Task); }
            }
            writer.Release();
        }
    }

    /// <summary>Copies settings before asynchronous work so caller mutations cannot alter an admitted change.</summary>
    /// <param name="settings">Settings whose values are copied.</param>
    /// <returns>A detached line-settings object retaining the device and Bluetooth selection.</returns>
    private static SerialPortSettings CopyLineSettings(SerialPortSettings settings) => new()
    {
        PortName = settings.PortName, BluetoothChannel = settings.BluetoothChannel, BluetoothProfile = settings.BluetoothProfile,
        BaudRate = settings.BaudRate, DataBits = settings.DataBits, Parity = settings.Parity, StopBits = settings.StopBits,
        Handshake = settings.Handshake, DtrEnable = settings.DtrEnable, RtsEnable = settings.RtsEnable
    };

    /// <summary>Checks that a live update does not select a different physical or Bluetooth endpoint.</summary>
    /// <param name="current">Settings associated with the current opening.</param>
    /// <param name="next">Requested settings.</param>
    /// <returns>True when the native device selection is unchanged.</returns>
    private static bool SameDevice(SerialPortSettings current, SerialPortSettings next) =>
        string.Equals(current.PortName, next.PortName, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal)
        && current.BluetoothChannel == next.BluetoothChannel && ReferenceEquals(current.BluetoothProfile, next.BluetoothProfile);

    /// <summary>Applies real operating-system settings and attempts recovery if a property setter fails.</summary>
    /// <param name="owner">Open desktop port protected by the lifecycle and send locks.</param>
    /// <param name="next">Validated line values.</param>
    /// <exception cref="NotSupportedException">The Linux TTY has no modem-control API and the request requires modem lines.</exception>
    /// <exception cref="AggregateException">Applying and restoring settings both failed; the physical port is closed.</exception>
    private static void ApplyPhysicalSettings(SerialPort owner, SerialPortSettings next)
    {
        bool modemSignals = ReadPhysicalSignals(owner, out bool dtr, out bool rts);
        if (!modemSignals && (next.DtrEnable || next.RtsEnable
            || next.Handshake is Handshake.RequestToSend or Handshake.RequestToSendXOnXOff))
        {
            throw new NotSupportedException(global::Gurux.Serial.Properties.Resources.Get("LiveSettingsModemSignalsUnsupported"));
        }
        var previous = new SerialPortSettings
        {
            PortName = owner.PortName, BaudRate = owner.BaudRate, DataBits = owner.DataBits,
            Parity = owner.Parity, StopBits = owner.StopBits, Handshake = owner.Handshake,
            DtrEnable = dtr, RtsEnable = rts
        };
        try { SetPhysicalSettings(owner, next, modemSignals); }
        catch (Exception applyFailure)
        {
            try { SetPhysicalSettings(owner, previous, modemSignals); }
            catch (Exception recoveryFailure)
            {
                try { owner.Close(); }
                catch (Exception closeFailure) { throw new AggregateException(applyFailure, recoveryFailure, closeFailure); }
                throw new AggregateException(applyFailure, recoveryFailure);
            }
            throw;
        }
    }

    /// <summary>Reads manual signal state while recognizing Linux TTYs that report ENOTTY for modem-control queries.</summary>
    /// <param name="owner">Open physical serial port.</param><param name="dtr">Current DTR value, or false when unsupported.</param>
    /// <param name="rts">Current manual RTS value, or false when automatic or unsupported.</param>
    /// <returns>True when the TTY exposes modem-control lines; false only for Linux ENOTTY.</returns>
    /// <exception cref="IOException">A native error other than the unsupported Linux modem-control query occurs.</exception>
    private static bool ReadPhysicalSignals(SerialPort owner, out bool dtr, out bool rts)
    {
        try
        {
            dtr = owner.DtrEnable;
            rts = owner.Handshake is Handshake.RequestToSend or Handshake.RequestToSendXOnXOff ? false : owner.RtsEnable;
            return true;
        }
        catch (IOException failure) when (OperatingSystem.IsLinux() && (failure.HResult & 0xffff) == 25)
        {
            dtr = false;
            rts = false;
            return false;
        }
    }

    /// <summary>Sets standard framing and flow control, leaving RTS under the driver when hardware handshaking controls it.</summary>
    /// <param name="owner">Open physical port.</param>
    /// <param name="settings">Settings to apply.</param>
    /// <param name="modemSignals">Whether this device supports DTR/RTS queries and changes.</param>
    /// <remarks>Linux TTYs without modem-control support still receive actual framing and software flow-control changes.</remarks>
    private static void SetPhysicalSettings(SerialPort owner, SerialPortSettings settings, bool modemSignals)
    {
        owner.Handshake = Handshake.None;
        owner.BaudRate = settings.BaudRate;
        owner.DataBits = settings.DataBits;
        owner.Parity = settings.Parity;
        owner.StopBits = settings.StopBits;
        if (modemSignals) { owner.DtrEnable = settings.DtrEnable; }
        owner.Handshake = settings.Handshake;
        if (modemSignals && settings.Handshake is not (Handshake.RequestToSend or Handshake.RequestToSendXOnXOff))
        {
            owner.RtsEnable = settings.RtsEnable;
        }
    }
}