using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Freakout.MsSql;

class MsSqlOutbox(string connectionString) : IOutbox
{
    public async Task<IReadOnlyList<OutboxTask>> GetPendingOutboxTasksAsync(CancellationToken cancellationToken = default)
    {
        return new List<OutboxTask>();
    }
}