using System;

using CommunityToolkit.Mvvm.Messaging;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Maui;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;

using Tf3Drpc.App.Logging;
using Tf3Drpc.App.Messages;
using Tf3Drpc.App.Services.Interfaces;
using Tf3Drpc.App.Views;
using Tf3Drpc.Core.Enums;
using Tf3Drpc.Core.Settings;

namespace Tf3Drpc.App;

/// <summary>
/// The MAUI application: one hidden settings window, the tray icon and the monitor loop.
/// </summary>
#pragma warning disable CA1812 // Avoid uninstantiated internal classes
internal sealed partial class CompanionApplication : Microsoft.Maui.Controls.Application, IRecipient<SettingsChangedMessage>
#pragma warning restore CA1812
{
    private readonly IServiceProvider _services;
    private readonly IPathService _paths;
    private readonly ITrayService _tray;
    private readonly IMonitorService _monitor;
    private readonly IWindowService _windows;
    private readonly ILogger<CompanionApplication> _logger;
    private bool _started;

    public CompanionApplication
    (
        IServiceProvider services,
        IPathService paths,
        ITrayService tray,
        IMonitorService monitor,
        IWindowService windows,
        ISettingsService settings,
        IMessenger messenger,
        ISingleInstanceService singleInstance,
        ILogger<CompanionApplication> logger
    )
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(messenger);
        ArgumentNullException.ThrowIfNull(singleInstance);
        _services = services;
        _paths = paths;
        _tray = tray;
        _monitor = monitor;
        _windows = windows;
        _logger = logger;
        singleInstance.ActivationRequested += OnActivationRequested;
        messenger.Register<SettingsChangedMessage>(this);
        InitializeComponent();
        ApplyTheme(settings.Current.Theme);
    }

    public void Receive(SettingsChangedMessage message)
        => MainThread.BeginInvokeOnMainThread(() => ApplyTheme(message.Settings.Theme));

    protected override Window CreateWindow(IActivationState? activationState)
    {
        Window window = _services.GetRequiredService<SettingsWindow>();

        if (!_started)
        {
            _started = true;
            MainThread.BeginInvokeOnMainThread(StartServices);
        }

        return window;
    }

    private void OnActivationRequested(object? sender, EventArgs e)
        => _windows.ShowSettings();

    private void ApplyTheme(string setting)
    {
        ThemeMode mode = (ThemeCatalog.TryParse(setting, out ThemeMode parsed)) ? parsed : ThemeMode.System;

        AppTheme theme = mode switch
        {
            ThemeMode.Light => AppTheme.Light,
            ThemeMode.Dark => AppTheme.Dark,
            _ => AppTheme.Unspecified
        };

        if (UserAppTheme != theme)
            UserAppTheme = theme;
    }

    private void StartServices()
    {
        _logger.AppStarted(ThisAssembly.Info.InformationalVersion, _paths.DataDirectory);

        bool showSettings = false;

        if (_paths.IsTranslocated)
            _logger.AppTranslocated(_paths.DataDirectory);

        if (!_paths.IsDataWritable)
        {
            _logger.DataNotWritable(_paths.DataDirectory);
            showSettings = true;
        }
        else if (!_paths.IsLogDirectoryWritable)
        {
            _logger.LogDirectoryNotWritable(_paths.LogDirectory);
            showSettings = true;
        }

        // Without a tray icon the settings window is the only way left to reach the app and its Exit button.
        if (!_tray.Show())
            showSettings = true;

        if (showSettings)
            _windows.ShowSettings();

        _monitor.Start();
    }
}
