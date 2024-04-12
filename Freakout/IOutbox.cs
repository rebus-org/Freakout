using System.Threading;
using System.Threading.Tasks;

namespace Freakout;

/// <summary>
/// Main outbox implementation. Must be implemented for the chosen type of technology chosen as the outbox
/// </summary>
public interface IOutbox
{
    /// <summary>
    /// Must return an "outbox batch", which is 0..n outbox commands and a "completion method" (i.e. a way of marking
    /// the contained outbox commands as handled).
    /// </summary>
    Task<OutboxCommandBatch> GetPendingOutboxCommandsAsync(CancellationToken cancellationToken = default);
}