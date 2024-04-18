using System.Threading;
using System.Threading.Tasks;

namespace Freakout;

/// <summary>
/// Interface of Freakout's batch dispatcher. This one basically defines what it means to process an <see cref="OutboxCommandBatch"/> and how it's done.
/// </summary>
public interface IBatchDispatcher
{
    /// <summary>
    /// This method will be called by Freakout to process the <paramref name="batch"/>. If it throws an exception,
    /// handling is considered as FAILED - if it doesn't, then it's considered SUCCESSFUL.
    /// </summary>
    Task ExecuteAsync(OutboxCommandBatch batch, CancellationToken cancellationToken = default);
}