using System;
using System.Threading;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;

using Tf3Drpc.Linux.Services;

namespace Tf3Drpc.Linux;

/// <summary>
/// Owns the feasibility window and optional tray icon without starting the game monitor.
/// </summary>
#pragma warning disable CA1812 // Avoid uninstantiated internal classes
internal sealed class App : Application, IDisposable
#pragma warning restore CA1812
{
    private readonly TrayAvailabilityService _trayAvailability = new();
    private readonly CancellationTokenSource _lifetimeCancellation = new();
    private readonly DispatcherTimer _trayRecoveryTimer = new() { Interval = TimeSpan.FromSeconds(1) };
    private IDisposable? _smokeTimer;
    private int _visibilityVersion;
    private bool _checkingTray;
    private bool _exiting;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);

        if (!Program.DisableTray && Resources["SpikeTrayIcon"] is TrayIcon icon)
            TrayIcon.SetIcons(this, [icon]);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.ShutdownMode = ShutdownMode.OnMainWindowClose;
            desktop.MainWindow = new MainWindow();
            desktop.Exit += OnExit;
            _trayRecoveryTimer.Tick += OnTrayRecoveryTickAsync;

            if (Program.SmokeTest)
                desktop.MainWindow.Opened += OnSmokeWindowOpened;
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void OnSmokeWindowOpened(object? sender, EventArgs args)
        => _smokeTimer = DispatcherTimer.RunOnce(() => Exit(this, EventArgs.Empty), TimeSpan.FromSeconds(3));

    private void ShowWindow(object? sender, EventArgs args)
    {
        _visibilityVersion++;
        _trayRecoveryTimer.Stop();

        if (_exiting || ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime { MainWindow: Window window })
            return;

        window.Show();
        window.Activate();
    }

    private async void HideWindowAsync(object? sender, EventArgs args)
    {
        if (_exiting || Program.DisableTray)
            return;

        if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime { MainWindow: Window window })
            return;

        int version = ++_visibilityVersion;
        bool available = await _trayAvailability.IsAvailableAsync(_lifetimeCancellation.Token);

        if (_exiting || version != _visibilityVersion)
            return;

        if (!available)
        {
            ShowWindow(this, EventArgs.Empty);

            return;
        }

        window.Hide();
        _trayRecoveryTimer.Start();
    }

    private async void OnTrayRecoveryTickAsync(object? sender, EventArgs args)
    {
        if (_checkingTray || _exiting)
            return;

        _checkingTray = true;

        try
        {
            bool available = await _trayAvailability.IsAvailableAsync(_lifetimeCancellation.Token);

            if (!available && !_exiting)
                ShowWindow(this, EventArgs.Empty);
        }
        finally
        {
            _checkingTray = false;
        }
    }

    private void Exit(object? sender, EventArgs args)
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.Shutdown();
    }

    private void OnExit(object? sender, ControlledApplicationLifetimeExitEventArgs args)
        => Dispose();

    public void Dispose()
    {
        if (_exiting)
            return;

        _exiting = true;
        _visibilityVersion++;
        _trayRecoveryTimer.Stop();
        _trayRecoveryTimer.Tick -= OnTrayRecoveryTickAsync;
        _lifetimeCancellation.Cancel();
        _lifetimeCancellation.Dispose();
        _smokeTimer?.Dispose();
        _smokeTimer = null;

        if (TrayIcon.GetIcons(this) is not TrayIcons icons)
            return;

        foreach (TrayIcon icon in icons)
        {
            icon.Dispose();
        }
    }
}
