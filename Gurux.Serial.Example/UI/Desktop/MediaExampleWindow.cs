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

using Gurux.Common;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Threading;
using Gurux.Serial.Example.Desktop.Properties;
using System.Globalization;
using Gurux.Serial.Example.Properties;
using System.Text.Json.Nodes;

using StatusResources = Gurux.Serial.Example.Desktop.Properties.ExampleSettingsResources;

namespace Gurux.Serial.Example.Desktop;

/// <summary>Provides native controls for the serial send and receive example.</summary>
public sealed class MediaExampleWindow : Window, IAsyncDisposable
{
    /// <summary>Displays connection state and selected endpoints in the fixed bottom status bar.</summary>
    private readonly TextBlock _mediaStatus = new() { Name = "MediaStatus" };
    /// <summary>Shows indeterminate connection progress only while the transport is opening or closing.</summary>
    private readonly ProgressBar _connectionProgress = new() { Name = "ConnectionProgress", IsIndeterminate = true, IsVisible = false, Height = 4 };

    /// <summary>Persists this application's media and outgoing data preferences.</summary>
    private readonly ExampleSettingsStore _preferences = new();
    /// <summary>Completes when startup preferences have been applied on the UI thread.</summary>
    private Task _preferencesLoading = Task.CompletedTask;
    /// <summary>Tracks the latest queued write so shutdown waits for the final disk update.</summary>
    private Task _preferencesSaving = Task.CompletedTask;
    /// <summary>Prevents persistence until startup restoration has completed.</summary>
    private bool _preferencesReady;
    /// <summary>Reserves navigation and media commands while the modal settings editor owns input.</summary>
    private bool _settingsShowing;
    /// <summary>Tracks the most recently requested snapshot and avoids saving unchanged controls.</summary>
    private string? _preferencesSnapshot;

    /// <summary>Stores the latest transport connection phase independently of command activity.</summary>
    private int _connectionPhase;
    /// <summary>Prevents callbacks from refreshing disposed resources.</summary>
    private int _shuttingDown;
    /// <summary>Cancels UI operations during shutdown.</summary>
    private readonly CancellationTokenSource _lifetime = new();
    /// <summary>Tracks discovery of the selected serial port without blocking the UI thread.</summary>
    private readonly global::Gurux.Serial.SerialPortAvailability _portAvailability = new();
    /// <summary>Owns the serial connection lifecycle.</summary>
    private readonly ExampleSession _session;
    /// <summary>Allows closing an open connection, including canceling an active reply.</summary>
    private Button? _closeButton;
    /// <summary>Tracks commands whose availability depends on connection state.</summary>
    private readonly List<Button> _unsafeButtons = [];
    /// <summary>Tracks the single shutdown operation.</summary>
    private Task? _disposal;
    /// <summary>Refreshes connection state on the UI thread.</summary>
    private readonly DispatcherTimer _timer;
    /// <summary>Displays the current connection state.</summary>
    private readonly TextBlock _state = new() { Name = "ConnectionState" };
    /// <summary>Displays action errors and replies.</summary>
    private readonly TextBlock _error = new() { Name = "ActionError", TextWrapping = global::Avalonia.Media.TextWrapping.Wrap };
    /// <summary>Displays the bounded activity history.</summary>
    private readonly TextBox _log = new() { Name = "ActivityLog", IsReadOnly = true, AcceptsReturn = true, MinHeight = 220 };
    /// <summary>Contains the outgoing payload.</summary>
    private readonly TextBox _text = new() { Name = "SendText", PlaceholderText = ExampleResources.SendPlaceholder };
    /// <summary>Contains the optional reply terminator.</summary>
    private readonly TextBox _eop = new() { Name = "ReplyEop", PlaceholderText = ExampleResources.EopPlaceholder };
    /// <summary>Contains the minimum reply byte count.</summary>
    private readonly TextBox _count = new() { Name = "ReplyCount", Text = "0" };
    /// <summary>Contains the reply timeout in milliseconds.</summary>
    private readonly TextBox _timeout = new() { Name = "ReplyTimeout", Text = "5000" };
    /// <summary>Selects hexadecimal data encoding.</summary>
    private readonly ToggleSwitch _hex = new() { Name = "HexMode", Content = ExampleResources.Hexadecimal, OnContent = null, OffContent = null };
    /// <summary>Selects whether received bytes are sent back unchanged.</summary>
    private readonly ToggleSwitch _echo = new() { Name = "Echo", Content = ExampleResources.Echo, OnContent = null, OffContent = null };
    /// <summary>Displays received frames independently of diagnostic activity.</summary>
    private readonly TextBox _received = new() { Name = "ReceivedData", IsReadOnly = true, AcceptsReturn = true, MinHeight = 220 };
    /// <summary>Contains the connection and payload controls.</summary>
    private readonly StackPanel _dataView = new() { Name = "DataView", Spacing = 12 };
    /// <summary>Contains receive framing controls.</summary>
    private readonly StackPanel _communicationView = new() { Name = "CommunicationView", Spacing = 12, IsVisible = false };
    /// <summary>Contains the activity history displayed by its own navigation item.</summary>
    private readonly StackPanel _activityView = new() { Name = "ActivityView", Spacing = 12, IsVisible = false };
    /// <summary>Displays the desktop's persistent connection, view and settings menus.</summary>
    private readonly Menu _applicationMenu = new() { Name = "ApplicationMenu" };
    /// <summary>Mirrors whether the connection can currently be opened.</summary>
    private readonly MenuItem _openMenuItem = new() { Name = "OpenMenu" };
    /// <summary>Mirrors whether the connection can currently be closed.</summary>
    private readonly MenuItem _closeMenuItem = new() { Name = "CloseMenu" };
    /// <summary>Allows media configuration while the connection is closed and idle.</summary>
    private readonly MenuItem _settingsMenuItem = new() { Name = "Settings" };
    /// <summary>Creates the native serial example and closes its window after asynchronous cleanup, including when cleanup fails.</summary>
    /// <param name="serialFactory">An optional platform serial stream factory.</param>
    public MediaExampleWindow(Func<SerialPortSettings, CancellationToken, ValueTask<Stream>>? serialFactory = null)
    {
        Title = ExampleResources.Title;
        _session = new(serialFactory);
        ExampleLayout.ConfigureWindow(this);
        var body = ExampleLayout.CreateBody(_state);
        global::Avalonia.Automation.AutomationProperties.SetName(_connectionProgress, ExampleSettingsResources.ConnectionBusy);
        body.Children.Add(_connectionProgress);
        CreateDesktopMenu();
        var connection = ExampleLayout.Toolbar(_dataView);
        AddButton(connection, "Open", ExampleResources.Open, OpenConnectionAsync, true);
        AddButton(connection, "Close", ExampleResources.Close, () => _session.CloseAsync());
        _text.FontFamily = new global::Avalonia.Media.FontFamily("Cascadia Mono, DejaVu Sans Mono, Consolas, Menlo, monospace");
        var payload = ExampleLayout.Field(ExampleSettingsResources.PayloadLabel, _text);
        payload.MaxWidth = double.PositiveInfinity;
        payload.HorizontalAlignment = HorizontalAlignment.Stretch;
        var exchange = new StackPanel { Spacing = 10 };
        exchange.Children.Add(payload);
        var actions = ExampleLayout.Row(_hex, _echo);
        _echo.IsCheckedChanged += (_, _) => _session.Echo = _echo.IsChecked == true;
        exchange.Children.Add(actions);
        _dataView.Children.Add(ExampleLayout.Group(ExampleResources.Data, exchange));
        ExampleLayout.ConfigureLog(_received);
        _dataView.Children.Add(ExampleLayout.Group(ExampleResources.ReceivedData, _received));
        _count.Width = 140;
        _eop.Width = 180;
        _timeout.Width = 140;
        var replySettings = new StackPanel { Spacing = 10 };
        replySettings.Children.Add(new TextBlock { Text = ExampleResources.CloseBeforeReplySettings, TextWrapping = global::Avalonia.Media.TextWrapping.Wrap });
        replySettings.Children.Add(ExampleLayout.Row(
            ExampleLayout.Field(ExampleSettingsResources.CountLabel, _count),
            ExampleLayout.Field(ExampleSettingsResources.TerminatorLabel, _eop),
            ExampleLayout.Field(ExampleSettingsResources.TimeoutLabel, _timeout)));
        _communicationView.Children.Add(ExampleLayout.Group(ExampleSettingsResources.ReplyGroup, replySettings));
        AddButton(actions, "Send", ExampleResources.Send, async () =>
        {
            await _session.SendAsync(_text.Text ?? "", _hex.IsChecked == true, cancellationToken: _lifetime.Token);
        }, true);
        AddButton(actions, "Clear", ExampleResources.Clear, () => { _session.ClearReceived(); return Task.CompletedTask; });
        body.Children.Add(_dataView);
        body.Children.Add(_communicationView);
        body.Children.Add(_activityView);
        body.Children.Add(_error);
        ExampleLayout.ConfigureLog(_log);
        var activityActions = ExampleLayout.Row();
        AddButton(activityActions, "ClearActivity", ExampleResources.Clear, () => { _session.ClearActivity(); return Task.CompletedTask; });
        _activityView.Children.Add(activityActions);
        _activityView.Children.Add(ExampleLayout.Group(ExampleSettingsResources.ActivityGroup, _log));
        var frame = new Grid { RowDefinitions = new RowDefinitions("Auto,*") };
        frame.Children.Add(_applicationMenu);
        var content = ExampleLayout.WithStatus(body, _mediaStatus);
        Grid.SetRow(content, 1);
        frame.Children.Add(content);
        Content = frame;
        _timer = new DispatcherTimer(TimeSpan.FromMilliseconds(200), DispatcherPriority.Background, (_, _) => Refresh());
        Volatile.Write(ref _connectionPhase, (int)(_session.Media.IsOpen ? MediaState.Open : MediaState.Closed));
        _session.Media.OnMediaStateChange += MediaStateChanged;
        _timer.Start(); Refresh();
        _preferencesLoading = RestorePreferencesAsync();
        Closing += async (_, args) =>
        {
            if (_disposal is null)
            {
                args.Cancel = true;
                await RunAsync(async () =>
                {
                    try
                    {
                        await DisposeAsync();
                    }
                    finally
                    {
                        // Finish the current native closing event before issuing the final close request.
                        Dispatcher.UIThread.Post(() => Close());
                    }
                });
            }
            else if (!_disposal.IsCompleted) { args.Cancel = true; }
        };
    }
    /// <summary>Gets whether preferences are restored and no settings editor or shutdown owns input.</summary>
    private bool CanNavigate => _preferencesReady && !_settingsShowing && Volatile.Read(ref _shuttingDown) == 0;
    /// <summary>Switches existing sections only while navigation owns input, preserving the media session and controls.</summary>
    /// <param name="section">The existing data, communication settings or activity section to display.</param>
    private void ShowSection(Control section)
    {
        if (!CanNavigate)
        {
            return;
        }
        _dataView.IsVisible = ReferenceEquals(section, _dataView);
        _communicationView.IsVisible = ReferenceEquals(section, _communicationView);
        _activityView.IsVisible = ReferenceEquals(section, _activityView);
    }
    /// <summary>Builds the native desktop menu without replacing the session or its content sections.</summary>
    private void CreateDesktopMenu()
    {
        _applicationMenu.ItemsSource = new MenuItem[]
        {
            new()
            {
                Name = "ConnectionMenu", Header = ExampleResources.ConnectionMenu,
                ItemsSource = new[]
                {
                    MenuAction(_openMenuItem, ExampleResources.Open, OpenConnectionAsync),
                    MenuAction(_closeMenuItem, ExampleResources.Close, _session.CloseAsync)
                }
            },
            new()
            {
                Name = "ViewMenu", Header = ExampleResources.ViewMenu,
                ItemsSource = new[]
                {
                    MenuAction(new MenuItem { Name = "DataNavigation" }, ExampleResources.Data, () => { ShowSection(_dataView); return Task.CompletedTask; }),
                    MenuAction(new MenuItem { Name = "ActivityNavigation" }, ExampleSettingsResources.ActivityGroup, () => { ShowSection(_activityView); return Task.CompletedTask; })
                }
            },
            new()
            {
                Name = "SettingsMenu", Header = ExampleResources.Settings,
                ItemsSource = new[]
                {
                    MenuAction(_settingsMenuItem, ExampleResources.MediaSettings, EditMediaSettingsAsync),
                    MenuAction(new MenuItem { Name = "CommunicationNavigation" }, ExampleResources.CommunicationSettings, () => { ShowSection(_communicationView); return Task.CompletedTask; })
                }
            }
        };
    }
    /// <summary>Connects an existing menu item to the same asynchronous error handling as the toolbar.</summary>
    /// <param name="item">The menu item whose enabled state is retained.</param>
    /// <param name="caption">The localized action caption.</param>
    /// <param name="action">The operation to run when selected.</param>
    /// <returns>The configured item for inclusion in its parent menu.</returns>
    private MenuItem MenuAction(MenuItem item, string caption, Func<Task> action)
    {
        item.Header = caption;
        item.Click += async (_, _) =>
        {
            if (CanNavigate && item.IsEnabled && _applicationMenu.IsEnabled)
            {
                await RunAsync(action);
            }
        };
        return item;
    }
    /// <summary>Applies receive framing and opens the selected serial connection.</summary>
    /// <returns>A task completing when the connection opens or fails.</returns>
    /// <exception cref="FormatException">A framing count, timeout or hexadecimal terminator is malformed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The receive count or timeout is outside its supported range.</exception>
    /// <exception cref="OperationCanceledException">Shutdown cancels opening.</exception>
    private Task OpenConnectionAsync()
    {
        _session.ConfigureReceive(_hex.IsChecked == true, _eop.Text,
            int.Parse(_count.Text ?? "0", CultureInfo.CurrentCulture),
            int.Parse(_timeout.Text ?? "5000", CultureInfo.CurrentCulture));
        return _session.OpenAsync(_lifetime.Token);
    }
    /// <summary>Shows the closed connection's settings editor while reserving menu input until it closes.</summary>
    /// <returns>A task completing after accepted settings, cancellation, or an ignored unavailable action.</returns>
    /// <exception cref="OperationCanceledException">Application shutdown cancels the editor.</exception>
    private async Task EditMediaSettingsAsync()
    {
        if (!CanNavigate || _session.IsBusy || (MediaState)Volatile.Read(ref _connectionPhase) != MediaState.Closed)
        {
            return;
        }
        _settingsShowing = true;
        Refresh();
        try
        {
            await new AvaloniaMediaSettingsEditor(this).ShowAsync(_session.Media, cancellationToken: _lifetime.Token);
        }
        finally
        {
            _settingsShowing = false;
            Refresh();
        }
    }
    /// <summary>Records transport lifecycle events and refreshes controls on the UI thread.</summary>
    /// <param name="sender">The transport publishing the event.</param>
    /// <param name="args">The new media state.</param>
    private void MediaStateChanged(object? sender, MediaStateEventArgs args)
    {
        if (Volatile.Read(ref _shuttingDown) != 0 || args.State == MediaState.Changed)
        {
            return;
        }
        Volatile.Write(ref _connectionPhase, (int)args.State);
        if (Dispatcher.UIThread.CheckAccess())
        {
            RefreshMediaState();
        }
        else
        {
            Dispatcher.UIThread.Post(RefreshMediaState);
        }
    }
    /// <summary>Ignores queued transport notifications after shutdown begins.</summary>
    private void RefreshMediaState()
    {
        if (Volatile.Read(ref _shuttingDown) == 0)
        {
            Refresh();
        }
    }
    /// <summary>Adds an asynchronous command to the native layout.</summary>
    /// <param name="parent">The layout receiving the command.</param>
    /// <param name="name">The stable automation identifier.</param>
    /// <param name="caption">The localized button caption.</param>
    /// <param name="action">The asynchronous command to execute.</param>
    /// <param name="unsafeAction">Whether connection state restricts this command.</param>
    private void AddButton(Panel parent, string name, string caption, Func<Task> action, bool unsafeAction = false)
    {
        var button = new Button { Name = name, Content = caption, HorizontalAlignment = HorizontalAlignment.Left };
        ExampleLayout.ConfigureAction(button, name is "Open" or "Send");
        if (name == "Close") { _closeButton = button; }
        button.Click += async (_, _) => await RunAsync(action);
        parent.Children.Add(button);
        if (unsafeAction) { _unsafeButtons.Add(button); }
    }
    /// <summary>Executes a command and reports any failure in the native view.</summary>
    /// <param name="action">The asynchronous action to execute.</param>
    /// <returns>A task completing after the UI state is refreshed.</returns>
    private async Task RunAsync(Func<Task> action)
    {
        try { _error.Text = ""; Task operation = action(); Refresh(); await operation; }
        catch (OperationCanceledException) { _error.Text = ExampleResources.Canceled; }
        catch (Exception ex) { _error.Text = ex.Message; }
        finally { Refresh(); }
    }
    /// <summary>Refreshes connection status, selected serial port availability, commands and activity history.</summary>
    private void Refresh()
    {
        if (Volatile.Read(ref _shuttingDown) != 0)
        {
            _connectionProgress.IsVisible = false;
            _applicationMenu.IsEnabled = false;
            _openMenuItem.IsEnabled = false;
            _closeMenuItem.IsEnabled = false;
            _settingsMenuItem.IsEnabled = false;
            if (_closeButton is not null) { _closeButton.IsEnabled = false; }
            foreach (var button in _unsafeButtons) { button.IsEnabled = false; }
            return;
        }
        _applicationMenu.IsEnabled = CanNavigate;
        MediaState phase = (MediaState)Volatile.Read(ref _connectionPhase);
        _connectionProgress.IsVisible = phase is MediaState.Opening or MediaState.Closing;
        bool closed = phase == MediaState.Closed;
        bool busy = _session.IsBusy;
        bool open = phase == MediaState.Open;
        _count.IsEnabled = closed && !busy;
        _eop.IsEnabled = closed && !busy;
        _timeout.IsEnabled = closed && !busy;
        if (closed && !busy && !_lifetime.IsCancellationRequested)
        {
            _ = _portAvailability.RefreshAsync(_session.Media.PortName, _lifetime.Token);
        }
        ExampleLayout.UpdateStatus(_state, open, busy);
        ExampleLayout.UpdateError(_error);
        if (_closeButton is not null) { _closeButton.IsEnabled = open; }
        foreach (var button in _unsafeButtons)
        {
            button.IsEnabled = !busy && (button.Name is "Settings" or "RightSettings" or "Open" ? closed : open)
                && (button.Name != "Open" || (!_lifetime.IsCancellationRequested && _session.CanAttemptOpen(_portAvailability.IsAvailable)));
        }
        _openMenuItem.IsEnabled = _preferencesReady && _unsafeButtons.Any(button => button.Name == "Open" && button.IsEnabled);
        _closeMenuItem.IsEnabled = _preferencesReady && _closeButton?.IsEnabled == true;
        _settingsMenuItem.IsEnabled = _preferencesReady && closed && !busy;
        string snapshot = string.Join(Environment.NewLine, _session.Log);
        if (_log.Text != snapshot) { _log.Text = snapshot; }
        string receivedSnapshot = _session.Received(_hex.IsChecked == true);
        if (_received.Text != receivedSnapshot) { _received.Text = receivedSnapshot; }
        _ = SavePreferencesAsync();
        _mediaStatus.Text = string.Format(System.Globalization.CultureInfo.CurrentCulture, StatusResources.StatusSerial,
            _session.Media.IsOpen ? StatusResources.StatusOpen : StatusResources.StatusClosed,
            string.IsNullOrWhiteSpace(_session.Media.PortName) ? StatusResources.StatusNotConfigured : _session.Media.PortName);

    }
    /// <inheritdoc/>
    public ValueTask DisposeAsync() => new(_disposal ??= DisposeCoreAsync());
    /// <summary>Stops UI refresh and releases the owned serial connection.</summary>
    /// <returns>A task completing after transport and cancellation cleanup.</returns>
    private async Task DisposeCoreAsync()
    {
        Volatile.Write(ref _shuttingDown, 1);
        _session.Media.OnMediaStateChange -= MediaStateChanged;
        if (Dispatcher.UIThread.CheckAccess()) { Refresh(); }
        else { Dispatcher.UIThread.Post(Refresh); }
        _timer.Stop(); _lifetime.Cancel();
        await _preferencesLoading;
        await SavePreferencesAsync();
        await _session.DisposeAsync();
        _lifetime.Dispose();
    }
    /// <summary>Captures media configuration and outgoing controls without opening a connection.</summary>
    /// <returns>A detached JSON object containing this view's persistent preferences.</returns>
    private JsonObject CapturePreferences()
    {
        var values = new JsonObject
        {
            ["Media"] = JsonNode.Parse(_session.Media.JsonSettings ?? "{}"),
            ["SendText"] = _text.Text ?? "",
            ["Hex"] = _hex.IsChecked == true,
            ["Echo"] = _echo.IsChecked == true,
            ["ReplyEop"] = _eop.Text ?? "",
            ["ReplyCount"] = _count.Text ?? "",
            ["ReplyTimeout"] = _timeout.Text ?? ""
        };
        return values;
    }

    /// <summary>Applies stored media configuration and interface values while the transport remains closed.</summary>
    /// <param name="values">The saved preferences; missing entries retain their current defaults.</param>
    private void ApplyPreferences(JsonObject values)
    {
        if (values["Media"] is JsonObject media) { _session.Media.JsonSettings = media.ToJsonString(); }
        _text.Text = values["SendText"]?.GetValue<string>() ?? _text.Text;
        _hex.IsChecked = values["Hex"]?.GetValue<bool>() ?? (_hex.IsChecked == true);
        _echo.IsChecked = values["Echo"]?.GetValue<bool>() ?? false;
        _eop.Text = values["ReplyEop"]?.GetValue<string>() ?? _eop.Text;
        _count.Text = values["ReplyCount"]?.GetValue<string>() ?? _count.Text;
        _timeout.Text = values["ReplyTimeout"]?.GetValue<string>() ?? _timeout.Text;
    }

    /// <summary>Restores preferences asynchronously before enabling interaction and keeps defaults if loading fails.</summary>
    /// <returns>A task completing after the saved values or original defaults have been applied.</returns>
    private async Task RestorePreferencesAsync()
    {
        IsEnabled = false;
        JsonObject defaults = CapturePreferences();
        try
        {
            JsonObject? values = await _preferences.LoadAsync();
            if (System.Threading.Volatile.Read(ref _shuttingDown) != 0) { return; }
            if (values is not null) { ApplyPreferences(values); }
        }
        catch (Exception ex)
        {
            if (System.Threading.Volatile.Read(ref _shuttingDown) != 0) { return; }
            ApplyPreferences(defaults);
            _error.Text = ex.Message;
        }
        _preferencesSnapshot = CapturePreferences().ToJsonString();
        _preferencesReady = true;
        IsEnabled = true;
        Refresh();
    }

    /// <summary>Persists changed controls and reports disk failures without interrupting transport cleanup.</summary>
    /// <returns>A task completing after the current snapshot has been saved or skipped.</returns>
    private async Task SavePreferencesAsync()
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            await Dispatcher.UIThread.InvokeAsync(SavePreferencesAsync);
            return;
        }
        if (!_preferencesReady) { return; }
        string? snapshot = null;
        try
        {
            JsonObject values = CapturePreferences();
            snapshot = values.ToJsonString();
            if (snapshot == _preferencesSnapshot) { await _preferencesSaving; return; }
            _preferencesSnapshot = snapshot;
            _preferencesSaving = _preferences.SaveAsync(values);
            await _preferencesSaving;
        }
        catch (Exception ex)
        {
            if (_preferencesSnapshot == snapshot) { _preferencesSnapshot = null; }
            _error.Text = ex.Message;
        }
    }

}
