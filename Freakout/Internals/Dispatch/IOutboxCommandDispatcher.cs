using System.Threading;
using System.Threading.Tasks;

namespace Freakout.Internals.Dispatch;

/// <summary>
/// Interface of Freakout's command dispatcher. This one basically defines what it means to process an <see cref="OutboxCommand"/>.
/// </summary>
public interface IOutboxCommandDispatcher
{
    /// <summary>
    /// This method will be called by Freakout to process the <paramref name="outboxCommand"/>. If it throws an exception,
    /// handling is considered as FAILED - if it doesn't, then it's considered SUCCESSFUL.
    /// </summary>
    Task ExecuteAsync(OutboxCommand outboxCommand, CancellationToken cancellationToken = default);
}