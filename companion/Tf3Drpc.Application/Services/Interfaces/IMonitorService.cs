using System;
using System.Threading.Tasks;

using Tf3Drpc.Core.Structs;

namespace Tf3Drpc.Application.Services.Interfaces;

/// <summary>
/// The one background loop that follows the game and updates Discord.
/// </summary>
public interface IMonitorService : IAsyncDisposable
{
    /// <summary>
    /// The latest status; also available before the first tick.
    /// </summary>
    AppStatusStruct Status { get; }

    /// <summary>
    /// Starts the loop.
    /// </summary>
    void Start();

    /// <summary>
    /// Stops the loop and waits for it.
    /// </summary>
    Task StopAsync();
}
