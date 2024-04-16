using System.Threading;
using System.Threading.Tasks;

namespace Freakout;

/// <summary>
/// Main store implementation. Must be implemented for the chosen type of technology chosen as the store
/// </summary>
public interface IOutboxCommandStore
{
    /// <summary>
    /// Must return an "store batch", which is 0..n store commands and a "completion method" (i.e. a way of marking
    /// the contained store commands as handled).
    /// </summary>
    Task<OutboxCommandBatch> GetPendingOutboxCommandsAsync(int commandProcessingBatchSize = 1, CancellationToken cancellationToken = default);
}