using System.Threading;
using System.Threading.Tasks;

namespace Freakout;

public interface IOutbox
{
    Task<OutboxCommandBatch> GetPendingOutboxCommandsAsync(CancellationToken cancellationToken = default);
}