using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace Freakout.MsSql;

class MsSqlOutbox(string connectionString) : IOutbox
{
    public async Task<IReadOnlyList<OutboxTask>> GetPendingOutboxTasksAsync(CancellationToken cancellationToken = default)
    {
        return new List<OutboxTask>();
    }

    class MsSqlOutboxTask(Func<CancellationToken, Task> executeAsync, SqlConnection sqlConnection, SqlTransaction sqlTransaction) : OutboxTask
    {


        public override async Task ExecuteAsync()
        {
            
        }
    }
}