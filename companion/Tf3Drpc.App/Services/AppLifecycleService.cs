using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;

using Tf3Drpc.App.Logging;
using Tf3Drpc.App.Services.Interfaces;

namespace Tf3Drpc.App.Services;

/// <summary>
/// Ends the app: stops the loop, clears the presence, then quits.
/// </summary>
#pragma warning disable CA1812 // Avoid uninstantiated internal classes
internal sealed class AppLifecycleService(IMonitorService monitor, IDiscordService discord, ILogger<AppLifecycleService> logger) : IAppLifecycleService
#pragma warning restore CA1812
{
    private readonly IMonitorService _monitor = monitor;
    private readonly IDiscordService _discord = discord;
    private readonly ILogger<AppLifecycleService> _logger = logger;
    private int _exiting;

    public event EventHandler? Exiting;

    public async Task ExitAsync()
    {
        if (Interlocked.Exchange(ref _exiting, 1) is not 0)
            return;

        _logger.AppExiting();
        await _monitor.StopAsync();
        await _discord.DisposeAsync();
        await MainThread.InvokeOnMainThreadAsync(RaiseExitingAndQuit);
    }

    private void RaiseExitingAndQuit()
    {
        Exiting?.Invoke(this, EventArgs.Empty);
        Microsoft.Maui.Controls.Application.Current?.Quit();
    }
}
