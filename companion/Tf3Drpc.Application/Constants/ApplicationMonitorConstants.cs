using System;

namespace Tf3Drpc.Application.Constants;

/// <summary>
/// Timing of the shared monitor loop.
/// </summary>
internal static class ApplicationMonitorConstants
{
    public static readonly TimeSpan TickInterval = TimeSpan.FromSeconds(1);
}
