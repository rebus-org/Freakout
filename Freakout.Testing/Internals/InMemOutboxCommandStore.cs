using System.Threading;
using System.Threading.Tasks;
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

namespace Freakout.Testing.Internals;

class InMemOutboxCommandStore : IOutboxCommandStore
{
    public async Task<OutboxCommandBatch> GetPendingOutboxCommandsAsync(int commandProcessingBatchSize, CancellationToken cancellationToken = default) => OutboxCommandBatch.Empty;
}