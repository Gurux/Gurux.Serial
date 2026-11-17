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
    private readonly TextBlock mediaStatus = new() { Name = "MediaStatus" };
    /// <summary>Shows indeterminate connection progress only while the transport is opening or closing.</summary>
    private readonly ProgressBar connectionProgress = new() { Name = "ConnectionProgress", IsIndeterminate = true, IsVisible = false, Height = 4 };

    /// <summary>Persists this application's media and outgoing data preferences.</summary>
    private readonly ExampleSettingsStore preferences = new();
    /// <summary>Completes when startup preferences have been applied on the UI thread.</summary>
    private Task preferencesLoading = Task.CompletedTask;
    /// <summary>Tracks the latest queued write so shutdown waits for the final disk update.</summary>
    private Task preferencesSaving = Task.CompletedTask;
    /// <summary>Prevents persistence until startup restoration has completed.</summary>
    private bool preferencesReady;
    /// <summary>Reserves navigation and media commands while the modal settings editor owns input.</summary>
    private bool settingsShowing;
    /// <summary>Tracks the most recently requested snapshot and avoids saving unchanged controls.</summary>
    private string? preferencesSnapshot;

    /// <summary>Stores the latest transport connection phase independently of command activity.</summary>
    private int connectionPhase;
    /// <summary>Prevents callbacks from refreshing disposed resources.</summary>
    private int shuttingDown;
    /// <summary>Cancels UI operations during shutdown.</summary>
    private readonly CancellationTokenSource lifetime = new();
    /// <summary>Tracks discovery of the selected serial port without blocking the UI thread.</summary>
    private readonly global::Gurux.Serial.SerialPortAvailability portAvailability = new();
    /// <summary>Owns the serial connection lifecycle.</summary>
    private readonly ExampleSession session;
    /// <summary>Allows closing an open connection, including canceling an active reply.</summary>
    private Button? closeButton;
    /// <summary>Tracks commands whose availability depends on connection state.</summary>
    private readonly List<Button> unsafeButtons = [];
    /// <summary>Tracks the single shutdown operation.</summary>
    private Task? disposal;
    /// <summary>Refreshes connection state on the UI thread.</summary>
    private readonly DispatcherTimer timer;
    /// <summary>Displays the current connection state.</summary>
    private readonly TextBlock state = new() { Name = "ConnectionState" };
    /// <summary>Displays action errors and replies.</summary>
    private readonly TextBlock error = new() { Name = "ActionError", TextWrapping = global::Avalonia.Media.TextWrapping.Wrap };
    /// <summary>Displays the bounded activity history.</summary>
    private readonly TextBox log = new() { Name = "ActivityLog", IsReadOnly = true, AcceptsReturn = true, MinHeight = 220 };
    /// <summary>Contains the outgoing payload.</summary>
    private readonly TextBox text = new() { Name = "SendText", PlaceholderText = ExampleResources.SendPlaceholder };
    /// <summary>Contains the optional reply terminator.</summary>
    private readonly TextBox eop = new() { Name = "ReplyEop", PlaceholderText = ExampleResources.EopPlaceholder };
    /// <summary>Contains the minimum reply byte count.</summary>
    private readonly TextBox count = new() { Name = "ReplyCount", Text = "0" };
    /// <summary>Contains the reply timeout in milliseconds.</summary>
    private readonly TextBox timeout = new() { Name = "ReplyTimeout", Text = "5000" };
    /// <summary>Selects hexadecimal data encoding.</summary>
    private readonly ToggleSwitch hex = new() { Name = "HexMode", Content = ExampleResources.Hexadecimal, OnContent = null, OffContent = null };
    /// <summary>Selects whether received bytes are sent back unchanged.</summary>
    private readonly ToggleSwitch echo = new() { Name = "Echo", Content = ExampleResources.Echo, OnContent = null, OffContent = null };
    /// <summary>Displays received frames independently of diagnostic activity.</summary>
    private readonly TextBox received = new() { Name = "ReceivedData", IsReadOnly = true, AcceptsReturn = true, MinHeight = 220 };
    /// <summary>Contains the connection and payload controls.</summary>
    private readonly StackPanel dataView = new() { Name = "DataView", Spacing = 12 };
    /// <summary>Contains receive framing controls.</summary>
    private readonly StackPanel communicationView = new() { Name = "CommunicationView", Spacing = 12, IsVisible = false };
    /// <summary>Contains the activity history displayed by its own navigation item.</summary>
    private readonly StackPanel activityView = new() { Name = "ActivityView", Spacing = 12, IsVisible = false };
    /// <summary>Displays the desktop's persistent connection, view and settings menus.</summary>
    private readonly Menu applicationMenu = new() { Name = "ApplicationMenu" };
    /// <summary>Mirrors whether the connection can currently be opened.</summary>
    private readonly MenuItem openMenuItem = new() { Name = "OpenMenu" };
    /// <summary>Mirrors whether the connection can currently be closed.</summary>
    private readonly MenuItem closeMenuItem = new() { Name = "CloseMenu" };
    /// <summary>Allows media configuration while the connection is closed and idle.</summary>
    private readonly MenuItem settingsMenuItem = new() { Name = "Settings" };
    /// <summary>Creates the native serial example and closes its window after asynchronous cleanup, including when cleanup fails.</summary>
    /// <param name="serialFactory">An optional platform serial stream factory.</param>
    public MediaExampleWindow(Func<SerialPortSettings, CancellationToken, ValueTask<Stream>>? serialFactory = null)
    {
        Title = ExampleResources.Title;
        session = new(serialFactory);
        ExampleLayout.ConfigureWindow(this);
        var body = ExampleLayout.CreateBody(state);
        global::Avalonia.Automation.AutomationProperties.SetName(connectionProgress, ExampleSettingsResources.ConnectionBusy);
        body.Children.Add(connectionProgress);
        CreateDesktopMenu();
        var connection = ExampleLayout.Toolbar(dataView);
        AddButton(connection, "Open", ExampleResources.Open, OpenConnectionAsync, true);
        AddButton(connection, "Close", ExampleResources.Close, () => session.CloseAsync());
        text.FontFamily = new global::Avalonia.Media.FontFamily("Cascadia Mono, DejaVu Sans Mono, Consolas, Menlo, monospace");
        var payload = ExampleLayout.Field(ExampleSettingsResources.PayloadLabel, text);
        payload.MaxWidth = double.PositiveInfinity;
        payload.HorizontalAlignment = HorizontalAlignment.Stretch;
        var exchange = new StackPanel { Spacing = 10 };
        exchange.Children.Add(payload);
        var actions = ExampleLayout.Row(hex, echo);
        echo.IsCheckedChanged += (_, _) => session.Echo = echo.IsChecked == true;
        exchange.Children.Add(actions);
        dataView.Children.Add(ExampleLayout.Group(ExampleResources.Data, exchange));
        ExampleLayout.ConfigureLog(received);
        dataView.Children.Add(ExampleLayout.Group(ExampleResources.ReceivedData, received));
        count.Width = 140;
        eop.Width = 180;
        timeout.Width = 140;
        var replySettings = new StackPanel { Spacing = 10 };
        replySettings.Children.Add(new TextBlock { Text = ExampleResources.CloseBeforeReplySettings, TextWrapping = global::Avalonia.Media.TextWrapping.Wrap });
        replySettings.Children.Add(ExampleLayout.Row(
            ExampleLayout.Field(ExampleSettingsResources.CountLabel, count),
            ExampleLayout.Field(ExampleSettingsResources.TerminatorLabel, eop),
            ExampleLayout.Field(ExampleSettingsResources.TimeoutLabel, timeout)));
        communicationView.Children.Add(ExampleLayout.Group(ExampleSettingsResources.ReplyGroup, replySettings));
        AddButton(actions, "Send", ExampleResources.Send, async () =>
        {
            await session.SendAsync(text.Text ?? "", hex.IsChecked == true, cancellationToken: lifetime.Token);
        }, true);
        AddButton(actions, "Clear", ExampleResources.Clear, () => { session.ClearReceived(); return Task.CompletedTask; });
        body.Children.Add(dataView);
        body.Children.Add(communicationView);
        body.Children.Add(activityView);
        body.Children.Add(error);
        ExampleLayout.ConfigureLog(log);
        var activityActions = ExampleLayout.Row();
        AddButton(activityActions, "ClearActivity", ExampleResources.Clear, () => { session.ClearActivity(); return Task.CompletedTask; });
        activityView.Children.Add(activityActions);
        activityView.Children.Add(ExampleLayout.Group(ExampleSettingsResources.ActivityGroup, log));
        var frame = new Grid { RowDefinitions = new RowDefinitions("Auto,*") };
        frame.Children.Add(applicationMenu);
        var content = ExampleLayout.WithStatus(body, mediaStatus);
        Grid.SetRow(content, 1);
        frame.Children.Add(content);
        Content = frame;
        timer = new DispatcherTimer(TimeSpan.FromMilliseconds(200), DispatcherPriority.Background, (_, _) => Refresh());
        Volatile.Write(ref connectionPhase, (int)(session.Media.IsOpen ? MediaState.Open : MediaState.Closed));
        session.Media.OnMediaStateChange += MediaStateChanged;
        timer.Start(); Refresh();
        preferencesLoading = RestorePreferencesAsync();
        Closing += async (_, args) =>
        {
            if (disposal is null)
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
            else if (!disposal.IsCompleted) { args.Cancel = true; }
        };
    }
    /// <summary>Gets whether preferences are restored and no settings editor or shutdown owns input.</summary>
    private bool CanNavigate => preferencesReady && !settingsShowing && Volatile.Read(ref shuttingDown) == 0;
    /// <summary>Switches existing sections only while navigation owns input, preserving the media session and controls.</summary>
    /// <param name="section">The existing data, communication settings or activity section to display.</param>
    private void ShowSection(Control section)
    {
        if (!CanNavigate)
        {
            return;
        }
        dataView.IsVisible = ReferenceEquals(section, dataView);
        communicationView.IsVisible = ReferenceEquals(section, communicationView);
        activityView.IsVisible = ReferenceEquals(section, activityView);
    }
    /// <summary>Builds the native desktop menu without replacing the session or its content sections.</summary>
    private void CreateDesktopMenu()
    {
        applicationMenu.ItemsSource = new MenuItem[]
        {
            new()
            {
                Name = "ConnectionMenu", Header = ExampleResources.ConnectionMenu,
                ItemsSource = new[]
                {
                    MenuAction(openMenuItem, ExampleResources.Open, OpenConnectionAsync),
                    MenuAction(closeMenuItem, ExampleResources.Close, session.CloseAsync)
                }
            },
            new()
            {
                Name = "ViewMenu", Header = ExampleResources.ViewMenu,
                ItemsSource = new[]
                {
                    MenuAction(new MenuItem { Name = "DataNavigation" }, ExampleResources.Data, () => { ShowSection(dataView); return Task.CompletedTask; }),
                    MenuAction(new MenuItem { Name = "ActivityNavigation" }, ExampleSettingsResources.ActivityGroup, () => { ShowSection(activityView); return Task.CompletedTask; })
                }
            },
            new()
            {
                Name = "SettingsMenu", Header = ExampleResources.Settings,
                ItemsSource = new[]
                {
                    MenuAction(settingsMenuItem, ExampleResources.MediaSettings, EditMediaSettingsAsync),
                    MenuAction(new MenuItem { Name = "CommunicationNavigation" }, ExampleResources.CommunicationSettings, () => { ShowSection(communicationView); return Task.CompletedTask; })
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
            if (CanNavigate && item.IsEnabled && applicationMenu.IsEnabled)
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
        session.ConfigureReceive(hex.IsChecked == true, eop.Text,
            int.Parse(count.Text ?? "0", CultureInfo.CurrentCulture),
            int.Parse(timeout.Text ?? "5000", CultureInfo.CurrentCulture));
        return session.OpenAsync(lifetime.Token);
    }
    /// <summary>Shows the closed connection's settings editor while reserving menu input until it closes.</summary>
    /// <returns>A task completing after accepted settings, cancellation, or an ignored unavailable action.</returns>
    /// <exception cref="OperationCanceledException">Application shutdown cancels the editor.</exception>
    private async Task EditMediaSettingsAsync()
    {
        if (!CanNavigate || session.IsBusy || (MediaState)Volatile.Read(ref connectionPhase) != MediaState.Closed)
        {
            return;
        }
        settingsShowing = true;
        Refresh();
        try
        {
            await new AvaloniaMediaSettingsEditor(this).ShowAsync(session.Media, cancellationToken: lifetime.Token);
        }
        finally
        {
            settingsShowing = false;
            Refresh();
        }
    }
    /// <summary>Records transport lifecycle events and refreshes controls on the UI thread.</summary>
    /// <param name="sender">The transport publishing the event.</param>
    /// <param name="args">The new media state.</param>
    private void MediaStateChanged(object? sender, MediaStateEventArgs args)
    {
        if (Volatile.Read(ref shuttingDown) != 0 || args.State == MediaState.Changed)
        {
            return;
        }
        Volatile.Write(ref connectionPhase, (int)args.State);
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
        if (Volatile.Read(ref shuttingDown) == 0)
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
        if (name == "Close") { closeButton = button; }
        button.Click += async (_, _) => await RunAsync(action);
        parent.Children.Add(button);
        if (unsafeAction) { unsafeButtons.Add(button); }
    }
    /// <summary>Executes a command and reports any failure in the native view.</summary>
    /// <param name="action">The asynchronous action to execute.</param>
    /// <returns>A task completing after the UI state is refreshed.</returns>
    private async Task RunAsync(Func<Task> action)
    {
        try { error.Text = ""; Task operation = action(); Refresh(); await operation; }
        catch (OperationCanceledException) { error.Text = ExampleResources.Canceled; }
        catch (Exception ex) { error.Text = ex.Message; }
        finally { Refresh(); }
    }
    /// <summary>Refreshes connection status, selected serial port availability, commands and activity history.</summary>
    private void Refresh()
    {
        if (Volatile.Read(ref shuttingDown) != 0)
        {
            connectionProgress.IsVisible = false;
            applicationMenu.IsEnabled = false;
            openMenuItem.IsEnabled = false;
            closeMenuItem.IsEnabled = false;
            settingsMenuItem.IsEnabled = false;
            if (closeButton is not null) { closeButton.IsEnabled = false; }
            foreach (var button in unsafeButtons) { button.IsEnabled = false; }
            return;
        }
        applicationMenu.IsEnabled = CanNavigate;
        MediaState phase = (MediaState)Volatile.Read(ref connectionPhase);
        connectionProgress.IsVisible = phase is MediaState.Opening or MediaState.Closing;
        bool closed = phase == MediaState.Closed;
        bool busy = session.IsBusy;
        bool open = phase == MediaState.Open;
        count.IsEnabled = closed && !busy;
        eop.IsEnabled = closed && !busy;
        timeout.IsEnabled = closed && !busy;
        if (closed && !busy && !lifetime.IsCancellationRequested)
        {
            _ = portAvailability.RefreshAsync(session.Media.PortName, lifetime.Token);
        }
        ExampleLayout.UpdateStatus(state, open, busy);
        ExampleLayout.UpdateError(error);
        if (closeButton is not null) { closeButton.IsEnabled = open; }
        foreach (var button in unsafeButtons)
        {
            button.IsEnabled = !busy && (button.Name is "Settings" or "RightSettings" or "Open" ? closed : open)
                && (button.Name != "Open" || (!lifetime.IsCancellationRequested && session.CanAttemptOpen(portAvailability.IsAvailable)));
        }
        openMenuItem.IsEnabled = preferencesReady && unsafeButtons.Any(button => button.Name == "Open" && button.IsEnabled);
        closeMenuItem.IsEnabled = preferencesReady && closeButton?.IsEnabled == true;
        settingsMenuItem.IsEnabled = preferencesReady && closed && !busy;
        string snapshot = string.Join(Environment.NewLine, session.Log);
        if (log.Text != snapshot) { log.Text = snapshot; }
        string receivedSnapshot = session.Received(hex.IsChecked == true);
        if (received.Text != receivedSnapshot) { received.Text = receivedSnapshot; }
        _ = SavePreferencesAsync();
        mediaStatus.Text = string.Format(System.Globalization.CultureInfo.CurrentCulture, StatusResources.StatusSerial,
            session.Media.IsOpen ? StatusResources.StatusOpen : StatusResources.StatusClosed,
            string.IsNullOrWhiteSpace(session.Media.PortName) ? StatusResources.StatusNotConfigured : session.Media.PortName);

    }
    /// <inheritdoc/>
    public ValueTask DisposeAsync() => new(disposal ??= DisposeCoreAsync());
    /// <summary>Stops UI refresh and releases the owned serial connection.</summary>
    /// <returns>A task completing after transport and cancellation cleanup.</returns>
    private async Task DisposeCoreAsync()
    {
        Volatile.Write(ref shuttingDown, 1);
        session.Media.OnMediaStateChange -= MediaStateChanged;
        if (Dispatcher.UIThread.CheckAccess()) { Refresh(); }
        else { Dispatcher.UIThread.Post(Refresh); }
        timer.Stop(); lifetime.Cancel();
        await preferencesLoading;
        await SavePreferencesAsync();
        await session.DisposeAsync();
        lifetime.Dispose();
    }
    /// <summary>Captures media configuration and outgoing controls without opening a connection.</summary>
    /// <returns>A detached JSON object containing this view's persistent preferences.</returns>
    private JsonObject CapturePreferences()
    {
        var values = new JsonObject
        {
            ["Media"] = JsonNode.Parse(session.Media.JsonSettings ?? "{}"),
            ["SendText"] = text.Text ?? "",
            ["Hex"] = hex.IsChecked == true,
            ["Echo"] = echo.IsChecked == true,
            ["ReplyEop"] = eop.Text ?? "",
            ["ReplyCount"] = count.Text ?? "",
            ["ReplyTimeout"] = timeout.Text ?? ""
        };
        return values;
    }

    /// <summary>Applies stored media configuration and interface values while the transport remains closed.</summary>
    /// <param name="values">The saved preferences; missing entries retain their current defaults.</param>
    private void ApplyPreferences(JsonObject values)
    {
        if (values["Media"] is JsonObject media) { session.Media.JsonSettings = media.ToJsonString(); }
        text.Text = values["SendText"]?.GetValue<string>() ?? text.Text;
        hex.IsChecked = values["Hex"]?.GetValue<bool>() ?? (hex.IsChecked == true);
        echo.IsChecked = values["Echo"]?.GetValue<bool>() ?? false;
        eop.Text = values["ReplyEop"]?.GetValue<string>() ?? eop.Text;
        count.Text = values["ReplyCount"]?.GetValue<string>() ?? count.Text;
        timeout.Text = values["ReplyTimeout"]?.GetValue<string>() ?? timeout.Text;
    }

    /// <summary>Restores preferences asynchronously before enabling interaction and keeps defaults if loading fails.</summary>
    /// <returns>A task completing after the saved values or original defaults have been applied.</returns>
    private async Task RestorePreferencesAsync()
    {
        IsEnabled = false;
        JsonObject defaults = CapturePreferences();
        try
        {
            JsonObject? values = await preferences.LoadAsync();
            if (System.Threading.Volatile.Read(ref shuttingDown) != 0) { return; }
            if (values is not null) { ApplyPreferences(values); }
        }
        catch (Exception ex)
        {
            if (System.Threading.Volatile.Read(ref shuttingDown) != 0) { return; }
            ApplyPreferences(defaults);
            error.Text = ex.Message;
        }
        preferencesSnapshot = CapturePreferences().ToJsonString();
        preferencesReady = true;
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
        if (!preferencesReady) { return; }
        string? snapshot = null;
        try
        {
            JsonObject values = CapturePreferences();
            snapshot = values.ToJsonString();
            if (snapshot == preferencesSnapshot) { await preferencesSaving; return; }
            preferencesSnapshot = snapshot;
            preferencesSaving = preferences.SaveAsync(values);
            await preferencesSaving;
        }
        catch (Exception ex)
        {
            if (preferencesSnapshot == snapshot) { preferencesSnapshot = null; }
            error.Text = ex.Message;
        }
    }

}
