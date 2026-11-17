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

using Microsoft.Maui;
using Microsoft.Maui.Controls;
using System.Globalization;
using Gurux.Serial.Example.Properties;
using LayoutResources = Gurux.Serial.Example.Maui.Properties.ExampleSettingsResources;
using System.Text.Json.Nodes;

using StatusResources = Gurux.Serial.Example.Maui.Properties.ExampleSettingsResources;

namespace Gurux.Serial.Example.Maui;

/// <summary>Provides native controls for the serial send and receive example.</summary>
public sealed class MediaExamplePage : ContentPage, IAsyncDisposable
{
    /// <summary>Displays a repeating progress sweep while the native connection is opening or closing.</summary>
    private readonly ProgressBar connectionProgress = new() { AutomationId = "ConnectionProgress", IsVisible = false, HeightRequest = 4, HorizontalOptions = LayoutOptions.Fill };
    /// <summary>Displays connection state and selected endpoints in the fixed bottom status bar.</summary>
    private readonly Label mediaStatus = new() { AutomationId = "MediaStatus" };

    /// <summary>Persists this application's media and outgoing data preferences.</summary>
    private readonly ExampleSettingsStore preferences = new();
    /// <summary>Completes when startup preferences have been applied on the UI thread.</summary>
    private Task preferencesLoading = Task.CompletedTask;
    /// <summary>Tracks the latest queued write so shutdown waits for the final disk update.</summary>
    private Task preferencesSaving = Task.CompletedTask;
    /// <summary>Prevents persistence until startup restoration has completed.</summary>
    private bool preferencesReady;
    /// <summary>Tracks the most recently requested snapshot and avoids saving unchanged controls.</summary>
    private string? preferencesSnapshot;

    /// <summary>Cancels UI operations during shutdown.</summary>
    private readonly CancellationTokenSource lifetime = new();
    /// <summary>Tracks whether the selected serial port is currently available without blocking UI refresh.</summary>
    private readonly global::Gurux.Serial.SerialPortAvailability portAvailability = new();
    /// <summary>Owns the serial connection lifecycle.</summary>
    private readonly ExampleSession session;
    /// <summary>Tracks commands whose availability depends on connection state.</summary>
    private readonly List<Button> unsafeButtons = [];
    /// <summary>Allows Close to cancel pending sends or close an open connection.</summary>
    private Button? closeButton;
    /// <summary>Tracks the single shutdown operation.</summary>
    private Task? disposal;
    /// <summary>Refreshes connection state on the UI thread.</summary>
    private readonly Microsoft.Maui.Dispatching.IDispatcherTimer timer;
    /// <summary>Displays the current connection state.</summary>
    private readonly Label state = new();
    /// <summary>Displays action errors and replies.</summary>
    private readonly Label error = new();
    /// <summary>Displays the bounded activity history.</summary>
    private readonly Editor log = new() { IsReadOnly = true, HeightRequest = 240, AutomationId = "ActivityLog" };
    /// <summary>Contains the outgoing payload.</summary>
    private readonly Entry text = new() { AutomationId = "SendText", Placeholder = ExampleResources.SendPlaceholder };
    /// <summary>Contains the optional reply terminator.</summary>
    private readonly Entry eop = new() { AutomationId = "ReplyEop", Placeholder = ExampleResources.EopPlaceholder };
    /// <summary>Contains the minimum reply byte count.</summary>
    private readonly Entry count = new() { AutomationId = "ReplyCount", Text = "0" };
    /// <summary>Contains the reply timeout in milliseconds.</summary>
    private readonly Entry timeout = new() { AutomationId = "ReplyTimeout", Text = "5000" };
    /// <summary>Selects hexadecimal data encoding.</summary>
    private readonly Switch hex = new() { AutomationId = "HexMode" };
    /// <summary>Selects whether received bytes are sent back unchanged.</summary>
    private readonly Switch echo = new() { AutomationId = "Echo" };
    /// <summary>Displays received frames independently of diagnostic activity.</summary>
    private readonly Editor received = new() { AutomationId = "ReceivedData", IsReadOnly = true, HeightRequest = 240 };
    /// <summary>Contains the connection and payload controls.</summary>
    private readonly VerticalStackLayout dataView = new() { AutomationId = "DataView", Spacing = 12 };
    /// <summary>Contains receive framing controls.</summary>
    private readonly VerticalStackLayout communicationView = new() { AutomationId = "CommunicationView", Spacing = 12, IsVisible = false };
    /// <summary>Contains the activity history displayed by its own navigation item.</summary>
    private readonly VerticalStackLayout activityView = new() { AutomationId = "ActivityView", Spacing = 12, IsVisible = false };
    /// <summary>Runs the desktop Open action using the toolbar's current availability.</summary>
    private readonly Command openMenuCommand;
    /// <summary>Runs the desktop Close action using the toolbar's current availability.</summary>
    private readonly Command closeMenuCommand;
    /// <summary>Shows media settings only while editing is safe.</summary>
    private readonly Command settingsMenuCommand;
    /// <summary>Opens native mobile navigation after preferences have loaded.</summary>
    private readonly Command navigationCommand;
    /// <summary>Updates native view actions when dialogs or shutdown restrict navigation.</summary>
    private readonly List<Command> viewMenuCommands = [];
    /// <summary>Prevents native menu actions while media settings own the page content.</summary>
    private bool settingsShowing;
    /// <summary>Prevents a second mobile navigation sheet from opening over the first.</summary>
    private bool navigationShowing;
    /// <summary>Stores the latest connection phase independently of periodic UI refreshes.</summary>
    private int mediaPhase;
    /// <summary>Prevents callbacks and queued refreshes from touching a disposed view.</summary>
    private int shuttingDown;
    /// <summary>Marshals transport state notifications onto the native UI thread.</summary>
    private readonly Microsoft.Maui.Dispatching.IDispatcher uiDispatcher;

    /// <summary>Creates the native serial example and its connection controls.</summary>
    /// <param name="serialFactory">An optional platform serial stream factory.</param>
    public MediaExamplePage(Func<SerialPortSettings, CancellationToken, ValueTask<Stream>>? serialFactory = null)
    {
        Title = ExampleResources.Title;
        session = new(serialFactory);
        ExampleLayout.Apply(this);
        ExampleLayout.Activity(log);
        ExampleLayout.Activity(received);
        ExampleLayout.Notice(error);
        var body = ExampleLayout.Body();
        var toolbar = ExampleLayout.Row();
        var options = ExampleLayout.Row(ExampleLayout.Toggle(hex, ExampleResources.Hexadecimal), ExampleLayout.Toggle(echo, ExampleResources.Echo));
        var sendActions = OperatingSystem.IsWindows() ? options : ExampleLayout.Row();
        if (OperatingSystem.IsWindows())
        {
            text.FontFamily = "Consolas";
        }
        body.Children.Add(ExampleLayout.Header(state));
        SemanticProperties.SetDescription(connectionProgress, LayoutResources.ConnectionBusy);
        body.Children.Add(connectionProgress);
        if (OperatingSystem.IsIOS()) { dataView.Children.Add(new Label { Text = ExampleResources.Unsupported }); }
        AddButton(toolbar, "Open", ExampleResources.Open, OpenConnectionAsync, true);
        AddButton(toolbar, "Close", ExampleResources.Close, () => session.CloseAsync());
        AddButton(sendActions, "Send", ExampleResources.Send, async () =>
        {
            await session.SendAsync(text.Text ?? "", hex.IsToggled, cancellationToken: lifetime.Token);
        }, true);
        AddButton(sendActions, "Clear", ExampleResources.Clear, () => { session.ClearReceived(); return Task.CompletedTask; });
        echo.Toggled += (_, args) => session.Echo = args.Value;
        dataView.Children.Add(ExampleLayout.Group(LayoutResources.ConnectionGroup, toolbar));
        var data = ExampleLayout.Stack(ExampleLayout.Field(LayoutResources.PayloadLabel, text),
            options);
        if (!OperatingSystem.IsWindows())
        {
            data.Children.Add(sendActions);
        }
        var replies = ExampleLayout.Row(ExampleLayout.Field(LayoutResources.CountLabel, count, 164),
            ExampleLayout.Field(LayoutResources.TerminatorLabel, eop, 164), ExampleLayout.Field(LayoutResources.TimeoutLabel, timeout, 164));
        dataView.Children.Add(ExampleLayout.Group(ExampleResources.Data, data));
        dataView.Children.Add(ExampleLayout.Group(ExampleResources.ReceivedData, received));
        communicationView.Children.Add(ExampleLayout.Group(LayoutResources.ReplyGroup,
            ExampleLayout.Stack(new Label { Text = ExampleResources.CloseBeforeReplySettings }, replies)));
        body.Children.Add(dataView);
        body.Children.Add(communicationView);
        body.Children.Add(activityView);
        body.Children.Add(error);
        var activityActions = ExampleLayout.Row();
        AddButton(activityActions, "ClearActivity", ExampleResources.Clear, () => { session.ClearActivity(); return Task.CompletedTask; });
        activityView.Children.Add(activityActions);
        activityView.Children.Add(ExampleLayout.Group(LayoutResources.ActivityGroup, log));
        Content = ExampleLayout.WithStatus(body, mediaStatus);
        openMenuCommand = new Command(async () => await RunAsync(OpenConnectionAsync),
            () => CanNavigate
                && unsafeButtons.Any(button => button.AutomationId == "Open" && button.IsEnabled));
        closeMenuCommand = new Command(async () => await RunAsync(session.CloseAsync),
            () => CanNavigate && closeButton?.IsEnabled == true);
        settingsMenuCommand = new Command(async () => await RunAsync(EditMediaSettingsAsync), () => CanEditMediaSettings);
        navigationCommand = new Command(async () => await RunAsync(ShowMobileMenuAsync),
            () => CanNavigate);
        if (OperatingSystem.IsWindows())
        {
            CreateDesktopMenu();
        }
        else
        {
            Title = ExampleResources.Title;
            var navigationItem = new ToolbarItem
            {
                AutomationId = "NavigationMenu", Text = ExampleResources.Menu,
                Order = ToolbarItemOrder.Primary, Command = navigationCommand
            };
            if (OperatingSystem.IsAndroid())
            {
                var icon = new FontImageSource
                {
                    Glyph = "\u2630", FontFamily = "sans-serif", Size = 24
                };
                icon.SetAppThemeColor(FontImageSource.ColorProperty,
                    Microsoft.Maui.Graphics.Colors.Black, Microsoft.Maui.Graphics.Colors.White);
                navigationItem.IconImageSource = icon;
                navigationItem.Order = ToolbarItemOrder.Primary;
                navigationItem.Text = string.Empty;
                SemanticProperties.SetDescription(navigationItem, ExampleResources.Menu);
            }
            ToolbarItems.Add(navigationItem);
        }
        uiDispatcher = Dispatcher;
        mediaPhase = (int)(session.Media.IsOpen ? Gurux.Common.MediaState.Open : Gurux.Common.MediaState.Closed);
        session.Media.OnMediaStateChange += MediaStateChanged;
        timer = Dispatcher.CreateTimer(); timer.Interval = TimeSpan.FromMilliseconds(200);
        timer.Tick += (_, _) => Refresh(); timer.Start(); Refresh();
        preferencesLoading = RestorePreferencesAsync();

    }
    /// <summary>Switches sections while preserving the active connection and received data.</summary>
    /// <param name="section">The existing data, communication settings or activity section to display.</param>
    private void ShowSection(View section)
    {
        dataView.IsVisible = ReferenceEquals(section, dataView);
        communicationView.IsVisible = ReferenceEquals(section, communicationView);
        activityView.IsVisible = ReferenceEquals(section, activityView);
        if (!OperatingSystem.IsWindows())
        {
            Title = ExampleResources.Title;
        }
    }
    /// <summary>Gets whether the page is initialized and no native editor or navigation sheet owns input.</summary>
    private bool CanNavigate => preferencesReady && !settingsShowing && !navigationShowing
        && System.Threading.Volatile.Read(ref shuttingDown) == 0;
    /// <summary>Gets whether the current connection can safely stage and apply media settings.</summary>
    private bool CanEditMediaSettings => CanNavigate && !session.IsBusy
        && (Gurux.Common.MediaState)System.Threading.Volatile.Read(ref mediaPhase) == Gurux.Common.MediaState.Closed;

    /// <summary>Builds native Windows menu groups over the existing commands and content sections.</summary>
    private void CreateDesktopMenu()
    {
        var connection = new MenuBarItem { Text = ExampleResources.ConnectionMenu };
        connection.Add(new MenuFlyoutItem { Text = ExampleResources.Open, Command = openMenuCommand });
        connection.Add(new MenuFlyoutItem { Text = ExampleResources.Close, Command = closeMenuCommand });
        var views = new MenuBarItem { Text = ExampleResources.ViewMenu };
        views.Add(new MenuFlyoutItem { Text = ExampleResources.Data, Command = CreateViewCommand(dataView) });
        views.Add(new MenuFlyoutItem { Text = LayoutResources.ActivityGroup, Command = CreateViewCommand(activityView) });
        var settings = new MenuBarItem { Text = ExampleResources.Settings };
        settings.Add(new MenuFlyoutItem { Text = ExampleResources.MediaSettings, Command = settingsMenuCommand });
        settings.Add(new MenuFlyoutItem { Text = ExampleResources.CommunicationSettings, Command = CreateViewCommand(communicationView) });
        MenuBarItems.Add(connection);
        MenuBarItems.Add(views);
        MenuBarItems.Add(settings);
    }
    /// <summary>Creates a view command whose availability follows native modal ownership.</summary>
    /// <param name="section">The existing section to display when navigation is permitted.</param>
    /// <returns>A command retained for native enabled-state notifications.</returns>
    private Command CreateViewCommand(View section)
    {
        var command = new Command(() => ShowSection(section), () => CanNavigate);
        viewMenuCommands.Add(command);
        return command;
    }
    /// <summary>Shows platform-native mobile choices without replacing the active connection.</summary>
    /// <returns>A task completing after navigation, settings editing or dismissal.</returns>
    private async Task ShowMobileMenuAsync()
    {
        var choices = new List<string> { ExampleResources.Data };
        if (CanEditMediaSettings) { choices.Add(ExampleResources.MediaSettings); }
        choices.Add(ExampleResources.CommunicationSettings);
        choices.Add(LayoutResources.ActivityGroup);
        string? selected;
        navigationShowing = true;
        RefreshMenuCommands();
        try
        {
            selected = await DisplayActionSheetAsync(ExampleResources.Menu, ExampleResources.Cancel, null, choices.ToArray());
        }
        finally
        {
            navigationShowing = false;
            RefreshMenuCommands();
        }
        if (System.Threading.Volatile.Read(ref shuttingDown) != 0) { return; }
        if (selected == ExampleResources.Data) { ShowSection(dataView); }
        else if (selected == ExampleResources.CommunicationSettings) { ShowSection(communicationView); }
        else if (selected == LayoutResources.ActivityGroup) { ShowSection(activityView); }
        else if (selected == ExampleResources.MediaSettings && CanEditMediaSettings) { await EditMediaSettingsAsync(); }
    }
    /// <summary>Applies receive framing, opens the selected serial connection and persists any Android device choice before returning.</summary>
    /// <returns>A task completing when the connection opens or fails.</returns>
    /// <exception cref="FormatException">A framing value or hexadecimal terminator is malformed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The receive count or timeout is outside its supported range.</exception>
    /// <exception cref="OperationCanceledException">Shutdown cancels opening.</exception>
    private async Task OpenConnectionAsync()
    {
        if (OperatingSystem.IsAndroid() && string.IsNullOrWhiteSpace(session.Media.PortName))
        {
            session.Media.PortName = "android:select";
        }
        session.ConfigureReceive(hex.IsToggled, eop.Text,
            int.Parse(count.Text ?? "0", CultureInfo.CurrentCulture),
            int.Parse(timeout.Text ?? "5000", CultureInfo.CurrentCulture));
        try
        {
            await session.OpenAsync(lifetime.Token);
        }
        finally
        {
            await SavePreferencesAsync();
        }
    }
    /// <summary>Opens the existing media editor while the connection remains closed and idle.</summary>
    /// <returns>A task completing after editing or dismissal.</returns>
    private async Task EditMediaSettingsAsync()
    {
        if (CanEditMediaSettings)
        {
            settingsShowing = true;
            RefreshMenuCommands();
            try
            {
                JsonObject? usbSelection = ExampleUsbDeviceSelection.Capture(session.Media.PortSettings);
                await new MauiMediaSettingsEditor(this).ShowAsync(session.Media, cancellationToken: lifetime.Token);
                ExampleUsbDeviceSelection.Restore(session.Media.PortSettings, usbSelection);
                await SavePreferencesAsync();
            }
            finally
            {
                settingsShowing = false;
                RefreshMenuCommands();
            }
        }
    }
    /// <summary>Notifies native menus and mobile toolbar items after their lifecycle conditions change.</summary>
    private void RefreshMenuCommands()
    {
        openMenuCommand.ChangeCanExecute();
        closeMenuCommand.ChangeCanExecute();
        settingsMenuCommand.ChangeCanExecute();
        navigationCommand.ChangeCanExecute();
        foreach (var command in viewMenuCommands) { command.ChangeCanExecute(); }
    }
    /// <summary>Adds an asynchronous command to the native layout.</summary>
    /// <param name="parent">The layout receiving the command.</param>
    /// <param name="name">The stable automation identifier.</param>
    /// <param name="caption">The localized button caption.</param>
    /// <param name="action">The asynchronous command to execute.</param>
    /// <param name="unsafeAction">Whether connection state restricts this command.</param>
    private void AddButton(Layout parent, string name, string caption, Func<Task> action, bool unsafeAction = false)
    {
        var button = new Button { AutomationId = name, Text = caption, HorizontalOptions = LayoutOptions.Start };
        ExampleLayout.StyleButton(button, name is "Open" or "Send");
        button.Margin = new Thickness(0, 0, 8, 8);
        button.Clicked += async (_, _) => await RunAsync(action);
        parent.Children.Add(button);
        if (name == "Close") { closeButton = button; }
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
    /// <summary>Caches connection phases and schedules immediate native control updates.</summary>
    /// <param name="sender">The media reporting its lifecycle state.</param>
    /// <param name="args">The reported lifecycle phase or settings-change notification.</param>
    private void MediaStateChanged(object? sender, Gurux.Common.MediaStateEventArgs args)
    {
        if (System.Threading.Volatile.Read(ref shuttingDown) != 0) { return; }
        if (args.State != Gurux.Common.MediaState.Changed)
        {
            System.Threading.Volatile.Write(ref mediaPhase, (int)args.State);
        }
        if (uiDispatcher.IsDispatchRequired) { uiDispatcher.Dispatch(Refresh); }
        else { Refresh(); }
    }
    /// <summary>Polls selected port availability and refreshes connection status, controls, and activity history.</summary>
    private void Refresh()
    {
        if (System.Threading.Volatile.Read(ref shuttingDown) != 0)
        {
            UpdateConnectionProgress(false);
            foreach (var button in unsafeButtons) { button.IsEnabled = false; }
            if (closeButton is not null) { closeButton.IsEnabled = false; }
            RefreshMenuCommands();
            return;
        }
        var phase = (Gurux.Common.MediaState)System.Threading.Volatile.Read(ref mediaPhase);
        UpdateConnectionProgress(phase is Gurux.Common.MediaState.Opening or Gurux.Common.MediaState.Closing);
        bool closed = phase == Gurux.Common.MediaState.Closed;
        bool busy = session.IsBusy;
        bool open = phase == Gurux.Common.MediaState.Open;
        count.IsEnabled = closed && !busy;
        eop.IsEnabled = closed && !busy;
        timeout.IsEnabled = closed && !busy;
        if (!lifetime.IsCancellationRequested && !open && !busy)
        {
            _ = portAvailability.RefreshAsync(session.Media.PortName, lifetime.Token);
        }
        ExampleLayout.Status(state, busy, open);
        if (closeButton is not null) { closeButton.IsEnabled = open; }
        foreach (var button in unsafeButtons)
        {
            button.IsEnabled = (!OperatingSystem.IsIOS() || session.Media.PortName?.StartsWith("ble:", StringComparison.OrdinalIgnoreCase) == true || button.AutomationId is "Settings" or "RightSettings") && !busy && (button.AutomationId is "Settings" or "RightSettings" or "Open" ? closed : open)
                && (button.AutomationId != "Open" || (!lifetime.IsCancellationRequested && (OperatingSystem.IsAndroid() || session.CanAttemptOpen(portAvailability.IsAvailable))));
        }
        RefreshMenuCommands();
        string snapshot = string.Join(Environment.NewLine, session.Log);
        if (log.Text != snapshot) { log.Text = snapshot; }
        string receivedSnapshot = session.Received(hex.IsToggled);
        if (received.Text != receivedSnapshot) { received.Text = receivedSnapshot; }
        _ = SavePreferencesAsync();
        mediaStatus.Text = string.Format(System.Globalization.CultureInfo.CurrentCulture, StatusResources.StatusSerial,
            session.Media.IsOpen ? StatusResources.StatusOpen : StatusResources.StatusClosed,
            string.IsNullOrWhiteSpace(session.Media.PortName) ? StatusResources.StatusNotConfigured : session.Media.PortName);

    }
    /// <summary>Starts or stops an animated sweep without implying a known completion percentage.</summary>
    /// <param name="visible">Whether a connection open or close operation is in progress.</param>
    private void UpdateConnectionProgress(bool visible)
    {
        if (connectionProgress.IsVisible == visible) { return; }
        connectionProgress.IsVisible = visible;
        this.AbortAnimation("ConnectionProgress");
        connectionProgress.Progress = 0;
        if (visible)
        {
            new Animation(value => connectionProgress.Progress = value, 0, 1)
                .Commit(this, "ConnectionProgress", length: 1200, repeat: () => connectionProgress.IsVisible);
        }
    }
    /// <summary>Persists the latest device selection when Android stops the window.</summary>
    /// <returns>A task completing after changed settings have been saved or a save error has been displayed.</returns>
    internal Task SaveStateAsync() => SavePreferencesAsync();
    /// <summary>Shuts down the page and displays any transport shutdown error.</summary>
    /// <returns>A task completing when shutdown has finished.</returns>
    public async Task ShutdownAsync()
    {
        try { await DisposeAsync(); }
        catch (Exception ex) { error.Text = ex.Message; }
    }
    /// <inheritdoc/>
    public ValueTask DisposeAsync() => new(disposal ??= DisposeCoreAsync());
    /// <summary>Stops UI refresh and releases the owned serial connection.</summary>
    /// <returns>A task completing after transport and cancellation cleanup.</returns>
    private async Task DisposeCoreAsync()
    {
        System.Threading.Interlocked.Exchange(ref shuttingDown, 1);
        if (uiDispatcher.IsDispatchRequired) { uiDispatcher.Dispatch(Refresh); }
        else { Refresh(); }
        session.Media.OnMediaStateChange -= MediaStateChanged;
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
            ["AndroidUsbDevice"] = ExampleUsbDeviceSelection.Capture(session.Media.PortSettings),
            ["SendText"] = text.Text ?? "",
            ["Hex"] = hex.IsToggled,
            ["Echo"] = echo.IsToggled,
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
        ExampleUsbDeviceSelection.Restore(session.Media.PortSettings, values["AndroidUsbDevice"] as JsonObject);
        text.Text = values["SendText"]?.GetValue<string>() ?? text.Text;
        hex.IsToggled = values["Hex"]?.GetValue<bool>() ?? (hex.IsToggled);
        echo.IsToggled = values["Echo"]?.GetValue<bool>() ?? false;
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
        if (uiDispatcher.IsDispatchRequired)
        {
            await Microsoft.Maui.ApplicationModel.MainThread.InvokeOnMainThreadAsync(SavePreferencesAsync);
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
