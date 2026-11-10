using System;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;

namespace Tf3Drpc.Linux;

/// <summary>
/// Owns the feasibility window and optional tray icon without starting the game monitor.
/// </summary>
#pragma warning disable CA1812 // Avoid uninstantiated internal classes
internal sealed class App : Application
#pragma warning restore CA1812
{
    private IDisposable? _smokeTimer;

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

            if (Program.SmokeTest)
                desktop.MainWindow.Opened += OnSmokeWindowOpened;
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void OnSmokeWindowOpened(object? sender, EventArgs args)
        => _smokeTimer = DispatcherTimer.RunOnce(() => Exit(this, EventArgs.Empty), TimeSpan.FromSeconds(3));

    private void ShowWindow(object? sender, EventArgs args)
    {
        if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime { MainWindow: Window window })
            return;

        window.Show();
        window.Activate();
    }

    private void Exit(object? sender, EventArgs args)
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.Shutdown();
    }

    private void OnExit(object? sender, ControlledApplicationLifetimeExitEventArgs args)
    {
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
