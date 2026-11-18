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
namespace Gurux.Serial;

/// <summary>Routes native GATT callbacks to their owning serial stream.</summary>
internal sealed class BluetoothLeCallback : BluetoothGattCallback
{
    /// <summary>Session receiving callback results.</summary>
    private readonly BluetoothLeStream _owner;
    /// <summary>Associates callbacks with one owned session.</summary>
    /// <param name="owner">Stream receiving native events.</param>
    internal BluetoothLeCallback(BluetoothLeStream owner) { this._owner = owner; }
    /// <inheritdoc/>
    public override void OnConnectionStateChange(BluetoothGatt? gatt, GattStatus status, ProfileState newState) => _owner.ConnectionChanged(status, newState);
    /// <inheritdoc/>
    public override void OnServicesDiscovered(BluetoothGatt? gatt, GattStatus status) => _owner.Complete("services", status);
    /// <inheritdoc/>
    public override void OnMtuChanged(BluetoothGatt? gatt, int mtu, GattStatus status) => _owner.MtuChanged(mtu, status);
    /// <inheritdoc/>
    public override void OnDescriptorWrite(BluetoothGatt? gatt, BluetoothGattDescriptor? descriptor, GattStatus status) => _owner.Complete("descriptor", status);
    /// <inheritdoc/>
    public override void OnCharacteristicWrite(BluetoothGatt? gatt, BluetoothGattCharacteristic? characteristic, GattStatus status) => _owner.Complete("write", status);
    /// <inheritdoc/>
    public override void OnCharacteristicChanged(BluetoothGatt gatt, BluetoothGattCharacteristic characteristic, byte[] value) => _owner.Receive(characteristic, value);
    /// <inheritdoc/>
    public override void OnCharacteristicChanged(BluetoothGatt? gatt, BluetoothGattCharacteristic? characteristic)
    {
        if (characteristic != null && !OperatingSystem.IsAndroidVersionAtLeast(33))
        {
#pragma warning disable CA1422
            _owner.Receive(characteristic, characteristic.GetValue() ?? Array.Empty<byte>());
#pragma warning restore CA1422
        }
    }
}
#endif
