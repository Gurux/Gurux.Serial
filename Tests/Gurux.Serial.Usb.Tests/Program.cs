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
using Gurux.Serial;
using System.IO.Ports;

namespace Gurux.Serial.Usb.Tests;

/// <summary>Runs CH34x control-sequence regressions against the Java reference without Android hardware.</summary>
internal static class Program
{
    /// <summary>Checks version boundaries, reference ordering, configured outputs and transfer failures.</summary>
    /// <returns>Zero when every regression passes; otherwise one.</returns>
    private static int Main()
    {
        var failures = new List<string>();
        foreach (byte version in new byte[] { 0x00, 0x26, 0x27, 0x28, 0x80, 0xff })
        {
            Run(failures, $"chip version {version:X2}", () =>
            {
                var connection = new ScriptedUsbConnection { Version = version };
                Apply(connection);
                int baud = connection.Requests.Last(item => item.Request == 0x9a && item.Value == 0x1312).Index;
                Check(baud == (version > 0x27 ? 0xb282 : 0xb202), "Unexpected version-dependent baud register.");
            });
        }
        Run(failures, "Java initialization sequence", () =>
        {
            var connection = new ScriptedUsbConnection { Version = 0x28 };
            Apply(connection);
            (int, int, int, int, int)[] expected =
            [
                (0x40, 0xa1, 0, 0, 0),
                (0xc0, 0x5f, 0, 0, 2),
                (0x40, 0x9a, 0x1312, 0xd982, 0),
                (0x40, 0x9a, 0x0f2c, 4, 0),
                (0x40, 0x9a, 0x2727, 0, 0),
                (0x40, 0xa4, 0xff, 0, 0),
                (0x40, 0xa1, 0xc39c, 0x88, 0),
                (0x40, 0x9a, 0x1312, 0xb282, 0),
                (0x40, 0x9a, 0x0f2c, 0x13, 0),
                (0x40, 0xa4, 0xff9f, 0, 0)
            ];
            Check(connection.Requests.SequenceEqual(expected), "CH34x initialization differs from the Java sequence or loses configured DTR/RTS.");
        });
        foreach (int result in new[] { -1, 0, 1, 3 })
        {
            Run(failures, $"invalid version response length {result}", () =>
            {
                var connection = new ScriptedUsbConnection { VersionResult = result };
                ExpectIOException(() => Apply(connection));
                Check(connection.Requests.Count == 2, "Initialization continued after an invalid version response.");
            });
        }
        Run(failures, "initial handshake failure", () =>
        {
            var connection = new ScriptedUsbConnection { Version = 0x28, FailAt = 5 };
            ExpectIOException(() => Apply(connection));
            Check(connection.Requests[5].Request == 0xa4 && connection.Requests[5].Value == 0xff,
                "The initial handshake must be checked before line configuration.");
            Check(connection.Requests.Count == 6, "Initialization continued after handshake failure.");
        });
        Run(failures, "cancel before initialization", () =>
        {
            var connection = new ScriptedUsbConnection();
            try
            {
                AndroidUsbConfiguration.Apply(connection, AndroidUsbChipset.Ch34x, 0, Settings(), new CancellationToken(true));
                throw new InvalidOperationException("Cancellation was ignored.");
            }
            catch (OperationCanceledException)
            {
                Check(connection.Requests.Count == 0, "Cancelled initialization touched the device.");
            }
        });
        foreach (string failure in failures)
        {
            Console.Error.WriteLine(failure);
        }
        Console.WriteLine($"CH34x: {13 - failures.Count}/13 scenarios passed.");
        return failures.Count == 0 ? 0 : 1;
    }

    /// <summary>Runs a scenario and retains its failure so the remaining boundaries are exercised.</summary>
    /// <param name="failures">The collected scenario failures.</param>
    /// <param name="name">The internal scenario identifier.</param>
    /// <param name="check">The regression assertions.</param>
    private static void Run(List<string> failures, string name, Action check)
    {
        try
        {
            check();
        }
        catch (Exception error)
        {
            failures.Add(name + ": " + error.Message);
        }
    }

    /// <summary>Creates framing and modem outputs with independently verifiable register values.</summary>
    /// <returns>9600 baud, eight data bits, no parity, one stop bit, and both modem outputs enabled.</returns>
    private static SerialPortSettings Settings() => new()
    {
        BaudRate = 9600,
        DataBits = 8,
        Parity = Parity.None,
        StopBits = StopBits.One,
        Handshake = Handshake.None,
        DtrEnable = true,
        RtsEnable = true
    };

    /// <summary>Runs the production CH34x initialization against a scripted connection.</summary>
    /// <param name="connection">The scripted Android USB boundary.</param>
    private static void Apply(ScriptedUsbConnection connection)
    {
        AndroidUsbConfiguration.Apply(connection, AndroidUsbChipset.Ch34x, 0, Settings(), CancellationToken.None);
    }

    /// <summary>Rejects a failed regression assertion.</summary>
    /// <param name="condition">Whether the expected behavior occurred.</param>
    /// <param name="message">The internal diagnostic for an unexpected result.</param>
    /// <exception cref="InvalidOperationException">The assertion fails.</exception>
    private static void Check(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    /// <summary>Requires a control-transfer failure to surface as an I/O error.</summary>
    /// <param name="action">The initialization expected to fail.</param>
    /// <exception cref="InvalidOperationException">Initialization incorrectly succeeds.</exception>
    private static void ExpectIOException(Action action)
    {
        try
        {
            action();
        }
        catch (IOException)
        {
            return;
        }
        throw new InvalidOperationException("Expected an I/O failure.");
    }
}
