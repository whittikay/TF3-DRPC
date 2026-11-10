using System.Threading;
using System.Threading.Tasks;

namespace Tf3Drpc.Linux.Services.Interfaces;

/// <summary>
/// Checks whether a desktop host can display a StatusNotifierItem.
/// </summary>
internal interface ITrayAvailabilityService
{
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken);
}
