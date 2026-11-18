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
using System.Text.Json.Nodes;

namespace Gurux.Serial.Example;

/// <summary>Reads and atomically writes this example application's local JSON preferences.</summary>
/// <remarks>Each example owns a separate directory. GURUX_EXAMPLE_SETTINGS_DIRECTORY can isolate test profiles.</remarks>
public sealed class ExampleSettingsStore
{
    /// <summary>Serializes writes so an older snapshot cannot replace a newer one.</summary>
    private readonly SemaphoreSlim _writes = new(1, 1);
    /// <summary>Identifies the JSON file owned by this application.</summary>
    private readonly string _fileName;
    /// <summary>Tracks the last successfully written document to avoid redundant disk writes.</summary>
    private string? _saved;

    /// <summary>Creates a store in this example's directory under the current user's local application data.</summary>
    public ExampleSettingsStore()
    {
        string root = Environment.GetEnvironmentVariable("GURUX_EXAMPLE_SETTINGS_DIRECTORY")
            ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Gurux", "Examples");
        _fileName = Path.Combine(root, "serial", "settings.json");
    }

    /// <summary>Reads the saved JSON object without opening any media connection.</summary>
    /// <returns>The saved preferences, or null when no settings file exists.</returns>
    /// <exception cref="System.Text.Json.JsonException">The saved document is not valid JSON.</exception>
    /// <exception cref="InvalidOperationException">The saved document is not a JSON object.</exception>
    /// <exception cref="IOException">The existing file cannot be read.</exception>
    /// <exception cref="UnauthorizedAccessException">The current user cannot read the existing file.</exception>
    public async Task<JsonObject?> LoadAsync()
    {
        try
        {
            string json = await File.ReadAllTextAsync(_fileName).ConfigureAwait(false);
            return JsonNode.Parse(json)?.AsObject();
        }
        catch (FileNotFoundException)
        {
            return null;
        }
        catch (DirectoryNotFoundException)
        {
            return null;
        }
    }

    /// <summary>Writes a detached snapshot, replacing the destination only after the temporary file is complete.</summary>
    /// <param name="settings">The complete media and interface preferences to persist.</param>
    /// <returns>A task completing after the snapshot is written or an identical snapshot is skipped.</returns>
    /// <exception cref="ArgumentNullException">The settings object is null.</exception>
    /// <exception cref="IOException">The settings directory or file cannot be written.</exception>
    /// <exception cref="UnauthorizedAccessException">The current user cannot write the settings file.</exception>
    public async Task SaveAsync(JsonObject settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        string json = settings.ToJsonString().Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace("\n", "\r\n", StringComparison.Ordinal);
        await _writes.WaitAsync().ConfigureAwait(false);
        string? temporary = null;
        try
        {
            if (_saved == json)
            {
                return;
            }
            Directory.CreateDirectory(Path.GetDirectoryName(_fileName)!);
            temporary = _fileName + "." + Guid.NewGuid().ToString("N") + ".tmp";
            await File.WriteAllTextAsync(temporary, json, new UTF8Encoding(false)).ConfigureAwait(false);
            File.Move(temporary, _fileName, true);
            temporary = null;
            _saved = json;
        }
        finally
        {
            try
            {
                if (temporary is not null)
                {
                    File.Delete(temporary);
                }
            }
            finally
            {
                _writes.Release();
            }
        }
    }
}
