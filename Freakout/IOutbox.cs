using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Freakout;

public interface IOutbox
{
    Task<IReadOnlyList<OutboxTask>> GetPendingOutboxTasksAsync(CancellationToken cancellationToken = default);
}

public abstract class OutboxTask
{
    public abstract Task ExecuteAsync();
}

